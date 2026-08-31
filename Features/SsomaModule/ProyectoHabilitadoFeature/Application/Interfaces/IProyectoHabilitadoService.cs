using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Interfaces
{
    public interface IProyectoHabilitadoService
    {
        Task<List<ProyectoHabilitadoListDto>> GetTodosAsync();
        Task<List<ProyectoSsomaSimpleDto>> GetHabilitadosAsync();
        Task SetHabilitadoAsync(int proyectoId, bool habilitado, int userId);
    }
}
