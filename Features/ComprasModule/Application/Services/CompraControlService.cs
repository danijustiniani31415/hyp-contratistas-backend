using System.Net;
using System.Text;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.ComprasModule.Application.Services
{
    /// <summary>
    /// Control de lo que llega de los proveedores: dañado, faltante (saldo cerrado) y sobrante quedan como diferencia ABIERTA
    /// hasta que logística la resuelve (reposición, reclamo, nota de crédito o aceptado). Cada evento avisa por correo.
    /// </summary>
    public class CompraControlService
    {
        public static readonly string[] MotivosDanio = { "GOLPEADO", "ROTO", "HUMEDAD", "VENCIDO", "DEFECTUOSO", "OTRO" };
        public static readonly string[] MotivosFaltante = { "NO_ENVIARA", "SIN_STOCK_PROVEEDOR", "ANULADO_PROVEEDOR", "OTRO" };
        private static readonly HashSet<string> Resoluciones = new() { "REPOSICION", "RECLAMO", "NOTA_CREDITO", "ACEPTADO" };

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _email;
        private readonly FrontendSettings _front;
        private readonly ILogger<CompraControlService> _log;

        public CompraControlService(IDbContextFactory<AppDbContext> factory, IEmailService email, IOptions<FrontendSettings> front, ILogger<CompraControlService> log)
        {
            _factory = factory;
            _email = email;
            _front = front.Value;
            _log = log;
        }

        public async Task<CompraDiferenciasResumenDto> Listar(string? estado, long? ordenId, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var baseQ = from d in ctx.CompraDiferencia.AsNoTracking()
                        join o in ctx.OrdenCompra.AsNoTracking() on d.OrdenCompraId equals o.Id
                        select new { d, o };
            if (ordenId.HasValue) baseQ = baseQ.Where(x => x.d.OrdenCompraId == ordenId);
            var almacenes = await ctx.Almacen.AsNoTracking().Select(a => new { a.Id, a.Nombre, a.ProyectoId }).ToListAsync();
            var visibles = almacenes.Where(a => !a.ProyectoId.HasValue || scope.Permite(a.ProyectoId.Value)).Select(a => a.Id).ToList();
            baseQ = baseQ.Where(x => visibles.Contains(x.o.AlmacenId));

            var filas = await baseQ.OrderByDescending(x => x.d.CreadoEn).ToListAsync();
            var abiertas = filas.Count(x => x.d.Estado == "ABIERTA");
            var resueltas = filas.Count - abiertas;
            if (!string.IsNullOrWhiteSpace(estado)) filas = filas.Where(x => x.d.Estado == estado.ToUpperInvariant()).ToList();

            return new CompraDiferenciasResumenDto { Data = await Armar(ctx, filas.Select(x => x.d).ToList()), Abiertas = abiertas, Resueltas = resueltas };
        }

        internal static async Task<List<CompraDiferenciaDto>> Armar(AppDbContext ctx, List<CompraDiferencia> lista)
        {
            if (lista.Count == 0) return new();
            var ordenIds = lista.Select(d => d.OrdenCompraId).Distinct().ToList();
            var itemIds = lista.Select(d => d.OrdenCompraItemId).Distinct().ToList();
            var ordenes = await ctx.OrdenCompra.AsNoTracking().Where(o => ordenIds.Contains(o.Id))
                .Select(o => new { o.Id, o.Codigo, Proveedor = o.Proveedor!.RazonSocial, Almacen = o.Almacen!.Nombre }).ToDictionaryAsync(o => o.Id);
            var items = await ctx.OrdenCompraItem.AsNoTracking().Where(i => itemIds.Contains(i.Id))
                .Select(i => new { i.Id, Producto = i.Producto!.Nombre, i.Talla, i.CostoUnitario }).ToDictionaryAsync(i => i.Id);
            var usuarioIds = lista.Select(d => d.CreadoPor).Distinct().ToList();
            var nombres = await ctx.UsuarioSistema.AsNoTracking().Where(u => usuarioIds.Contains(u.Id))
                .Select(u => new { u.Id, N = u.Persona!.Apellidos + " " + u.Persona.Nombres }).ToDictionaryAsync(u => u.Id, u => u.N);
            return lista.Select(d =>
            {
                var o = ordenes[d.OrdenCompraId];
                var it = items[d.OrdenCompraItemId];
                return new CompraDiferenciaDto
                {
                    Id = d.Id, OrdenId = d.OrdenCompraId, OrdenCodigo = o.Codigo, ProveedorNombre = o.Proveedor, AlmacenNombre = o.Almacen,
                    ProductoNombre = it.Producto, Talla = it.Talla, Tipo = d.Tipo, Cantidad = d.Cantidad, Motivo = d.Motivo, Observacion = d.Observacion,
                    Estado = d.Estado, Resolucion = d.Resolucion, ResolucionNota = d.ResolucionNota, ReportadoPorNombre = nombres.GetValueOrDefault(d.CreadoPor),
                    CreadoEn = d.CreadoEn, ValorEstimado = Math.Round(d.Cantidad * it.CostoUnitario, 2),
                };
            }).ToList();
        }

        public async Task<CompraDiferenciaDto> Resolver(long id, CompraDiferenciaResolverDto dto, long usuarioId, LbScopeProyectos scope)
        {
            var resolucion = (dto.Resolucion ?? "").Trim().ToUpperInvariant();
            if (!Resoluciones.Contains(resolucion)) throw new AbrilException("Elige cómo se resuelve la diferencia.", 400);
            var nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim();
            if (nota == null || nota.Length < 3) throw new AbrilException("Escribe una nota que explique la resolución.", 400);

            using var ctx = _factory.CreateDbContext();
            var dif = await ctx.CompraDiferencia.FirstOrDefaultAsync(d => d.Id == id) ?? throw new AbrilException("Diferencia no encontrada.", 404);
            var orden = await ctx.OrdenCompra.Include(o => o.Items).Include(o => o.Almacen).FirstAsync(o => o.Id == dif.OrdenCompraId);
            if (orden.Almacen!.ProyectoId.HasValue && !scope.Permite(orden.Almacen.ProyectoId.Value))
                throw new AbrilException("No tienes permiso sobre las compras de este proyecto.", 403);
            if (dif.Estado != "ABIERTA") throw new AbrilException("Esta diferencia ya fue resuelta.", 400);
            if (dif.Tipo == "SOBRANTE" && resolucion != "ACEPTADO" && resolucion != "RECLAMO")
                throw new AbrilException("Un sobrante se acepta (queda en stock) o se reclama al proveedor para devolverlo.", 400);

            dif.Estado = "RESUELTA";
            dif.Resolucion = resolucion;
            dif.ResolucionNota = nota;
            dif.ResueltaPor = usuarioId;
            dif.ResueltaEn = DateTimeOffset.UtcNow;

            // Reposición de un faltante: el proveedor sí enviará el saldo, así que se reabre la orden para recibirlo.
            if (dif.Tipo == "FALTANTE" && resolucion == "REPOSICION")
            {
                var item = orden.Items.First(i => i.Id == dif.OrdenCompraItemId);
                item.CantidadFaltante = Math.Max(0m, item.CantidadFaltante - dif.Cantidad);
                orden.Estado = ComprasService.CalcularEstado(orden);
            }
            await ctx.SaveChangesAsync();

            var dto2 = (await Armar(ctx, new List<CompraDiferencia> { dif })).First();
            await NotificarResolucion(ctx, orden, dif, dto2, usuarioId);
            return dto2;
        }

        // ───────────────────────── Correos ─────────────────────────

        public static string NombreMotivo(string? m) => m switch
        {
            "GOLPEADO" => "Golpeado", "ROTO" => "Roto", "HUMEDAD" => "Humedad", "VENCIDO" => "Vencido", "DEFECTUOSO" => "Defectuoso",
            "NO_ENVIARA" => "El proveedor no lo enviará", "SIN_STOCK_PROVEEDOR" => "Proveedor sin stock", "ANULADO_PROVEEDOR" => "Anulado por el proveedor",
            "OTRO" => "Otro", _ => m ?? "—",
        };

        internal async Task NotificarRecepcion(AppDbContext ctx, OrdenCompra orden, OrdenCompraItem item, decimal ok, decimal danada, decimal sobrante, string? motivo, long usuarioId)
        {
            // Una recepción normal (todo bien, sin sobrante) no necesita correo: solo avisan las novedades.
            if (danada <= 0 && sobrante <= 0) return;
            var producto = await ctx.Producto.AsNoTracking().Where(p => p.Id == item.ProductoId).Select(p => p.Nombre).FirstOrDefaultAsync() ?? "Producto";
            var proveedor = await ctx.Proveedor.AsNoTracking().Where(p => p.Id == orden.ProveedorId).Select(p => p.RazonSocial).FirstOrDefaultAsync() ?? "";
            var quien = await NombreUsuario(ctx, usuarioId);
            var sb = new StringBuilder();
            sb.Append($"<p><b>{WebUtility.HtmlEncode(quien)}</b> registró una recepción de la orden <b>{WebUtility.HtmlEncode(orden.Codigo)}</b> ({WebUtility.HtmlEncode(proveedor)}) con novedades:</p><ul>");
            sb.Append($"<li>{WebUtility.HtmlEncode(producto)}: llegaron bien <b>{ok}</b>");
            if (danada > 0) sb.Append($", dañados <b>{danada}</b> ({WebUtility.HtmlEncode(NombreMotivo(motivo))})");
            if (sobrante > 0) sb.Append($", de más (sobrante) <b>{sobrante}</b>");
            sb.Append("</li></ul><p>Queda por resolver en <i>Diferencias de compras</i>.</p>");
            var correos = await Destinatarios(ctx, orden.Almacen?.ProyectoId ?? await ctx.Almacen.Where(a => a.Id == orden.AlmacenId).Select(a => a.ProyectoId).FirstOrDefaultAsync(),
                orden.SolicitadoPorUsuarioSistemaId, new[] { "LOGISTICA", "COMPRAS", "GERENTE_GENERAL" });
            await Enviar(correos, $"Compra {orden.Codigo}: recepción con novedades", sb.ToString(), "compras");
        }

        internal async Task NotificarCierre(AppDbContext ctx, OrdenCompra orden, List<CompraDiferencia> difs, string motivo, long usuarioId)
        {
            var itemIds = difs.Select(d => d.OrdenCompraItemId).ToList();
            var prods = await ctx.OrdenCompraItem.AsNoTracking().Where(i => itemIds.Contains(i.Id)).Select(i => new { i.Id, N = i.Producto!.Nombre }).ToDictionaryAsync(x => x.Id, x => x.N);
            var quien = await NombreUsuario(ctx, usuarioId);
            var filas = string.Concat(difs.Select(d => $"<li>{WebUtility.HtmlEncode(prods.GetValueOrDefault(d.OrdenCompraItemId, "Producto"))}: faltan <b>{d.Cantidad}</b></li>"));
            var cuerpo = $"<p><b>{WebUtility.HtmlEncode(quien)}</b> cerró el saldo de la orden <b>{WebUtility.HtmlEncode(orden.Codigo)}</b> ({WebUtility.HtmlEncode(NombreMotivo(motivo))}).</p><ul>{filas}</ul><p>Cada faltante queda por resolver en <i>Diferencias de compras</i>.</p>";
            var proyecto = await ctx.Almacen.Where(a => a.Id == orden.AlmacenId).Select(a => a.ProyectoId).FirstOrDefaultAsync();
            var correos = await Destinatarios(ctx, proyecto, orden.SolicitadoPorUsuarioSistemaId, new[] { "LOGISTICA", "COMPRAS", "GERENTE_GENERAL", "GERENTE_OPERACIONES" });
            await Enviar(correos, $"Compra {orden.Codigo}: saldo cerrado con faltante", cuerpo, "compras");
        }

        private async Task NotificarResolucion(AppDbContext ctx, OrdenCompra orden, CompraDiferencia dif, CompraDiferenciaDto dto, long usuarioId)
        {
            var quien = await NombreUsuario(ctx, usuarioId);
            var res = dif.Resolucion switch { "REPOSICION" => "Reposición", "RECLAMO" => "Reclamo al proveedor", "NOTA_CREDITO" => "Nota de crédito", _ => "Aceptado" };
            var cuerpo = $"<p><b>{WebUtility.HtmlEncode(quien)}</b> resolvió una diferencia de la orden <b>{WebUtility.HtmlEncode(orden.Codigo)}</b>: {dto.Cantidad} {WebUtility.HtmlEncode(dto.ProductoNombre)} ({dif.Tipo.ToLower()}) → <b>{res}</b>.</p><p><i>{WebUtility.HtmlEncode(dif.ResolucionNota ?? "")}</i></p>";
            var roles = new List<string> { "LOGISTICA", "COMPRAS", "GERENTE_GENERAL" };
            // Nota de crédito o aceptar con descuento cambian lo que se le debe al proveedor: contabilidad tiene que saberlo.
            if (dif.Resolucion is "NOTA_CREDITO" or "ACEPTADO") roles.Add("CONTADORA");
            var proyecto = await ctx.Almacen.Where(a => a.Id == orden.AlmacenId).Select(a => a.ProyectoId).FirstOrDefaultAsync();
            var correos = await Destinatarios(ctx, proyecto, dif.CreadoPor, roles.ToArray());
            await Enviar(correos, $"Compra {orden.Codigo}: diferencia resuelta ({res})", cuerpo, "compras");
        }

        private static async Task<string> NombreUsuario(AppDbContext ctx, long usuarioId) =>
            await ctx.UsuarioSistema.AsNoTracking().Where(u => u.Id == usuarioId).Select(u => u.Persona!.Apellidos + " " + u.Persona.Nombres).FirstOrDefaultAsync() ?? "Alguien";

        private static async Task<List<string>> Destinatarios(AppDbContext ctx, int? proyectoId, long? usuarioExtraId, string[] roles)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var correos = await (from a in ctx.UsuarioAsignacion.AsNoTracking()
                                 join r in ctx.Rol.AsNoTracking() on a.RolId equals r.Id
                                 join u in ctx.UsuarioSistema.AsNoTracking() on a.UsuarioSistemaId equals u.Id
                                 where u.Estado == "ACTIVO" && a.Notificar && (a.FechaFin == null || a.FechaFin >= hoy) && roles.Contains(r.Codigo)
                                       && (a.ProyectoId == null || proyectoId == null || a.ProyectoId == proyectoId)
                                 select u.EmailLogin).Distinct().ToListAsync();
            if (usuarioExtraId.HasValue)
            {
                var extra = await ctx.UsuarioSistema.AsNoTracking().Where(u => u.Id == usuarioExtraId && u.Estado == "ACTIVO").Select(u => u.EmailLogin).FirstOrDefaultAsync();
                if (extra != null && !correos.Contains(extra)) correos.Add(extra);
            }
            return correos;
        }

        private async Task Enviar(List<string> destinatarios, string asunto, string cuerpoHtml, string ruta)
        {
            if (destinatarios.Count == 0) return;
            var link = string.IsNullOrWhiteSpace(_front.LbAppUrl) ? "" : $"<a href='{_front.LbAppUrl.TrimEnd('/')}/{ruta}' style='background:#0F172A;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Abrir en el sistema</a>";
            try
            {
                await _email.SendAsync(to: destinatarios, subject: asunto, body: $"<h2 style='font-size:17px'>{WebUtility.HtmlEncode(asunto)}</h2>{cuerpoHtml}{link}", isHtml: true);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "[Control compras] No se pudo enviar el correo «{Asunto}» a {Destinatarios}", asunto, string.Join(", ", destinatarios));
            }
        }
    }
}
