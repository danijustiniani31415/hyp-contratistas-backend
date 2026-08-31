using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces
{
    public interface IPlaneamientoBimConfiguracionService
    {
        Task<ConfiguracionInicialDto> GetConfiguracion(int projectId);
        Task<List<ResponsableBimLookupDto>> GetResponsables();
        Task GuardarConfiguracion(int projectId, ConfiguracionInicialUpdateDto dto);
    }
}
