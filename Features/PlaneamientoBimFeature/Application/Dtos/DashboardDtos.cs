namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos
{
    // ── Avance por zona/nivel/sector ────────────────────────────────────────

    /// <summary>% avance = suma de PorcentajeAvance de los registros / celdas con registro (ausente =
    /// "sin cargar", no cuenta). CumplidosRegistros ahora es decimal: generaliza el viejo conteo de
    /// Cumplida=true a una suma de porcentajes — con datos migrados (100/0) da el mismo número.</summary>
    public class AvanceProyectoDto
    {
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        public int TotalRegistros { get; set; }
        public decimal CumplidosRegistros { get; set; }
        public decimal PorcentajeAvance { get; set; }
        public List<ZonaAvanceDto> Zonas { get; set; } = new();
    }

    public class ZonaAvanceDto
    {
        public int ZonaId { get; set; }
        public string ZonaNombre { get; set; } = string.Empty;
        public int TotalRegistros { get; set; }
        public decimal CumplidosRegistros { get; set; }
        public decimal PorcentajeAvance { get; set; }
        public List<CeldaAvanceDto> Celdas { get; set; } = new();
    }

    /// <summary>Una fila por combinación (nivel, sector) con al menos un registro en el rango.</summary>
    public class CeldaAvanceDto
    {
        public int NivelId { get; set; }
        public string NivelNombre { get; set; } = string.Empty;
        public int SectorId { get; set; }
        public string SectorNombre { get; set; } = string.Empty;
        public int TotalRegistros { get; set; }
        public decimal CumplidosRegistros { get; set; }
        public decimal PorcentajeAvance { get; set; }
    }

    // ── PPC histórico ────────────────────────────────────────────────────────

    public class PpcHistoricoDto
    {
        public decimal? MetaPpc { get; set; }
        public List<PpcDiaDto> Dias { get; set; } = new();
    }

    public class PpcDiaDto
    {
        public DateOnly Fecha { get; set; }
        public int TotalProgramadas { get; set; }
        public decimal Cumplidas { get; set; }
        public decimal PorcentajePpc { get; set; }
    }

    // ── Metas semanales (Plan Maestro) ──────────────────────────────────────

    public class MetaSemanalDto
    {
        public int Id { get; set; }
        public int MacroActividadId { get; set; }
        public string MacroActividadNombre { get; set; } = string.Empty;
        public DateOnly FechaInicioSemana { get; set; }
        public DateOnly FechaFinSemana { get; set; }
        public decimal MetaAvance { get; set; }
    }

    public class MetaSemanalUpdateDto
    {
        public List<MetaSemanalItemDto> Items { get; set; } = new();
    }

    /// <summary>Upsert por la tupla natural (macro_actividad, fecha_inicio_semana) + projectId de la URL.</summary>
    public class MetaSemanalItemDto
    {
        public int MacroActividadId { get; set; }
        public DateOnly FechaInicioSemana { get; set; }
        public DateOnly FechaFinSemana { get; set; }
        public decimal MetaAvance { get; set; }
    }

    /// <summary>Meta vs. real, ambos como % ACUMULADO al cierre de cada semana (curva S).</summary>
    public class PlanMaestroSemanaDto
    {
        public int MacroActividadId { get; set; }
        public string MacroActividadNombre { get; set; } = string.Empty;
        public DateOnly FechaInicioSemana { get; set; }
        public DateOnly FechaFinSemana { get; set; }
        public decimal MetaAvance { get; set; }
        public decimal AvanceReal { get; set; }
    }

    // ── Pareto de causas de incumplimiento ──────────────────────────────────

    public class CausasParetoDto
    {
        public int TotalNoCumplidas { get; set; }
        public List<CausaParetoDto> Causas { get; set; } = new();
    }

    public class CausaParetoDto
    {
        public int CausaId { get; set; }
        public string CausaNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Porcentaje { get; set; }
    }
}
