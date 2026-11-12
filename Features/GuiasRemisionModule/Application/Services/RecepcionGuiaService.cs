using System.Net;
using System.Text;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Services;
using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.GuiasRemisionModule.Application.Services
{
    /// <summary>
    /// Recepción con control de las guías de remisión: el almacenero declara por viaje qué llegó bien, qué llegó dañado y, al
    /// cerrar, qué no llegó. El stock del destino sube solo con lo que llegó bien. Lo faltante o dañado queda como diferencia
    /// ABIERTA hasta que logística la resuelve (reposición, reclamo, merma o "llegó después"). Cada evento avisa por correo.
    /// </summary>
    public class RecepcionGuiaService
    {
        public static readonly string[] MotivosDanio = { "GOLPEADO", "ROTO", "HUMEDAD", "VENCIDO", "DEFECTUOSO", "OTRO" };
        public static readonly string[] MotivosFaltante = { "NO_LLEGO", "LLEGO_INCOMPLETO", "EXTRAVIADO", "OTRO" };
        private static readonly HashSet<string> Resoluciones = new() { "REPOSICION", "RECLAMO", "MERMA", "LLEGO" };
        private static readonly HashSet<string> MimesFoto = new() { "image/jpeg", "image/png", "image/webp" };
        private const decimal Tol = 0.004m;

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _email;
        private readonly FrontendSettings _front;
        private readonly ILogger<RecepcionGuiaService> _log;

        public RecepcionGuiaService(IDbContextFactory<AppDbContext> factory, IEmailService email, IOptions<FrontendSettings> front, ILogger<RecepcionGuiaService> log)
        {
            _factory = factory;
            _email = email;
            _front = front.Value;
            _log = log;
        }

        private static decimal Pendiente(GuiaRemisionItem i) => i.Cantidad - (i.CantidadConfirmada ?? 0) - i.CantidadDanada - i.CantidadFaltante;

        public static string CodigoGuia(GuiaRemision g) => g.Numero == null ? $"Borrador #{g.Id}" : $"{g.Serie}-{g.Numero:D8}";

        // ───────────────────────── Recepción ─────────────────────────

        public async Task<RecepcionResultadoDto> Registrar(long guiaId, RecepcionRegistrarDto dto, long usuarioId, LbScopeProyectos scope)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new AbrilException("Indica qué llegó de al menos un producto.", 400);
            if (dto.Items.Select(i => i.ItemId).Distinct().Count() != dto.Items.Count)
                throw new AbrilException("Un mismo producto está repetido en la recepción.", 400);

            using var ctx = _factory.CreateDbContext();
            var resumen = new List<LineaResumen>();
            long recepcionId = 0;
            string estadoFinal = "";
            var nuevas = 0;
            GuiaRemision guiaCorreo = null!;
            int? proyectoDestino = null;

            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                ctx.ChangeTracker.Clear();
                resumen.Clear();
                nuevas = 0;
                using var tx = await ctx.Database.BeginTransactionAsync();

                var guia = await ctx.GuiaRemision
                    .Include(g => g.AlmacenDestino)
                    .Include(g => g.Items).ThenInclude(i => i.Producto)
                    .FirstOrDefaultAsync(g => g.Id == guiaId)
                    ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

                if (guia.AlmacenDestinoId is null || guia.AlmacenDestino is null)
                    throw new AbrilException("Esta guía no tiene almacén de destino — no hay nada que recibir.", 400);
                if (guia.AlmacenDestino.ProyectoId.HasValue && !scope.Permite(guia.AlmacenDestino.ProyectoId.Value))
                    throw new AbrilException("No tienes permiso para recibir en el proyecto de destino de esta guía.", 403);
                if (guia.Estado != "ACEPTADA")
                    throw new AbrilException($"Esta guía todavía no fue aceptada por SUNAT (estado {guia.Estado}) — recíbela cuando esté ACEPTADA.", 400);
                if (guia.RecepcionEstado is "COMPLETA" or "CON_DIFERENCIAS")
                    throw new AbrilException("La recepción de esta guía ya está cerrada.", 409);

                var itemsGuia = guia.Items.ToDictionary(i => i.Id);
                foreach (var d in dto.Items)
                    if (!itemsGuia.ContainsKey(d.ItemId)) throw new AbrilException($"El ítem {d.ItemId} no pertenece a esta guía.", 404);

                var ahora = DateTimeOffset.UtcNow;
                var recepcion = new GuiaRecepcion
                {
                    GuiaRemisionId = guia.Id, RecibidoPor = usuarioId, RecibidoEn = ahora, Cierra = dto.Cierra,
                    Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(),
                };
                ctx.GuiaRecepcion.Add(recepcion);
                await ctx.SaveChangesAsync();

                var algo = false;
                foreach (var item in guia.Items)
                {
                    var d = dto.Items.FirstOrDefault(x => x.ItemId == item.Id);
                    var ok = Math.Round(d?.CantidadOk ?? 0, 2);
                    var danada = Math.Round(d?.CantidadDanada ?? 0, 2);
                    var nombre = item.Producto?.Nombre ?? item.Descripcion ?? $"ítem {item.Id}";
                    if (ok < 0 || danada < 0) throw new AbrilException($"Las cantidades de «{nombre}» no pueden ser negativas.", 400);

                    var pendiente = Pendiente(item);
                    if (ok + danada > pendiente + Tol)
                        throw new AbrilException($"De «{nombre}» solo quedan {pendiente:0.##} por declarar (pusiste {ok + danada:0.##}).", 400);

                    var faltante = dto.Cierra ? Math.Round(pendiente - ok - danada, 2) : 0m;
                    if (faltante < 0) faltante = 0;
                    if (ok == 0 && danada == 0 && faltante == 0) continue;

                    var motivoDeclarado = d?.Motivo?.Trim().ToUpperInvariant();
                    string? motivoDanio = null;
                    if (danada > 0)
                    {
                        if (string.IsNullOrWhiteSpace(motivoDeclarado) || !MotivosDanio.Contains(motivoDeclarado))
                            throw new AbrilException($"Indica el motivo del daño de «{nombre}».", 400);
                        motivoDanio = motivoDeclarado;
                    }
                    // Lo que no llegó se registra como "No llegó" salvo que se haya indicado otro motivo de faltante.
                    var motivoFaltante = faltante > 0 ? (motivoDeclarado != null && MotivosFaltante.Contains(motivoDeclarado) ? motivoDeclarado : "NO_LLEGO") : null;
                    var motivo = motivoDanio ?? motivoFaltante;

                    // ── Stock del destino ──
                    if (item.ProductoId.HasValue)
                    {
                        if (guia.DestinoAcreditado)
                        {
                            // Guía anterior al control: el destino ya recibió todo al aceptarse; se descuenta lo que no sirvió o no llegó.
                            var aDescontar = danada + faltante;
                            if (aDescontar > 0)
                                await AlmacenKardexService.AplicarMovimientoAsync(ctx, new RegistrarMovimientoDto
                                {
                                    AlmacenId = guia.AlmacenDestinoId.Value, ProductoId = item.ProductoId.Value, Talla = item.Talla, Color = item.Color,
                                    TipoMovimiento = "SALIDA", Cantidad = aDescontar, ReferenciaTipo = "GUIA_REMISION_AJUSTE", ReferenciaId = guia.Id,
                                }, usuarioId);
                        }
                        else if (ok > 0)
                        {
                            await AlmacenKardexService.AplicarMovimientoAsync(ctx, new RegistrarMovimientoDto
                            {
                                AlmacenId = guia.AlmacenDestinoId.Value, ProductoId = item.ProductoId.Value, Talla = item.Talla, Color = item.Color,
                                TipoMovimiento = "INGRESO", Cantidad = ok, CostoUnitario = await CostoDeSalida(ctx, guia.Id, item),
                                ReferenciaTipo = "GUIA_REMISION_RECEPCION", ReferenciaId = guia.Id,
                            }, usuarioId);
                        }
                    }

                    if (ok > 0 && item.PedidoItemId.HasValue)
                    {
                        var pi = await ctx.PedidoItem.FindAsync(item.PedidoItemId.Value);
                        if (pi != null) pi.CantidadConfirmadaMina += ok;
                    }

                    item.CantidadConfirmada = (item.CantidadConfirmada ?? 0) + ok;
                    item.CantidadDanada += danada;
                    item.CantidadFaltante += faltante;
                    item.ConfirmadoEn ??= ahora;
                    item.ConfirmadoPorUsuarioSistemaId = usuarioId;

                    ctx.GuiaRecepcionItem.Add(new GuiaRecepcionItem
                    {
                        RecepcionId = recepcion.Id, GuiaItemId = item.Id, CantidadOk = ok, CantidadDanada = danada, CantidadFaltante = faltante,
                        Motivo = motivo, Observacion = string.IsNullOrWhiteSpace(d?.Observacion) ? null : d!.Observacion!.Trim(),
                    });
                    if (danada > 0)
                    {
                        ctx.GuiaDiferencia.Add(new GuiaDiferencia { GuiaRemisionId = guia.Id, GuiaItemId = item.Id, RecepcionId = recepcion.Id, Tipo = "DANADO", Cantidad = danada, Motivo = motivoDanio, Observacion = d?.Observacion?.Trim(), CreadoPor = usuarioId, CreadoEn = ahora });
                        nuevas++;
                    }
                    if (faltante > 0)
                    {
                        ctx.GuiaDiferencia.Add(new GuiaDiferencia { GuiaRemisionId = guia.Id, GuiaItemId = item.Id, RecepcionId = recepcion.Id, Tipo = "FALTANTE", Cantidad = faltante, Motivo = motivoFaltante, Observacion = d?.Observacion?.Trim(), CreadoPor = usuarioId, CreadoEn = ahora });
                        nuevas++;
                    }
                    resumen.Add(new LineaResumen(nombre, item.Talla, ok, danada, faltante, motivo));
                    algo = true;
                }
                if (!algo) throw new AbrilException("Indica qué llegó: escribe al menos una cantidad.", 400);

                var todoDeclarado = guia.Items.All(i => Pendiente(i) <= Tol);
                var cerrada = dto.Cierra || todoDeclarado;
                recepcion.Cierra = cerrada;
                if (cerrada)
                {
                    guia.RecepcionCerradaEn = ahora;
                    guia.RecepcionEstado = guia.Items.Any(i => i.CantidadDanada > 0 || i.CantidadFaltante > 0) ? "CON_DIFERENCIAS" : "COMPLETA";
                }
                else guia.RecepcionEstado = "PARCIAL";

                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
                recepcionId = recepcion.Id;
                estadoFinal = guia.RecepcionEstado;
                proyectoDestino = guia.AlmacenDestino.ProyectoId;
                guiaCorreo = guia;
            });

            await NotificarRecepcion(ctx, guiaCorreo, estadoFinal, resumen, usuarioId, proyectoDestino);
            return new RecepcionResultadoDto { RecepcionId = recepcionId, RecepcionEstado = estadoFinal, DiferenciasNuevas = nuevas };
        }

        private static async Task<decimal?> CostoDeSalida(AppDbContext ctx, long guiaId, GuiaRemisionItem item)
        {
            var c = await ctx.Movimiento.AsNoTracking()
                .Where(m => m.ReferenciaTipo == "GUIA_REMISION" && m.ReferenciaId == guiaId && m.TipoMovimiento == "SALIDA"
                            && m.ProductoId == item.ProductoId && m.Talla == item.Talla && m.Color == item.Color)
                .Select(m => m.CostoUnitario).FirstOrDefaultAsync();
            return c > 0 ? c : null;
        }

        // ───────────────────────── Fotos ─────────────────────────

        public async Task<long> GuardarFoto(long recepcionId, string nombre, string mime, byte[] contenido, long usuarioId)
        {
            if (!MimesFoto.Contains(mime)) throw new AbrilException("La foto debe ser JPG, PNG o WEBP.", 400);
            if (contenido.Length == 0 || contenido.Length > 6 * 1024 * 1024) throw new AbrilException("La foto debe pesar menos de 6 MB.", 400);
            using var ctx = _factory.CreateDbContext();
            var rec = await ctx.GuiaRecepcion.AsNoTracking().FirstOrDefaultAsync(r => r.Id == recepcionId) ?? throw new AbrilException("Recepción no encontrada.", 404);
            if (rec.RecibidoPor != usuarioId) throw new AbrilException("Solo quien registró la recepción puede adjuntar fotos.", 403);
            if (await ctx.GuiaRecepcionFoto.CountAsync(f => f.RecepcionId == recepcionId) >= 5) throw new AbrilException("Máximo 5 fotos por recepción.", 400);
            var foto = new GuiaRecepcionFoto { RecepcionId = recepcionId, Nombre = nombre.Length > 200 ? nombre[..200] : nombre, Mime = mime, Contenido = contenido, CreadoEn = DateTimeOffset.UtcNow };
            ctx.GuiaRecepcionFoto.Add(foto);
            await ctx.SaveChangesAsync();
            return foto.Id;
        }

        public async Task<(byte[] Contenido, string Mime)> ObtenerFoto(long fotoId)
        {
            using var ctx = _factory.CreateDbContext();
            var f = await ctx.GuiaRecepcionFoto.AsNoTracking().FirstOrDefaultAsync(x => x.Id == fotoId) ?? throw new AbrilException("Foto no encontrada.", 404);
            return (f.Contenido, f.Mime);
        }

        // ───────────────────────── Diferencias ─────────────────────────

        public async Task<DiferenciasResumenDto> ListarDiferencias(string? estado, long? guiaId, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.GuiaDiferencia.AsNoTracking().AsQueryable();
            if (guiaId.HasValue) q = q.Where(d => d.GuiaRemisionId == guiaId.Value);
            if (!scope.EsGlobal)
            {
                var ids = scope.ProyectoIds;
                var guias = ctx.GuiaRemision.Where(g => (g.AlmacenDestino != null && g.AlmacenDestino.ProyectoId != null && ids.Contains(g.AlmacenDestino.ProyectoId.Value))
                                                       || (g.AlmacenOrigen!.ProyectoId != null && ids.Contains(g.AlmacenOrigen.ProyectoId.Value))).Select(g => g.Id);
                q = q.Where(d => guias.Contains(d.GuiaRemisionId));
            }
            var abiertas = await q.CountAsync(d => d.Estado == "ABIERTA");
            var resueltas = await q.CountAsync(d => d.Estado == "RESUELTA");
            if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(d => d.Estado == estado);
            var lista = await q.OrderBy(d => d.Estado == "RESUELTA").ThenByDescending(d => d.CreadoEn).Take(300).ToListAsync();
            return new DiferenciasResumenDto { Abiertas = abiertas, Resueltas = resueltas, Data = await Armar(ctx, lista) };
        }

        /// <summary>Convierte diferencias en DTOs con nombres y valor estimado (a costo de salida del origen).</summary>
        internal static async Task<List<DiferenciaDto>> Armar(AppDbContext ctx, List<GuiaDiferencia> lista)
        {
            if (lista.Count == 0) return new List<DiferenciaDto>();
            var guiaIds = lista.Select(d => d.GuiaRemisionId).Distinct().ToList();
            var itemIds = lista.Select(d => d.GuiaItemId).Distinct().ToList();
            var guias = await ctx.GuiaRemision.AsNoTracking().Include(g => g.AlmacenDestino).Where(g => guiaIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id);
            var items = await ctx.GuiaRemisionItem.AsNoTracking().Include(i => i.Producto).Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
            var userIds = lista.Select(d => d.CreadoPor).Concat(lista.Where(d => d.ResueltaPor.HasValue).Select(d => d.ResueltaPor!.Value)).Distinct().ToList();
            var nombres = await ctx.UsuarioSistema.AsNoTracking().Include(u => u.Persona).Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.Persona!.Apellidos} {u.Persona.Nombres}".Trim());
            var salidas = await ctx.Movimiento.AsNoTracking()
                .Where(m => m.ReferenciaTipo == "GUIA_REMISION" && m.TipoMovimiento == "SALIDA" && m.ReferenciaId != null && guiaIds.Contains(m.ReferenciaId.Value))
                .Select(m => new { m.ReferenciaId, m.ProductoId, m.Talla, m.Color, m.CostoUnitario }).ToListAsync();

            return lista.Select(d =>
            {
                var g = guias[d.GuiaRemisionId];
                var it = items[d.GuiaItemId];
                var costo = salidas.FirstOrDefault(s => s.ReferenciaId == g.Id && s.ProductoId == it.ProductoId && s.Talla == it.Talla && s.Color == it.Color)?.CostoUnitario ?? 0m;
                return new DiferenciaDto
                {
                    Id = d.Id, GuiaId = g.Id, GuiaCodigo = CodigoGuia(g), AlmacenDestinoNombre = g.AlmacenDestino?.Nombre,
                    ProductoNombre = it.Producto?.Nombre ?? it.Descripcion ?? "", Talla = it.Talla, UnidadMedida = it.UnidadMedida,
                    Tipo = d.Tipo, Cantidad = d.Cantidad, Motivo = d.Motivo, Observacion = d.Observacion, Estado = d.Estado,
                    Resolucion = d.Resolucion, ResolucionNota = d.ResolucionNota, CreadoEn = d.CreadoEn, ResueltaEn = d.ResueltaEn,
                    ReportadoPorNombre = nombres.GetValueOrDefault(d.CreadoPor),
                    ResueltaPorNombre = d.ResueltaPor.HasValue ? nombres.GetValueOrDefault(d.ResueltaPor.Value) : null,
                    ValorEstimado = Math.Round(costo * d.Cantidad, 2),
                };
            }).ToList();
        }

        /// <summary>Recepciones (viajes) de una guía con sus líneas y fotos, para el detalle.</summary>
        internal static async Task<List<RecepcionHistorialDto>> Historial(AppDbContext ctx, long guiaId)
        {
            var recs = await ctx.GuiaRecepcion.AsNoTracking().Where(r => r.GuiaRemisionId == guiaId).OrderBy(r => r.RecibidoEn).ToListAsync();
            if (recs.Count == 0) return new List<RecepcionHistorialDto>();
            var ids = recs.Select(r => r.Id).ToList();
            var lineas = await ctx.GuiaRecepcionItem.AsNoTracking().Where(i => ids.Contains(i.RecepcionId)).ToListAsync();
            var itemIds = lineas.Select(l => l.GuiaItemId).Distinct().ToList();
            var items = await ctx.GuiaRemisionItem.AsNoTracking().Include(i => i.Producto).Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
            var fotos = await ctx.GuiaRecepcionFoto.AsNoTracking().Where(f => ids.Contains(f.RecepcionId)).Select(f => new { f.Id, f.RecepcionId }).ToListAsync();
            var userIds = recs.Select(r => r.RecibidoPor).Distinct().ToList();
            var nombres = await ctx.UsuarioSistema.AsNoTracking().Include(u => u.Persona).Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.Persona!.Apellidos} {u.Persona.Nombres}".Trim());
            return recs.Select(r => new RecepcionHistorialDto
            {
                Id = r.Id, RecibidoEn = r.RecibidoEn, RecibidoPorNombre = nombres.GetValueOrDefault(r.RecibidoPor, ""), Cierra = r.Cierra, Observacion = r.Observacion,
                FotoIds = fotos.Where(f => f.RecepcionId == r.Id).Select(f => f.Id).ToList(),
                Items = lineas.Where(l => l.RecepcionId == r.Id).Select(l =>
                {
                    var it = items[l.GuiaItemId];
                    return new RecepcionHistorialItemDto
                    {
                        ProductoNombre = it.Producto?.Nombre ?? it.Descripcion ?? "", Talla = it.Talla, CantidadOk = l.CantidadOk,
                        CantidadDanada = l.CantidadDanada, CantidadFaltante = l.CantidadFaltante, Motivo = l.Motivo, Observacion = l.Observacion,
                    };
                }).ToList(),
            }).ToList();
        }

        public async Task<DiferenciaDto> Resolver(long id, DiferenciaResolverDto dto, long usuarioId, LbScopeProyectos scope)
        {
            var resolucion = (dto.Resolucion ?? "").Trim().ToUpperInvariant();
            if (!Resoluciones.Contains(resolucion)) throw new AbrilException("Elige cómo se resuelve: reposición, reclamo, merma o llegó.", 400);
            var nota = dto.Nota?.Trim();
            if (resolucion != "LLEGO" && (string.IsNullOrWhiteSpace(nota) || nota.Length < 3))
                throw new AbrilException("Escribe una nota que explique la resolución.", 400);

            using var ctx = _factory.CreateDbContext();
            GuiaRemision guiaCorreo = null!;
            GuiaDiferencia? dif = null;
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                ctx.ChangeTracker.Clear();
                using var tx = await ctx.Database.BeginTransactionAsync();
                dif = await ctx.GuiaDiferencia.FirstOrDefaultAsync(d => d.Id == id) ?? throw new AbrilException("Diferencia no encontrada.", 404);
                var guia = await ctx.GuiaRemision.Include(g => g.AlmacenDestino).Include(g => g.AlmacenOrigen).Include(g => g.Items).ThenInclude(i => i.Producto)
                    .FirstAsync(g => g.Id == dif.GuiaRemisionId);
                var proy = guia.AlmacenDestino?.ProyectoId ?? guia.AlmacenOrigen?.ProyectoId;
                if (proy.HasValue && !scope.Permite(proy.Value)) throw new AbrilException("No tienes permiso sobre el proyecto de esta guía.", 403);
                if (dif.Estado != "ABIERTA") throw new AbrilException("Esta diferencia ya fue resuelta.", 409);
                var item = guia.Items.First(i => i.Id == dif.GuiaItemId);

                if (resolucion == "LLEGO")
                {
                    if (dif.Tipo != "FALTANTE") throw new AbrilException("Solo lo faltante puede \"llegar después\"; lo dañado se repone, se reclama o se da de baja.", 400);
                    if (guia.AlmacenDestinoId.HasValue && item.ProductoId.HasValue)
                        await AlmacenKardexService.AplicarMovimientoAsync(ctx, new RegistrarMovimientoDto
                        {
                            AlmacenId = guia.AlmacenDestinoId.Value, ProductoId = item.ProductoId.Value, Talla = item.Talla, Color = item.Color,
                            TipoMovimiento = "INGRESO", Cantidad = dif.Cantidad, CostoUnitario = await CostoDeSalida(ctx, guia.Id, item),
                            ReferenciaTipo = "GUIA_REMISION_RECEPCION", ReferenciaId = guia.Id,
                        }, usuarioId);
                    item.CantidadFaltante = Math.Max(0, item.CantidadFaltante - dif.Cantidad);
                    item.CantidadConfirmada = (item.CantidadConfirmada ?? 0) + dif.Cantidad;
                    if (item.PedidoItemId.HasValue)
                    {
                        var pi = await ctx.PedidoItem.FindAsync(item.PedidoItemId.Value);
                        if (pi != null) pi.CantidadConfirmadaMina += dif.Cantidad;
                    }
                    if (guia.RecepcionEstado is "CON_DIFERENCIAS" or "COMPLETA")
                        guia.RecepcionEstado = guia.Items.Any(i => i.CantidadDanada > 0 || i.CantidadFaltante > 0) ? "CON_DIFERENCIAS" : "COMPLETA";
                }

                dif.Estado = "RESUELTA";
                dif.Resolucion = resolucion;
                dif.ResolucionNota = string.IsNullOrWhiteSpace(nota) ? null : nota;
                dif.ResueltaPor = usuarioId;
                dif.ResueltaEn = DateTimeOffset.UtcNow;
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
                guiaCorreo = guia;
            });

            var resultado = (await Armar(ctx, new List<GuiaDiferencia> { dif! }))[0];
            await NotificarResolucion(ctx, guiaCorreo, dif!, resultado, usuarioId);
            return resultado;
        }

        // ───────────────────────── Recordatorio ─────────────────────────

        /// <summary>Guías aceptadas hace más de 24 h con destino propio y sin ninguna recepción: avisa una sola vez al destino y a logística.</summary>
        public async Task<int> EnviarRecordatorios()
        {
            using var ctx = _factory.CreateDbContext();
            var limite = DateTimeOffset.UtcNow.AddHours(-24);
            var guias = await ctx.GuiaRemision.Include(g => g.AlmacenDestino).Include(g => g.AlmacenOrigen).Include(g => g.Items).ThenInclude(i => i.Producto)
                .Where(g => g.Estado == "ACEPTADA" && g.AlmacenDestinoId != null && g.RecepcionEstado == "PENDIENTE" && g.RecordatorioEnviadoEn == null
                            && g.RespondidoEn != null && g.RespondidoEn <= limite && !g.Items.Any(i => i.ConfirmadoEn != null))
                .ToListAsync();
            var enviados = 0;
            foreach (var g in guias)
            {
                var correos = await Destinatarios(ctx, g.AlmacenDestino?.ProyectoId, g.CreadoPorUsuarioSistemaId, new[] { "ALMACENERO", "RESIDENTE", "ADMINISTRATIVO" });
                var filas = string.Join("", g.Items.Select(i => $"<tr><td style='padding:4px 10px'>{WebUtility.HtmlEncode(i.Producto?.Nombre ?? i.Descripcion ?? "")} {WebUtility.HtmlEncode(i.Talla)}</td><td style='padding:4px 10px;text-align:right'>{i.Cantidad:0.##}</td></tr>"));
                var ok = await Enviar(correos, $"Recordatorio: la guía {CodigoGuia(g)} sigue sin recibirse",
                    $"<p>La guía <strong>{CodigoGuia(g)}</strong> con destino <strong>{WebUtility.HtmlEncode(g.AlmacenDestino?.Nombre)}</strong> fue aceptada hace más de 24 horas y todavía no se registra su recepción.</p>" +
                    $"<table style='border-collapse:collapse;font-size:13px'>{filas}</table><p>Si la mercadería ya llegó, regístrala en Guías de Remisión → Recibir. Si no llegó, avisa a logística.</p>", "guias-remision");
                g.RecordatorioEnviadoEn = DateTimeOffset.UtcNow; // una sola vez, aunque el correo falle: no insistir cada hora
                if (ok) enviados++;
            }
            await ctx.SaveChangesAsync();
            return enviados;
        }

        // ───────────────────────── Correos ─────────────────────────

        private record LineaResumen(string Producto, string Talla, decimal Ok, decimal Danada, decimal Faltante, string? Motivo);

        private static string NombreMotivo(string? m) => m switch
        {
            "NO_LLEGO" => "No llegó", "LLEGO_INCOMPLETO" => "Llegó incompleto", "EXTRAVIADO" => "Extraviado", "GOLPEADO" => "Golpeado", "ROTO" => "Roto",
            "HUMEDAD" => "Humedad", "VENCIDO" => "Vencido", "DEFECTUOSO" => "Defectuoso", "OTRO" => "Otro", _ => "",
        };

        private async Task NotificarRecepcion(AppDbContext ctx, GuiaRemision guia, string estado, List<LineaResumen> lineas, long usuarioId, int? proyecto)
        {
            var quien = await NombreUsuario(ctx, usuarioId);
            var codigo = CodigoGuia(guia);
            var conDif = lineas.Any(l => l.Danada > 0 || l.Faltante > 0);
            var filas = new StringBuilder();
            foreach (var l in lineas)
                filas.Append($"<tr><td style='padding:4px 10px'>{WebUtility.HtmlEncode(l.Producto)} {WebUtility.HtmlEncode(l.Talla)}</td><td style='padding:4px 10px;text-align:right'>{l.Ok:0.##}</td>" +
                             $"<td style='padding:4px 10px;text-align:right;color:#b45309'>{(l.Danada > 0 ? l.Danada.ToString("0.##") : "")}</td>" +
                             $"<td style='padding:4px 10px;text-align:right;color:#c0392b'>{(l.Faltante > 0 ? l.Faltante.ToString("0.##") : "")}</td><td style='padding:4px 10px'>{NombreMotivo(l.Motivo)}</td></tr>");
            var tabla = "<table style='border-collapse:collapse;font-size:13px'><tr style='background:#f1f5f9'><th style='padding:4px 10px;text-align:left'>Producto</th><th style='padding:4px 10px'>Llegó bien</th><th style='padding:4px 10px'>Dañado</th><th style='padding:4px 10px'>Faltó</th><th style='padding:4px 10px;text-align:left'>Motivo</th></tr>" + filas + "</table>";
            var destino = WebUtility.HtmlEncode(guia.AlmacenDestino?.Nombre);

            string asunto, intro;
            if (estado == "CON_DIFERENCIAS") { asunto = $"Guía {codigo}: recepción con diferencias en {guia.AlmacenDestino?.Nombre}"; intro = $"<strong>{WebUtility.HtmlEncode(quien)}</strong> cerró la recepción de la guía <strong>{codigo}</strong> en <strong>{destino}</strong> y <strong>hay diferencias por resolver</strong>:"; }
            else if (estado == "PARCIAL") { asunto = $"Guía {codigo}: llegó una parte en {guia.AlmacenDestino?.Nombre}"; intro = $"<strong>{WebUtility.HtmlEncode(quien)}</strong> registró una recepción <strong>parcial</strong> de la guía <strong>{codigo}</strong> en <strong>{destino}</strong>. Falta que llegue el resto:"; }
            else { asunto = $"Guía {codigo}: recibida completa en {guia.AlmacenDestino?.Nombre}"; intro = $"<strong>{WebUtility.HtmlEncode(quien)}</strong> confirmó que la guía <strong>{codigo}</strong> llegó <strong>completa</strong> a <strong>{destino}</strong>:"; }

            // Sin diferencias y completa: basta avisar a quien despachó y a logística; con diferencias o parcial, también a gerencia.
            var roles = conDif || estado == "PARCIAL" ? new[] { "LOGISTICA", "GERENTE_GENERAL", "GERENTE_OPERACIONES" } : new[] { "LOGISTICA" };
            var correos = await Destinatarios(ctx, proyecto, guia.CreadoPorUsuarioSistemaId, roles);
            await Enviar(correos, asunto, $"<p>{intro}</p>{tabla}" + (conDif ? "<p>Las diferencias quedan abiertas en <strong>Diferencias de recepción</strong> hasta que logística las resuelva.</p>" : ""), conDif ? "diferencias" : "guias-remision");
        }

        private async Task NotificarResolucion(AppDbContext ctx, GuiaRemision guia, GuiaDiferencia dif, DiferenciaDto dto, long usuarioId)
        {
            var quien = await NombreUsuario(ctx, usuarioId);
            var nombreRes = dif.Resolucion switch { "REPOSICION" => "Reposición", "RECLAMO" => "Reclamo", "MERMA" => "Merma (baja)", "LLEGO" => "Llegó después", _ => dif.Resolucion };
            var roles = new List<string> { "LOGISTICA", "GERENTE_GENERAL", "GERENTE_OPERACIONES" };
            if (dif.Resolucion == "MERMA") roles.Add("CONTADORA"); // una baja tiene efecto contable
            var correos = await Destinatarios(ctx, guia.AlmacenDestino?.ProyectoId, dif.CreadoPor, roles.ToArray());
            var extra = dif.Resolucion == "MERMA" ? $"<p>Valor estimado de la baja: <strong>S/ {dto.ValorEstimado:N2}</strong>.</p>" : "";
            await Enviar(correos, $"Diferencia resuelta en la guía {CodigoGuia(guia)}: {nombreRes}",
                $"<p><strong>{WebUtility.HtmlEncode(quien)}</strong> resolvió la diferencia de <strong>{dto.Cantidad:0.##} {WebUtility.HtmlEncode(dto.ProductoNombre)}</strong> ({(dto.Tipo == "FALTANTE" ? "faltante" : "dañado")}) de la guía <strong>{CodigoGuia(guia)}</strong>.</p>" +
                $"<p>Resolución: <strong>{nombreRes}</strong>{(string.IsNullOrWhiteSpace(dif.ResolucionNota) ? "" : $" — {WebUtility.HtmlEncode(dif.ResolucionNota)}")}</p>{extra}", "diferencias");
        }

        private static async Task<string> NombreUsuario(AppDbContext ctx, long usuarioId) =>
            await ctx.UsuarioSistema.AsNoTracking().Where(u => u.Id == usuarioId).Select(u => u.Persona!.Apellidos + " " + u.Persona.Nombres).FirstOrDefaultAsync() ?? "Alguien";

        /// <summary>Correos activos (con "notificar") de quienes tienen alguno de los roles en el proyecto indicado o en toda la empresa, más el de quien creó la guía.</summary>
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

        private async Task<bool> Enviar(List<string> destinatarios, string asunto, string cuerpoHtml, string ruta)
        {
            if (destinatarios.Count == 0) return false;
            var link = string.IsNullOrWhiteSpace(_front.LbAppUrl) ? "" : $"<a href='{_front.LbAppUrl.TrimEnd('/')}/{ruta}' style='background:#0F172A;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Abrir en el sistema</a>";
            try
            {
                await _email.SendAsync(to: destinatarios, subject: asunto, body: $"<h2 style='font-size:17px'>{WebUtility.HtmlEncode(asunto)}</h2>{cuerpoHtml}{link}", isHtml: true);
                return true;
            }
            catch (Exception ex)
            {
                // El correo es informativo: no debe tumbar la recepción, pero tampoco fallar en silencio.
                _log.LogWarning(ex, "[Recepción guías] No se pudo enviar el correo «{Asunto}» a {Destinatarios}", asunto, string.Join(", ", destinatarios));
                return false;
            }
        }
    }
}
