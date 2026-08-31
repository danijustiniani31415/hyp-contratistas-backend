using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces
{
    /// <summary>
    /// Persistencia de la aprobación de una solicitud de personal en sus dos niveles —gerente del
    /// área y Gerencia General— (<c>gth_aprobacion_gg</c> + su detalle por vacante).
    ///
    /// Las lecturas y la decisión reciben el <see cref="AprobacionScope"/> del usuario: es acá
    /// donde se aplica, para que ninguna consulta pueda devolver una solicitud fuera de su alcance.
    /// </summary>
    public interface IAprobacionGgRepository
    {
        /// <summary>
        /// Crea la aprobación con sus dos casillas en PENDIENTE (y una fila de detalle por vacante)
        /// y devuelve el contexto para armar el correo. Si ya existía una aprobación vigente
        /// devuelve esa (idempotente): el token no se regenera para que el enlace ya enviado siga
        /// sirviendo.
        /// </summary>
        Task<AprobacionGgEnvioContextoDto> PrepararEnvio(int solicitudId, string nuevoToken, int? userId);

        /// <summary>
        /// Contexto del correo de una aprobación existente, con el alcance de «Solicitud de
        /// Personal» (el área del usuario, no solo lo suyo) para el reenvío desde su panel. Null si
        /// no existe o queda fuera de su alcance.
        /// </summary>
        Task<AprobacionGgEnvioContextoDto?> GetEnvioContextoByRequerimiento(
            int requerimientoId, SolicitudPersonalScope scope);

        /// <summary>
        /// Contexto del correo sin pasar por la aprobación: el ingreso directo FFT se salta ese
        /// paso, así que no hay fila que crear ni decisión que leer. Null si la solicitud no existe
        /// o se dio de baja.
        /// </summary>
        Task<AprobacionGgEnvioContextoDto?> GetContextoSinAprobacion(int solicitudId);

        /// <summary>Registra los destinatarios y el momento del envío del correo (o del reenvío).</summary>
        Task RegistrarEnvio(int aprobacionId, List<string> principales, List<string> copias, bool esReenvio, int? userId);

        /// <summary>
        /// Pantalla «Aprobaciones» completa: tarjetas de resumen + las solicitudes que el usuario
        /// alcanza (pendientes de SU decisión e historial), en dos roundtrips. Vacía —sin tocar la
        /// BD— cuando el usuario no es gerente de nada.
        /// </summary>
        Task<AprobacionGgBandejaDto> GetBandeja(AprobacionScope scope);

        /// <summary>
        /// Detalle de una aprobación (cabecera + vacantes + las dos casillas). Null si no existe o
        /// se dio de baja; 403 si existe pero está fuera del alcance del usuario.
        /// </summary>
        Task<AprobacionGgDetalleDto?> GetDetalle(int aprobacionId, AprobacionScope scope);

        /// <summary>
        /// Registra la decisión del usuario en la casilla de SU nivel:
        ///   • Gerencia General: guarda el detalle, mueve cada requerimiento (aprobado →
        ///     VALIDACION_GTH, rechazado → RECHAZADO_GG) y cierra la solicitud.
        ///   • Gerente del área: solo guarda su visto bueno; no toca el pipeline.
        /// Devuelve el contexto con las vacantes de esa decisión (el servicio decide si avisa a GTH).
        /// </summary>
        Task<AprobacionGgDecisionContextoDto> RegistrarDecision(
            int aprobacionId, AprobacionGgDecisionDto dto, int userId, AprobacionScope scope);

        /// <summary>
        /// Registra la MISMA decisión (aprobar o rechazar todas las vacantes) en la casilla del
        /// nivel del usuario sobre varias solicitudes a la vez, en un solo lote de escritura. Las
        /// solicitudes que ya no admiten su decisión —fuera de alcance, dadas de baja, sin vacantes
        /// o con su casilla ya cerrada— se devuelven como omitidas en vez de tumbar el lote
        /// completo: en una selección de diez, que una se haya cerrado no invalida las otras nueve.
        /// </summary>
        Task<AprobacionGgDecisionMasivaContextoDto> RegistrarDecisionMasiva(
            List<int> aprobacionIds, bool aprobado, string? comentario, int userId, AprobacionScope scope);

        /// <summary>
        /// Resumen de la aprobación de un requerimiento (ambos niveles) para la tarjeta del
        /// seguimiento. Null en los requerimientos que no pasaron por el paso de aprobación.
        /// </summary>
        Task<AprobacionGgResumenDto?> GetResumenByRequerimiento(int requerimientoId);
    }
}
