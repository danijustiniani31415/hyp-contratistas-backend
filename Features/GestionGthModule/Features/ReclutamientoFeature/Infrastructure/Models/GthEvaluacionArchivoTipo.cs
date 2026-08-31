namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Documento que GTH puede adjuntar al informe de la entrevista de un candidato (tabla
    /// <c>gth_evaluacion_archivo_tipo</c>): hoy el informe final y los resultados de la evaluación
    /// de conocimientos. Es un catálogo y no dos juegos de columnas en
    /// <see cref="GthCandidatoEvaluacion"/> porque sumar un tercer documento tiene que ser un
    /// INSERT y no una migración de esquema.
    /// </summary>
    public class GthEvaluacionArchivoTipo
    {
        public int GthEvaluacionArchivoTipoId { get; set; }

        /// <summary>Clave estable usada en código (INFORME_FINAL, EVALUACION_CONOCIMIENTOS).</summary>
        public string Codigo { get; set; } = null!;

        /// <summary>Nombre visible; lo muestran la pantalla de GTH, la del solicitante y el correo.</summary>
        public string Nombre { get; set; } = null!;

        public int Orden { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
