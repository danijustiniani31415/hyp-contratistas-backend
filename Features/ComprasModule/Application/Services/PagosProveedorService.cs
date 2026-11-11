using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.ComprasModule.Application.Services
{
    /// <summary>
    /// Cuentas por pagar: los comprobantes de compra (factura, boleta, recibo...) con lo pagado y el saldo, y el registro de pagos
    /// totales o parciales. Las notas de crédito/débito y las guías no se pagan. Un pago anulado deja de contar pero queda en el historial.
    /// </summary>
    public class PagosProveedorService
    {
        private static readonly string[] NoPagables = { "NOTA_CREDITO", "NOTA_DEBITO", "GUIA_REMISION" };
        private static readonly HashSet<string> Medios = new() { "TRANSFERENCIA", "EFECTIVO", "CHEQUE", "DEPOSITO", "YAPE_PLIN" };

        private readonly IDbContextFactory<AppDbContext> _factory;

        public PagosProveedorService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        private static string EstadoPago(decimal total, decimal pagado) =>
            total - pagado <= 0.004m ? "PAGADO" : pagado > 0 ? "PARCIAL" : "PENDIENTE";

        private static IQueryable<CuentaPorPagarDto> Proyectar(AppDbContext ctx)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            return ctx.ComprobanteCompra.AsNoTracking()
                .Where(c => c.Estado == "REGISTRADO" && !NoPagables.Contains(c.Tipo))
                .Select(c => new
                {
                    c,
                    Pagado = ctx.PagoProveedor.Where(p => p.ComprobanteCompraId == c.Id && p.Estado == "VIGENTE").Sum(p => (decimal?)p.Monto) ?? 0m,
                })
                .Select(x => new CuentaPorPagarDto
                {
                    ComprobanteId = x.c.Id,
                    ProveedorId = x.c.ProveedorId,
                    ProveedorNombre = x.c.Proveedor!.RazonSocial,
                    ProveedorRuc = x.c.Proveedor.Ruc,
                    Tipo = x.c.Tipo,
                    Documento = x.c.Serie + "-" + x.c.Numero,
                    FechaEmision = x.c.FechaEmision,
                    Moneda = x.c.Moneda,
                    Total = x.c.Total,
                    DetraccionMonto = x.c.DetraccionMonto ?? 0m,
                    Pagado = x.Pagado,
                    Saldo = x.c.Total - x.Pagado,
                    EstadoPago = x.c.Total - x.Pagado <= 0.004m ? "PAGADO" : x.Pagado > 0 ? "PARCIAL" : "PENDIENTE",
                    DiasAntiguedad = hoy.DayNumber - x.c.FechaEmision.DayNumber,
                });
        }

        public async Task<(List<CuentaPorPagarDto> Data, int Total)> Listar(string? search, string? estadoPago, int? proveedorId, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();
            var q = Proyectar(ctx);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.ProveedorNombre.ToLower().Contains(s) || (x.ProveedorRuc != null && x.ProveedorRuc.Contains(s)) || x.Documento.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(estadoPago)) q = q.Where(x => x.EstadoPago == estadoPago);
            if (proveedorId.HasValue) q = q.Where(x => x.ProveedorId == proveedorId.Value);

            var total = await q.CountAsync();
            // Primero lo que se debe (más antiguo arriba), después lo ya pagado.
            var data = await q.OrderBy(x => x.EstadoPago == "PAGADO").ThenBy(x => x.FechaEmision).ThenBy(x => x.ComprobanteId)
                .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync();
            return (data, total);
        }

        public async Task<PagosResumenDto> Resumen()
        {
            using var ctx = _factory.CreateDbContext();
            var pendientes = await Proyectar(ctx).Where(x => x.EstadoPago != "PAGADO").ToListAsync();
            return new PagosResumenDto
            {
                PendientePorMoneda = pendientes.GroupBy(x => x.Moneda).Select(g => new SaldoMonedaDto { Moneda = g.Key, Saldo = g.Sum(x => x.Saldo) }).OrderBy(x => x.Moneda).ToList(),
                DocumentosPendientes = pendientes.Count,
                ProveedoresConDeuda = pendientes.Select(x => x.ProveedorId).Distinct().Count(),
            };
        }

        public async Task<CuentaPorPagarDetalleDto> Obtener(long comprobanteId)
        {
            using var ctx = _factory.CreateDbContext();
            return await Detalle(ctx, comprobanteId);
        }

        private static async Task<CuentaPorPagarDetalleDto> Detalle(AppDbContext ctx, long comprobanteId)
        {
            var c = await Proyectar(ctx).FirstOrDefaultAsync(x => x.ComprobanteId == comprobanteId)
                ?? throw new AbrilException("Comprobante no encontrado o no es un documento pagable.", 404);
            var pagos = await (from p in ctx.PagoProveedor.AsNoTracking()
                               where p.ComprobanteCompraId == comprobanteId
                               join u in ctx.UsuarioSistema.AsNoTracking().Include(u => u.Persona) on p.RegistradoPor equals u.Id into uj
                               from u in uj.DefaultIfEmpty()
                               orderby p.FechaPago descending, p.Id descending
                               select new PagoDto
                               {
                                   Id = p.Id, FechaPago = p.FechaPago, Monto = p.Monto, Medio = p.Medio, Referencia = p.Referencia,
                                   Observacion = p.Observacion, Estado = p.Estado, RegistradoEn = p.RegistradoEn, MotivoAnulacion = p.MotivoAnulacion,
                                   RegistradoPorNombre = u == null || u.Persona == null ? "" : u.Persona.Apellidos + " " + u.Persona.Nombres,
                               }).ToListAsync();
            return new CuentaPorPagarDetalleDto
            {
                ComprobanteId = c.ComprobanteId, ProveedorId = c.ProveedorId, ProveedorNombre = c.ProveedorNombre, ProveedorRuc = c.ProveedorRuc,
                Tipo = c.Tipo, Documento = c.Documento, FechaEmision = c.FechaEmision, Moneda = c.Moneda, Total = c.Total,
                DetraccionMonto = c.DetraccionMonto, Pagado = c.Pagado, Saldo = c.Saldo, EstadoPago = c.EstadoPago, DiasAntiguedad = c.DiasAntiguedad,
                Pagos = pagos,
            };
        }

        public async Task<CuentaPorPagarDetalleDto> Registrar(long comprobanteId, PagoRegistrarDto dto, long usuarioId)
        {
            var medio = (dto.Medio ?? "").Trim().ToUpperInvariant();
            if (!Medios.Contains(medio)) throw new AbrilException("Elige un medio de pago válido.", 400);
            var monto = Math.Round(dto.Monto, 2);
            if (monto <= 0) throw new AbrilException("El monto debe ser mayor a cero.", 400);
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            if (dto.FechaPago > hoy) throw new AbrilException("La fecha de pago no puede ser futura.", 400);
            var referencia = string.IsNullOrWhiteSpace(dto.Referencia) ? null : dto.Referencia.Trim();
            if (medio is "TRANSFERENCIA" or "CHEQUE" or "DEPOSITO" && referencia is null)
                throw new AbrilException("Indica el número de operación o de cheque.", 400);

            using var ctx = _factory.CreateDbContext();
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                ctx.ChangeTracker.Clear();
                using var tx = await ctx.Database.BeginTransactionAsync();
                // Bloquea el comprobante: dos pagos simultáneos no pueden pasarse del saldo.
                await ctx.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM lb_comprobante_compra WHERE id = {comprobanteId} FOR UPDATE");
                var cuenta = await Proyectar(ctx).FirstOrDefaultAsync(x => x.ComprobanteId == comprobanteId)
                    ?? throw new AbrilException("Comprobante no encontrado o no es un documento pagable.", 404);
                if (monto > cuenta.Saldo + 0.004m)
                    throw new AbrilException($"El monto ({monto:N2}) supera el saldo pendiente ({cuenta.Saldo:N2} {cuenta.Moneda}).", 409);
                if (dto.FechaPago < cuenta.FechaEmision)
                    throw new AbrilException("La fecha de pago no puede ser anterior a la emisión del comprobante.", 400);

                ctx.PagoProveedor.Add(new PagoProveedor
                {
                    ComprobanteCompraId = comprobanteId, FechaPago = dto.FechaPago, Monto = monto, Medio = medio, Referencia = referencia,
                    Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(),
                    RegistradoPor = usuarioId, RegistradoEn = DateTimeOffset.UtcNow,
                });
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
            });
            return await Detalle(ctx, comprobanteId);
        }

        public async Task<CuentaPorPagarDetalleDto> Anular(long pagoId, string motivo, long usuarioId)
        {
            if (string.IsNullOrWhiteSpace(motivo)) throw new AbrilException("Indica el motivo de la anulación.", 400);
            using var ctx = _factory.CreateDbContext();
            var pago = await ctx.PagoProveedor.FirstOrDefaultAsync(p => p.Id == pagoId) ?? throw new AbrilException("Pago no encontrado.", 404);
            if (pago.Estado == "ANULADO") throw new AbrilException("El pago ya estaba anulado.", 409);
            pago.Estado = "ANULADO";
            pago.AnuladoPor = usuarioId;
            pago.AnuladoEn = DateTimeOffset.UtcNow;
            pago.MotivoAnulacion = motivo.Trim();
            await ctx.SaveChangesAsync();
            return await Detalle(ctx, pago.ComprobanteCompraId);
        }
    }
}
