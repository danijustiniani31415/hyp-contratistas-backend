using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_equipo_prestado")]
    public class SsEquipoPrestado
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("accidente_id")]
        public int AccidenteId { get; set; }

        [Column("tipo_equipo_id")]
        public int TipoEquipoId { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; } = 1;

        [Column("fecha_prestamo")]
        public DateOnly FechaPrestamo { get; set; }

        [Column("fecha_devolucion")]
        public DateOnly? FechaDevolucion { get; set; }

        [Column("devuelto")]
        public bool Devuelto { get; set; } = false;

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Column("url_evidencia")]
        public string? UrlEvidencia { get; set; }

        [Column("registrado_por_id")]
        public int RegistradoPorId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("state")]
        public bool State { get; set; } = true;

        [ForeignKey(nameof(AccidenteId))]
        public SsAccidenteTrabajo? Accidente { get; set; }

        [ForeignKey(nameof(TipoEquipoId))]
        public SsEquipoTipo? TipoEquipo { get; set; }
    }
}
