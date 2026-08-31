using Abril_Backend.Infrastructure.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.CostsModule.Shared.Models {
    public class ContractorEmail {
        public int ContractorEmailId { get; set; }
        public int ContractorId { get; set; }
        [Column("contractor_email")]
        public string Email { get; set; } = null!;
        public int? ContractorPersonTypeId { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
        public int? UserId { get; set; }
        public Contractor Contractor { get; set; } = null!;
        public ContractorPersonType? PersonType { get; set; }
        public User? User { get; set; }
    }
}
