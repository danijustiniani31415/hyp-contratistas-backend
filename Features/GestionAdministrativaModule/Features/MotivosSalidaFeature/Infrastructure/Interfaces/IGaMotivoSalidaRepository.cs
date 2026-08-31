using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Infrastructure.Interfaces
{
    public interface IGaMotivoSalidaRepository
    {
        Task<List<GaMotivoSalidaConfigItemDto>> GetAll();
        Task Create(GaMotivoSalidaCreateDto dto);
        Task<bool> Toggle(int id);
        Task Edit(int id, GaMotivoSalidaEditDto dto);
    }
}
