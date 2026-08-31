namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de distritos de residencia del postulante (tabla <c>gth_distrito</c>): distritos de
    /// Lima Metropolitana y de la Provincia Constitucional del Callao. <c>Provincia</c> agrupa a qué
    /// jurisdicción pertenece cada distrito (LIMA / CALLAO). En el formulario intranet se muestran
    /// juntos en un solo desplegable con búsqueda (el MS Forms original los separaba en dos preguntas).
    /// <c>codigo</c> es la clave estable.
    /// </summary>
    public class GthDistrito
    {
        public int GthDistritoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        /// <summary>Jurisdicción del distrito: LIMA (Lima Metropolitana) o CALLAO (Prov. Const. del Callao).</summary>
        public string Provincia { get; set; } = null!;

        public int Orden { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
