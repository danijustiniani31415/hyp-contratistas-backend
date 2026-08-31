namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Auth
{
    public class ObreroTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "OBRERO";
        public List<string> AllowedFeatures { get; set; } = new();
    }
}
