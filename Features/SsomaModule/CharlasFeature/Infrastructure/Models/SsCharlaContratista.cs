using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Infrastructure.Models;

/// <summary>
/// Charla diaria que un contratista sube por obligación cuando su empresa fue
/// tareada (ss_tareo_detalle_contratista) para un proyecto en una fecha dada.
/// Independiente del flujo de "programa de charlas" del staff interno de Abril.
/// </summary>
[Table("ss_charla_contratista")]
public class SsCharlaContratista
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("proyecto_id")] public int ProyectoId { get; set; }
    [Column("empresa_id")] public int EmpresaId { get; set; }
    [Column("fecha")] public DateOnly Fecha { get; set; }
    [Column("tema")][MaxLength(200)] public string Tema { get; set; } = string.Empty;
    [Column("descripcion")] public string? Descripcion { get; set; }
    [Column("evidencia_url")][MaxLength(1000)] public string? EvidenciaUrl { get; set; }
    [Column("evidencia_nombre")][MaxLength(300)] public string? EvidenciaNombre { get; set; }
    [Column("subido_por_user_id")] public int? SubidoPorUserId { get; set; }
    [Column("estado")][MaxLength(20)] public string Estado { get; set; } = "Enviado";
    [Column("aprobado_por_id")] public int? AprobadoPorId { get; set; }
    [Column("aprobado_en")] public DateTime? AprobadoEn { get; set; }
    [Column("motivo_rechazo")] public string? MotivoRechazo { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("state")] public bool State { get; set; } = true;
}
