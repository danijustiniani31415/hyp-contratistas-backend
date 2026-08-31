namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion
{
    public class ConvalidacionUpdateDto
    {
        public int? EmpresaDestinoId { get; set; }
        public DateOnly FechaConvalidacion { get; set; }
        public int? MedicoId { get; set; }
        public string Resultado { get; set; } = "Pendiente";
        public DateOnly? FechaVencimiento { get; set; }
        public string? UrlDocumento { get; set; }
        public string? Notas { get; set; }

        public string? PuestoOrigen { get; set; }
        public string? PuestoDestino { get; set; }
        public int? ObraOficinaStaffOrigenId { get; set; }
        public int? ObraOficinaStaffDestinoId { get; set; }

        public string? PinFirma { get; set; }
        public string? MicrosoftAccessToken { get; set; }
    }
}
