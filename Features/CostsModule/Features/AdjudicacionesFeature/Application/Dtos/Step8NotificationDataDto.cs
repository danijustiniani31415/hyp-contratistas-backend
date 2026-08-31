namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class Step8NotificationDataDto
    {
        public string ProjectDescription  { get; set; } = null!;
        public string ContributorName     { get; set; } = null!;
        public List<string> StaffObraEmails  { get; set; } = new();
        /// <summary>Correos de tipo "Oficina Técnica" (StaffProjectEmailType = 3) del proyecto — copia en el paso 8.</summary>
        public List<string> OficinaTecnicaEmails { get; set; } = new();
        public List<ProjectSubContractorFileDto> ScannedDocs { get; set; } = new();
    }
}
