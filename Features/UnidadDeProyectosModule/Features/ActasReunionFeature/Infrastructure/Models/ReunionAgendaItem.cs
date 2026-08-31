namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Tema a tratar propuesto por un participante para una ocurrencia concreta de reunión.
    /// Solo aplica cuando el tema de la reunión tiene agenda dinámica (ReunionTema.AgendaFija = false).
    /// </summary>
    public class ReunionAgendaItem
    {
        public int ReunionAgendaItemId { get; set; }
        public int ReunionId { get; set; }
        public int WorkerId { get; set; }
        public string Descripcion { get; set; } = null!;
        public int Orden { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
