namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del motivo de cese de la última experiencia laboral del postulante
    /// (tabla <c>gth_motivo_cese</c>): Actualmente sigo laborando ahí / Renuncia personal /
    /// Término de contrato / Mutuo acuerdo / Cese colectivo / Quiebre o cierre del empleador /
    /// Otras. Alimenta el desplegable del formulario del postulante. <c>codigo</c> es la clave estable.
    /// </summary>
    public class GthMotivoCese
    {
        public int GthMotivoCeseId { get; set; }
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
