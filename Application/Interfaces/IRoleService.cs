using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Application.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleSimpleDTO>> GetAllAsync();
    }
}
