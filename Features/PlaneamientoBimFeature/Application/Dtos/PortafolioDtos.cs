namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos
{
    // ── KPIs agregados de portafolio ─────────────────────────────────────────

    public class PortafolioKpisDto
    {
        /// <summary>PPC = cumplidas/programadas sobre bim_registro_diario de TODO el portafolio, últimos 7 días.</summary>
        public decimal PpcPromedioUltimaSemana { get; set; }
        public List<ProyectosPorFaseDto> ProyectosPorFase { get; set; } = new();
        /// <summary>Proyectos con al menos una restricción sin cerrar creada hace más de 3 días.</summary>
        public int ProyectosConRestriccionesVencidas { get; set; }
        /// <summary>Pareto de causas de incumplimiento del mes calendario en curso, cross-proyecto.</summary>
        public CausasParetoDto CausasTopMes { get; set; } = new();
    }

    /// <summary>FaseId = 0 / FaseNombre = "Sin fase iniciada" agrupa proyectos sin ninguna fase con fecha_inicio cargada.</summary>
    public class ProyectosPorFaseDto
    {
        public int FaseId { get; set; }
        public string FaseNombre { get; set; } = string.Empty;
        public int CantidadProyectos { get; set; }
    }

    // ── Tabla de proyectos con semáforo ──────────────────────────────────────

    public class ProyectoPortafolioDto
    {
        public int ProjectId { get; set; }
        public string ProjectNombre { get; set; } = string.Empty;
        public int TotalRegistros { get; set; }
        public decimal CumplidosRegistros { get; set; }
        /// <summary>null = sin registros (Semaforo = "GRIS").</summary>
        public decimal? PorcentajeAvance { get; set; }
        /// <summary>"VERDE" (≥90%) | "AMARILLO" (70-89%) | "ROJO" (&lt;70%) | "GRIS" (sin registros).</summary>
        public string Semaforo { get; set; } = string.Empty;
    }
}
