namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo "Tipo de proceso y SLA" de un requerimiento (tabla <c>gth_tipo_proceso</c>).
    /// Valores iniciales: Junior (20 días), Semisenior (25 días), Senior (35 días). La
    /// clasificación la determina GTH considerando especialización, oferta del mercado,
    /// banda salarial y dificultad de búsqueda. <c>codigo</c> es la clave estable.
    /// </summary>
    public class GthTipoProceso
    {
        public int GthTipoProcesoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        /// <summary>SLA referencial del proceso, en días.</summary>
        public int SlaDias { get; set; }

        /// <summary>Descripción corta del tipo. Ya no se muestra en la UI; se conserva la columna.</summary>
        public string? Descripcion { get; set; }

        public int Orden { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
