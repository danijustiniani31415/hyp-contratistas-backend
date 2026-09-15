using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    [Table("lb_empresa_contratista")]
    public class EmpresaContratista
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = null!;
        public string? Ruc { get; set; }
        public bool Activo { get; set; } = true;
    }
}
