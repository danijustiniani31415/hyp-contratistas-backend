using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Dtos;

namespace Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Interfaces
{
    public interface IMicrosoftProfileService
    {
        Task<MicrosoftProfileDto?> GetProfile(string accessToken);
        Task<string?> GetPhotoBase64(string accessToken);
    }
}
