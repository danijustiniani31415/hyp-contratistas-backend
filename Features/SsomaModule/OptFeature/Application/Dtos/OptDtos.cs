namespace Abril_Backend.Features.SsomaModule.OptFeature.Application.Dtos;

// ── Catálogos ──────────────────────────────────────────────────────────────

public class OptPetDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? SharepointUrl { get; set; }
}

public class OptCriterioVerificacionDto
{
    public int Id { get; set; }
    public string Pregunta { get; set; } = string.Empty;
    public int Orden { get; set; }
}

// ── Crear OPT ──────────────────────────────────────────────────────────────

public class OptTrabajadorRequest
{
    public int TrabajadorId { get; set; }
    public string? TipoTrabajador { get; set; }
    public string? TiempoEnObra { get; set; }
    public string? AniosExperiencia { get; set; }
    public string? FirmaTrabajadorBase64 { get; set; }
}

public class OptVerificacionRequest
{
    public int CriterioId { get; set; }
    public bool Resultado { get; set; }
}

public class OptPasoRequest
{
    public string NumeroDisplay { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Nivel { get; set; } = 1;
    public string? Resultado { get; set; }
    public string? DesviacionObservada { get; set; }
    public int Orden { get; set; }
}

public class CrearOptRequest
{
    public int ProyectoId { get; set; }
    public int? PetId { get; set; }
    public DateTime Fecha { get; set; }
    public string TipoObservacion { get; set; } = string.Empty;
    public bool CuentaConPet { get; set; }
    public string? Area { get; set; }
    public bool SeInformaTrabajador { get; set; }
    public int? ObservadorId { get; set; }
    public string? ObservadorNombre { get; set; }
    public string? ObservadorCargo { get; set; }
    public string? FirmaObservadorBase64 { get; set; }
    public bool SeFelicito { get; set; }
    public bool SeRecibieronComentarios { get; set; }
    public bool SeRetroalimento { get; set; }
    public bool SeObtuvoCCompromiso { get; set; }
    public string? AccionRequerida { get; set; }
    public string? AccionObservacion { get; set; }
    public List<OptTrabajadorRequest> Trabajadores { get; set; } = [];
    public List<OptVerificacionRequest> Verificaciones { get; set; } = [];
    public List<OptPasoRequest> Pasos { get; set; } = [];
    public List<string> FotosAreaBase64 { get; set; } = [];
}

// ── Respuestas ─────────────────────────────────────────────────────────────

public class OptTrabajadorDto
{
    public int Id { get; set; }
    public int TrabajadorId { get; set; }
    public string NombreTrabajador { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? TipoTrabajador { get; set; }
    public string? TiempoEnObra { get; set; }
    public string? AniosExperiencia { get; set; }
    public string? FirmaTrabajadorUrl { get; set; }
    public int? EmpresaId { get; set; }
    public string? EmpresaNombre { get; set; }
}

public class OptVerificacionDto
{
    public int CriterioId { get; set; }
    public string Pregunta { get; set; } = string.Empty;
    public bool Resultado { get; set; }
}

public class OptPasoDto
{
    public int Id { get; set; }
    public string NumeroDisplay { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public string? Resultado { get; set; }
    public string? DesviacionObservada { get; set; }
    public int Orden { get; set; }
}

public class OptDetalleDto
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public int? PetId { get; set; }
    public string? PetNombre { get; set; }
    public string? PetSharepointUrl { get; set; }
    public DateTime Fecha { get; set; }
    public string TipoObservacion { get; set; } = string.Empty;
    public bool CuentaConPet { get; set; }
    public string? Area { get; set; }
    public bool SeInformaTrabajador { get; set; }
    public int? ObservadorId { get; set; }
    public string? ObservadorNombre { get; set; }
    public string? ObservadorCargo { get; set; }
    public int? ObservadorEmpresaId { get; set; }
    public string? ObservadorEmpresaNombre { get; set; }
    public string? FirmaObservadorUrl { get; set; }
    public bool SeFelicito { get; set; }
    public bool SeRecibieronComentarios { get; set; }
    public bool SeRetroalimento { get; set; }
    public bool SeObtuvoCCompromiso { get; set; }
    public string? AccionRequerida { get; set; }
    public string? AccionObservacion { get; set; }
    public int TotalPasos { get; set; }
    public int TotalSeguros { get; set; }
    public int TotalInseguros { get; set; }
    public decimal? ScorePct { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OptTrabajadorDto> Trabajadores { get; set; } = [];
    public List<OptVerificacionDto> Verificaciones { get; set; } = [];
    public List<OptPasoDto> Pasos { get; set; } = [];
    public List<string> FotosArea { get; set; } = [];
}

public class OptListItemDto
{
    public int Id { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public string? PetNombre { get; set; }
    public DateTime Fecha { get; set; }
    public string TipoObservacion { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? ObservadorNombre { get; set; }
    public string? ObservadorEmpresaNombre { get; set; }
    public string TrabajadoresPrincipal { get; set; } = string.Empty;
    public string? TrabajadorPrincipalEmpresaNombre { get; set; }
    public int TotalTrabajadores { get; set; }
    public decimal? ScorePct { get; set; }
    public string? AccionRequerida { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── Dashboard ──────────────────────────────────────────────────────────────

public class OptDashboardDto
{
    public int TotalOpts { get; set; }
    public int TotalEsteMes { get; set; }
    public decimal? ScorePromedioGlobal { get; set; }
    public decimal? ScorePromedioEsteMes { get; set; }
    public int AccionesPendientes { get; set; }
    public List<OptScoreMensualDto> TendenciaMensual { get; set; } = [];
    public List<OptEmpresaRankingDto> RankingEmpresas { get; set; } = [];
    public List<OptTrabajadorRiesgoDto> TopTrabajadoresRiesgo { get; set; } = [];
    public List<OptAccionResumenDto> AccionesRequeridas { get; set; } = [];
}

public class OptScoreMensualDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string MesNombre { get; set; } = string.Empty;
    public decimal? ScorePromedio { get; set; }
    public int TotalOpts { get; set; }
}

public class OptEmpresaRankingDto
{
    public int EmpresaId { get; set; }
    public string EmpresaNombre { get; set; } = string.Empty;
    public decimal? ScorePromedio { get; set; }
    public int TotalOpts { get; set; }
}

public class OptTrabajadorRiesgoDto
{
    public int TrabajadorId { get; set; }
    public string NombreTrabajador { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public decimal? ScorePromedio { get; set; }
    public int TotalOpts { get; set; }
    public int TotalInseguros { get; set; }
}

public class OptAccionResumenDto
{
    public string TipoAccion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}
