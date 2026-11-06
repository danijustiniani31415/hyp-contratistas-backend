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
                ContratoId = dto.ContratoId,
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
                    PartidaId = item.PartidaId,
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

        /// <summary>Resuelve cada ítem del pedido según <paramref name="decisiones"/>: acepta solo
        /// ítems que estén en <paramref name="estadoOrigen"/> (PENDIENTE para Visar, VISADO para
        /// Aprobar) — un ítem ya RECHAZADO por una etapa anterior no puede volver a decidirse, y
        /// ninguno puede quedar sin resolver. <paramref name="topeCantidad"/> da el máximo
        /// permitido de CantidadAprobada por ítem (CantidadSolicitada en Visar; lo que el
        /// Residente ya visó en Aprobar — Gerencia no puede aprobar más de lo que el Residente
        /// dejó pasar). Devuelve true si absolutamente todos los ítems terminaron RECHAZADO.
        /// </summary>
        private static bool ResolverItems(
            AppDbContext ctx, Pedido pedido, long usuarioId, string etapaHistorial,
            List<PedidoItemDecisionDto> decisiones, string estadoOrigen, string estadoDestino,
            Func<PedidoItem, decimal> topeCantidad)
        {
            var pendientes = pedido.Items.Where(i => i.Estado == estadoOrigen).ToList();
            if (pendientes.Count == 0)
                throw new AbrilException($"Este pedido no tiene ítems en estado {estadoOrigen} para decidir.", 400);

            var decisionesPorItem = decisiones.ToDictionary(d => d.PedidoItemId);
            var faltantes = pendientes.Where(i => !decisionesPorItem.ContainsKey(i.Id)).ToList();
            if (faltantes.Count > 0)
                throw new AbrilException(
                    "Falta decidir sobre todos los ítems: " + string.Join(", ", faltantes.Select(i => i.Id)), 400);

            foreach (var item in pendientes)
            {
                var decision = decisionesPorItem[item.Id];
                if (decision.Rechazado)
                {
                    if (string.IsNullOrWhiteSpace(decision.MotivoRechazo))
                        throw new AbrilException($"El ítem {item.Id} necesita un motivo de rechazo.", 400);
                    item.Estado = "RECHAZADO";
                    item.MotivoRechazo = decision.MotivoRechazo;
                }
                else
                {
                    var tope = topeCantidad(item);
                    if (decision.CantidadAprobada is not > 0 || decision.CantidadAprobada > tope)
                        throw new AbrilException(
                            $"La cantidad aprobada del ítem {item.Id} debe ser mayor a 0 y no mayor a {tope}.", 400);
                    item.CantidadAprobada = decision.CantidadAprobada;
                    item.Estado = estadoDestino;
                    item.MotivoRechazo = null;
                }

                ctx.PedidoItemHistorial.Add(new PedidoItemHistorial
                {
                    PedidoItemId = item.Id,
                    Etapa = etapaHistorial,
                    UsuarioSistemaId = usuarioId,
                    EstadoResultante = item.Estado,
                    CantidadSolicitada = item.CantidadSolicitada,
                    CantidadAprobada = item.CantidadAprobada,
                    MotivoRechazo = item.MotivoRechazo,
                    CreadoEn = DateTimeOffset.UtcNow,
                });
            }

            return pedido.Items.All(i => i.Estado == "RECHAZADO");
        }

        public async Task<PedidoDetailDto> Visar(long id, long visadorId, PedidoDecisionDto dto, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (!scope.Permite(pedido.ProyectoId))
                throw new AbrilException("No tienes permiso para visar pedidos de este proyecto.", 403);
            if (pedido.Estado != "PENDIENTE")
                throw new AbrilException($"Solo se puede visar un pedido PENDIENTE (está en {pedido.Estado}).", 400);

            var todoRechazado = ResolverItems(ctx, pedido, visadorId, "VISADO", dto.Items, "PENDIENTE", "VISADO", i => i.CantidadSolicitada);

            pedido.VisadoPorUsuarioSistemaId = visadorId;
            pedido.VisadoEn = DateTimeOffset.UtcNow;

            if (todoRechazado)
            {
                pedido.Estado = "RECHAZADO";
                pedido.MotivoRechazo = "Todos los ítems fueron rechazados en el visado del Residente.";
            }
            else
            {
                pedido.Estado = "PENDIENTE_GERENTE";
            }
            await ctx.SaveChangesAsync();

            var detalle = await BuildDetail(ctx, id);

            if (todoRechazado)
            {
                await NotificarSolicitante(ctx, pedido.SolicitanteUsuarioSistemaId,
                    $"Tu pedido {detalle.Codigo} fue rechazado",
                    $"Tu pedido <strong>{detalle.Codigo}</strong> fue rechazado por <strong>{detalle.VisadoPorNombre}</strong> " +
                    "en la revisión del Residente (todos los ítems fueron rechazados).");
            }
            else
            {
                var aprobadores = await GetEmailsConPermiso(ctx, "PEDIDO_APROBAR", pedido.ProyectoId);
                await NotificarEvento(
                    aprobadores.ToArray(),
                    $"Pedido {detalle.Codigo} visado, pendiente de tu aprobación",
                    $"El pedido <strong>{detalle.Codigo}</strong> ({detalle.Items.Count} producto(s)) en " +
                    $"<strong>{detalle.ProyectoNombre}</strong> ya fue visado por {detalle.VisadoPorNombre} y " +
                    "está listo para tu aprobación final.");
            }

            return detalle;
        }

        public async Task<PedidoDetailDto> Aprobar(long id, long aprobadorId, PedidoDecisionDto dto, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (!scope.Permite(pedido.ProyectoId))
                throw new AbrilException("No tienes permiso para aprobar pedidos de este proyecto.", 403);
            if (pedido.Estado != "PENDIENTE_GERENTE")
                throw new AbrilException($"Solo se puede aprobar un pedido ya visado (PENDIENTE_GERENTE) — está en {pedido.Estado}.", 400);

            // Tope = lo que el Residente ya visó para ese ítem (CantidadAprobada quedó en VISADO
            // con ese valor) — Gerencia puede reducir más, nunca subir por encima de eso.
            var todoRechazado = ResolverItems(ctx, pedido, aprobadorId, "APROBACION", dto.Items, "VISADO", "APROBADO", i => i.CantidadAprobada!.Value);

            pedido.AprobadoPorUsuarioSistemaId = aprobadorId;
            pedido.AprobadoEn = DateTimeOffset.UtcNow;

            if (todoRechazado)
            {
                pedido.Estado = "RECHAZADO";
                pedido.MotivoRechazo = "Todos los ítems fueron rechazados en la aprobación de Gerencia.";
            }
            else
            {
                pedido.Estado = "APROBADO";
            }
            await ctx.SaveChangesAsync();

            var detalle = await BuildDetail(ctx, id);

            if (todoRechazado)
            {
                await NotificarSolicitante(ctx, pedido.SolicitanteUsuarioSistemaId,
                    $"Tu pedido {detalle.Codigo} fue rechazado",
                    $"Tu pedido <strong>{detalle.Codigo}</strong> fue rechazado por <strong>{detalle.AprobadoPorNombre}</strong> " +
                    "(todos los ítems fueron rechazados).");
            }
            else
            {
                await NotificarSolicitante(ctx, pedido.SolicitanteUsuarioSistemaId,
                    $"Tu pedido {detalle.Codigo} fue aprobado",
                    $"Tu pedido <strong>{detalle.Codigo}</strong> fue aprobado por <strong>{detalle.AprobadoPorNombre}</strong> " +
                    "y está listo para ser atendido y entregado.");

                // Recién acá se avisa a quien despacha (Logística/Almacenero) — no antes, porque
                // hasta este punto el pedido todavía podía ser rechazado en cualquiera de los dos
                // niveles.
                var entregadores = await GetEmailsConPermiso(ctx, "PEDIDO_ENTREGAR", pedido.ProyectoId);
                await NotificarEvento(
                    entregadores.ToArray(),
                    $"Pedido {detalle.Codigo} aprobado, pendiente de despacho",
                    $"El pedido <strong>{detalle.Codigo}</strong> ({detalle.Items.Count} producto(s)) de " +
                    $"<strong>{detalle.SolicitanteNombre}</strong> en <strong>{detalle.ProyectoNombre}</strong> ya fue " +
                    $"aprobado por {detalle.AprobadoPorNombre} — está listo para atender y despachar.");
            }

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

            // Solo se despachan los ítems que sobrevivieron a las dos etapas de aprobación — los
            // RECHAZADO (por el Residente o por Gerencia) no consumen stock. La cantidad a entregar
            // es la aprobada, no la solicitada (puede haber sido reducida en Visar o Aprobar).
            var itemsAEntregar = pedido.Items.Where(i => i.Estado == "APROBADO").ToList();

            var faltantes = new List<string>();
            foreach (var item in itemsAEntregar)
            {
                var stock = await ctx.Stock.FirstOrDefaultAsync(s =>
                    s.AlmacenId == pedido.AlmacenId && s.ProductoId == item.ProductoId && s.Talla == item.Talla && s.Color == item.Color);
                var disponible = stock?.CantidadActual ?? 0;
                if (disponible < item.CantidadAprobada!.Value)
                    faltantes.Add($"{item.Producto!.Nombre} (pide {item.CantidadAprobada}, hay {disponible})");
            }
            if (faltantes.Count > 0)
                throw new AbrilException("Stock insuficiente para entregar: " + string.Join("; ", faltantes), 400);

            foreach (var item in itemsAEntregar)
            {
                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = pedido.AlmacenId,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    Color = item.Color,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.CantidadAprobada!.Value,
                    ReferenciaTipo = "PEDIDO",
                    ReferenciaId = pedido.Id,
                    PartidaId = item.PartidaId,
                }, entregadorId);

                item.CantidadEntregada = item.CantidadAprobada;
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
                .Where(i => i.Pedido!.Estado == "APROBADO" && i.Estado == "APROBADO" && i.CantidadEnCompra < i.CantidadAprobada!.Value)
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
                    CantidadSolicitada = i.CantidadAprobada!.Value,
                    CantidadEnCompra = i.CantidadEnCompra,
                    CantidadPendienteDeCompra = i.CantidadAprobada.Value - i.CantidadEnCompra,
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

        public async Task<List<PedidoItemHistorialDto>> GetHistorial(long pedidoId)
        {
            using var ctx = _factory.CreateDbContext();
            if (!await ctx.Pedido.AnyAsync(p => p.Id == pedidoId))
                throw new AbrilException("Pedido no encontrado.", 404);

            return await ctx.PedidoItemHistorial
                .Include(h => h.PedidoItem!).ThenInclude(i => i!.Producto)
                .Include(h => h.UsuarioSistema!).ThenInclude(u => u!.Persona)
                .Where(h => h.PedidoItem!.PedidoId == pedidoId)
                .OrderBy(h => h.CreadoEn)
                .Select(h => new PedidoItemHistorialDto
                {
                    Id = h.Id,
                    PedidoItemId = h.PedidoItemId,
                    ProductoNombre = h.PedidoItem!.Producto!.Nombre,
                    Etapa = h.Etapa,
                    UsuarioNombre = h.UsuarioSistema!.Persona!.Apellidos + " " + h.UsuarioSistema.Persona.Nombres,
                    EstadoResultante = h.EstadoResultante,
                    CantidadSolicitada = h.CantidadSolicitada,
                    CantidadAprobada = h.CantidadAprobada,
                    MotivoRechazo = h.MotivoRechazo,
                    CreadoEn = h.CreadoEn,
                })
                .ToListAsync();
        }

        public async Task<List<ContratoDto>> ListContratos(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Contrato
                .Where(c => c.ProyectoId == proyectoId && c.Activo)
                .OrderByDescending(c => c.VigenteDesde)
                .Select(c => new ContratoDto
                {
                    Id = c.Id,
                    ProyectoId = c.ProyectoId,
                    Codigo = c.Codigo,
                    Descripcion = c.Descripcion,
                    VigenteDesde = c.VigenteDesde,
                    VigenteHasta = c.VigenteHasta,
                    Activo = c.Activo,
                })
                .ToListAsync();
        }

        public async Task<ContratoDto> CrearContrato(ContratoCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo))
                throw new AbrilException("El código del contrato es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            if (!await ctx.Proyecto.AnyAsync(p => p.Id == dto.ProyectoId))
                throw new AbrilException("Proyecto no encontrado.", 404);
            if (await ctx.Contrato.AnyAsync(c => c.Codigo == dto.Codigo))
                throw new AbrilException($"Ya existe un contrato con código \"{dto.Codigo}\".", 400);

            var contrato = new Contrato
            {
                ProyectoId = dto.ProyectoId,
                Codigo = dto.Codigo,
                Descripcion = dto.Descripcion,
                VigenteDesde = dto.VigenteDesde,
                VigenteHasta = dto.VigenteHasta,
                Activo = true,
            };
            ctx.Contrato.Add(contrato);
            await ctx.SaveChangesAsync();

            return new ContratoDto
            {
                Id = contrato.Id,
                ProyectoId = contrato.ProyectoId,
                Codigo = contrato.Codigo,
                Descripcion = contrato.Descripcion,
                VigenteDesde = contrato.VigenteDesde,
                VigenteHasta = contrato.VigenteHasta,
                Activo = contrato.Activo,
            };
        }

        public async Task<List<PartidaDto>> ListPartidas(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Partida
                .Include(p => p.Moneda)
                .Where(p => p.ProyectoId == proyectoId && p.Activo)
                .OrderBy(p => p.Codigo)
                .Select(p => new PartidaDto
                {
                    Id = p.Id,
                    ProyectoId = p.ProyectoId,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    Activo = p.Activo,
                    MonedaId = p.MonedaId,
                    MonedaCodigo = p.Moneda != null ? p.Moneda.Codigo : null,
                    MontoPresupuestado = p.MontoPresupuestado,
                })
                .ToListAsync();
        }

        public async Task<PartidaDto> CrearPartida(PartidaCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo) || string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("Código y nombre de la partida son obligatorios.", 400);
            if (dto.MontoPresupuestado is < 0)
                throw new AbrilException("El monto presupuestado no puede ser negativo.", 400);

            using var ctx = _factory.CreateDbContext();
            if (!await ctx.Proyecto.AnyAsync(p => p.Id == dto.ProyectoId))
                throw new AbrilException("Proyecto no encontrado.", 404);
            if (await ctx.Partida.AnyAsync(p => p.ProyectoId == dto.ProyectoId && p.Codigo == dto.Codigo))
                throw new AbrilException($"Ya existe una partida con código \"{dto.Codigo}\" en este proyecto.", 400);
            if (dto.MonedaId.HasValue && !await ctx.Moneda.AnyAsync(m => m.Id == dto.MonedaId))
                throw new AbrilException("Moneda no encontrada.", 404);

            var partida = new Partida
            {
                ProyectoId = dto.ProyectoId,
                Codigo = dto.Codigo,
                Nombre = dto.Nombre,
                Activo = true,
                MonedaId = dto.MonedaId,
                MontoPresupuestado = dto.MontoPresupuestado,
            };
            ctx.Partida.Add(partida);
            await ctx.SaveChangesAsync();

            var moneda = dto.MonedaId.HasValue ? await ctx.Moneda.FindAsync(dto.MonedaId.Value) : null;
            return new PartidaDto
            {
                Id = partida.Id,
                ProyectoId = partida.ProyectoId,
                Codigo = partida.Codigo,
                Nombre = partida.Nombre,
                Activo = partida.Activo,
                MonedaId = partida.MonedaId,
                MonedaCodigo = moneda?.Codigo,
                MontoPresupuestado = partida.MontoPresupuestado,
            };
        }

        public async Task<List<MonedaDto>> ListMonedas()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Moneda
                .OrderBy(m => m.Codigo)
                .Select(m => new MonedaDto { Id = m.Id, Codigo = m.Codigo, Nombre = m.Nombre })
                .ToListAsync();
        }

        public async Task<ConsumoPartidaDto> GetConsumoPartida(int partidaId)
        {
            using var ctx = _factory.CreateDbContext();
            var partida = await ctx.Partida.Include(p => p.Moneda).FirstOrDefaultAsync(p => p.Id == partidaId)
                ?? throw new AbrilException("Partida no encontrada.", 404);

            // Consumido = valorizado de todas las SALIDAs de Kardex imputadas a esta partida — no
            // solo las que vinieron de un Pedido, para que una salida futura de otro módulo (EPP,
            // Herramientas, ajuste manual) que se impute acá también cuente.
            var consumido = await ctx.Movimiento
                .Where(m => m.PartidaId == partidaId && m.TipoMovimiento == "SALIDA")
                .SumAsync(m => (decimal?)(m.Cantidad * (m.CostoUnitario ?? 0))) ?? 0;

            return new ConsumoPartidaDto
            {
                PartidaId = partida.Id,
                PartidaCodigo = partida.Codigo,
                PartidaNombre = partida.Nombre,
                MonedaCodigo = partida.Moneda?.Codigo,
                MontoPresupuestado = partida.MontoPresupuestado,
                Consumido = consumido,
                Saldo = partida.MontoPresupuestado.HasValue ? partida.MontoPresupuestado - consumido : null,
                PorcentajeConsumido = partida.MontoPresupuestado is > 0
                    ? Math.Round(consumido / partida.MontoPresupuestado.Value * 100, 1)
                    : null,
            };
        }

        private static async Task<PedidoDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var pedido = await ctx.Pedido
                .Include(p => p.Proyecto)
                .Include(p => p.Almacen)
                .Include(p => p.Contrato)
                .Include(p => p.Solicitante!).ThenInclude(u => u!.Persona)
                .Include(p => p.VisadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.AprobadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.EntregadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.Items).ThenInclude(i => i.Producto)
                .Include(p => p.Items).ThenInclude(i => i.Partida)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Pedido no encontrado.", 404);

            return new PedidoDetailDto
            {
                Id = pedido.Id,
                Codigo = pedido.Codigo,
                ProyectoNombre = pedido.Proyecto!.Nombre,
                AlmacenNombre = pedido.Almacen!.Nombre,
                ContratoId = pedido.ContratoId,
                ContratoCodigo = pedido.Contrato?.Codigo,
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
                    CantidadAprobada = i.CantidadAprobada,
                    Estado = i.Estado,
                    MotivoRechazo = i.MotivoRechazo,
                    PartidaId = i.PartidaId,
                    PartidaNombre = i.Partida?.Nombre,
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
