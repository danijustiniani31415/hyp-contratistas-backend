namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    public class CurrencySimpleDTO {
        public int CurrencyId {get; set;}
        public string CurrencyCode {get;set;}
        public string CurrencyDescription {get; set;}
        public string? CurrencySymbol {get;set;}
    }
}