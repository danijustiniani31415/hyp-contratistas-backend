using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Interfaces
{
    public interface IGestionSalidaService
    {
        Task<List<GestionSalidaListItemDto>> GetAll(GestionSalidaFiltersDto filters);

        /// <summary>Tabla ordenada y paginada (la vista principal de gestión de salidas).</summary>
        Task<PagedResult<GestionSalidaListItemDto>> GetPaged(GestionSalidaFiltersDto filters);
        /// <summary>
        /// Datos de los filtros. El árbol de áreas se recorta al alcance de visibilidad del
        /// usuario: quien ve todo (GTH / recepción) recibe el árbol completo; un gerente recibe
        /// su gerencia + descendientes; un jefe recibe su área + subáreas. Así el desplegable en
        /// cascada arranca en el nodo tope que cada usuario controla, no siempre en la gerencia.
        /// </summary>
        Task<GestionSalidaFilterDataDto> GetFilterData(int? currentUserId, bool seesAllOverride, bool tieneRolTesorero = false);
        Task<byte[]> GetExcel(GestionSalidaFiltersDto filters);
        Task Aprobar(int id, int reviewerUserId);
        Task Rechazar(int id, int reviewerUserId);

        /// <summary>
        /// El propio solicitante cancela una salida SUYA que esté Pendiente. Reutiliza la misma
        /// lógica de Solicitud de Salidas (guard de propiedad + estado). 403 si es de otro, 400 si
        /// no está Pendiente.
        /// </summary>
        Task Cancelar(int id, int userId);
        /// <summary>
        /// Marca solicitudes elegibles como Rendidas y genera la planilla de gasto por movilidad (PDF).
        /// Devuelve los bytes del PDF + cuántas se procesaron.
        /// </summary>
        /// <param name="ownerUserId">
        /// Si se indica, actúa como guard: todas las solicitudes deben pertenecer al trabajador de ese
        /// usuario (rendición desde el autoservicio del trabajador). Null = sin restricción (Gestión de Salidas).
        /// </param>
        Task<(byte[] Pdf, int Count)> RendirYGenerarPlanilla(IEnumerable<int> ids, int userId, int? ownerUserId = null);

        /// <summary>
        /// Rinde de una sola vez todas las salidas del MES ANTERIOR al actual (por fecha de salida,
        /// en hora de Perú) que estén listas: aprobadas, no rendidas y con todos sus trayectos
        /// cubiertos (captura con monto, o catálogo para TI). Las que no cumplen se ignoran.
        /// Devuelve la planilla generada + cuántas se rindieron.
        /// </summary>
        /// <param name="filters">
        /// Filtros vigentes de la pantalla (trabajador, área, proyecto). Se respetan tal cual: la
        /// acción rinde lo que el usuario está viendo. El estado, el rango de fechas y el filtro
        /// "Hoy" los fija el propio método.
        /// </param>
        Task<(byte[] Pdf, int Count)> RendirMesAnterior(GestionSalidaFiltersDto filters, int userId);

        /// <summary>
        /// Adjunta (o reemplaza) el PDF Consolidado del S10 de una salida ya rendida desde Gestión
        /// de Salidas. El ámbito decide si cubre toda la planilla de rendición o solo esa salida.
        /// </summary>
        Task<ConsolidadoS10Dto> UploadConsolidadoS10(int solicitudId, ConsolidadoS10Ambito ambito, IFormFile file, int userId);

        /// <summary>Detalle de una solicitud para el modal — devuelve null si no existe.</summary>
        Task<GestionSalidaDetalleDto?> GetDetalle(int id);

        /// <summary>Registra (o limpia) la hora real de salida. Para uso del rol USUARIO DE RECEPCIÓN.</summary>
        Task SetHoraSalidaReal(int id, TimeOnly? hora, int registradaPorUserId);

        /// <summary>Registra (o limpia) la hora real de retorno. Para uso del rol USUARIO DE RECEPCIÓN.</summary>
        Task SetHoraRetornoReal(int id, TimeOnly? hora, int registradaPorUserId);

        // ── Reembolso ────────────────────────────────────────────────────────

        /// <summary>
        /// Aprueba o rechaza en bloque el reembolso de las salidas indicadas y avisa por correo a
        /// cada solicitante. Solo entran las que están rendidas y con Consolidado del S10 adjunto.
        /// El correo es best-effort: si falla, la decisión ya quedó guardada.
        /// </summary>
        /// <param name="observacion">Obligatoria al rechazar: es lo que el trabajador va a subsanar.</param>
        Task<ReembolsoBulkResultDto> DecidirReembolso(
            IEnumerable<int> ids, bool aprobar, string? observacion, int reviewerUserId);

        /// <summary>
        /// Firma la planilla de rendición de las salidas indicadas: descarga el PDF original,
        /// le estampa la firma del usuario y sube la copia firmada a SharePoint sin tocar el
        /// original. Una planilla se firma una sola vez aunque la selección traiga varias de sus
        /// salidas. Las salidas firmadas pasan a estado Firmado.
        ///
        /// Lanza 409 si el usuario todavía no registró su firma: la pantalla usa ese código para
        /// abrir el modal donde la dibuja en el momento.
        /// </summary>
        Task<ReembolsoBulkResultDto> FirmarPlanillas(IEnumerable<int> ids, int userId);

        /// <summary>
        /// Marca como Pagadas las salidas Firmadas indicadas. Es la acción de Tesorería y el
        /// controller ya validó que el usuario entra como tesorero.
        /// </summary>
        Task<ReembolsoBulkResultDto> MarcarPagadas(IEnumerable<int> ids, int tesoreroUserId);
    }
}
