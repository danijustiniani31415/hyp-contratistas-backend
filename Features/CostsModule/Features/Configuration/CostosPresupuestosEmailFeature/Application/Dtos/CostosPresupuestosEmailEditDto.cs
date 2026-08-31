namespace Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Dtos
{
    public class CostosPresupuestosEmailEditDto
    {
        public int CostosPresupuestosEmailId { get; set; }
        public string Email { get; set; } = null!;
        public bool Active { get; set; }
    }
}
