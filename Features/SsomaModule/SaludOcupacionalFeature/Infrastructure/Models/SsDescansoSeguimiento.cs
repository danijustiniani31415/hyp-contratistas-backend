using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_descanso_seguimiento")]
    public class SsDescansoSeguimiento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("descanso_id")]
        public int DescansoId { get; set; }

        /// <summary>Agrupador del caso (ver SsDescansoCaso) — se guarda acá además de en
        /// DescansoId para que el timeline del caso liste todos los seguimientos sin importar
        /// cuál de sus descansos individuales estaba vigente cuando se registró cada uno.</summary>
        [Column("caso_id")]
        public int CasoId { get; set; }

        [Column("fecha_seguimiento")]
        public DateTimeOffset FechaSeguimiento { get; set; }

        /// <summary>LEGACY — texto libre. El clasificador real es <see cref="TipoId"/>.</summary>
        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        /// <summary>FK a ss_seguimiento_tipo — reemplaza la lista hardcodeada que vivía en el
        /// frontend ('Médico', 'Asistenta Social', 'Seguimiento', 'Alta').</summary>
        [Column("tipo_id")]
        public int? TipoId { get; set; }

        [Column("realizado_por_rol")]
        public string? RealizadoPorRol { get; set; }

        [Column("realizado_por_id")]
        public int? RealizadoPorId { get; set; }

        [Column("nota")]
        public string? Nota { get; set; }

        [Column("proxima_cita")]
        public DateOnly? ProximaCita { get; set; }

        [Column("url_evidencia")]
        public string? UrlEvidencia { get; set; }

        /// <summary>FK a cie10_catalogo — mismo diagnóstico oficial que en el descanso, editable
        /// también acá porque el seguimiento puede refinar o confirmar el diagnóstico inicial.</summary>
        [Column("diagnostico_cie10_codigo")]
        public string? DiagnosticoCie10Codigo { get; set; }

        [ForeignKey(nameof(DiagnosticoCie10Codigo))]
        public Cie10Catalogo? DiagnosticoCie10Catalogo { get; set; }

        /// <summary>Snapshot del puesto de trabajo del paciente al momento del seguimiento — se
        /// congela igual que worker_vinculaciones.puesto, no se recalcula después. Es contexto
        /// para que el médico evalúe aptitud para ESE puesto, no un dato editable.</summary>
        [Column("puesto_trabajo_snapshot")]
        public string? PuestoTrabajoSnapshot { get; set; }

        /// <summary>true = el detalle clínico (Nota) no se expone a roles sin permiso de ver
        /// detalle clínico — ver DescansoMedicoRepository.GetSeguimientos.</summary>
        [Column("confidencial")]
        public bool Confidencial { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("state")]
        public bool State { get; set; } = true;

        [ForeignKey(nameof(DescansoId))]
        public SsDescansoMedico? Descanso { get; set; }

        [ForeignKey(nameof(TipoId))]
        public SsSeguimientoTipo? TipoCatalogo { get; set; }
    }
}
