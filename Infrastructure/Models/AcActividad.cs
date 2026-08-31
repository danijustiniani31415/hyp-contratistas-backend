using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ac_actividades")]
    public class AcActividad
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("user_id2")]
        public int? UserId2 { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("tipo")]
        public string? Tipo { get; set; }

        [Column("etapa_id")]
        public int? EtapaId { get; set; }

        [Column("categoria_id")]
        public int? CategoriaId { get; set; }

        [Column("especialidad_id")]
        public int? EspecialidadId { get; set; }

        [Column("prioridad")]
        public string? Prioridad { get; set; }

        [Column("estado")]
        public string? Estado { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }

        [Column("orden")]
        public int? Orden { get; set; }

        [Column("spi")]
        public decimal? Spi { get; set; }

        [Column("inicio_programado")]
        public DateOnly? InicioProgramado { get; set; }

        [Column("fin_programado")]
        public DateOnly? FinProgramado { get; set; }

        [Column("inicio_efectivo")]
        public DateOnly? InicioEfectivo { get; set; }

        [Column("fin_efectivo")]
        public DateOnly? FinEfectivo { get; set; }

        [Column("observaciones")]
        public string? Observaciones { get; set; }
    }
}
