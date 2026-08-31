using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.EquipoPrestado;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class EquipoPrestadoService : IEquipoPrestadoService
    {
        private readonly IEquipoPrestadoRepository _repo;

        public EquipoPrestadoService(IEquipoPrestadoRepository repo)
        {
            _repo = repo;
        }

        public Task<List<EquipoPrestadoListItemDto>> GetByAccidenteId(int accidenteId) =>
            _repo.GetByAccidenteId(accidenteId);

        public Task<List<(int Id, string Nombre)>> GetTipos() => _repo.GetTipos();

        public Task<int> Create(int accidenteId, EquipoPrestadoCreateDto dto, int? userId)
        {
            if (dto.TipoEquipoId <= 0)
                throw new AbrilException("El tipo de equipo es obligatorio.", 400);
            if (dto.Cantidad <= 0)
                throw new AbrilException("La cantidad debe ser mayor a cero.", 400);
            if (dto.FechaPrestamo == default)
                throw new AbrilException("La fecha de préstamo es obligatoria.", 400);
            return _repo.Create(accidenteId, dto, userId ?? 0);
        }

        public Task Devolver(int id, EquipoPrestadoDevolverDto dto)
        {
            if (dto.FechaDevolucion == default)
                throw new AbrilException("La fecha de devolución es obligatoria.", 400);
            return _repo.Devolver(id, dto);
        }

        public Task Delete(int id) => _repo.Delete(id);
    }
}
