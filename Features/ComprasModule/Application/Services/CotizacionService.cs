using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.ComprasModule.Application.Services
{
    /// <summary>
    /// Cotizaciones comparativas. Flujo: (1) se piden cotizaciones sobre ítems pendientes de compra, (2) se carga lo que
    /// responde cada proveedor, (3) se comparan lado a lado y (4) al adjudicar se generan las órdenes de compra, una por
    /// proveedor ganador, con los precios elegidos. Elegir algo que no es lo más barato (o adjudicar con una sola
    /// cotización) exige escribir el motivo, que queda registrado.
    /// </summary>
    public class CotizacionService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IComprasService _compras;

        public CotizacionService(IDbContextFactory<AppDbContext> factory, IComprasService compras)
        {
            _factory = factory;
            _compras = compras;
        }

        public async Task<List<CotizacionListItemDto>> Listar(string? estado)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.CotizacionSolicitud.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(s => s.Estado == estado.ToUpper());
            var lista = await q.OrderByDescending(s => s.CreadoEn).Take(300).ToListAsync();
            var ids = lista.Select(s => s.Id).ToList();
            var nItems = await ctx.CotizacionItem.AsNoTracking().Where(i => ids.Contains(i.SolicitudId)).GroupBy(i => i.SolicitudId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
            var nCot = await ctx.Cotizacion.AsNoTracking().Where(c => ids.Contains(c.SolicitudId)).GroupBy(c => c.SolicitudId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
            var almacenes = await ctx.Almacen.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.Nombre);
            return lista.Select(s => new CotizacionListItemDto
            {
                Id = s.Id, Codigo = s.Codigo, AlmacenNombre = almacenes.GetValueOrDefault(s.AlmacenId, ""), Estado = s.Estado,
                Items = nItems.GetValueOrDefault(s.Id), Cotizaciones = nCot.GetValueOrDefault(s.Id), Resumen = s.OrdenesGeneradas, CreadoEn = s.CreadoEn,
            }).ToList();
        }

        public async Task<CotizacionDetalleDto> Detalle(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await Armar(ctx, id);
        }

        public async Task<CotizacionDetalleDto> Crear(CotizacionCrearDto dto, long usuarioId)
        {
            if (dto.PedidoItemIds.Count + dto.Reposicion.Count == 0)
                throw new AbrilException("Selecciona al menos un ítem para cotizar.", 400);

            using var ctx = _factory.CreateDbContext();
            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId)) throw new AbrilException("Almacén no encontrado.", 404);

            var items = new List<CotizacionItem>();
            var pedidoItems = await ctx.PedidoItem.Include(pi => pi.Pedido).Where(pi => dto.PedidoItemIds.Contains(pi.Id)).ToListAsync();
            foreach (var pid in dto.PedidoItemIds.Distinct())
            {
                var pi = pedidoItems.FirstOrDefault(x => x.Id == pid) ?? throw new AbrilException($"Ítem de pedido {pid} no encontrado.", 404);
                if (pi.Pedido!.Estado != "APROBADO") throw new AbrilException($"El pedido {pi.Pedido.Codigo} no está aprobado.", 400);
                var pendiente = pi.CantidadSolicitada - pi.CantidadEnCompra;
                if (pendiente <= 0) continue; // ya está cubierto por otra compra
                items.Add(new CotizacionItem { ProductoId = pi.ProductoId, Talla = pi.Talla, Color = pi.Color, Cantidad = pendiente, PedidoItemId = pi.Id });
            }
            foreach (var r in dto.Reposicion)
            {
                if (r.Cantidad <= 0) throw new AbrilException("La cantidad a reponer debe ser mayor a cero.", 400);
                if (!await ctx.Producto.AnyAsync(p => p.Id == r.ProductoId)) throw new AbrilException($"Producto {r.ProductoId} no encontrado.", 404);
                items.Add(new CotizacionItem { ProductoId = r.ProductoId, Talla = r.Talla ?? "", Color = r.Color ?? "", Cantidad = r.Cantidad });
            }
            if (items.Count == 0) throw new AbrilException("Todo lo seleccionado ya está cubierto por una orden de compra.", 400);

            var correlativo = await ctx.Database.SqlQuery<long>($"""SELECT nextval('lb_cotizacion_correlativo') AS "Value" """).SingleAsync();
            var sol = new CotizacionSolicitud
            {
                Codigo = $"COT-{DateTime.UtcNow.Year}-{correlativo:D6}", AlmacenId = dto.AlmacenId, Estado = "ABIERTA",
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(), CreadoPor = usuarioId, CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.CotizacionSolicitud.Add(sol);
            await ctx.SaveChangesAsync();
            foreach (var it in items) it.SolicitudId = sol.Id;
            ctx.CotizacionItem.AddRange(items);
            await ctx.SaveChangesAsync();
            return await Armar(ctx, sol.Id);
        }

        /// <summary>Carga (o reemplaza) lo que respondió un proveedor.</summary>
        public async Task<CotizacionDetalleDto> AgregarCotizacion(long solicitudId, CotizacionAgregarDto dto, long usuarioId)
        {
            using var ctx = _factory.CreateDbContext();
            var sol = await ctx.CotizacionSolicitud.FirstOrDefaultAsync(s => s.Id == solicitudId) ?? throw new AbrilException("Solicitud no encontrada.", 404);
            if (sol.Estado != "ABIERTA") throw new AbrilException("Esta solicitud ya no admite cotizaciones.", 400);
            if (!await ctx.Proveedor.AnyAsync(p => p.Id == dto.ProveedorId)) throw new AbrilException("Elige un proveedor.", 400);
            if (dto.PlazoDias is < 0) throw new AbrilException("El plazo no puede ser negativo.", 400);

            var itemIds = await ctx.CotizacionItem.Where(i => i.SolicitudId == solicitudId).Select(i => i.Id).ToListAsync();
            var precios = dto.Precios.Where(p => p.Precio is > 0).ToList();
            if (precios.Count == 0) throw new AbrilException("Escribe el precio de al menos un ítem.", 400);
            if (precios.Any(p => !itemIds.Contains(p.ItemId))) throw new AbrilException("Hay un precio que no corresponde a esta solicitud.", 400);

            var previa = await ctx.Cotizacion.FirstOrDefaultAsync(c => c.SolicitudId == solicitudId && c.ProveedorId == dto.ProveedorId);
            if (previa != null)
            {
                ctx.CotizacionPrecio.RemoveRange(ctx.CotizacionPrecio.Where(p => p.CotizacionId == previa.Id));
                previa.PlazoDias = dto.PlazoDias;
                previa.CondicionPago = dto.CondicionPago?.Trim();
                previa.Observacion = dto.Observacion?.Trim();
                await ctx.SaveChangesAsync();
            }
            else
            {
                previa = new Cotizacion
                {
                    SolicitudId = solicitudId, ProveedorId = dto.ProveedorId, PlazoDias = dto.PlazoDias, CondicionPago = dto.CondicionPago?.Trim(),
                    Observacion = dto.Observacion?.Trim(), CreadoPor = usuarioId, CreadoEn = DateTimeOffset.UtcNow,
                };
                ctx.Cotizacion.Add(previa);
                await ctx.SaveChangesAsync();
            }
            ctx.CotizacionPrecio.AddRange(precios.Select(p => new CotizacionPrecio { CotizacionId = previa.Id, ItemId = p.ItemId, PrecioUnitario = p.Precio!.Value }));
            await ctx.SaveChangesAsync();
            return await Armar(ctx, solicitudId);
        }

        public async Task<CotizacionDetalleDto> EliminarCotizacion(long solicitudId, long cotizacionId)
        {
            using var ctx = _factory.CreateDbContext();
            var sol = await ctx.CotizacionSolicitud.FirstOrDefaultAsync(s => s.Id == solicitudId) ?? throw new AbrilException("Solicitud no encontrada.", 404);
            if (sol.Estado != "ABIERTA") throw new AbrilException("Esta solicitud ya está cerrada.", 400);
            var c = await ctx.Cotizacion.FirstOrDefaultAsync(x => x.Id == cotizacionId && x.SolicitudId == solicitudId) ?? throw new AbrilException("Cotización no encontrada.", 404);
            ctx.Cotizacion.Remove(c);
            await ctx.SaveChangesAsync();
            return await Armar(ctx, solicitudId);
        }

        public async Task<CotizacionDetalleDto> Cancelar(long solicitudId)
        {
            using var ctx = _factory.CreateDbContext();
            var sol = await ctx.CotizacionSolicitud.FirstOrDefaultAsync(s => s.Id == solicitudId) ?? throw new AbrilException("Solicitud no encontrada.", 404);
            if (sol.Estado != "ABIERTA") throw new AbrilException("Solo se puede cancelar una solicitud abierta.", 400);
            sol.Estado = "CANCELADA";
            await ctx.SaveChangesAsync();
            return await Armar(ctx, solicitudId);
        }

        /// <summary>Elige el ganador de cada ítem y genera una orden de compra por proveedor ganador.</summary>
        public async Task<CotizacionDetalleDto> Adjudicar(long solicitudId, CotizacionAdjudicarDto dto, long usuarioId)
        {
            using var ctx = _factory.CreateDbContext();
            var sol = await ctx.CotizacionSolicitud.FirstOrDefaultAsync(s => s.Id == solicitudId) ?? throw new AbrilException("Solicitud no encontrada.", 404);
            if (sol.Estado != "ABIERTA") throw new AbrilException("Esta solicitud ya fue adjudicada o cancelada.", 400);

            var items = await ctx.CotizacionItem.Where(i => i.SolicitudId == solicitudId).ToListAsync();
            var cotizaciones = await ctx.Cotizacion.AsNoTracking().Where(c => c.SolicitudId == solicitudId).ToListAsync();
            if (cotizaciones.Count == 0) throw new AbrilException("Primero carga al menos una cotización.", 400);
            var cotIds = cotizaciones.Select(c => c.Id).ToList();
            var precios = await ctx.CotizacionPrecio.AsNoTracking().Where(p => cotIds.Contains(p.CotizacionId)).ToListAsync();

            if (dto.Selecciones.Count != items.Count || items.Any(i => dto.Selecciones.Count(s => s.ItemId == i.Id) != 1))
                throw new AbrilException("Elige un proveedor para cada ítem.", 400);

            var motivo = dto.Motivo?.Trim();
            var requiereMotivo = cotizaciones.Count < 2;
            var elegidos = new List<(CotizacionItem Item, Cotizacion Cot, decimal Precio)>();
            foreach (var it in items)
            {
                var sel = dto.Selecciones.First(s => s.ItemId == it.Id);
                var cot = cotizaciones.FirstOrDefault(c => c.Id == sel.CotizacionId) ?? throw new AbrilException("Elegiste una cotización que no es de esta solicitud.", 400);
                var precio = precios.FirstOrDefault(p => p.CotizacionId == cot.Id && p.ItemId == it.Id)?.PrecioUnitario
                    ?? throw new AbrilException("Elegiste un proveedor que no cotizó uno de los ítems.", 400);
                var minimo = precios.Where(p => p.ItemId == it.Id).Min(p => p.PrecioUnitario);
                if (precio > minimo) requiereMotivo = true;
                elegidos.Add((it, cot, precio));
            }
            if (requiereMotivo && (motivo == null || motivo.Length < 5))
                throw new AbrilException(cotizaciones.Count < 2
                    ? "Hay una sola cotización: escribe por qué se compra así (por ejemplo, único proveedor)."
                    : "Elegiste algo que no es lo más barato: escribe el motivo (plazo, calidad, stock, etc.).", 400);

            // Una orden por proveedor ganador, con los precios que se eligieron.
            var generadas = new List<string>();
            foreach (var grupo in elegidos.GroupBy(e => e.Cot.ProveedorId))
            {
                try
                {
                    var oc = await _compras.GenerarDesdePedidos(new GenerarOrdenCompraDesdePedidosDto
                    {
                        ProveedorId = grupo.Key,
                        AlmacenId = sol.AlmacenId,
                        Observacion = $"Adjudicada de {sol.Codigo}",
                        Items = grupo.Select(g => new GenerarDesdePedidosItemDto
                        {
                            PedidoItemId = g.Item.PedidoItemId,
                            ProductoId = g.Item.PedidoItemId.HasValue ? null : g.Item.ProductoId,
                            Talla = g.Item.Talla, Color = g.Item.Color,
                            Cantidad = g.Item.PedidoItemId.HasValue ? null : g.Item.Cantidad,
                            CostoUnitario = g.Precio,
                        }).ToList(),
                    }, usuarioId);
                    generadas.Add(oc.Codigo);
                    foreach (var g in grupo) items.First(i => i.Id == g.Item.Id).CotizacionAdjudicadaId = g.Cot.Id;
                }
                catch (AbrilException ex) when (generadas.Count > 0)
                {
                    // Ya se crearon órdenes para otros proveedores: se deja constancia para no duplicarlas.
                    sol.OrdenesGeneradas = string.Join(", ", generadas);
                    await ctx.SaveChangesAsync();
                    throw new AbrilException($"{ex.Message} (Ya se crearon: {string.Join(", ", generadas)}; revisa Compras antes de reintentar.)", ex.StatusCode);
                }
            }

            sol.Estado = "ADJUDICADA";
            sol.MotivoAdjudicacion = motivo;
            sol.OrdenesGeneradas = string.Join(", ", generadas);
            sol.AdjudicadaPor = usuarioId;
            sol.AdjudicadaEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
            return await Armar(ctx, solicitudId);
        }

        private static async Task<CotizacionDetalleDto> Armar(AppDbContext ctx, long id)
        {
            var sol = await ctx.CotizacionSolicitud.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id) ?? throw new AbrilException("Solicitud no encontrada.", 404);
            var items = await ctx.CotizacionItem.AsNoTracking().Where(i => i.SolicitudId == id).OrderBy(i => i.Id).ToListAsync();
            var prodIds = items.Select(i => i.ProductoId).Distinct().ToList();
            var productos = await ctx.Producto.AsNoTracking().Where(p => prodIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Nombre);
            var pedidoItemIds = items.Where(i => i.PedidoItemId.HasValue).Select(i => i.PedidoItemId!.Value).ToList();
            var pedidos = await (from pi in ctx.PedidoItem.AsNoTracking() join pe in ctx.Pedido.AsNoTracking() on pi.PedidoId equals pe.Id
                                 where pedidoItemIds.Contains(pi.Id) select new { pi.Id, pe.Codigo }).ToDictionaryAsync(x => x.Id, x => x.Codigo);
            var cots = await ctx.Cotizacion.AsNoTracking().Where(c => c.SolicitudId == id).OrderBy(c => c.CreadoEn).ToListAsync();
            var cotIds = cots.Select(c => c.Id).ToList();
            var precios = await ctx.CotizacionPrecio.AsNoTracking().Where(p => cotIds.Contains(p.CotizacionId)).ToListAsync();
            var provIds = cots.Select(c => c.ProveedorId).ToList();
            var provs = await ctx.Proveedor.AsNoTracking().Where(p => provIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.RazonSocial);
            var autor = await ctx.UsuarioSistema.AsNoTracking().Where(u => u.Id == sol.CreadoPor).Select(u => u.Persona!.Apellidos + " " + u.Persona.Nombres).FirstOrDefaultAsync() ?? "";
            var almacen = await ctx.Almacen.AsNoTracking().Where(a => a.Id == sol.AlmacenId).Select(a => a.Nombre).FirstOrDefaultAsync() ?? "";
            var cantPorItem = items.ToDictionary(i => i.Id, i => i.Cantidad);

            return new CotizacionDetalleDto
            {
                Id = sol.Id, Codigo = sol.Codigo, AlmacenId = sol.AlmacenId, AlmacenNombre = almacen, Estado = sol.Estado, Observacion = sol.Observacion,
                MotivoAdjudicacion = sol.MotivoAdjudicacion, OrdenesGeneradas = sol.OrdenesGeneradas, CreadoPorNombre = autor, CreadoEn = sol.CreadoEn,
                Items = items.Select(i => new CotizacionItemDto
                {
                    Id = i.Id, ProductoNombre = productos.GetValueOrDefault(i.ProductoId, ""), Talla = i.Talla, Color = i.Color, Cantidad = i.Cantidad,
                    PedidoCodigo = i.PedidoItemId.HasValue ? pedidos.GetValueOrDefault(i.PedidoItemId.Value) : null, CotizacionAdjudicadaId = i.CotizacionAdjudicadaId,
                }).ToList(),
                Cotizaciones = cots.Select(c =>
                {
                    var mios = precios.Where(p => p.CotizacionId == c.Id).ToList();
                    return new CotizacionRespuestaDto
                    {
                        Id = c.Id, ProveedorId = c.ProveedorId, ProveedorNombre = provs.GetValueOrDefault(c.ProveedorId, ""), PlazoDias = c.PlazoDias,
                        CondicionPago = c.CondicionPago, Observacion = c.Observacion,
                        Precios = mios.ToDictionary(p => p.ItemId, p => p.PrecioUnitario),
                        Total = Math.Round(mios.Sum(p => p.PrecioUnitario * cantPorItem.GetValueOrDefault(p.ItemId)), 2),
                        Completa = mios.Count == items.Count,
                    };
                }).ToList(),
            };
        }
    }
}
