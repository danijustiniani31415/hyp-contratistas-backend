using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Infrastructure.Models;
using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;
using Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces;
using Abril_Backend.Features.PedidosModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.ComprasModule.Application.Services
{
    public class ComprasService : IComprasService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAlmacenKardexService _almacenService;
        private readonly IGuiaRemisionService _guiaRemisionService;
        private readonly CompraControlService _control;

        public ComprasService(
            IDbContextFactory<AppDbContext> factory,
            IAlmacenKardexService almacenService,
            IGuiaRemisionService guiaRemisionService,
            CompraControlService control)
        {
            _control = control;
            _factory = factory;
            _almacenService = almacenService;
            _guiaRemisionService = guiaRemisionService;
        }

        public async Task<List<ProveedorDto>> ListProveedores()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Proveedor.Where(p => p.Activo)
                .OrderBy(p => p.RazonSocial)
                .Select(p => new ProveedorDto
                {
                    Id = p.Id,
                    RazonSocial = p.RazonSocial,
                    Ruc = p.Ruc,
                    Contacto = p.Contacto,
                    Telefono = p.Telefono,
                    Email = p.Email,
                })
                .ToListAsync();
        }

        public async Task<ProveedorDto> CrearProveedor(ProveedorCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            if (!string.IsNullOrWhiteSpace(dto.Ruc) && await ctx.Proveedor.AnyAsync(p => p.Ruc == dto.Ruc))
                throw new AbrilException($"Ya existe un proveedor con RUC {dto.Ruc}.", 409);

            var proveedor = new Proveedor
            {
                RazonSocial = dto.RazonSocial,
                Ruc = dto.Ruc,
                Contacto = dto.Contacto,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Activo = true,
            };
            ctx.Proveedor.Add(proveedor);
            await ctx.SaveChangesAsync();

            return new ProveedorDto
            {
                Id = proveedor.Id,
                RazonSocial = proveedor.RazonSocial,
                Ruc = proveedor.Ruc,
                Contacto = proveedor.Contacto,
                Telefono = proveedor.Telefono,
                Email = proveedor.Email,
            };
        }

        public async Task<OrdenCompraDetailDto> Crear(OrdenCompraCreateDto dto, long solicitadoPorId)
        {
            if (dto.Items.Count == 0)
                throw new AbrilException("La orden debe tener al menos un producto.", 400);
            if (dto.Items.Any(i => i.CantidadSolicitada <= 0 || i.CostoUnitario < 0))
                throw new AbrilException("Cantidades y costos deben ser válidos.", 400);

            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Proveedor.AnyAsync(p => p.Id == dto.ProveedorId))
                throw new AbrilException("Proveedor no encontrado.", 404);
            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                throw new AbrilException("Almacén no encontrado.", 404);

            var correlativo = await ctx.Database
                .SqlQuery<long>($"""SELECT nextval('lb_orden_compra_correlativo') AS "Value" """)
                .SingleAsync();
            var codigo = $"OC-{DateTime.UtcNow.Year}-{correlativo:D6}";

            var orden = new OrdenCompra
            {
                Codigo = codigo,
                ProveedorId = dto.ProveedorId,
                AlmacenId = dto.AlmacenId,
                SolicitadoPorUsuarioSistemaId = solicitadoPorId,
                Estado = "PENDIENTE",
                Observacion = dto.Observacion,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.OrdenCompra.Add(orden);
            await ctx.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                if (!await ctx.Producto.AnyAsync(p => p.Id == item.ProductoId))
                    throw new AbrilException($"Producto {item.ProductoId} no encontrado.", 404);

                ctx.OrdenCompraItem.Add(new OrdenCompraItem
                {
                    OrdenCompraId = orden.Id,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    Color = item.Color,
                    CantidadSolicitada = item.CantidadSolicitada,
                    CostoUnitario = item.CostoUnitario,
                    CantidadRecibida = 0,
                    PedidoItemId = item.PedidoItemId,
                });
            }
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, orden.Id);
        }

        /// <summary>
        /// Arma una OC a partir de ítems pendientes de uno o varios pedidos APROBADOs — la
        /// cantidad de cada fila es siempre la pendiente de compra de ese ítem al momento de
        /// generar (CantidadSolicitada - CantidadEnCompra), nunca la que mande el caller, para que
        /// no se pueda comprar de más/menos por error. Cada pedido_item solo puede tener una
        /// compra en curso a la vez — si ya está 100% cubierto, se salta en vez de reventar, para
        /// que Logística pueda seleccionar varios ítems sin tener que revisar uno por uno antes.
        /// </summary>
        public async Task<OrdenCompraDetailDto> GenerarDesdePedidos(GenerarOrdenCompraDesdePedidosDto dto, long solicitadoPorId)
        {
            if (dto.Items.Count == 0)
                throw new AbrilException("Selecciona al menos un ítem para comprar.", 400);

            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Proveedor.AnyAsync(p => p.Id == dto.ProveedorId))
                throw new AbrilException("Proveedor no encontrado.", 404);
            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                throw new AbrilException("Almacén no encontrado.", 404);

            var pedidoItemIds = dto.Items.Where(i => i.PedidoItemId.HasValue).Select(i => i.PedidoItemId!.Value).ToList();
            var pedidoItems = await ctx.PedidoItem
                .Include(pi => pi.Pedido)
                .Where(pi => pedidoItemIds.Contains(pi.Id))
                .ToListAsync();

            // Se valida todo (incluido que quede al menos un ítem con pendiente real) ANTES de
            // crear la cabecera — evita dejar una OC vacía en la base si todo lo seleccionado ya
            // estaba cubierto por otra compra.
            var itemsDePedido = new List<(PedidoItem PedidoItem, decimal Pendiente, decimal CostoUnitario)>();
            var itemsDeReposicion = new List<(long ProductoId, string Talla, string Color, decimal Cantidad, decimal CostoUnitario)>();

            foreach (var itemDto in dto.Items)
            {
                if (itemDto.PedidoItemId.HasValue)
                {
                    var pedidoItem = pedidoItems.FirstOrDefault(pi => pi.Id == itemDto.PedidoItemId.Value)
                        ?? throw new AbrilException($"Ítem de pedido {itemDto.PedidoItemId} no encontrado.", 404);

                    if (pedidoItem.Pedido!.Estado != "APROBADO")
                        throw new AbrilException($"El pedido {pedidoItem.Pedido.Codigo} no está APROBADO — no se puede comprar para él.", 400);

                    var pendiente = pedidoItem.CantidadSolicitada - pedidoItem.CantidadEnCompra;
                    if (pendiente <= 0) continue; // ya cubierto por otra OC — se salta, no revienta
                    itemsDePedido.Add((pedidoItem, pendiente, itemDto.CostoUnitario));
                }
                else
                {
                    // Reposición sin pedido de por medio (sugerida por stock bajo mínimo, o
                    // compra libre elegida a mano desde el mismo panel).
                    if (!itemDto.ProductoId.HasValue || !itemDto.Cantidad.HasValue || itemDto.Cantidad.Value <= 0)
                        throw new AbrilException("Un ítem de reposición sin pedido necesita producto y cantidad.", 400);
                    if (!await ctx.Producto.AnyAsync(p => p.Id == itemDto.ProductoId.Value))
                        throw new AbrilException($"Producto {itemDto.ProductoId} no encontrado.", 404);
                    itemsDeReposicion.Add((itemDto.ProductoId.Value, itemDto.Talla, itemDto.Color, itemDto.Cantidad.Value, itemDto.CostoUnitario));
                }
            }

            if (itemsDePedido.Count == 0 && itemsDeReposicion.Count == 0)
                throw new AbrilException("Todos los ítems de pedido seleccionados ya están cubiertos por otra orden de compra.", 400);

            var correlativo = await ctx.Database
                .SqlQuery<long>($"""SELECT nextval('lb_orden_compra_correlativo') AS "Value" """)
                .SingleAsync();
            var codigo = $"OC-{DateTime.UtcNow.Year}-{correlativo:D6}";

            var orden = new OrdenCompra
            {
                Codigo = codigo,
                ProveedorId = dto.ProveedorId,
                AlmacenId = dto.AlmacenId,
                SolicitadoPorUsuarioSistemaId = solicitadoPorId,
                Estado = "PENDIENTE",
                Observacion = dto.Observacion,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.OrdenCompra.Add(orden);
            await ctx.SaveChangesAsync();

            foreach (var (pedidoItem, pendiente, costoUnitario) in itemsDePedido)
            {
                ctx.OrdenCompraItem.Add(new OrdenCompraItem
                {
                    OrdenCompraId = orden.Id,
                    ProductoId = pedidoItem.ProductoId,
                    Talla = pedidoItem.Talla,
                    Color = pedidoItem.Color,
                    CantidadSolicitada = pendiente,
                    CostoUnitario = costoUnitario,
                    CantidadRecibida = 0,
                    PedidoItemId = pedidoItem.Id,
                });
                pedidoItem.CantidadEnCompra += pendiente;
            }

            foreach (var (productoId, talla, color, cantidad, costoUnitario) in itemsDeReposicion)
            {
                ctx.OrdenCompraItem.Add(new OrdenCompraItem
                {
                    OrdenCompraId = orden.Id,
                    ProductoId = productoId,
                    Talla = talla,
                    Color = color,
                    CantidadSolicitada = cantidad,
                    CostoUnitario = costoUnitario,
                    CantidadRecibida = 0,
                    PedidoItemId = null,
                });
            }

            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, orden.Id);
        }

        public async Task<OrdenCompraListResponseDto> List(string? search, string? estado, HashSet<int>? proyectosPermitidos, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.OrdenCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Almacen)
                .Include(o => o.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.Codigo.ToLower().Contains(s) || o.Proveedor!.RazonSocial.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(o => o.Estado == estado);
            // La orden no tiene proyecto propio — se filtra por el proyecto de SU almacén.
            if (proyectosPermitidos != null)
                query = query.Where(o => o.Almacen!.ProyectoId != null && proyectosPermitidos.Contains(o.Almacen.ProyectoId.Value));

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(o => o.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrdenCompraListItemDto
                {
                    Id = o.Id,
                    Codigo = o.Codigo,
                    ProveedorNombre = o.Proveedor!.RazonSocial,
                    AlmacenNombre = o.Almacen!.Nombre,
                    Estado = o.Estado,
                    CantidadItems = o.Items.Count,
                    CreadoEn = o.CreadoEn,
                })
                .ToListAsync();

            return new OrdenCompraListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<OrdenCompraDetailDto> GetById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        /// <summary>Estado de la OC según lo recibido y lo cerrado como faltante.</summary>
        internal static string CalcularEstado(OrdenCompra orden)
        {
            var completos = orden.Items.All(i => i.CantidadRecibida + i.CantidadFaltante >= i.CantidadSolicitada);
            if (completos) return orden.Items.Any(i => i.CantidadFaltante > 0) ? "CERRADA" : "RECIBIDA";
            return orden.Items.Any(i => i.CantidadRecibida > 0) ? "RECIBIDA_PARCIAL" : "PENDIENTE";
        }

        public async Task<OrdenCompraDetailDto> RecibirItem(long ordenId, long itemId, RecibirItemDto dto, long recibidoPorId, LbScopeProyectos scope)
        {
            if (dto.Cantidad < 0 || dto.CantidadDanada < 0)
                throw new AbrilException("Las cantidades no pueden ser negativas.", 400);
            if (dto.Cantidad + dto.CantidadDanada <= 0)
                throw new AbrilException("Indica cuánto llegó bien o cuánto llegó dañado.", 400);
            if (dto.CantidadDanada > 0 && !CompraControlService.MotivosDanio.Contains(dto.Motivo ?? ""))
                throw new AbrilException("Indica el motivo del daño.", 400);

            using var ctx = _factory.CreateDbContext();
            var orden = await ctx.OrdenCompra.Include(o => o.Items).Include(o => o.Almacen)
                .FirstOrDefaultAsync(o => o.Id == ordenId)
                ?? throw new AbrilException("Orden de compra no encontrada.", 404);

            // La orden no tiene proyecto propio — se valida contra el proyecto de su almacén.
            if (orden.Almacen!.ProyectoId.HasValue && !scope.Permite(orden.Almacen.ProyectoId.Value))
                throw new AbrilException("No tienes permiso para registrar recepciones de este proyecto.", 403);

            if (orden.Estado == "CANCELADA")
                throw new AbrilException("Esta orden está cancelada.", 400);

            var item = orden.Items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new AbrilException("Ítem de la orden no encontrado.", 404);

            var pendiente = Math.Max(0m, item.CantidadSolicitada - item.CantidadRecibida - item.CantidadFaltante);
            var exceso = Math.Max(0m, dto.Cantidad - pendiente);
            if (exceso > 0 && !dto.AceptarSobrante)
                throw new AbrilException($"Solo quedan {pendiente} pendientes de este ítem — llegaron {dto.Cantidad}. Si llegó de más, confirma el sobrante.", 400);

            if (dto.Cantidad > 0)
                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = orden.AlmacenId,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    Color = item.Color,
                    TipoMovimiento = "INGRESO",
                    Cantidad = dto.Cantidad,
                    CostoUnitario = item.CostoUnitario,
                    ReferenciaTipo = "COMPRA",
                    ReferenciaId = orden.Id,
                }, recibidoPorId);

            var recepcion = new OrdenCompraRecepcion
            {
                OrdenCompraItemId = item.Id,
                Cantidad = dto.Cantidad,
                CantidadDanada = dto.CantidadDanada,
                Motivo = dto.CantidadDanada > 0 ? dto.Motivo : null,
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(),
                FacturaNumero = dto.FacturaNumero,
                FacturaMonto = dto.FacturaMonto,
                RecibidoPorUsuarioSistemaId = recibidoPorId,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.OrdenCompraRecepcion.Add(recepcion);

            item.CantidadRecibida += dto.Cantidad;

            CompraDiferencia Nueva(string tipo, decimal cant, string? motivo) => new()
            {
                OrdenCompraId = orden.Id, OrdenCompraItemId = item.Id, Tipo = tipo, Cantidad = cant,
                Motivo = motivo, Observacion = recepcion.Observacion, CreadoPor = recibidoPorId, CreadoEn = DateTimeOffset.UtcNow,
            };
            var difs = new List<(CompraDiferencia Dif, bool EsRecepcion)>();
            if (dto.CantidadDanada > 0) difs.Add((Nueva("DANADO", dto.CantidadDanada, dto.Motivo), true));
            if (exceso > 0) difs.Add((Nueva("SOBRANTE", exceso, null), true));

            // Si esta OC nació de un pedido, la recepción también avanza la trazabilidad del
            // pedido — recién con esto Logística puede ver que ya está en el almacén de Lima,
            // listo para despachar con guía.
            if (item.PedidoItemId.HasValue && dto.Cantidad > 0)
            {
                var pedidoItem = await ctx.PedidoItem.FindAsync(item.PedidoItemId.Value);
                if (pedidoItem != null) pedidoItem.CantidadRecibidaAlmacen += dto.Cantidad;
            }

            orden.Estado = CalcularEstado(orden);
            await ctx.SaveChangesAsync();

            // La diferencia necesita el id de la recepción, que existe recién después de guardar.
            if (difs.Count > 0)
            {
                foreach (var (d, _) in difs) d.RecepcionId = recepcion.Id;
                ctx.CompraDiferencia.AddRange(difs.Select(x => x.Dif));
                await ctx.SaveChangesAsync();
            }

            await _control.NotificarRecepcion(ctx, orden, item, dto.Cantidad, dto.CantidadDanada, exceso, dto.Motivo, recibidoPorId);
            return await BuildDetail(ctx, ordenId);
        }

        /// <summary>El proveedor no enviará el saldo: lo pendiente pasa a faltante (con diferencia por resolver) y la orden se cierra.</summary>
        public async Task<OrdenCompraDetailDto> CerrarSaldo(long ordenId, CerrarSaldoDto dto, long usuarioId, LbScopeProyectos scope)
        {
            if (!CompraControlService.MotivosFaltante.Contains(dto.Motivo ?? ""))
                throw new AbrilException("Indica por qué no llegará el saldo.", 400);

            using var ctx = _factory.CreateDbContext();
            var orden = await ctx.OrdenCompra.Include(o => o.Items).Include(o => o.Almacen)
                .FirstOrDefaultAsync(o => o.Id == ordenId)
                ?? throw new AbrilException("Orden de compra no encontrada.", 404);
            if (orden.Almacen!.ProyectoId.HasValue && !scope.Permite(orden.Almacen.ProyectoId.Value))
                throw new AbrilException("No tienes permiso para cerrar órdenes de este proyecto.", 403);
            if (orden.Estado == "CANCELADA" || orden.Estado == "CERRADA" || orden.Estado == "RECIBIDA")
                throw new AbrilException($"Esta orden ya está {orden.Estado.ToLower()}: no hay saldo por cerrar.", 400);

            var nuevas = new List<CompraDiferencia>();
            foreach (var it in orden.Items)
            {
                var pend = it.CantidadSolicitada - it.CantidadRecibida - it.CantidadFaltante;
                if (pend <= 0) continue;
                it.CantidadFaltante += pend;
                nuevas.Add(new CompraDiferencia
                {
                    OrdenCompraId = orden.Id, OrdenCompraItemId = it.Id, Tipo = "FALTANTE", Cantidad = pend, Motivo = dto.Motivo,
                    Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(), CreadoPor = usuarioId, CreadoEn = DateTimeOffset.UtcNow,
                });
            }
            if (nuevas.Count == 0) throw new AbrilException("No hay saldo pendiente por cerrar.", 400);
            ctx.CompraDiferencia.AddRange(nuevas);
            orden.Estado = CalcularEstado(orden);
            await ctx.SaveChangesAsync();

            await _control.NotificarCierre(ctx, orden, nuevas, dto.Motivo!, usuarioId);
            return await BuildDetail(ctx, ordenId);
        }

        public async Task<DevolverItemResultDto> DevolverItem(long ordenId, long itemId, DevolverItemDto dto, long usuarioId, LbScopeProyectos scope)
        {
            if (dto.Cantidad <= 0)
                throw new AbrilException("La cantidad a devolver debe ser mayor a cero.", 400);

            using var ctx = _factory.CreateDbContext();
            var orden = await ctx.OrdenCompra
                .Include(o => o.Items)
                .Include(o => o.Almacen)
                .Include(o => o.Proveedor)
                .FirstOrDefaultAsync(o => o.Id == ordenId)
                ?? throw new AbrilException("Orden de compra no encontrada.", 404);

            if (orden.Almacen!.ProyectoId.HasValue && !scope.Permite(orden.Almacen.ProyectoId.Value))
                throw new AbrilException("No tienes permiso para devolver mercadería de este proyecto.", 403);

            var item = orden.Items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new AbrilException("Ítem de la orden no encontrado.", 404);

            if (dto.Cantidad > item.CantidadRecibida)
                throw new AbrilException($"No puedes devolver {dto.Cantidad} — solo hay {item.CantidadRecibida} recibido de este ítem.", 400);

            // El material sale físicamente del almacén de vuelta al proveedor — se genera una
            // Guía de Remisión real (no un simple ajuste interno), motivo SUNAT "02" (compra:
            // devolución a proveedor). GuiaRemisionService ya valida los datos de transporte según
            // la modalidad y registra la SALIDA de kardex — sin destino, no hay INGRESO de vuelta.
            var guia = await _guiaRemisionService.Crear(new GuiaRemisionCreateDto
            {
                MotivoTraslado = "02",
                ModalidadTraslado = dto.ModalidadTraslado,
                FechaTraslado = dto.FechaTraslado,
                PesoBrutoTotal = dto.PesoBrutoTotal,
                PesoBrutoUnidad = dto.PesoBrutoUnidad,
                NumBultos = dto.NumBultos,
                AlmacenOrigenId = orden.AlmacenId,
                AlmacenDestinoId = null,
                DestinatarioRuc = orden.Proveedor!.Ruc,
                DestinatarioRazonSocial = orden.Proveedor.RazonSocial,
                TransportistaRuc = dto.TransportistaRuc,
                TransportistaRazonSocial = dto.TransportistaRazonSocial,
                VehiculoPlaca = dto.VehiculoPlaca,
                ConductorNombres = dto.ConductorNombres,
                ConductorLicencia = dto.ConductorLicencia,
                ConductorDni = dto.ConductorDni,
                Observacion = dto.Observacion ?? $"Devolución de mercadería — orden de compra {orden.Codigo}",
                ReferenciaTipo = "ORDEN_COMPRA",
                ReferenciaId = orden.Id,
                Items = new List<GuiaRemisionItemCreateDto>
                {
                    new() { ProductoId = item.ProductoId, Talla = item.Talla, Color = item.Color, Cantidad = dto.Cantidad, UnidadMedida = "NIU" },
                },
            }, usuarioId);

            // Ya no cuenta como recibido — si el proveedor manda un reemplazo correcto, vuelve a
            // quedar pendiente para una recepción nueva, en vez de quedar "recibido" en el aire.
            item.CantidadRecibida -= dto.Cantidad;
            if (item.PedidoItemId.HasValue)
            {
                var pedidoItem = await ctx.PedidoItem.FindAsync(item.PedidoItemId.Value);
                if (pedidoItem != null) pedidoItem.CantidadRecibidaAlmacen -= dto.Cantidad;
            }

            orden.Estado = CalcularEstado(orden);

            await ctx.SaveChangesAsync();

            return new DevolverItemResultDto
            {
                Orden = await BuildDetail(ctx, ordenId),
                GuiaCodigo = guia.Codigo,
            };
        }

        public async Task<OrdenCompraDetailDto> Cancelar(long id, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var orden = await ctx.OrdenCompra.Include(o => o.Items).Include(o => o.Almacen)
                .FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new AbrilException("Orden de compra no encontrada.", 404);

            if (orden.Almacen!.ProyectoId.HasValue && !scope.Permite(orden.Almacen.ProyectoId.Value))
                throw new AbrilException("No tienes permiso para cancelar órdenes de compra de este proyecto.", 403);

            if (orden.Items.Any(i => i.CantidadRecibida > 0))
                throw new AbrilException("No se puede cancelar una orden que ya tiene mercadería recibida.", 400);
            if (orden.Estado == "CANCELADA")
                throw new AbrilException("Esta orden ya está cancelada.", 400);

            orden.Estado = "CANCELADA";
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, id);
        }

        private static async Task<OrdenCompraDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var orden = await ctx.OrdenCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Almacen)
                .Include(o => o.SolicitadoPor!).ThenInclude(u => u!.Persona)
                .Include(o => o.Items).ThenInclude(i => i.Producto)
                .Include(o => o.Items).ThenInclude(i => i.PedidoItem!).ThenInclude(pi => pi.Pedido)
                .Include(o => o.Items).ThenInclude(i => i.Recepciones).ThenInclude(r => r.RecibidoPor!).ThenInclude(u => u!.Persona)
                .FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new AbrilException("Orden de compra no encontrada.", 404);

            return new OrdenCompraDetailDto
            {
                Id = orden.Id,
                Codigo = orden.Codigo,
                ProveedorNombre = orden.Proveedor!.RazonSocial,
                AlmacenNombre = orden.Almacen!.Nombre,
                SolicitadoPorNombre = $"{orden.SolicitadoPor!.Persona!.Apellidos} {orden.SolicitadoPor.Persona.Nombres}",
                Estado = orden.Estado,
                Observacion = orden.Observacion,
                CreadoEn = orden.CreadoEn,
                DiferenciasAbiertas = await ctx.CompraDiferencia.CountAsync(d => d.OrdenCompraId == orden.Id && d.Estado == "ABIERTA"),
                Items = orden.Items.Select(i => new OrdenCompraItemDetailDto
                {
                    Id = i.Id,
                    ProductoNombre = i.Producto!.Nombre,
                    ProductoCodigo = i.Producto.Codigo,
                    Talla = i.Talla,
                    Color = i.Color,
                    CantidadSolicitada = i.CantidadSolicitada,
                    CostoUnitario = i.CostoUnitario,
                    CantidadRecibida = i.CantidadRecibida,
                    CantidadPendiente = Math.Max(0m, i.CantidadSolicitada - i.CantidadRecibida - i.CantidadFaltante),
                    CantidadFaltante = i.CantidadFaltante,
                    CantidadDanada = i.Recepciones.Sum(r => r.CantidadDanada),
                    PedidoItemId = i.PedidoItemId,
                    PedidoCodigo = i.PedidoItem?.Pedido?.Codigo,
                    Recepciones = i.Recepciones.Select(r => new OrdenCompraRecepcionDetailDto
                    {
                        Id = r.Id,
                        Cantidad = r.Cantidad,
                        CantidadDanada = r.CantidadDanada,
                        Motivo = r.Motivo,
                        FacturaNumero = r.FacturaNumero,
                        FacturaMonto = r.FacturaMonto,
                        RecibidoPorNombre = $"{r.RecibidoPor!.Persona!.Apellidos} {r.RecibidoPor.Persona.Nombres}",
                        CreadoEn = r.CreadoEn,
                    }).OrderByDescending(r => r.CreadoEn).ToList(),
                }).ToList(),
            };
        }
    }
}
