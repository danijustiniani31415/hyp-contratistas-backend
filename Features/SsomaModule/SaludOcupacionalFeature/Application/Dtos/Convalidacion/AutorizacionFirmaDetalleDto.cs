namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion
{
    public class AutorizacionFirmaDetalleDto
    {
        public int MedicoId { get; set; }
        public string MedicoNombre { get; set; } = string.Empty;
        public string? MedicoDni { get; set; }
        public string? MedicoCmp { get; set; }
        public string? MedicoEspecialidad { get; set; }
        public string? FirmaDigitalUrl { get; set; }
        public string? Email { get; set; }
    }
}
