using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;

namespace Abril_Backend.Features.AlmacenModule.Infrastructure.Models
{
    /// <summary>Kardex — historial inmutable de movimientos. Nunca se edita ni se borra una fila.</summary>
    [Table("lb_movimiento")]
    public class Movimiento
    {
        public long Id { get; set; }
        public int AlmacenId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        /// <summary>INGRESO, SALIDA (TRANSFERENCIA soportado en el esquema, no usado aún).</summary>
        public string TipoMovimiento { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        /// <summary>PEDIDO, COMPRA, ENTREGA_EPP, PRESTAMO_HERRAMIENTA — null para ajustes manuales.</summary>
        public string? ReferenciaTipo { get; set; }
        public long? ReferenciaId { get; set; }
        public long? UsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(AlmacenId))]
        public Almacen? Almacen { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
        [ForeignKey(nameof(UsuarioSistemaId))]
        public UsuarioSistema? UsuarioSistema { get; set; }
    }
}
