using Abril_Backend.Features.Habilitacion.Application.Dtos.Responsables;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IResponsablesRepository
    {
        Task<ResponsablesDto> GetAll();
        Task UpdateRazonSocial(int contributorId, ResponsableRazonSocialUpdateDto dto);
        Task UpdateProyecto(int projectId, ResponsableProyectoUpdateDto dto);
    }
}
