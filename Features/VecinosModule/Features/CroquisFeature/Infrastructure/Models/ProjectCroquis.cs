using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Models
{
    /// <summary>
    /// Croquis (imagen del plano de lotes) asignado a un proyecto. Hay como máximo
    /// un croquis activo por proyecto (índice único parcial sobre project_id con state = true).
    /// </summary>
    public class ProjectCroquis
    {
        public int ProjectCroquisId { get; set; }

        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        public string ImageUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
