using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_sctr_vidaley")]
    public class SsSctrVidaley
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int? ProyectoId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? ArchivoUrl2 { get; set; }
        public string Estado { get; set; } = "Falta";
        public string TipoPoliza { get; set; } = "Renovacion";
        public DateTime? FechaInicio { get; set; }
        public DateTime? Vigencia { get; set; }
        public string? ObsAbril { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public Contributor? Empresa { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        public ICollection<SsSctrVidaLeyWorker> Workers { get; set; } = [];
    }
}
