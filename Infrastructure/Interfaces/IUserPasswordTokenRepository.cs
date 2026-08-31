using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IUserPasswordTokenRepository
    {
        Task CreateAsync(UserPasswordTokenDTO token);
        Task<UserPasswordTokenDTO?> GetValidTokenAsync(string token);
        Task InvalidateTokensByUserAsync(int userId);
        Task SaveAsync();
    }
}