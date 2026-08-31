using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.CostsModule.Shared.Models
{
    public class ContractorUser
    {
        public int ContractorUserId { get; set; }
        public int ContractorId { get; set; }
        public int UserId { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }

        public Contractor Contractor { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
