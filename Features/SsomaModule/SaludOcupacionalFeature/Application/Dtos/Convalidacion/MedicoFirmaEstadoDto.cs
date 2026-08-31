namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion
{
    /// <summary>Estado interno de firma del médico — nunca se expone tal cual por la API.</summary>
    public class MedicoFirmaEstadoDto
    {
        public string? PinFirmaHash { get; set; }
        public string? Email { get; set; }
        public int PinFirmaIntentosFallidos { get; set; }
        public DateTimeOffset? PinFirmaBloqueadoHasta { get; set; }
    }
}
