namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Puesto asociado a la convocatoria recurrente de un tema (ej. el tema "Reunión de Jefaturas
    /// de Proyectos" tiene el puesto "Jefe de Proyectos" asociado). Se combina con
    /// ReunionTema.AreaScopeId para resolver los participantes sugeridos al elegir el tema.
    /// </summary>
    public class ReunionTemaPuesto
    {
        public int ReunionTemaPuestoId { get; set; }
        /// <summary>Legado: el tema al que pertenece, redundante con ReunionTemaRegla.ReunionTemaId
        /// pero se conserva para no romper limpiezas existentes (ej. EliminarTema).</summary>
        public int ReunionTemaId { get; set; }
        /// <summary>Regla (área/gerencia y/o proyecto) de la que forma parte este puesto. Un tema
        /// puede tener varias reglas independientes (ver ReunionTemaRegla).</summary>
        public int? ReunionTemaReglaId { get; set; }
        public int PuestoId { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
