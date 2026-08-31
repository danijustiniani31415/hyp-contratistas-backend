namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Equipos
{
    public class SsHabDocumentoVersionEquipoDto
    {
        public int Id { get; set; }
        public int? HabEquipoId { get; set; }
        public int Version { get; set; }
        public string ArchivoUrl { get; set; } = string.Empty;
        public int? SubidoPorUserId { get; set; }
        public string? SubidoPorNombre { get; set; }
        public int? SubidoPorEmpresaId { get; set; }
        public string? EstadoAlSubir { get; set; }
        public string? EstadoAnterior { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public int? AprobadoPorUserId { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
