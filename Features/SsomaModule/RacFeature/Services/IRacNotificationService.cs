using Abril_Backend.Features.Ssoma.Rac.Dtos;

namespace Abril_Backend.Features.Ssoma.Rac.Services;

public interface IRacNotificationService
{
    Task NotificarRacCreadoAsync(RacDetalleDto detalle);
    Task NotificarPenalidadAsync(RacDetalleDto racDto);
}
