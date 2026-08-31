using System.Text.Json.Serialization;
namespace Abril_Backend.Shared.Services.Reniec.Dtos
{
    public class ReniecPersonDto
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = null!;

        [JsonPropertyName("first_last_name")]
        public string FirstLastName { get; set; } = null!;

        [JsonPropertyName("second_last_name")]
        public string SecondLastName { get; set; } = null!;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = null!;

        [JsonPropertyName("document_number")]
        public string DocumentNumber { get; set; } = null!;
    }
}