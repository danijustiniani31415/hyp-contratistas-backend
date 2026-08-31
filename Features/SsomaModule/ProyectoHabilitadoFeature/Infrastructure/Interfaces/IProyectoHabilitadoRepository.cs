using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Infrastructure.Interfaces
{
    public interface IProyectoHabilitadoRepository
    {
        Task<List<ProyectoHabilitadoListDto>> GetTodosAsync();
        Task<List<ProyectoSsomaSimpleDto>> GetHabilitadosAsync();
        Task SetHabilitadoAsync(int proyectoId, bool habilitado, int userId);
    }
}
