namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared
{
    /// <summary>
    /// Cuántos dígitos admite el documento de un candidato <b>FFT</b> según su tipo, y cómo se
    /// escribe para mostrarlo. Vive acá porque la regla la aplican tres sitios que no se pueden
    /// contradecir: el formulario de Solicitud de Personal (que además limita lo que se puede
    /// teclear), la validación del servicio y los correos que muestran el dato.
    ///
    /// El largo NO está en <c>gth_tipo_documento</c>: la tabla es el catálogo de los tipos, no de
    /// sus formatos, y agregarle columnas de validación la convertiría en un motor de reglas. Lo
    /// que sí es de la base es el <c>codigo</c>, que es por lo que se decide acá.
    /// </summary>
    internal static class FftDocumento
    {
        /// <summary>DNI: 8 dígitos exactos, como en el resto del sistema.</summary>
        public const string Dni = "DNI";

        /// <summary>Carné de extranjería: entre 8 y 12 dígitos.</summary>
        public const string Ce = "CE";

        /// <summary>
        /// Rango de dígitos válido para el tipo indicado. Un tipo que se agregue al catálogo sin
        /// regla propia entra por el caso amplio (8 a 12): es preferible aceptar un documento
        /// legítimo que bloquear un alta por una regla que nadie escribió.
        /// </summary>
        public static (int Min, int Max) Longitud(string? tipoCodigo) =>
            string.Equals(tipoCodigo?.Trim(), Dni, StringComparison.OrdinalIgnoreCase)
                ? (8, 8)
                : (8, 12);

        /// <summary>
        /// Deja solo los dígitos: los separadores se copian junto con el número ("12.345.678",
        /// "12 345 678") y eso es un dedazo de tipeo, no un documento mal declarado.
        /// </summary>
        public static string SoloDigitos(string? valor) =>
            new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>¿El número (ya sin separadores) cumple el largo de su tipo?</summary>
        public static bool EsValido(string? tipoCodigo, string digitos)
        {
            var (min, max) = Longitud(tipoCodigo);
            return digitos.Length >= min && digitos.Length <= max;
        }

        /// <summary>Qué se le dice al solicitante cuando el largo no cuadra.</summary>
        public static string ReglaTexto(string? tipoCodigo)
        {
            var (min, max) = Longitud(tipoCodigo);
            return min == max ? $"{min} dígitos" : $"entre {min} y {max} dígitos";
        }

        /// <summary>
        /// El documento como se muestra: «DNI 12345678». El tipo va pegado al número porque desde
        /// que hay dos, el número solo no dice cuál es. Null si no hay número que mostrar.
        /// </summary>
        public static string? Texto(string? tipoNombre, string? numero)
        {
            if (string.IsNullOrWhiteSpace(numero)) return null;
            return string.IsNullOrWhiteSpace(tipoNombre)
                ? numero.Trim()
                : $"{tipoNombre.Trim()} {numero.Trim()}";
        }
    }
}
