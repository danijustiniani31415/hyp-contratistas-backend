using Abril_Backend.Features.ComprasModule.Application.Dtos;

namespace Abril_Backend.Features.ComprasModule.Application.Services
{
    /// <summary>
    /// Revisa lo que leyó la IA con reglas que no se equivocan: aritmética, RUC, cliente, tipo de documento.
    /// Un aviso no bloquea el guardado — la persona decide —, pero obliga a mirar justo lo dudoso.
    /// </summary>
    public static class ComprobanteValidador
    {
        public const string RucEmpresa = "20608374087";
        private const decimal UmbralDetraccionServicios = 700m;

        /// <summary>Dígito verificador del RUC peruano (módulo 11).</summary>
        public static bool RucValido(string? ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 11 || !ruc.All(char.IsDigit)) return false;
            int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            var suma = 0;
            for (var i = 0; i < 10; i++) suma += (ruc[i] - '0') * pesos[i];
            var resto = 11 - (suma % 11);
            var digito = resto == 10 ? 0 : resto == 11 ? 1 : resto;
            return digito == ruc[10] - '0';
        }

        private static int DigitosDistintos(string a, string b) =>
            a.Length != b.Length ? int.MaxValue : a.Zip(b).Count(p => p.First != p.Second);

        /// <summary>Errores de lectura probables (la IA confundió un número) → conviene reintentar con el modelo más preciso.</summary>
        public static bool NecesitaSegundaLectura(ComprobanteLeidoDto l, List<ComprobanteAdvertenciaDto> avisos) =>
            l.Confianza < 0.85
            || avisos.Any(a => a.Nivel == "ERROR" && (a.Mensaje.StartsWith("El RUC del emisor") || a.Mensaje.StartsWith("La fecha de emisión") || a.Mensaje.StartsWith("El comprobante está a nombre")))
            || avisos.Any(a => a.Nivel == "ERROR" && (a.Mensaje.StartsWith("Los totales") || a.Mensaje.StartsWith("La suma de las líneas")));

