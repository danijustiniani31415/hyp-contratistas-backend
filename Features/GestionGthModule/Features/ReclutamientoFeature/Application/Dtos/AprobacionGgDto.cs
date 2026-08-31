namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    // RutaAprobacion vive en el namespace padre (Application): la ruta de una vacante es
    // regla de negocio, no forma del DTO, pero varios de estos la derivan.
    using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application;
    // FftDocumento: cómo se escribe el documento del candidato de un ingreso directo.
    using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared;

    /// <summary>
    /// Una de las dos casillas de decisión de la solicitud (gerente del área o Gerencia General),
    /// tal como se muestra en la lista y en el modal. Las dos tienen la misma forma para que la
    /// pantalla las pinte con el mismo componente.
    /// </summary>
    public class AprobacionNivelResumenDto
    {
        /// <summary>PENDIENTE / APROBADA / APROBADA_PARCIAL / RECHAZADA.</summary>
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>true cuando ese nivel ya decidió (deja de admitir cambios de ESE nivel).</summary>
        public bool Decidida { get; set; }

        /// <summary>Momento de la decisión en hora Perú (null si ese nivel sigue pendiente).</summary>
        public DateTime? DecididoEn { get; set; }

        /// <summary>
        /// Quién decidió. Null si sigue pendiente o si es una aprobación anterior a la pantalla
        /// (las decididas por el enlace del correo no dejaban usuario).
        /// </summary>
        public string? DecididoPor { get; set; }

        public string? Comentario { get; set; }

        public int VacantesAprobadas { get; set; }
        public int VacantesRechazadas { get; set; }
    }

    /// <summary>
    /// Detalle de una aprobación para el modal de «Aprobaciones»: cabecera de la solicitud, las
    /// vacantes de la ruta del usuario y sus casillas de decisión, en una sola petición.
    ///
    /// <b>Solo trae las vacantes de la ruta de quien pregunta</b> (ver <see cref="Nivel"/>): el
    /// Gerente General recibe las nuevas y las FFT, y el gerente del área y GTH los reemplazos.
    /// Las de la otra ruta no viajan — ni sus datos ni sus casillas —, así que el modal no puede
    /// mostrar por error algo que no le toca decidir.
    ///
    /// El modal se arma según <see cref="Nivel"/>: el usuario marca su casilla y ve la del otro
    /// firmante de su misma ruta (gerente del área ↔ GTH en un reemplazo) como información. Si su
    /// nivel ya decidió, abre completo en modo lectura.
    /// </summary>
    public class AprobacionGgDetalleDto
    {
        /// <summary>Id de la aprobación (el que viaja en el enlace del correo).</summary>
        public int AprobacionId { get; set; }

        /// <summary>Área solicitante (snapshot al registrar la solicitud).</summary>
        public string? Area { get; set; }

        /// <summary>Nombre del solicitante que registró la solicitud.</summary>
        public string? SolicitanteNombre { get; set; }

        public string? Justificacion { get; set; }

        /// <summary>Sustento adjunto de la solicitud (link de SharePoint), si hay.</summary>
        public string? SustentoNombre { get; set; }
        public string? SustentoUrl { get; set; }

        /// <summary>Fecha de registro de la solicitud en hora Perú (UTC-5).</summary>
        public DateTime Enviado { get; set; }

        // ── Las tres casillas ────────────────────────────────────────────────
        /// <summary>Decisión del gerente del área. Solo aplica si la solicitud trae reemplazos.</summary>
        public AprobacionNivelResumenDto GerenteArea { get; set; } = new();

        /// <summary>Decisión de Gerencia General. Solo aplica si la solicitud trae vacantes nuevas o FFT.</summary>
        public AprobacionNivelResumenDto GerenteGeneral { get; set; } = new();

        /// <summary>Decisión de GTH. Solo aplica si la solicitud trae reemplazos.</summary>
        public AprobacionNivelResumenDto Gth { get; set; } = new();

        // ── Qué firmas necesita lo que este usuario VE ───────────────────────
        // Se derivan de las vacantes que viajan, que ya son solo las de su ruta. Por eso la casilla
        // de la otra ruta llega siempre en false: al Gerente General no se le pintan las del
        // gerente del área ni de GTH, y a ellos no se les pinta la de Gerencia General. Dentro de
        // una misma ruta sí se pintan las dos, porque el reemplazo necesita las dos firmas.
        public bool RequiereGerenteGeneral { get; set; }
        public bool RequiereGerenteArea { get; set; }
        public bool RequiereGth { get; set; }

        // ── Con qué poder entra el usuario que abrió el modal ────────────────
        /// <summary>GERENTE_GENERAL / GERENTE_AREA / GTH / NINGUNO (ver <c>AprobacionNivel</c>).</summary>
        public string Nivel { get; set; } = string.Empty;

        /// <summary>
        /// true si este usuario todavía puede registrar SU decisión sobre esta solicitud: tiene
        /// nivel, la solicitud trae vacantes de su ruta y su casilla sigue pendiente. false ⇒ el
        /// modal abre en lectura.
        /// </summary>
        public bool PuedeDecidir { get; set; }

        public List<AprobacionGgVacanteDto> Vacantes { get; set; } = new();

        /// <summary>
        /// A quién le llegarán los correos que dispara la decisión de quien abre el modal,
        /// fusionados en una sola lista: para el Gerente General, el aviso a GTH (tipo SOLICITUD) y
        /// el de TI (tipo TI_VACANTES); para el gerente del área y para GTH, el de reemplazos
        /// aprobados (tipo REEMPLAZO_APROBADO). Salen del mismo resolver que usa el envío, así que
        /// el aviso del modal no puede prometer algo distinto de lo que se manda. Solo se resuelve
        /// cuando el usuario aún no ha decidido.
        ///
        /// Que estén resueltos no significa que el correo vaya a salir: un reemplazo lo dispara
        /// recién la SEGUNDA firma, así que la pantalla solo muestra el aviso cuando la decisión en
        /// curso completa alguna vacante.
        /// </summary>
        public SolicitudDestinatariosDto? Destinatarios { get; set; }
    }

    /// <summary>
    /// Pantalla «Aprobaciones» (Gestión GTH) en una sola petición: el alcance del usuario, las
    /// tarjetas de resumen y las solicitudes que puede ver — las pendientes de su decisión y el
    /// historial.
    /// </summary>
    public class AprobacionGgBandejaDto
    {
        /// <summary>GERENTE_GENERAL / GERENTE_AREA / NINGUNO (ver <c>AprobacionNivel</c>).</summary>
        public string Nivel { get; set; } = string.Empty;

        /// <summary>
        /// Área de la que el usuario es gerente (solo con nivel GERENTE_AREA), para poder explicar
        /// en pantalla por qué ve lo que ve.
        /// </summary>
        public string? AreaAlcance { get; set; }

        public AprobacionGgBandejaResumenDto Resumen { get; set; } = new();
        public List<AprobacionGgBandejaItemDto> Aprobaciones { get; set; } = new();
    }

    /// <summary>
    /// Contadores de las tarjetas. Se calculan SIEMPRE contra la casilla del usuario que consulta:
    /// "por aprobar" es lo que espera SU decisión, no lo que espera la del otro nivel. Todo en cero
    /// cuando el nivel es NINGUNO.
    /// </summary>
    public class AprobacionGgBandejaResumenDto
    {
        /// <summary>Solicitudes esperando la decisión de este usuario.</summary>
        public int Pendientes { get; set; }

        /// <summary>Vacantes que suman esas solicitudes pendientes (lo que está realmente en cola).</summary>
        public int VacantesPendientes { get; set; }

        /// <summary>Solicitudes que este usuario aprobó (total o parcialmente) — histórico.</summary>
        public int Aprobadas { get; set; }

        /// <summary>Solicitudes en las que este usuario rechazó todas las vacantes — histórico.</summary>
        public int Rechazadas { get; set; }
    }

    /// <summary>Una solicitud en la lista de «Aprobaciones» (una fila = una solicitud de personal).</summary>
    public class AprobacionGgBandejaItemDto
    {
        public int AprobacionId { get; set; }

        /// <summary>
        /// Códigos de las vacantes que este usuario ve —las de su ruta—, separados por ", " (para
        /// buscar y mostrar). Una solicitud mixta se lee distinto según quién pregunta: el Gerente
        /// General ve solo los códigos de sus vacantes nuevas.
        /// </summary>
        public string Codigos { get; set; } = string.Empty;

        public string? Area { get; set; }
        public string? SolicitanteNombre { get; set; }
        public string? Justificacion { get; set; }

        /// <summary>Fecha de registro de la solicitud en hora Perú (UTC-5).</summary>
        public DateTime Enviado { get; set; }

        /// <summary>
        /// Cuántas vacantes de esta solicitud le tocan al usuario que consulta. NO es el total de la
        /// solicitud: las de la otra ruta no se cuentan porque no se le muestran. El total real lo
        /// ve el solicitante en «Solicitud de Personal».
        /// </summary>
        public int TotalVacantes { get; set; }

        /// <summary>Decisión del gerente del área (mueve los reemplazos, junto con la de GTH).</summary>
        public AprobacionNivelResumenDto GerenteArea { get; set; } = new();

        /// <summary>Decisión de Gerencia General (mueve las vacantes nuevas y las FFT).</summary>
        public AprobacionNivelResumenDto GerenteGeneral { get; set; } = new();

        /// <summary>Decisión de GTH (mueve los reemplazos, junto con la del gerente del área).</summary>
        public AprobacionNivelResumenDto Gth { get; set; } = new();

        /// <summary>
        /// Qué firmas necesita lo que este usuario ve, derivado de los tipos de las vacantes de su
        /// ruta. La casilla de la otra ruta llega siempre en false (ver <see cref="TotalVacantes"/>).
        /// </summary>
        public bool RequiereGerenteGeneral { get; set; }
        public bool RequiereGerenteArea { get; set; }
        public bool RequiereGth { get; set; }

        /// <summary>
        /// true si esta fila espera la decisión del usuario que consulta. Es lo que decide el orden
        /// de la lista y el botón de la fila ("Revisar y aprobar" vs "Ver decisión").
        /// </summary>
        public bool EsperaMiDecision { get; set; }
    }

    /// <summary>Una vacante de la solicitud, con la decisión de cada nivel.</summary>
    public class AprobacionGgVacanteDto
    {
        public int RequerimientoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;

        /// <summary>Tipo de requerimiento (Nuevo / Reemplazo).</summary>
        public string TipoRequerimiento { get; set; } = string.Empty;

        /// <summary>
        /// Trabajador al que reemplaza la vacante: es el dato que le da sentido a un Reemplazo a la
        /// hora de aprobarlo. Null en las vacantes nuevas y en las anteriores a este dato.
        /// </summary>
        public string? TrabajadorReemplazado { get; set; }

        public string? ProyectoObra { get; set; }

        /// <summary>
        /// Salario bruto mensual que el área declaró para la vacante, en soles: es parte de lo que
        /// se está aprobando, así que va en el modal y en el correo. Null en las vacantes anteriores
        /// a que se pidiera el dato.
        /// </summary>
        public decimal? SalarioBrutoMensual { get; set; }

        /// <summary>
        /// true = la vacante es un ingreso directo <b>FFT</b>: no se publica ni se arma long list,
        /// el candidato ya viene con nombre y correo. Lo que se aprueba es a esa persona, así que el
        /// modal y el correo lo tienen que decir.
        /// </summary>
        public bool EsFft { get; set; }

        /// <summary>Nombre del candidato FFT que nombró el solicitante. Null en las vacantes normales.</summary>
        public string? FftCandidatoNombre { get; set; }

        /// <summary>
        /// Número de documento del candidato FFT. Va en el modal y en el correo por la misma razón
        /// que el nombre: lo que se aprueba es a una persona concreta, y el documento es lo único
        /// que la identifica sin ambigüedad (dos candidatos pueden llamarse igual). Null en las
        /// vacantes normales y en los FFT anteriores a que se pidiera el dato.
        /// </summary>
        public string? FftCandidatoDocumento { get; set; }

        /// <summary>
        /// Nombre del tipo de ese documento (DNI / CE). Null en las vacantes normales y en los FFT
        /// anteriores al desplegable — esos eran todos DNI, que era lo único que se podía declarar.
        /// </summary>
        public string? FftTipoDocumento { get; set; }

        /// <summary>
        /// El documento como se muestra: «DNI 12345678». Desde que la casilla FFT ofrece dos tipos,
        /// el número solo no dice cuál es. Los FFT anteriores al desplegable se muestran como DNI,
        /// que es lo que efectivamente eran.
        /// </summary>
        public string? FftDocumentoTexto =>
            FftDocumento.Texto(FftTipoDocumento ?? FftDocumento.Dni, FftCandidatoDocumento);

        /// <summary>Correo personal del candidato FFT. Null en las vacantes normales.</summary>
        public string? FftCandidatoCorreo { get; set; }

        /// <summary>
        /// Código estable del tipo (<c>NUEVO</c> / <c>REEMPLAZO</c>). Es lo que decide la
        /// <see cref="Ruta"/>: el nombre de al lado es presentación y se puede renombrar.
        /// </summary>
        public string TipoRequerimientoCodigo { get; set; } = string.Empty;

        /// <summary>
        /// Solo en las vacantes FFT: true cuando la vacante quedó enganchada a la aprobación
        /// (tiene su fila en <c>gth_aprobacion_gg_detalle</c>) porque se registró antes de que el
        /// ingreso directo dejara de aprobarse. Es lo que mantiene decidibles —y visibles en el
        /// historial de Gerencia General— a los FFT viejos. Ver <see cref="RutaAprobacion.De"/>.
        /// </summary>
        public bool FftEnAprobacionLegada { get; set; }

        /// <summary>
        /// Por dónde se aprueba esta vacante: <c>GG</c> (solo Gerencia General), <c>AREA_GTH</c>
        /// (gerente del área + GTH, los dos) o <c>NINGUNA</c> (el ingreso directo FFT, que no
        /// firma nadie). Se deriva y no se guarda — ver <see cref="RutaAprobacion"/>. La pantalla
        /// la usa para mostrar solo las casillas que aplican y para saber qué vacantes puede marcar
        /// quien tiene el modal abierto.
        /// </summary>
        public string Ruta => RutaAprobacion.De(EsFft, TipoRequerimientoCodigo, FftEnAprobacionLegada);

        /// <summary>Decisión del gerente del área: true / false / null = sin decidir.</summary>
        public bool? AprobadoGerenteArea { get; set; }

        /// <summary>Decisión de Gerencia General: true = aprobada, false = rechazada, null = sin decidir.</summary>
        public bool? AprobadoGerenteGeneral { get; set; }

        /// <summary>Decisión de GTH: true / false / null = sin decidir. Solo aplica en la ruta <c>AREA_GTH</c>.</summary>
        public bool? AprobadoGth { get; set; }
    }

    /// <summary>
    /// Decisión que envía un gerente desde la pantalla «Aprobaciones». El nivel con el que se
    /// registra NO viaja en el payload: lo resuelve el backend desde la categoría del usuario, para
    /// que nadie pueda pedir que su firma cuente como la del Gerente General.
    /// </summary>
    public class AprobacionGgDecisionDto
    {
        public List<VacanteDecisionGgDto> Decisiones { get; set; } = new();

        /// <summary>Comentario opcional (motivo del rechazo, condiciones, etc.).</summary>
        public string? Comentario { get; set; }
    }

    /// <summary>Decisión sobre una vacante concreta.</summary>
    public class VacanteDecisionGgDto
    {
        public int RequerimientoId { get; set; }
        public bool Aprobado { get; set; }
    }

    /// <summary>Resultado de registrar una decisión.</summary>
    public class AprobacionGgDecisionResultDto
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>Nivel con el que se registró (GERENTE_GENERAL / GERENTE_AREA).</summary>
        public string Nivel { get; set; } = string.Empty;

        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;
        public int Aprobados { get; set; }
        public int Rechazados { get; set; }
    }

    /// <summary>
    /// Decisión en bloque desde la lista de «Aprobaciones»: se aprueban (o rechazan) TODAS las
    /// vacantes de cada solicitud seleccionada. Es el mismo acto que abrir el modal de cada una y
    /// marcar todo igual, pero en una sola petición.
    ///
    /// Como en la decisión de una sola, el nivel con el que se registra NO viaja en el payload: lo
    /// resuelve el backend desde la categoría del usuario. Un gerente de área deja su visto bueno
    /// en las solicitudes de su alcance; el Gerente General registra la aprobación obligatoria y,
    /// solo entonces, lo aprobado sale a GTH y a TI.
    /// </summary>
    public class AprobacionGgDecisionMasivaDto
    {
        /// <summary>Solicitudes seleccionadas (ids de <c>gth_aprobacion_gg</c>).</summary>
        public List<int> AprobacionIds { get; set; } = new();

        /// <summary>
        /// true = aprobar todas las vacantes de cada solicitud; false = rechazarlas todas. La
        /// decisión es la misma para todas: en bloque no hay forma de decidir vacante por vacante
        /// (para eso está el modal de la fila).
        /// </summary>
        public bool Aprobado { get; set; }

        /// <summary>Comentario opcional; queda igual en todas las solicitudes del lote.</summary>
        public string? Comentario { get; set; }
    }

    /// <summary>
    /// Solicitud que quedó fuera del lote y por qué. No es un error de la petición: entre que la
    /// pantalla se cargó y el gerente decidió, la solicitud pudo cerrarse (el otro gerente de su
    /// mismo nivel decidió, o se dio de baja). Se devuelven para poder decirlo en pantalla en vez
    /// de dejar que el conteo no cuadre en silencio.
    /// </summary>
    public class AprobacionGgDecisionOmitidaDto
    {
        public int AprobacionId { get; set; }

        /// <summary>Motivo legible, listo para mostrarse al usuario.</summary>
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>Resultado de una decisión en bloque.</summary>
    public class AprobacionGgDecisionMasivaResultDto
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>Nivel con el que se registró (GERENTE_GENERAL / GERENTE_AREA).</summary>
        public string Nivel { get; set; } = string.Empty;

        /// <summary>true si el lote se aprobó; false si se rechazó (eco de lo pedido).</summary>
        public bool Aprobado { get; set; }

        /// <summary>Solicitudes en las que la decisión quedó registrada.</summary>
        public int Solicitudes { get; set; }

        /// <summary>Vacantes decididas en total (la suma de las vacantes de esas solicitudes).</summary>
        public int Vacantes { get; set; }

        /// <summary>Solicitudes seleccionadas que no se pudieron decidir, con su motivo.</summary>
        public List<AprobacionGgDecisionOmitidaDto> Omitidas { get; set; } = new();
    }

    /// <summary>
    /// Contexto de una decisión en bloque ya registrada: una entrada por solicitud decidida —con la
    /// misma forma que la decisión de una sola, para que los correos se armen con el mismo código—
    /// más las que se omitieron.
    /// </summary>
    public class AprobacionGgDecisionMasivaContextoDto
    {
        /// <summary>Nivel con el que se registró el lote.</summary>
        public string Nivel { get; set; } = string.Empty;

        public List<AprobacionGgDecisionContextoDto> Registradas { get; set; } = new();
        public List<AprobacionGgDecisionOmitidaDto> Omitidas { get; set; } = new();
    }

    /// <summary>
    /// Contexto para armar el correo que va a los gerentes (y, tras la decisión del GG, el de GTH
    /// con las vacantes aprobadas). Lo resuelve el repositorio en un solo roundtrip.
    /// </summary>
    public class AprobacionGgEnvioContextoDto
    {
        public int SolicitudId { get; set; }

        /// <summary>Id de la aprobación: es lo que viaja en el enlace del correo.</summary>
        public int AprobacionId { get; set; }

        public string? Area { get; set; }

        /// <summary>
        /// <c>area_scope</c> del solicitante (snapshot de la solicitud). Con él se resuelve al
        /// gerente del área, que recibe el correo junto al Gerente General.
        /// </summary>
        public int? AreaScopeId { get; set; }

        public string? SolicitanteNombre { get; set; }
        public string? Justificacion { get; set; }
        public string? SustentoNombre { get; set; }
        public string? SustentoUrl { get; set; }

        /// <summary>true si Gerencia General ya decidió (cierra las vacantes de ruta <c>GG</c>).</summary>
        public bool DecididaGg { get; set; }

        /// <summary>true si el gerente del área ya decidió (una de las dos firmas de los reemplazos).</summary>
        public bool DecididaGerenteArea { get; set; }

        /// <summary>true si GTH ya decidió (la otra firma de los reemplazos).</summary>
        public bool DecididaGth { get; set; }

        public List<AprobacionGgVacanteDto> Vacantes { get; set; } = new();

        /// <summary>
        /// Las vacantes que aprueba Gerencia General: las nuevas que no son un ingreso directo. Es
        /// exactamente lo que lista su correo — una solicitud que solo trae reemplazos (o puros
        /// FFT) no le manda nada.
        /// </summary>
        public List<AprobacionGgVacanteDto> VacantesGg =>
            Vacantes.Where(v => v.Ruta == RutaAprobacion.GerenciaGeneral).ToList();

        /// <summary>Las vacantes que aprueban el gerente del área y GTH: los reemplazos no-FFT.</summary>
        public List<AprobacionGgVacanteDto> VacantesReemplazo =>
            Vacantes.Where(v => v.Ruta == RutaAprobacion.AreaYGth).ToList();

        /// <summary>
        /// Los reemplazos que ya tienen el visto bueno del gerente del área y esperan el de GTH:
        /// es lo que lista el correo de <see cref="CorreoTipoReclutamiento.AprobacionReemplazoGth"/>
        /// cuando hay que reenviarlo. Los que el área rechazó no están —esos ya no continúan— y
        /// los que todavía no decidió tampoco: GTH no los ve hasta que el área firma.
        /// </summary>
        public List<AprobacionGgVacanteDto> VacantesReemplazoParaGth =>
            VacantesReemplazo.Where(v => v.AprobadoGerenteArea == true).ToList();

        /// <summary>
        /// Los ingresos directos de la solicitud: no los firma nadie, así que no salen en ningún
        /// correo de aprobación — son exactamente lo que lista el aviso a GTH. En una solicitud
        /// mixta esto es lo que impide que ese correo arrastre las vacantes normales, que sí están
        /// esperando una firma.
        /// </summary>
        public List<AprobacionGgVacanteDto> VacantesFft =>
            Vacantes.Where(v => v.EsFft && v.Ruta == RutaAprobacion.Ninguna).ToList();

        /// <summary>
        /// ¿Queda algo por reenviar? Un correo se reenvía mientras su ruta siga esperando alguna
        /// firma; el de reemplazos, mientras falte la del área o la de GTH.
        /// </summary>
        public bool PendienteGg => VacantesGg.Count > 0 && !DecididaGg;

        public bool PendienteReemplazo =>
            VacantesReemplazo.Count > 0 && (!DecididaGerenteArea || !DecididaGth);

        /// <summary>
        /// De quién es el turno abierto en la ruta del reemplazo: <c>GERENTE_AREA</c> mientras el
        /// área no decida, <c>GTH</c> una vez que decidió y quedó algo aprobado, y null cuando ya
        /// no hay a quién recordarle nada (las dos firmas puestas, el área rechazó todo, o la
        /// solicitud no trae reemplazos). Es lo que elige cuál de los dos correos de la ruta sale
        /// en cada momento: nunca los dos a la vez.
        /// </summary>
        public string? TurnoReemplazo =>
            VacantesReemplazo.Count == 0        ? null
            : !DecididaGerenteArea              ? AprobacionNivel.GerenteArea
            : DecididaGth                       ? null
            : VacantesReemplazoParaGth.Count > 0 ? AprobacionNivel.Gth
            : null;
    }

    /// <summary>
    /// Contexto de la decisión ya registrada: lo que necesita el servicio para notificar a GTH —solo
    /// con las vacantes que esta decisión dejó completamente aprobadas— y para armar el mensaje de
    /// respuesta de la pantalla.
    /// </summary>
    public class AprobacionGgDecisionContextoDto
    {
        public AprobacionGgDecisionResultDto Resultado { get; set; } = new();

        public int SolicitudId { get; set; }

        /// <summary>
        /// Id de la aprobación: es lo que viaja en el enlace de «Aprobaciones». Lo necesita el
        /// correo que le pide su firma a GTH, que es el único de esta decisión que manda a alguien
        /// de vuelta a esa pantalla — los demás llevan a Reclutamiento.
        /// </summary>
        public int AprobacionId { get; set; }

        public string? Area { get; set; }
        public string? SolicitanteNombre { get; set; }
        public string? Justificacion { get; set; }
        public string? SustentoNombre { get; set; }
        public string? SustentoUrl { get; set; }
        public string? Comentario { get; set; }

        /// <summary>
        /// Visto bueno que el gerente del área dejó registrado, si ya lo hizo. Va como contexto en
        /// los dos correos a GTH: en el de Gerencia General es una opinión que no condiciona nada, y
        /// en el del reemplazo es una de las dos firmas que lo movieron. Null si el área nunca opinó.
        /// </summary>
        public string? GerenteAreaResumen { get; set; }

        /// <summary>
        /// Vacantes que ESTA decisión dejó completamente aprobadas: son las únicas que pasan a
        /// manos de GTH para reclutarse. En la ruta de Gerencia General son las que el GG aprobó;
        /// en la del reemplazo, solo las que con esta firma juntaron las dos —o sea, las que firmó
        /// GTH, que va segundo.
        /// </summary>
        public List<AprobacionGgVacanteDto> Aprobadas { get; set; } = new();

        /// <summary>
        /// Vacantes que ESTA decisión aprobó pero que todavía esperan la firma que sigue. Hoy son
        /// siempre los reemplazos que acaba de aprobar el gerente del área: con su visto bueno se
        /// le abre el turno a GTH, así que son exactamente las que lleva el correo
        /// <see cref="CorreoTipoReclutamiento.AprobacionReemplazoGth"/>. Vacía en la decisión de
        /// Gerencia General (su firma va sola) y en la de GTH (es la última).
        /// </summary>
        public List<AprobacionGgVacanteDto> EsperandoSiguienteFirma { get; set; } = new();
    }

    /// <summary>
    /// Resumen de la aprobación de un requerimiento, para la tarjeta "Aprobación" del modal de
    /// seguimiento del solicitante. Null en los requerimientos anteriores a esta funcionalidad (no
    /// pasaron por el paso de aprobación).
    /// </summary>
    public class AprobacionGgResumenDto
    {
        /// <summary>Estado de la decisión de Gerencia General sobre la solicitud completa.</summary>
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>Decisión del GG sobre ESTA vacante: true / false / null = sin decidir.</summary>
        public bool? Aprobado { get; set; }

        /// <summary>Estado del visto bueno del gerente del área sobre la solicitud completa.</summary>
        public string GerenteAreaEstadoCodigo { get; set; } = string.Empty;
        public string GerenteAreaEstadoNombre { get; set; } = string.Empty;

        /// <summary>Visto bueno del gerente del área sobre ESTA vacante: true / false / null = no opinó.</summary>
        public bool? AprobadoGerenteArea { get; set; }

        /// <summary>Momento del envío del correo a los gerentes en hora Perú (null si nunca se pudo enviar).</summary>
        public DateTime? EnviadoEn { get; set; }

        /// <summary>Momento de la decisión del GG en hora Perú (null si sigue pendiente).</summary>
        public DateTime? DecididoEn { get; set; }

        /// <summary>Momento del visto bueno del gerente del área en hora Perú.</summary>
        public DateTime? GerenteAreaDecididoEn { get; set; }

        /// <summary>Comentario del Gerente General.</summary>
        public string? Comentario { get; set; }

        /// <summary>Comentario del gerente del área.</summary>
        public string? GerenteAreaComentario { get; set; }

        // ── GTH (solo en los reemplazos) ─────────────────────────────────────
        /// <summary>Estado de la decisión de GTH sobre la solicitud completa.</summary>
        public string GthEstadoCodigo { get; set; } = string.Empty;
        public string GthEstadoNombre { get; set; } = string.Empty;

        /// <summary>Decisión de GTH sobre ESTA vacante: true / false / null = sin decidir.</summary>
        public bool? AprobadoGth { get; set; }

        /// <summary>Momento de la decisión de GTH en hora Perú.</summary>
        public DateTime? GthDecididoEn { get; set; }

        /// <summary>Comentario de GTH.</summary>
        public string? GthComentario { get; set; }

        /// <summary>
        /// Ruta de esta vacante (<c>GG</c> / <c>AREA_GTH</c>): le dice al modal de seguimiento del
        /// solicitante qué firmas mostrar. Sin esto pintaría las tres y dos quedarían siempre en
        /// pendiente sin que nadie las vaya a tocar.
        /// </summary>
        public string Ruta { get; set; } = RutaAprobacion.GerenciaGeneral;
    }

    /// <summary>Resultado de reenviar el correo de aprobación a los gerentes.</summary>
    public class AprobacionGgReenvioResultDto
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>Destinatarios principales a los que se envió (para mostrarlos en el mensaje).</summary>
        public List<string> Destinatarios { get; set; } = new();
    }
}
