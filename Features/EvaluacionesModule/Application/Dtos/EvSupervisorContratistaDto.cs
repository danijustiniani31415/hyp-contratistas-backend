namespace Abril_Backend.Features.Evaluaciones.Application.Dtos
{
    // ─── CREATE ────────────────────────────────────────────────────────────────
    public class EvSupervisorContratistaEvaluacionCreateDto
    {
        public int SupervisorSsContratistaUsuarioId { get; set; }
        public int ProyectoId { get; set; }
        public string? Comentario { get; set; }
        public List<EvSupervisorContratistaDetalleCreateDto> Detalles { get; set; } = [];
    }

    public class EvSupervisorContratistaDetalleCreateDto
    {
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int? Puntaje { get; set; }
        public bool EsNa { get; set; } = false;
    }

    public class EvSupervisorContratistaNoAplicaCreateDto
    {
        public string Motivo { get; set; } = string.Empty;
        public int? ProyectoId { get; set; }
        public int? SupervisorSsContratistaUsuarioId { get; set; }
    }

    // ─── INICIO (pantalla evaluar) ──────────────────────────────────────────────
    public class EvSupervisorContratistaInicioDto
    {
        public EvPeriodoDto? Periodo { get; set; }
        public List<EvSupervisorContratistaCriterioDto> Plantilla { get; set; } = [];
        public List<EvSupervisorContratistaAEvaluarDto> SupervisoresAEvaluar { get; set; } = [];
        public bool YaMarcoNoAplica { get; set; }
    }

    public class EvSupervisorContratistaCriterioDto
    {
        public int Id { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class EvSupervisorContratistaAEvaluarDto
    {
        public int SupervisorSsContratistaUsuarioId { get; set; }
        public string SupervisorNombre { get; set; } = string.Empty;
        public int ContributorId { get; set; }
        public string ContributorNombre { get; set; } = string.Empty;
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public bool YaEvalue { get; set; }
        public decimal? NotaPrevia { get; set; }

        // Para poder editarla mientras el período siga abierto (ver EvSupervisorContratistaController.Update).
        public int? EvaluacionId { get; set; }
        public string? ComentarioPrevio { get; set; }
        public List<EvSupervisorContratistaDetallePrevioDto> DetallesPrevios { get; set; } = [];
    }

    public class EvSupervisorContratistaDetallePrevioDto
    {
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int? Puntaje { get; set; }
        public bool EsNa { get; set; }
    }

    // ─── MI PERFIL (el propio supervisor/prevencionista de la contratista — sin
    //     identidad de quién lo calificó, igual que EvPrevencionistaMiPerfilDto) ─
    public class EvSupervisorContratistaMiPerfilDto
    {
        public decimal? PromedioGeneral { get; set; }
        public int TotalEvaluaciones { get; set; }
        public List<string> Comentarios { get; set; } = [];
    }

    // ─── VER EVALUACIONES / DASHBOARD (solo Jefe SSOMA) ────────────────────────
    public class EvSupervisorContratistaVerInicioDto
    {
        public List<EvPeriodoDto> Periodos { get; set; } = [];
        public List<EvSupervisorContratistaProyectoFiltroDto> Proyectos { get; set; } = [];
        public List<EvSupervisorContratistaResumenDto> Evaluaciones { get; set; } = [];
    }

    public class EvSupervisorContratistaProyectoFiltroDto
    {
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
    }

    public class EvSupervisorContratistaResumenDto
    {
        public int EvaluacionId { get; set; }
        public int SupervisorSsContratistaUsuarioId { get; set; }
        public string SupervisorNombre { get; set; } = string.Empty;
        public int ContributorId { get; set; }
        public string ContributorNombre { get; set; } = string.Empty;
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public string EvaluadorNombre { get; set; } = string.Empty;
        public decimal? Nota { get; set; }
        public string? Comentario { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EvSupervisorContratistaDashboardDto
    {
        public int TotalEvaluaciones { get; set; }
        public decimal? PromedioGeneral { get; set; }
        public List<EvSupervisorContratistaResumenDto> Evaluaciones { get; set; } = [];
    }
}
