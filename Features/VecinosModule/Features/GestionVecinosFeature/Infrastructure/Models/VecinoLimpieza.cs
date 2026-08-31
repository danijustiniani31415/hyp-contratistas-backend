namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    /// <summary>Catálogo de tipos de limpieza: "Área común", "Departamento".</summary>
    public class VecinoLimpiezaTipo
    {
        public int VecinoLimpiezaTipoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>
    /// Limpieza programada en una fecha concreta de un proyecto. <see cref="VecinoId"/> solo
    /// aplica cuando el tipo es "Departamento" (limpieza del depto de un vecino del proyecto).
    /// </summary>
    public class VecinoLimpieza
    {
        public int VecinoLimpiezaId { get; set; }

        public int ProjectId { get; set; }

        public int VecinoLimpiezaTipoId { get; set; }
        public VecinoLimpiezaTipo? Tipo { get; set; }

        public int? VecinoId { get; set; }
        public Vecino? Vecino { get; set; }

        public DateOnly Fecha { get; set; }
        public string? Descripcion { get; set; }

        // ── Atención de limpieza (evidencia de ejecución) ──────────────────────
        /// <summary>Archivo de atención de limpieza (evidencia de que se realizó).</summary>
        public string? AtencionArchivoUrl { get; set; }
        public string? AtencionOriginalFileName { get; set; }
        /// <summary>Compromiso al que se asocia la atención (solo para limpiezas de departamento).</summary>
        public int? AtencionVecinoCompromisoId { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
