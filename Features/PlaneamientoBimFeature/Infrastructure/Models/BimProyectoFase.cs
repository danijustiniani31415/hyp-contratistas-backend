using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    [Table("bim_proyecto_fase")]
    public class BimProyectoFase
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Column("fase_id")]
        public int FaseId { get; set; }
        public BimFase Fase { get; set; } = null!;

        [Column("fecha_inicio")]
        public DateOnly? FechaInicio { get; set; }

        [Column("fecha_fin_meta")]
        public DateOnly? FechaFinMeta { get; set; }

        [Column("fecha_fin_real")]
        public DateOnly? FechaFinReal { get; set; }
    }
}
