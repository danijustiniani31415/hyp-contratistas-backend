using System.Net.Http.Headers;
using System.Net.Http.Json;
using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Dtos;
using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Interfaces;

namespace Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Services
{
    public class MicrosoftProfileService : IMicrosoftProfileService
    {
        private readonly HttpClient _httpClient;

        public MicrosoftProfileService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<MicrosoftProfileDto?> GetProfile(string accessToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                "v1.0/me?$select=id,displayName,givenName,surname,userPrincipalName,mail,jobTitle,officeLocation,mobilePhone,businessPhones,department");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<MicrosoftProfileDto>();
        }

        public async Task<string?> GetPhotoBase64(string accessToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "v1.0/me/photo/$value");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
