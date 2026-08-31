namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class WorkerEmoHistorialDto
    {
        public int WorkerId { get; set; }
        public string? ApellidoNombre { get; set; }
        public string? Dni { get; set; }
        public string? ContrataCasa { get; set; }
        public bool? HabilitadoObra { get; set; }
        public List<VinculacionHistorialDto> Vinculaciones { get; set; } = new();
    }

    public class VinculacionHistorialDto
    {
        public int Id { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public string? Puesto { get; set; }
        public string? TipoVinculacion { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public string? MotivoRetiro { get; set; }
        public List<EmoListItemDto> Emos { get; set; } = new();
    }
}
