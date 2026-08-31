using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("worker_emo_convalidaciones")]
    public class WorkerEmoConvalidacion
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("emo_id")]
        public int EmoId { get; set; }

        [Column("empresa_destino_id")]
        public int? EmpresaDestinoId { get; set; }

        [Column("fecha_convalidacion")]
        public DateOnly FechaConvalidacion { get; set; }

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("medico_id")]
        public int? MedicoId { get; set; }

        [Column("resultado")]
        public string Resultado { get; set; } = "Aprobada";

        [Column("fecha_vencimiento")]
        public DateOnly? FechaVencimiento { get; set; }

        [Column("url_documento")]
        public string? UrlDocumento { get; set; }

        [Column("registrado_por_id")]
        public int? RegistradoPorId { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [Column("puesto_origen")]
        public string? PuestoOrigen { get; set; }

        [Column("puesto_destino")]
        public string? PuestoDestino { get; set; }

        [Column("categoria_origen")]
        public string? CategoriaOrigen { get; set; }

        [Column("categoria_destino")]
        public string? CategoriaDestino { get; set; }

        [Column("obra_oficina_staff_origen_id")]
        public int? ObraOficinaStaffOrigenId { get; set; }

        [Column("obra_oficina_staff_destino_id")]
        public int? ObraOficinaStaffDestinoId { get; set; }

        /// <summary>True si el puesto destino sube de riesgo bajo (Oficina Central) a
        /// riesgo alto (Staff/Obra) respecto al puesto origen — ver
        /// <see cref="Shared.Constants.ObraOficinaStaffIds.EsCambioRiesgoCritico"/>.</summary>
        [Column("cambio_riesgo")]
        public bool CambioRiesgo { get; set; }

        [ForeignKey(nameof(EmoId))]
        public WorkerEmo? Emo { get; set; }

        [ForeignKey(nameof(EmpresaDestinoId))]
        public Contributor? EmpresaDestino { get; set; }

        [ForeignKey(nameof(MedicoId))]
        public SsMedicoOcupacional? Medico { get; set; }
    }
}
