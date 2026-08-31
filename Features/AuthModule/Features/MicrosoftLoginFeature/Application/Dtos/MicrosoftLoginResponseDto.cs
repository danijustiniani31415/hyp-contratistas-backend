namespace Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Dtos
{
    public class MicrosoftLoginResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string SessionToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
        public string UserPrincipalName { get; set; } = null!;
        public string? Mail { get; set; }
        public string? JobTitle { get; set; }
        public string? OfficeLocation { get; set; }
        public string? MobilePhone { get; set; }
        public List<string> BusinessPhones { get; set; } = new();
        public string? Department { get; set; }
        public string? PhotoBase64 { get; set; }
        public List<string> AllowedFeatures { get; set; } = new();
    }
}
