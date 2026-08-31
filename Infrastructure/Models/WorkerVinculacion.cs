using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("worker_vinculaciones")]
    public class WorkerVinculacion
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("fecha_inicio")]
        public DateOnly FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateOnly? FechaFin { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("proyecto_id")]
        public int? ProyectoId { get; set; }

        [Column("puesto")]
        public string? Puesto { get; set; }

        /// <summary>Clasificación de riesgo vigente durante ESTA vinculación (catálogo
        /// workers_obra_oficina_staff). Se guarda por vinculación, no solo en Worker, para
        /// poder saber qué clasificación tenía el trabajador en cada obra/empresa pasada —
        /// necesario para evaluar convalidaciones de EMO con precisión histórica.</summary>
        [Column("obra_oficina_staff_id")]
        public int? ObraOficinaStaffId { get; set; }

        /// <summary>Categoría vigente (campo de LÓGICA, catálogo <c>categoria</c>) durante ESTA
        /// vinculación — congelada al momento del cambio, igual que <see cref="Puesto"/> y
        /// <see cref="ObraOficinaStaffId"/>, para poder reconstruir el historial con precisión
        /// (p.ej. convalidaciones de EMO necesitan saber la categoría de origen vs destino).</summary>
        [Column("categoria_id")]
        public int? CategoriaId { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        public Shared.Models.Categoria? Categoria { get; set; }

        [Column("tipo_vinculacion")]
        public string? TipoVinculacion { get; set; }

        [Column("motivo_retiro")]
        public string? MotivoRetiro { get; set; }

        [Column("registrado_por_id")]
        public int? RegistradoPorId { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public Contributor? Empresa { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }
    }
}
