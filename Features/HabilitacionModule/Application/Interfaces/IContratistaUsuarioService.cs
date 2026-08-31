using Abril_Backend.Features.Habilitacion.Application.Dtos.ContratistaUsuarios;

namespace Abril_Backend.Features.Habilitacion.Application.Interfaces
{
    public interface IContratistaUsuarioService
    {
        Task<List<ContratistaUsuarioListDto>> GetUsuariosAsync(int contractorId);
        Task InvitarUsuarioAsync(int contractorId, ContratistaUsuarioCreateDto dto, int creadoPor);
        Task ActualizarUsuarioAsync(int id, int contractorId, ContratistaUsuarioUpdateDto dto);
        Task DesactivarUsuarioAsync(int id, int contractorId);
    }
}
