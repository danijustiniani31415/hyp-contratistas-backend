namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Models
{
    /// <summary>
    /// Onboarding de un nuevo colaborador (tabla <c>gth_onboarding</c>): la continuación del proceso
    /// de Reclutamiento. Solo se puede abrir para un candidato SELECCIONADO cuyo requerimiento ya
    /// quedó CERRADO — es decir, después de que el área solicitante le dio el puesto.
    ///
    /// Arranca con la carga de la carta oferta y el envío de un enlace con token al correo personal
    /// del colaborador (<c>person.email</c>, que es el que GTH validó al aprobar su formulario de
    /// postulante), para que la lea y la firme en línea. La carta ya no viaja adjunta en el correo: el
    /// archivo va a SharePoint y la fila conserva el enlace, el token, a quién se le avisó y cuándo.
    ///
    /// Los datos de la vacante (código del requerimiento, puesto, área, razón social, jefe directo)
    /// no se copian: se leen por <c>gth_candidato → gth_requerimiento</c>, que es donde ya viven.
    /// </summary>
    public class GthOnboarding
    {
        public int GthOnboardingId { get; set; }

        /// <summary>
        /// FK al candidato seleccionado que da origen al onboarding. Único entre los vigentes: un
        /// seleccionado no puede tener dos onboardings abiertos.
        /// </summary>
        public int GthCandidatoId { get; set; }

        /// <summary>
        /// FK a <c>person</c>: la ficha de la data maestra del colaborador. Se resuelve del formulario
        /// aprobado del postulante (<c>gth_postulante_formulario.person_id</c>). Null solo si el
        /// formulario nunca se aprobó, caso en el que el correo se toma del formulario y la fase
        /// «Base maestra» quedará pendiente de crear la ficha.
        /// </summary>
        public int? PersonId { get; set; }

        /// <summary>FK a <c>gth_onboarding_fase</c>: en qué paso del checklist está.</summary>
        public int GthOnboardingFaseId { get; set; }

        /// <summary>FK a <c>gth_onboarding_estado</c>: el badge de la tabla.</summary>
        public int GthOnboardingEstadoId { get; set; }

        /// <summary>
        /// Fecha de ingreso pactada. La escribe GTH al abrir el onboarding: el requerimiento ya no
        /// lleva una fecha requerida con la que proponerla.
        /// </summary>
        public DateOnly? FechaIngreso { get; set; }

        // ── Carta oferta (archivo en SharePoint + trazabilidad del envío) ─────
        public string? CartaOfertaNombre { get; set; }
        public string? CartaOfertaUrl { get; set; }
        public string? CartaOfertaItemId { get; set; }
        public string? CartaOfertaDriveId { get; set; }

        /// <summary>Correo personal al que se envió la carta oferta.</summary>
        public string? CartaOfertaCorreo { get; set; }

        public DateTimeOffset? CartaOfertaEnviadaDateTime { get; set; }
        public int? CartaOfertaEnviadaUserId { get; set; }

        /// <summary>
        /// Token del enlace público con el que el postulante ve y firma su carta oferta. Es la única
        /// credencial de esa página, así que es único entre los onboardings vigentes. Se genera al
        /// abrir el onboarding y NO se rota al reenviar el enlace: un mismo postulante puede recibir
        /// el correo más de una vez y los dos enlaces tienen que seguir funcionando.
        /// </summary>
        public string? CartaOfertaToken { get; set; }

        // ── Carta oferta FIRMADA (la que el colaborador devuelve) ─────────────
        // Va aparte de la enviada porque son dos documentos distintos del mismo expediente: la
        // enviada es la propuesta y la firmada es la evidencia que abre el file digital. Se guarda
        // en la misma carpeta de SharePoint que la enviada.
        public string? CartaFirmadaNombre { get; set; }
        public string? CartaFirmadaUrl { get; set; }
        public string? CartaFirmadaItemId { get; set; }
        public string? CartaFirmadaDriveId { get; set; }

        public DateTimeOffset? CartaFirmadaSubidaDateTime { get; set; }
        public int? CartaFirmadaSubidaUserId { get; set; }

        /// <summary>
        /// Momento en que el postulante firmó la carta desde la página pública. Es lo que distingue
        /// las dos procedencias del documento firmado: con fecha lo firmó él en la intranet; en null
        /// con <see cref="CartaFirmadaUrl"/> llena lo subió GTH a mano (la vía de respaldo, que se
        /// conserva). Cuando firma el postulante <see cref="CartaFirmadaSubidaUserId"/> queda en null
        /// porque no es un usuario del sistema.
        /// </summary>
        public DateTimeOffset? CartaFirmadaPostulanteDateTime { get; set; }

        /// <summary>
        /// Momento en que GTH aprobó la carta firmada (RF-ONB-02). Mientras esté en null el
        /// onboarding no puede avanzar de fase: es la primera actividad obligatoria del checklist.
        /// </summary>
        public DateTimeOffset? CartaFirmadaAprobadaDateTime { get; set; }
        public int? CartaFirmadaAprobadaUserId { get; set; }

        // ── File digital del colaborador (RF-ONB-04) ──────────────────────────
        // La carpeta de SharePoint donde se guardan TODOS los documentos de este onboarding:
        // «{DNI} - {NOMBRE}» dentro de la biblioteca configurada, con una subcarpeta por tipo de
        // documento («Carta Oferta Enviada», «Carta Oferta Firmada»). Lo que se guarda acá es la
        // carpeta del colaborador —la padre—, no las subcarpetas: esas se resuelven al subir. Se
        // resuelve al enviar la carta oferta y se persiste acá para no volver a derivarla del nombre,
        // que puede cambiar en la base maestra después del envío.
        public string? FileDigitalDriveId { get; set; }
        public string? FileDigitalItemId { get; set; }

        /// <summary>Ruta legible de esa carpeta, solo para mostrarla en pantalla.</summary>
        public string? FileDigitalRuta { get; set; }

        /// <summary>Observación interna de GTH al abrir el onboarding (opcional).</summary>
        public string? Observacion { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
