namespace Abril_Backend.Features.AuthModule.Role.Application.Dtos
{
    public class FeatureDto
    {
        public int FeatureId { get; set; }
        public string FeatureKey { get; set; } = null!;
        public int? ModuleId { get; set; }
        public string? ModuleName { get; set; }
    }
}
