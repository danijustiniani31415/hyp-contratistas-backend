using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces
{
    /// <summary>
    /// Acceso a datos del formulario de información del postulante (tabla <c>gth_postulante_formulario</c>
    /// + sus catálogos). Cubre las dos caras: la pública del postulante (por token) y la de GTH
    /// (enviar, revisar, aprobar/rechazar).
    /// </summary>
    public interface IPostulanteFormularioRepository
    {
        /// <summary>
        /// Formulario público por token (contexto + catálogos + respuestas guardadas + estado), en 1
        /// roundtrip lógico. Devuelve null si el token no corresponde a ningún formulario vigente.
        /// </summary>
        Task<PostulanteFormularioPublicoDto?> GetByToken(string token);

        /// <summary>
        /// Lo necesario para subir el CV documentado del postulante antes de guardar el formulario:
        /// código del requerimiento (nombra la carpeta y el archivo en SharePoint), candidato y si
        /// ya tenía un CV cargado. Devuelve null si el token no corresponde a ningún formulario
        /// vigente. Solo se consulta cuando el envío trae archivo o cuando hay que exigirlo.
        /// </summary>
        Task<PostulanteCvContextoDto?> GetCvContexto(string token);

        /// <summary>
        /// Guarda las respuestas del postulante (por token) y avanza el formulario a COMPLETADO. Sirve
        /// tanto para el primer envío como para la corrección de un formulario RECHAZADO. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si el token no existe y
        /// 409 si el formulario ya fue APROBADO por GTH (único estado de solo lectura). Devuelve la
        /// cabecera del proceso para el correo que le avisa a GTH que ya lo puede revisar.
        /// </summary>
        /// <param name="cv">
        /// CV documentado ya subido a SharePoint en este envío, o null si el postulante no adjuntó
        /// ninguno (conserva el del envío anterior, si lo había).
        /// </param>
        Task<FormularioCompletadoContextoDto> GuardarRespuestasByToken(
            string token, PostulanteFormularioRespuestasDto respuestas, PostulanteCvSubidaDto? cv);

        /// <summary>
        /// Prepara el envío del formulario a un candidato APROBADO: crea (o reactiva) el formulario con
        /// estado ENVIADO usando <paramref name="nuevoToken"/> si aún no existía (si existe se conserva su
        /// token), y devuelve el contexto para armar el correo. El reenvío de un formulario RECHAZADO es
        /// la excepción: conserva ese estado con sus observaciones y marca el contexto como rechazo, para
        /// que se repita el correo de correcciones en vez del de invitación. Un formulario ya APROBADO
        /// también se puede reenviar (si el postulante se equivocó en un dato): vuelve a ENVIADO
        /// conservando sus respuestas. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si el candidato no existe
        /// y 400 si no está aprobado por el solicitante.
        /// </summary>
        Task<EnviarFormularioContextoDto> PrepararEnvio(int candidatoId, string correo, string nuevoToken, int? userId);

        /// <summary>
        /// Versión por lote de <see cref="PrepararEnvio"/> (envío del formulario a varios candidatos a la
        /// vez), con las mismas reglas de estado. Resuelve todo el lote con 3 consultas y un único
        /// guardado, sin importar cuántos candidatos vengan. A diferencia del individual no lanza
        /// excepción por un candidato inválido: ese envío queda con <c>Error</c> y los demás siguen su
        /// curso, para que un candidato mal seleccionado no cancele el envío de los otros.
        /// </summary>
        Task<List<EnvioMasivoPreparadoDto>> PrepararEnvioMasivo(
            IReadOnlyList<EnvioMasivoSolicitudDto> solicitudes, int? userId);

        /// <summary>
        /// Vista de GTH del formulario de un candidato (modal "Ver formulario"): estado + trazabilidad +
        /// datos (catálogos resueltos a nombre) si el postulante ya completó. Nunca es null: si GTH aún no
        /// envió el formulario devuelve <c>Existe = false</c>.
        /// </summary>
        Task<FormularioRevisionDto> GetRevision(int candidatoId);

        /// <summary>
        /// Registra la decisión de GTH sobre el formulario completado (aprobar/rechazar), guardando el
        /// revisor (id + nombre snapshot) y la fecha. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si no existe el formulario
        /// y 409 si aún no está COMPLETADO (o ya fue revisado). Devuelve el resumen para refrescar el modal
        /// y, cuando la decisión es un rechazo, el contexto del correo que se le envía al postulante.
        /// </summary>
        Task<DecisionFormularioContextoDto> RegistrarDecision(int candidatoId, bool aprobado, string? motivo, int? userId);
    }
}
