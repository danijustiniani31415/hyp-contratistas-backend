using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application;

/// <summary>
/// Única definición de cómo se calculan los % de los indicadores proactivos. Vive aquí
/// —y no en el repositorio— porque el nivel proyecto se arma desde DOS sitios: el
/// dashboard de un proyecto (<c>IndicadoresProactivosService.GetIndicadoresProyectoAsync</c>)
/// y el seguimiento de todos los proyectos (<c>IndicadoresProactivosRepository</c>). Cada
/// uno tenía su propia fórmula y podían mostrar % distintos para el mismo proyecto y mes.
/// </summary>
public static class IndicadorProactivoCalculo
{
    /// <summary>
    /// % de cumplimiento de un indicador. Se cuenta como máximo hasta lo programado: si se
    /// ejecutó de más, el "Ejec" real se sigue mostrando tal cual pero el % no pasa de 100.
    /// Meta 0 devuelve 0, y ese 0 significa "NO APLICA" — nunca debe entrar a un promedio
    /// (para eso está <see cref="PromedioAplicables"/>).
    /// </summary>
    public static decimal Pct(int actual, int meta)
        => meta > 0 ? Math.Round((decimal)Math.Min(actual, meta) / meta * 100, 1) : 0m;

    /// <summary>
    /// Promedia solo los indicadores que aplican. Un indicador sin meta (Prog = 0) es "N/A"
    /// —así lo muestra la tarjeta— y contarlo como 0% de cumplimiento hundía el resultado:
    /// una obra con un único indicador aplicable al 53% mostraba 9% (53 dividido entre 6).
    /// </summary>
    public static decimal PromedioAplicables(params (bool Aplica, decimal Pct)[] indicadores)
    {
        var aplicables = indicadores.Where(i => i.Aplica).Select(i => i.Pct).ToList();
        return aplicables.Count > 0 ? Math.Round(aplicables.Average(), 1) : 0m;
    }

    /// <summary>
    /// Arma el nivel proyecto sumando sus empresas activas. El % general del proyecto es el
    /// promedio de sus 6 indicadores agregados — los mismos Prog/Ejec que la tarjeta muestra,
    /// para que el número grande y las filas de abajo nunca discrepen. Antes era el promedio
    /// de los % POR EMPRESA, así que SAUCE ZEN mostraba 28% mientras sus propias 6 filas
    /// promediaban 58% (incidencia Corilla, ago-2026).
    /// </summary>
    public static IndicadorProactivoProyectoDto ConstruirProyecto(
        int proyectoId, string proyectoNombre, List<MetaEmpresaDto> activas)
    {
        var metaRacs    = activas.Sum(e => e.MetaRacs);
        var metaOpt     = activas.Sum(e => e.MetaOpt);
        var metaAts     = activas.Sum(e => e.MetaAts);
        var metaCharlas = activas.Sum(e => e.MetaCharlas);
        var metaInsp    = activas.Sum(e => e.MetaInspecciones);

        var actualRacs      = activas.Sum(e => e.ActualRacs);
        var actualRacsAtrib = activas.Sum(e => e.ActualRacsAtribuidos);
        var actualRacsCer   = activas.Sum(e => e.ActualRacsCerrados);
        var actualOpt       = activas.Sum(e => e.ActualOpt);
        var actualAts       = activas.Sum(e => e.ActualAts);
        var actualCharlas   = activas.Sum(e => e.ActualCharlas);
        var actualInsp      = activas.Sum(e => e.ActualInspecciones);

        // "RACs cerrados" se mide contra los RACs ATRIBUIDOS al proyecto (los hallazgos que
        // hay que cerrar), no contra los reportados — son poblaciones distintas, igual que
        // en el nivel empresa.
        var pctRacs    = Pct(actualRacs, metaRacs);
        var pctRacsCer = Pct(actualRacsCer, actualRacsAtrib);
        var pctOpt     = Pct(actualOpt, metaOpt);
        var pctAts     = Pct(actualAts, metaAts);
        var pctCharlas = Pct(actualCharlas, metaCharlas);
        var pctInsp    = Pct(actualInsp, metaInsp);

        return new IndicadorProactivoProyectoDto
        {
            ProyectoId = proyectoId,
            ProyectoNombre = proyectoNombre,
            TotalEmpresasActivas = activas.Count(e => e.EsActiva),

            MetaRacsTotal = metaRacs,
            MetaOptTotal = metaOpt,
            MetaAtsTotal = metaAts,
            MetaCharlasTotal = metaCharlas,
            MetaInspeccionesTotal = metaInsp,

            ActualRacsTotal = actualRacs,
            ActualRacsAtribuidosTotal = actualRacsAtrib,
            ActualRacsCerradosTotal = actualRacsCer,
            ActualOptTotal = actualOpt,
            ActualAtsTotal = actualAts,
            ActualCharlasTotal = actualCharlas,
            ActualInspeccionesTotal = actualInsp,

            PctRacs = pctRacs,
            PctRacsCerrados = pctRacsCer,
            PctOpt = pctOpt,
            PctAts = pctAts,
            PctCharlas = pctCharlas,
            PctInspecciones = pctInsp,

            PctProactivoGeneral = PromedioAplicables(
                (metaRacs > 0, pctRacs),
                (actualRacsAtrib > 0, pctRacsCer),
                (metaOpt > 0, pctOpt),
                (metaAts > 0, pctAts),
                (metaCharlas > 0, pctCharlas),
                (metaInsp > 0, pctInsp)),

            Empresas = activas,
        };
    }
}
