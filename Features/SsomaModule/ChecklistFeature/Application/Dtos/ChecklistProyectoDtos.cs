namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Dtos
{
    // Resumen de todos los checklists de un proyecto (para indicadores/dashboard)
    public class ChecklistProyectoResumenDto
    {
        public int ProyectoId { get; set; }
        public List<ChecklistProyectoCardDto> Checklists { get; set; } = new();
    }

    public class ChecklistProyectoCardDto
    {
        public int ChecklistProyectoId { get; set; }
        public int PlantillaId { get; set; }
        public string NombrePlantilla { get; set; } = null!;
        public bool EsObligatorio { get; set; }
        public string Estado { get; set; } = null!;
        public decimal PorcentajeCompletado { get; set; }
        public int TotalItems { get; set; }
        public int ItemsCompletados { get; set; }
        public DateTimeOffset FechaActivacion { get; set; }
        public DateTimeOffset? FechaCompletado { get; set; }
        public string? ActivadoPor { get; set; }
    }

    // Detalle completo de un checklist de proyecto con sus items
    public class ChecklistProyectoDetalleDto
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = null!;
        public int PlantillaId { get; set; }
        public string NombrePlantilla { get; set; } = null!;
        public bool EsObligatorio { get; set; }
        public string Estado { get; set; } = null!;
        public decimal PorcentajeCompletado { get; set; }
        public DateTimeOffset FechaActivacion { get; set; }
        public DateTimeOffset? FechaCompletado { get; set; }
        public List<ChecklistProyectoItemDto> Items { get; set; } = new();
    }

    public class ChecklistProyectoItemDto
    {
        public int Id { get; set; }
        public int PlantillaItemId { get; set; }
        public string Descripcion { get; set; } = null!;
        public int Orden { get; set; }
        public bool TieneAdjuntoRef { get; set; }
        public bool Completado { get; set; }
        public DateTimeOffset? FechaCompletado { get; set; }
        public string? CompletadoPor { get; set; }
        public string? Observacion { get; set; }
        public string? UrlAdjunto { get; set; }
    }

    // Para activar manualmente un checklist en un proyecto
    public class ChecklistActivarDto
    {
        public int PlantillaId { get; set; }
    }

    // Para marcar/desmarcar un item
    public class ChecklistItemToggleDto
    {
        public bool Completado { get; set; }
        public string? Observacion { get; set; }
        public string? UrlAdjunto { get; set; }
    }
}
