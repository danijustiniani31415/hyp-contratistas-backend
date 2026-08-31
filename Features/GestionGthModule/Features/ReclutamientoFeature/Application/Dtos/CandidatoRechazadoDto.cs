namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Un candidato que quedó rechazado en algún punto del proceso de un requerimiento, con la
    /// etapa en la que se lo rechazó. Arma el «Historial de candidatos rechazados» que ven tanto
    /// GTH (detalle del requerimiento) como el área solicitante (Estado del reclutamiento).
    ///
    /// Incluye a los candidatos de long lists anteriores (los que quedaron con <c>state = false</c>
    /// al enviar una nueva): cuando el solicitante rechaza a todos, el requerimiento vuelve a
    /// LONG_LIST y esos son precisamente los que hay que poder consultar para no volver a
    /// presentarlos.
    /// </summary>
    public class CandidatoRechazadoDto
    {
        public int CandidatoId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        /// <summary>Puesto del requerimiento al momento de cargar la long list (snapshot).</summary>
        public string? Puesto { get; set; }

        /// <summary>
        /// En qué long list del requerimiento estaba el candidato (1 = la primera). Sin este dato,
        /// dos rechazados en la etapa "Long list" de vueltas distintas se leen igual.
        /// </summary>
        public int NumeroLongList { get; set; }

        /// <summary>
        /// Etapa en la que se lo rechazó: <c>LONG_LIST</c> (revisión de CVs), <c>FORMULARIO</c> o
        /// <c>ENTREVISTAS</c> (descartado por GTH), <c>DECISION_FINAL</c> (el solicitante rechazó al
        /// finalista) o <c>EMO</c> (el seleccionado salió No Apto en su examen médico de ingreso).
        /// Se deriva del estado del candidato y del resultado de su evaluación.
        /// </summary>
        public string EtapaCodigo { get; set; } = string.Empty;

        /// <summary>Nombre de la etapa para mostrar ("Long list", "Entrevistas", "Decisión final").</summary>
        public string EtapaNombre { get; set; } = string.Empty;

        /// <summary>Quién lo rechazó: <c>SOLICITANTE</c>, <c>GTH</c> o <c>SALUD_OCUPACIONAL</c>.</summary>
        public string RechazadoPorCodigo { get; set; } = string.Empty;

        /// <summary>Nombre de quien lo rechazó ("Área solicitante" / "GTH" / "Salud Ocupacional").</summary>
        public string RechazadoPorNombre { get; set; } = string.Empty;

        /// <summary>
        /// true si GTH puede retomar el proceso con este candidato desde el punto en que se lo
        /// rechazó. Lo son todos menos los de la etapa <c>EMO</c>: un No Apto del examen médico no
        /// se revierte volviendo a elegir a la misma persona.
        ///
        /// Que sea true no significa que el botón esté disponible ahora: retomar solo se ofrece con
        /// el requerimiento en la fase EMO_NO_APTO, y eso lo sabe la pantalla por el estado del
        /// requerimiento. Acá se responde únicamente si ESTE candidato es retomable.
        /// </summary>
        public bool PuedeRetomar { get; set; }

        /// <summary>
        /// Momento del rechazo en hora de Perú (UTC-5). En los candidatos decididos antes de que
        /// existiera <c>gth_candidato.decision_date_time</c> cae a la fecha de actualización y,
        /// en última instancia, a la de creación: nunca queda una fila del historial sin fecha.
        /// </summary>
        public DateTime RechazadoEn { get; set; }

        /// <summary>Comentario interno que GTH registró sobre el candidato al cargar la long list.</summary>
        public string? Comentario { get; set; }

        // ── CV en SharePoint (para poder volver a revisarlo desde el historial) ─
        public string? CvNombre { get; set; }
        public string? CvUrl { get; set; }
    }
}
