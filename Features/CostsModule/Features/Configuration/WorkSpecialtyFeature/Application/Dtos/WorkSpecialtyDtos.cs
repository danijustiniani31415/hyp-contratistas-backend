namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Dtos
{
    public class WorkSpecialtyDto
    {
        public int WorkSpecialtyId { get; set; }
        public string WorkSpecialtyDescription { get; set; } = null!;
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
    }

    public class WorkSpecialtyCreateDto
    {
        public string WorkSpecialtyDescription { get; set; } = null!;
    }

    public class WorkSpecialtyEditDto
    {
        public int WorkSpecialtyId { get; set; }
        public string WorkSpecialtyDescription { get; set; } = null!;
        public bool Active { get; set; }
    }

    public class WorkSpecialtyFilterDto
    {
        public string? Description { get; set; }
        /// <summary>true: solo activas · false: solo inactivas · null: todas.</summary>
        public bool? Active { get; set; }
        public int Page { get; set; } = 1;
    }
}
