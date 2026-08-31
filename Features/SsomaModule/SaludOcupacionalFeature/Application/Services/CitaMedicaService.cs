using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CitaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class CitaMedicaService : ICitaMedicaService
    {
        private readonly ICitaMedicaRepository _repo;

        public CitaMedicaService(ICitaMedicaRepository repo)
        {
            _repo = repo;
        }

        public Task<List<CitaMedicaListItemDto>> GetByAccidenteId(int accidenteId) =>
            _repo.GetByAccidenteId(accidenteId);

        public Task<List<(int Id, string Nombre)>> GetTipos() => _repo.GetTipos();

        public Task<int> Create(int accidenteId, CitaMedicaCreateDto dto, int? userId)
        {
            if (dto.TipoId <= 0)
                throw new AbrilException("El tipo de cita es obligatorio.", 400);
            if (dto.FechaCita == default)
                throw new AbrilException("La fecha de cita es obligatoria.", 400);
            return _repo.Create(accidenteId, dto, userId ?? 0);
        }

        public Task Update(int id, CitaMedicaUpdateDto dto)
        {
            if (dto.TipoId <= 0)
                throw new AbrilException("El tipo de cita es obligatorio.", 400);
            return _repo.Update(id, dto);
        }

        public Task Delete(int id) => _repo.Delete(id);
    }
}
