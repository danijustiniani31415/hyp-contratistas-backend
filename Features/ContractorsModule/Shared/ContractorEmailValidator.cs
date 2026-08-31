using System.Text.RegularExpressions;
using Abril_Backend.Application.Exceptions;

namespace Abril_Backend.Features.ContractorsModule.Shared
{
    /// <summary>
    /// Validación de correos compartida por las features del módulo de contratistas
    /// (registro y gestión).
    ///
    /// Regla: el correo solo puede contener letras y números, además del '@', '.', '_' y '-'.
    /// No se permiten otros símbolos como ' &lt; &gt; + etc., no puede empezar ni terminar con un
    /// símbolo (debe empezar y terminar con una letra o número) ni tener símbolos consecutivos.
    /// Debe tener exactamente un '@' y un dominio con al menos un punto.
    /// </summary>
    public static partial class ContractorEmailValidator
    {
        [GeneratedRegex(@"^[A-Za-z0-9]+([._-][A-Za-z0-9]+)*@[A-Za-z0-9]+(-[A-Za-z0-9]+)*(\.[A-Za-z0-9]+(-[A-Za-z0-9]+)*)+$")]
        private static partial Regex EmailRegex();

        /// <summary>Devuelve true si el correo cumple con la regla (letras, números, '@', '.', '_' y '-').</summary>
        public static bool IsValid(string? email)
        {
            return !string.IsNullOrWhiteSpace(email) && EmailRegex().IsMatch(email.Trim());
        }

        /// <summary>
        /// Valida cada correo de la lista. Lanza <see cref="AbrilException"/> (400) con un
        /// mensaje en español si alguno no cumple el formato.
        /// </summary>
        public static void ValidateOrThrow(IEnumerable<string?> emails)
        {
            foreach (var email in emails)
            {
                var trimmed = email?.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                if (!IsValid(trimmed))
                    throw new AbrilException(
                        $"El correo \"{trimmed}\" no es válido. Solo puede contener letras, números, \"@\", \".\", \"_\" y \"-\", y no puede empezar ni terminar con un símbolo.",
                        400);
            }
        }
    }
}
