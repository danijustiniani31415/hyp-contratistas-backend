namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Archivo del "Portafolio/Anexos" de un candidato de la long list (tabla
    /// <c>gth_candidato_anexo</c>): los N documentos que GTH adjunta además del CV (portafolio,
    /// certificados, cartas de recomendación…). El solicitante los abre desde "Revisar long list
    /// y CVs" y además le llegan adjuntos en el correo de la long list.
    ///
    /// Cuelga del candidato y no del requerimiento porque la long list se vuelve a cargar cuando
    /// el solicitante rechaza a todos: cada vuelta tiene sus propias filas de
    /// <see cref="GthCandidato"/> y sus anexos viajan con ellas.
    /// </summary>
    public class GthCandidatoAnexo
    {
        public int GthCandidatoAnexoId { get; set; }

        /// <summary>FK al candidato dueño del anexo.</summary>
        public int GthCandidatoId { get; set; }

        /// <summary>Nombre del archivo tal como quedó en SharePoint.</summary>
        public string Nombre { get; set; } = null!;

        /// <summary>
        /// Nombre con el que GTH lo subió. Es el que se muestra y el que viaja en el correo: en
        /// una lista de anexos el nombre es la única pista de qué es cada archivo, y el de
        /// SharePoint lleva el código del requerimiento y un timestamp.
        /// </summary>
        public string? NombreOriginal { get; set; }

        public string? Url { get; set; }
        public string? ItemId { get; set; }
        public string? DriveId { get; set; }

        /// <summary>Orden de carga dentro del candidato (1, 2, 3…).</summary>
        public int Orden { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
