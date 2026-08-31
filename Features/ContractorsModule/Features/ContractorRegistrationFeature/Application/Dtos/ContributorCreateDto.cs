namespace Abril_Backend.Features.Contractors.ContractorRegistration.Application.Dtos {
    public class ContributorCreateDto {
        public string ContributorRuc { get; set; }
        public string ContributorName { get; set; }
        public string ContributorAddress { get; set; }
        public string ContributorEconomicActivityDescription { get; set; }
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
        public string? LegalRepresentativeDni { get; set; }
        public string? LegalRepresentativeFullName { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }
        public IFormFile? LogoFile { get; set; }
        public IFormFile? BrochureFile { get; set; }
        public IFormFile? FichaRucFile { get; set; }
        public IFormFile? ReferencesListFile { get; set; }
        /// <summary>Lista de correos. En multipart/form-data se envía como campos repetidos: ContributorEmails=a&amp;ContributorEmails=b</summary>
        public List<string> ContributorEmails { get; set; } = [];
        /// <summary>Clasificaciones paralelas a ContributorEmails (mismo orden). Valor vacío = sin clasificación.</summary>
        public List<string?> ContributorEmailPersonTypeIds { get; set; } = [];
        public string? GraphAccessToken { get; set; }
        /// <summary>
        /// true cuando el usuario, tras ser advertido de que el RUC ya existe, confirma que
        /// desea enviar una solicitud de actualización de datos del contratista existente.
        /// </summary>
        public bool IsUpdateRequest { get; set; }
        /// <summary>
        /// true cuando el registro se hace desde la ruta interna /contractors/registro-interno
        /// (personal del sistema). Es una regla de RUTA (igual que la obligatoriedad del logo):
        /// el backend no distingue la ruta por el token, así que el frontend la informa.
        /// Solo los registros internos notifican por correo al equipo de Costos y Presupuestos.
        /// </summary>
        public bool IsInternalRegistration { get; set; }
    }
}
