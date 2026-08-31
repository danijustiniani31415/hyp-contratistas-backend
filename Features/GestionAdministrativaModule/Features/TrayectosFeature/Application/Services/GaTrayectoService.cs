using Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Infrastructure.Interfaces;

namespace Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Services
{
    public class GaTrayectoService : IGaTrayectoService
    {
        private readonly IGaTrayectoRepository _repo;

        public GaTrayectoService(IGaTrayectoRepository repo)
        {
            _repo = repo;
        }

        public Task<List<GaTrayectoListItemDto>> GetAll()        => _repo.GetAll();
        public Task<List<GaTrayectoLugarOptionDto>> GetLugaresActivos() => _repo.GetLugaresActivos();
        public Task Create(GaTrayectoCreateDto dto)              => _repo.Create(dto);
        public Task<bool> Toggle(int id)                         => _repo.Toggle(id);
        public Task Edit(int id, GaTrayectoEditDto dto)          => _repo.Edit(id, dto);
    }
}
