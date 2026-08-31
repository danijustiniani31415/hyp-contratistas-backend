namespace Abril_Backend.Features.CostsModule.Shared.Models
{
    public class WorkItemCategory
    {
        public int WorkItemCategoryId { get; set; }
        public string WorkItemCategoryDescription { get; set; } = null!;

        // Especialidad a la que pertenece la partida de control (FK a work_specialty).
        public int? WorkSpecialtyId { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }

        // Sync con carpeta de Instructivos en OneDrive
        public string? InstructivosFolderId { get; set; }
        public string? InstructivosFolderName { get; set; }
        // 1 = automático, 2 = manual, 3 = sin instructivo
        public int? InstructivosSyncStatus { get; set; }
        public DateTimeOffset? InstructivosSyncedAt { get; set; }
    }
}
