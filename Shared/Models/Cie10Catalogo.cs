using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Shared.Models
{
    /// <summary>
    /// Catálogo oficial CIE-10 (Clasificación Estadística Internacional de Enfermedades).
    /// Tabla transversal (no exclusiva de SSOMA) porque el diagnóstico CIE-10 aparece en varios
    /// módulos (descanso médico, seguimiento médico, accidentes de trabajo).
    ///
    /// La carga de códigos se hace por SQL directo (import masivo desde el archivo oficial
    /// MINSA/OPS), no por seed manual acá — nadie debe escribir un código CIE-10 a mano.
    /// </summary>
    [Table("cie10_catalogo")]
    public class Cie10Catalogo
    {
        /// <summary>Código CIE-10 (ej. "J45.9"). Es la PK — es el valor que se guarda como FK
        /// en las tablas que referencian un diagnóstico, no un id numérico autoincremental.</summary>
        [Key]
        [Column("codigo")]
        [MaxLength(10)]
        public string Codigo { get; set; } = string.Empty;

        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}
