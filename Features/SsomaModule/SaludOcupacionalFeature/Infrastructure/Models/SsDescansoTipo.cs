using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_descanso_tipo")]
    public class SsDescansoTipo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Nombre normalizado con el que se clasifica y se guarda ("Accidente común").</summary>
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Etiqueta corta que ve el trabajador en Mi Salud ("Accidente"). Lo que se guarda sigue
        /// siendo <see cref="Nombre"/>; el corto es solo presentación.
        /// </summary>
        [Column("nombre_corto")]
        public string? NombreCorto { get; set; }

        /// <summary>true = el trabajador puede elegirlo desde Mi Salud (solo los tipos "común").</summary>
        [Column("disponible_mi_salud")]
        public bool DisponibleMiSalud { get; set; } = false;

        /// <summary>Orden fijo del desplegable (no alfabético).</summary>
        [Column("orden")]
        public int Orden { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
