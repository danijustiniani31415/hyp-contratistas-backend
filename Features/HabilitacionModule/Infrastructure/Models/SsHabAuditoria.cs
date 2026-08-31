using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_hab_auditoria")]
    public class SsHabAuditoria
    {
        [Key][Column("id")] public int Id { get; set; }
        [Column("contractor_id")] public int ContractorId { get; set; }
        [Column("worker_id")] public int? WorkerId { get; set; }
        [Column("empresa_entregable_id")] public int? EmpresaEntregableId { get; set; }
        [Column("worker_entregable_id")] public int? WorkerEntregableId { get; set; }
        [Column("accion")] public string Accion { get; set; } = string.Empty;
        [Column("realizado_por_user_id")] public int RealizadoPorUserId { get; set; }
        [Column("realizado_por_nombre")] public string? RealizadoPorNombre { get; set; }
        [Column("fecha")] public DateTime Fecha { get; set; } = DateTime.UtcNow;
        [Column("detalle")] public string? Detalle { get; set; }
    }
}
