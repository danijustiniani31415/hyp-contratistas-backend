namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos
{
    public class ClinicaUpsertDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Ruc { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public bool Activo { get; set; } = true;
    }
}
