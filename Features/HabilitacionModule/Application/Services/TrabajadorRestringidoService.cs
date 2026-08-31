using Abril_Backend.Features.Habilitacion.Application.Dtos.Restringidos;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Habilitacion.Application.Services
{
    public class TrabajadorRestringidoService : ITrabajadorRestringidoService
    {
        private readonly ITrabajadorRestringidoRepository _repo;

        public TrabajadorRestringidoService(ITrabajadorRestringidoRepository repo)
        {
            _repo = repo;
        }

        public Task<bool> EstaRestringidoPorDniAsync(string? dni) =>
            _repo.EstaRestringidoPorDniAsync(dni);

        public Task<List<TrabajadorRestringidoListDto>> GetAllAsync(bool soloActivos = true, string? dni = null, bool incluirDescansoMedico = false) =>
            _repo.GetAllAsync(soloActivos, dni, incluirDescansoMedico);

        public Task<TrabajadorRestringidoListDto> CreateAsync(TrabajadorRestringidoCreateDto dto, int? userId = null) =>
            _repo.CreateAsync(dto, userId);

        public Task DesactivarAsync(int id) =>
            _repo.DesactivarAsync(id);

        public Task DesactivarPorWorkerIdAsync(int workerId) =>
            _repo.DesactivarPorWorkerIdAsync(workerId);
    }
}
