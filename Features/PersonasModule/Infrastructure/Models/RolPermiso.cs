using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    [Table("lb_rol_permiso")]
    [PrimaryKey(nameof(RolId), nameof(PermisoId))]
    public class RolPermiso
    {
        public short RolId { get; set; }
        public int PermisoId { get; set; }

        [ForeignKey(nameof(RolId))]
        public Rol? Rol { get; set; }
        [ForeignKey(nameof(PermisoId))]
        public Permiso? Permiso { get; set; }
    }
}
