namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Models
{
    /// <summary>Especialidad (cross-cutting de partidas). CRUD de catálogo.</summary>
    public class WorkSpecialty
    {
        public int WorkSpecialtyId { get; set; }
        public string WorkSpecialtyDescription { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
