using Abril_Backend.Features.Ssoma.Rac.Dtos;

namespace Abril_Backend.Features.Ssoma.Rac.Services;

public class NullRacNotificationService : IRacNotificationService
{
    public Task NotificarRacCreadoAsync(RacDetalleDto detalle) => Task.CompletedTask;
    public Task NotificarPenalidadAsync(RacDetalleDto racDto) => Task.CompletedTask;
}
