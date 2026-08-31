using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    /// <summary>Tipo: 'VERTICAL' | 'HORIZONTAL'.</summary>
    [Table("bim_actividad")]
    public class BimActividad
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("macro_actividad_id")]
        public int MacroActividadId { get; set; }
        public BimMacroActividad MacroActividad { get; set; } = null!;

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("orden")]
        public int Orden { get; set; }
    }
}
