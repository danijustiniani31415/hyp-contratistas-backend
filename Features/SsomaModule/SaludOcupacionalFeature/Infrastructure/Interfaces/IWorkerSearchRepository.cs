using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface IWorkerSearchRepository
    {
        Task<List<WorkerSearchResultDto>> Search(string? q, int limit, int? empresaIdContratista = null);
        Task<WorkerSearchResultDto?> GetByUserId(int userId, bool esContratista);
        Task<List<DocumentTypeDto>> GetDocumentTypes();
        Task<List<WorkerCategoryDto>> GetWorkerCategories();

        /// <summary>
        /// Resuelve en un solo roundtrip la clasificación/correo actual del trabajador
        /// <paramref name="workerId"/> (null si aún no existe) y el trabajador no retirado que
        /// ya tiene asignado <paramref name="emailNormalizado"/> (minúsculas, sin espacios).
        /// </summary>
        Task<EmailCorporativoContextoDto> GetContextoEmailCorporativo(string? emailNormalizado, int? workerId);

        Task<int> Create(WorkerCreateDto dto);
        Task Update(int id, WorkerUpdateDto dto, bool puedeEditarDni);
        Task UpdateDatosBasicos(int id, WorkerDatosBasicosDto dto, bool puedeEditarDni);
        Task Retirar(int id);
    }
}
