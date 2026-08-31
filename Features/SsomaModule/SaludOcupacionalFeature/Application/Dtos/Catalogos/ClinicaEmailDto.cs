namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos
{
    public class ClinicaEmailDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public bool Activo { get; set; }
    }

    public class ClinicaEmailCreateDto
    {
        public string Email { get; set; } = string.Empty;
        public string? Nombre { get; set; }
    }
}
