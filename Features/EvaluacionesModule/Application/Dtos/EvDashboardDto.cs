namespace Abril_Backend.Features.Evaluaciones.Application.Dtos
{
    public class EvResidenteResumenDto
    {
        public int UserId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public string? ProjectNombre { get; set; }
        public decimal? PromedioGeneral { get; set; }
        public decimal? PromedioMesAnterior { get; set; }
        public List<EvAreaPromedioDto> PromediosPorArea { get; set; } = [];
        public List<EvEvaluacionResidenteResponseDto> Evaluaciones { get; set; } = [];
    }

    public class EvAreaPromedioDto
    {
        public string AreaNombre { get; set; } = string.Empty;
        public decimal? Promedio { get; set; }
        public int TotalEvaluaciones { get; set; }
    }

    public class EvDashboardGerenciaDto
    {
        public decimal? PromedioGeneral { get; set; }
        public int TotalResidentes { get; set; }
        public int EvaluacionesCompletadas { get; set; }
        public int EvaluacionesEsperadas { get; set; }
        public int BajoRendimiento { get; set; }
        public List<EvResidenteResumenDto> Residentes { get; set; } = [];
        public List<EvAreaPromedioDto> PromediosPorArea { get; set; } = [];
        public List<EvEvaluacionResidenteResponseDto> UltimosComentarios { get; set; } = [];
        public List<EvTendenciaDto> Tendencia { get; set; } = [];
    }

    public class EvTendenciaDto
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal? Promedio { get; set; }
    }

    public class EvPendienteDto
    {
        public int UserId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> AreasAsignadas { get; set; } = [];
        public List<string> AreasCompletadas { get; set; } = [];
        public List<string> AreasPendientes { get; set; } = [];
    }
}
