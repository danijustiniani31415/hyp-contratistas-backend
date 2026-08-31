using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces
{
    /// <summary>
    /// Aprobación de la solicitud de personal: primer paso del flujo. Manda UN correo con todas las
    /// vacantes al Gerente General y al gerente del área del solicitante, recibe la decisión de
    /// cada uno desde la pantalla «Aprobaciones» (con sesión iniciada) y, cuando la que decide es
    /// Gerencia General, notifica a GTH las vacantes aprobadas.
    ///
    /// Quién ve y quién decide qué lo resuelve <see cref="IAprobacionScopeResolver"/> desde la
    /// categoría de la ficha de trabajador, no desde el rol: el rol solo abre la pantalla.
    /// </summary>
    public interface IAprobacionGgService
    {
        /// <summary>
        /// Envía el correo de aprobación a los gerentes (destinatarios del tipo APROBACION_GG:
        /// Gerente General + gerente del área del solicitante). Devuelve true si el correo salió;
        /// false si no se pudo enviar (sin destinatarios configurados o error del proveedor) — la
        /// solicitud queda esperando un reenvío. No lanza: la solicitud ya está registrada y no
        /// debe caerse por el correo.
        /// </summary>
        Task<bool> EnviarSolicitudAGerencia(int solicitudId, int? userId);

        /// <summary>
        /// Envía a GTH el aviso de las vacantes de ingreso directo <b>FFT</b> de la solicitud
        /// (destinatarios del tipo FFT_SOLICITUD_GG). En esas vacantes reemplaza al correo de
        /// aprobación: un ingreso directo no lo aprueba nadie —quien pide ya nombró a la persona—
        /// así que este es el correo que arranca (y casi termina) su flujo: GTH ya tiene un
        /// candidato al que programarle su EMO de ingreso. Lleva SOLO las vacantes FFT, para que en
        /// una solicitud mixta no arrastre las que sí están esperando una firma, y sale <b>un correo
        /// por vacante</b>: así el botón lleva al detalle de ese requerimiento y le abre a GTH el
        /// modal donde le programa el EMO.
        /// Devuelve true si salieron todos; false si no había destinatarios o falló alguno. No
        /// lanza, por el mismo motivo que <see cref="EnviarSolicitudAGerencia"/>.
        /// </summary>
        Task<bool> EnviarIngresoDirectoAGth(int solicitudId, int? userId);

        /// <summary>
        /// Reenvía el correo de aprobación desde el panel del solicitante (para cuando el primer
        /// envío falló o hubo que corregir los destinatarios). Mismo token: el enlace anterior
        /// sigue siendo válido.
        /// </summary>
        Task<AprobacionGgReenvioResultDto> Reenviar(int requerimientoId, int? userId);

        /// <summary>
        /// Pantalla «Aprobaciones» para este usuario: su nivel, las tarjetas de resumen calculadas
        /// contra SU casilla y las solicitudes que alcanza, en una sola petición.
        /// </summary>
        Task<AprobacionGgBandejaDto> GetBandeja(int? userId);

        /// <summary>
        /// Detalle de una aprobación para el modal (de decisión o de solo consulta). 403 si la
        /// solicitud está fuera del alcance del usuario.
        /// </summary>
        Task<AprobacionGgDetalleDto> GetDetalle(int aprobacionId, int? userId);

        /// <summary>
        /// Registra la decisión del usuario en la casilla de su nivel. Si quien decide es Gerencia
        /// General, además notifica a GTH las vacantes aprobadas (correo del tipo SOLICITUD +
        /// campanita); el visto bueno del gerente del área no dispara ningún correo.
        /// </summary>
        Task<AprobacionGgDecisionResultDto> RegistrarDecision(int aprobacionId, AprobacionGgDecisionDto dto, int? userId);

        /// <summary>
        /// Registra la MISMA decisión (aprobar o rechazar todas las vacantes) sobre varias
        /// solicitudes seleccionadas en la lista, en la casilla del nivel del usuario. Si quien
        /// decide es Gerencia General, cada solicitud aprobada dispara sus correos a GTH y a TI —los
        /// mismos que dispara la decisión de una— pero los destinatarios se resuelven una sola vez
        /// para todo el lote. Las solicitudes que ya no admitían la decisión se devuelven como
        /// omitidas.
        /// </summary>
        Task<AprobacionGgDecisionMasivaResultDto> RegistrarDecisionMasiva(
            AprobacionGgDecisionMasivaDto dto, int? userId);
    }
}
