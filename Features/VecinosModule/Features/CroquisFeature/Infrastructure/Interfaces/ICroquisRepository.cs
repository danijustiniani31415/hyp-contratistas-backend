using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Dtos;

namespace Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Interfaces
{
    public interface ICroquisRepository
    {
        /// <summary>Todos los proyectos activos con su croquis asignado (si tienen).</summary>
        Task<List<ProjectCroquisItemDto>> GetProjectsWithCroquis(string? search);

        /// <summary>
        /// Asigna (o reemplaza) el croquis de un proyecto. Si ya existe uno activo, lo
        /// soft-deleta y crea el nuevo, manteniendo un solo registro con state = true.
        /// </summary>
        Task UpsertCroquis(int projectId, string imageUrl, string? originalFileName, int userId);

        /// <summary>Lotes activos dibujados sobre un croquis.</summary>
        Task<List<CroquisLoteDto>> GetLotes(int projectCroquisId);

        /// <summary>Reemplaza el conjunto completo de lotes de un croquis (soft-delete + insert).</summary>
        Task ReplaceLotes(int projectCroquisId, List<CroquisLoteDto> lotes, int userId);

        /// <summary>Todos los croquis registrados con sus lotes y los vecinos de su proyecto (vista de Gestión).</summary>
        Task<CroquisGestionResponseDto> GetGestion();
    }
}
