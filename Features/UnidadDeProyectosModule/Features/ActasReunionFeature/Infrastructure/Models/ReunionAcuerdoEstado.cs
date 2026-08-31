namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>Catálogo de estados de un acuerdo: PENDIENTE, EN PROCESO, CUMPLIDO, ANULADO.</summary>
    public class ReunionAcuerdoEstado
    {
        public int ReunionAcuerdoEstadoId { get; set; }
        public string Descripcion { get; set; } = null!;

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
