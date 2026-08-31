namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application
{
    /// <summary>
    /// Códigos estables de los documentos que GTH puede adjuntar al informe de la entrevista
    /// (espejo de <c>gth_evaluacion_archivo_tipo.codigo</c>). Los dos son opcionales: el informe
    /// se puede enviar sin ninguno, con uno o con los dos.
    ///
    /// Viven en Application y no dentro del repositorio porque el servicio también los necesita:
    /// es el que enlaza cada archivo del multipart con su tipo y el que los adjunta al correo.
    /// </summary>
    public static class EvaluacionArchivoCodigo
    {
        /// <summary>Informe final del candidato.</summary>
        public const string InformeFinal = "INFORME_FINAL";

        /// <summary>Resultados de la evaluación de conocimientos.</summary>
        public const string EvaluacionConocimientos = "EVALUACION_CONOCIMIENTOS";

        /// <summary>
        /// Clave del form file en el multipart de la evaluación → código del tipo. El frontend
        /// manda cada archivo con su clave; acá se decide de qué documento se trata, para que el
        /// cliente no pueda inventarse un tipo que no existe en el catálogo.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> PorClaveDeFormulario =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["informeFinal"]            = InformeFinal,
                ["evaluacionConocimientos"] = EvaluacionConocimientos,
            };
    }
}
