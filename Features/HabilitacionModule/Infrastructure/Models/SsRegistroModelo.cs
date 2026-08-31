using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_registro_modelo")]
    public class SsRegistroModelo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ArchivoUrl { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
