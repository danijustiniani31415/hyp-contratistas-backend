using Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Models;

namespace Abril_Backend.Features.FacturacionElectronicaModule.Application.Services
{
    /// <summary>
    /// Cálculo de impuestos del comprobante, con las reglas de redondeo de SUNAT (2 decimales, mitad hacia arriba).
    /// Es una función pura: no toca la base ni SUNAT, para poder probarla sola. IGV 18 % [VERIFICAR que siga vigente].
    /// </summary>
    public static class CpeCalculo
    {
        public const decimal TasaIgv = 0.18m;

        /// <summary>Precio unitario con IGV de una línea (el que va en el PricingReference del XML).</summary>
        public static decimal PrecioConIgv(CpeItem it) => it.AfectacionIgv.StartsWith('1') ? it.ValorUnitario * (1 + TasaIgv) : it.ValorUnitario;

        public static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        /// <summary>Recalcula cada línea y los totales del comprobante a partir de cantidad, precio, descuento y afectación.</summary>
        public static void Recalcular(Cpe cpe)
        {
            decimal gravado = 0, exonerado = 0, inafecto = 0, exportacion = 0, descuentos = 0, igv = 0;

            foreach (var it in cpe.Items)
            {
                var gravada = it.AfectacionIgv.StartsWith('1');
                // PrecioUnitario es SIEMPRE lo que digitó el usuario y no se modifica: así recalcular es idempotente. De ahí sale el valor sin IGV.
                var digitado = it.PrecioUnitario;
                var valorUnit = gravada && cpe.PreciosIncluyenIgv ? digitado / (1 + TasaIgv) : digitado;
                it.ValorUnitario = Math.Round(valorUnit, 6, MidpointRounding.AwayFromZero);
                it.ValorVenta = R2(it.Cantidad * valorUnit - it.Descuento);
                it.Igv = gravada ? R2(it.ValorVenta * TasaIgv) : 0;

                switch (it.AfectacionIgv[0])
                {
                    case '1': gravado += it.ValorVenta; break;
                    case '2': exonerado += it.ValorVenta; break;
                    case '3': inafecto += it.ValorVenta; break;
                    case '4': exportacion += it.ValorVenta; break;
                }
                descuentos += it.Descuento;
                igv += it.Igv;
            }

            cpe.TotalGravado = R2(gravado);
            cpe.TotalExonerado = R2(exonerado);
            cpe.TotalInafecto = R2(inafecto);
            cpe.TotalExportacion = R2(exportacion);
            cpe.TotalDescuentos = R2(descuentos);
            cpe.Igv = R2(igv);
            cpe.Total = R2(gravado + exonerado + inafecto + exportacion + igv);

            if (!string.IsNullOrWhiteSpace(cpe.DetraccionCodigo) && cpe.DetraccionPorcentaje is > 0)
            {
                // El depósito se calcula sobre el total en soles y se redondea al entero [VERIFICAR regla vigente de redondeo].
                var totalSoles = cpe.Moneda == "USD" && cpe.TipoCambio is > 0 ? cpe.Total * cpe.TipoCambio.Value : cpe.Total;
                cpe.DetraccionMonto = Math.Round(totalSoles * cpe.DetraccionPorcentaje.Value / 100m, 0, MidpointRounding.AwayFromZero);
            }
            else
            {
                cpe.DetraccionMonto = null;
            }
        }
    }
}
