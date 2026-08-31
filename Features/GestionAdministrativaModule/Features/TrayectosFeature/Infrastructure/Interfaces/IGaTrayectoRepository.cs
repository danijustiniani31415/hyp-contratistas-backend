using Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.Trayectos.Infrastructure.Interfaces
{
    public interface IGaTrayectoRepository
    {
        Task<List<GaTrayectoListItemDto>> GetAll();
        Task<List<GaTrayectoLugarOptionDto>> GetLugaresActivos();
        Task Create(GaTrayectoCreateDto dto);
        Task<bool> Toggle(int id);
        Task Edit(int id, GaTrayectoEditDto dto);
    }
}
