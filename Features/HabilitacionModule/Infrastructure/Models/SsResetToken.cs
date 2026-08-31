using Abril_Backend.Infrastructure.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_reset_token")]
    public class SsResetToken
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraAt { get; set; }
        public bool Usado { get; set; } = false;
        public DateTime? CreatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
