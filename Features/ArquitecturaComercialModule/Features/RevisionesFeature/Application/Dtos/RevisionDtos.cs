namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Dtos;

public class RevisionDTO
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public string? ProyectoNombre { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Lugar { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class CreateRevisionDTO
{
    public int ProyectoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Lugar { get; set; } = string.Empty;
}

public class RevisionObservacionFotoDTO
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class RevisionObservacionListItemDTO
{
    public int Id { get; set; }
    public int RevisionId { get; set; }
    public string? RevisionNombre { get; set; }
    public int ProyectoId { get; set; }
    public string? ProyectoNombre { get; set; }
    public DateTime Fecha { get; set; }
    public string? PersonaReporta { get; set; }
    public string? ZonaAmbiente { get; set; }
    public string? PartidaReportada { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime? PlazoLevantamiento { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public int? LevantaPorWorkerId { get; set; }
    public string? LevantaPorNombre { get; set; }
    public List<RevisionObservacionFotoDTO> Fotos { get; set; } = new();
}

public class RevisionObservacionListResponseDTO
{
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int PorPagina { get; set; }
    public List<RevisionObservacionListItemDTO> Items { get; set; } = new();
}

public class RevisionFiltrosDTO
{
    public List<ProyectoRevisionFiltroDTO> Proyectos { get; set; } = new();
    public List<string> Partidas { get; set; } = new();
    public List<string> Estados { get; set; } = new();
    public List<string> Tipos { get; set; } = new();
}

public class ProyectoRevisionFiltroDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class CreateRevisionObservacionDTO
{
    public int RevisionId { get; set; }
    public DateTime Fecha { get; set; }
    public string? PersonaReporta { get; set; }
    public string? ZonaAmbiente { get; set; }
    public string? PartidaReportada { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime? PlazoLevantamiento { get; set; }
    public string? CreadoPor { get; set; }
}

public class LevantarRevisionObservacionDTO
{
    public string? Comentario { get; set; }

    /// <summary>Worker.Id de quien levanta — obligatorio, mismo criterio que en Observaciones
    /// (login de campo compartido, no se puede inferir de la sesión).</summary>
    public int? LevantaPorWorkerId { get; set; }
}

/// <summary>Edición de una observación de revisión ya reportada (requiere el featureKey
/// "arquitectura-comercial.revisiones.editar"). Campos null = no tocar.</summary>
public class UpdateRevisionObservacionDTO
{
    public string? ZonaAmbiente { get; set; }
    public string? Descripcion { get; set; }
    public string? PartidaReportada { get; set; }
    public string? PersonaReporta { get; set; }
}

public class RevisionDashboardGrupoDTO
{
    public string PersonaReporta { get; set; } = string.Empty;
    public string? RevisionNombre { get; set; }
    public int TotalReportadas { get; set; }
    public int TotalCompletadas { get; set; }
    public int TotalPendientes { get; set; }
    public int TotalEnProceso { get; set; }
    public decimal PctAvance { get; set; }
    public List<RevisionObservacionPorPartidaDTO> PorPartida { get; set; } = new();
}

public class RevisionObservacionPorPartidaDTO
{
    public string Partida { get; set; } = string.Empty;
    public int Completado { get; set; }
    public int Pendiente { get; set; }
}

public class RevisionDashboardDTO
{
    public List<RevisionDashboardGrupoDTO> Grupos { get; set; } = new();
}

/// <summary>Los 4 totales de las cards (Reportados/Completados/Pendientes/En Proceso),
/// separado del dashboard completo — mismo motivo que ObservacionStatsDTO.</summary>
public class RevisionObservacionStatsDTO
{
    public int Reportados { get; set; }
    public int Completados { get; set; }
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
}
