using Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Dtos;

namespace Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Interfaces
{
    public interface IMicrosoftLoginService
    {
        Task<MicrosoftLoginResponseDto> Login(string graphAccessToken);
    }
}
