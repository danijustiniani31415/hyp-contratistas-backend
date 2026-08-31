using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Models
{
    /// <summary>
    /// Pertenencia de un trabajador a un proyecto, para el módulo de salidas. Se usa cuando el área
    /// del trabajador está marcada como "filtrar por proyecto": el revisor se resuelve contra el
    /// revisor del proyecto al que pertenece (area_revisores.project_id). Por ahora un trabajador
    /// pertenece a un solo proyecto (índice único parcial por worker_id WHERE state).
    /// Tabla desacoplada de la matriz base (no se toca la tabla workers).
    /// </summary>
    [Table("ga_salidas_workers_project")]
    public class GaSalidasWorkersProject
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>Soft delete: false = eliminado (se conserva para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;
    }
}
