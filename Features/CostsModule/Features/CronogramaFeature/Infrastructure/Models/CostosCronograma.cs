namespace Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Infrastructure.Models
{
    /// <summary>Cronograma de una adjudicación (1 vivo por project_sub_contractor).</summary>
    public class CostosCronograma
    {
        public int CostosCronogramaId { get; set; }
        public int ProjectSubContractorId { get; set; }
        public string? FileUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>Catálogo de actividades de cronograma (Costos).</summary>
    public class CostosCronogramaActividad
    {
        public int CostosCronogramaActividadId { get; set; }
        public string Nombre { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>Nodo del árbol del cronograma (jerarquía + orden + fechas).</summary>
    public class CostosCronogramaActividadNodo
    {
        public int CostosCronogramaActividadNodoId { get; set; }
        public int CostosCronogramaId { get; set; }
        public int CostosCronogramaActividadId { get; set; }
        public int? CostosCronogramaActividadNodoPadreId { get; set; }
        public int CostosCronogramaNodoOrden { get; set; }
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
    }
}
