using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<RoleSimpleDTO>> GetAllAsync();
    }
}
