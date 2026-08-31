using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_clinicas")]
    public class SsClinica
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("ruc")]
        public string? Ruc { get; set; }

        [Column("direccion")]
        public string? Direccion { get; set; }

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; } = "PENDIENTE_RESET";

        [Column("activo")]
        public bool Activo { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
