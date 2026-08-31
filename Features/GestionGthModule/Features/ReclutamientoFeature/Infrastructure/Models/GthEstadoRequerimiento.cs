namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de estados/fases operativos de un requerimiento de reclutamiento
    /// (tabla <c>gth_estado_requerimiento</c>). Representa el pipeline completo (9 fases): el
    /// requerimiento avanza por ellas según <c>orden</c>. Por ahora la creación solo asigna la
    /// fase inicial <c>NUEVO</c>; el avance por el resto del pipeline se irá agregando con las
    /// siguientes funcionalidades. <c>codigo</c> es la clave estable usada en código.
    /// </summary>
    public class GthEstadoRequerimiento
    {
        public int GthEstadoRequerimientoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        /// <summary>Descripción de la fase (texto de ayuda mostrado en el seguimiento vertical del requerimiento).</summary>
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
