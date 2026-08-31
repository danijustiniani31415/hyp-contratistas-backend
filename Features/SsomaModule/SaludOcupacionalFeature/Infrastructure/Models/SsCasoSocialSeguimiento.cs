using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_caso_social_seguimiento")]
    public class SsCasoSocialSeguimiento
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("caso_id")]
        public Guid CasoId { get; set; }

        [Column("fecha")]
        public DateOnly Fecha { get; set; }

        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("responsable_id")]
        public int? ResponsableId { get; set; }

        [Column("proxima_accion")]
        public DateOnly? ProximaAccion { get; set; }

        [Column("accion_tomada")]
        public string? AccionTomada { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("state")]
        public bool State { get; set; } = true;

        [ForeignKey(nameof(CasoId))]
        public SsCasoSocial? Caso { get; set; }

        [ForeignKey(nameof(ResponsableId))]
        public Worker? Responsable { get; set; }
    }
}
