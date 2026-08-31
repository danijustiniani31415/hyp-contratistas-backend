using System.Text.RegularExpressions;

namespace Abril_Backend.Shared.Helpers
{
    public static class DocumentoHelper
    {
        // DNI: exactamente 8 dígitos numéricos
        private static readonly Regex RegexDni = new(@"^\d{8}$", RegexOptions.Compiled);

        // CE: alfanumérico sin espacios, entre 6 y 12 caracteres
        private static readonly Regex RegexCe = new(@"^[A-Za-z0-9]{6,12}$", RegexOptions.Compiled);

        /// <summary>
        /// Valida formato de DNI o CE según tipoDocumento.
        /// Si tipoDocumento es null, infiere por formato.
        /// </summary>
        public static bool EsFormatoValido(string documento, string? tipoDocumento = null)
        {
            if (string.IsNullOrWhiteSpace(documento)) return false;
            var doc = documento.Trim();

            if (tipoDocumento?.ToUpper() == "DNI") return RegexDni.IsMatch(doc);
            if (tipoDocumento?.ToUpper() == "CE")  return RegexCe.IsMatch(doc);

            return RegexDni.IsMatch(doc) || RegexCe.IsMatch(doc);
        }

        /// <summary>
        /// Infiere el tipo de documento por formato.
        /// </summary>
        public static string InferirTipo(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento)) return "DNI";
            return RegexDni.IsMatch(documento.Trim()) ? "DNI" : "CE";
        }
    }
}
