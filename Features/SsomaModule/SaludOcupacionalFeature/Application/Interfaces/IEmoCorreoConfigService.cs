using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IEmoCorreoConfigService
    {
        /// <summary>Toda la matriz de los 4 correos en una sola petición.</summary>
        Task<EmoCorreosConfigDto> GetConfig();

        Task<int> CrearAdicional(EmoCorreoAdicionalCreateDto dto);

        Task ActualizarDestinatario(int id, EmoCorreoDestinatarioUpdateDto dto);

        Task SetReglaActive(int reglaId, bool active);

        Task EliminarAdicional(int id);
    }
}
