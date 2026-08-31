namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Historial de cambios de fecha de una reunión. Cada vez que la reunión se
    /// reprograma se inserta un registro con la fecha anterior y la nueva.
    /// </summary>
    public class ReunionReprogramacion
    {
        public int ReunionReprogramacionId { get; set; }
        public int ReunionId { get; set; }
        public DateOnly FechaAnterior { get; set; }
        public TimeOnly? HoraInicioAnterior { get; set; }
        public TimeOnly? HoraFinAnterior { get; set; }
        public DateOnly FechaNueva { get; set; }
        public TimeOnly? HoraInicioNueva { get; set; }
        public TimeOnly? HoraFinNueva { get; set; }
        public string? Motivo { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
