using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_descanso_medico")]
    public class SsDescansoMedico
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        /// <summary>Agrupador del caso clínico (ver SsDescansoCaso) — el descanso original y
        /// cada "más descanso" que lo extiende comparten el mismo CasoId.</summary>
        [Column("caso_id")]
        public int CasoId { get; set; }

        [ForeignKey(nameof(CasoId))]
        public SsDescansoCaso? Caso { get; set; }

        // Nota: las columnas legacy `tipo` (texto), `motivo` (texto) y `motivo_id` siguen en la
        // tabla con su valor histórico para auditoría, pero ya no se mapean: el único
        // clasificador es TipoId → ss_descanso_tipo (ver Migrations_Manual/ss_descanso_tipo_unificado.sql).

        [Column("fecha_inicio")]
        public DateOnly FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateOnly FechaFin { get; set; }

        [Column("diagnostico")]
        public string? Diagnostico { get; set; }

        /// <summary>LEGACY — texto libre, se conserva solo para histórico. El diagnóstico CIE-10
        /// real vive en <see cref="DiagnosticoCie10Codigo"/> (FK al catálogo oficial).</summary>
        [Column("diagnostico_cie10")]
        public string? DiagnosticoCie10 { get; set; }

        /// <summary>FK a cie10_catalogo. Solo lo asigna el médico al revisar el caso — el
        /// trabajador que sube el descanso no lo indica (no tiene por qué saberlo).</summary>
        [Column("diagnostico_cie10_codigo")]
        public string? DiagnosticoCie10Codigo { get; set; }

        [ForeignKey(nameof(DiagnosticoCie10Codigo))]
        public Cie10Catalogo? DiagnosticoCie10Catalogo { get; set; }

        [Column("url_certificado")]
        public string? UrlCertificado { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Pendiente";

        [Column("motivo_rechazo")]
        public string? MotivoRechazo { get; set; }

        [Column("aprobado_por_id")]
        public int? AprobadoPorId { get; set; }

        [Column("fecha_aprobacion")]
        public DateTimeOffset? FechaAprobacion { get; set; }

        [Column("accidente_id")]
        public int? AccidenteId { get; set; }

        [Column("es_recaida")]
        public bool EsRecaida { get; set; } = false;

        [Column("proyecto_id")]
        public int? ProyectoId { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("notificado_gth")]
        public bool NotificadoGth { get; set; } = false;

        [Column("notificado_jefe")]
        public bool NotificadoJefe { get; set; } = false;

        [Column("reportado_por_trabajador")]
        public bool ReportadoPorTrabajador { get; set; } = false;

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Column("dias")]
        public int Dias { get; set; }

        /// <summary>Único clasificador del descanso (ss_descanso_tipo). Obligatorio.</summary>
        [Column("tipo_id")]
        public int TipoId { get; set; }

        [Column("url_documento")]
        public string? UrlDocumento { get; set; }

        [Column("topico_origen_id")]
        public int? TopicoOrigenId { get; set; }

        [Column("prorroga_del_id")]
        public int? ProrrogaDelId { get; set; }

        /// <summary>LEGACY — el alta ahora cierra el CASO (<see cref="SsDescansoCaso.FechaCierre"/>
        /// / AltaPorId / AltaObservaciones), no un descanso individual. Estas tres columnas se
        /// conservan solo por los casos dados de alta antes de que existiera SsDescansoCaso.</summary>
        [Column("fecha_alta")]
        public DateOnly? FechaAlta { get; set; }

        /// <summary>LEGACY — ver comentario de <see cref="FechaAlta"/>.</summary>
        [Column("alta_por_id")]
        public int? AltaPorId { get; set; }

        /// <summary>LEGACY — ver comentario de <see cref="FechaAlta"/>.</summary>
        [Column("alta_observaciones")]
        public string? AltaObservaciones { get; set; }

        [Column("state")]
        public bool State { get; set; } = true;

        [Column("registrado_por_id")]
        public int RegistradoPorId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(TipoId))]
        public SsDescansoTipo? TipoCatalogo { get; set; }

        [ForeignKey(nameof(AccidenteId))]
        public SsAccidenteTrabajo? Accidente { get; set; }

        [ForeignKey(nameof(TopicoOrigenId))]
        public TopicoAtencion? TopicoOrigen { get; set; }
    }
}
