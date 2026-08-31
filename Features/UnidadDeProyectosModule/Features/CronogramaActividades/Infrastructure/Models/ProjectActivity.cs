namespace Abril_Backend.Shared.Models
{
    public class ProjectActivity
    {
        public int ProjectActivityId { get; set; }
        public int ProjectId { get; set; }
        public string ActivityDescription { get; set; } = string.Empty;
        public DateOnly? PlannedStartDate { get; set; }
        public DateOnly? PlannedEndDate { get; set; }
        public DateOnly? ActualEndDate { get; set; }
        public DateOnly? BaselineStartDate { get; set; }
        public DateOnly? BaselineEndDate { get; set; }
        public int ProgressPercentage { get; set; }
        public int Order { get; set; }
        public int? ParentId { get; set; }
        public int HierarchyLevel { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool IsManual { get; set; } = false;
        public string TipoCronograma { get; set; } = "ANTEPROYECTO";
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
