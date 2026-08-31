namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Archivo del informe de la entrevista de un candidato (tabla
    /// <c>gth_candidato_evaluacion_archivo</c>): el informe final y los resultados de la evaluación
    /// de conocimientos que GTH sube al enviarlo como finalista. Los dos son opcionales.
    ///
    /// Cuelga de la evaluación y no del candidato porque es parte del informe: si la evaluación se
    /// rehace, sus archivos van con ella. Hay como máximo uno vivo por evaluación y tipo — volver a
    /// subir el mismo documento da de baja al anterior (<see cref="State"/> en false, nunca se
    /// borra) y guarda el nuevo.
    ///
    /// Mismas columnas de SharePoint que <see cref="GthCandidatoAnexo"/>: el archivo se sube a la
    /// carpeta del requerimiento y acá queda el enlace con el que lo abren GTH y el solicitante.
    /// </summary>
    public class GthCandidatoEvaluacionArchivo
    {
        public int GthCandidatoEvaluacionArchivoId { get; set; }

        /// <summary>FK a la evaluación dueña del archivo.</summary>
        public int GthCandidatoEvaluacionId { get; set; }

        /// <summary>FK a <c>gth_evaluacion_archivo_tipo</c>: qué documento es.</summary>
        public int GthEvaluacionArchivoTipoId { get; set; }

        /// <summary>Nombre del archivo tal como quedó en SharePoint.</summary>
        public string Nombre { get; set; } = null!;

        /// <summary>
        /// Nombre con el que GTH lo subió. Es el que se muestra y el que viaja en el correo: el de
        /// SharePoint lleva el código del requerimiento y un timestamp.
        /// </summary>
        public string? NombreOriginal { get; set; }

        public string? Url { get; set; }
        public string? ItemId { get; set; }
        public string? DriveId { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
