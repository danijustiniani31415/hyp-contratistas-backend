using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces
{
    public interface IPlaneamientoBimDashboardRepository
    {
        Task<AvanceProyectoDto?> GetAvance(int projectId, DateOnly? desde, DateOnly? hasta);
        Task<PpcHistoricoDto?> GetPpcHistorico(int projectId, DateOnly? desde, DateOnly? hasta);
        Task<List<MetaSemanalDto>?> GetMetasSemanales(int projectId);
        Task GuardarMetasSemanales(int projectId, MetaSemanalUpdateDto dto, int userId);
        Task<List<PlanMaestroSemanaDto>?> GetPlanMaestro(int projectId);
        Task<CausasParetoDto?> GetCausasPareto(int projectId, DateOnly? desde, DateOnly? hasta);
    }
}
