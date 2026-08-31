namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class ScNotificationDataDto
    {
        public string ProjectDescription { get; set; } = null!;
        public string WorkItemDescription { get; set; } = null!;
        public List<string> ContractorEmails { get; set; } = new();
        public List<string> StaffObraEmails  { get; set; } = new();
        /// <summary>Correos de tipo "Oficina Técnica" (StaffProjectEmailType = 3) del proyecto.</summary>
        public List<string> OficinaTecnicaEmails { get; set; } = new();
    }
}
