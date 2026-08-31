namespace Abril_Backend.Features.Evaluaciones.Application.Dtos
{
    public class EvEvaluacionCreateDto
    {
        public int EvaluadoUserId { get; set; }
        public int? ProjectId { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public string? Comentario { get; set; }
        public bool NoAplica { get; set; } = false;
        public string? NoAplicaMotivo { get; set; }
        public List<EvDetalleCreateDto> Detalles { get; set; } = [];
    }

    public class EvDetalleCreateDto
    {
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int? Puntaje { get; set; }
        public bool EsNa { get; set; } = false;
    }

    public class EvEvaluacionResidenteResponseDto
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public int EvaluadorUserId { get; set; }
        public string EvaluadorNombre { get; set; } = string.Empty;
        public int EvaluadoUserId { get; set; }
        public string EvaluadoNombre { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public string? ProjectNombre { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public decimal? Nota { get; set; }
        public string? Comentario { get; set; }
        public bool NoAplica { get; set; }
        public string? NoAplicaMotivo { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<EvDetalleResponseDto> Detalles { get; set; } = [];
    }

    public class EvDetalleResponseDto
    {
        public int Id { get; set; }
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int? Puntaje { get; set; }
        public bool EsNa { get; set; }
    }

    public class ResidenteEvaluableDto
    {
        public int UserId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public string? ProjectNombre { get; set; }
        public bool PuedeVerTodos { get; set; }
        public string? Area { get; set; }
        public string? Subarea { get; set; }
    }
}
