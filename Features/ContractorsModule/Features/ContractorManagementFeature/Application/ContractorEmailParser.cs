using System.Text.Json;
using Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos;

namespace Abril_Backend.Features.Contractors.ContractorManagement.Application
{
    /// <summary>
    /// Deserializa la lista de correos enviada en el campo EmailsJson del multipart/form-data.
    /// Cada elemento: { contractorEmailId: number|null, email: string, active: bool }.
    /// </summary>
    public static class ContractorEmailParser
    {
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

        public static List<ContractorEmailItemDto> Parse(string? emailsJson)
        {
            if (string.IsNullOrWhiteSpace(emailsJson))
                return new();

            try
            {
                return JsonSerializer.Deserialize<List<ContractorEmailItemDto>>(emailsJson, _options) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }
    }
}
