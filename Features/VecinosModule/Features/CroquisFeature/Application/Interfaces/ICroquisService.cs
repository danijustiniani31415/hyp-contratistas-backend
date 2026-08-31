using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Dtos;

namespace Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Interfaces
{
    public interface ICroquisService
    {
        Task<List<ProjectCroquisItemDto>> GetProjectsWithCroquis(string? search);

        /// <summary>Sube la imagen del croquis y la asigna al proyecto. Devuelve la URL guardada.</summary>
        Task<string> AssignCroquis(int projectId, IFormFile file, int userId);

        Task<List<CroquisLoteDto>> GetLotes(int projectCroquisId);

        Task SaveLotes(int projectCroquisId, List<CroquisLoteDto> lotes, int userId);

        Task<CroquisGestionResponseDto> GetGestion();
    }
}
