using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>
    /// Catálogo genérico para listas fijas SIN integridad referencial (banco, tipo AFP/ONP,
    /// categoría laboral...). Cargo/TipoVinculo/EmpresaContratista NO van acá — son tablas propias
    /// porque otras tablas les hacen FK. Esto es solo para texto plano editable desde el front.
    /// </summary>
    [Table("lb_catalogo_valor")]
    public class CatalogoValor
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = null!;
        public string Valor { get; set; } = null!;
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }
}
