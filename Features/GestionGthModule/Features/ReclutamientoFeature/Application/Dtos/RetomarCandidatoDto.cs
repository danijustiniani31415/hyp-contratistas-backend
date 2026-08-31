namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Resultado de retomar el proceso con un candidato del historial de rechazados: a qué fase
    /// volvió el requerimiento y desde qué etapa se lo retomó.
    ///
    /// Es la salida del camino que abre un EMO de ingreso No Apto: el requerimiento queda en
    /// EMO_NO_APTO y GTH elige a quién de los que ya se descartaron vuelve a poner en carrera. La
    /// fase de destino no la elige GTH — la decide la etapa en la que se rechazó al candidato,
    /// porque retomarlo significa continuar justo desde ahí y no volver a empezar el proceso.
    /// </summary>
    public class RetomarCandidatoResultDto
    {
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>Nombre del candidato retomado (para el aviso de la pantalla).</summary>
        public string CandidatoNombre { get; set; } = string.Empty;

        /// <summary>
        /// Etapa en la que se lo había rechazado y desde la que se retoma:
        /// <c>LONG_LIST</c>, <c>FORMULARIO</c>, <c>ENTREVISTAS</c> o <c>DECISION_FINAL</c>.
        /// </summary>
        public string EtapaCodigo { get; set; } = string.Empty;

        /// <summary>Nombre de esa etapa ("Long list", "Formulario", "Entrevistas", "Decisión final").</summary>
        public string EtapaNombre { get; set; } = string.Empty;

        /// <summary>Qué le toca hacer a GTH ahora, para el mensaje de confirmación de la pantalla.</summary>
        public string SiguientePaso { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lo que el repositorio devuelve al retomar a un candidato: el resultado que ve la pantalla
    /// más los datos del requerimiento y de su solicitante, que son los que necesita el correo de
    /// aviso (tipo <c>CANDIDATO_RETOMADO</c>).
    ///
    /// Va en un contexto aparte y no dentro de <see cref="RetomarCandidatoResultDto"/> porque el
    /// correo del solicitante no es asunto de la pantalla de GTH: mandarle su buzón al frontend
    /// sería exponer un dato que no usa.
    /// </summary>
    public class RetomarCandidatoContextoDto
    {
        public RetomarCandidatoResultDto Resultado { get; set; } = new();

        public int RequerimientoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }

        /// <summary>
        /// Correo corporativo del solicitante que registró la vacante: el destinatario del aviso.
        /// Null si su usuario ya no existe; ahí el correo no sale.
        /// </summary>
        public string? SolicitanteEmail { get; set; }

        public string? SolicitanteNombre { get; set; }
    }
}
