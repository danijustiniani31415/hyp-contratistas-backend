namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion
{
    public class ConvalidacionDetalleDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string WorkerNombre { get; set; } = string.Empty;
        public string WorkerDni { get; set; } = string.Empty;
        /// <summary>Nombre del puesto del trabajador (campo de presentación).</summary>
        public string? WorkerPuesto { get; set; }
        public string? TipoEmo { get; set; }
        public DateOnly? FechaEmoOrigen { get; set; }
        public string? AptitudOrigen { get; set; }
        public string? EmpresaOrigen { get; set; }
        public string? EmpresaDestino { get; set; }
        public string? MedicoNombre { get; set; }
        public string? MedicoEspecialidad { get; set; }
        public string? MedicoRegistroCmp { get; set; }
        public DateOnly FechaConvalidacion { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public string Resultado { get; set; } = string.Empty;
        public string? Notas { get; set; }

        // Cambio de puesto: datos y evaluación de riesgo, para el PDF.
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
