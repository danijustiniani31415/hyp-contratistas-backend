using Abril_Backend.Features.Habilitacion.Application.Dtos;

namespace Abril_Backend.Features.Habilitacion.Application.Interfaces
{
    public interface IRetiroAutomaticoService
    {
        Task<RetiroAutomaticoResultDto> EjecutarAsync();
    }
}
