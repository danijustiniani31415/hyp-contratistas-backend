using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_tareo_detalle_contratista")]
    public class SsTareoDetalleContratista
    {
        public int Id { get; set; }
        public int TareoId { get; set; }
        public int EmpresaId { get; set; }
        public int CantidadPersonas { get; set; }

        [ForeignKey(nameof(TareoId))]
        public SsTareo? Tareo { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public Contributor? Empresa { get; set; }
    }
}
