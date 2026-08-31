using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Models;

/// <summary>ssoma_inhabilitacion — historial de bloqueos SSOMA por trabajador</summary>
[Table("ssoma_inhabilitacion")]
public class SsomaInhabilitacion
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    // 'PUNTOS' | 'RETIRO_DEFINITIVO'
    public string Tipo { get; set; } = "";
    public string? Motivo { get; set; }
    public int? PuntosAlMomento { get; set; }
    public DateTimeOffset FechaInicio { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaFin { get; set; }       // null = sigue bloqueado
    public int? DesbloqueadoPor { get; set; }           // user_id
    public int? EscuelitaId { get; set; }               // curso que desbloqueó
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>ssoma_escuelita — cursos de Escuelita Abril (descuentan puntos)</summary>
[Table("ssoma_escuelita")]
public class SsomaEscuelita
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public DateOnly Fecha { get; set; }
    public int PuntosDescontados { get; set; }
    public string? Observaciones { get; set; }
    public int? RegistradoPor { get; set; }             // user_id coordinador SSOMA
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
