using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ac_avance_semanal")]
    public class AcAvanceSemanal
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("actividad_id")]
        public int ActividadId { get; set; }

        [Column("semana")]
        public DateOnly Semana { get; set; }

        [Column("porcentaje_avance")]
        public decimal PorcentajeAvance { get; set; }

        [Column("spi")]
        public decimal Spi { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
