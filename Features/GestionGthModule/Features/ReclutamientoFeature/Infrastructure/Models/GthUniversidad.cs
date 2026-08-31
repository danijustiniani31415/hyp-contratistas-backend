namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de universidades/institutos del postulante (tabla <c>gth_universidad</c>):
    /// las principales universidades del país más la opción "Otras". Alimenta el desplegable
    /// del formulario del postulante. <c>codigo</c> es la clave estable.
    /// </summary>
    public class GthUniversidad
    {
        public int GthUniversidadId { get; set; }
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
