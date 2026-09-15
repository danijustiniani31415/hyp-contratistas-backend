using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;

namespace Abril_Backend.Features.AlmacenModule.Infrastructure.Models
{
    /// <summary>
    /// Cantidad actual por almacén+producto+talla — se mantiene como contador (no se recalcula
    /// sumando el historial cada vez), para que consultar el stock sea instantáneo. Ver
    /// CONTEXT_LOGISTICA.md sección 7 para el patrón de concurrencia (SELECT ... FOR UPDATE).
    /// </summary>
    [Table("lb_stock")]
    public class Stock
    {
        public long Id { get; set; }
        public int AlmacenId { get; set; }
        public long ProductoId { get; set; }
        /// <summary>'' (no NULL) para productos sin talla — así el UNIQUE de la tabla detecta duplicados.</summary>
        public string Talla { get; set; } = "";
        public decimal CantidadActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        public DateTimeOffset ActualizadoEn { get; set; }

        [ForeignKey(nameof(AlmacenId))]
        public Almacen? Almacen { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
    }
}
