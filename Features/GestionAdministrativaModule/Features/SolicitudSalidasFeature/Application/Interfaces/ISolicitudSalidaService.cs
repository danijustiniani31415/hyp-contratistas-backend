using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Interfaces
{
    public interface ISolicitudSalidaService
    {
        Task<SolicitudSalidaFormDataDto> GetFormData(int? userId);
        Task<List<SolicitudSalidaListItemDto>> GetByUserId(int userId, SolicitudSalidaFiltersDto? filters = null);
        Task<SolicitudSalidaFilterDataDto> GetFilterData(int userId);
        /// <summary>
        /// Crea la solicitud. <paramref name="adjuntos"/> trae los documentos adjuntos por índice
        /// de trayecto (0-based); son obligatorios para los trayectos cuyo motivo tiene
        /// requiere_adjunto = true y se suben a la carpeta configurada (ga_adjunto_folder).
        /// </summary>
        Task<int> Create(SolicitudSalidaCreateDto dto, int? userId, IReadOnlyList<(int TrayectoIndex, IFormFile File)>? adjuntos = null);

        Task<string> ProcessAprobarFromEmail(string token);
        Task<string> ProcessRechazarFromEmail(string token, string? motivoRechazo);
        string RenderRechazarForm(string token);

        Task<SolicitudSalidaDetalleDto> GetDetalle(int solicitudId, int userId);

        /// <summary>
        /// El propio solicitante cancela una solicitud SUYA que esté Pendiente. Lanza 403 si la
        /// solicitud es de otro trabajador y 400 si no está Pendiente. Estado resultante: Cancelado.
        /// </summary>
        Task Cancelar(int solicitudId, int userId);

        /// <summary>Sube N (imagen, monto) a SharePoint, asociadas a un trayecto específico de una solicitud aprobada/no rendida del propio usuario.</summary>
        Task<List<SolicitudSalidaCapturaDto>> UploadCapturasToTrayecto(int trayectoId, IEnumerable<(IFormFile File, decimal Monto)> items, int userId);

        /// <summary>
        /// Ids de las salidas PROPIAS del mes anterior (por fecha de salida, en hora de Perú) que
        /// están listas para rendir: aprobadas, no rendidas y con todos sus trayectos cubiertos.
        /// Lanza 400 si no hay ninguna.
        /// </summary>
        Task<List<int>> GetIdsRendiblesMesAnterior(int userId);

        /// <summary>
        /// Adjunta (o reemplaza) el PDF Consolidado del S10 de una salida PROPIA ya rendida. El
        /// ámbito decide si el archivo cubre toda la planilla de rendición o solo esa salida.
        /// </summary>
        Task<ConsolidadoS10Dto> UploadConsolidadoS10(int solicitudId, ConsolidadoS10Ambito ambito, IFormFile file, int userId);

        /// <summary>Envía email de confirmación al solicitante de que su solicitud fue aprobada. Best-effort, no lanza.</summary>
        Task NotifySolicitanteAprobada(int solicitudId);

        /// <summary>Envía email al solicitante de que su solicitud fue rechazada (mismos destinatarios/copias que el de aprobación). Best-effort, no lanza.</summary>
        Task NotifySolicitanteRechazada(int solicitudId);

        /// <summary>
        /// El trabajador avisa a su jefe/revisor que ya adjuntó el Consolidado del S10 y su
        /// reembolso está listo para revisión. Solo funciona sobre salidas SUYAS que estén
        /// rendidas, con el consolidado adjunto y con el reembolso todavía abierto.
        ///
        /// El correo lleva un botón que abre Gestión de Salidas en esa solicitud. Respeta la
        /// configuración de correos: si el correo está apagado, no se envía y se avisa al usuario
        /// (a diferencia del resto de correos del flujo, este lo dispara una persona apretando un
        /// botón, así que el resultado tiene que ser visible).
        /// </summary>
        /// <returns>Mensaje para mostrar en la pantalla.</returns>
        Task<string> NotificarRevisorS10(int solicitudId, int userId);
    }
}
