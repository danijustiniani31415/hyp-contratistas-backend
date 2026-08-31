using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("worker_eventos")]
    public class WorkerEvento
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string TipoEvento { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int? ProyectoAnteriorId { get; set; }
        public int? ProyectoNuevoId { get; set; }
        public int? EmpresaAnteriorId { get; set; }
        public int? EmpresaNuevaId { get; set; }
        public string? Datos { get; set; }
        public int? UsuarioId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
