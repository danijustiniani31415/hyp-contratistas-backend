namespace Abril_Backend.Features.CostsModule.Shared.Models
{
    public class WorkItemCategoryClause
    {
        public int    WorkItemCategoryClauseId { get; set; }
        public int    WorkItemCategoryId       { get; set; }
        public string ClauseText              { get; set; } = null!;
        public int    SortOrder               { get; set; }
        public bool   State                   { get; set; }

        // Modalidad de contrato a la que pertenece la cláusula (FK a contract_modality):
        // 1 = Suministro e Instalación, 2 = Suministro, 3 = Instalación.
        // Cada cláusula es independiente por modalidad (editarla no afecta a otras modalidades).
        public int    ContractModalityId      { get; set; }

        public DateTimeOffset  CreatedDatetime  { get; set; }
        public int             CreatedUserId    { get; set; }
        public DateTimeOffset? UpdatedDatetime  { get; set; }
        public int?            UpdatedUserId    { get; set; }

        // Navegación
        public WorkItemCategory WorkItemCategory { get; set; } = null!;
    }
}
