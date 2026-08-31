namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del grado académico del postulante (tabla <c>gth_grado_academico</c>):
    /// Egresado / Bachiller / Titulado / Magíster / Doctor / Técnico Superior /
    /// Bachiller Técnico / No culminado o trunco. Alimenta el desplegable del formulario
    /// del postulante. <c>codigo</c> es la clave estable.
    /// </summary>
    public class GthGradoAcademico
    {
        public int GthGradoAcademicoId { get; set; }
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
