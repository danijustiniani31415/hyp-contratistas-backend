using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Models;

/// <summary>ssoma_amonestacion_tipo_sancion</summary>
public class SsomaAmonestacionTipoSancion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    // BAJO | MEDIO | ALTO | CRITICO — controla destinatarios del correo
    public string NivelGravedad { get; set; } = "BAJO";
    public bool GeneraSuspension { get; set; }
    public bool State { get; set; } = true;
}

/// <summary>ssoma_amonestacion_infraccion_tipo</summary>
public class SsomaAmonestacionInfraccionTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool State { get; set; } = true;
}

/// <summary>ssoma_amonestacion</summary>
public class SsomaAmonestacion
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";

    // Proyecto y fecha
    public int ProyectoId { get; set; }
    public DateTime Fecha { get; set; }

    // Trabajador notificado
    public int WorkerId { get; set; }
    public int? PartidaId { get; set; }

    // Tipo de sanción y tipo de infracción
    public int TipoSancionId { get; set; }
    public int InfraccionTipoId { get; set; }

    // Descripción
    public string Descripcion { get; set; } = "";

    // Penalización (solo para contratistas)
    public bool AplicaPenalizacion { get; set; }
    public int? SancionInfraccionId { get; set; }   // FK ssoma_rac_infraccion
    public decimal MontoCalculado { get; set; }
    public decimal UitReferencia { get; set; }

    // Puntaje y suspensión
    public int PuntosInfraccion { get; set; }        // 0-10
    public int? DiasSuspension { get; set; }
    public DateOnly? FechaInicioSuspension { get; set; }
    public DateOnly? FechaFinSuspension { get; set; }

    // Persona que reporta (usuario logueado)
    public int? PersonaReportaId { get; set; }       // FK app_user (user_id)

    // PDF generado
    public string? PdfUrl { get; set; }

    // Ciclo de vida: Registrada | PendienteFirma | Cerrada
    public string Estado { get; set; } = "Registrada";
    public string? DocumentoFirmadoUrl { get; set; }
    public DateTime? FechaCierre { get; set; }

    // Auditoría
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    /// <summary>Usuario que corrigió el registro vía el endpoint de edición (acceso restringido). Null = nunca editado.</summary>
    public int? UpdatedBy { get; set; }
    public bool State { get; set; } = true;

    // Navegación
    public List<SsomaAmonestacionFoto> Fotos { get; set; } = new();
}

/// <summary>ssoma_amonestacion_foto</summary>
public class SsomaAmonestacionFoto
{
    public int Id { get; set; }
    public int AmonestacionId { get; set; }
    public string Url { get; set; } = "";
    public string? SpId { get; set; }
    public string? NombreArchivo { get; set; }
    public int Orden { get; set; } = 1;
    [Column("base64data")]
    public string? Base64Data { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public SsomaAmonestacion Amonestacion { get; set; } = null!;
}
