using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces
{
    /// <summary>
    /// Configuración de los correos de Reclutamiento: qué correos existen, si están prendidos
    /// y quién recibe cada uno. Sirve a la pantalla de Configuración (lectura completa + CRUD
    /// de correos adicionales) y al envío real, que consume solo las filas activas.
    /// </summary>
    public interface ICorreoConfigRepository
    {
        /// <summary>
        /// Los correos indicados con su interruptor y todos sus destinatarios, resolviendo de
        /// paso los dinámicos que no dependen de la solicitud (Gerente General y área de GTH)
        /// para que la pantalla muestre a quién le llega de verdad. Una sola llamada.
        /// </summary>
        Task<CorreoConfigDto> GetConfigAsync(IReadOnlyList<string> tipoCodigos);

        /// <summary>
        /// Configuración de envío de un correo: sus destinatarios vigentes y activos (lista vacía si
        /// el correo está apagado con el interruptor maestro o no existe) más el interruptor del
        /// destinatario principal que pone el sistema, que es independiente del maestro.
        /// </summary>
        Task<CorreoEnvioConfigDto> GetEnvioConfigAsync(string tipoCodigo);

        /// <summary>Gerente General vigente (puesto "GERENTE GENERAL"); null si no hay uno con correo.</summary>
        Task<CorreoDestinatarioResueltoDto?> GetGerenteGeneralAsync();

        /// <summary>Correo del área de Gestión del Talento Humano; null si el área no tiene uno cargado.</summary>
        Task<string?> GetEmailAreaGthAsync();

        /// <summary>Correo del área de Tecnología de la Información; null si el área no tiene uno cargado.</summary>
        Task<string?> GetEmailAreaTiAsync();

        /// <summary>Alta de un correo adicional en un correo. Devuelve el id creado.</summary>
        Task<int> CreateAdicionalAsync(string tipoCodigo, string email, string? nombre, bool esCopia, int? userId);

        /// <summary>
        /// Edición de un correo adicional (los dinámicos no se editan). <paramref name="tiposPermitidos"/>
        /// acota la operación a los correos de la pantalla que la pide: un id de otra pantalla se
        /// rechaza en vez de editarse a ciegas.
        /// </summary>
        Task UpdateAdicionalAsync(
            int destinatarioId, string email, string? nombre, bool esCopia,
            IReadOnlyList<string> tiposPermitidos, int? userId);

        /// <summary>Prende o apaga un destinatario de alguno de los correos de la pantalla.</summary>
        Task SetDestinatarioActiveAsync(
            int destinatarioId, bool active, IReadOnlyList<string> tiposPermitidos, int? userId);

        /// <summary>Prende o apaga un correo completo (interruptor maestro).</summary>
        Task SetTipoActiveAsync(string tipoCodigo, bool active, int? userId);

        /// <summary>
        /// Prende o apaga el destinatario principal que pone el sistema (el solicitante, el
        /// postulante, el candidato…). 400 si el correo no tiene uno.
        /// </summary>
        Task SetPrincipalAutomaticoActiveAsync(string tipoCodigo, bool active, int? userId);

        /// <summary>Baja lógica de un correo adicional (los dinámicos no se eliminan, se apagan).</summary>
        Task DeleteAdicionalAsync(int destinatarioId, IReadOnlyList<string> tiposPermitidos, int? userId);
    }
}
