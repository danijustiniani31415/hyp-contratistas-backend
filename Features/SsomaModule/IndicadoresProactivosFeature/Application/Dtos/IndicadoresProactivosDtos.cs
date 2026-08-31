namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Dtos;

// ─────────────────────────────────────────────────────────────────────────────
// PROGRAMACIÓN DE INSPECCIONES
// ─────────────────────────────────────────────────────────────────────────────

public record InspeccionTipoDto(int Id, string Nombre);

public record EmpresaProgInspeccionDto(
    int? EmpresaId,
    string EmpresaTipo,      // "Casa" | "Contratista"
    string EmpresaNombre,
    List<int> TiposAplicables  // ids de ssoma_inspeccion_tipo seleccionados
);

public record GuardarProgInspeccionRequest(
    int ProyectoId,
    int Mes,
    int Anio,
    int? EmpresaId,           // null = casa
    string EmpresaTipo,
    List<int> TiposAplicables
);

public record ProgInspeccionResumenDto(
    int ProyectoId,
    int Mes,
    int Anio,
    List<EmpresaProgInspeccionDto> Empresas
);

// ─────────────────────────────────────────────────────────────────────────────
// CÁLCULO DE META PROACTIVA POR EMPRESA
// ─────────────────────────────────────────────────────────────────────────────

public record MetaEmpresaDto
{
    public int? EmpresaId { get; init; }
    public string EmpresaTipo { get; init; } = "";
    public string EmpresaNombre { get; init; } = "";
    public decimal PromedioTrabajadores { get; init; }
    public int DiasLaborados { get; init; }
    public bool EsActiva { get; init; }  // >= 10 días

    // Metas calculadas
    public int MetaRacs { get; init; }
    public int MetaOpt { get; init; }
    public int MetaAts { get; init; }
    public int MetaCharlas { get; init; }
    public int MetaInspecciones { get; init; }

    // Actuals
    public int ActualRacs { get; init; }
    // RACs atribuidos a esta empresa (empresa reportada) — base sobre la que se mide el cierre.
    public int ActualRacsAtribuidos { get; init; }
    public int ActualRacsCerrados { get; init; }
    public int ActualOpt { get; init; }
    public int ActualAts { get; init; }
    public int ActualCharlas { get; init; }
    public int ActualInspecciones { get; init; }

    // % cumplimiento por indicador (0-100, puede superar 100)
    public decimal PctRacs { get; init; }
    public decimal PctRacsCerrados { get; init; }
    public decimal PctOpt { get; init; }
    public decimal PctAts { get; init; }
    public decimal PctCharlas { get; init; }
    public decimal PctInspecciones { get; init; }

    // Promedio general (0-100+) — solo sobre los indicadores APLICABLES (los que tienen
    // meta > 0). Un indicador con Prog = 0 se muestra como "N/A" y no entra al promedio.
    public decimal PctProactivoGeneral { get; init; }

    // Cuántos de los 6 indicadores le aplican este mes. Si es 0, la empresa no es
    // evaluable y no debe entrar en el promedio del proyecto (un 0% que no ganó).
    public int IndicadoresAplicables { get; init; }

    // Ocultar manualmente empresas cuyo % no refleja la realidad (ej. sin supervisores)
    public bool EsOculto { get; init; } = false;
    public bool PuedeOcultarse { get; init; } = false;
}

// ─────────────────────────────────────────────────────────────────────────────
// SEGUIMIENTO INDICADORES — NIVEL PROYECTO
// ─────────────────────────────────────────────────────────────────────────────

public record IndicadorProactivoProyectoDto
{
    public int ProyectoId { get; init; }
    public string ProyectoNombre { get; init; } = "";

    // Totales del proyecto (suma de todas las empresas activas)
    public int TotalEmpresasActivas { get; init; }

    public int MetaRacsTotal { get; init; }
    public int MetaOptTotal { get; init; }
    public int MetaAtsTotal { get; init; }
    public int MetaCharlasTotal { get; init; }
    public int MetaInspeccionesTotal { get; init; }

    public int ActualRacsTotal { get; init; }
    // RACs atribuidos al proyecto (empresa reportada) — base contra la que se mide el cierre.
    // Es distinto de ActualRacsTotal (los que el proyecto reportó): son poblaciones distintas.
    public int ActualRacsAtribuidosTotal { get; init; }
    public int ActualRacsCerradosTotal { get; init; }
    public int ActualOptTotal { get; init; }
    public int ActualAtsTotal { get; init; }
    public int ActualCharlasTotal { get; init; }
    public int ActualInspeccionesTotal { get; init; }

    // % cumplimiento por indicador
    public decimal PctRacs { get; init; }
    public decimal PctRacsCerrados { get; init; }
    public decimal PctOpt { get; init; }
    public decimal PctAts { get; init; }
    public decimal PctCharlas { get; init; }
    public decimal PctInspecciones { get; init; }

    // Promedio general (base para puntaje)
    public decimal PctProactivoGeneral { get; init; }

    // Desglose por empresa (para la pantalla de programación/seguimiento interno)
    public List<MetaEmpresaDto> Empresas { get; init; } = [];

    // Checklists del proyecto (para dashboard-proyecto)
    public List<ChecklistResumenDto> Checklists { get; init; } = [];
}

public record ChecklistResumenDto(
    int Id,
    string Nombre,
    decimal Porcentaje,
    string Estado,
    bool EsObligatorio,
    int TotalItems,
    int ItemsCompletados
);

// ─────────────────────────────────────────────────────────────────────────────
// PUNTAJE DEL MES
// ─────────────────────────────────────────────────────────────────────────────

public record PuntajeMesDto
{
    public int ProyectoId { get; init; }
    public string ProyectoNombre { get; init; } = "";
    public int Mes { get; init; }
    public int Anio { get; init; }

    // Componentes (0-100 cada uno)
    public decimal PctProactivos { get; init; }    // 40% del total
    public decimal PctPasso { get; init; }          // 25% del total
    public decimal PctCierreAccidentes { get; init; }  // 20% del total
    public decimal PctCierreHallazgos { get; init; }   // 15% del total

    // Subtotales ponderados
    public decimal PuntajeProactivos { get; init; }   // PctProactivos * 0.40
    public decimal PuntajePasso { get; init; }          // PctPasso * 0.25
    public decimal PuntajeCierreAccidentes { get; init; }
    public decimal PuntajeCierreHallazgos { get; init; }

    // Bonus por superar meta proactiva (hasta +10 pts)
    public decimal BonusProactivos { get; init; }

    // Total (máx 110)
    public decimal PuntajeTotal { get; init; }

    // Detalle accidentes
    public int AccidentesAbiertos { get; init; }
    public int AccidentesTotales { get; init; }

    // Detalle hallazgos
    public int HallazgosCerrados { get; init; }
    public int HallazgosTotales { get; init; }
}

// ─────────────────────────────────────────────────────────────────────────────
// QUERY PARAMS
// ─────────────────────────────────────────────────────────────────────────────

public record IndicadoresQuery(int Mes, int Anio, int? ProyectoId = null);
