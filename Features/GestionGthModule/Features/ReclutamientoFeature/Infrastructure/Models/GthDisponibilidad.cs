namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de la disponibilidad de incorporación del postulante (tabla <c>gth_disponibilidad</c>):
    /// Inmediata / 1 semana / 10 a 15 días / Más de 15 días. Alimenta el desplegable del formulario
    /// del postulante. <c>codigo</c> es la clave estable. (Distinto del campo libre
    /// <c>disponibilidad</c> de <see cref="GthCandidato"/>, que lo estima GTH desde el CV.)
    /// </summary>
    public class GthDisponibilidad
    {
        public int GthDisponibilidadId { get; set; }
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
