using Abril_Backend.Features.Habilitacion.Application.Dtos.ContratistaUsuarios;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IContratistaUsuarioRepository
    {
        Task<List<ContratistaUsuarioListDto>> GetUsuariosAsync(int contractorId);
        Task<SsContratistaUsuario?> GetByIdAsync(int id, int contractorId);
        Task InvitarUsuarioAsync(SsContratistaUsuario entity, List<int>? proyectoIds);
        Task ActualizarUsuarioAsync(int id, SsContratistaUsuario entity, List<int>? proyectoIds);
        Task DesactivarUsuarioAsync(int id, int contractorId);
    }
}
