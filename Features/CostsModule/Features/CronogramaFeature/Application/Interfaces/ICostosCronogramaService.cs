using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Interfaces
{
    public interface ICostosCronogramaService
    {
        Task<CronogramaFormDataDto> GetFormData(int projectSubContractorId);
        Task<CronogramaActividadDto> CreateActividad(CronogramaActividadCreateDto dto, int userId);
        Task Save(int projectSubContractorId, CronogramaSaveDto dto, int userId);
    }
}
