namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de las fases del onboarding (tabla <c>gth_onboarding_fase</c>), en el orden en que
    /// las recorre un nuevo colaborador: CARTA_OFERTA_FIRMADA → FILE_DIGITAL → CORREO_BIENVENIDA →
    /// FORMULARIO_WEB → PREINICIO → CIERRE_ONBOARDING → BASE_MAESTRA.
    ///
    /// Es el mismo catálogo que pinta el embudo «Fases del onboarding» de la pantalla y el que dice
    /// en qué paso está cada colaborador. <c>codigo</c> es la clave estable usada en código;
    /// <c>orden</c> define la posición en el embudo y, con el total de fases, el % de avance.
    /// </summary>
    public class GthOnboardingFase
    {
        public int GthOnboardingFaseId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        /// <summary>Qué se hace en esta fase (texto de apoyo para la pantalla).</summary>
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
