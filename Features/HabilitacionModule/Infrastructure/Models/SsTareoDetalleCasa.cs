using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_tareo_detalle_casa")]
    public class SsTareoDetalleCasa
    {
        public int Id { get; set; }
        public int TareoId { get; set; }
        public int PartidaId { get; set; }
        public int CantidadPersonas { get; set; }

        [ForeignKey(nameof(TareoId))]
        public SsTareo? Tareo { get; set; }

        [ForeignKey(nameof(PartidaId))]
        public SsTareoPartida? Partida { get; set; }
    }
}
