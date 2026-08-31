namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de estado civil del postulante (tabla <c>gth_estado_civil</c>):
    /// Soltero(a) / Casado(a) / Divorciado(a) / Viudo(a) / Conviviente. Alimenta el
    /// desplegable del formulario del postulante. <c>codigo</c> es la clave estable.
    /// </summary>
    public class GthEstadoCivil
    {
        public int GthEstadoCivilId { get; set; }
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
