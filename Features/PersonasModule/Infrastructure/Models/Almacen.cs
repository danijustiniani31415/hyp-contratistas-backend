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
        /// <summary>Código del establecimiento anexo en la Ficha RUC (0000 = domicilio fiscal). Lo pide la GRE motivo 04.</summary>
        public string CodigoEstablecimientoSunat { get; set; } = "0000";

        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }
    }
}
