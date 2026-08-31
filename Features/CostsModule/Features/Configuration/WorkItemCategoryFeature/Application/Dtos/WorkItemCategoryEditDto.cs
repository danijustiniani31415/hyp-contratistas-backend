namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Dtos
{
    public class WorkItemCategoryClauseUpsertDto
    {
        public int?   WorkItemCategoryClauseId { get; set; }
        public string ClauseText              { get; set; } = null!;
        public int    SortOrder               { get; set; }
        public int    ContractModalityId      { get; set; }
    }

    public class WorkItemCategoryAnexo3ClauseUpsertDto
    {
        public int?   WorkItemCategoryAnexo3ClauseId { get; set; }
        public string ClauseText                     { get; set; } = null!;
        public int    SortOrder                      { get; set; }
    }

    public class WorkItemCategoryAnexo4ClauseUpsertDto
    {
        public int?   WorkItemCategoryAnexo4ClauseId { get; set; }
        public string ClauseText                     { get; set; } = null!;
        public int    SortOrder                      { get; set; }
    }

    public class WorkItemCategoryEditDto
    {
        public int WorkItemCategoryId { get; set; }
        public string WorkItemCategoryDescription { get; set; } = null!;
        /// <summary>Especialidad a la que pertenece la partida de control (requerido).</summary>
        public int? WorkSpecialtyId { get; set; }
        public bool Active { get; set; }
        public List<WorkItemCategoryClauseUpsertDto> Clauses { get; set; } = [];
        public List<WorkItemCategoryAnexo3ClauseUpsertDto> Anexo3Clauses { get; set; } = [];
        public List<WorkItemCategoryAnexo4ClauseUpsertDto> Anexo4Clauses { get; set; } = [];
    }
}
