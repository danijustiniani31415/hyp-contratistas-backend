using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_equipo")]
    public class SsEquipo
    {
        public int Id { get; set; }
        public int TipoEquipoId { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? NSerie { get; set; }
        public string? NVin { get; set; }
        public string? Capacidad { get; set; }
        public int? PropietarioEmpresaId { get; set; }
        public int ProyectoId { get; set; }
        public string? DatosEquipo { get; set; }
        public bool Activo { get; set; } = true;
        public int? IdLegacy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(PropietarioEmpresaId))]
        public Contributor? PropietarioEmpresa { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        [ForeignKey(nameof(TipoEquipoId))]
        public SsTipoEquipo? TipoEquipo { get; set; }
    }
}
