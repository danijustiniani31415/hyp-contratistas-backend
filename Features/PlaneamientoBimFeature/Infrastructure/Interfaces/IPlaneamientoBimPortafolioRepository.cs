using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces
{
    public interface IPlaneamientoBimPortafolioRepository
    {
        Task<PortafolioKpisDto> GetKpis();
        Task<List<ProyectoPortafolioDto>> GetProyectos();

        /// <summary>Datos crudos para el header del PDF: nombre del proyecto y fase actual. Null si el proyecto no existe.</summary>
        Task<(string ProjectNombre, string FaseActualNombre)?> GetContextoProyecto(int projectId);
    }
}
