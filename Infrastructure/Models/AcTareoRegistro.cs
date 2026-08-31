using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ac_tareo_registro")]
    public class AcTareoRegistro
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        /// <summary>INICIO_JORNADA | INICIO_ALMUERZO | RETORNO | FIN_JORNADA</summary>
        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("fecha")]
        public DateOnly Fecha { get; set; }

        [Column("hora_servidor")]
        public DateTime HoraServidor { get; set; }

        [Column("hora_dispositivo")]
        public DateTime? HoraDispositivo { get; set; }

        [Column("foto_url")]
        public string FotoUrl { get; set; } = string.Empty;

        [Column("foto_hash")]
        public string FotoHash { get; set; } = string.Empty;

        [Column("idempotency_key")]
        public Guid IdempotencyKey { get; set; }

        [Column("lat")]
        public decimal? Lat { get; set; }

        [Column("lng")]
        public decimal? Lng { get; set; }

        [Column("precision_metros")]
        public decimal? PrecisionMetros { get; set; }

        [Column("project_id")]
        public int? ProjectId { get; set; }

        [Column("distancia_metros")]
        public decimal? DistanciaMetros { get; set; }

        [Column("face_match_score")]
        public decimal? FaceMatchScore { get; set; }

        /// <summary>VERIFICADO | REVISAR | RECHAZADO | SIN_ENROLAR</summary>
        [Column("estado")]
        public string Estado { get; set; } = "PENDIENTE";

        [Column("motivo_revision")]
        public string? MotivoRevision { get; set; }

        [Column("revisado_por")]
        public int? RevisadoPor { get; set; }

        [Column("revisado_en")]
        public DateTime? RevisadoEn { get; set; }

        [Column("ip_origen")]
        public string? IpOrigen { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
