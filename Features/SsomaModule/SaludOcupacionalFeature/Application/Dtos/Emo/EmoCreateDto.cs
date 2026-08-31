using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoCreateDto
    {
        public int WorkerId { get; set; }
        public int? TipoEmoId { get; set; }
        public int? EmpresaOrigenId { get; set; }
        public DateOnly FechaEmo { get; set; }
        public int? ClinicaId { get; set; }
        public int? MedicoId { get; set; }
        public string? Aptitud { get; set; }
        public bool RequiereInterconsulta { get; set; }
        public string? NumeroInforme { get; set; }
        public DateOnly? FechaLectura { get; set; }
        public string? UrlResultado { get; set; }
        public bool RequiereLecturaAbril { get; set; }
        public string? Notas { get; set; }
        public string? InterconsultaInlineJson { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public InterconsultaInlineDto? InterconsultaInline =>
            string.IsNullOrEmpty(InterconsultaInlineJson)
                ? null
                : JsonSerializer.Deserialize<InterconsultaInlineDto>(InterconsultaInlineJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        public List<EmoExamenDto> Examenes { get; set; } = new();
        public List<EmoRestriccionDto> Restricciones { get; set; } = new();

        [System.Text.Json.Serialization.JsonIgnore]
        public IFormFile? DocumentoInterconsulta { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public IFormFile? ArchivoLectura { get; set; }
    }
}
