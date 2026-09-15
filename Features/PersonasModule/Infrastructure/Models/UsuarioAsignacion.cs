using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>
    /// Rol + permiso + scope (proyecto/almacén) + vigencia. El corazón del control de acceso —
    /// ver la query de resolución en CONTEXT_LOGISTICA.md sección 4.4 para armar el JWT.
    /// AlmacenId es el scope fino (NULL = todos los almacenes de ese proyecto); OtorgadoPor
    /// registra quién concedió el rol, relevante porque ALMACENERO/RESIDENTE controlan acceso
    /// físico al socavón, no solo datos.
    /// </summary>
    [Table("lb_usuario_asignacion")]
    public class UsuarioAsignacion
    {
        public long Id { get; set; }
        public long UsuarioSistemaId { get; set; }
        public short RolId { get; set; }
        public int? ProyectoId { get; set; }
        public int? AlmacenId { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public long? OtorgadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(UsuarioSistemaId))]
        public UsuarioSistema? UsuarioSistema { get; set; }
        [ForeignKey(nameof(RolId))]
        public Rol? Rol { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }
        [ForeignKey(nameof(AlmacenId))]
        public Almacen? Almacen { get; set; }
    }
}
