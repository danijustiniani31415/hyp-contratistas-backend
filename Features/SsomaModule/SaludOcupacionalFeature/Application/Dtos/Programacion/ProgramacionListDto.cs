namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion
{
    public class ProgramacionListDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? Empresa { get; set; }
        public string? Proyecto { get; set; }
        public int? TipoEmoId { get; set; }
        public string? TipoEmo { get; set; }
        public DateOnly FechaProgramada { get; set; }
        public TimeOnly? HoraProgramada { get; set; }
        public string? Clinica { get; set; }
        public string? Medico { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public int? EmoResultadoId { get; set; }
        public string Origen { get; set; } = "Manual";
        public TimeOnly? CheckInHora { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTimeOffset? FechaNotificacion { get; set; }
        /// <summary>Nombre del puesto (campo de presentación).</summary>
        public string? Puesto { get; set; }
        public DateOnly? FechaVencimientoEmo { get; set; }
        public string? Categoria { get; set; }
        public string? TipoTrabajador { get; set; }
        public string? InterconsultaEstado { get; set; }
        public bool TieneInterconsulta { get; set; }
    }
}
