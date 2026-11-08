namespace Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Sunat
{
    /// <summary>Monto en letras para la leyenda 1000 de SUNAT: «SON: MIL DOSCIENTOS TREINTA Y CUATRO CON 50/100 SOLES».</summary>
    public static class NumeroALetras
    {
        private static readonly string[] Unidades =
        {
            "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE",
            "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE", "VEINTE", "VEINTIUNO", "VEINTIDOS", "VEINTITRES",
            "VEINTICUATRO", "VEINTICINCO", "VEINTISEIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE",
        };
        private static readonly string[] Decenas = { "", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
        private static readonly string[] Centenas =
            { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

        public static string Monto(decimal monto, string moneda)
        {
            var entero = (long)Math.Floor(monto);
            var centavos = (int)Math.Round((monto - entero) * 100m, MidpointRounding.AwayFromZero);
            if (centavos == 100) { entero++; centavos = 0; }
            var nombre = moneda == "USD" ? "DOLARES AMERICANOS" : "SOLES";
            return $"SON: {Entero(entero)} CON {centavos:00}/100 {nombre}";
        }

        public static string Entero(long n)
        {
            if (n == 0) return "CERO";
            if (n < 0) return "MENOS " + Entero(-n);
            var partes = new List<string>();
            var millones = n / 1_000_000;
            var resto = n % 1_000_000;
            if (millones > 0)
                partes.Add(millones == 1 ? "UN MILLON" : Entero(millones) + " MILLONES");
            var miles = resto / 1000;
            var cientos = resto % 1000;
            if (miles > 0)
                partes.Add(miles == 1 ? "MIL" : Menor1000(miles, apocopar: true) + " MIL");
            if (cientos > 0) partes.Add(Menor1000(cientos, apocopar: false));
            return string.Join(" ", partes);
        }

        /// <summary>1..999. «apocopar» convierte UNO en UN (veintiún mil, treinta y un mil).</summary>
        private static string Menor1000(long n, bool apocopar)
        {
            if (n == 100) return "CIEN";
            var c = (int)(n / 100);
            var d = (int)(n % 100);
            var texto = c > 0 ? Centenas[c] : "";
            if (d > 0)
            {
                if (texto.Length > 0) texto += " ";
                if (d < 30) texto += Unidades[d];
                else
                {
                    texto += Decenas[d / 10];
                    if (d % 10 > 0) texto += " Y " + Unidades[d % 10];
                }
            }
            if (apocopar)
            {
                if (texto.EndsWith("VEINTIUNO")) texto = texto[..^1];
                else if (texto.EndsWith(" UNO") || texto == "UNO") texto = texto[..^1];
            }
            return texto;
        }
    }
}
