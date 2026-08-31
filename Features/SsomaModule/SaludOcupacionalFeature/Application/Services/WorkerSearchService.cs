using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class WorkerSearchService : IWorkerSearchService
    {
        private const int LimitDefault = 20;
        private const int LimitMax = 100;

        private readonly IWorkerSearchRepository _repo;
        private readonly IWorkerEmailValidator _emailValidator;

        public WorkerSearchService(IWorkerSearchRepository repo, IWorkerEmailValidator emailValidator)
        {
            _repo = repo;
            _emailValidator = emailValidator;
        }

        public Task<List<WorkerSearchResultDto>> Search(string? q, int limit, int? empresaIdContratista = null)
        {
            if (limit <= 0) limit = LimitDefault;
            if (limit > LimitMax) limit = LimitMax;
            return _repo.Search(q, limit, empresaIdContratista);
        }

        public Task<WorkerSearchResultDto?> GetByUserId(int userId, bool esContratista) => _repo.GetByUserId(userId, esContratista);

        public Task<List<DocumentTypeDto>> GetDocumentTypes() => _repo.GetDocumentTypes();

        public Task<List<WorkerCategoryDto>> GetWorkerCategories() => _repo.GetWorkerCategories();

        public Task<EmailCorporativoValidacionDto> ValidarEmailCorporativo(string? email, int? workerId, bool? esCorporativo) =>
            _emailValidator.ValidarCorporativoAsync(email, workerId, esCorporativo);

        public async Task<int> Create(WorkerCreateDto dto)
        {
            var correos = await _emailValidator.ValidarYNormalizarAsync(
                dto.EmailCorporativo, dto.EmailPersonal, workerId: null, EsCorporativo(dto.ContrataCasa, dto.ObraOficinaStaffId));

            dto.EmailCorporativo = correos.Corporativo;
            dto.EmailPersonal = correos.Personal;

            return await _repo.Create(dto);
        }

        public async Task Update(int id, WorkerUpdateDto dto, bool puedeEditarDni)
        {
            var correos = await _emailValidator.ValidarYNormalizarAsync(
                dto.EmailCorporativo, dto.EmailPersonal, id, EsCorporativo(dto.ContrataCasa, dto.ObraOficinaStaffId));

            dto.EmailCorporativo = correos.Corporativo;
            dto.EmailPersonal = correos.Personal;

            await _repo.Update(id, dto, puedeEditarDni);
        }

        public async Task UpdateDatosBasicos(int id, WorkerDatosBasicosDto dto, bool puedeEditarDni)
        {
            // Este formulario no envía la clasificación del trabajador: se deduce de la guardada.
            var correos = await _emailValidator.ValidarYNormalizarAsync(
                dto.EmailCorporativo, dto.EmailPersonal, id, esCorporativo: null);

            dto.EmailCorporativo = correos.Corporativo;
            dto.EmailPersonal = correos.Personal;

            await _repo.UpdateDatosBasicos(id, dto, puedeEditarDni);
        }

        public Task Retirar(int id) => _repo.Retirar(id);

        /// <summary>
        /// La clasificación que envía el formulario manda sobre la guardada (una edición puede
        /// mover al trabajador de Obra a Staff y viceversa).
        /// </summary>
        private static bool EsCorporativo(string? contrataCasa, int? obraOficinaStaffId) =>
            WorkerEmailValidator.EsClasificacionCorporativa(contrataCasa, obraOficinaStaffId);
    }
}
