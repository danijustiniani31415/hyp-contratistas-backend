using Abril_Backend.Features.Contractors.ContractorRegistration.Application.Dtos;

namespace Abril_Backend.Features.Contractors.ContractorRegistration.Infrastructure.Interfaces
{
    public interface IContractorRegistrationRepository
    {
        Task<List<ContractorPersonTypeDto>> GetPersonTypes();

        /// <summary>Estado del RUC frente al registro (existencia, contratista activo y conteos).</summary>
        Task<ContractorRucStatusDto> GetRucStatusAsync(string ruc);

        /// <summary>Registro nuevo: crea contributor + contractor en espera. Usar solo si el RUC no existe.</summary>
        Task CreateNew(ContributorCreateDto dto, int? userId, string? logoUrl, string? brochureUrl, string? fichaRucUrl, string? referencesUrl);

        /// <summary>Solicitud de actualización sobre un contratista APROBADO: guarda staging y pasa el contratista a estado 4.</summary>
        Task CreateUpdateRequest(int contractorId, ContributorCreateDto dto, int? userId, string? logoUrl, string? brochureUrl, string? fichaRucUrl, string? referencesUrl);

        /// <summary>Sobrescribe en sitio los datos (contratista pendiente/rechazado o sin activo) dejándolo en espera.</summary>
        Task OverwriteOrCreateDirect(int contributorId, int? existingContractorId, ContributorCreateDto dto, int? userId, string? logoUrl, string? brochureUrl, string? fichaRucUrl, string? referencesUrl);
    }
}
