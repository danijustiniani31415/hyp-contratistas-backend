using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.SctrGestion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class SctrGestionService : ISctrGestionService
    {
        private readonly ISctrGestionRepository _repo;

        public SctrGestionService(ISctrGestionRepository repo)
        {
            _repo = repo;
        }

        public Task<List<SctrGestionDto>> GetByCasoSocialId(Guid casoSocialId) =>
            _repo.GetByCasoSocialId(casoSocialId);

        public Task<int> Create(Guid casoSocialId, SctrGestionCreateDto dto, int? userId)
        {
            if (dto.EstadoId <= 0)
                throw new AbrilException("El estado del SCTR es obligatorio.", 400);
            return _repo.Create(casoSocialId, dto, userId ?? 0);
        }

        public Task Update(int id, SctrGestionUpdateDto dto)
        {
            if (dto.EstadoId <= 0)
                throw new AbrilException("El estado del SCTR es obligatorio.", 400);
            return _repo.Update(id, dto);
        }

        public Task Delete(int id) => _repo.Delete(id);
    }
}
