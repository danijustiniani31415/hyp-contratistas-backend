using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    [Table("lb_tipo_vinculo")]
    public class TipoVinculo
    {
        public short Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public bool RequiereEmo { get; set; }
        public bool RequiereEpp { get; set; }
        public bool RequiereInduccion { get; set; }
        public bool RequiereSctr { get; set; }
        public bool PermiteAccesoObra { get; set; }
        public bool Activo { get; set; } = true;
    }
}
