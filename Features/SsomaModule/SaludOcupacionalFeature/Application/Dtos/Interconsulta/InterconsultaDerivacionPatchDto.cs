namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta
{
    public class InterconsultaDerivacionPatchDto
    {
        public string Especialidad { get; set; } = string.Empty;
        public string? Diagnostico { get; set; }
    }
}
