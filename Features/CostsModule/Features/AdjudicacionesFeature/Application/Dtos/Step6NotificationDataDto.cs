namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class Step6NotificationDataDto
    {
        public string ProjectDescription  { get; set; } = null!;
        public string ContributorName     { get; set; } = null!;
        public string WorkItemDescription { get; set; } = null!;
        public int?   ContractNumber      { get; set; }
        public List<string> StaffObraEmails { get; set; } = new();
        /// <summary>Correos de tipo "Oficina Técnica" (StaffProjectEmailType = 3) del proyecto.</summary>
        public List<string> OficinaTecnicaEmails { get; set; } = new();
    }
}
