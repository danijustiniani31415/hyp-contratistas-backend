using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Infrastructure.Interfaces
{
    public interface IGestionSalidaRepository
    {
        Task<List<GestionSalidaListItemDto>> GetAll(GestionSalidaFiltersDto filters);

        /// <summary>Igual que <see cref="GetAll"/> pero ordenado por la columna indicada y paginado.</summary>
        Task<PagedResult<GestionSalidaListItemDto>> GetPaged(GestionSalidaFiltersDto filters);
        /// <summary>
        /// Datos de los filtros (trabajadores, lugares y árbol de áreas). Cuando
        /// <paramref name="seesAll"/> es false, tanto los trabajadores como el árbol de áreas se
        /// recortan a <paramref name="visibleAreaScopeIds"/> (área del usuario hacia abajo). El
        /// propio trabajador del usuario siempre se incluye en la lista de trabajadores.
        /// </summary>
        Task<GestionSalidaFilterDataDto> GetFilterData(bool seesAll, List<int> visibleAreaScopeIds, int? currentUserId);
        Task Aprobar(int id, int reviewerUserId);
        Task Rechazar(int id, int reviewerUserId);

        /// <summary>
        /// Crea un registro <c>GaRendicion</c> con la info del PDF subido y marca como rendidas
        /// todas las solicitudes elegibles vinculándolas al rendicion. Todo en una transacción.
        /// </summary>
        Task<List<int>> CrearRendicionYMarcarBulk(
            IEnumerable<int> ids,
            int userId,
            string pdfUrl,
            string? pdfItemId,
            string pdfFilename,
            int numeroPlanilla);

        /// <summary>Consume el siguiente número de la secuencia <c>seq_planilla_numero</c>.</summary>
        Task<int> GetNextNumeroPlanillaAsync();

        /// <summary>
        /// Link de la carpeta de SharePoint (tabla singleton <c>ga_rendicion_folder</c>) donde se
        /// suben los PDF de planillas de rendición. Null si no hay carpeta configurada.
        /// </summary>
        Task<string?> GetRendicionFolderUrl();

        /// <summary>IDs elegibles (Aprobadas + No rendidas) sin tocar BD. Pre-flight.</summary>
        Task<List<int>> GetEligibleIdsForRendicion(IEnumerable<int> ids);

        /// <summary>
        /// Del set dado, devuelve los IDs que NO pertenecen al trabajador del usuario indicado
        /// (worker → person → user). Se usa como guard cuando el propio trabajador rinde sus salidas
        /// desde el autoservicio: solo puede rendir lo suyo.
        /// </summary>
        Task<List<int>> GetIdsNotOwnedByUser(IEnumerable<int> ids, int userId);

        /// <summary>
        /// Del set dado, devuelve solicitudes que tienen al menos UN trayecto SIN ninguna captura.
        /// (Una solicitud sin trayectos también se incluye como incompleta).
        /// </summary>
        Task<List<int>> GetIdsConTrayectosSinCapturas(IEnumerable<int> ids);

        /// <summary>Detalle completo (cabecera + trayectos con capturas + rendición si existe).</summary>
        Task<GestionSalidaDetalleDto?> GetDetalle(int id);

        /// <summary>Datos para armar la planilla — una fila por TRAYECTO de las solicitudes dadas.</summary>
        Task<List<RendicionItemDto>> GetRendicionData(List<int> solicitudIds);

        /// <summary>Registra (o limpia) la hora real en la que la persona salió. Solo se actualiza el campo extra; no afecta el flujo principal.</summary>
        Task SetHoraSalidaReal(int solicitudId, TimeOnly? hora, int registradaPorUserId);

        /// <summary>Registra (o limpia) la hora real en la que la persona retornó. Solo se actualiza el campo extra; no afecta el flujo principal.</summary>
        Task SetHoraRetornoReal(int solicitudId, TimeOnly? hora, int registradaPorUserId);

        // ── Reembolso ────────────────────────────────────────────────────────

        /// <summary>
        /// Aprueba o rechaza el reembolso de las salidas indicadas. Solo pasan las que están
        /// Rendidas, tienen Consolidado del S10 y su reembolso sigue Pendiente o Rechazado — el
        /// resto se ignora en silencio (la selección de la pantalla puede traer de todo).
        ///
        /// Un usuario no decide el reembolso de sus propias salidas, misma regla que la aprobación
        /// de la salida: la excepción son los Gerentes.
        /// </summary>
        /// <param name="aprobar">true = Aprobado; false = Rechazado (exige observación).</param>
        /// <returns>Ids de las salidas que efectivamente cambiaron de estado.</returns>
        Task<List<int>> DecidirReembolso(IEnumerable<int> ids, bool aprobar, string? observacion, int reviewerUserId);

        /// <summary>
        /// Las salidas listas para firmar de la selección: reembolso Aprobado, ya rendidas y con
        /// planilla. Devuelve, por planilla, el id de la rendición y el PDF que hay que estampar.
        /// </summary>
        Task<List<RendicionPorFirmarDto>> GetRendicionesPorFirmar(IEnumerable<int> ids);

        /// <summary>
        /// Guarda la copia firmada de una planilla y pasa a Firmado las salidas indicadas de esa
        /// planilla. Si la planilla ya estaba firmada se conserva el archivo anterior y solo se
        /// mueven los estados (dos jefes pueden firmar salidas distintas de la misma planilla).
        /// </summary>
        Task MarcarFirmadas(int rendicionId, IEnumerable<int> solicitudIds, int userId,
                            string? pdfUrl, string? pdfItemId, string? pdfFilename);

        /// <summary>
        /// Marca como Pagadas las salidas indicadas que estén Firmadas. Las demás se ignoran.
        /// Devuelve los ids que efectivamente cambiaron.
        /// </summary>
        Task<List<int>> MarcarPagadas(IEnumerable<int> ids, int tesoreroUserId);

        /// <summary>
        /// Datos de una salida para armar los correos del reembolso (trabajador, área, planilla,
        /// monto rendido y a quién avisar). Null si la salida no existe.
        /// </summary>
        Task<ReembolsoCorreoInfoDto?> GetReembolsoCorreoInfo(int solicitudId);
    }
}
