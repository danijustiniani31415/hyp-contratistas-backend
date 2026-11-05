namespace Abril_Backend.Features.AlmacenModule.Application.Dtos
{
    /// <summary>Datos para crear en el catálogo un producto que viene del Excel y todavía no existe.</summary>
    public class NuevoProductoDto
    {
        public string Nombre { get; set; } = "";
        /// <summary>Marca detectada o escrita: se guarda aparte y se agrega al nombre si no la menciona.</summary>
        public string? Marca { get; set; }
        public int CategoriaId { get; set; }
        public string UnidadMedida { get; set; } = "UND";
        public bool RequiereTalla { get; set; }
        /// <summary>true si el producto se maneja por color (variante, como la talla): pintura, cinta aislante…</summary>
        public bool RequiereColor { get; set; }
        /// <summary>ROPA, CALZADO o GUANTES — obligatorio si RequiereTalla.</summary>
        public string? TipoTalla { get; set; }
        /// <summary>Cómo viene en la factura (PAQUETE…) y cuántas unidades trae: se guarda para próximas cargas.</summary>
        public string? Presentacion { get; set; }
        public decimal? UnidadesPorPresentacion { get; set; }
    }

    public class CargaMasivaFilaDto
    {
        /// <summary>Producto que ya existe en el catálogo. Si es null, se crea con NuevoProducto.</summary>
        public long? ProductoId { get; set; }
        public NuevoProductoDto? NuevoProducto { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        /// <summary>Texto original del Excel: si la fila se vinculó a un producto, el sistema lo aprende como alias.</summary>
        public string? Alias { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
    }

    public class CargaMasivaDto
    {
        public int AlmacenId { get; set; }
        public List<CargaMasivaFilaDto> Filas { get; set; } = new();
    }

    public class CargaMasivaFilaResultadoDto
    {
        /// <summary>Número de fila (desde 1) tal como llegó en la solicitud.</summary>
        public int Fila { get; set; }
        public bool Ok { get; set; }
        public string? Mensaje { get; set; }
        public long? ProductoId { get; set; }
        public bool ProductoCreado { get; set; }
    }

    public class CargaMasivaResultadoDto
    {
        public int Registradas { get; set; }
        public int ProductosCreados { get; set; }
        public int Fallidas { get; set; }
        public List<CargaMasivaFilaResultadoDto> Filas { get; set; } = new();
    }
}
