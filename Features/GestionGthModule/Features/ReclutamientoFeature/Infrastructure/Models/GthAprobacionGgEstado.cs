namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del estado de la aprobación de Gerencia General
    /// (tabla <c>gth_aprobacion_gg_estado</c>). <c>codigo</c> es la clave estable usada en código:
    /// <c>PENDIENTE</c> → el correo se envió y el GG todavía no decide.
    /// <c>APROBADA</c> → aprobó todas las vacantes de la solicitud.
    /// <c>APROBADA_PARCIAL</c> → aprobó algunas y rechazó el resto.
    /// <c>RECHAZADA</c> → rechazó todas.
    /// </summary>
    public class GthAprobacionGgEstado
    {
        public int GthAprobacionGgEstadoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int Orden { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
