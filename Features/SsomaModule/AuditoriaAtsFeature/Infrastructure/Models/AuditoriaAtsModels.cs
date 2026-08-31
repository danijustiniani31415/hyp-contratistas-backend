using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Infrastructure.Models;

public class SsomaAuditoriaAtsPregunta
{
    public int Id { get; set; }
    public short Orden { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SsomaAuditoriaAtsRespuesta> Respuestas { get; set; } = [];
}

public class SsomaAuditoriaAts
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public int AuditorWorkerId { get; set; }
    public int AuditadoWorkerId { get; set; }
    public int? ProyectoId { get; set; }
    public string? EmailAuditado { get; set; }
    public string? Actividad { get; set; }
    public string? Lugar { get; set; }
    public decimal? PuntajePromedio { get; set; }
    public string? Nivel { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = "Evaluado";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SsomaAuditoriaAtsRespuesta> Respuestas { get; set; } = [];
    public ICollection<SsomaAuditoriaAtsFoto> Fotos { get; set; } = [];
}

public class SsomaAuditoriaAtsRespuesta
{
    public int Id { get; set; }
    public int AuditoriaId { get; set; }
    public int PreguntaId { get; set; }
    public short Puntaje { get; set; }
    public string? Comentario { get; set; }

    public SsomaAuditoriaAts? Auditoria { get; set; }
    public SsomaAuditoriaAtsPregunta? Pregunta { get; set; }
}

public class SsomaAuditoriaAtsFoto
{
    public int Id { get; set; }
    public int AuditoriaId { get; set; }
    public string FotoBase64 { get; set; } = string.Empty;
    public short Orden { get; set; }

    public SsomaAuditoriaAts? Auditoria { get; set; }
}
