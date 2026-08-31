namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Evaluación de la entrevista de un candidato (tabla <c>gth_candidato_evaluacion</c>):
    /// los tres comentarios del informe que se comparte con el área solicitante y el
    /// resultado alcanzado.
    ///
    /// Hay como máximo una fila vigente por candidato: volver a guardar actualiza esta misma
    /// fila (igual que <see cref="GthEntrevista"/>). Solo se evalúa a los candidatos a los que
    /// ya se les envió la invitación a la entrevista.
    ///
    /// Cuando el candidato no continúa, GTH le envía el correo de agradecimiento: eso deja el
    /// resultado en NO_PASO y registra aquí a qué correo, cuándo y quién lo envió (RG-12 del
    /// requerimiento: no se cierra el resultado de un "No pasó" sin ese correo).
    /// </summary>
    public class GthCandidatoEvaluacion
    {
        public int GthCandidatoEvaluacionId { get; set; }

        /// <summary>FK al candidato evaluado (1:1 entre las filas vigentes).</summary>
        public int GthCandidatoId { get; set; }

        /// <summary>
        /// El candidato evaluado. Existe por el ingreso directo <b>FFT</b>: ahí el candidato y su
        /// evaluación (que lo deja SELECCIONADO) nacen en el mismo acto, y sin navegación habría que
        /// guardar dos veces —el <c>GthCandidatoId</c> no existe hasta el primer <c>SaveChanges</c>—
        /// partiendo en dos una operación que tiene que ser atómica. Con ella, EF resuelve la FK
        /// sola. El resto del módulo sigue leyendo por <see cref="GthCandidatoId"/>.
        /// </summary>
        public GthCandidato? Candidato { get; set; }

        /// <summary>FK a <c>gth_candidato_resultado</c>: PENDIENTE / PASO / NO_PASO.</summary>
        public int GthCandidatoResultadoId { get; set; }

        // ── Comentarios del informe de finalista ──────────────────────────────
        /// <summary>Resultado de la entrevista (qué se observó en la entrevista).</summary>
        public string? ComentarioEntrevista { get; set; }

        /// <summary>Informe psicotécnico del candidato.</summary>
        public string? ComentarioPsicotecnico { get; set; }

        /// <summary>Recomendación de GTH al área solicitante.</summary>
        public string? ComentarioRecomendacion { get; set; }

        // ── Correo de agradecimiento (no continuidad) ─────────────────────────
        /// <summary>Correo al que se envió el agradecimiento. Null si aún no se envió.</summary>
        public string? AgradecimientoCorreo { get; set; }

        /// <summary>Momento del envío del agradecimiento.</summary>
        public DateTimeOffset? AgradecimientoDateTime { get; set; }

        /// <summary>Usuario de GTH que envió el agradecimiento.</summary>
        public int? AgradecimientoUserId { get; set; }

        // ── Decisión final del área solicitante sobre el finalista ────────────
        /// <summary>
        /// Momento en que el solicitante aprobó (SELECCIONADO) o rechazó (RECHAZADO) al finalista.
        /// Null mientras no haya decidido.
        /// </summary>
        public DateTimeOffset? DecisionDateTime { get; set; }

        /// <summary>Usuario del área solicitante que tomó la decisión final.</summary>
        public int? DecisionUserId { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
