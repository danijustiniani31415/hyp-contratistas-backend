using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Infrastructure.Models;

[Table("ss_charla")]
public class SsCharla
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("programa_id")] public int ProgramaId { get; set; }
    [Column("fecha")] public DateTime Fecha { get; set; }
    [Column("titulo")][MaxLength(300)] public string Titulo { get; set; } = string.Empty;
    [Column("descripcion")] public string? Descripcion { get; set; }
    [Column("tema")][MaxLength(200)] public string? Tema { get; set; }
    [Column("duracion_horas")] public decimal DuracionHoras { get; set; } = 1;
    [Column("supervisor_id")] public int? SupervisorId { get; set; }
    [Column("estado")][MaxLength(20)] public string Estado { get; set; } = "Programada";
    [Column("evidencia_url")][MaxLength(1000)] public string? EvidenciaUrl { get; set; }
    [Column("evidencia_nombre")][MaxLength(300)] public string? EvidenciaNombre { get; set; }
    [Column("evidencia_sp_id")][MaxLength(200)] public string? EvidenciaSpId { get; set; }
    [Column("evidencia_subida_por_id")] public int? EvidenciaSubidaPorId { get; set; }
    [Column("evidencia_subida_en")] public DateTime? EvidenciaSubidaEn { get; set; }
    [Column("creado_por_id")] public int? CreadoPorId { get; set; }
    [Column("proyecto_id")] public int? ProyectoId { get; set; }
    [Column("es_capacitacion_individual")] public bool EsCapacitacionIndividual { get; set; } = false;

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Column("state")] public bool State { get; set; } = true;

    public virtual SsCharlaPrograma? Programa { get; set; }
    public virtual ICollection<SsCharlaAsistencia> Asistencias { get; set; } = new List<SsCharlaAsistencia>();
    public virtual ICollection<SsCharlaArchivo> Archivos { get; set; } = new List<SsCharlaArchivo>();
}
