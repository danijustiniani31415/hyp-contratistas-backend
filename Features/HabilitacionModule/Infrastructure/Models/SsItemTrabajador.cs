using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_item_trabajador")]
    public class SsItemTrabajador
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string AplicaA { get; set; } = "TODOS";
        public string? AplicaCategoria { get; set; }
        public string? AplicaObraOficina { get; set; }
        public string? ExcluyeObraOficina { get; set; }
        public string? ExcluyeCategoriaContratista { get; set; }
        public string Responsable { get; set; } = "SSOMA";
        public bool RequiereVigencia { get; set; } = true;
        public bool EsSctrVidaley { get; set; } = false;
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }
}
