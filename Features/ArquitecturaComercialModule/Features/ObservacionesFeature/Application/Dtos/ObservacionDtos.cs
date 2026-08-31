namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;

public class ObservacionFotoDTO
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class ObservacionListItemDTO
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public string? ProyectoNombre { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? PersonaReporta { get; set; }
    public string? EmpresaReporta { get; set; }
    public string? Lugar { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime? PlazoLevantamiento { get; set; }
    public string? PartidaReportada { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? TipoObservacion { get; set; }
    public string? AreaResponsable { get; set; }
    public string? Ejecutor { get; set; }
    public string Origen { get; set; } = string.Empty;
    public int? LevantaPorWorkerId { get; set; }
    public string? LevantaPorNombre { get; set; }
    public List<ObservacionFotoDTO> Fotos { get; set; } = new();
}

public class ObservacionListResponseDTO
{
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int PorPagina { get; set; }
    public List<ObservacionListItemDTO> Items { get; set; } = new();
}

public class ObservacionFiltrosDTO
{
    public List<ProyectoFiltroDTO> Proyectos { get; set; } = new();
    public List<string> Partidas { get; set; } = new();
    public List<string> Estados { get; set; } = new();
}

public class ProyectoFiltroDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class CreateObservacionDTO
{
    public int ProyectoId { get; set; }
    public DateTime Fecha { get; set; }
    public string? PersonaReporta { get; set; }
    public string? EmpresaReporta { get; set; }
    public string? Lugar { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime? PlazoLevantamiento { get; set; }
    public string? PartidaReportada { get; set; }
    public string? TipoObservacion { get; set; }
    public string? AreaResponsable { get; set; }
    public string? Ejecutor { get; set; }
    public string? CreadoPor { get; set; }
}

public class LevantarObservacionDTO
{
    public string? Comentario { get; set; }

    /// <summary>Worker.Id de quien levanta (catálogo de trabajadores Subarea = "Arquitectura
    /// Comercial", mismo que alimenta SupervisorAcDTO). Obligatorio: la cuenta de campo es
    /// compartida, no hay forma de inferirlo de la sesión.</summary>
    public int? LevantaPorWorkerId { get; set; }
}

/// <summary>
/// Edición de una observación ya reportada (requiere el featureKey
/// "arquitectura-comercial.observaciones.editar", asignado por rol desde
/// Configuración > Roles y Permisos). Campos null = no tocar.
/// </summary>
public class UpdateObservacionDTO
{
    public string? Lugar { get; set; }
    public string? Descripcion { get; set; }
    public string? PartidaReportada { get; set; }
    public string? AreaResponsable { get; set; }
    public string? PersonaReporta { get; set; }
}

public class ObservacionDashboardSupervisorDTO
{
    public string PersonaReporta { get; set; } = string.Empty;
    public string? ProyectoNombre { get; set; }
    public int TotalReportadas { get; set; }
    public int TotalCompletadas { get; set; }
    public int TotalPendientes { get; set; }
    public int TotalEnProceso { get; set; }
    public decimal PctAvance { get; set; }
    public List<ObservacionPorPartidaDTO> PorPartida { get; set; } = new();
}

public class ObservacionPorPartidaDTO
{
    public string Partida { get; set; } = string.Empty;
    public int Completado { get; set; }
    public int Pendiente { get; set; }
}

public class ObservacionDashboardDTO
{
    public List<ObservacionDashboardSupervisorDTO> Supervisores { get; set; } = new();
}

/// <summary>
/// Los 4 totales que muestran las cards (Reportados/Completados/Pendientes/En Proceso).
/// Separado de <see cref="ObservacionDashboardDTO"/> a propósito: la Lista solo necesita
/// estos 4 números y no debe pagar el costo de traer/agrupar el desglose por supervisor.
/// </summary>
public class ObservacionStatsDTO
{
    public int Reportados { get; set; }
    public int Completados { get; set; }
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
}
