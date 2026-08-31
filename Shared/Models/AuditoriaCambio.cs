using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Shared.Models
{
    [Table("auditoria_cambios")]
    public class AuditoriaCambio
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
