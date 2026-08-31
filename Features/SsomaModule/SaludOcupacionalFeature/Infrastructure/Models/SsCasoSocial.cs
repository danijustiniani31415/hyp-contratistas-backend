using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;
using Abril_Backend.Features.CostsModule.Shared.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_caso_social")]
    public class SsCasoSocial
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("proyecto_id")]
        public int? ProyectoId { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("fecha_apertura")]
        public DateOnly FechaApertura { get; set; }

        [Column("tipo_caso")]
        public string TipoCaso { get; set; } = string.Empty;

        [Column("prioridad")]
        public string Prioridad { get; set; } = string.Empty;

        [Column("motivo")]
        public string? Motivo { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Abierto";

        [Column("fecha_cierre")]
        public DateOnly? FechaCierre { get; set; }

        [Column("resultado")]
        public string? Resultado { get; set; }

        [Column("registrado_por_id")]
        public int? RegistradoPorId { get; set; }

        [Column("cerrado_por_id")]
        public int? CerradoPorId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("accidente_id")]
        public int? AccidenteId { get; set; }

        [Column("state")]
        public bool State { get; set; } = true;

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public Contributor? Empresa { get; set; }

        public ICollection<SsCasoSocialSeguimiento> Seguimientos { get; set; } = [];
    }
}
