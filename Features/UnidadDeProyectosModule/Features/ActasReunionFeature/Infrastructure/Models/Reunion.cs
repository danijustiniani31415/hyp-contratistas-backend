namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Acta de reunión (SIG-FO-17). Registra una reunión de un proyecto, de un nodo del árbol
    /// area_scope (gerencia/área/subárea) o de toda la organización (ambos campos null): puede
    /// agendarse a futuro (estado PROGRAMADA), reprogramarse varias veces (ver ReunionReprogramacion)
    /// y al realizarse concentra participantes, acuerdos y archivos adjuntos.
    /// </summary>
    public class Reunion
    {
        public int ReunionId { get; set; }

        /// <summary>Null si la reunión es de un area_scope o de toda la organización.</summary>
        public int? ProjectId { get; set; }

        /// <summary>
        /// Nodo del árbol area_scope (gerencia/área/subárea) al que pertenece la reunión.
        /// Null si es de proyecto o de toda la organización. Nunca coexiste con ProjectId
        /// (ver constraint chk_reunion_ambito_unico).
        /// </summary>
        public int? AreaScopeId { get; set; }

        /// <summary>Correlativo dentro de su serie: por proyecto, por area_scope, o global si ambos son null.</summary>
        public int Numero { get; set; }
        public string Tema { get; set; } = null!;
        public string? ConvocadoPor { get; set; }
        public string? Lugar { get; set; }

        /// <summary>Fecha vigente de la reunión; cambia con cada reprogramación.</summary>
        public DateOnly Fecha { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }

        public int ReunionEstadoId { get; set; }
        public string? Observaciones { get; set; }

        /// <summary>
        /// Tema del catálogo elegido al agendar (null si el tema fue personalizado y no se guardó
        /// como recurrente). Permite al job de recordatorios resolver la configuración de agenda
        /// vigente para esta ocurrencia.
        /// </summary>
        public int? ReunionTemaId { get; set; }

        /// <summary>Reunión de la que se promovió el tema de esta reunión.</summary>
        public int? ReunionAnteriorId { get; set; }

        /// <summary>
        /// Agenda fija ad-hoc de esta reunión puntual (tema personalizado, no guardado como
        /// recurrente): al no depender de ningún ReunionTema, se define aquí directamente y
        /// tiene prioridad sobre la configuración de agenda del tema del catálogo, si hubiera.
        /// </summary>
        public string? AgendaTexto { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
