namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Trabajador excluido manualmente de una regla de convocatoria "Staff de un proyecto": la
    /// regla sigue siendo dinámica (si mañana entra personal nuevo a la obra, se convoca solo),
    /// pero a estos workers puntuales no se les convoca aunque sigan con vinculación vigente al
    /// proyecto. No aplica a reglas de área/puesto — ahí no hay lista de personas que excluir.
    /// </summary>
    public class ReunionTemaReglaWorkerExcluido
    {
        public int ReunionTemaReglaWorkerExcluidoId { get; set; }
        public int ReunionTemaReglaId { get; set; }
        public int WorkerId { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
