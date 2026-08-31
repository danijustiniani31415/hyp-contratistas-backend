using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Infrastructure.Models
{
    /// <summary>
    /// Habilitación de un proyecto para el módulo SSOMA. Independiente del estado
    /// genérico de "project" (active/estado), porque cada módulo decide su propio
    /// subconjunto de proyectos habilitados sin afectar a otros módulos.
    /// </summary>
    [Table("ss_proyecto_habilitado")]
    public class SsProyectoHabilitado
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("proyecto_id")]
        public int ProyectoId { get; set; }
        public Project Proyecto { get; set; } = null!;

        [Column("active")]
        public bool Active { get; set; } = false;

        [Column("created_date_time")]
        public DateTimeOffset CreatedDateTime { get; set; }

        [Column("created_user_id")]
        public int CreatedUserId { get; set; }

        [Column("updated_date_time")]
        public DateTimeOffset? UpdatedDateTime { get; set; }

        [Column("updated_user_id")]
        public int? UpdatedUserId { get; set; }

        [Column("state")]
        public bool State { get; set; } = true;
    }
}
