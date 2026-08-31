using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ac_actividades_plantilla")]
    public class AcActividadPlantilla
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("tipo")]
        public string? Tipo { get; set; }

        [Column("etapa_id")]
        public int? EtapaId { get; set; }

        [Column("orden")]
        public int? Orden { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }

        [Column("categoria_id")]
        public int? CategoriaId { get; set; }

        [Column("especialidad_id")]
        public int? EspecialidadId { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        public AcCategoria? Categoria { get; set; }

        [ForeignKey(nameof(EspecialidadId))]
        public AcEspecialidad? Especialidad { get; set; }
    }
}
