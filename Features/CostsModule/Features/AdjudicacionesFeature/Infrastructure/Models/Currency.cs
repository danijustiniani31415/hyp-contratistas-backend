namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    public class Currency {
        public int CurrencyId {get; set;}
        public string CurrencyCode {get; set;} = null!;
        public string CurrencyDescription {get; set;} = null!;
        public string CurrencySymbol {get; set;} = null!;
        public DateTimeOffset CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTimeOffset? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}