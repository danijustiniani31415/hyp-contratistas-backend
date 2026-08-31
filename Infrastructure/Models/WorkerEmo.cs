using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("worker_emos")]
    public class WorkerEmo
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("empresa_origen_id")]
        public int? EmpresaOrigenId { get; set; }

        [Column("fecha_emo")]
        public DateOnly FechaEmo { get; set; }

        [Column("fecha_vencimiento")]
        public DateOnly? FechaVencimiento { get; set; }

        [Column("fecha_lectura")]
        public DateOnly? FechaLectura { get; set; }

        [Column("resultado")]
        public string? Resultado { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("tipo_emo_id")]
        public int? TipoEmoId { get; set; }

        [Column("clinica_id")]
        public int? ClinicaId { get; set; }

        [Column("medico_id")]
        public int? MedicoId { get; set; }

        [Column("fecha_vencimiento_calculada")]
        public DateOnly? FechaVencimientoCalculada { get; set; }

        [Column("aptitud")]
        public string? Aptitud { get; set; }

        [Column("requiere_interconsulta")]
        public bool RequiereInterconsulta { get; set; }

        [Column("url_resultado")]
        public string? UrlResultado { get; set; }

        [Column("url_aptitud")]
        public string? UrlAptitud { get; set; }

        [Column("url_emo_completo")]
        public string? UrlEmoCompleto { get; set; }

        [Column("numero_informe")]
        public string? NumeroInforme { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Vigente";

        [Column("registrado_por_id")]
        public int? RegistradoPorId { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [Column("interconsulta_resuelta")]
        public bool InterconsultaResuelta { get; set; }

        /// <summary>
        /// true = la lectura de este EMO la hace el médico ocupacional de Abril Grupo
        /// Inmobiliario (no la clínica). Mientras sea true y UrlResultado siga null, el EMO
        /// aparece en la subtab "Pendientes de lectura" de la pantalla de EMOs.
        /// </summary>
        [Column("requiere_lectura_abril")]
        public bool RequiereLecturaAbril { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(EmpresaOrigenId))]
        public Contributor? EmpresaOrigen { get; set; }

        [ForeignKey(nameof(TipoEmoId))]
        public SsEmoTipo? TipoEmo { get; set; }

        [ForeignKey(nameof(ClinicaId))]
        public SsClinica? Clinica { get; set; }

        [ForeignKey(nameof(MedicoId))]
        public SsMedicoOcupacional? Medico { get; set; }

        public ICollection<SsEmoExamenDetalle> Examenes { get; set; } = new List<SsEmoExamenDetalle>();
        public ICollection<SsEmoRestriccion> Restricciones { get; set; } = new List<SsEmoRestriccion>();
        public ICollection<WorkerEmoConvalidacion> Convalidaciones { get; set; } = new List<WorkerEmoConvalidacion>();
    }
}
