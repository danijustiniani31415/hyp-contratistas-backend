using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_clinica_emails")]
    public class SsClinicaEmail
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("clinica_id")]
        public int ClinicaId { get; set; }

        [Column("nombre")]
        public string? Nombre { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [ForeignKey(nameof(ClinicaId))]
        public SsClinica? Clinica { get; set; }
    }
}
