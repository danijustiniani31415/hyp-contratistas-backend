using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    /// <summary>
    /// Catálogo normalizado de categorías de trabajador. Reemplaza el uso del texto
    /// libre <c>workers.categoria</c> SOLO en Lecciones Aprendidas y Solicitud de
    /// Salidas (esas features apuntan a <c>workers.worker_category_id</c>). La
    /// columna <c>categoria</c> original se conserva porque otras funcionalidades
    /// la siguen usando.
    /// </summary>
    [Table("workers_category")]
    public class WorkersCategory
    {
        [Column("workers_category_id")]
        public int WorkersCategoryId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Habilitar/inhabilitar en la app.</summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>Soft-delete: false = eliminado (se conserva para histórico).</summary>
        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
