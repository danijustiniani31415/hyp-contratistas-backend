namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de la respuesta del candidato a su citación (tabla
    /// <c>gth_entrevista_respuesta</c>): CONFIRMADA (asistirá) o RECHAZADA (no podrá asistir).
    /// La responde el propio candidato desde los dos botones del correo de invitación, sin login.
    /// <c>codigo</c> es la clave estable usada en código.
    ///
    /// Es distinto de <see cref="GthCandidatoResultado"/>, que es el resultado que registra GTH
    /// <b>después</b> de la entrevista: esto es solo si el candidato acepta la cita o no.
    /// </summary>
    public class GthEntrevistaRespuesta
    {
        public int GthEntrevistaRespuestaId { get; set; }
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
