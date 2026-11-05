using System.Text.RegularExpressions;

namespace Abril_Backend.Features.GuiasRemisionModule.Application.Services
{
    /// <summary>Normalización y validación de datos que SUNAT exige en la guía de remisión.</summary>
    public static class GuiaValidaciones
    {
        /// <summary>RUC válido: 11 dígitos y dígito verificador correcto (módulo 11).</summary>
        public static bool RucValido(string? ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc) || !Regex.IsMatch(ruc, @"^\d{11}$")) return false;
            int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            var suma = 0;
            for (var i = 0; i < 10; i++) suma += (ruc[i] - '0') * pesos[i];
            var digito = 11 - (suma % 11);
            if (digito == 10) digito = 0;
            else if (digito == 11) digito = 1;
            return digito == ruc[10] - '0';
        }

        /// <summary>Mayúsculas, sin guiones ni espacios: así la pide SUNAT (ej. "aky-719" → "AKY719").</summary>
        public static string? NormalizarPlaca(string? placa) =>
            string.IsNullOrWhiteSpace(placa) ? null : Regex.Replace(placa.ToUpperInvariant(), @"[^A-Z0-9]", "");

        public static bool PlacaValida(string? placa) => placa != null && Regex.IsMatch(placa, @"^[A-Z0-9]{6,7}$");

        public static string? NormalizarLicencia(string? licencia) =>
            string.IsNullOrWhiteSpace(licencia) ? null : Regex.Replace(licencia.ToUpperInvariant(), @"[^A-Z0-9]", "");

        public static bool LicenciaValida(string? licencia) => licencia != null && Regex.IsMatch(licencia, @"^[A-Z0-9]{8,12}$");

        public static bool DniValido(string? dni) => dni != null && Regex.IsMatch(dni, @"^\d{8}$");
    }
}
