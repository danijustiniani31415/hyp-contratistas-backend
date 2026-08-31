using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Models;

/// <summary>Catálogo de revisiones por proyecto (ej. "R1-9 Nogales-Sala de ventas") — las
/// observaciones se reportan dentro de una de estas, no directo al proyecto.</summary>
public class AcRevision
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }

    /// <summary>R1 | R2 | R1-AC | R2-AC | RF-AC — ver <see cref="TipoRevision"/>.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Catálogo (AcCatalogoItem tipo LugarRevision) o texto libre desde "Otro lugar".</summary>
    public string Lugar { get; set; } = string.Empty;

    /// <summary>Generado: "{Tipo}-{ProyectoNombre}-{Lugar}".</summary>
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Project? Proyecto { get; set; }
}

public static class TipoRevision
{
    public static readonly string[] Valores = ["R1", "R2", "R1-AC", "R2-AC", "RF-AC"];

    public static bool EsValido(string tipo) => Valores.Contains(tipo);
}

public class AcRevisionObservacion
{
    public int Id { get; set; }
    public int RevisionId { get; set; }
    public DateTime Fecha { get; set; }
    public string? PersonaReporta { get; set; }

    /// <summary>Zona/Ambiente dentro de la revisión (ej. "Cocina", "Baño 2") — texto libre,
    /// distinto del "Lugar" catalogado que ya vive en <see cref="AcRevision"/>.</summary>
    public string? ZonaAmbiente { get; set; }

    public string? PartidaReportada { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public DateTime? PlazoLevantamiento { get; set; }
    public string? CreadoPor { get; set; }

    /// <summary>"Importado" para histórico cargado desde SharePoint/Power Apps, "Nuevo" para
    /// lo creado desde esta app — mismo criterio que AcObservacion.Origen.</summary>
    public string Origen { get; set; } = "Nuevo";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FechaLevantamiento { get; set; }

    /// <summary>Trabajador que levantó la observación — login de campo compartido, igual que
    /// en Observaciones (ver AcObservacion.LevantaPorWorkerId).</summary>
    public int? LevantaPorWorkerId { get; set; }

    public AcRevision? Revision { get; set; }

    [ForeignKey(nameof(LevantaPorWorkerId))]
    public Worker? LevantaPor { get; set; }

    public ICollection<AcRevisionObservacionFoto> Fotos { get; set; } = [];
}

public class AcRevisionObservacionFoto
{
    public int Id { get; set; }
    public int RevisionObservacionId { get; set; }

    /// <summary>"Observacion" (foto al reportar) o "Levantamiento" (foto al cerrar).</summary>
    public string Tipo { get; set; } = "Observacion";
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AcRevisionObservacion? RevisionObservacion { get; set; }
}
