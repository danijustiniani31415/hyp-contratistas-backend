using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IEmoResumenDiarioService
    {
        Task<EmoResumenDiarioResultDto> EnviarResumenDiario();
    }
}
