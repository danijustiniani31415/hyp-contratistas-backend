namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos
{
    public class MedicoOcupacionalUpsertDto
    {
        public string ApellidoNombre { get; set; } = string.Empty;
        public string? Dni { get; set; }
        public string? Cmp { get; set; }
        public string? Especialidad { get; set; }
        public int? ClinicaId { get; set; }
        public string? Email { get; set; }
        public string? Celular { get; set; }
        public bool Activo { get; set; } = true;
    }
}
