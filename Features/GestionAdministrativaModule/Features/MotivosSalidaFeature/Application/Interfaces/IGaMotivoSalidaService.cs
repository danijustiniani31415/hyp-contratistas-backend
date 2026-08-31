using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Interfaces
{
    public interface IGaMotivoSalidaService
    {
        Task<List<GaMotivoSalidaConfigItemDto>> GetAll();
        Task Create(GaMotivoSalidaCreateDto dto);
        Task<bool> Toggle(int id);
        Task Edit(int id, GaMotivoSalidaEditDto dto);
    }
}
