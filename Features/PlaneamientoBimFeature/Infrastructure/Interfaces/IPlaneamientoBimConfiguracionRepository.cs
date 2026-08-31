using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces
{
    public interface IPlaneamientoBimConfiguracionRepository
    {
        Task<ConfiguracionInicialDto?> GetConfiguracion(int projectId);
        Task<List<ResponsableBimLookupDto>> GetResponsables();
        Task GuardarConfiguracion(int projectId, ConfiguracionInicialUpdateDto dto);
    }
}
