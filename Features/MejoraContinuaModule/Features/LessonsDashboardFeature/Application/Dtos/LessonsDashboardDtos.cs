namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Dtos
{
    public class ChartItemDTO
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class PhaseStageChartDTO
    {
        public int PhaseId { get; set; }
        public string PhaseLabel { get; set; } = string.Empty;
        public List<ChartItemDTO> Stages { get; set; } = new();
    }

    /// <summary>Tarjetas resumen del dashboard.</summary>
    public class DashboardSummaryDTO
    {
        public int TotalLessons { get; set; }
        public int TotalProjects { get; set; }     // proyectos distintos con lecciones
        public int TotalAreas { get; set; }        // áreas (lesson_area) distintas con lecciones
        public int TotalPhases { get; set; }       // fases distintas con lecciones
        public int TotalUsers { get; set; }        // usuarios distintos que registraron lecciones
    }

    public class LessonsDashboardDataDTO
    {
        public DashboardSummaryDTO Summary { get; set; } = new();
        public List<ChartItemDTO> LessonsByMonth { get; set; } = new();
        public List<ChartItemDTO> LessonsByProject { get; set; } = new();
        public List<ChartItemDTO> LessonsByArea { get; set; } = new();
        public List<ChartItemDTO> LessonsByUser { get; set; } = new();
        public List<ChartItemDTO> LessonsByPhase { get; set; } = new();
        public List<PhaseStageChartDTO> LessonsByPhaseAndStage { get; set; } = new();
        public List<ChartItemDTO> LessonsBySubStage { get; set; } = new();
        /// <summary>
        /// Lecciones por Obra / Staff / Oficina Central. Antes esta apertura salia del
        /// ultimo nodo del arbol de areas (tipo Obra_Oficina); ahora viene de
        /// lesson.obra_oficina_staff_id.
        /// </summary>
        public List<ChartItemDTO> LessonsByObraOficina { get; set; } = new();

        // Usuarios que debían registrar lecciones (user_project) y no lo han hecho en el período.
        public string PendingPeriodLabel { get; set; } = string.Empty;
        public List<PendingUserDTO> PendingUsers { get; set; } = new();
    }

    public class PendingUserDTO
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public List<string> Projects { get; set; } = new();
    }

    public class DashboardPeriodDTO
    {
        public DateTimeOffset? PeriodDate { get; set; }
    }

    public class DashboardUserDTO
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
    }

    public class DashboardAreaDTO
    {
        public int LessonAreaId { get; set; }
        public string AreaDescription { get; set; } = string.Empty;
    }

    public class DashboardProjectDTO
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = string.Empty;
    }

    public class DashboardObraOficinaStaffDTO
    {
        public int ObraOficinaStaffId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LessonsDashboardFiltersDTO
    {
        public List<DashboardPeriodDTO> Periods { get; set; } = new();
        public List<DashboardUserDTO> Users { get; set; } = new();
        public List<DashboardAreaDTO> Areas { get; set; } = new();
        public List<DashboardProjectDTO> Projects { get; set; } = new();
        /// <summary>Opciones del filtro Obra / Staff / Oficina Central.</summary>
        public List<DashboardObraOficinaStaffDTO> ObraOficinaStaff { get; set; } = new();
    }
}
