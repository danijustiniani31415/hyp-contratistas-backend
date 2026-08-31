namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Una regla de convocatoria de un tema: a quién convocar automáticamente (un área/gerencia
    /// y/o un proyecto, combinado con los puestos en ReunionTemaPuesto.ReunionTemaReglaId). Un
    /// tema puede tener varias reglas independientes — ej. "Reunión de Jefaturas de Proyectos"
    /// convoca a los Jefes de Proyectos de su gerencia, PERO además al Gerente Inmobiliario de
    /// otra gerencia: son dos reglas, no una sola combinación de área+puestos.
    /// </summary>
    public class ReunionTemaRegla
    {
        public int ReunionTemaReglaId { get; set; }
        public int ReunionTemaId { get; set; }

        /// <summary>Nodo del árbol area_scope. Null = no filtra por área (junto con ProjectId null
        /// y sin puestos, la regla completa se descarta al guardar por no aportar nada).</summary>
        public int? AreaScopeId { get; set; }
        /// <summary>Proyecto: convoca a todo su staff, o combinado con puestos para acotar.</summary>
        public int? ProjectId { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
