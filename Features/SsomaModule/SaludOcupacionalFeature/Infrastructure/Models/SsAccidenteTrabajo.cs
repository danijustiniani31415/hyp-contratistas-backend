using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_accidente_trabajo")]
    public class SsAccidenteTrabajo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("fecha_accidente")]
        public DateOnly FechaAccidente { get; set; }

        [Column("hora_accidente")]
        public TimeOnly? HoraAccidente { get; set; }

        [Column("proyecto_id")]
        public int? ProyectoId { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("lugar_accidente")]
        public string? LugarAccidente { get; set; }

        [Column("tipo_accidente")]
        public string? TipoAccidente { get; set; }

        [Column("mecanismo")]
        public string? Mecanismo { get; set; }

        [Column("parte_cuerpo_afectada")]
        public string? ParteCuerpoAfectada { get; set; }

        [Column("agente_riesgo_id")]
        public int? AgenteRiesgoId { get; set; }

        [ForeignKey(nameof(AgenteRiesgoId))]
        public SsAgenteRiesgo? AgenteRiesgo { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("descripcion_lesion")]
        public string? DescripcionLesion { get; set; }

        [Column("diagnostico_cie10")]
        public string? DiagnosticoCie10 { get; set; }

        [Column("requiere_hospitalizacion")]
        public bool RequiereHospitalizacion { get; set; } = false;

        [Column("hospital_nombre")]
        public string? HospitalNombre { get; set; }

        [Column("atencion_topico_id")]
        public int? AtencionTopicoId { get; set; }

        [Column("dias_descanso_estimados")]
        public int DiasDescansoEstimados { get; set; } = 0;

        [Column("dias_descanso_reales")]
        public int? DiasDescansoReales { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Registrado";

        [Column("fecha_alta")]
        public DateOnly? FechaAlta { get; set; }

        [Column("restricciones_reintegro")]
        public string? RestriccionesReintegro { get; set; }

        [Column("notificado_sunafil")]
        public bool NotificadoSunafil { get; set; } = false;

        [Column("fecha_notificacion_sunafil")]
        public DateOnly? FechaNotificacionSunafil { get; set; }

        [Column("numero_notificacion_sunafil")]
        public string? NumeroNotificacionSunafil { get; set; }

        [Column("paso_id")]
        public int? PasoId { get; set; }

        [Column("url_informe")]
        public string? UrlInforme { get; set; }

        [Column("registrado_por_id")]
        public int RegistradoPorId { get; set; }

        [Column("cerrado_por_id")]
        public int? CerradoPorId { get; set; }

        [Column("fecha_cierre")]
        public DateTimeOffset? FechaCierre { get; set; }

        [Column("flash_report_id")]
        public int? FlashReportId { get; set; }

        [Column("caso_social_id")]
        public Guid? CasoSocialId { get; set; }

        [Column("requiere_reinduccion")]
        public bool RequiereReinduccion { get; set; } = true;

        [Column("reinduccion_completada")]
        public bool ReinduccionCompletada { get; set; } = false;

        [Column("fecha_reinduccion")]
        public DateOnly? FechaReinduccion { get; set; }

        [Column("reinduccion_por_id")]
        public int? ReinduccionPorId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        public ICollection<SsAccidenteSeguimiento> Seguimientos { get; set; } = [];
    }
}
