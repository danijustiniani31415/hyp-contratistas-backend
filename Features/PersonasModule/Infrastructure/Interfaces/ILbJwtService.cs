using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Interfaces
{
    public interface ILbJwtService
    {
        string GenerateToken(LbLoginResponseDto usuario);
    }
}
