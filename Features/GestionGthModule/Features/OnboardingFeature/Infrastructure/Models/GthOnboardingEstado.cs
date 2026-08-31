namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del estado de un onboarding (tabla <c>gth_onboarding_estado</c>): es el badge de la
    /// columna «Estado» de la tabla de colaboradores ingresados.
    ///
    /// CARTA_ENVIADA (GTH le envió la carta oferta al correo personal y espera que la devuelva
    /// firmada), EN_PROCESO (ya avanzó de la carta y está recorriendo el checklist) y COMPLETO
    /// (terminó el onboarding y sus datos ya están en la base maestra). <c>codigo</c> es la clave
    /// estable usada en código.
    ///
    /// Va aparte de <see cref="GthOnboardingFase"/> a propósito: la fase dice DÓNDE está el
    /// colaborador y el estado dice CÓMO va ahí (esperando algo de él, avanzando o cerrado).
    /// </summary>
    public class GthOnboardingEstado
    {
        public int GthOnboardingEstadoId { get; set; }
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
