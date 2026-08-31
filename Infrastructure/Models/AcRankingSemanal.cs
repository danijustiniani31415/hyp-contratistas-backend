using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ac_ranking_semanal")]
    public class AcRankingSemanal
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("semana")]
        public DateOnly Semana { get; set; }

        [Column("ies")]
        public decimal Ies { get; set; }

        [Column("comp_spi")]
        public decimal CompSpi { get; set; }

        [Column("comp_cierre")]
        public decimal CompCierre { get; set; }

        [Column("comp_inicio")]
        public decimal CompInicio { get; set; }

        [Column("total")]
        public int Total { get; set; }

        [Column("completadas")]
        public int Completadas { get; set; }

        [Column("sin_compromisos")]
        public bool SinCompromisos { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
