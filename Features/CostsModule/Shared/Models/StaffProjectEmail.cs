using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.CostsModule.Shared.Models {
    public class StaffProjectEmail
    {
        public int StaffProjectEmailId { get; set; }
        public int ProjectId { get; set; }
        public string Email { get; set; } = null!;
        public int StaffProjectEmailTypeId { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
        public Project Project { get; set; } = null!;
        public StaffProjectEmailType EmailType { get; set; } = null!;
    }
}
