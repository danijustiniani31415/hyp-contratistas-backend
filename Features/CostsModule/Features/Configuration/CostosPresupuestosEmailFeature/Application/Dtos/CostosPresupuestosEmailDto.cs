namespace Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Dtos
{
    public class CostosPresupuestosEmailDto
    {
        public int CostosPresupuestosEmailId { get; set; }
        public string Email { get; set; } = null!;
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
    }
}
