using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Services
{
    /// <summary>
    /// Sube y consulta el PDF "Consolidado del S10" de una salida ya rendida. Lo usan las dos
    /// pantallas (Solicitud de Salidas y Gestión de Salidas), de ahí que viva en el Shared del módulo.
    /// </summary>
    public interface IConsolidadoS10Service
    {
        /// <summary>
        /// Sube el PDF y lo asocia al ámbito elegido. La salida debe estar Rendida; con
        /// <paramref name="ownerUserId"/> además debe pertenecer al trabajador de ese usuario
        /// (autoservicio). Si ya había un consolidado vigente para ese ámbito, queda con
        /// state = false y el nuevo pasa a ser el vigente.
        /// </summary>
        Task<ConsolidadoS10Dto> Upload(
            int solicitudId,
            ConsolidadoS10Ambito ambito,
            IFormFile file,
            int userId,
            int? ownerUserId = null);

        /// <summary>
        /// Consolidado vigente de una solicitud: el propio de la salida si tiene, sino el de su
        /// rendición. Null si no hay ninguno.
        /// </summary>
        Task<ConsolidadoS10Dto?> GetForSolicitud(int solicitudId);

        /// <summary>
        /// Igual que <see cref="GetForSolicitud"/> pero en lote, para no caer en N+1 al armar las
        /// tablas. Devuelve solo las solicitudes que tienen consolidado.
        /// </summary>
        Task<Dictionary<int, ConsolidadoS10Dto>> GetForSolicitudes(IEnumerable<int> solicitudIds);
    }
}
