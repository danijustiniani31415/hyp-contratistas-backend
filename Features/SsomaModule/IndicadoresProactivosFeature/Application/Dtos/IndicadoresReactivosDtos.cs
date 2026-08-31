namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Dtos;

public record IndicadoresReactivosQuery(int Mes, int Anio, int? ProyectoId = null);

public record IndicadorReactivoProyectoDto
{
    public int ProyectoId { get; init; }
    public string ProyectoNombre { get; init; } = "";
    public int Mes { get; init; }
    public int Anio { get; init; }

    // ── Mes consultado ────────────────────────────────────────────────────
    public decimal HorasHombreTrabajadas { get; init; }
    public int TotalAccidentes { get; init; }
    public int TotalDiasPerdidos { get; init; }
    public decimal IndiceFrecuencia { get; init; }    // IF = accidentes × 10⁶ / HHT
    public decimal IndiceGravedad { get; init; }      // IG = días perdidos × 10⁶ / HHT
    public decimal IndiceAccidentabilidad { get; init; } // IA = IF × IG / 1000

    // ── Año consultado (enero-diciembre de Anio) ─────────────────────────
    public decimal HorasHombreTrabajadasAnio { get; init; }
    public int TotalAccidentesAnio { get; init; }
    public int TotalDiasPerdidosAnio { get; init; }
    public decimal IndiceFrecuenciaAnio { get; init; }
    public decimal IndiceGravedadAnio { get; init; }
    public decimal IndiceAccidentabilidadAnio { get; init; }

    // ── Histórico completo del proyecto (todos los años) ─────────────────
    public decimal HorasHombreTrabajadasTotal { get; init; }
    public int TotalAccidentesTotal { get; init; }
    public int TotalDiasPerdidosTotal { get; init; }
    public decimal IndiceFrecuenciaTotal { get; init; }
    public decimal IndiceGravedadTotal { get; init; }
    public decimal IndiceAccidentabilidadTotal { get; init; }
}

// ── Meta anual de reactivos ─────────────────────────────────────────────────

public record MetaAnualDto
{
    public int Anio { get; init; }
    public decimal? MetaIndiceFrecuencia { get; init; }
    public decimal? MetaIndiceGravedad { get; init; }
    public decimal? MetaIndiceAccidentabilidad { get; init; }
}

public record GuardarMetaAnualRequest
{
    public int Anio { get; init; }
    public decimal? MetaIndiceFrecuencia { get; init; }
    public decimal? MetaIndiceGravedad { get; init; }
    public decimal? MetaIndiceAccidentabilidad { get; init; }
}
