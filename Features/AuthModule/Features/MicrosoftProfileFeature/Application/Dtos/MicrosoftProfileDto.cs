using System.Text.Json.Serialization;

namespace Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Dtos
{
    public class MicrosoftProfileDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = null!;

        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("surname")]
        public string? Surname { get; set; }

        [JsonPropertyName("userPrincipalName")]
        public string UserPrincipalName { get; set; } = null!;

        [JsonPropertyName("mail")]
        public string? Mail { get; set; }

        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }

        [JsonPropertyName("officeLocation")]
        public string? OfficeLocation { get; set; }

        [JsonPropertyName("mobilePhone")]
        public string? MobilePhone { get; set; }

        [JsonPropertyName("businessPhones")]
        public List<string> BusinessPhones { get; set; } = new();

        [JsonPropertyName("department")]
        public string? Department { get; set; }
    }
}
