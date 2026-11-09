using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.AlmacenModule.Application.Services
{
    /// <summary>
    /// Kardex valorizado (promedio ponderado) y toma de inventario físico. El kardex se calcula recorriendo el historial
    /// inmutable de lb_movimiento; la toma de inventario guarda lo contado y, al cerrarla, genera los ajustes como
    /// movimientos reales (referencia INVENTARIO) para que el stock y el kardex siempre cuadren.
    /// </summary>
    public class AlmacenInventarioService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AlmacenInventarioService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        // ── Kardex valorizado ──────────────────────────────────────────────────────────────────────────────────

        public async Task<KardexValorizadoDto> Kardex(int almacenId, long productoId, string talla, string color, DateOnly? desde, DateOnly? hasta, HashSet<int>? alc = null)
        {
            AlmacenAlcance.Exigir(alc, almacenId);
            using var ctx = _factory.CreateDbContext();
            talla ??= "";
            color ??= "";

            var almacen = await ctx.Almacen.AsNoTracking().FirstOrDefaultAsync(a => a.Id == almacenId)
                ?? throw new AbrilException("Almacén no encontrado.", 404);
            var producto = await ctx.Producto.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productoId)
                ?? throw new AbrilException("Producto no encontrado.", 404);

            var movs = await ctx.Movimiento.AsNoTracking()
                .Where(m => m.AlmacenId == almacenId && m.ProductoId == productoId && m.Talla == talla && m.Color == color)
                .OrderBy(m => m.CreadoEn).ThenBy(m => m.Id)
                .ToListAsync();

            // Fechas en hora de Perú (UTC-5): un movimiento de las 22:00 del día 5 pertenece al día 5, no al 6 UTC.
            static DateOnly Dia(DateTimeOffset t) => DateOnly.FromDateTime(t.ToOffset(TimeSpan.FromHours(-5)).DateTime);

            var res = new KardexValorizadoDto
            {
                AlmacenNombre = almacen.Nombre,
                ProductoNombre = producto.Nombre,
                UnidadMedida = producto.UnidadMedida,
                Talla = talla,
                Color = color,
            };

            decimal saldo = 0m, promedio = 0m;
            foreach (var m in movs)
            {
                var fecha = Dia(m.CreadoEn);
                var antes = desde.HasValue && fecha < desde.Value;
                if (hasta.HasValue && fecha > hasta.Value) break;

                var linea = new KardexLineaDto { Fecha = m.CreadoEn, Tipo = m.TipoMovimiento, Referencia = m.ReferenciaTipo };
                if (m.TipoMovimiento == "INGRESO")
                {
                    // Igual que RegistrarMovimiento: un ingreso con costo recalcula el promedio; sin costo no lo toca.
                    var costo = m.CostoUnitario ?? promedio;
                    if (m.CostoUnitario.HasValue)
                    {
                        var total = saldo + m.Cantidad;
                        promedio = total > 0 ? ((saldo * promedio) + (m.Cantidad * m.CostoUnitario.Value)) / total : m.CostoUnitario.Value;
                    }
                    saldo += m.Cantidad;
                    linea.EntradaCantidad = m.Cantidad;
                    linea.EntradaCostoUnitario = costo;
                    linea.EntradaTotal = Math.Round(m.Cantidad * costo, 2);
                }
                else
                {
                    var costo = m.CostoUnitario ?? promedio;
                    saldo -= m.Cantidad;
                    linea.SalidaCantidad = m.Cantidad;
                    linea.SalidaCostoUnitario = costo;
                    linea.SalidaTotal = Math.Round(m.Cantidad * costo, 2);
                }
                linea.SaldoCantidad = saldo;
                linea.SaldoCostoUnitario = Math.Round(promedio, 4);
                linea.SaldoTotal = Math.Round(saldo * promedio, 2);

                if (antes)
                {
                    res.SaldoInicialCantidad = saldo;
                    res.SaldoInicialCostoUnitario = Math.Round(promedio, 4);
                    res.SaldoInicialTotal = linea.SaldoTotal;
                }
                else
                {
                    res.Lineas.Add(linea);
                    res.TotalEntradas += linea.EntradaTotal;
                    res.TotalSalidas += linea.SalidaTotal;
                }
            }
            res.SaldoFinalCantidad = saldo;
            res.SaldoFinalTotal = Math.Round(saldo * promedio, 2);
            return res;
        }

        private static async Task Chequear(AppDbContext ctx, long inventarioId, HashSet<int>? alc)
        {
            if (alc == null) return;
            var almacenId = await ctx.Inventario.AsNoTracking().Where(i => i.Id == inventarioId).Select(i => (int?)i.AlmacenId).FirstOrDefaultAsync()
                ?? throw new AbrilException("Inventario no encontrado.", 404);
            AlmacenAlcance.Exigir(alc, almacenId);
        }

        // ── Toma de inventario ─────────────────────────────────────────────────────────────────────────────────

        public async Task<List<InventarioListDto>> Listar(int? almacenId, HashSet<int>? alc = null)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.Inventario.AsNoTracking().AsQueryable();
            if (alc != null) q = q.Where(i => alc.Contains(i.AlmacenId));
            if (almacenId.HasValue) q = q.Where(i => i.AlmacenId == almacenId.Value);
            var nombres = await ctx.Almacen.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.Nombre);
            var lista = await q.OrderByDescending(i => i.Id).Take(100)
                .Select(i => new
                {
                    i.Id, i.AlmacenId, i.Fecha, i.Estado, i.Observacion, i.CantidadAjustes, i.CreadoEn,
                    Total = i.Items.Count, Contados = i.Items.Count(x => x.CantidadContada != null),
                }).ToListAsync();
            return lista.Select(i => new InventarioListDto
            {
                Id = i.Id, AlmacenId = i.AlmacenId, AlmacenNombre = nombres.GetValueOrDefault(i.AlmacenId, ""),
                Fecha = i.Fecha, Estado = i.Estado, Observacion = i.Observacion, TotalItems = i.Total,
                Contados = i.Contados, CantidadAjustes = i.CantidadAjustes, CreadoEn = i.CreadoEn,
            }).ToList();
        }

        public async Task<InventarioDetalleDto> Obtener(long id, HashSet<int>? alc = null)
        {
            using var ctx = _factory.CreateDbContext();
            await Chequear(ctx, id, alc);
            return await Detalle(ctx, id);
        }

        private static async Task<InventarioDetalleDto> Detalle(AppDbContext ctx, long id)
        {
            var inv = await ctx.Inventario.AsNoTracking().Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new AbrilException("Inventario no encontrado.", 404);
            var almacen = await ctx.Almacen.AsNoTracking().FirstOrDefaultAsync(a => a.Id == inv.AlmacenId);
            var ids = inv.Items.Select(x => x.ProductoId).Distinct().ToList();
            var productos = await ctx.Producto.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
            var costos = await ctx.Stock.AsNoTracking().Where(s => s.AlmacenId == inv.AlmacenId && ids.Contains(s.ProductoId))
                .ToDictionaryAsync(s => (s.ProductoId, s.Talla, s.Color), s => s.CostoPromedio);

            return new InventarioDetalleDto
            {
                Id = inv.Id, AlmacenId = inv.AlmacenId, AlmacenNombre = almacen?.Nombre ?? "", Fecha = inv.Fecha,
                Estado = inv.Estado, Observacion = inv.Observacion, CantidadAjustes = inv.CantidadAjustes, CreadoEn = inv.CreadoEn,
                TotalItems = inv.Items.Count, Contados = inv.Items.Count(x => x.CantidadContada != null),
                Items = inv.Items
                    .OrderBy(x => productos.TryGetValue(x.ProductoId, out var p) ? p.Nombre : "").ThenBy(x => x.Talla).ThenBy(x => x.Color)
                    .Select(x =>
                    {
                        productos.TryGetValue(x.ProductoId, out var p);
                        return new InventarioItemDto
                        {
                            Id = x.Id, ProductoId = x.ProductoId, ProductoNombre = p?.Nombre ?? "", ProductoCodigo = p?.Codigo,
                            UnidadMedida = p?.UnidadMedida ?? "", Talla = x.Talla, Color = x.Color,
                            CantidadSistema = x.CantidadSistema, CantidadContada = x.CantidadContada, CantidadAjuste = x.CantidadAjuste,
                            CostoPromedio = costos.GetValueOrDefault((x.ProductoId, x.Talla, x.Color)), Motivo = x.Motivo,
                        };
                    }).ToList(),
            };
        }

        /// <summary>Abre el conteo de un almacén con una foto de su stock actual. Un almacén tiene un solo conteo abierto a la vez.</summary>
        public async Task<InventarioDetalleDto> Crear(InventarioCrearDto dto, long? usuarioId, HashSet<int>? alc = null)
        {
            AlmacenAlcance.Exigir(alc, dto.AlmacenId);
            using var ctx = _factory.CreateDbContext();
            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId)) throw new AbrilException("Almacén no encontrado.", 404);
            if (await ctx.Inventario.AnyAsync(i => i.AlmacenId == dto.AlmacenId && i.Estado == "EN_CONTEO"))
                throw new AbrilException("Este almacén ya tiene un conteo abierto. Ciérralo o anúlalo antes de abrir otro.", 409);

            var stock = await ctx.Stock.AsNoTracking().Where(s => s.AlmacenId == dto.AlmacenId && s.CantidadActual != 0).ToListAsync();
            var inv = new Inventario
            {
                AlmacenId = dto.AlmacenId,
                Fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)),
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(),
                CreadoPor = usuarioId,
                CreadoEn = DateTimeOffset.UtcNow,
                Items = stock.Select(s => new InventarioItem
                {
                    ProductoId = s.ProductoId, Talla = s.Talla, Color = s.Color, CantidadSistema = s.CantidadActual,
                }).ToList(),
            };
            ctx.Inventario.Add(inv);
            await ctx.SaveChangesAsync();
            return await Detalle(ctx, inv.Id);
        }

        public async Task<InventarioDetalleDto> GuardarConteo(long id, List<InventarioConteoItemDto> items, HashSet<int>? alc = null)
        {
            using var ctx = _factory.CreateDbContext();
            await Chequear(ctx, id, alc);
            var inv = await ctx.Inventario.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new AbrilException("Inventario no encontrado.", 404);
            if (inv.Estado != "EN_CONTEO") throw new AbrilException("El inventario ya está cerrado o anulado.", 409);

            foreach (var d in items)
            {
                var it = inv.Items.FirstOrDefault(x => x.Id == d.ItemId) ?? throw new AbrilException("Ítem no pertenece a este inventario.", 400);
                if (d.CantidadContada is < 0) throw new AbrilException("La cantidad contada no puede ser negativa.", 400);
                it.CantidadContada = d.CantidadContada;
                it.Motivo = string.IsNullOrWhiteSpace(d.Motivo) ? null : d.Motivo.Trim();
            }
            await ctx.SaveChangesAsync();
            return await Detalle(ctx, id);
        }

        /// <summary>Agrega a un conteo abierto un producto que el sistema no tiene en ese almacén (sobrante hallado en el físico).</summary>
        public async Task<InventarioDetalleDto> AgregarItem(long id, InventarioAgregarItemDto dto, HashSet<int>? alc = null)
        {
            using var ctx = _factory.CreateDbContext();
            await Chequear(ctx, id, alc);
            var inv = await ctx.Inventario.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new AbrilException("Inventario no encontrado.", 404);
            if (inv.Estado != "EN_CONTEO") throw new AbrilException("El inventario ya está cerrado o anulado.", 409);
            if (!await ctx.Producto.AnyAsync(p => p.Id == dto.ProductoId)) throw new AbrilException("Producto no encontrado.", 404);
            var talla = dto.Talla ?? ""; var color = dto.Color ?? "";
            if (inv.Items.Any(x => x.ProductoId == dto.ProductoId && x.Talla == talla && x.Color == color))
                throw new AbrilException("Ese producto ya está en el conteo.", 409);

            var actual = await ctx.Stock.AsNoTracking().FirstOrDefaultAsync(s => s.AlmacenId == inv.AlmacenId && s.ProductoId == dto.ProductoId && s.Talla == talla && s.Color == color);
            inv.Items.Add(new InventarioItem { ProductoId = dto.ProductoId, Talla = talla, Color = color, CantidadSistema = actual?.CantidadActual ?? 0 });
            await ctx.SaveChangesAsync();
            return await Detalle(ctx, id);
        }

        /// <summary>
        /// Cierra el conteo: por cada ítem contado compara contra el stock VIGENTE (no contra la foto de apertura, que pudo
        /// moverse) y genera el ajuste: sobrante → INGRESO, faltante → SALIDA, ambos con referencia INVENTARIO. Todo en una
        /// sola transacción: o se ajusta todo o nada.
        /// </summary>
        public async Task<InventarioDetalleDto> Cerrar(long id, long? usuarioId, HashSet<int>? alc = null)
        {
            using var ctx = _factory.CreateDbContext();
            await Chequear(ctx, id, alc);
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                ctx.ChangeTracker.Clear();
                using var tx = await ctx.Database.BeginTransactionAsync();
                var inv = await ctx.Inventario.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id)
                    ?? throw new AbrilException("Inventario no encontrado.", 404);
                if (inv.Estado != "EN_CONTEO") throw new AbrilException("El inventario ya está cerrado o anulado.", 409);
                if (!inv.Items.Any(x => x.CantidadContada.HasValue)) throw new AbrilException("Todavía no se contó ningún producto.", 400);

                var ajustes = 0;
                foreach (var it in inv.Items.Where(x => x.CantidadContada.HasValue))
                {
                    var vigente = await ctx.Stock.AsNoTracking()
                        .Where(s => s.AlmacenId == inv.AlmacenId && s.ProductoId == it.ProductoId && s.Talla == it.Talla && s.Color == it.Color)
                        .Select(s => (decimal?)s.CantidadActual).FirstOrDefaultAsync() ?? 0m;
                    var diff = it.CantidadContada!.Value - vigente;
                    it.CantidadAjuste = diff;
                    if (diff == 0) continue;

                    await AlmacenKardexService.AplicarMovimientoAsync(ctx, new RegistrarMovimientoDto
                    {
                        AlmacenId = inv.AlmacenId, ProductoId = it.ProductoId, Talla = it.Talla, Color = it.Color,
                        TipoMovimiento = diff > 0 ? "INGRESO" : "SALIDA", Cantidad = Math.Abs(diff),
                        ReferenciaTipo = "INVENTARIO", ReferenciaId = inv.Id,
                    }, usuarioId);
                    ajustes++;
                }
                inv.Estado = "CERRADO";
                inv.CerradoPor = usuarioId;
                inv.CerradoEn = DateTimeOffset.UtcNow;
                inv.CantidadAjustes = ajustes;
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
            });
            ctx.ChangeTracker.Clear();
            return await Detalle(ctx, id);
        }

        public async Task Anular(long id, HashSet<int>? alc = null)
        {
            using var ctx = _factory.CreateDbContext();
            await Chequear(ctx, id, alc);
            var inv = await ctx.Inventario.FirstOrDefaultAsync(i => i.Id == id) ?? throw new AbrilException("Inventario no encontrado.", 404);
            if (inv.Estado != "EN_CONTEO") throw new AbrilException("Solo se puede anular un conteo abierto.", 409);
            inv.Estado = "ANULADO";
            await ctx.SaveChangesAsync();
        }
    }
}
