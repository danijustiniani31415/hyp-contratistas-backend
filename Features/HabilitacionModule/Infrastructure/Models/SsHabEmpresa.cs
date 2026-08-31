using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_hab_empresa")]
    public class SsHabEmpresa
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int ProyectoId { get; set; }
        public int ItemId { get; set; }
        public int? Mes { get; set; }
        public int? Anio { get; set; }
        public string Estado { get; set; } = "Falta";
        public DateTime? Vigencia { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? ObsAbril { get; set; }
        public string? ObsContratista { get; set; }
        public int? AprobadoPor { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public Contributor? Empresa { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        [ForeignKey(nameof(ItemId))]
        public SsItemEmpresa? Item { get; set; }
    }
}
