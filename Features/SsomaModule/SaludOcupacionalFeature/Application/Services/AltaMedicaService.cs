using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AltaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class AltaMedicaService : IAltaMedicaService
    {
        private readonly IAltaMedicaRepository _repo;

        public AltaMedicaService(IAltaMedicaRepository repo)
        {
            _repo = repo;
        }

        public Task<AltaMedicaDto?> GetByAccidenteId(int accidenteId) =>
            _repo.GetByAccidenteId(accidenteId);

        public Task<List<(int Id, string Nombre)>> GetTipos() => _repo.GetTipos();

        public Task<int> Create(int accidenteId, AltaMedicaCreateDto dto, int? userId)
        {
            if (dto.TipoId <= 0)
                throw new AbrilException("El tipo de alta es obligatorio.", 400);
            if (dto.FechaAlta == default)
                throw new AbrilException("La fecha de alta es obligatoria.", 400);
            if (dto.TieneRestriccion && string.IsNullOrWhiteSpace(dto.DescripcionRestriccion))
                throw new AbrilException("Debe describir la restricción cuando el alta tiene restricción.", 400);
            return _repo.Create(accidenteId, dto, userId ?? 0);
        }

        public Task Update(int accidenteId, AltaMedicaUpdateDto dto)
        {
            if (dto.TipoId <= 0)
                throw new AbrilException("El tipo de alta es obligatorio.", 400);
            if (dto.FechaAlta == default)
                throw new AbrilException("La fecha de alta es obligatoria.", 400);
            if (dto.TieneRestriccion && string.IsNullOrWhiteSpace(dto.DescripcionRestriccion))
                throw new AbrilException("Debe describir la restricción cuando el alta tiene restricción.", 400);
            return _repo.Update(accidenteId, dto);
        }

        public Task Delete(int accidenteId) => _repo.Delete(accidenteId);
    }
}
