namespace Abril_Backend.Infrastructure.Services
{
    public class StorageOptions
    {
        public string StorageProvider { get; set; }
        public AzureStorageOptions AzureStorage { get; set; }
        public LocalStorageOptions LocalStorage { get; set; }
    }

    public class AzureStorageOptions
    {
        public string ConnectionString { get; set; }
        public string LessonsContainer { get; set; }
        public string IvtContainer { get; set; }
        public string ConstructionSiteLogbookContainer { get; set; }
        public string ResidentReportIncidence { get; set; }
        public string ProjectSubContractor { get; set; }
        public string ProjectFotosContainer { get; set; } = "project-fotos";
        public string ProjectCroquisContainer { get; set; } = "project-croquis";
        public string VecinoRequisitosContainer { get; set; } = "vecino-requisitos";
        public string VecinoEntregablesContainer { get; set; } = "vecino-entregables";
        public string VecinoPropiedadImagenesContainer { get; set; } = "vecino-propiedad-imagenes";
        public string InvoicesContainer { get; set; } = "facturas";
        public string ActasReunionContainer { get; set; } = "actas-reunion";
        public string TareosContainer { get; set; } = "tareos";
    }

    public class LocalStorageOptions
    {
        public string LessonsContainer { get; set; }
        public string IvtContainer { get; set; }
        public string ConstructionSiteLogbookContainer { get; set; }
        public string ResidentReportIncidence { get; set; }
        public string ProjectSubContractor { get; set; }
        public string ProjectFotosContainer { get; set; } = "project-fotos";
        public string ProjectCroquisContainer { get; set; } = "project-croquis";
        public string VecinoRequisitosContainer { get; set; } = "vecino-requisitos";
        public string VecinoEntregablesContainer { get; set; } = "vecino-entregables";
        public string VecinoPropiedadImagenesContainer { get; set; } = "vecino-propiedad-imagenes";
        public string InvoicesContainer { get; set; } = "facturas";
        public string ActasReunionContainer { get; set; } = "actas-reunion";
        public string TareosContainer { get; set; } = "tareos";
    }
}