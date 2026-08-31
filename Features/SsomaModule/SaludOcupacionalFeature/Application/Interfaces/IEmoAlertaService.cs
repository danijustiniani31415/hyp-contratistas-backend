using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IEmoAlertaService
    {
        Task<EmoAlertaResultDto> ProcesarAlertas();

        /// <summary>
        /// Aviso adicional a 7 días CALENDARIO (no hábiles) del vencimiento, a la misma
        /// audiencia que <see cref="ProcesarAlertas"/> (trabajador + residente/responsable/coord
        /// SSOMA/coord admin del proyecto). Corre independiente del aviso por días hábiles —
        /// usa su propio TipoAlerta para no chocar con el guard de "ya enviada hoy".
        /// </summary>
        Task<EmoAlertaResultDto> ProcesarAlertas7DiasCalendario();
    }
}
