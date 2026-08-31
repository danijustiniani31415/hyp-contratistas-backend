using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Models;

public class AcObservacion
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? PersonaReporta { get; set; }
    public string? EmpresaReporta { get; set; }
    public string? Lugar { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime? PlazoLevantamiento { get; set; }
    public string? PartidaReportada { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? TipoObservacion { get; set; }
    public string? AreaResponsable { get; set; }
    public string? Ejecutor { get; set; }
    public string? Observacion { get; set; }
    public string? Levantamiento { get; set; }
    public string? EstadoCierre { get; set; }
    public string? CreadoPor { get; set; }

    /// <summary>"Importado" para el histórico cargado desde SharePoint, "Nuevo" para lo creado desde esta app.</summary>
    public string Origen { get; set; } = "Nuevo";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FechaLevantamiento { get; set; }

    /// <summary>Trabajador (Subarea = "Arquitectura Comercial") que levantó la observación — el
    /// login de campo es una sola cuenta compartida (operarioscomercial@abril.pe), así que quién
    /// levantó no se puede inferir de la sesión y hay que registrarlo explícitamente.</summary>
    public int? LevantaPorWorkerId { get; set; }

    public Project? Proyecto { get; set; }

    [ForeignKey(nameof(LevantaPorWorkerId))]
    public Worker? LevantaPor { get; set; }
    public ICollection<AcObservacionFoto> Fotos { get; set; } = [];
}

public class AcObservacionFoto
{
    public int Id { get; set; }
    public int ObservacionId { get; set; }

    /// <summary>"Observacion" (foto al reportar) o "Levantamiento" (foto al cerrar).</summary>
    public string Tipo { get; set; } = "Observacion";
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AcObservacion? Observacion { get; set; }
}
