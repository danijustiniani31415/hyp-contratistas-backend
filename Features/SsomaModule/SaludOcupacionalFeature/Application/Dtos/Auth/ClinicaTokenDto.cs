namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Auth
{
    public class ClinicaTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public int ClinicaUsuarioId { get; set; }
        public int ClinicaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Tipo { get; set; } = "CLINICA";
    }
}
