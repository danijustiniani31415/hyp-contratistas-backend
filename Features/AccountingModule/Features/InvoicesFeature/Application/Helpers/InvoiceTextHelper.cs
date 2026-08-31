using System.Globalization;
using System.Text;

namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Helpers
{
    /// <summary>Utilidades de texto para el módulo de facturas (normalización de nombres).</summary>
    public static class InvoiceTextHelper
    {
        /// <summary>Quita tildes/diacríticos de un texto.</summary>
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Normaliza una razón social para comparaciones/lookups: sin tildes, sin espacios sobrantes,
        /// en mayúsculas. Ej. "Máximo  s.a.c" ≈ "MAXIMO S.A.C".
        /// </summary>
        public static string NormalizeName(string? text)
            => RemoveDiacritics((text ?? string.Empty).Trim()).ToUpper();
    }
}
