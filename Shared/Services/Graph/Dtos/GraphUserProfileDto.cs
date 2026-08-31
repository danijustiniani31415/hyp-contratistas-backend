namespace Abril_Backend.Shared.Services.Graph.Dtos
{
    public class GraphUserProfileDto
    {
        public string Mail { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? JobTitle { get; set; }
        public string? Phone { get; set; }
    }
}
