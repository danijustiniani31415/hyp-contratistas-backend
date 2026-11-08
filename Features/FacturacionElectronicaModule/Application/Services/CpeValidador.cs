using System.Text.Json;
using Abril_Backend.Features.ComprasModule.Application.Services;
using Abril_Backend.Features.FacturacionElectronicaModule.Application.Dtos;
using Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Models;

namespace Abril_Backend.Features.FacturacionElectronicaModule.Application.Services
{
    /// <summary>
    /// Reglas que se revisan antes de gastar un correlativo y de molestar a SUNAT. ERROR bloquea la emisión;
    /// AVISO solo se muestra. Los plazos y topes marcados [VERIFICAR] hay que confirmarlos con la norma vigente.
    /// </summary>
    public static class CpeValidador
    {
        public static readonly string[] MotivosNotaCredito = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "13" };
        public static readonly string[] MotivosNotaDebito = { "01", "02", "03", "10" };
        private static readonly string[] AfectacionesValidas = { "10", "20", "30", "40" };
        private const decimal UmbralDetraccion = 700m;

        public static List<CpeAvisoDto> Validar(Cpe c, DateOnly hoy)
        {
            var av = new List<CpeAvisoDto>();
            void E(string m) => av.Add(new CpeAvisoDto { Nivel = "ERROR", Mensaje = m });
            void A(string m) => av.Add(new CpeAvisoDto { Nivel = "AVISO", Mensaje = m });

            if (c.TipoDocumento is not ("01" or "03" or "07" or "08")) E("Tipo de documento no soportado (factura, boleta, nota de crédito y nota de débito).");
            if (c.Moneda is not ("PEN" or "USD")) E("La moneda debe ser PEN o USD.");
            if (c.Moneda == "USD" && c.TipoCambio is not > 0) E("Indica el tipo de cambio para facturar en dólares.");

            // Cliente
            if (string.IsNullOrWhiteSpace(c.ClienteRazon)) E("Falta la razón social o el nombre del cliente.");
            var esExportacion = c.TipoOperacion == "0200";
            if (c.TipoDocumento == "01" && !esExportacion && c.ClienteTipoDoc != "6") E("La factura se emite solo a clientes con RUC.");
            if (esExportacion)
            {
                if (c.ClienteTipoDoc == "6") E("En una exportación el cliente no se identifica con RUC: usa documento del extranjero (4 carnet de extranjería, 7 pasaporte, 0 sin documento).");
                if (string.IsNullOrWhiteSpace(c.ClientePais) || c.ClientePais!.Length != 2) E("Indica el país del cliente (2 letras, por ejemplo CL, US).");
            }
            if (c.ClienteTipoDoc == "6" && !ComprobanteValidador.RucValido(c.ClienteNumeroDoc)) E($"El RUC del cliente ({c.ClienteNumeroDoc}) no es válido.");
            if (c.ClienteTipoDoc == "1" && (c.ClienteNumeroDoc.Length != 8 || !c.ClienteNumeroDoc.All(char.IsDigit))) E("El DNI del cliente debe tener 8 dígitos.");
            if (c.TipoDocumento == "03" && c.ClienteTipoDoc == "0" && c.Total > 700m)
                E("Una boleta mayor a S/ 700 debe identificar al cliente con su DNI u otro documento [VERIFICAR monto vigente].");
            if (c.TipoDocumento == "03" && c.Moneda == "USD") E("Las boletas se emiten en soles.");
            if (c.ClienteTipoDoc == "6" && c.ClienteNumeroDoc == ComprobanteValidador.RucEmpresa) E("No puedes emitir un comprobante a tu propio RUC.");

            // Fecha
            if (c.FechaEmision > hoy) E("La fecha de emisión no puede estar en el futuro.");
            else if (c.FechaEmision < hoy.AddDays(-3)) A("La fecha de emisión tiene más de 3 días: SUNAT puede rechazarla por extemporánea [VERIFICAR plazo vigente].");
            if (c.FechaVencimiento.HasValue && c.FechaVencimiento < c.FechaEmision) E("El vencimiento no puede ser anterior a la emisión.");

            // Líneas
            if (c.Items.Count == 0) E("Agrega al menos una línea.");
            foreach (var (it, i) in c.Items.Select((x, i) => (x, i + 1)))
            {
                if (string.IsNullOrWhiteSpace(it.Descripcion)) E($"Línea {i}: falta la descripción.");
                if (it.Cantidad <= 0) E($"Línea {i}: la cantidad debe ser mayor que cero.");
                if (it.PrecioUnitario < 0) E($"Línea {i}: el precio no puede ser negativo.");
                if (it.UnidadMedida.Length is < 1 or > 3) E($"Línea {i}: la unidad de medida debe ser un código SUNAT (NIU, ZZ, KGM…).");
                if (!AfectacionesValidas.Contains(it.AfectacionIgv)) E($"Línea {i}: afectación de IGV inválida.");
                if (it.Descuento < 0 || it.Descuento > it.Cantidad * it.ValorUnitario + 0.01m) E($"Línea {i}: el descuento no puede ser mayor que el valor de la línea.");
            }
            if (c.TipoOperacion == "0200" && c.Items.Any(i => i.AfectacionIgv != "40")) E("En una exportación todas las líneas deben ser de afectación 40 (exportación).");
            if (c.TipoOperacion != "0200" && c.Items.Any(i => i.AfectacionIgv == "40")) E("La afectación 40 (exportación) solo va con el tipo de operación 0200.");
            if (c.Items.Count > 0 && c.Total <= 0) E("El total debe ser mayor que cero.");

            // Notas
            if (c.TipoDocumento is "07" or "08")
            {
                if (c.RefCpeId == null && c.RefNumero == null) E("La nota debe indicar el comprobante que modifica.");
                var validos = c.TipoDocumento == "07" ? MotivosNotaCredito : MotivosNotaDebito;
                if (string.IsNullOrWhiteSpace(c.MotivoCodigo) || !validos.Contains(c.MotivoCodigo)) E("Elige un motivo válido para la nota.");
                if (string.IsNullOrWhiteSpace(c.MotivoDescripcion)) E("Describe el motivo de la nota.");
            }

            // Forma de pago
            if (c.FormaPago == "CREDITO")
            {
                var cuotas = LeerCuotas(c.CuotasJson);
                if (cuotas.Count == 0) E("Una venta al crédito necesita al menos una cuota.");
                else
                {
                    var porPagar = c.Total - (c.Moneda == "PEN" ? c.DetraccionMonto ?? 0 : 0);
                    if (Math.Abs(cuotas.Sum(q => q.Monto) - porPagar) > 0.01m)
                        E($"Las cuotas ({cuotas.Sum(q => q.Monto):0.00}) deben sumar el monto por pagar ({porPagar:0.00}) [VERIFICAR si SUNAT lo exige neto de detracción].");
                    if (cuotas.Any(q => q.Fecha < c.FechaEmision)) E("Una cuota no puede vencer antes de la emisión.");
                }
            }

            // Detracción
            if (!string.IsNullOrWhiteSpace(c.DetraccionCodigo))
            {
                if (c.DetraccionPorcentaje is not > 0) E("Indica el porcentaje de la detracción.");
                if (string.IsNullOrWhiteSpace(c.DetraccionCuenta)) E("Falta la cuenta de detracciones de H&P en el Banco de la Nación.");
            }
            else if (c.TipoDocumento == "01")
            {
                var soles = c.Moneda == "USD" && c.TipoCambio is > 0 ? c.Total * c.TipoCambio.Value : c.Total;
                if (soles > UmbralDetraccion)
                    A("El total supera S/ 700: revisa si la operación está sujeta a detracción [VERIFICAR anexos vigentes].");
            }
            return av;
        }

        public static List<CpeCuotaDto> LeerCuotas(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<CpeCuotaDto>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new(); }
            catch (JsonException) { return new(); }
        }
    }
}
