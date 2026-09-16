using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    [Table("lb_almacen")]
    public class Almacen
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        /// <summary>CENTRAL o PROYECTO.</summary>
        public string Tipo { get; set; } = null!;
        public int? ProyectoId { get; set; }
        public bool Activo { get; set; } = true;
        /// <summary>Dirección + ubigeo (catálogo SUNAT) — punto de partida/llegada de la GRE.</summary>
        public string? Direccion { get; set; }
        public string? Ubigeo { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }
    }
}
