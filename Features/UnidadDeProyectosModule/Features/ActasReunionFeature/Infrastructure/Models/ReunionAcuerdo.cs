namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Acuerdo/acción tomada en una reunión. Tiene fecha programada, una posible
    /// reprogramación y la fecha real en la que se terminó cumpliendo.
    /// </summary>
    public class ReunionAcuerdo
    {
        public int ReunionAcuerdoId { get; set; }
        public int ReunionId { get; set; }
        public string Descripcion { get; set; } = null!;
        public string? Acciones { get; set; }
        public DateOnly? FechaProgramada { get; set; }
        public DateOnly? FechaReprogramacion { get; set; }
        public DateOnly? FechaCumplimiento { get; set; }
        /// <summary>Cómo se levantó el acuerdo. Siempre obligatorio al marcar CUMPLIDO.</summary>
        public string? ComentarioCumplimiento { get; set; }
        public int ReunionAcuerdoEstadoId { get; set; }
        public int Orden { get; set; }

        /// <summary>NORMAL | MEDIO | CRITICO.</summary>
        public string Criticidad { get; set; } = "NORMAL";

        /// <summary>Cuántas veces se reprogramó la fecha programada (vía el flujo de revisión de
        /// pendientes de reuniones anteriores). 2+ es señal de alerta: el acuerdo se sigue
        /// postergando.</summary>
        public int VecesReprogramado { get; set; }
        /// <summary>Motivo de la última reprogramación, si hubo alguna.</summary>
        public string? UltimoMotivoReprogramacion { get; set; }

        /// <summary>Si true, cada responsable debe aceptar el acuerdo antes de que quede activo (ver ReunionAcuerdoResponsable.EstadoAceptacion).</summary>
        public bool RequiereAceptacion { get; set; }
        /// <summary>Si true, no se puede marcar CUMPLIDO sin EvidenciaUrl.</summary>
        public bool RequiereEvidencia { get; set; }
        public string? EvidenciaUrl { get; set; }

        /// <summary>Si true, es solo un registro informativo: no requiere seguimiento ni acción de
        /// ningún responsable. Se excluye del dashboard personal "Mis Acuerdos".</summary>
        public bool EsInformativo { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
