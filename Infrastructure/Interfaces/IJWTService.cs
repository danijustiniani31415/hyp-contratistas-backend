using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IJWTService
    {
        string GenerateToken(UserDTO user);
    }
}