namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del estado del formulario del postulante (tabla <c>gth_postulante_formulario_estado</c>):
    /// ENVIADO (GTH envió el enlace al correo del postulante y aún no lo completa), COMPLETADO
    /// (el postulante llenó el formulario y está pendiente de revisión de GTH), APROBADO o
    /// RECHAZADO (GTH ya revisó). <c>codigo</c> es la clave estable usada en código.
    /// </summary>
    public class GthPostulanteFormularioEstado
    {
        public int GthPostulanteFormularioEstadoId { get; set; }
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
