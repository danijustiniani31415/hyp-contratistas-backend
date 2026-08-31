namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion
{
    /// <summary>Estado de los dos archivos que el médico debe completar antes de poder
    /// configurar su PIN de firma: la firma digital y el SSO-FO-149 ya escaneado.</summary>
    public class MedicoFirmaArchivosDto
    {
        public string? Email { get; set; }
        public string? FirmaDigitalUrl { get; set; }
        public string? UrlAutorizacionFirmada { get; set; }
    }
}
