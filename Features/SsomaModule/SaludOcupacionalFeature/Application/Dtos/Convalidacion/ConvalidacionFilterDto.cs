namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion
{
    public class ConvalidacionFilterDto
    {
        public int? WorkerId { get; set; }
        public string? Resultado { get; set; }
        public string? Search { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaDestinoId { get; set; }
        public int? TipoEmoId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }
}
