namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Dtos
{
    public class ProjectsDashboardResponseDto
    {
        /// <summary>Filtros disponibles — incluidos para evitar una segunda llamada a GET /filters.</summary>
        public ProjectsDashboardFiltersResponseDto Filtros { get; set; } = new();
        public int TotalProyectos { get; set; }
        public int AlDia { get; set; }
        public int ConRetraso { get; set; }
        public int SinActividades { get; set; }
        public double AvancePromedio { get; set; }
        public List<ProyectoDetalleDto> Proyectos { get; set; } = new();
        public List<EstadoDistribucionDto> DistribucionPorEstado { get; set; } = new();
        public List<ResponsableRankingDto> RankingResponsables { get; set; } = new();
        public List<HeatmapResponsableDto> HeatmapCarga { get; set; } = new();
    }

    public class ProyectoDetalleDto
    {
        public int ProyectoId { get; set; }
        public string? ProjectDescription { get; set; }
        public string? Estado { get; set; }
        public string? ResponsableNombre { get; set; }
        public int TotalActividades { get; set; }
        public int Culminadas { get; set; }
        public int EnProceso { get; set; }
        public int Vencidas { get; set; }
        public double AvanceProgramado { get; set; }
        public double AvanceReal { get; set; }
        public bool EstaConRetraso { get; set; }
        public int DiasRetraso { get; set; }
        public string Semaforo { get; set; } = "verde";
        public string? EtapaNombre { get; set; }
    }

    public class EstadoDistribucionDto
    {
        public string Estado { get; set; } = string.Empty;
        public int CantidadProyectos { get; set; }
    }

    public class ResponsableRankingDto
    {
        public int ResponsableId { get; set; }
        public string? Nombre { get; set; }
        public int Proyectos { get; set; }
        public int Completadas { get; set; }
        public int Vencidas { get; set; }
        public double Score { get; set; }
    }

    public class HeatmapResponsableDto
    {
        public string Responsable { get; set; } = string.Empty;
        public List<HeatmapSemanaDto> Semanas { get; set; } = new();
    }

    public class HeatmapSemanaDto
    {
        public string Semana { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class ProyectoDetailDashboardDto
    {
        public int ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public string? Estado { get; set; }
        public double AvanceProgramado { get; set; }
        public double AvanceReal { get; set; }
        public int DiasRetraso { get; set; }
        public string Semaforo { get; set; } = "verde";
        public List<ActividadCriticaDto> ActividadesVencidas { get; set; } = new();
        public List<ActividadCriticaDto> ActividadesCriticas { get; set; } = new();
        public GanttDataDto Gantt { get; set; } = new();
    }

    public class ActividadCriticaDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? ResponsableNombre { get; set; }
        public DateOnly? FinProgramado { get; set; }
        public int DiasRetraso { get; set; }
    }

    public class GanttDataDto
    {
        public List<GanttTareaDto> Tasks { get; set; } = new();
        public List<object> Links { get; set; } = new();
    }

    public class GanttTareaDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("start_date")]
        public string StartDate { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("duration")]
        public int Duration { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("progress")]
        public double Progress { get; set; }
    }
}
