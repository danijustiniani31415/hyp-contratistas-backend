using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Interfaces
{
    public interface IActasReunionRepository
    {
        Task<ReunionPaginaInicialDto> GetPaginaInicial(ReunionFiltroRequest filtro, int userId);
        Task<PagedResultDto<ReunionListItemDto>> GetReuniones(ReunionFiltroRequest filtro, int userId);
        Task<ReunionDetalleDto> GetDetalle(int reunionId, int userId);
        Task<int> Create(ReunionCreateRequest request, int userId);
        /// <summary>
        /// Trabajadores que calzan con un área/gerencia (con descendencia), una lista de puestos,
        /// y/o el staff vinculado actualmente a un proyecto (worker_vinculaciones vigente),
        /// para convocatoria masiva.
        /// </summary>
        Task<List<TrabajadorAbrilDto>> BuscarTrabajadoresPorFiltro(int? areaScopeId, List<int>? puestoIds, int? projectId);
        Task<List<CatalogoDto>> GetPuestos();
        Task<List<CatalogoDto>> GetPuestosPorArea(int? areaScopeId);
        Task<CatalogoDto> AgregarTema(string descripcion, int userId);
        Task<List<ReunionTemaOpcionDto>> GetTemasCatalogo();
        Task<TemaConvocatoriaDto> GetConvocatoriaTema(int reunionTemaId);
        Task GuardarConvocatoriaTema(int reunionTemaId, TemaConvocatoriaSaveRequest request, int userId);
        /// <summary>Elimina un tema del catálogo (borrado real, no soft-delete). Las reuniones que ya lo usaban
        /// conservan su tema (texto propio) y solo pierden el vínculo al catálogo. Devuelve cuántas se desvincularon.</summary>
        Task<int> EliminarTema(int reunionTemaId);

        /// <summary>Datos de la reunión + emails de sus participantes, para el correo de convocatoria.
        /// soloWorkerIds filtra a solo esos workers; null = todos.</summary>
        Task<ReunionConvocatoriaInfoDto?> GetInfoParaConvocatoria(int reunionId, List<int>? soloWorkerIds = null);

        // ── Agenda de reunión ──────────────────────────────────────────────
        /// <summary>Agenda de una reunión concreta (fija o temas cargados por participante), vista por el usuario autenticado.</summary>
        Task<ReunionAgendaDto> GetAgenda(int reunionId, int userId);
        /// <summary>Reemplaza los temas a tratar del worker del usuario autenticado para esta reunión.</summary>
        Task GuardarMisTemas(int reunionId, int userId, List<string> temas);
        Task<ReunionAgendaItemDto> AgregarTemaPuntual(int reunionId, int userId, string descripcion);
        Task EliminarTemaPuntual(int reunionId, int reunionAgendaItemId, int userId);
        Task<List<MisAcuerdoDto>> GetMisAcuerdos(int userId);
        /// <summary>Todos los acuerdos de reuniones que el usuario organizó o a las que fue
        /// convocado (mismo alcance que GetReuniones), sin filtrar por si es responsable.</summary>
        Task<PagedResultDto<AcuerdoBusquedaItemDto>> GetAcuerdos(AcuerdoBusquedaFiltroRequest filtro, int userId);
        Task<List<AcuerdoPendienteAnteriorDto>> GetAcuerdosPendientesAnteriores(int reunionId);
        Task ReprogramarAcuerdo(int reunionAcuerdoId, AcuerdoReprogramarRequest request, int userId);
        Task MarcarAcuerdoCumplido(int reunionAcuerdoId, AcuerdoMarcarCumplidoRequest request, int userId);

        // ── Recurrencia ────────────────────────────────────────────────────
        Task<TemaRecurrenciaDto> GetRecurrenciaTema(int reunionTemaId);
        Task GuardarRecurrenciaTema(int reunionTemaId, TemaRecurrenciaSaveRequest request, int userId);
        Task<List<ReunionTema>> GetTemasRecurrentesActivos();
        Task<List<(int? AreaScopeId, int? ProjectId, List<int> PuestoIds, List<int> WorkerIdsExcluidos)>> GetReglasTemaParaGeneracion(int reunionTemaId);
        Task AvanzarRecurrenciaTema(int reunionTemaId, DateOnly nuevaFechaGenerada, int nuevaReunionId);

        /// <summary>Reuniones PROGRAMADA con agenda dinámica cuyo recordatorio aún no se envió.</summary>
        Task<List<ReunionRecordatorioCandidatoDto>> GetCandidatosRecordatorioAgenda();
        /// <summary>Registra que ya se envió el recordatorio de una reunión (idempotencia del job).</summary>
        Task RegistrarRecordatorioEnviado(int reunionId);
        /// <summary>True si la fecha es un feriado configurado (fijo o recurrente anual).</summary>
        Task<bool> EsFeriado(DateOnly fecha);
        /// <summary>Guarda los cambios y devuelve los WorkerId de participantes recién agregados (para notificarles).</summary>
        Task<List<int>> Update(int reunionId, ReunionUpdateRequest request, int userId);
        Task Reprogramar(int reunionId, ReunionReprogramarRequest request, int userId);
        Task CambiarEstado(int reunionId, string estado, int userId);
        Task Eliminar(int reunionId, int userId);

        Task<int> CrearAcuerdo(int reunionId, ReunionAcuerdoRequest request, int userId);
        Task ActualizarAcuerdo(int reunionAcuerdoId, ReunionAcuerdoRequest request, int userId);
        Task EliminarAcuerdo(int reunionAcuerdoId, int userId);

        Task<List<ReunionArchivoDto>> AgregarArchivos(int reunionId, List<(string Url, string? OriginalFileName)> archivos, int userId);
        Task EliminarArchivo(int reunionArchivoId, int userId);

        // ── Aceptación de acuerdos + envío del acta al marcar Realizada ──────
        /// <summary>Info del acuerdo/responsable para la página de aceptar/rechazar; valida que
        /// userId corresponda a ese responsable (por persona, no worker_id exacto).</summary>
        Task<AcuerdoResponsableInfoDto> GetAcuerdoResponsableInfo(int reunionAcuerdoResponsableId, int userId);
        /// <summary>Registra la decisión (aceptar/rechazar) del responsable autenticado.</summary>
        Task ResponderAcuerdo(int reunionAcuerdoResponsableId, int userId, AcuerdoResponsableDecisionRequest request);
        /// <summary>Destinatarios del correo del acta final: asistentes + responsables de acuerdos
        /// (con sus acuerdos pendientes de aceptación, si los tienen), deduplicados por worker.</summary>
        Task<List<ActaEnvioDestinatarioDto>> GetDestinatariosActaRealizada(int reunionId);

        // ── Carpeta de SharePoint para adjuntos (singleton) ──────────────────
        /// <summary>Devuelve la carpeta única vigente (state = true) o null si aún no se configuró.</summary>
        Task<ReunionFolderDto?> GetFolderSingleton();
        /// <summary>Crea o actualiza (upsert) la carpeta única con la ubicación resuelta.</summary>
        Task UpsertFolder(string linkUrl, string driveId, string folderId, string? folderName, string? webUrl, int userId);
        /// <summary>Destino de subida vigente (driveId + folderId) o null si no hay carpeta configurada.</summary>
        Task<(string DriveId, string FolderId)?> GetFolderDestination();
        /// <summary>Proyecto y número de la reunión, para nombrar su subcarpeta en SharePoint.</summary>
        Task<(string ProjectDescription, int Numero)> GetDatosCarpetaReunion(int reunionId);
    }
}
