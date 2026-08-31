using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Interfaces
{
    public interface ISolicitudSalidaRepository
    {
        Task<SolicitudSalidaFormDataDto> GetFormData();
        Task<List<SolicitudSalidaListItemDto>> GetByUserId(int userId, SolicitudSalidaFiltersDto? filters = null);
        Task<SolicitudSalidaFilterDataDto> GetFilterData(int userId);

        /// <summary>Crea la solicitud + sus trayectos en una transacción. Devuelve la solicitud (con trayectos) y el worker solicitante.
        /// <paramref name="adjuntosPorIndice"/>: documentos adjuntos ya subidos a SharePoint por índice de trayecto (0-based); cada trayecto puede tener N.</summary>
        Task<(GaSolicitudSalida Solicitud, List<GaSolicitudTrayecto> Trayectos, Worker Solicitante)> Create(SolicitudSalidaCreateDto dto, int? userId, Dictionary<int, List<TrayectoAdjuntoSubidoDto>>? adjuntosPorIndice = null);

        /// <summary>Guarda el correo al que se envió la solicitud para aprobación (enviado_a_correo).</summary>
        Task SetEnviadoACorreo(int solicitudId, string correo);
        Task<GaSolicitudSalida?> Aprobar(int solicitudId);
        Task<GaSolicitudSalida?> Rechazar(int solicitudId, string? motivoRechazo);

        /// <summary>
        /// El propio solicitante cancela su solicitud. Valida que la solicitud sea del worker
        /// del usuario (403 si es de otro) y que esté en estado Pendiente (400 si no). Deja el
        /// estado en Cancelado y registra quién/cuándo la canceló.
        /// </summary>
        Task Cancelar(int solicitudId, int userId);

        /// <summary>Detalle de la solicitud (con trayectos y capturas). Solo retorna si pertenece al usuario.</summary>
        Task<SolicitudSalidaDetalleDto?> GetDetalleForUser(int solicitudId, int userId);

        /// <summary>Carga un trayecto verificando que pertenezca al user y que la solicitud esté aprobada + no rendida.</summary>
        Task<GaSolicitudTrayecto?> GetTrayectoForUploadingCapturas(int trayectoId, int userId);

        Task<List<SolicitudSalidaCapturaDto>> InsertCapturas(int trayectoId, IEnumerable<(string Url, string? ItemId, string Filename, decimal Monto)> items, int userId);
    }
}
