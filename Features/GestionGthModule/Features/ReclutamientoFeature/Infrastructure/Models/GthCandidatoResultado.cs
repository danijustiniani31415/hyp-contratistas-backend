namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del resultado de la entrevista de un candidato
    /// (tabla <c>gth_candidato_resultado</c>): PENDIENTE (aún sin cerrar), PASO (continúa
    /// como finalista) o NO_PASO (no continúa; se le envió el correo de agradecimiento).
    /// <c>codigo</c> es la clave estable usada en código.
    ///
    /// Es distinto de <see cref="GthCandidatoEstado"/>, que registra la decisión del
    /// solicitante sobre la long list (antes de la entrevista).
    /// </summary>
    public class GthCandidatoResultado
    {
        public int GthCandidatoResultadoId { get; set; }
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
