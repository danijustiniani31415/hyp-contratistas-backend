namespace Abril_Backend.Features.Evaluaciones.Application.Dtos
{
    // ─── CREATE ────────────────────────────────────────────────────────────────
    public class EvJefeSsomaEvaluacionCreateDto
    {
        public string? Comentario { get; set; }
        public List<EvJefeSsomaDetalleCreateDto> Detalles { get; set; } = [];
    }

    public class EvJefeSsomaDetalleCreateDto
    {
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Puntaje { get; set; }
    }

    // ─── INICIO (pantalla evaluar) ──────────────────────────────────────────────
    public class EvJefeSsomaInicioDto
    {
        public EvPeriodoDto? Periodo { get; set; }
        public List<EvSupervisorContratistaCriterioDto> Plantilla { get; set; } = [];
        public bool YaEvalue { get; set; }
    }

    // ─── PENDIENTES (solo Jefe SSOMA — nunca incluye notas) ────────────────────
    public class EvJefeSsomaPendienteDto
    {
        public int UserId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string EmailCorporativo { get; set; } = string.Empty;
    }

    public class EvJefeSsomaCumplimientoDto
    {
        public int TotalEvaluadores { get; set; }
        public int TotalCompletaron { get; set; }
        public List<EvJefeSsomaPendienteDto> Pendientes { get; set; } = [];
    }

    // ─── RESULTADOS (solo Jefe SSOMA — promedio + comentarios, sin autor) ──────
    public class EvJefeSsomaResultadosDto
    {
        public EvPeriodoDto? Periodo { get; set; }
        public int TotalRespuestas { get; set; }
        public decimal? PromedioGeneral { get; set; }
        public List<EvJefeSsomaCriterioPromedioDto> PromediosPorCriterio { get; set; } = [];
        public List<string> Comentarios { get; set; } = [];
        public List<EvJefeSsomaTendenciaDto> Tendencia { get; set; } = [];
    }

    public class EvJefeSsomaCriterioPromedioDto
    {
        public string Criterio { get; set; } = string.Empty;
        public decimal Promedio { get; set; }
    }

    public class EvJefeSsomaTendenciaDto
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public decimal? Promedio { get; set; }
    }
}
