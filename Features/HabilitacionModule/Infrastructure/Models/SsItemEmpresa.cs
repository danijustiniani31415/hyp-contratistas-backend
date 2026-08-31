using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_item_empresa")]
    public class SsItemEmpresa
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Responsable { get; set; } = "SSOMA";
        public int Orden { get; set; }
        public bool RequiereVigencia { get; set; } = true;
        public bool Activo { get; set; } = true;
        public bool EsMensual { get; set; } = false;
    }
}
