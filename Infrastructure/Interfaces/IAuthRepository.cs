using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IAuthRepository
    {
        Task<UserDTO?> ValidateUserAsync(string email, string password);
        Task<UserSession> CreateSessionAsync(int userId);
        Task<(int UserId, string Email)?> GetUserByEmailAsync(string email);
        Task<(int UserId, string Email)?> GetUserByIdAsync(int userId);
        Task<int?> GetUserIdByValidSessionAsync(string sessionToken);
        Task<UserDTO?> GetUserForTokenAsync(int userId);
        Task<List<string>> GetAllowedFeaturesAsync(int userId);
    }
}