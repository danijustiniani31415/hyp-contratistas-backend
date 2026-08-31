namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    public class Contract {
        public int ContractId {get; set;}
        public string ContractDescription {get; set;} = null!;
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}