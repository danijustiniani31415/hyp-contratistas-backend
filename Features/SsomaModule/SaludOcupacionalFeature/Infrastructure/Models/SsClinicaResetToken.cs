using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_clinica_reset_token")]
    public class SsClinicaResetToken
    {
        public int Id { get; set; }
        public int ClinicaId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEn { get; set; }
        public bool Usado { get; set; } = false;
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(ClinicaId))]
        public SsClinica? Clinica { get; set; }
    }
}
