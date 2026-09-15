using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    [Table("lb_rol")]
    public class Rol
    {
        public short Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool EsGlobal { get; set; }
        public bool Activo { get; set; } = true;

        public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
    }
}
