using Abril_Backend.Application.Exceptions;

namespace Abril_Backend.Shared.Helpers
{
    /// <summary>
    /// Validación de una firma dibujada en un canvas del frontend, que siempre llega como el
    /// <c>toDataURL('image/png')</c> del canvas.
    ///
    /// Vive en Shared porque la firma de una persona se registra desde dos módulos y la regla es la
    /// misma en los dos: Contabilidad (la firma del Gerente General, que se estampa en las facturas) y
    /// Gestión GTH (la firma del postulante, que se estampa en su carta oferta). Las dos terminan en
    /// las mismas columnas <c>person.signature_*</c>, así que si acá se aceptara algo que allá no, la
    /// misma ficha podría quedar con una firma que un módulo no sabe estampar.
    /// </summary>
    public static class FirmaImagenHelper
    {
        /// <summary>Tope de tamaño de la firma ya decodificada.</summary>
        public const int MaxBytes = 2 * 1024 * 1024; // 2 MB

        /// <summary>Cabecera de un PNG válido.</summary>
        private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>MIME con el que se guarda la firma en la ficha.</summary>
        public const string Mime = "image/png";

        /// <summary>
        /// Decodifica y valida la firma. Acepta tanto el data URL completo
        /// (<c>data:image/png;base64,XXXX</c>) como solo el base64. Lanza
        /// <see cref="AbrilException"/> con un mensaje para el usuario si no es un PNG usable.
        /// </summary>
        public static byte[] DecodePng(string? imageBase64)
        {
            if (string.IsNullOrWhiteSpace(imageBase64))
                throw new AbrilException("Debe dibujar una firma antes de guardar.");

            var raw = imageBase64.Trim();

            var commaIdx = raw.IndexOf(',');
            if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIdx >= 0)
                raw = raw[(commaIdx + 1)..];

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(raw);
            }
            catch (FormatException)
            {
                throw new AbrilException("La firma no tiene un formato de imagen válido.");
            }

            if (bytes.Length == 0)
                throw new AbrilException("La firma está vacía.");
            if (bytes.Length > MaxBytes)
                throw new AbrilException("La firma es demasiado grande (máximo 2 MB).");
            if (!bytes.Take(PngMagic.Length).SequenceEqual(PngMagic))
                throw new AbrilException("La firma debe ser una imagen PNG.");

            return bytes;
        }
    }
}
