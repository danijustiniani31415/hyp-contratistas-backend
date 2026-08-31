using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_contratista_usuario_proyecto")]
    public class SsContratistaUsuarioProyecto
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("contratista_usuario_id")]
        public int ContratistaUsuarioId { get; set; }

        [Column("proyecto_id")]
        public int ProyectoId { get; set; }

        [ForeignKey(nameof(ContratistaUsuarioId))]
        public SsContratistaUsuario? ContratistaUsuario { get; set; }
    }
}
