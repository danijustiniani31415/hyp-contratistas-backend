namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Dtos
{
    /// <summary>
    /// Raíz de la carpeta de adjudicaciones de un proyecto (ubicación estable resuelta en
    /// la configuración de "Carpeta Adj."). Punto de partida del recorrido del sincronizador.
    /// </summary>
    public class AdjudicacionFolderRootDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
        public string DriveId { get; set; } = null!;
        public string FolderId { get; set; } = null!;
    }

    /// <summary>Partida activa ya registrada (para dedup en la sincronización).</summary>
    public class ExistingWorkItemDto
    {
        public int WorkItemId { get; set; }
        public string WorkItemDescription { get; set; } = null!;
    }

    /// <summary>Resultado de la sincronización de partidas desde las carpetas de OneDrive.</summary>
    public class WorkItemSyncResultDto
    {
        /// <summary>Proyectos con carpeta de adjudicaciones configurada que se recorrieron.</summary>
        public int ProjectsScanned { get; set; }
        /// <summary>Partidas nuevas insertadas en la BD.</summary>
        public int Created { get; set; }
        /// <summary>Carpetas de partida que ya existían como partida (se omitieron en el alta).</summary>
        public int Existing { get; set; }
        public List<string> CreatedDescriptions { get; set; } = new();
        /// <summary>Proyectos en los que no se encontró una carpeta de "Contratos".</summary>
        public List<string> ProjectsWithoutContratosFolder { get; set; } = new();
    }
}
