using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule.Application.Interfaces
{
    public interface ITareoService
    {
        Task<TareoMesResponseDto> GetMes(int anio, int mes);
        Task GuardarMes(TareoGuardarDto dto, long? registradoPor);
    }
}
