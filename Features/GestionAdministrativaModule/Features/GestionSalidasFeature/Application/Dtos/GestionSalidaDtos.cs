namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Dtos
{
    public class GestionSalidaListItemDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string Trabajador { get; set; } = string.Empty;
        /// <summary>
        /// Área del trabajador: el nombre del nodo al que apunta <c>workers.area_scope_id</c>, es
        /// decir el área más baja a la que pertenece (una persona puede colgar de Gerencia de
        /// Proyectos › Unidad de Proyectos › Unidad de Proyectos y aquí va solo la última). El
        /// detalle sí devuelve la ruta completa en <see cref="GestionSalidaDetalleDto.AreaRuta"/>.
        /// </summary>
        public string? Area { get; set; }
        /// <summary>
        /// Nombre completo del jefe/revisor del solicitante — el mismo que recibe el correo de
        /// aprobación al crear la solicitud (<c>IJefeRevisorResolver</c>). Cuando la resolución
        /// cae al fallback de GTH no hay persona: ahí va el nombre del área.
        /// </summary>
        public string? RevisorNombre { get; set; }
        public DateOnly FechaSalida { get; set; }
        /// <summary>Hora de salida del primer trayecto.</summary>
        public TimeOnly HoraSalida { get; set; }
        /// <summary>Hora de retorno del último trayecto.</summary>
        public TimeOnly? HoraRetorno { get; set; }
        /// <summary>Motivo del primer trayecto.</summary>
        public string Motivo { get; set; } = string.Empty;
        /// <summary>Origen del primer trayecto.</summary>
        public string? LugarOrigen { get; set; }
        /// <summary>Destino del último trayecto.</summary>
        public string? LugarDestino { get; set; }
        public int TrayectosCount { get; set; }
        public string EstadoAprobacion { get; set; } = string.Empty;
        public string EstadoRendicion { get; set; } = "No rendido";
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>True si todos los trayectos tienen al menos una captura — habilita la rendición.</summary>
        public bool PuedeRendirse { get; set; }
        /// <summary>Hora real de salida registrada por recepción. Dato extra, opcional.</summary>
        public TimeOnly? HoraSalidaReal { get; set; }
        /// <summary>Hora real de retorno registrada por recepción. Dato extra, opcional.</summary>
        public TimeOnly? HoraRetornoReal { get; set; }
        /// <summary>
        /// True cuando TODOS los trayectos tienen motivos con es_hora_estimada: las horas declaradas
        /// son estimadas y recepción no registra hora real de salida/retorno. Si al menos un trayecto
        /// tiene motivo de hora exacta (o motivo libre), se sigue registrando.
        /// </summary>
        public bool EsHoraEstimada { get; set; }
        /// <summary>
        /// True si el usuario logueado puede aprobar/rechazar ESTA salida. Es false cuando la salida
        /// es propia (worker del propio usuario) y el usuario no es Gerente — nadie aprueba sus
        /// propias salidas salvo los gerentes. Solo afecta Aprobar/Rechazar, no la rendición.
        /// </summary>
        public bool PuedeDecidir { get; set; } = true;

        /// <summary>
        /// True si la salida es del propio usuario logueado (su worker). Habilita el botón
        /// "Cancelar": un trabajador solo puede cancelar SUS propias solicitudes Pendientes.
        /// </summary>
        public bool EsPropia { get; set; }

        // -- Consolidado del S10 (solo salidas rendidas) -----------------
        /// <summary>webUrl del PDF Consolidado del S10 vigente, o null si aun no se adjunto.</summary>
        public string? ConsolidadoS10Url { get; set; }
        /// <summary>Nombre del archivo del consolidado vigente. Null si no hay.</summary>
        public string? ConsolidadoS10Filename { get; set; }
        /// <summary>"Rendicion" (cubre toda la planilla) | "Solicitud" (solo esta salida) | null si no hay.</summary>
        public string? ConsolidadoS10Ambito { get; set; }

        // -- Reembolso ---------------------------------------------------
        /// <summary>"Pendiente" | "Aprobado" | "Rechazado" | "Firmado" | "Pagado".</summary>
        public string EstadoReembolso { get; set; } = "Pendiente";

        /// <summary>
        /// True cuando el reembolso ya se puede decidir: la salida esta Rendida Y tiene adjunto el
        /// Consolidado del S10. Sin eso no hay que revisar.
        /// </summary>
        public bool ReembolsoRevisable { get; set; }

        /// <summary>Observacion del ultimo rechazo del reembolso. Es lo que el trabajador subsana.</summary>
        public string? ObservacionReembolso { get; set; }

        /// <summary>Nombre de quien aprobo/rechazo el reembolso. Null si nadie lo decidio aun.</summary>
        public string? ReembolsoDecididoPor { get; set; }
        public DateTimeOffset? ReembolsoDecididoAt { get; set; }

        /// <summary>Momento en que el trabajador aviso al revisor que ya adjunto el S10. Null si nunca aviso.</summary>
        public DateTimeOffset? RevisorNotificadoAt { get; set; }

        /// <summary>webUrl de la planilla de rendicion FIRMADA. Null mientras nadie la firme.</summary>
        public string? PlanillaFirmadaUrl { get; set; }
    }

    public class RegistrarHoraSalidaRealDto
    {
        /// <summary>"HH:mm" o null para limpiar.</summary>
        public TimeOnly? HoraSalidaReal { get; set; }
    }

    public class RegistrarHoraRetornoRealDto
    {
        /// <summary>"HH:mm" o null para limpiar.</summary>
        public TimeOnly? HoraRetornoReal { get; set; }
    }

    public class GestionSalidaFiltersDto
    {
        public int? WorkerId { get; set; }
        public int? LugarProyectoId { get; set; }
        public string? EstadoRendicion { get; set; }
        /// <summary>"Pendiente" | "Aprobado" | "Rechazado" | null para todos.</summary>
        public string? EstadoAprobacion { get; set; }

        /// <summary>
        /// "Pendiente" | "Aprobado" | "Rechazado" | "Firmado" | "Pagado" | null para todos.
        /// Para un tesorero se acota a Firmado/Pagado aunque pida otra cosa (ver <see cref="EsTesorero"/>).
        /// </summary>
        public string? EstadoReembolso { get; set; }

        /// <summary>
        /// True cuando el usuario entra como TESORERO: tiene el rol y ademas su puesto es de
        /// categoria Tesorero. Lo resuelve el servicio, no el controller (el rol sale del token
        /// pero la categoria sale de la base).
        ///
        /// En ese modo ve TODAS las areas, pero solo las salidas ya firmadas por la jefatura y las
        /// ya pagadas: es la bandeja de tesoreria, no la de aprobacion.
        /// </summary>
        public bool EsTesorero { get; set; }

        /// <summary>
        /// True si el token del usuario trae el rol TESORERO. Solo dice que tiene el rol: la
        /// segunda condicion (la categoria del puesto) la resuelve el servicio contra la base.
        /// </summary>
        public bool TieneRolTesorero { get; set; }

        /// <summary>
        /// True = solo las solicitudes cuya <c>fecha_salida</c> es la de HOY. El día se calcula en
        /// hora de Perú (UTC-5) y no en la del servidor, que corre en UTC. Es el filtro que la
        /// pantalla de Gestión de Salidas manda encendido por defecto.
        /// </summary>
        public bool SoloHoy { get; set; }

        /// <summary>UserId del usuario logueado (de claims). Necesario para el scoping de visibilidad.</summary>
        public int? CurrentUserId { get; set; }

        /// <summary>
        /// Visibilidad ya resuelta por el servicio (SalidaVisibilityResolver). Si true, el usuario
        /// ve TODAS las solicitudes sin restricción por área.
        /// </summary>
        public bool SeesAll { get; set; }

        /// <summary>
        /// True cuando el usuario autenticado tiene el rol USUARIO DE RECEPCIÓN (lo setea el
        /// controller desde los claims). El rol se sobrepone al alcance por área: fuerza
        /// <see cref="SeesAll"/> sin correr el resolver de visibilidad.
        /// </summary>
        public bool SeesAllOverride { get; set; }

        /// <summary>
        /// Nodos area_scope cuyos trabajadores puede ver el usuario. El usuario también ve
        /// siempre las solicitudes donde él es el aprobador resuelto (aprobador_worker_id).
        /// </summary>
        public List<int>? VisibleAreaScopeIds { get; set; }

        /// <summary>
        /// Filtro de área elegido por el usuario en la UI (desplegable en cascada): nodo
        /// seleccionado + sus descendientes, resueltos en el frontend. Null/vacío = sin filtro.
        /// Es independiente de <see cref="VisibleAreaScopeIds"/> (visibilidad obligatoria).
        /// </summary>
        public List<int>? FilterAreaScopeIds { get; set; }

        /// <summary>Página solicitada (1-based). Solo aplica a la vista paginada de la tabla.</summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Columna por la que ordenar la tabla. Null/desconocida = orden original
        /// (pendientes primero, luego más recientes). Valores: trabajador, fechaSalida,
        /// horaSalida, horaRetorno, motivo, lugarOrigen, lugarDestino, estadoAprobacion,
        /// estadoRendicion, createdAt.
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>Dirección del orden: "asc" o "desc" (por defecto "asc").</summary>
        public string? SortDir { get; set; }

        /// <summary>Límite inferior (inclusive) de fecha_salida. Lo usa la rendición del mes anterior.</summary>
        public DateOnly? FechaSalidaDesde { get; set; }
        /// <summary>Límite superior (inclusive) de fecha_salida. Lo usa la rendición del mes anterior.</summary>
        public DateOnly? FechaSalidaHasta { get; set; }
    }

    public class MarcarRendidasBulkDto
    {
        public List<int> Ids { get; set; } = new();
    }

    /// <summary>Cuerpo de las acciones en bloque sobre el reembolso (aprobar, firmar, pagar).</summary>
    public class ReembolsoBulkDto
    {
        public List<int> Ids { get; set; } = new();
    }

    /// <summary>
    /// Rechazo del reembolso en bloque. La observacion es obligatoria: es lo unico que el
    /// trabajador va a leer para saber que corregir.
    /// </summary>
    public class RechazarReembolsoBulkDto
    {
        public List<int> Ids { get; set; } = new();
        public string? Observacion { get; set; }
    }

    /// <summary>
    /// Una planilla de rendicion pendiente de firma, con las salidas de la seleccion que cuelgan
    /// de ella. El PDF se firma UNA vez por planilla aunque la seleccion traiga varias salidas
    /// suyas: el documento es uno solo.
    /// </summary>
    public class RendicionPorFirmarDto
    {
        public int RendicionId { get; set; }
        /// <summary>webUrl del PDF original de la planilla (el que se descarga para estampar).</summary>
        public string PdfUrl { get; set; } = string.Empty;
        public string PdfFilename { get; set; } = string.Empty;
        /// <summary>webUrl de la copia ya firmada, si otra firma anterior la genero.</summary>
        public string? PdfFirmadoUrl { get; set; }
        /// <summary>Salidas de la seleccion que cuelgan de esta planilla y estan listas para firmar.</summary>
        public List<int> SolicitudIds { get; set; } = new();
    }

    /// <summary>
    /// Lo que necesitan los correos del reembolso de UNA salida. Sale de una sola consulta para no
    /// volver a la base por cada correo.
    /// </summary>
    public class ReembolsoCorreoInfoDto
    {
        public int SolicitudId { get; set; }
        public int WorkerId { get; set; }
        public string Trabajador { get; set; } = string.Empty;
        /// <summary>
        /// Correlativo de la solicitud DENTRO del trabajador: el "#3" que él ve en su pantalla y en
        /// los otros correos del flujo, no el id de la tabla. Se calcula igual que en
        /// SolicitudSalidaService para que el mismo pedido no salga con dos numeros distintos.
        /// </summary>
        public int NumeroUsuario { get; set; }
        /// <summary>Correo del solicitante (app_user.email). Null si no tiene usuario.</summary>
        public string? SolicitanteEmail { get; set; }
        public string? Area { get; set; }
        public DateOnly FechaSalida { get; set; }
        /// <summary>Numero de planilla formateado ("TI: 000123"), o null si no tiene planilla.</summary>
        public string? NumeroPlanilla { get; set; }
        public int TrayectosCount { get; set; }
        public decimal MontoTotal { get; set; }
        public string EstadoReembolso { get; set; } = string.Empty;
        public string? ObservacionReembolso { get; set; }
        /// <summary>Nombre de quien decidio el reembolso (para mostrarlo en el correo).</summary>
        public string? DecididoPor { get; set; }
    }

    /// <summary>Resultado de una accion en bloque sobre el reembolso.</summary>
    public class ReembolsoBulkResultDto
    {
        /// <summary>Cuantas salidas cambiaron de estado.</summary>
        public int Procesadas { get; set; }
        /// <summary>Cuantas planillas distintas se firmaron (solo lo usa Firmar).</summary>
        public int PlanillasFirmadas { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GestionSalidaFilterDataDto
    {
        public List<TrabajadorOptionDto> Trabajadores { get; set; } = new();
        public List<LugarProyectoOptionDto> LugaresProyecto { get; set; } = new();
        /// <summary>Árbol area_scope (lista plana) para el filtro de área en cascada.</summary>
        public List<AreaNodeDto> AreaTree { get; set; } = new();

        /// <summary>
        /// True si el usuario entra en modo TESORERÍA: tiene el rol TESORERO Y su puesto es de
        /// categoría Tesorero. Lo decide el backend porque la mitad del criterio (la categoría)
        /// vive en la base: el frontend solo ve el rol del token y con eso pintaría la bandeja de
        /// tesorería a alguien que no lo es.
        ///
        /// En ese modo la pantalla solo muestra reembolsos firmados y pagados, esconde las acciones
        /// de aprobación/rendición y habilita "Marcar como pagadas".
        /// </summary>
        public bool EsTesorero { get; set; }
    }

    /// <summary>Nodo del árbol area_scope (lista plana; el frontend arma la jerarquía). </summary>
    public class AreaNodeDto
    {
        public int AreaScopeId { get; set; }
        public int AreaItemId { get; set; }
        public string AreaItemName { get; set; } = string.Empty;
        public int AreaTypeId { get; set; }
        public string AreaTypeName { get; set; } = string.Empty;
        public int? AreaScopeParentId { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class TrabajadorOptionDto
    {
        public int WorkerId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
    }

    public class LugarProyectoOptionDto
    {
        public int GaLugarId { get; set; }
        public string NombreDisplay { get; set; } = string.Empty;
    }

    public class AprobarRechazarDto { }

    public class GestionSalidaCapturaDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }

    /// <summary>Un documento adjunto (prueba) de un trayecto, para mostrar en el detalle.</summary>
    public class GestionSalidaAdjuntoDto
    {
        public string Url { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
    }

    public class GestionSalidaTrayectoDto
    {
        public int Id { get; set; }
        public int Orden { get; set; }
        public TimeOnly HoraSalida { get; set; }
        public TimeOnly? HoraRetorno { get; set; }
        public string Motivo { get; set; } = string.Empty;
        /// <summary>Detalle que escribió el trabajador cuando el motivo lo exige. Null si no aplica.</summary>
        public string? MotivoAdicional { get; set; }
        public string? LugarOrigen { get; set; }
        public string? LugarDestino { get; set; }
        /// <summary>Documentos adjuntos del trayecto (motivos con requiere_adjunto). Vacío si no tiene.</summary>
        public List<GestionSalidaAdjuntoDto> Adjuntos { get; set; } = new();
        public List<GestionSalidaCapturaDto> Capturas { get; set; } = new();
        /// <summary>Monto del catálogo ga_trayecto si aplica (worker TI + match origen/destino).</summary>
        public decimal? MontoCatalogo { get; set; }
        /// <summary>Monto efectivo: sum(capturas) si hay; sino MontoCatalogo si aplica; sino 0.</summary>
        public decimal MontoTotal { get; set; }
    }

    public class GestionSalidaDetalleDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string Trabajador { get; set; } = string.Empty;
        /// <summary>Área más baja del trabajador (el nodo de <c>workers.area_scope_id</c>).</summary>
        public string? Area { get; set; }
        /// <summary>
        /// Ruta completa del área en el árbol <c>area_scope</c>, de la raíz al nodo del trabajador
        /// (ej. ["Gerencia de Proyectos", "Unidad de Proyectos", "Unidad de Proyectos"]). Vacía si
        /// el trabajador no tiene área asignada.
        /// </summary>
        public List<string> AreaRuta { get; set; } = new();
        /// <summary>Nombre completo del jefe/revisor del solicitante (o el área en el fallback GTH).</summary>
        public string? RevisorNombre { get; set; }
        /// <summary>Correo al que se le notificó la solicitud: el email corporativo del revisor.</summary>
        public string? RevisorEmail { get; set; }
        public DateOnly FechaSalida { get; set; }
        public string EstadoAprobacion { get; set; } = string.Empty;
        public string EstadoRendicion { get; set; } = "No rendido";
        public DateTimeOffset CreatedAt { get; set; }
        public string? MotivoRechazo { get; set; }

        // -- Reembolso ---------------------------------------------------
        /// <summary>"Pendiente" | "Aprobado" | "Rechazado" | "Firmado" | "Pagado".</summary>
        public string EstadoReembolso { get; set; } = "Pendiente";
        /// <summary>Observacion del ultimo rechazo del reembolso.</summary>
        public string? ObservacionReembolso { get; set; }
        /// <summary>Nombre de quien aprobo/rechazo el reembolso.</summary>
        public string? ReembolsoDecididoPor { get; set; }
        public DateTimeOffset? ReembolsoDecididoAt { get; set; }
        /// <summary>Nombre de quien firmo la planilla de esta salida.</summary>
        public string? FirmadoPor { get; set; }
        public DateTimeOffset? FirmadoAt { get; set; }
        /// <summary>Nombre del tesorero que marco el reembolso como pagado.</summary>
        public string? PagadoPor { get; set; }
        public DateTimeOffset? PagadoAt { get; set; }

        public GestionSalidaRendicionDto? Rendicion { get; set; }
        /// <summary>PDF Consolidado del S10 vigente (propio de la salida o heredado de su planilla). Null si no hay.</summary>
        public Abril_Backend.Features.GestionAdministrativa.Shared.Dtos.ConsolidadoS10Dto? ConsolidadoS10 { get; set; }
        public List<GestionSalidaTrayectoDto> Trayectos { get; set; } = new();
    }

    public class GestionSalidaRendicionDto
    {
        public int Id { get; set; }
        public string PdfUrl { get; set; } = string.Empty;
        public string PdfFilename { get; set; } = string.Empty;
        public DateTimeOffset RendidoAt { get; set; }
        /// <summary>webUrl de la copia FIRMADA por la jefatura. Null mientras nadie la firme.</summary>
        public string? PdfFirmadoUrl { get; set; }
    }

    /// <summary>Una fila del PDF de planilla — un registro = UN TRAYECTO (no una solicitud).</summary>
    public class RendicionItemDto
    {
        /// <summary>Trayecto ID.</summary>
        public int Id { get; set; }
        /// <summary>Solicitud a la que pertenece este trayecto — para agrupar el TOTAL al final.</summary>
        public int SolicitudId { get; set; }
        public int WorkerId { get; set; }
        public string TrabajadorNombre { get; set; } = string.Empty;
        public string? TrabajadorDni { get; set; }
        /// <summary>person.document_identity_type_id (1 = DNI, 2 = CE). Define la etiqueta del documento en el PDF.</summary>
        public int? TrabajadorDocumentTypeId { get; set; }
        public string? Area { get; set; }
        public DateOnly FechaSalida { get; set; }
        public string Motivo { get; set; } = string.Empty;
        /// <summary>Detalle del motivo (motivos con requiere_motivo_adicional). Se imprime pegado
        /// al motivo en la columna MOTIVO de la planilla. Null si no aplica.</summary>
        public string? MotivoAdicional { get; set; }
        public string? LugarOrigen { get; set; }
        public string? LugarDestino { get; set; }
        /// <summary>Razón social de la empresa a la que está afiliado el trabajador.</summary>
        public string? RazonSocial { get; set; }
        public string? Ruc { get; set; }
        /// <summary>Suma de los montos de las capturas de este trayecto (columna IMPORTE).</summary>
        public decimal Importe { get; set; }
        /// <summary>True si el importe proviene del catálogo ga_trayecto (incluso si vale 0).</summary>
        public bool EsCatalogo { get; set; }
    }
}
