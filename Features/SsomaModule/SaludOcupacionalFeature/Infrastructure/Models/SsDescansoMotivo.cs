using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// LEGACY. El "motivo" del descanso desapareció: ahora el único clasificador es
    /// ss_descanso_tipo (ver <see cref="SsDescansoTipo"/> y
    /// Migrations_Manual/ss_descanso_tipo_unificado.sql). La tabla y esta entidad se conservan
    /// solo para poder consultar el histórico; ningún flujo la escribe ni la lee.
    /// </summary>
    [Table("ss_descanso_motivo")]
    public class SsDescansoMotivo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

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
