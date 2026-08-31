using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_item_equipo")]
    public class SsItemEquipo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool RequiereVigencia { get; set; } = false;
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Tipo de equipo al que aplica este ítem entregable. NULL = ítem genérico,
        /// se exige a todos los equipos sin importar su tipo (caso más común).
        /// Con valor = específico de ese tipo (ej. items propios de "Volquete").
        /// </summary>
        public int? TipoEquipoId { get; set; }

        [ForeignKey(nameof(TipoEquipoId))]
        public SsTipoEquipo? TipoEquipo { get; set; }
    }
}
