using Microsoft.Extensions.Options;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Infrastructure.Services
{
    public class StorageContainerResolver : IStorageContainerResolver
    {
        private readonly StorageOptions _options;

        public StorageContainerResolver(IOptions<StorageOptions> options)
        {
            _options = options.Value;
        }

        public string GetLessonsContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.LessonsContainer,
                "local" => _options.LocalStorage.LessonsContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetIvtContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.IvtContainer,
                "local" => _options.LocalStorage.IvtContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetConstructionSiteLogbookContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.ConstructionSiteLogbookContainer,
                "local" => _options.LocalStorage.ConstructionSiteLogbookContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetResidentIncidentContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.ResidentReportIncidence,
                "local" => _options.LocalStorage.ResidentReportIncidence,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }
        public string GetProjectSubContractorContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.ProjectSubContractor,
                "local" => _options.LocalStorage.ProjectSubContractor,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetProjectFotosContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.ProjectFotosContainer,
                "local" => _options.LocalStorage.ProjectFotosContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetProjectCroquisContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.ProjectCroquisContainer,
                "local" => _options.LocalStorage.ProjectCroquisContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetVecinoRequisitosContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.VecinoRequisitosContainer,
                "local" => _options.LocalStorage.VecinoRequisitosContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetVecinoEntregablesContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.VecinoEntregablesContainer,
                "local" => _options.LocalStorage.VecinoEntregablesContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetVecinoPropiedadImagenesContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.VecinoPropiedadImagenesContainer,
                "local" => _options.LocalStorage.VecinoPropiedadImagenesContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetInvoicesContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.InvoicesContainer,
                "local" => _options.LocalStorage.InvoicesContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetActasReunionContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.ActasReunionContainer,
                "local" => _options.LocalStorage.ActasReunionContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }

        public string GetTareosContainerName()
        {
            return _options.StorageProvider.ToLower() switch
            {
                "azure" => _options.AzureStorage.TareosContainer,
                "local" => _options.LocalStorage.TareosContainer,
                _ => throw new InvalidOperationException("Proveedor de storage no válido")
            };
        }
    }
}