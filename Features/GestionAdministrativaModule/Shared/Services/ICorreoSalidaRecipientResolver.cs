using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Services
{
    /// <summary>
    /// Resuelve a quién se le manda un correo del flujo de solicitud de salidas a partir de la
    /// configuración editable (ga_correo_evento / ga_correo_regla): combina el destinatario
    /// principal y las copias base calculados en código con las inclusiones configuradas, quita
    /// las exclusiones y respeta los dos interruptores del correo. Reemplaza los correos que
    /// antes estaban hardcodeados (GTH, recepción).
    /// </summary>
    public interface ICorreoSalidaRecipientResolver
    {
        /// <summary>
        /// Devuelve a quién enviar el correo <paramref name="eventoCodigo"/> (REVISOR,
        /// CONFIRMACION, APROBADA, RECHAZADA):
        /// <list type="bullet">
        ///   <item>Si el correo está apagado (ga_correo_evento.active = false) ⇒ no se envía.</item>
        ///   <item>Para = <paramref name="destinatarioPrincipal"/>, salvo que su interruptor esté
        ///     apagado (destinatario_principal_activo = false), en cuyo caso queda fuera. Las
        ///     exclusiones nunca aplican al principal, solo a las copias.</item>
        ///   <item>Copia = (<paramref name="baseCc"/> ∪ inclusiones) − exclusiones − Para, sin
        ///     duplicados (case-insensitive) ni vacíos.</item>
        ///   <item>Si no queda ningún "Para", las copias pasan a ser el "Para"; si tampoco hay
        ///     copias, no se envía nada.</item>
        /// </list>
        /// Es best-effort: ante cualquier error devuelve el principal y las copias base tal cual
        /// (el correo debe enviarse igual).
        /// </summary>
        Task<CorreoSalidaEnvioDto> ResolveEnvioAsync(
            string eventoCodigo,
            IEnumerable<string>? destinatarioPrincipal = null,
            IEnumerable<string>? baseCc = null);
    }
}
