using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Dtos;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Interfaces
{
    public interface IActasReunionService
    {
        Task<ReunionPaginaInicialDto> GetPaginaInicial(ReunionFiltroRequest filtro, int userId);
        Task<PagedResultDto<ReunionListItemDto>> GetReuniones(ReunionFiltroRequest filtro, int userId);
        Task<ReunionDetalleDto> GetDetalle(int reunionId, int userId);
        Task<int> Create(ReunionCreateRequest request, int userId);
        Task<List<TrabajadorAbrilDto>> BuscarTrabajadoresPorFiltro(int? areaScopeId, List<int>? puestoIds, int? projectId);
        Task<List<CatalogoDto>> GetPuestos();
        Task<List<CatalogoDto>> GetPuestosPorArea(int? areaScopeId);
        Task<CatalogoDto> AgregarTema(string descripcion, int userId);
        Task<List<ReunionTemaOpcionDto>> GetTemasCatalogo();
        Task<TemaConvocatoriaDto> GetConvocatoriaTema(int reunionTemaId);
        Task GuardarConvocatoriaTema(int reunionTemaId, TemaConvocatoriaSaveRequest request, int userId);
        /// <summary>Elimina un tema del catálogo (borrado real). Devuelve cuántas reuniones existentes se desvincularon.</summary>
        Task<int> EliminarTema(int reunionTemaId);

        // ── Agenda de reunión ──────────────────────────────────────────────
        Task<ReunionAgendaDto> GetAgenda(int reunionId, int userId);
        Task GuardarMisTemas(int reunionId, int userId, GuardarMisTemasRequest request);
        Task<ReunionAgendaItemDto> AgregarTemaPuntual(int reunionId, int userId, string descripcion);
        Task EliminarTemaPuntual(int reunionId, int reunionAgendaItemId, int userId);
        Task<List<MisAcuerdoDto>> GetMisAcuerdos(int userId);
        Task<PagedResultDto<AcuerdoBusquedaItemDto>> GetAcuerdos(AcuerdoBusquedaFiltroRequest filtro, int userId);
        Task<List<AcuerdoPendienteAnteriorDto>> GetAcuerdosPendientesAnteriores(int reunionId);
        Task ReprogramarAcuerdo(int reunionAcuerdoId, AcuerdoReprogramarRequest request, int userId);
        Task MarcarAcuerdoCumplido(int reunionAcuerdoId, AcuerdoMarcarCumplidoRequest request, int userId);

        // ── Recurrencia ────────────────────────────────────────────────────
        Task<TemaRecurrenciaDto> GetRecurrenciaTema(int reunionTemaId);
        Task GuardarRecurrenciaTema(int reunionTemaId, TemaRecurrenciaSaveRequest request, int userId);
        /// <summary>Job periódico (disparado por cron externo): genera las siguientes ocurrencias
        /// de cada convocatoria recurrente cuya fecha teórica ya entró en su ventana de
        /// anticipación.</summary>
        Task<ProcesarGeneracionRecurrenteResultDto> ProcesarGeneracionRecurrente();

        /// <summary>
        /// Job de recordatorio (disparado por cron externo): revisa las reuniones con agenda
        /// dinámica cuya hora de aviso ya llegó y envía correo + notificación in-app con el link
        /// directo para cargar los temas.
        /// </summary>
        Task<object> ProcesarRecordatoriosAgenda();
        Task Update(int reunionId, ReunionUpdateRequest request, int userId);
        Task Reprogramar(int reunionId, ReunionReprogramarRequest request, int userId);
        Task CambiarEstado(int reunionId, ReunionCambiarEstadoRequest request, int userId);
        Task Eliminar(int reunionId, int userId);

        Task<int> CrearAcuerdo(int reunionId, ReunionAcuerdoRequest request, int userId);
        Task ActualizarAcuerdo(int reunionAcuerdoId, ReunionAcuerdoRequest request, int userId);
        Task EliminarAcuerdo(int reunionAcuerdoId, int userId);

        Task<List<ReunionArchivoDto>> SubirArchivos(int reunionId, IFormFileCollection files, int userId);
        Task EliminarArchivo(int reunionArchivoId, int userId);

        // ── Aceptación de acuerdos (link personal enviado por correo) ────────
        Task<AcuerdoResponsableInfoDto> GetAcuerdoResponsableInfo(int reunionAcuerdoResponsableId, int userId);
        Task ResponderAcuerdo(int reunionAcuerdoResponsableId, int userId, AcuerdoResponsableDecisionRequest request);

        // ── Carpeta de SharePoint para adjuntos (singleton) ──────────────────
        /// <summary>Devuelve la carpeta única configurada (o null si aún no se configuró).</summary>
        Task<ReunionFolderDto?> GetFolder();
        /// <summary>Valida el link, lo resuelve vía Graph y guarda/actualiza la carpeta única.</summary>
        Task<ReunionFolderDto> SaveFolder(ReunionFolderSaveDto dto, int userId);
    }
}
