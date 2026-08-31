namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Auditoria
{
    public class AuditoriaCambioDto
    {
        public long Id { get; set; }
        public string Tabla { get; set; } = string.Empty;
        public int RegistroId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? DatosAnteriores { get; set; }
        public string? DatosNuevos { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public int? EmpresaContratistaId { get; set; }
        public string? IpAddress { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
