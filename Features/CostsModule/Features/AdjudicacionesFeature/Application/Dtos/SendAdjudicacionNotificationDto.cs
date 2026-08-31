namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class SendAdjudicacionNotificationDto
    {
        public int ProjectSubContractorId { get; set; }
        public string GraphAccessToken { get; set; } = null!;
    }
}
