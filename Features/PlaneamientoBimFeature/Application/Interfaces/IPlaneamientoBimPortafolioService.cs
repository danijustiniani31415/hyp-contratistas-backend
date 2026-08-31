using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces
{
    public interface IPlaneamientoBimPortafolioService
    {
        Task<PortafolioKpisDto> GetKpis();
        Task<List<ProyectoPortafolioDto>> GetProyectos();
        Task<byte[]> ExportarPdf(int projectId, DateOnly fecha);
    }
}
