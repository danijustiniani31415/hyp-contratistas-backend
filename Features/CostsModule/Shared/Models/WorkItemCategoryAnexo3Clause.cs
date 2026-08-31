namespace Abril_Backend.Features.CostsModule.Shared.Models
{
    public class WorkItemCategoryAnexo3Clause
    {
        public int    WorkItemCategoryAnexo3ClauseId { get; set; }
        public int    WorkItemCategoryId             { get; set; }
        public string ClauseText                     { get; set; } = null!;
        public int    SortOrder                      { get; set; }
        public bool   State                          { get; set; }

        public DateTimeOffset  CreatedDatetime  { get; set; }
        public int             CreatedUserId    { get; set; }
        public DateTimeOffset? UpdatedDatetime  { get; set; }
        public int?            UpdatedUserId    { get; set; }

        // Navegación
        public WorkItemCategory WorkItemCategory { get; set; } = null!;
    }
}
