using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Application.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserDTO>> GetPagedFactory(int page, int pageSize);
        Task Create(UserCreateDTO dto);
        Task Update(int userId, UserUpdateDTO dto);
        Task ToggleActive(int userId, int updatedUserId);
    }
}