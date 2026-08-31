namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Dtos
{
    public class WorkItemCategoryClauseDto
    {
        public int    WorkItemCategoryClauseId { get; set; }
        public string ClauseText              { get; set; } = null!;
        public int    SortOrder               { get; set; }
        public int    ContractModalityId      { get; set; }
    }

    public class WorkItemCategoryAnexo3ClauseDto
    {
        public int    WorkItemCategoryAnexo3ClauseId { get; set; }
        public string ClauseText                     { get; set; } = null!;
        public int    SortOrder                      { get; set; }
    }

    public class WorkItemCategoryAnexo4ClauseDto
    {
        public int    WorkItemCategoryAnexo4ClauseId { get; set; }
        public string ClauseText                     { get; set; } = null!;
        public int    SortOrder                      { get; set; }
    }

    public class WorkItemCategoryDto
    {
        public int WorkItemCategoryId { get; set; }
        public string WorkItemCategoryDescription { get; set; } = null!;
        public int? WorkSpecialtyId { get; set; }
        public string? WorkSpecialtyDescription { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public string? InstructivosFolderId { get; set; }
        public string? InstructivosFolderName { get; set; }
        public int? InstructivosSyncStatus { get; set; }
        public DateTime? InstructivosSyncedAt { get; set; }
        public List<WorkItemCategoryClauseDto> Clauses { get; set; } = [];
        public List<WorkItemCategoryAnexo3ClauseDto> Anexo3Clauses { get; set; } = [];
        public List<WorkItemCategoryAnexo4ClauseDto> Anexo4Clauses { get; set; } = [];
    }

    /// <summary>Opción para el desplegable de especialidades en el formulario.</summary>
    public class WorkSpecialtyOptionDto
    {
        public int WorkSpecialtyId { get; set; }
        public string WorkSpecialtyDescription { get; set; } = null!;
    }

    public class WorkItemCategorySyncResultDto
    {
        public int Total { get; set; }
        public int Matched { get; set; }
        public int Unmatched { get; set; }
        public int Created { get; set; }
        public List<string> UnmatchedDescriptions { get; set; } = [];
        public List<string> CreatedDescriptions { get; set; } = [];
    }
}
