namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>Body del PATCH que marca/desmarca el check informativo del Multitest de un candidato.</summary>
    public class MultitestUpdateDto
    {
        /// <summary>true = el candidato ya rindió el Multitest.</summary>
        public bool Realizado { get; set; }
    }

    /// <summary>
    /// Body del POST que programa (o reprograma) la entrevista de un candidato. El correo del
    /// postulante no viaja en el body: lo resuelve el backend desde el formulario aprobado del
    /// candidato, para que GTH no pueda citar a una dirección distinta de la registrada.
    /// </summary>
    public class EntrevistaGuardarDto
    {
        public DateOnly Fecha { get; set; }

        /// <summary>Hora de la cita en formato "HH:mm" (24h), igual que el <c>app-time-picker</c>.</summary>
        public string Hora { get; set; } = string.Empty;

        /// <summary>Id de <c>gth_lugar_entrevista</c>.</summary>
        public int LugarId { get; set; }
    }

    /// <summary>Entrevista programada de un candidato, como la muestra la vista de GTH.</summary>
    public class EntrevistaResumenDto
    {
        public DateOnly Fecha { get; set; }

        /// <summary>Hora en formato "HH:mm" (24h).</summary>
        public string Hora { get; set; } = string.Empty;

        public int LugarId { get; set; }
        public string LugarNombre { get; set; } = string.Empty;

        /// <summary>Correo al que se envió la invitación.</summary>
        public string CorreoEnvio { get; set; } = string.Empty;

        /// <summary>Momento del último envío de la invitación (hora de Perú). Null si aún no se envió.</summary>
        public DateTime? EnviadoEn { get; set; }

        /// <summary>
        /// Respuesta del candidato a la citación (CONFIRMADA / RECHAZADA), la que dio desde los
        /// botones del correo. Null mientras no responda — que no es lo mismo que rechazar.
        /// </summary>
        public string? RespuestaCodigo { get; set; }

        /// <summary>Nombre visible de la respuesta ("Confirmada por el candidato"). Null si aún no responde.</summary>
        public string? RespuestaNombre { get; set; }

        /// <summary>Momento en que el candidato respondió (hora de Perú). Null si aún no responde.</summary>
        public DateTime? RespondidoEn { get; set; }
    }

    /// <summary>Resultado de programar/reprogramar una entrevista: mensaje + estado resultante para refrescar la fila.</summary>
    public class EntrevistaAccionResultDto
    {
        public string Message { get; set; } = string.Empty;
        public EntrevistaResumenDto Entrevista { get; set; } = new();
    }

    /// <summary>Datos que necesita el servicio para armar el correo de invitación a la entrevista.</summary>
    public class EntrevistaEnvioContextoDto
    {
        public string CandidatoNombre { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public EntrevistaResumenDto Resumen { get; set; } = new();

        /// <summary>
        /// Enlace al mapa del lugar (<c>gth_lugar_entrevista.maps_url</c>), para que el postulante
        /// sepa dónde encontrarnos. Null en los lugares que no lo tienen cargado: ahí el correo
        /// muestra solo el nombre del lugar.
        /// </summary>
        public string? LugarMapsUrl { get; set; }

        /// <summary>
        /// Token de acceso público con el que se arman los enlaces de los botones Confirmar y
        /// Rechazar del correo. Se genera nuevo en cada envío.
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Contexto de la respuesta del candidato a su citación: lo que necesita el servicio para
    /// avisarle a GTH y lo que la página pública le muestra al candidato después de responder.
    /// </summary>
    public class EntrevistaRespuestaContextoDto
    {
        public string CandidatoNombre { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Área solicitante del requerimiento (para el aviso a GTH).</summary>
        public string? Area { get; set; }

        /// <summary>Correo desde el que responde el candidato (al que se le envió la invitación).</summary>
        public string CorreoCandidato { get; set; } = string.Empty;

        /// <summary>Id del requerimiento, para que el aviso a GTH enlace a su bandeja.</summary>
        public int RequerimientoId { get; set; }

        /// <summary>
        /// Correo corporativo del solicitante que registró la vacante: es el destinatario del aviso
        /// de "entrevista confirmada", que le dice cuándo y dónde tiene que presentarse. Null en los
        /// requerimientos cuyo solicitante ya no tiene usuario del sistema; ahí ese correo no sale.
        /// </summary>
        public string? SolicitanteEmail { get; set; }

        /// <summary>Nombre del solicitante, para saludarlo en ese aviso.</summary>
        public string? SolicitanteNombre { get; set; }

        /// <summary>
        /// Enlace al mapa del lugar de la cita (<c>gth_lugar_entrevista.maps_url</c>), para que el
        /// solicitante sepa dónde es. Null en los lugares que no lo tienen cargado.
        /// </summary>
        public string? LugarMapsUrl { get; set; }

        /// <summary>La cita, ya con la respuesta aplicada.</summary>
        public EntrevistaResumenDto Resumen { get; set; } = new();

        /// <summary>
        /// true si el candidato volvió a pulsar el mismo botón (mismo enlace abierto dos veces).
        /// El aviso a GTH no se reenvía en ese caso: no cambió nada que contar.
        /// </summary>
        public bool YaHabiaRespondidoLoMismo { get; set; }
    }

    /// <summary>
    /// Lo que ve el candidato en la página pública después de pulsar Confirmar o Rechazar: su
    /// respuesta y la cita, para que la pantalla confirme sobre qué entrevista respondió.
    /// </summary>
    public class EntrevistaRespuestaPublicaDto
    {
        /// <summary>CONFIRMADA o RECHAZADA.</summary>
        public string RespuestaCodigo { get; set; } = string.Empty;

        public string CandidatoNombre { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }

        /// <summary>Hora en formato "HH:mm" (24h).</summary>
        public string Hora { get; set; } = string.Empty;

        public string LugarNombre { get; set; } = string.Empty;
    }
}
