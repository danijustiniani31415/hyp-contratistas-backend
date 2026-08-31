using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_hab_documento_version")]
    public class SsHabDocumentoVersion
    {
        public int Id { get; set; }
        public int? HabTrabajadorId { get; set; }
        public int? HabEmpresaId { get; set; }
        public int? HabEquipoId { get; set; }
        public int Version { get; set; } = 1;
        public string ArchivoUrl { get; set; } = string.Empty;
        public int? SubidoPorUserId { get; set; }
        public int? SubidoPorEmpresaId { get; set; }
        public string? EstadoAlSubir { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public string? EstadoAnterior { get; set; }
        public int? AprobadoPorUserId { get; set; }
        public string? MotivoRechazo { get; set; }
        public bool Enviado { get; set; } = true;
        public DateTime? FechaEnvio { get; set; }
        public ICollection<SsHabDocumentoArchivo> Archivos { get; set; } = [];

        [ForeignKey(nameof(HabTrabajadorId))]
        public SsHabTrabajador? HabTrabajador { get; set; }

        [ForeignKey(nameof(HabEmpresaId))]
        public SsHabEmpresa? HabEmpresa { get; set; }

        [ForeignKey(nameof(HabEquipoId))]
        public SsHabEquipo? HabEquipo { get; set; }
    }
}
