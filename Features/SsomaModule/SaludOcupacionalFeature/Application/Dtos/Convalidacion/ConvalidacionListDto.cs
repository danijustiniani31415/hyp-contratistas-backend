namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion
{
    public class ConvalidacionListDto
    {
        public int Id { get; set; }
        public int EmoOrigenId { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? EmpresaOrigen { get; set; }
        public int? EmpresaDestinoId { get; set; }
        public string? EmpresaDestino { get; set; }
        public string? Proyecto { get; set; }
        public string? TipoEmo { get; set; }
        public string? Medico { get; set; }
        public DateOnly? FechaEmoOrigen { get; set; }
        public DateOnly FechaConvalidacion { get; set; }
        public string Resultado { get; set; } = string.Empty;
        public DateOnly? FechaVencimiento { get; set; }
        public int? DiasParaVencer { get; set; }
        public string? Notas { get; set; }
        public string? UrlDocumento { get; set; }

        // Datos del EMO origen, para que el médico pueda revisarlos antes de resolver.
        public DateOnly? EmoFechaVencimiento { get; set; }
        public string? UrlResultado { get; set; }
        public string? UrlAptitud { get; set; }
        public string? UrlEmoCompleto { get; set; }
        public string? InterconsultaEstado { get; set; }
        public string? InterconsultaEspecialidad { get; set; }
        public string? InterconsultaUrlInforme { get; set; }

        // Cambio de puesto: datos y evaluación de riesgo (ver ObraOficinaStaffIds.RiesgoEmo).
        public string? PuestoOrigen { get; set; }
        public string? PuestoDestino { get; set; }
        public string? CategoriaOrigen { get; set; }
        public string? CategoriaDestino { get; set; }
        public int? ObraOficinaStaffOrigenId { get; set; }
        public string? ObraOficinaStaffOrigenNombre { get; set; }
        public int? ObraOficinaStaffDestinoId { get; set; }
        public string? ObraOficinaStaffDestinoNombre { get; set; }
        public string? RiesgoOrigen { get; set; }
        public string? RiesgoDestino { get; set; }
        public bool CambioRiesgo { get; set; }
    }
}
