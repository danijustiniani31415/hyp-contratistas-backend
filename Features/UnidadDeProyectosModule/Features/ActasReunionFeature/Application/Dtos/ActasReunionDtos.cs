namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Dtos
{
    // ── Genéricos ────────────────────────────────────────────────────────────
    public class CatalogoDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
    }

    public class ProyectoFiltroDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
    }

    /// <summary>
    /// Tema del desplegable "Tema de la reunión", con el área/gerencia de su convocatoria recurrente
    /// (si tiene) para poder ocultarlo cuando no aplica al ámbito elegido — ej. "Reunión de Jefaturas
    /// de Proyectos" (AreaScopeId = Gerencia de Proyectos) no debe aparecer al agendar una reunión de
    /// un proyecto puntual. AreaScopeId null = sin área asociada, aplica a cualquier ámbito.
    /// </summary>
    public class ReunionTemaOpcionDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
        public int? AreaScopeId { get; set; }
    }

    /// <summary>Trabajador de Abril (workers con email_corporativo @abril.pe) para los desplegables.</summary>
    public class TrabajadorAbrilDto
    {
        public int WorkerId { get; set; }
        public string FullName { get; set; } = null!;
        /// <summary>Nombre del puesto del trabajador (catálogo <c>puesto</c>).</summary>
        public string? Cargo { get; set; }
    }

    public class PagedResultDto<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public List<T> Data { get; set; } = new();
    }

    // ── Listado ──────────────────────────────────────────────────────────────
    public class ReunionFiltroRequest
    {
        public int? ProjectId { get; set; }
        /// <summary>Nodo del árbol area_scope; incluye descendientes (ver AreaScopeTree.ResolveDescendantsAsync).</summary>
        public int? AreaScopeId { get; set; }
        public int? ReunionEstadoId { get; set; }
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ReunionListItemDto
    {
        public int ReunionId { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectDescription { get; set; }
        public int? AreaScopeId { get; set; }
        public string? AreaScopeDescripcion { get; set; }
        public int Numero { get; set; }
        public string Tema { get; set; } = null!;
        public string? Lugar { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public int ReunionEstadoId { get; set; }
        public string ReunionEstado { get; set; } = null!;
        public int TotalAcuerdos { get; set; }
        public int AcuerdosCumplidos { get; set; }
        public int VecesReprogramada { get; set; }
        public int TotalArchivos { get; set; }
    }

    public class ReunionPaginaInicialDto
    {
        public List<ProyectoFiltroDto> Proyectos { get; set; } = new();
        public List<CatalogoDto> ReunionEstados { get; set; } = new();
        public List<TrabajadorAbrilDto> Trabajadores { get; set; } = new();
        /// <summary>Temas predefinidos para el desplegable de "Tema de la reunión" al agendar.</summary>
        public List<ReunionTemaOpcionDto> Temas { get; set; } = new();
        public PagedResultDto<ReunionListItemDto> Reuniones { get; set; } = new();
    }

    // ── Detalle ──────────────────────────────────────────────────────────────
    public class ReunionDetalleDto
    {
        public int ReunionId { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectDescription { get; set; }
        public int? AreaScopeId { get; set; }
        public string? AreaScopeDescripcion { get; set; }
        public int Numero { get; set; }
        public string Tema { get; set; } = null!;
        public string? ConvocadoPor { get; set; }
        public string? Lugar { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public int ReunionEstadoId { get; set; }
        public string ReunionEstado { get; set; } = null!;
        public string? Observaciones { get; set; }

        public int CreatedUserId { get; set; }
        /// <summary>true si el usuario autenticado es el creador del acta o uno de sus coautores;
        /// controla si el frontend muestra las acciones de edición.</summary>
        public bool PuedeEditar { get; set; }

        public int? ReunionAnteriorId { get; set; }
        public int? ReunionAnteriorNumero { get; set; }
        public string? ReunionAnteriorTema { get; set; }
        public int? ReunionSiguienteId { get; set; }
        public int? ReunionSiguienteNumero { get; set; }
        public string? ReunionSiguienteTema { get; set; }

        public List<ReunionParticipanteDto> Participantes { get; set; } = new();
        public List<ReunionAcuerdoDto> Acuerdos { get; set; } = new();
        public List<ReunionArchivoDto> Archivos { get; set; } = new();
        public List<ReunionReprogramacionDto> Reprogramaciones { get; set; } = new();
        public List<CatalogoDto> AcuerdoEstados { get; set; } = new();
        public List<TrabajadorAbrilDto> Trabajadores { get; set; } = new();
        /// <summary>Temas predefinidos para el desplegable al "Agendar siguiente reunión".</summary>
        public List<ReunionTemaOpcionDto> Temas { get; set; } = new();
    }

    public class ReunionParticipanteDto
    {
        public int ReunionParticipanteId { get; set; }
        public int? WorkerId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Cargo { get; set; }
        public string? Iniciales { get; set; }
        public bool Asistio { get; set; }
        public int Orden { get; set; }
        /// <summary>true si este participante puede editar el acta igual que su creador.</summary>
        public bool EsCoautor { get; set; }
    }

    /// <summary>Responsable de un acuerdo, con su estado de aceptación individual.</summary>
    public class ReunionAcuerdoResponsableDto
    {
        public int ReunionAcuerdoResponsableId { get; set; }
        public int WorkerId { get; set; }
        public string WorkerNombre { get; set; } = null!;
        /// <summary>PENDIENTE | ACEPTADO | RECHAZADO.</summary>
        public string EstadoAceptacion { get; set; } = null!;
        public string? MotivoRechazo { get; set; }
        /// <summary>true cuando este responsable es el encargado principal de que el acuerdo se cumpla.</summary>
        public bool EsPrincipal { get; set; }
    }

    public class ReunionAcuerdoDto
    {
        public int ReunionAcuerdoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public string? Acciones { get; set; }
        public DateOnly? FechaProgramada { get; set; }
        public DateOnly? FechaReprogramacion { get; set; }
        public DateOnly? FechaCumplimiento { get; set; }
        public int ReunionAcuerdoEstadoId { get; set; }
        public string ReunionAcuerdoEstado { get; set; } = null!;
        public int Orden { get; set; }
        /// <summary>NORMAL | MEDIO | CRITICO.</summary>
        public string Criticidad { get; set; } = null!;
        public bool RequiereAceptacion { get; set; }
        public bool RequiereEvidencia { get; set; }
        public string? EvidenciaUrl { get; set; }
        /// <summary>Solo registra información: no requiere seguimiento ni acción de ningún responsable.</summary>
        public bool EsInformativo { get; set; }
        /// <summary>Cómo se levantó el acuerdo (obligatorio si no tiene evidencia).</summary>
        public string? ComentarioCumplimiento { get; set; }
        public int VecesReprogramado { get; set; }
        public string? UltimoMotivoReprogramacion { get; set; }
        public List<ReunionAcuerdoResponsableDto> Responsables { get; set; } = new();
    }

    /// <summary>Al marcar un acuerdo como cumplido: comentario obligatorio si no tiene evidencia
    /// adjunta, opcional si sí la tiene.</summary>
    public class AcuerdoMarcarCumplidoRequest
    {
        public string? Comentario { get; set; }
        /// <summary>URL del archivo ya subido (vía POST {reunionId}/archivos) a usar como evidencia
        /// de este acuerdo. Null si no se adjunta nada en esta acción.</summary>
        public string? EvidenciaUrl { get; set; }
    }

    // ── Revisión de acuerdos pendientes de ediciones anteriores ──────────────
    /// <summary>Un acuerdo aún no cumplido (ni anulado) de una edición anterior de la misma
    /// convocatoria recurrente, para revisarlo al abrir la siguiente reunión de la cadena.</summary>
    public class AcuerdoPendienteAnteriorDto
    {
        public int ReunionAcuerdoId { get; set; }
        public int ReunionId { get; set; }
        public int ReunionNumero { get; set; }
        public string ReunionTema { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string? Acciones { get; set; }
        /// <summary>NORMAL | MEDIO | CRITICO.</summary>
        public string Criticidad { get; set; } = null!;
        public DateOnly? FechaProgramada { get; set; }
        public int ReunionAcuerdoEstadoId { get; set; }
        public string ReunionAcuerdoEstado { get; set; } = null!;
        public bool RequiereEvidencia { get; set; }
        public string? EvidenciaUrl { get; set; }
        public int VecesReprogramado { get; set; }
        public string? UltimoMotivoReprogramacion { get; set; }
        public List<ReunionAcuerdoResponsableDto> Responsables { get; set; } = new();
    }

    public class AcuerdoReprogramarRequest
    {
        public DateOnly NuevaFechaProgramada { get; set; }
        public string Motivo { get; set; } = null!;
    }

    // ── Dashboard "Mis acuerdos" ─────────────────────────────────────────────
    /// <summary>Un acuerdo del que el usuario autenticado es responsable, en cualquier reunión,
    /// para el dashboard personal de seguimiento (agrupado por criticidad/vencimiento en el frontend).</summary>
    public class MisAcuerdoDto
    {
        public int ReunionAcuerdoId { get; set; }
        public int ReunionAcuerdoResponsableId { get; set; }
        public int ReunionId { get; set; }
        public int ReunionNumero { get; set; }
        public string ReunionTema { get; set; } = null!;
        /// <summary>Proyecto, área o "Organización" (ámbito de la reunión de origen).</summary>
        public string Ambito { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string? Acciones { get; set; }
        /// <summary>NORMAL | MEDIO | CRITICO.</summary>
        public string Criticidad { get; set; } = null!;
        public DateOnly? FechaProgramada { get; set; }
        public int ReunionAcuerdoEstadoId { get; set; }
        public string ReunionAcuerdoEstado { get; set; } = null!;
        public DateOnly? FechaCumplimiento { get; set; }
        /// <summary>true si este responsable es el principal (encargado de que se cumpla).</summary>
        public bool EsPrincipal { get; set; }
        /// <summary>Nombres de los demás responsables del mismo acuerdo, si hay más de uno.</summary>
        public List<string> OtrosResponsables { get; set; } = new();
        public bool RequiereEvidencia { get; set; }
        public string? EvidenciaUrl { get; set; }
        /// <summary>Cómo se levantó el acuerdo (obligatorio al marcar CUMPLIDO).</summary>
        public string? ComentarioCumplimiento { get; set; }
        public bool RequiereAceptacion { get; set; }
        /// <summary>PENDIENTE | ACEPTADO | RECHAZADO.</summary>
        public string EstadoAceptacion { get; set; } = null!;
    }

    // ── Vista global "Acuerdos" (todas las reuniones donde el usuario participó) ─────────────
    public class AcuerdoBusquedaFiltroRequest
    {
        /// <summary>PENDIENTE | EN PROCESO | CUMPLIDO | ANULADO | INFORMATIVO. Null = todos.
        /// INFORMATIVO filtra por EsInformativo=true (no es un valor real del catálogo de estados).</summary>
        public string? Estado { get; set; }
        public int? ResponsableWorkerId { get; set; }
        /// <summary>Filtra por FechaProgramada.</summary>
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        /// <summary>Busca en la descripción del acuerdo y en el tema de la reunión.</summary>
        public string? Texto { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class AcuerdoBusquedaItemDto
    {
        public int ReunionAcuerdoId { get; set; }
        public int ReunionId { get; set; }
        public int ReunionNumero { get; set; }
        public string ReunionTema { get; set; } = null!;
        /// <summary>Proyecto, área o "Organización" (ámbito de la reunión de origen).</summary>
        public string Ambito { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        /// <summary>NORMAL | MEDIO | CRITICO.</summary>
        public string Criticidad { get; set; } = null!;
        public DateOnly? FechaProgramada { get; set; }
        public DateOnly? FechaCumplimiento { get; set; }
        public int ReunionAcuerdoEstadoId { get; set; }
        public string ReunionAcuerdoEstado { get; set; } = null!;
        public bool EsInformativo { get; set; }
        public bool RequiereEvidencia { get; set; }
        public string? EvidenciaUrl { get; set; }
        /// <summary>Cómo se levantó el acuerdo (obligatorio al marcar CUMPLIDO).</summary>
        public string? ComentarioCumplimiento { get; set; }
        public List<string> Responsables { get; set; } = new();
    }

    public class ReunionArchivoDto
    {
        public int ReunionArchivoId { get; set; }
        public string ArchivoUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }

    public class ReunionReprogramacionDto
    {
        public int ReunionReprogramacionId { get; set; }
        public DateOnly FechaAnterior { get; set; }
        public TimeOnly? HoraInicioAnterior { get; set; }
        public TimeOnly? HoraFinAnterior { get; set; }
        public DateOnly FechaNueva { get; set; }
        public TimeOnly? HoraInicioNueva { get; set; }
        public TimeOnly? HoraFinNueva { get; set; }
        public string? Motivo { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string? CreatedUserName { get; set; }
    }

    // ── Requests ─────────────────────────────────────────────────────────────
    public class ReunionParticipanteInput
    {
        /// <summary>Null cuando es un participante nuevo.</summary>
        public int? ReunionParticipanteId { get; set; }
        /// <summary>
        /// workers.id cuando el participante se eligió del desplegable de trabajadores de Abril.
        /// Si el worker no tiene puesto, el Cargo ingresado a mano se da de alta en el catálogo `puesto`.
        /// </summary>
        public int? WorkerId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Cargo { get; set; }
        public string? Iniciales { get; set; }
        public bool Asistio { get; set; }
        /// <summary>Solo tiene efecto si WorkerId viene informado (un invitado externo no puede ser coautor).</summary>
        public bool EsCoautor { get; set; }
    }

    public class ReunionCreateRequest
    {
        /// <summary>Reunión de proyecto. Exactamente uno de ProjectId/AreaScopeId debe venir informado, o ninguno (reunión de toda la organización).</summary>
        public int? ProjectId { get; set; }
        /// <summary>Reunión de un nodo del árbol area_scope (gerencia/área/subárea).</summary>
        public int? AreaScopeId { get; set; }
        public string Tema { get; set; } = null!;
        /// <summary>Tema del catálogo elegido (null si es personalizado), para heredar su configuración de agenda/recordatorio.</summary>
        public int? ReunionTemaId { get; set; }
        public string? ConvocadoPor { get; set; }
        public string? Lugar { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public int? ReunionAnteriorId { get; set; }
        /// <summary>
        /// Agenda fija ad-hoc, obligatoria cuando la reunión es puntual: tema personalizado
        /// (ReunionTemaId null) y no se guarda como recurrente. Un tema del catálogo ya trae su
        /// propia configuración de agenda (fija o dinámica) y no necesita esto.
        /// </summary>
        public string? AgendaTexto { get; set; }
        public List<ReunionParticipanteInput> Participantes { get; set; } = new();
    }

    public class ReunionUpdateRequest
    {
        public string Tema { get; set; } = null!;
        public string? ConvocadoPor { get; set; }
        public string? Lugar { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public string? Observaciones { get; set; }
        /// <summary>Lista completa de participantes: los existentes que no vengan se eliminan (soft delete).</summary>
        public List<ReunionParticipanteInput> Participantes { get; set; } = new();
    }

    public class ReunionReprogramarRequest
    {
        public DateOnly Fecha { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public string? Motivo { get; set; }
    }

    // ── Carpeta de SharePoint para adjuntos ──────────────────────────────────

    /// <summary>Carpeta única (singleton) configurada para guardar los adjuntos de las actas.</summary>
    public class ReunionFolderDto
    {
        public int ReunionFolderId { get; set; }
        public string LinkUrl { get; set; } = null!;
        public string DriveId { get; set; } = null!;
        public string FolderId { get; set; } = null!;
        public string? FolderName { get; set; }
        public string? WebUrl { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
    }

    /// <summary>Datos para configurar/actualizar la carpeta única: solo el link pegado por el usuario.</summary>
    public class ReunionFolderSaveDto
    {
        public string LinkUrl { get; set; } = null!;
    }

    public class TemaCreateRequest
    {
        public string Descripcion { get; set; } = null!;
    }

    /// <summary>
    /// Una regla de la convocatoria de un tema: a quién convocar (área/gerencia y/o proyecto +
    /// puestos). Un tema puede tener varias reglas independientes — ej. "Reunión de Jefaturas de
    /// Proyectos" convoca a los jefes de su gerencia, PERO además al Gerente Inmobiliario de otra.
    /// </summary>
    public class TemaConvocatoriaReglaDto
    {
        public int? AreaScopeId { get; set; }
        public string? AreaScopeDescripcion { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectDescription { get; set; }
        public List<int> PuestoIds { get; set; } = new();
        /// <summary>Workers del staff del proyecto excluidos a mano de esta regla (solo aplica
        /// cuando ProjectId tiene valor).</summary>
        public List<int> WorkerIdsExcluidos { get; set; } = new();
    }

    public class TemaConvocatoriaReglaInput
    {
        public int? AreaScopeId { get; set; }
        public int? ProjectId { get; set; }
        public List<int> PuestoIds { get; set; } = new();
        public List<int> WorkerIdsExcluidos { get; set; } = new();
    }

    /// <summary>Convocatoria recurrente asociada a un tema (ej. "Reunión de Jefaturas de Proyectos").</summary>
    public class TemaConvocatoriaDto
    {
        public List<TemaConvocatoriaReglaDto> Reglas { get; set; } = new();
        public bool AgendaFija { get; set; }
        public string? AgendaTexto { get; set; }
        public decimal? RecordatorioHorasAntes { get; set; }
    }

    public class TemaConvocatoriaSaveRequest
    {
        public List<TemaConvocatoriaReglaInput> Reglas { get; set; } = new();
        public bool AgendaFija { get; set; }
        public string? AgendaTexto { get; set; }
        public decimal? RecordatorioHorasAntes { get; set; }
    }

    // ── Recurrencia (generación automática de reuniones) ─────────────────────
    public class TemaRecurrenciaDto
    {
        public bool EsRecurrente { get; set; }
        public bool RecurrenciaActiva { get; set; }
        /// <summary>Área/gerencia (nodo area_scope) a la que pertenecerán las reuniones generadas.
        /// Obligatorio para activar la recurrencia.</summary>
        public int? AreaScopeId { get; set; }
        public string? AreaScopeDescripcion { get; set; }
        public int? IntervaloDias { get; set; }
        public DateOnly? FechaAncla { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public string? Lugar { get; set; }
        public int DiasAnticipacion { get; set; }
        public DateOnly? UltimaFechaGenerada { get; set; }
        /// <summary>Próxima fecha teórica en la que se generaría la siguiente ocurrencia
        /// (informativo, calculado; no es un campo en base de datos).</summary>
        public DateOnly? ProximaFechaEstimada { get; set; }
    }

    public class TemaRecurrenciaSaveRequest
    {
        public bool EsRecurrente { get; set; }
        public bool RecurrenciaActiva { get; set; }
        public int? AreaScopeId { get; set; }
        public int? IntervaloDias { get; set; }
        public DateOnly? FechaAncla { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public string? Lugar { get; set; }
        public int DiasAnticipacion { get; set; } = 5;
    }

    public class ProcesarGeneracionRecurrenteResultDto
    {
        public int ReunionesGeneradas { get; set; }
        public List<string> Detalle { get; set; } = new();
    }

    // ── Agenda de reunión ────────────────────────────────────────────────────
    public class ReunionAgendaItemDto
    {
        public int ReunionAgendaItemId { get; set; }
        public int WorkerId { get; set; }
        public string WorkerNombre { get; set; } = null!;
        /// <summary>Subárea (nodo area_scope) del trabajador, si tiene una asignada.</summary>
        public string? SubareaDescripcion { get; set; }
        public string Descripcion { get; set; } = null!;
        public int Orden { get; set; }
    }

    /// <summary>Agenda de una reunión concreta: fija (texto único) o dinámica (temas por participante).</summary>
    public class ReunionAgendaDto
    {
        public bool RequiereAgenda { get; set; }
        public bool AgendaFija { get; set; }
        /// <summary>Texto único cuando AgendaFija es true.</summary>
        public string? AgendaTexto { get; set; }
        /// <summary>Temas cargados por cada participante cuando AgendaFija es false, o los "temas
        /// puntuales" agregados a mano para esta ocurrencia cuando AgendaFija es true.</summary>
        public List<ReunionAgendaItemDto> Items { get; set; } = new();
        /// <summary>Participantes convocados (con workerId) que aún no cargaron ningún tema.</summary>
        public List<string> ParticipantesPendientes { get; set; } = new();
        /// <summary>WorkerId del usuario autenticado que consulta, si es participante de esta reunión (para saber "mis temas").</summary>
        public int? WorkerIdActual { get; set; }
    }

    public class ReunionAgendaItemInput
    {
        public string Descripcion { get; set; } = null!;
    }

    /// <summary>Reemplaza por completo los temas a tratar del worker autenticado para una reunión.</summary>
    public class GuardarMisTemasRequest
    {
        public List<ReunionAgendaItemInput> Temas { get; set; } = new();
    }

    // ── Recordatorio de agenda (job) ─────────────────────────────────────────
    /// <summary>Reunión PROGRAMADA con agenda dinámica pendiente de recordatorio.</summary>
    public class ReunionRecordatorioCandidatoDto
    {
        public int ReunionId { get; set; }
        public int Numero { get; set; }
        public string Tema { get; set; } = null!;
        public string AmbitoDescripcion { get; set; } = null!;
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public decimal RecordatorioHorasAntes { get; set; }
        public List<ReunionRecordatorioDestinatarioDto> Destinatarios { get; set; } = new();
    }

    public class ReunionRecordatorioDestinatarioDto
    {
        public int UserId { get; set; }
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    // ── Convocatoria inmediata (al agendar) ──────────────────────────────────
    /// <summary>Datos de la reunión recién creada para armar el correo de convocatoria.</summary>
    public class ReunionConvocatoriaInfoDto
    {
        public int ReunionId { get; set; }
        public int Numero { get; set; }
        public string Tema { get; set; } = null!;
        public string AmbitoDescripcion { get; set; } = null!;
        public DateOnly Fecha { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public string? Lugar { get; set; }
        public List<ReunionRecordatorioDestinatarioDto> Destinatarios { get; set; } = new();
    }

    public class ReunionCambiarEstadoRequest
    {
        /// <summary>Descripción del estado destino: PROGRAMADA, REALIZADA o CANCELADA.</summary>
        public string Estado { get; set; } = null!;
    }

    public class ReunionAcuerdoRequest
    {
        public string Descripcion { get; set; } = null!;
        public string? Acciones { get; set; }
        public DateOnly? FechaProgramada { get; set; }
        public DateOnly? FechaReprogramacion { get; set; }
        public DateOnly? FechaCumplimiento { get; set; }
        /// <summary>Null al crear: se asigna PENDIENTE.</summary>
        public int? ReunionAcuerdoEstadoId { get; set; }
        /// <summary>NORMAL | MEDIO | CRITICO.</summary>
        public string Criticidad { get; set; } = "NORMAL";
        /// <summary>Si true, cada responsable debe aceptar el acuerdo antes de quedar activo.</summary>
        public bool RequiereAceptacion { get; set; }
        /// <summary>Si true, no se puede marcar CUMPLIDO sin adjuntar evidencia.</summary>
        public bool RequiereEvidencia { get; set; }
        public string? EvidenciaUrl { get; set; }
        /// <summary>Si true, solo registra información: no requiere seguimiento ni acción de ningún
        /// responsable (el frontend desactiva RequiereAceptacion/RequiereEvidencia junto con esto).</summary>
        public bool EsInformativo { get; set; }
        /// <summary>Ids de workers (cualquier trabajador de la organización, haya asistido o no) responsables del acuerdo.</summary>
        public List<int> ResponsableWorkerIds { get; set; } = new();
        /// <summary>WorkerId del responsable principal (encargado de que el acuerdo se cumpla) cuando hay más de uno; null si solo hay uno o ninguno.</summary>
        public int? ResponsablePrincipalWorkerId { get; set; }
    }

    // ── Aceptación de acuerdos (link personal enviado por correo) ────────────

    /// <summary>Info mínima para la página de aceptar/rechazar un acuerdo, accedida desde el link del correo.</summary>
    public class AcuerdoResponsableInfoDto
    {
        public int ReunionAcuerdoResponsableId { get; set; }
        public int ReunionId { get; set; }
        public int ReunionNumero { get; set; }
        public string ReunionTema { get; set; } = null!;
        public string AcuerdoDescripcion { get; set; } = null!;
        public string? AcuerdoAcciones { get; set; }
        public DateOnly? FechaProgramada { get; set; }
        /// <summary>PENDIENTE | ACEPTADO | RECHAZADO.</summary>
        public string EstadoAceptacion { get; set; } = null!;
        public string? MotivoRechazo { get; set; }
    }

    public class AcuerdoResponsableDecisionRequest
    {
        public bool Aceptado { get; set; }
        /// <summary>Obligatorio cuando Aceptado es false.</summary>
        public string? MotivoRechazo { get; set; }
    }

    // ── Envío del acta al marcar la reunión como realizada ───────────────────

    /// <summary>Un destinatario del correo con el acta final: asistió y/o es responsable de algún
    /// acuerdo. AcuerdosPendientes solo trae los que requieren su aceptación y siguen PENDIENTE.</summary>
    public class ActaEnvioDestinatarioDto
    {
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool Asistio { get; set; }
        public List<ActaEnvioAcuerdoPendienteDto> AcuerdosPendientes { get; set; } = new();
    }

    public class ActaEnvioAcuerdoPendienteDto
    {
        public int ReunionAcuerdoResponsableId { get; set; }
        public string Descripcion { get; set; } = null!;
    }
}
