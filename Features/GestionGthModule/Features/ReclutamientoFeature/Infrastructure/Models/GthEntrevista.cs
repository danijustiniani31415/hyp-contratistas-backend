namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Entrevista programada de un candidato (tabla <c>gth_entrevista</c>): fecha, hora y lugar de
    /// la cita, más el correo al que se envió la invitación. Hay como máximo una fila vigente por
    /// candidato: reprogramar actualiza esta misma fila y reenvía la invitación, dejando la nueva
    /// fecha/hora y el momento del último envío.
    ///
    /// Solo se programa a candidatos cuyo formulario de información del postulante fue APROBADO
    /// por GTH (<see cref="GthPostulanteFormulario"/>).
    /// </summary>
    public class GthEntrevista
    {
        public int GthEntrevistaId { get; set; }

        /// <summary>FK al candidato entrevistado (1:1 entre las filas vigentes).</summary>
        public int GthCandidatoId { get; set; }

        /// <summary>Fecha de la entrevista (hora de Perú; es una fecha de calendario, sin zona).</summary>
        public DateOnly Fecha { get; set; }

        /// <summary>Hora de la entrevista en formato 24h (hora de Perú).</summary>
        public TimeOnly Hora { get; set; }

        /// <summary>FK a <c>gth_lugar_entrevista</c>: dónde se cita al candidato.</summary>
        public int GthLugarEntrevistaId { get; set; }

        /// <summary>Correo del postulante al que se envió la invitación.</summary>
        public string CorreoEnvio { get; set; } = null!;

        /// <summary>
        /// Token de acceso público con el que el candidato confirma o rechaza la cita desde los
        /// botones del correo, sin login (mismo mecanismo que el del formulario del postulante).
        /// Único entre las filas vigentes. Se regenera en cada envío: al reprogramar, los botones
        /// del correo anterior dejan de responder por una cita que ya cambió de fecha.
        ///
        /// Null en las entrevistas programadas antes de que existieran los botones.
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// FK a <c>gth_entrevista_respuesta</c>: CONFIRMADA o RECHAZADA. Null mientras el candidato
        /// no responda — que es distinto de rechazar, así que no se rellena con un valor por defecto.
        /// </summary>
        public int? GthEntrevistaRespuestaId { get; set; }

        /// <summary>Momento en que el candidato respondió. Null si aún no respondió.</summary>
        public DateTimeOffset? RespuestaDateTime { get; set; }

        /// <summary>Momento del último envío de la invitación (se refresca al reprogramar).</summary>
        public DateTimeOffset? EnviadoDateTime { get; set; }

        /// <summary>Usuario de GTH que envió la última invitación.</summary>
        public int? EnviadoUserId { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
