namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    public class ConfirmStep5DTO {
        public bool ArrivedWithObservations { get; set; }
        public string? ArrivalObservation { get; set; }
        public string GraphAccessToken { get; set; } = null!;
    }
}
