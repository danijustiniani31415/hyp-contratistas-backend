using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.PedidosModule.Application.Dtos;
using Abril_Backend.Features.PedidosModule.Application.Interfaces;
using Abril_Backend.Features.PedidosModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.PedidosModule.Application.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAlmacenKardexService _almacenService;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;

        public PedidoService(
            IDbContextFactory<AppDbContext> factory,
            IAlmacenKardexService almacenService,
            IEmailService emailService,
            IOptions<FrontendSettings> frontendSettings)
        {
            _factory = factory;
            _almacenService = almacenService;
            _emailService = emailService;
            _frontendSettings = frontendSettings.Value;
        }

        /// <summary>
        /// A quién avisar por correo de un evento de este pedido: PEDIDO_APROBAR para pedidos
        /// nuevos (gerentes/logística con ese permiso, global o del proyecto del pedido), o el
        /// propio solicitante cuando se aprueba/rechaza/entrega. Resuelve por asignación vigente,
        /// mismo criterio de scope que LbClaimsExtensions.GetProyectosPermitidos — sin depender de
        /// un rol de nombre fijo, para no romperse si el usuario reorganiza roles desde el frontend.
        /// </summary>
        private static async Task<List<string>> GetEmailsConPermiso(AppDbContext ctx, string codigoPermiso, int proyectoId)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var rolIdsConPermiso = ctx.RolPermiso.Where(rp => rp.Permiso!.Codigo == codigoPermiso).Select(rp => rp.RolId);

            return await ctx.UsuarioAsignacion
                .Where(a => (a.FechaFin == null || a.FechaFin >= hoy) && (a.ProyectoId == null || a.ProyectoId == proyectoId))
                .Where(a => rolIdsConPermiso.Contains(a.RolId) && a.Notificar)
                .Join(ctx.UsuarioSistema.Where(u => u.Estado == "ACTIVO"), a => a.UsuarioSistemaId, u => u.Id, (a, u) => u.EmailLogin)
                .Distinct()
                .ToListAsync();
        }

        public async Task<PedidoDestinatariosDto> GetDestinatarios(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            return new PedidoDestinatariosDto
            {
                Visadores = await GetEmailsConPermiso(ctx, "PEDIDO_VISAR", proyectoId),
                Aprobadores = await GetEmailsConPermiso(ctx, "PEDIDO_APROBAR", proyectoId),
                Entregadores = await GetEmailsConPermiso(ctx, "PEDIDO_ENTREGAR", proyectoId),
            };
        }

        private string? LinkPedidos() =>
            string.IsNullOrWhiteSpace(_frontendSettings.LbAppUrl) ? null : $"{_frontendSettings.LbAppUrl.TrimEnd('/')}/pedidos";

        private async Task NotificarEvento(string[] destinatarios, string asunto, string mensajeHtml)
        {
            if (destinatarios.Length == 0) return;
            var link = LinkPedidos();
            var boton = link is null ? "" :
                $"<a href='{link}' style='background:#0F172A;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Ver pedidos</a>";
            var html = $"<h2>{asunto}</h2><p>{mensajeHtml}</p>{boton}";
            try
            {
                await _emailService.SendAsync(to: destinatarios.ToList(), subject: asunto, body: html, isHtml: true);
            }
            catch
            {
                // Un correo que falla no debe tumbar la operación de negocio (el pedido ya se
                // guardó) — la notificación es informativa, no parte de la transacción.
            }
        }

        private async Task NotificarSolicitante(AppDbContext ctx, long solicitanteUsuarioSistemaId, string asunto, string mensajeHtml)
        {
            var email = await ctx.UsuarioSistema
                .Where(u => u.Id == solicitanteUsuarioSistemaId && u.Estado == "ACTIVO")
                .Select(u => u.EmailLogin)
                .FirstOrDefaultAsync();
            if (email is null) return;

            await NotificarEvento(new[] { email }, asunto, mensajeHtml);
        }

        public async Task<PedidoDetailDto> Crear(PedidoCreateDto dto, long solicitanteId)
        {
            if (dto.Items.Count == 0)
                throw new AbrilException("El pedido debe tener al menos un producto.", 400);
            if (dto.Items.Any(i => i.CantidadSolicitada <= 0))
                throw new AbrilException("Todas las cantidades deben ser mayores a cero.", 400);

            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Proyecto.AnyAsync(p => p.Id == dto.ProyectoId))
                throw new AbrilException("Proyecto no encontrado.", 404);
            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                throw new AbrilException("Almacén no encontrado.", 404);

            // Correlativo legible (PED-2026-000001) vía secuencia — no MAX(id)+1, que colisiona
            // entre transacciones concurrentes.
            var correlativo = await ctx.Database
                .SqlQuery<long>($"""SELECT nextval('lb_pedido_correlativo') AS "Value" """)
                .SingleAsync();
            var codigo = $"PED-{DateTime.UtcNow.Year}-{correlativo:D6}";

            var pedido = new Pedido
            {
                Codigo = codigo,
                ProyectoId = dto.ProyectoId,
                AlmacenId = dto.AlmacenId,
                SolicitanteUsuarioSistemaId = solicitanteId,
                Estado = "PENDIENTE",
                Observacion = dto.Observacion,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.Pedido.Add(pedido);
            await ctx.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                var producto = await ctx.Producto.FindAsync(item.ProductoId)
                    ?? throw new AbrilException($"Producto {item.ProductoId} no encontrado.", 404);
                if (producto.RequiereTalla && string.IsNullOrWhiteSpace(item.Talla))
                    throw new AbrilException($"El producto \"{producto.Nombre}\" requiere indicar la talla.", 400);
                if (producto.RequiereColor && string.IsNullOrWhiteSpace(item.Color))
                    throw new AbrilException($"El producto \"{producto.Nombre}\" requiere indicar el color.", 400);

                ctx.PedidoItem.Add(new PedidoItem
                {
                    PedidoId = pedido.Id,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    Color = item.Color,
                    Observacion = item.Observacion,
                    CantidadSolicitada = item.CantidadSolicitada,
                });
            }
            await ctx.SaveChangesAsync();

            var detalle = await BuildDetail(ctx, pedido.Id);

            // Primer nivel: el Residente visa antes que Gerencia vea el pedido.
            var visadores = await GetEmailsConPermiso(ctx, "PEDIDO_VISAR", pedido.ProyectoId);
            await NotificarEvento(
                visadores.ToArray(),
                $"Nuevo pedido {detalle.Codigo} pendiente de visado",
                $"{detalle.SolicitanteNombre} solicitó el pedido <strong>{detalle.Codigo}</strong> " +
                $"({detalle.Items.Count} producto(s)) en <strong>{detalle.ProyectoNombre}</strong>. " +
                "Ingresa a Pedidos para visarlo.");

            return detalle;
        }

        public async Task<PedidoListResponseDto> List(string? estado, int? proyectoId, bool soloPropios, HashSet<int>? proyectosPermitidos, long usuarioSistemaId, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Pedido
                .Include(p => p.Proyecto)
                .Include(p => p.Almacen)
                .Include(p => p.Solicitante!).ThenInclude(u => u!.Persona)
                .Include(p => p.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(p => p.Estado == estado);
            if (proyectoId.HasValue) query = query.Where(p => p.ProyectoId == proyectoId.Value);
            if (soloPropios) query = query.Where(p => p.SolicitanteUsuarioSistemaId == usuarioSistemaId);
            // null = acceso global (sin restricción). No-null = solo estos proyectos, aunque tenga
            // PEDIDO_VER_TODOS — ese permiso se lo dieron acotado a su(s) proyecto(s), no a todos.
            if (proyectosPermitidos != null) query = query.Where(p => proyectosPermitidos.Contains(p.ProyectoId));

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(p => p.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PedidoListItemDto
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    ProyectoNombre = p.Proyecto!.Nombre,
                    AlmacenNombre = p.Almacen!.Nombre,
                    SolicitanteNombre = p.Solicitante!.Persona!.Apellidos + " " + p.Solicitante.Persona.Nombres,
                    Estado = p.Estado,
                    CantidadItems = p.Items.Count,
                    CreadoEn = p.CreadoEn,
                })
                .ToListAsync();

            return new PedidoListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<PedidoDetailDto> GetById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        public async Task<PedidoDetailDto> Visar(long id, long visadorId, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.FindAsync(id) ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (!scope.Permite(pedido.ProyectoId))
                throw new AbrilException("No tienes permiso para visar pedidos de este proyecto.", 403);
            if (pedido.Estado != "PENDIENTE")
                throw new AbrilException($"Solo se puede visar un pedido PENDIENTE (está en {pedido.Estado}).", 400);

            pedido.Estado = "PENDIENTE_GERENTE";
            pedido.VisadoPorUsuarioSistemaId = visadorId;
            pedido.VisadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            var detalle = await BuildDetail(ctx, id);

            var aprobadores = await GetEmailsConPermiso(ctx, "PEDIDO_APROBAR", pedido.ProyectoId);
            await NotificarEvento(
                aprobadores.ToArray(),
                $"Pedido {detalle.Codigo} visado, pendiente de tu aprobación",
                $"El pedido <strong>{detalle.Codigo}</strong> ({detalle.Items.Count} producto(s)) en " +
                $"<strong>{detalle.ProyectoNombre}</strong> ya fue visado por {detalle.VisadoPorNombre} y " +
                "está listo para tu aprobación final.");

            return detalle;
        }

        public async Task<PedidoDetailDto> RechazarVisado(long id, long visadorId, RechazarPedidoDto dto, LbScopeProyectos scope)
        {
            if (string.IsNullOrWhiteSpace(dto.MotivoRechazo))
                throw new AbrilException("El motivo de rechazo es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.FindAsync(id) ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (!scope.Permite(pedido.ProyectoId))
                throw new AbrilException("No tienes permiso para rechazar pedidos de este proyecto.", 403);
            if (pedido.Estado != "PENDIENTE")
                throw new AbrilException($"Solo se puede rechazar en visado un pedido PENDIENTE (está en {pedido.Estado}).", 400);

            pedido.Estado = "RECHAZADO";
            pedido.MotivoRechazo = dto.MotivoRechazo;
            pedido.VisadoPorUsuarioSistemaId = visadorId;
            pedido.VisadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            var detalle = await BuildDetail(ctx, id);
            await NotificarSolicitante(ctx, pedido.SolicitanteUsuarioSistemaId,
                $"Tu pedido {detalle.Codigo} fue rechazado",
                $"Tu pedido <strong>{detalle.Codigo}</strong> fue rechazado por <strong>{detalle.VisadoPorNombre}</strong> " +
                $"en la revisión del Residente.<br/>Motivo: {dto.MotivoRechazo}");

            return detalle;
        }

        public async Task<PedidoDetailDto> Aprobar(long id, long aprobadorId, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.FindAsync(id) ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (!scope.Permite(pedido.ProyectoId))
                throw new AbrilException("No tienes permiso para aprobar pedidos de este proyecto.", 403);
            if (pedido.Estado != "PENDIENTE_GERENTE")
                throw new AbrilException($"Solo se puede aprobar un pedido ya visado (PENDIENTE_GERENTE) — está en {pedido.Estado}.", 400);

            pedido.Estado = "APROBADO";
            pedido.AprobadoPorUsuarioSistemaId = aprobadorId;
            pedido.AprobadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            var detalle = await BuildDetail(ctx, id);
            await NotificarSolicitante(ctx, pedido.SolicitanteUsuarioSistemaId,
                $"Tu pedido {detalle.Codigo} fue aprobado",
                $"Tu pedido <strong>{detalle.Codigo}</strong> fue aprobado por <strong>{detalle.AprobadoPorNombre}</strong> " +
                "y está listo para ser atendido y entregado.");

            // Recién acá se avisa a quien despacha (Logística/Almacenero) — no antes, porque hasta
            // este punto el pedido todavía podía ser rechazado en cualquiera de los dos niveles.
            var entregadores = await GetEmailsConPermiso(ctx, "PEDIDO_ENTREGAR", pedido.ProyectoId);
            await NotificarEvento(
                entregadores.ToArray(),
                $"Pedido {detalle.Codigo} aprobado, pendiente de despacho",
                $"El pedido <strong>{detalle.Codigo}</strong> ({detalle.Items.Count} producto(s)) de " +
                $"<strong>{detalle.SolicitanteNombre}</strong> en <strong>{detalle.ProyectoNombre}</strong> ya fue " +
                $"aprobado por {detalle.AprobadoPorNombre} — está listo para atender y despachar.");

            return detalle;
        }

        public async Task<PedidoDetailDto> Rechazar(long id, long aprobadorId, RechazarPedidoDto dto, LbScopeProyectos scope)
        {
            if (string.IsNullOrWhiteSpace(dto.MotivoRechazo))
                throw new AbrilException("El motivo de rechazo es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.FindAsync(id) ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (!scope.Permite(pedido.ProyectoId))
                throw new AbrilException("No tienes permiso para rechazar pedidos de este proyecto.", 403);
            if (pedido.Estado != "PENDIENTE_GERENTE")
                throw new AbrilException($"Solo se puede rechazar un pedido ya visado (PENDIENTE_GERENTE) — está en {pedido.Estado}.", 400);

            pedido.Estado = "RECHAZADO";
            pedido.MotivoRechazo = dto.MotivoRechazo;
            pedido.AprobadoPorUsuarioSistemaId = aprobadorId;
            pedido.AprobadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            var detalle = await BuildDetail(ctx, id);
            await NotificarSolicitante(ctx, pedido.SolicitanteUsuarioSistemaId,
                $"Tu pedido {detalle.Codigo} fue rechazado",
                $"Tu pedido <strong>{detalle.Codigo}</strong> fue rechazado por <strong>{detalle.AprobadoPorNombre}</strong>.<br/>Motivo: {dto.MotivoRechazo}");

            return detalle;
        }

        /// <summary>
        /// Descuenta stock del almacén del pedido, un lb_movimiento SALIDA por item (referenciado
        /// al pedido). Valida stock de TODOS los items antes de mover cualquiera — si uno no
        /// alcanza, no se entrega nada (todo o nada; entrega parcial queda para más adelante).
        /// </summary>
        public async Task<PedidoDetailDto> Entregar(long id, long entregadorId, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.Include(p => p.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (!scope.Permite(pedido.ProyectoId))
                throw new AbrilException("No tienes permiso para entregar pedidos de este proyecto.", 403);
            if (pedido.Estado != "APROBADO")
                throw new AbrilException($"Solo se puede entregar un pedido APROBADO (está en {pedido.Estado}).", 400);

            var faltantes = new List<string>();
            foreach (var item in pedido.Items)
            {
                var stock = await ctx.Stock.FirstOrDefaultAsync(s =>
                    s.AlmacenId == pedido.AlmacenId && s.ProductoId == item.ProductoId && s.Talla == item.Talla && s.Color == item.Color);
                var disponible = stock?.CantidadActual ?? 0;
                if (disponible < item.CantidadSolicitada)
                    faltantes.Add($"{item.Producto!.Nombre} (pide {item.CantidadSolicitada}, hay {disponible})");
            }
            if (faltantes.Count > 0)
                throw new AbrilException("Stock insuficiente para entregar: " + string.Join("; ", faltantes), 400);

            foreach (var item in pedido.Items)
            {
                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = pedido.AlmacenId,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    Color = item.Color,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.CantidadSolicitada,
                    ReferenciaTipo = "PEDIDO",
                    ReferenciaId = pedido.Id,
                }, entregadorId);

                item.CantidadEntregada = item.CantidadSolicitada;
            }

            pedido.Estado = "ENTREGADO";
            pedido.EntregadoPorUsuarioSistemaId = entregadorId;
            pedido.EntregadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            var detalle = await BuildDetail(ctx, id);
            await NotificarSolicitante(ctx, pedido.SolicitanteUsuarioSistemaId,
                $"Tu pedido {detalle.Codigo} fue entregado",
                $"Tu pedido <strong>{detalle.Codigo}</strong> ya fue entregado por <strong>{detalle.EntregadoPorNombre}</strong>.");

            return detalle;
        }

        public async Task<PedidoDetailDto> Cancelar(long id, long solicitanteId)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.FindAsync(id) ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (pedido.SolicitanteUsuarioSistemaId != solicitanteId)
                throw new AbrilException("Solo quien creó el pedido puede cancelarlo.", 403);
            if (pedido.Estado != "PENDIENTE")
                throw new AbrilException($"Solo se puede cancelar un pedido PENDIENTE (está en {pedido.Estado}).", 400);

            pedido.Estado = "CANCELADO";
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, id);
        }

        public async Task<List<PendienteCompraDto>> ListPendientesDeCompra(HashSet<int>? proyectosPermitidos)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.PedidoItem
                .Include(i => i.Pedido!).ThenInclude(p => p.Proyecto)
                .Include(i => i.Producto)
                .Where(i => i.Pedido!.Estado == "APROBADO" && i.CantidadEnCompra < i.CantidadSolicitada)
                .AsQueryable();

            if (proyectosPermitidos != null)
                query = query.Where(i => proyectosPermitidos.Contains(i.Pedido!.ProyectoId));

            return await query
                .OrderBy(i => i.Pedido!.CreadoEn)
                .Select(i => new PendienteCompraDto
                {
                    PedidoItemId = i.Id,
                    PedidoId = i.PedidoId,
                    PedidoCodigo = i.Pedido!.Codigo,
                    ProyectoNombre = i.Pedido.Proyecto!.Nombre,
                    ProductoId = i.ProductoId,
                    ProductoNombre = i.Producto!.Nombre,
                    Talla = i.Talla,
                    Color = i.Color,
                    CantidadSolicitada = i.CantidadSolicitada,
                    CantidadEnCompra = i.CantidadEnCompra,
                    CantidadPendienteDeCompra = i.CantidadSolicitada - i.CantidadEnCompra,
                    PedidoCreadoEn = i.Pedido.CreadoEn,
                })
                .ToListAsync();
        }

        public async Task<List<PendienteDespachoDto>> ListPendientesDeDespacho(HashSet<int>? proyectosPermitidos)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.PedidoItem
                .Include(i => i.Pedido!).ThenInclude(p => p.Proyecto)
                .Include(i => i.Producto)
                .Where(i => i.Pedido!.Estado == "APROBADO" && i.CantidadRecibidaAlmacen > i.CantidadDespachada)
                .AsQueryable();

            if (proyectosPermitidos != null)
                query = query.Where(i => proyectosPermitidos.Contains(i.Pedido!.ProyectoId));

            return await query
                .OrderBy(i => i.Pedido!.CreadoEn)
                .Select(i => new PendienteDespachoDto
                {
                    PedidoItemId = i.Id,
                    PedidoId = i.PedidoId,
                    PedidoCodigo = i.Pedido!.Codigo,
                    ProyectoNombre = i.Pedido.Proyecto!.Nombre,
                    AlmacenId = i.Pedido.AlmacenId,
                    ProductoId = i.ProductoId,
                    ProductoNombre = i.Producto!.Nombre,
                    Talla = i.Talla,
                    Color = i.Color,
                    UnidadMedida = i.Producto.UnidadMedida,
                    CantidadPendienteDeDespacho = i.CantidadRecibidaAlmacen - i.CantidadDespachada,
                })
                .ToListAsync();
        }

        private static async Task<PedidoDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var pedido = await ctx.Pedido
                .Include(p => p.Proyecto)
                .Include(p => p.Almacen)
                .Include(p => p.Solicitante!).ThenInclude(u => u!.Persona)
                .Include(p => p.VisadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.AprobadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.EntregadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Pedido no encontrado.", 404);

            return new PedidoDetailDto
            {
                Id = pedido.Id,
                Codigo = pedido.Codigo,
                ProyectoNombre = pedido.Proyecto!.Nombre,
                AlmacenNombre = pedido.Almacen!.Nombre,
                SolicitanteUsuarioSistemaId = pedido.SolicitanteUsuarioSistemaId,
                SolicitanteNombre = $"{pedido.Solicitante!.Persona!.Apellidos} {pedido.Solicitante.Persona.Nombres}",
                Estado = pedido.Estado,
                Observacion = pedido.Observacion,
                MotivoRechazo = pedido.MotivoRechazo,
                VisadoPorNombre = pedido.VisadoPor?.Persona != null
                    ? $"{pedido.VisadoPor.Persona.Apellidos} {pedido.VisadoPor.Persona.Nombres}" : null,
                VisadoEn = pedido.VisadoEn,
                AprobadoPorNombre = pedido.AprobadoPor?.Persona != null
                    ? $"{pedido.AprobadoPor.Persona.Apellidos} {pedido.AprobadoPor.Persona.Nombres}" : null,
                AprobadoEn = pedido.AprobadoEn,
                EntregadoPorNombre = pedido.EntregadoPor?.Persona != null
                    ? $"{pedido.EntregadoPor.Persona.Apellidos} {pedido.EntregadoPor.Persona.Nombres}" : null,
                EntregadoEn = pedido.EntregadoEn,
                CreadoEn = pedido.CreadoEn,
                Items = pedido.Items.Select(i => new PedidoItemDetailDto
                {
                    Id = i.Id,
                    ProductoNombre = i.Producto!.Nombre,
                    ProductoCodigo = i.Producto.Codigo,
                    UnidadMedida = i.Producto.UnidadMedida,
                    Talla = i.Talla,
                    Color = i.Color,
                    Observacion = i.Observacion,
                    CantidadSolicitada = i.CantidadSolicitada,
                    CantidadEntregada = i.CantidadEntregada,
                    CantidadEnCompra = i.CantidadEnCompra,
                    CantidadRecibidaAlmacen = i.CantidadRecibidaAlmacen,
                    CantidadDespachada = i.CantidadDespachada,
                    CantidadConfirmadaMina = i.CantidadConfirmadaMina,
                }).ToList(),
            };
        }
    }
}
