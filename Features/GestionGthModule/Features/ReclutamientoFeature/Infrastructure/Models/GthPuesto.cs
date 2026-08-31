namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de puestos solicitables en Reclutamiento (tabla <c>gth_puesto</c>).
    /// Por ahora es una lista simple de nombres; a futuro se enriquecerá como "perfil de
    /// puesto" (área, equipamiento, accesos, tipo de contrato, duración) para alimentar el
    /// Onboarding. <c>state</c> = soft delete; <c>active</c> = visible en desplegables.
    /// </summary>
    public class GthPuesto
    {
        public int GthPuestoId { get; set; }
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