        public static List<ComprobanteAdvertenciaDto> Validar(ComprobanteLeidoDto l)
        {
            var av = new List<ComprobanteAdvertenciaDto>();
            void Aviso(string n, string m) => av.Add(new ComprobanteAdvertenciaDto { Nivel = n, Mensaje = m });

            if (l.Items.Count == 0) Aviso("ERROR", "No se pudo leer ninguna línea del documento.");
            if (string.IsNullOrWhiteSpace(l.Serie) || string.IsNullOrWhiteSpace(l.Numero))
                Aviso("ERROR", "No se leyó la serie o el número del comprobante.");
            if (string.IsNullOrWhiteSpace(l.FechaEmision) || !DateOnly.TryParse(l.FechaEmision, out _))
                Aviso("ERROR", "No se leyó la fecha de emisión.");

            if (DateOnly.TryParse(l.FechaEmision, out var fecha))
            {
                var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)); // hora de Perú
                if (fecha > hoy)
                    Aviso("ERROR", $"La fecha de emisión ({fecha:dd/MM/yyyy}) está en el futuro: probablemente se leyó mal el año o el día.");
                else if (fecha < hoy.AddDays(-90))
                    Aviso("AVISO", $"La fecha de emisión ({fecha:dd/MM/yyyy}) tiene más de 3 meses: confírmala en el documento (el crédito fiscal del IGV se pierde a los 12 meses).");
            }

            if (l.Tipo is "FACTURA" or "BOLETA" or "NOTA_CREDITO" or "NOTA_DEBITO" && !RucValido(l.EmisorRuc))
                Aviso("ERROR", $"El RUC del emisor ({l.EmisorRuc ?? "no leído"}) no es válido: revísalo en el documento.");

            if (l.Tipo == "BOLETA")
                Aviso("AVISO", "Es una boleta: no da crédito fiscal (no se puede deducir el IGV). Si el proveedor puede, pídele factura.");
            else if (l.Tipo is "FACTURA" or "NOTA_CREDITO" or "NOTA_DEBITO"
                     && !string.IsNullOrWhiteSpace(l.ClienteRuc) && l.ClienteRuc != RucEmpresa
                     && !RucValido(l.ClienteRuc) && DigitosDistintos(l.ClienteRuc, RucEmpresa) <= 2)
                // Un RUC con dígito verificador inválido y casi igual al de la empresa es una lectura borrosa, no otro cliente.
                Aviso("INFO", $"Se leyó el RUC del cliente como {l.ClienteRuc}, pero es casi el de H&P Constructores ({RucEmpresa}): se asume que es la empresa. Verifícalo en el documento.");
            else if (l.Tipo is "FACTURA" or "NOTA_CREDITO" or "NOTA_DEBITO"
                     && !string.IsNullOrWhiteSpace(l.ClienteRuc) && l.ClienteRuc != RucEmpresa)
                Aviso("ERROR", $"El comprobante está a nombre del RUC {l.ClienteRuc}, no de H&P Constructores ({RucEmpresa}).");

            // Aritmética: base + IGV = total, y las líneas suman la base (o el total si no hay desglose).
            if (l.Total > 0 && l.Subtotal > 0 && Math.Abs(l.Subtotal + l.Igv - l.Total) > 0.06m)
                Aviso("ERROR", $"Los totales no cuadran: {l.Subtotal:0.00} + IGV {l.Igv:0.00} ≠ {l.Total:0.00}. Alguna cifra se leyó mal.");

            if (l.Items.Count > 0)
            {
                var sumaLineas = l.Items.Sum(i => i.Total);
                var referencia = l.Subtotal > 0 ? l.Subtotal : l.Total;
                if (referencia > 0 && Math.Abs(sumaLineas - referencia) > Math.Max(1m, referencia * 0.005m)
                    && Math.Abs(sumaLineas - l.Total) > Math.Max(1m, l.Total * 0.005m))
                    Aviso("ERROR", $"La suma de las líneas ({sumaLineas:0.00}) no coincide con el comprobante ({referencia:0.00}). Revisa cantidades y precios.");

                foreach (var (it, i) in l.Items.Select((x, i) => (x, i + 1)))
                {
                    if (it.Cantidad <= 0) Aviso("ERROR", $"Línea {i}: cantidad ilegible o cero.");
                    else if (it.PrecioUnitario > 0 && it.Total > 0
                             && Math.Abs(it.Cantidad * it.PrecioUnitario - it.Total) > Math.Max(0.5m, it.Total * 0.06m))
                        // 6 %: el valor unitario puede venir sin IGV y el total con él.
                        Aviso("AVISO", $"Línea {i}: cantidad × precio no coincide con el total de la línea.");
                }
            }

            if (l.DetraccionMonto is > 0)
                Aviso("INFO", $"Tiene detracción de S/ {l.DetraccionMonto:0.00}: hay que depositarla al Banco de la Nación para no perder el crédito fiscal.");
            else if (l.Tipo == "FACTURA" && l.Total > UmbralDetraccionServicios && l.Items.Any(i => i.Categoria == "SERVICIO"))
                Aviso("INFO", "Es un servicio sobre S/ 700: revisa si le corresponde detracción.");

            if (l.Moneda == "USD")
                Aviso("INFO", "El comprobante está en dólares: indica el tipo de cambio para registrarlo en soles.");

            if (l.Tipo is "NOTA_CREDITO" or "NOTA_DEBITO")
                Aviso("AVISO", "Es una nota de crédito/débito: se registra el documento pero no mueve el stock.");

            if (l.Tipo == "GUIA_REMISION")
                Aviso("AVISO", "Es una guía de remisión, no un comprobante de pago: sirve para cruzar con la factura.");

            if (l.Confianza < 0.85)
                Aviso("AVISO", "La IA no está segura de haber leído todo bien: revisa cada línea con calma.");

            return av;
        }
    }
}
