namespace Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Infrastructure.Models
{
    /// <summary>
    /// Tipo de carpeta de adjudicaciones: "07_OT/BACK UP PROYECTO" o "04_OBRAS".
    /// Etiqueta la raíz base a la que apunta el link configurado por proyecto.
    /// </summary>
    public class ProjectAdjudicacionFolderType
    {
        public int ProjectAdjudicacionFolderTypeId { get; set; }
        public string ProjectAdjudicacionFolderTypeDescription { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
