using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.AlmacenModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.AlmacenModule.Application.Services
{
    public class AlmacenKardexService : IAlmacenKardexService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AlmacenKardexService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<StockListResponseDto> ListStock(int? almacenId, string? search, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Stock.Include(s => s.Almacen).Include(s => s.Producto).AsQueryable();

            if (almacenId.HasValue)
                query = query.Where(s => s.AlmacenId == almacenId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.Producto!.Nombre.ToLower().Contains(s));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(s => s.Producto!.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StockListItemDto
                {
                    Id = s.Id,
                    AlmacenId = s.AlmacenId,
                    AlmacenNombre = s.Almacen!.Nombre,
                    ProductoId = s.ProductoId,
                    ProductoNombre = s.Producto!.Nombre,
                    ProductoCodigo = s.Producto.Codigo,
                    UnidadMedida = s.Producto.UnidadMedida,
                    Talla = s.Talla,
                    Color = s.Color,
                    CantidadActual = s.CantidadActual,
                    CostoPromedio = s.CostoPromedio,
                    StockMinimo = s.StockMinimo,
                    StockMaximo = s.StockMaximo,
                    BajoMinimo = s.CantidadActual < s.StockMinimo,
                })
                .ToListAsync();

            return new StockListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<MovimientoListResponseDto> ListMovimientos(int? almacenId, long? productoId, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Movimiento
                .Include(m => m.Almacen)
                .Include(m => m.Producto)
                .Include(m => m.UsuarioSistema!.Persona)
                .AsQueryable();

            if (almacenId.HasValue) query = query.Where(m => m.AlmacenId == almacenId.Value);
            if (productoId.HasValue) query = query.Where(m => m.ProductoId == productoId.Value);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(m => m.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MovimientoListItemDto
                {
                    Id = m.Id,
                    AlmacenNombre = m.Almacen!.Nombre,
                    ProductoNombre = m.Producto!.Nombre,
                    Talla = m.Talla,
                    Color = m.Color,
                    TipoMovimiento = m.TipoMovimiento,
                    Cantidad = m.Cantidad,
                    CostoUnitario = m.CostoUnitario,
                    UsuarioNombre = m.UsuarioSistema != null && m.UsuarioSistema.Persona != null
                        ? m.UsuarioSistema.Persona.Apellidos + " " + m.UsuarioSistema.Persona.Nombres
                        : null,
                    CreadoEn = m.CreadoEn,
                })
                .ToListAsync();

            return new MovimientoListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        /// <summary>
        /// [CONTEXT_LOGISTICA.md sección 7 — DECIDIDO] cantidad_actual se mantiene como contador
        /// (no se recalcula sumando el historial cada vez). Todo movimiento se registra dentro de
        /// una sola transacción con SELECT ... FOR UPDATE, que bloquea solo esa fila puntual de
        /// lb_stock (ese almacén+producto+talla) hasta el COMMIT — dos personas descontando el
        /// mismo producto al mismo tiempo se serializan sin bloquear al resto del catálogo. Si la
        /// conexión se cae a mitad de camino, Postgres revierte todo solo (nunca queda un
        /// movimiento sin su descuento de stock, o viceversa).
        /// </summary>
        public async Task RegistrarMovimiento(RegistrarMovimientoDto dto, long? usuarioSistemaId)
        {
            if (dto.TipoMovimiento != "INGRESO" && dto.TipoMovimiento != "SALIDA")
                throw new AbrilException("Tipo de movimiento inválido — debe ser INGRESO o SALIDA.", 400);
            if (dto.Cantidad <= 0)
                throw new AbrilException("La cantidad debe ser mayor a cero.", 400);

            using var ctx = _factory.CreateDbContext();

            // [CORREGIDO] Npgsql tiene reintentos automáticos habilitados (EnableRetryOnFailure),
            // y esa estrategia de ejecución no permite una transacción abierta a mano
            // (Database.BeginTransactionAsync) porque no sabría cómo reintentarla completa si falla
            // a la mitad — hay que envolver TODO (transacción + queries) en CreateExecutionStrategy().
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await ctx.Database.BeginTransactionAsync();

                if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                    throw new AbrilException("Almacén no encontrado.", 404);
                if (!await ctx.Producto.AnyAsync(p => p.Id == dto.ProductoId))
                    throw new AbrilException("Producto no encontrado.", 404);

                // Asegura que exista la fila de stock antes de bloquearla — upsert nativo de
                // Postgres, aprovecha el UNIQUE(almacen_id, producto_id, talla, color) de la tabla.
                await ctx.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO lb_stock (almacen_id, producto_id, talla, color, cantidad_actual, costo_promedio, actualizado_en)
                    VALUES ({dto.AlmacenId}, {dto.ProductoId}, {dto.Talla}, {dto.Color}, 0, 0, now())
                    ON CONFLICT (almacen_id, producto_id, talla, color) DO NOTHING
                    """);

                var stockActual = await ctx.Database
                    .SqlQuery<StockActualRow>($"""
                        SELECT cantidad_actual AS "CantidadActual", costo_promedio AS "CostoPromedio" FROM lb_stock
                        WHERE almacen_id = {dto.AlmacenId} AND producto_id = {dto.ProductoId} AND talla = {dto.Talla} AND color = {dto.Color}
                        FOR UPDATE
                        """)
                    .SingleAsync();

                if (dto.TipoMovimiento == "SALIDA" && stockActual.CantidadActual < dto.Cantidad)
                    throw new AbrilException($"Stock insuficiente — quedan {stockActual.CantidadActual} y se pidieron {dto.Cantidad}.", 400);

                // Costeo por promedio ponderado [DECIDIDO 2026-09-24]: un INGRESO con costo conocido
                // (compra) recalcula el promedio del almacén para ese producto; un INGRESO sin costo
                // (ajuste manual) no lo toca, para no ensuciar el promedio con un costo desconocido.
                // Una SALIDA nunca cambia el promedio — solo se valoriza a él, snapshot que queda
                // grabado en el propio movimiento para no perder el costo histórico si el promedio
                // sigue moviéndose después.
                decimal? costoMovimiento = dto.CostoUnitario;
                decimal? nuevoCostoPromedio = null;
                if (dto.TipoMovimiento == "INGRESO" && dto.CostoUnitario.HasValue)
                {
                    var cantidadTotal = stockActual.CantidadActual + dto.Cantidad;
                    nuevoCostoPromedio = cantidadTotal > 0
                        ? ((stockActual.CantidadActual * stockActual.CostoPromedio) + (dto.Cantidad * dto.CostoUnitario.Value)) / cantidadTotal
                        : dto.CostoUnitario.Value;
                }
                else if (dto.TipoMovimiento == "SALIDA" && costoMovimiento is null)
                {
                    costoMovimiento = stockActual.CostoPromedio;
                }

                ctx.Movimiento.Add(new Movimiento
                {
                    AlmacenId = dto.AlmacenId,
                    ProductoId = dto.ProductoId,
                    Talla = dto.Talla,
                    Color = dto.Color,
                    TipoMovimiento = dto.TipoMovimiento,
                    Cantidad = dto.Cantidad,
                    CostoUnitario = costoMovimiento,
                    ReferenciaTipo = dto.ReferenciaTipo,
                    ReferenciaId = dto.ReferenciaId,
                    UsuarioSistemaId = usuarioSistemaId,
                    CreadoEn = DateTimeOffset.UtcNow,
                });
                await ctx.SaveChangesAsync();

                var signo = dto.TipoMovimiento == "INGRESO" ? 1 : -1;
                if (nuevoCostoPromedio.HasValue)
                {
                    await ctx.Database.ExecuteSqlInterpolatedAsync($"""
                        UPDATE lb_stock SET cantidad_actual = cantidad_actual + ({signo} * {dto.Cantidad}),
                            costo_promedio = {nuevoCostoPromedio.Value}, actualizado_en = now()
                        WHERE almacen_id = {dto.AlmacenId} AND producto_id = {dto.ProductoId} AND talla = {dto.Talla} AND color = {dto.Color}
                        """);
                }
                else
                {
                    await ctx.Database.ExecuteSqlInterpolatedAsync($"""
                        UPDATE lb_stock SET cantidad_actual = cantidad_actual + ({signo} * {dto.Cantidad}), actualizado_en = now()
                        WHERE almacen_id = {dto.AlmacenId} AND producto_id = {dto.ProductoId} AND talla = {dto.Talla} AND color = {dto.Color}
                        """);
                }

                await tx.CommitAsync();
            });
        }

        private class StockActualRow
        {
            public decimal CantidadActual { get; set; }
            public decimal CostoPromedio { get; set; }
        }

        public async Task<List<ReposicionSugeridaDto>> ListReposicionSugerida(int? almacenId)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Stock
                .Include(s => s.Almacen)
                .Include(s => s.Producto)
                .Where(s => s.CantidadActual < s.StockMinimo)
                .AsQueryable();

            if (almacenId.HasValue)
                query = query.Where(s => s.AlmacenId == almacenId.Value);

            var rows = await query.ToListAsync();

            return rows.Select(s => new ReposicionSugeridaDto
            {
                AlmacenId = s.AlmacenId,
                AlmacenNombre = s.Almacen!.Nombre,
                ProductoId = s.ProductoId,
                ProductoNombre = s.Producto!.Nombre,
                Talla = s.Talla,
                Color = s.Color,
                CantidadActual = s.CantidadActual,
                StockMinimo = s.StockMinimo,
                StockMaximo = s.StockMaximo,
                CantidadSugerida = (s.StockMaximo ?? s.StockMinimo) - s.CantidadActual,
            })
            .OrderBy(r => r.ProductoNombre)
            .ToList();
        }

        public async Task AjustarUmbrales(AjustarUmbralesDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                throw new AbrilException("Almacén no encontrado.", 404);
            if (!await ctx.Producto.AnyAsync(p => p.Id == dto.ProductoId))
                throw new AbrilException("Producto no encontrado.", 404);

            var stock = await ctx.Stock.FirstOrDefaultAsync(s =>
                s.AlmacenId == dto.AlmacenId && s.ProductoId == dto.ProductoId && s.Talla == dto.Talla && s.Color == dto.Color);

            if (stock == null)
            {
                stock = new Stock
                {
                    AlmacenId = dto.AlmacenId,
                    ProductoId = dto.ProductoId,
                    Talla = dto.Talla,
                    Color = dto.Color,
                    CantidadActual = 0,
                    ActualizadoEn = DateTimeOffset.UtcNow,
                };
                ctx.Stock.Add(stock);
            }

            stock.StockMinimo = dto.StockMinimo;
            stock.StockMaximo = dto.StockMaximo;
            stock.ActualizadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
