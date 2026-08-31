using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IWorkerSearchService
    {
        Task<List<WorkerSearchResultDto>> Search(string? q, int limit, int? empresaIdContratista = null);
        Task<WorkerSearchResultDto?> GetByUserId(int userId, bool esContratista);
        Task<List<DocumentTypeDto>> GetDocumentTypes();
        Task<List<WorkerCategoryDto>> GetWorkerCategories();

        /// <summary>
        /// Verificación en vivo del correo corporativo desde el formulario (no lanza excepciones).
        /// <paramref name="esCorporativo"/> lo envía el formulario cuando aún no existe el
        /// trabajador; si es null se deduce de la clasificación guardada del <paramref name="workerId"/>.
        /// </summary>
        Task<EmailCorporativoValidacionDto> ValidarEmailCorporativo(string? email, int? workerId, bool? esCorporativo);

        Task<int> Create(WorkerCreateDto dto);
        Task Update(int id, WorkerUpdateDto dto, bool puedeEditarDni);
        Task UpdateDatosBasicos(int id, WorkerDatosBasicosDto dto, bool puedeEditarDni);
        Task Retirar(int id);
    }
}
