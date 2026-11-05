using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.CatalogoModule.Infrastructure.Models
{
    /// <summary>
    /// Otra forma de escribir el nombre de un producto («ARCO SIERRA», «ARCOSIERRA», «Respirador original»).
    /// Cuando alguien vincula un texto de un Excel a un producto, el sistema aprende esa escritura y la próxima
    /// vez lo reconoce solo. AliasNorm es la escritura sin tildes, espacios ni signos: es única en todo el catálogo,
    /// así una misma escritura nunca apunta a dos productos.
    /// </summary>
    [Table("lb_producto_alias")]
    public class ProductoAlias
    {
        public long Id { get; set; }
        public long ProductoId { get; set; }
        public string Alias { get; set; } = null!;
        public string AliasNorm { get; set; } = null!;
        public DateTimeOffset CreadoEn { get; set; }
    }
}
