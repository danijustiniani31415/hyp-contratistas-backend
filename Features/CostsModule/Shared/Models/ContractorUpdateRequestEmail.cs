using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.CostsModule.Shared.Models {
    /// <summary>Correo propuesto dentro de una solicitud de actualización de datos.</summary>
    public class ContractorUpdateRequestEmail {
        public int ContractorUpdateRequestEmailId { get; set; }
        public int ContractorUpdateRequestId { get; set; }
        [Column("contractor_email")]
        public string Email { get; set; } = null!;
        public int? ContractorPersonTypeId { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
