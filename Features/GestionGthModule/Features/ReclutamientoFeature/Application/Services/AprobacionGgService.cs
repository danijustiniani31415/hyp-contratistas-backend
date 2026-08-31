using System.Globalization;
using System.Security.Cryptography;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services.Notificaciones.Dtos;
using Abril_Backend.Shared.Services.Notificaciones.Interfaces;
using Layout = Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared.ReclutamientoEmailLayout;
using Textos = Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared.ReclutamientoEmailTextos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Services
{
    /// <summary>
    /// Aprobación de la solicitud de personal. Quién firma lo decide cada VACANTE, no la solicitud
    /// completa (ver <see cref="RutaAprobacion"/>):
    ///
    /// <list type="bullet">
    ///   <item><b>Vacante nueva</b> → la firma Gerencia General, y su firma sola la mueve. Al
    ///   gerente del área le llega un correo informativo, sin botón de aprobar: se entera, no
    ///   decide.</item>
    ///   <item><b>Reemplazo</b> → lo firman el gerente del área del solicitante Y GTH, en ese
    ///   orden: primero el área, y recién con su visto bueno le llega a GTH (correo + bandeja) para
    ///   la segunda firma. La vacante avanza con las dos, y se cae apenas una diga que no —si es la
    ///   del área, GTH ni llega a verla.</item>
    ///   <item><b>Ingreso directo FFT</b> → no la firma nadie: nace en manos de GTH esperando el EMO
    ///   de ingreso y lo único que sale es el aviso de
    ///   <see cref="EnviarIngresoDirectoAGth"/>, un correo por vacante.</item>
    /// </list>
    ///
    /// Flujo: el solicitante registra la solicitud → sale un correo por turno abierto, cada uno
    /// listando solo sus vacantes y con un enlace a la pantalla «Aprobaciones» del módulo Gestión
    /// GTH → el firmante decide ahí (con su sesión; si no la tiene, el login lo devuelve a esa
    /// misma pantalla) → si su ruta necesita otra firma, esa decisión abre el turno siguiente; si
    /// no falta ninguna, se le notifica a GTH.
    ///
    /// El aviso a GTH es un correo por ruta —<c>SOLICITUD</c> para lo que aprueba Gerencia General,
    /// <c>REEMPLAZO_APROBADO</c> para los reemplazos que juntaron las dos firmas—, cada uno con su
    /// propia configuración de destinatarios. El de TI (<c>TI_VACANTES</c>) sale con las dos.
    ///
    /// En la ruta del reemplazo las firmas van en ORDEN y no en paralelo: el gerente del área
    /// primero, GTH después. Hasta que el área no firma, GTH no ve esas vacantes ni recibe correo
    /// (ver <see cref="RutaAprobacion.LeTocaAhora"/>).
    ///
    /// El enlace del correo NO ejecuta la decisión: abre la solicitud y la decisión se confirma
    /// dentro de la app. Es a propósito — los clientes de correo (Outlook/Safe Links) precargan
    /// los enlaces, y un enlace que aprobara al abrirse se dispararía solo.
    /// </summary>
    public class AprobacionGgService : IAprobacionGgService
    {
        private readonly IAprobacionGgRepository   _repo;
        private readonly IAprobacionScopeResolver  _scopes;
        private readonly ISolicitudPersonalScopeResolver _scopesSolicitante;
        private readonly ICorreoDestinatariosResolver _destinatarios;
        private readonly IEmailService             _email;
        private readonly INotificacionesService    _notificaciones;
        private readonly IConfiguration            _configuration;
        private readonly ILogger<AprobacionGgService> _logger;

        public AprobacionGgService(
            IAprobacionGgRepository repo,
            IAprobacionScopeResolver scopes,
            ISolicitudPersonalScopeResolver scopesSolicitante,
            ICorreoDestinatariosResolver destinatarios,
            IEmailService email,
            INotificacionesService notificaciones,
            IConfiguration configuration,
            ILogger<AprobacionGgService> logger)
        {
            _repo           = repo;
            _scopes         = scopes;
            _scopesSolicitante = scopesSolicitante;
            _destinatarios  = destinatarios;
            _email          = email;
            _notificaciones = notificaciones;
            _configuration  = configuration;
            _logger         = logger;
        }

        // ── Envío a quien tenga que aprobar ───────────────────────────────────
        /// <summary>
        /// Manda la solicitud a aprobar. Ya no es un solo correo: cada vacante va por su ruta según
        /// el tipo de requerimiento, así que salen hasta DOS —el de Gerencia General con las
        /// vacantes nuevas y las FFT, y el del gerente del área + GTH con los reemplazos—, cada uno
        /// listando solo las suyas. Una solicitud de un solo tipo dispara uno solo.
        ///
        /// Devuelve true solo si salió TODO lo que tenía que salir: con dos rutas y una sin
        /// destinatarios configurados, la solicitud queda a medio avisar y el solicitante tiene que
        /// verlo para poder reenviarla.
        /// </summary>
        public async Task<bool> EnviarSolicitudAGerencia(int solicitudId, int? userId)
        {
            try
            {
                // Identificador aleatorio de la fila (columna NOT NULL con índice único). Ya no da
                // acceso a nada: la decisión se toma dentro de la app, con sesión.
                var nuevoToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
                var ctx  = await _repo.PrepararEnvio(solicitudId, nuevoToken, userId);

                var todosSalieron = await EnviarPorRutasAsync(ctx, esReenvio: false, userId);
                return todosSalieron;
            }
            catch (Exception ex)
            {
                // La solicitud ya quedó registrada: un fallo del correo no la revierte. El
                // solicitante lo reintenta desde su panel.
                _logger.LogWarning(ex, "No se pudieron enviar los correos de aprobación de la solicitud {SolicitudId}", solicitudId);
                return false;
            }
        }

        /// <summary>
        /// El envío (o reenvío) de los correos de aprobación de una solicitud, uno por turno con
        /// vacantes pendientes. Registra UN solo envío en la aprobación con la unión de todos los
        /// destinatarios: la columna es una y lo que interesa es a quién se le avisó.
        /// </summary>
        /// <returns>true si salieron todos los correos que correspondían.</returns>
        private async Task<bool> EnviarPorRutasAsync(
            AprobacionGgEnvioContextoDto ctx, bool esReenvio, int? userId)
        {
            var principales = new List<string>();
            var copias      = new List<string>();
            // A quién hay que ponerle el pendiente en la campanita: solo los que tienen que firmar.
            // El informativo va aparte porque su destinatario no decide nada, y un aviso titulado
            // "por aprobar" que no le abre ninguna solicitud es peor que ninguno.
            var aFirmar     = new List<string>();
            var esperados   = 0;
            var enviados    = 0;

            // En el reenvío solo se repite lo que sigue pendiente: volver a pedirle su firma a quien
            // ya la dio es ruido. En el primer envío las dos rutas están pendientes por definición.
            foreach (var ruta in RutasAEnviar(ctx, esReenvio))
            {
                if (ruta.CuentaParaElResultado) esperados++;
                var dest = await _destinatarios.ResolverAsync(ruta.TipoCorreo, ctx.AreaScopeId);
                if (dest.Para.Count == 0)
                {
                    _logger.LogWarning(
                        "No hay destinatarios para el correo {Tipo} de la solicitud {SolicitudId}; no se envía. " +
                        "El solicitante puede reintentarlo desde «Mis solicitudes de vacante».",
                        ruta.TipoCorreo, ctx.SolicitudId);
                    continue;
                }

                await EnviarCorreoRutaAsync(ctx, ruta, dest, esReenvio);
                principales.AddRange(dest.EmailsPara);
                copias.AddRange(dest.EmailsCopias);
                if (ruta.CuentaParaElResultado)
                {
                    aFirmar.AddRange(dest.EmailsPara);
                    aFirmar.AddRange(dest.EmailsCopias);
                    enviados++;
                }
            }

            if (enviados > 0)
            {
                // El envío registrado sí lleva a TODOS los avisados, informativo incluido: la
                // columna guarda a quién se le mandó el correo, no a quién se le pidió una firma.
                await _repo.RegistrarEnvio(
                    ctx.AprobacionId, principales.Distinct().ToList(), copias.Distinct().ToList(), esReenvio, userId);
                await CrearNotificacionAprobacionAsync(ctx, aFirmar, userId);
            }

            return esperados > 0 && enviados == esperados;
        }

        /// <summary>
        /// Un correo del envío: de qué tipo es y qué vacantes lleva.
        /// </summary>
        /// <param name="CuentaParaElResultado">
        /// false en los correos que solo informan. Un informativo que no salga (porque nadie lo
        /// configuró o porque está apagado) no puede hacer que la solicitud figure como "a medio
        /// avisar" y le ponga al solicitante un botón de reenviar que no arregla nada: lo que se
        /// mide es si llegó a quien tiene que firmar.
        /// </param>
        private sealed record RutaCorreo(
            string TipoCorreo,
            List<AprobacionGgVacanteDto> Vacantes,
            bool CuentaParaElResultado = true);

        /// <summary>
        /// Los correos que hay que mandar al registrar (o al reenviar) la solicitud:
        ///
        /// <list type="bullet">
        ///   <item>Vacantes NUEVAS → el pedido de firma a Gerencia General, más el aviso
        ///   informativo al gerente del área, que no las decide pero tiene que enterarse.</item>
        ///   <item>REEMPLAZOS → un solo correo, el del turno que esté abierto: el del gerente del
        ///   área mientras no haya firmado, y el de GTH una vez que firmó (con solo las vacantes
        ///   que aprobó). Nunca los dos: las firmas van una después de la otra.</item>
        ///   <item>Ingresos directos → ninguno: no los firma nadie.</item>
        /// </list>
        ///
        /// En un reenvío se descartan los turnos ya cerrados y el aviso informativo: al gerente del
        /// área no se le debe ninguna firma sobre una vacante nueva, así que recordárselo sería
        /// ruido.
        /// </summary>
        private static List<RutaCorreo> RutasAEnviar(AprobacionGgEnvioContextoDto ctx, bool esReenvio)
        {
            var rutas = new List<RutaCorreo>();

            if (ctx.VacantesGg.Count > 0 && (!esReenvio || ctx.PendienteGg))
            {
                rutas.Add(new RutaCorreo(CorreoTipoReclutamiento.AprobacionGg, ctx.VacantesGg));
                if (!esReenvio)
                    rutas.Add(new RutaCorreo(
                        CorreoTipoReclutamiento.AvisoGerenteArea, ctx.VacantesGg,
                        CuentaParaElResultado: false));
            }

            // El turno lo decide el contexto (ver TurnoReemplazo): null = no hay a quién pedirle
            // nada, ni en el primer envío ni en el reenvío.
            switch (ctx.TurnoReemplazo)
            {
                case AprobacionNivel.GerenteArea:
                    rutas.Add(new RutaCorreo(
                        CorreoTipoReclutamiento.AprobacionReemplazo, ctx.VacantesReemplazo));
                    break;
                case AprobacionNivel.Gth:
                    rutas.Add(new RutaCorreo(
                        CorreoTipoReclutamiento.AprobacionReemplazoGth, ctx.VacantesReemplazoParaGth));
                    break;
            }

            return rutas;
        }

        // ── Ingreso directo FFT: aviso a GTH ──────────────────────────────────
        public async Task<bool> EnviarIngresoDirectoAGth(int solicitudId, int? userId)
        {
            try
            {
                var ctx = await _repo.GetContextoSinAprobacion(solicitudId);
                if (ctx == null)
                {
                    _logger.LogWarning(
                        "No se encontró la solicitud FFT {SolicitudId} para avisarle a GTH; no se envía.", solicitudId);
                    return false;
                }

                // SOLO las de ingreso directo: en una solicitud mixta el resto sigue esperando su
                // firma, y meterlas acá le anunciaría a GTH un trabajo que todavía no tiene.
                var vacantes = ctx.VacantesFft;
                if (vacantes.Count == 0)
                {
                    _logger.LogWarning(
                        "La solicitud {SolicitudId} no tiene vacantes de ingreso directo; no se envía el aviso a GTH.",
                        solicitudId);
                    return false;
                }

                var dest = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.FftSolicitudGg);
                if (dest.Para.Count == 0)
                {
                    // También entra acá cuando el correo está apagado con su interruptor maestro: en
                    // ese caso es una decisión de la Configuración, no una falla. La solicitud ya
                    // quedó registrada y GTH la ve igual en su bandeja.
                    _logger.LogWarning(
                        "No hay destinatarios para el correo del ingreso directo FFT " +
                        "(solicitud {SolicitudId}); no se envía.", solicitudId);
                    return false;
                }

                // Un correo por vacante: cada uno es un requerimiento con su propio candidato y su
                // propio botón al detalle. Ver EnviarFftPorVacanteAsync.
                var salieron = 0;
                foreach (var vacante in vacantes)
                {
                    if (await EnviarFftPorVacanteAsync(
                            vacante, dest, ctx.Area, ctx.SolicitanteNombre, ctx.Justificacion,
                            ctx.SustentoUrl, ctx.SustentoNombre, esPedido: true))
                        salieron++;
                }

                // La campanita va una sola vez con todas las vacantes: ya crea un aviso por
                // candidato, y partirla junto con los correos solo repetiría los roundtrips.
                await CrearNotificacionFftAsync(vacantes, ctx.Area, ctx.Justificacion, dest, userId);
                return salieron == vacantes.Count;
            }
            catch (Exception ex)
            {
                // La solicitud ya quedó registrada: un fallo del correo no la revierte.
                _logger.LogWarning(ex,
                    "No se pudo enviar a GTH el aviso del ingreso directo FFT de la solicitud {SolicitudId}", solicitudId);
                return false;
            }
        }

        public async Task<AprobacionGgReenvioResultDto> Reenviar(int requerimientoId, int? userId)
        {
            if (!userId.HasValue)
                throw new AbrilException("No se pudo identificar al usuario.", 401);

            // Reenviar es mover el requerimiento, así que se pide lo mismo que para decidirlo: ser
            // jefatura del área. La visibilidad, en cambio, ya no es del dueño sino del área.
            var scope = await _scopesSolicitante.ResolveAsync(userId.Value);
            if (!scope.PuedeGestionar)
                throw new AbrilException(
                    "Solo las jefaturas y gerencias del área pueden reenviar el correo de aprobación.", 403);

            var ctx = await _repo.GetEnvioContextoByRequerimiento(requerimientoId, scope);
            if (ctx == null)
                throw new AbrilException("No se encontró la aprobación de esta solicitud.", 404);

            // Se reenvía lo que siga esperando una firma. Con las dos rutas ya cerradas no queda
            // nada que recordar y decirlo es mejor que mandar un correo que nadie tiene que atender.
            var rutas = RutasAEnviar(ctx, esReenvio: true);
            if (rutas.Count == 0)
                throw new AbrilException("Esta solicitud ya fue decidida: no queda nada por reenviar.", 409);

            // Mismos destinatarios que el primer envío, resueltos ruta por ruta.
            var principales = new List<string>();
            var copias      = new List<string>();
            foreach (var ruta in rutas)
            {
                var dest = await _destinatarios.ResolverAsync(ruta.TipoCorreo, ctx.AreaScopeId);
                if (dest.Para.Count == 0)
                    throw new AbrilException(
                        "No hay destinatarios activos para el correo de aprobación. " +
                        "Revísalos en «Configuración» de Solicitud de Personal e inténtalo de nuevo.", 409);

                // Reenvío bloqueante: el usuario lo pidió explícitamente, así que si falla debe saberlo.
                try
                {
                    await EnviarCorreoRutaAsync(ctx, ruta, dest, esReenvio: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Falló el reenvío del correo {Tipo} (requerimiento {RequerimientoId})",
                        ruta.TipoCorreo, requerimientoId);
                    throw new AbrilException("No se pudo enviar el correo de aprobación. Vuelve a intentarlo.", 502);
                }

                principales.AddRange(dest.EmailsPara);
                copias.AddRange(dest.EmailsCopias);
            }

            principales = principales.Distinct().ToList();
            copias      = copias.Distinct().ToList();

            await _repo.RegistrarEnvio(ctx.AprobacionId, principales, copias, esReenvio: true, userId);
            // En el reenvío no hay correos informativos (ver RutasAEnviar), así que todos los
            // destinatarios de acá son gente a la que se le está pidiendo una firma.
            await CrearNotificacionAprobacionAsync(
                ctx, principales.Concat(copias).Distinct().ToList(), userId);

            return new AprobacionGgReenvioResultDto
            {
                Message       = $"Correo reenviado a {string.Join(", ", principales)}.",
                Destinatarios = principales,
            };
        }

        /// <summary>
        /// Envía el correo de UN turno, con solo las vacantes que le tocan. El registro del envío y
        /// la campanita van aparte (una sola vez por solicitud, aunque salgan varios correos): son
        /// de la solicitud, no del correo.
        /// </summary>
        private async Task EnviarCorreoRutaAsync(
            AprobacionGgEnvioContextoDto ctx,
            RutaCorreo ruta,
            SolicitudDestinatariosDto dest,
            bool esReenvio)
        {
            var vacantes = ruta.Vacantes;
            var esAviso  = ruta.TipoCorreo == CorreoTipoReclutamiento.AvisoGerenteArea;

            // El informativo no dice "Aprobación": nadie tiene que aprobar nada al recibirlo.
            var asunto = esAviso
                ? vacantes.Count == 1
                    ? $"[Reclutamiento] Solicitud de personal registrada — {vacantes[0].Codigo}"
                    : $"[Reclutamiento] Solicitud de {vacantes.Count} vacantes registrada — {ctx.Area}"
                : vacantes.Count == 1
                    ? $"[Reclutamiento] Aprobación de vacante — {vacantes[0].Codigo}"
                    : $"[Reclutamiento] Aprobación de {vacantes.Count} vacantes — {ctx.Area}";
            if (esReenvio) asunto = $"[Recordatorio] {asunto}";

            await _email.SendAsync(
                to:      dest.EmailsPara,
                subject: asunto,
                body:    ConstruirCuerpoGerencia(ctx, vacantes, esReenvio, esAviso),
                isHtml:  true,
                cc:      dest.Copias.Count > 0 ? dest.EmailsCopias : null);
        }

        /// <summary>
        /// Campanita para los destinatarios que sí tengan usuario (los buzones grupales se ignoran).
        /// Una sola notificación por solicitud aunque hayan salido los dos correos: para quien la
        /// recibe es un único pendiente —entrar a «Aprobaciones» y decidir lo suyo—, y partirla en
        /// dos solo duplicaría el aviso al gerente de un área que pidió de los dos tipos.
        /// </summary>
        /// <param name="aFirmar">
        /// Solo los destinatarios de los correos que PIDEN una firma. El del aviso informativo
        /// queda fuera: para él no hay ninguna solicitud que abrir en «Aprobaciones».
        /// </param>
        private async Task CrearNotificacionAprobacionAsync(
            AprobacionGgEnvioContextoDto ctx, List<string> aFirmar, int? userId)
        {
            try
            {
                var resumen = ctx.Vacantes.Count == 1
                    ? ctx.Vacantes[0].Puesto
                    : $"{ctx.Vacantes.Count} vacantes";
                await _notificaciones.CrearPorCorreosAsync(
                    NotificacionTipoCodigo.GthAprobacionGg,
                    aFirmar.Distinct().ToList(),
                    userId,
                    new List<NuevaNotificacionDto>
                    {
                        new()
                        {
                            Titulo      = "Solicitud de personal por aprobar",
                            Subtitulo   = string.IsNullOrWhiteSpace(ctx.Area) ? resumen : $"{resumen} — {ctx.Area}",
                            Descripcion = ctx.Justificacion,
                            Referencia  = string.Join(", ", ctx.Vacantes.Select(v => v.Codigo)),
                        },
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo crear la notificación in-app de la aprobación (solicitud {SolicitudId})", ctx.SolicitudId);
            }
        }

        /// <summary>
        /// Salario bruto mensual de la vacante formateado en soles para la tabla de los correos a
        /// los gerentes y a GTH (los dos que sí lo necesitan: uno lo aprueba y el otro arma la
        /// oferta). Null cuando la vacante no lo declara: los reemplazos ya no lo piden, y los
        /// requerimientos anteriores a que existiera el campo tampoco lo tienen. Con todas las
        /// vacantes del correo en null, la tabla sale sin esa columna.
        /// </summary>
        private static string? SalarioTexto(AprobacionGgVacanteDto v) =>
            v.SalarioBrutoMensual.HasValue
                ? $"S/ {v.SalarioBrutoMensual.Value.ToString("N2", CultureInfo.InvariantCulture)}"
                : null;

        /// <summary>
        /// ¿Alguna de estas vacantes declara sueldo? Es lo que decide si la tabla del correo lleva
        /// la columna: una solicitud de puros reemplazos la dejaría llena de guiones.
        /// </summary>
        private static bool ConSalario(IReadOnlyList<AprobacionGgVacanteDto> vacantes) =>
            vacantes.Any(v => v.SalarioBrutoMensual.HasValue);

        /// <summary>
        /// Cuerpo del correo de aprobación: las vacantes de ESE turno en una tabla + un acceso a la
        /// pantalla «Aprobaciones», donde cada uno decide vacante por vacante. El mismo cuerpo sirve
        /// para los tres correos porque el texto no le habla a nadie en particular —dentro de la
        /// pantalla cada quien ve su propia casilla—, y lo que cambia entre uno y otro es a quién se
        /// le manda y qué vacantes lleva, no cómo se ve.
        ///
        /// El HTML vive en <see cref="AprobacionGgEmailTemplate"/>, con la misma identidad visual
        /// que el correo de «EMO Confirmado».
        /// </summary>
        /// <param name="esAviso">
        /// true = el correo informativo al gerente del área de las vacantes NUEVAS. Sale sin el
        /// botón ni el enlace a «Aprobaciones» —no las decide él, y darle un acceso a una pantalla
        /// donde esas vacantes no le aparecen sería mandarlo a una puerta cerrada— y con otra
        /// bajada: se le está contando algo, no pidiendo.
        /// </param>
        private string ConstruirCuerpoGerencia(
            AprobacionGgEnvioContextoDto ctx, List<AprobacionGgVacanteDto> vacantesDeLaRuta,
            bool esReenvio, bool esAviso = false)
        {
            // El origen de las imágenes es una clave aparte de App:FrontendUrl a propósito: Outlook
            // no las descarga desde el cliente sino a través del proxy de imágenes de Microsoft, que
            // nunca puede alcanzar un localhost. Con App:FrontendUrl (que en dev tiene que seguir
            // apuntando a localhost para que el enlace del correo sea clicable) las imágenes salen
            // siempre rotas al probar en local.
            var assetsUrl = _configuration["App:EmailAssetsUrl"]
                ?? _configuration["App:FrontendUrl"]
                ?? "https://intranet.abril.pe";

            var vacantes = vacantesDeLaRuta
                .Select(v => new AprobacionGgEmailTemplate.Vacante(
                    Codigo:       v.Codigo,
                    Puesto:       v.Puesto,
                    Tipo:         v.TipoRequerimiento,
                    Reemplazado:  v.TrabajadorReemplazado,
                    ProyectoObra: string.IsNullOrWhiteSpace(v.ProyectoObra) ? "—" : v.ProyectoObra!,
                    Salario:      SalarioTexto(v)))
                .ToList();

            return AprobacionGgEmailTemplate.Construir(
                new AprobacionGgEmailTemplate.Datos(
                    Area:           string.IsNullOrWhiteSpace(ctx.Area) ? "—" : ctx.Area!,
                    Solicitante:    ctx.SolicitanteNombre,
                    Vacantes:       vacantes,
                    Justificacion:  ctx.Justificacion,
                    SustentoUrl:    ctx.SustentoUrl,
                    SustentoNombre: ctx.SustentoNombre,
                    // Sin enlace en el informativo: es lo único que lo distingue del correo de
                    // aprobación, y la plantilla se acomoda sola (ver Datos.Link).
                    Link:           esAviso ? null : ConstruirLink(ctx.AprobacionId),
                    EsRecordatorio: esReenvio),
                assetsUrl);
        }

        // ── Correos del flujo FFT ─────────────────────────────────────────────

        /// <summary>
        /// Manda el correo FFT de UNA vacante y dice si salió. Los correos FFT van de a uno por
        /// requerimiento —no uno por solicitud con la tabla de todos— justamente para que el botón
        /// pueda llevar al detalle de ESE requerimiento y abrirle a GTH el modal donde le programa
        /// el EMO: un enlace abre un modal, y una solicitud FFT puede traer varias vacantes.
        ///
        /// No lanza: la solicitud (o la decisión) ya quedó registrada y no se revierte porque un
        /// correo falle. Que uno se caiga tampoco frena a los demás — el resto de candidatos tiene
        /// que llegar igual.
        /// </summary>
        private async Task<bool> EnviarFftPorVacanteAsync(
            AprobacionGgVacanteDto vacante,
            SolicitudDestinatariosDto dest,
            string? area,
            string? solicitanteNombre,
            string? justificacion,
            string? sustentoUrl,
            string? sustentoNombre,
            bool esPedido)
        {
            try
            {
                var asunto = esPedido
                    ? $"[Reclutamiento] Ingreso directo FFT — {vacante.Codigo}"
                    : $"[Reclutamiento] Ingreso directo FFT aprobado — {vacante.Codigo}";

                await _email.SendAsync(
                    to:      dest.EmailsPara,
                    subject: asunto,
                    body:    ConstruirCuerpoFft(vacante, area, solicitanteNombre, justificacion,
                                                sustentoUrl, sustentoNombre, esPedido),
                    isHtml:  true,
                    cc:      dest.Copias.Count > 0 ? dest.EmailsCopias : null);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo enviar el correo del ingreso directo FFT del requerimiento {Codigo}", vacante.Codigo);
                return false;
            }
        }

        /// <summary>
        /// Columnas de la tabla de los correos FFT. Reemplaza a la de vacantes normales: en FFT lo
        /// que importa no es el tipo de requerimiento (siempre es un ingreso directo) sino a QUIÉN
        /// se está pidiendo, así que las dos columnas del candidato ocupan ese lugar. La del
        /// documento dice también de qué tipo es: desde que la casilla FFT ofrece DNI y carné de
        /// extranjería, el número solo no alcanza para identificarlo.
        ///
        /// La tabla lleva una sola fila (un correo = un requerimiento), pero sigue siendo tabla y no
        /// tarjeta: los datos del candidato se leen mejor en columnas, y así el correo del ingreso
        /// directo se ve igual que los demás del flujo.
        /// </summary>
        private static readonly IReadOnlyList<Layout.Columna> ColumnasFft = new List<Layout.Columna>
        {
            new("Código", 100),
            new("Puesto", 92),
            new("Candidato", 116),
            new("Documento", 96, Layout.Alineacion.Derecha),
            new("Correo personal", 132),
            new("Salario bruto", 92, Layout.Alineacion.Derecha),
        };

        /// <summary>
        /// La misma tabla sin la columna del sueldo, para los FFT que no lo declaran (los de tipo
        /// reemplazo ya no lo piden). Los anchos se reparten el espacio que deja.
        /// </summary>
        private static readonly IReadOnlyList<Layout.Columna> ColumnasFftSinSalario = new List<Layout.Columna>
        {
            new("Código", 116),
            new("Puesto", 112),
            new("Candidato", 140),
            new("Documento", 100, Layout.Alineacion.Derecha),
            new("Correo personal", 160),
        };

        /// <summary>
        /// Cuerpo de los dos correos FFT a GTH: el del ingreso directo recién registrado y el de la
        /// aprobación de Gerencia General sobre un FFT de los que quedaron esperando su firma. Son
        /// el mismo correo con una línea distinta —lo que cambia es de dónde viene, no lo que GTH
        /// tiene que hacer— así que comparten cuerpo en vez de duplicarse.
        ///
        /// Es de UNA vacante: dice quién pide, a quién, y que el siguiente paso es el EMO de
        /// ingreso. El botón lleva al detalle de ese requerimiento, que abre el modal parado ahí.
        /// </summary>
        private string ConstruirCuerpoFft(
            AprobacionGgVacanteDto vacante,
            string? area,
            string? solicitanteNombre,
            string? justificacion,
            string? sustentoUrl,
            string? sustentoNombre,
            bool esPedido)
        {
            var l    = Layout.Desde(_configuration);
            var link = ConstruirLinkFft(vacante.RequerimientoId);

            var datos = new List<Layout.Fila>
            {
                new("req-area", "Área solicitante", Textos.OGuion(area)),
            };
            if (!string.IsNullOrWhiteSpace(solicitanteNombre))
                datos.Add(new("req-solicitante", "Solicitante", Layout.Esc(solicitanteNombre)));

            var contexto = new List<Layout.Fila>();
            if (!string.IsNullOrWhiteSpace(justificacion))
                contexto.Add(new("req-justificacion", "Justificación", Layout.EscMultilinea(justificacion)));
            if (!string.IsNullOrWhiteSpace(sustentoUrl))
                contexto.Add(new("req-sustento", "Sustento adjunto",
                    Textos.Enlace(sustentoUrl!, sustentoNombre ?? "Ver documento")));

            var conSalario = vacante.SalarioBrutoMensual.HasValue;
            var bajada = esPedido
                ? "Se registró el ingreso directo de un nuevo candidato:"
                : "Gerencia General aprobó el ingreso de un nuevo candidato:";

            return l.Documento(
                new Layout.Cabecera("req-formulario", "Ingreso Directo FFT", bajada),
                l.Tarjeta(datos),
                l.Seccion("req-candidatos", "Candidato solicitado"),
                l.Tabla(conSalario ? ColumnasFft : ColumnasFftSinSalario, FilasFft(vacante, conSalario)),
                l.Tarjeta(contexto),
                // El paso que sigue, en una línea: el ingreso directo no publica la vacante, ni arma
                // long list, ni le manda formulario a nadie —sus datos ya los declaró quien lo
                // pidió—, así que lo único pendiente es el examen médico de ingreso.
                l.Franja("req-aviso", Layout.Tono.Info,
                    "<b>Siguiente paso:</b> programarle su EMO de ingreso."),
                l.Boton("Programar EMO de ingreso", link),
                l.EnlaceDirecto(link));
        }

        /// <summary>
        /// La única fila de la tabla de los correos FFT: el candidato que nombró el solicitante, con
        /// su DNI (lo que lo identifica sin ambigüedad y con lo que ya quedó registrado en la base
        /// maestra) y su correo personal (el buzón al que GTH le va a mandar el formulario).
        /// </summary>
        private static List<IReadOnlyList<Layout.Celda>> FilasFft(
            AprobacionGgVacanteDto v, bool conSalario)
        {
            var celdas = new List<Layout.Celda>
            {
                new(Layout.Esc(v.Codigo), Negrita: true, Color: Layout.Azul, NoWrap: true),
                new(Layout.Esc(v.Puesto)),
                new(Textos.OGuion(v.FftCandidatoNombre), Negrita: true),
                // "DNI 12345678": el tipo va pegado al número y no en una columna aparte —
                // agregarle una séptima columna a esta tabla la dejaría ilegible en Outlook.
                new(Textos.OGuion(v.FftDocumentoTexto), NoWrap: true),
                new(Textos.OGuion(v.FftCandidatoCorreo)),
            };
            if (conSalario) celdas.Add(new(Textos.OGuion(SalarioTexto(v)), Negrita: true, NoWrap: true));
            return new List<IReadOnlyList<Layout.Celda>> { celdas };
        }

        /// <summary>
        /// Enlace del botón de los correos FFT: el detalle del requerimiento, que abre el modal ya
        /// parado en el EMO de ingreso —el único paso pendiente— con el botón para programarlo a la
        /// vista. Por eso estos correos salen de a uno por vacante: un enlace abre un modal, y una
        /// solicitud FFT puede traer varios candidatos, cada uno con su requerimiento.
        /// </summary>
        private string ConstruirLinkFft(int requerimientoId)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/gestion-gth/reclutamiento/requerimiento/{requerimientoId}";
        }

        /// <summary>
        /// Campanita de los correos FFT: una por candidato pedido, a los mismos destinatarios del
        /// correo que sean usuarios del sistema. No bloquea — el correo ya salió.
        /// </summary>
        private async Task CrearNotificacionFftAsync(
            IReadOnlyList<AprobacionGgVacanteDto> vacantes,
            string? area,
            string? justificacion,
            SolicitudDestinatariosDto dest,
            int? userId)
        {
            try
            {
                var items = vacantes.Select(v => new NuevaNotificacionDto
                {
                    Titulo      = "Ingreso directo FFT",
                    Subtitulo   = string.IsNullOrWhiteSpace(v.FftCandidatoNombre)
                                    ? v.Puesto
                                    : $"{v.FftCandidatoNombre} — {v.Puesto}",
                    Descripcion = justificacion,
                    Referencia  = v.Codigo,
                }).ToList();

                await _notificaciones.CrearPorCorreosAsync(
                    NotificacionTipoCodigo.GthSolicitudPersonal,
                    dest.EmailsPara.Concat(dest.EmailsCopias).ToList(),
                    userId,
                    items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo crear la notificación in-app del ingreso directo FFT ({Area})", area);
            }
        }

        /// <summary>
        /// Enlace a la solicitud dentro de «Aprobaciones». Si el gerente no tiene sesión, el
        /// <c>authGuard</c> del frontend lo manda al login con esta URL como <c>returnUrl</c> y lo
        /// devuelve acá al entrar.
        /// </summary>
        private string ConstruirLink(int aprobacionId)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/gestion-gth/aprobaciones/{aprobacionId}";
        }

        /// <summary>
        /// Enlace del botón del correo a GTH, la contraparte del de «Aprobaciones»: lleva a
        /// «Reclutamiento», que es donde a GTH le toca trabajar la vacante recién aprobada.
        ///
        /// Con UNA vacante aprobada va al detalle del requerimiento, que abre el modal ya parado en
        /// la fase en la que quedó — recién aprobado, eso es la publicación de la vacante. Con
        /// VARIAS no puede: un enlace abre un modal y hay un requerimiento por vacante, así que
        /// lleva a la bandeja, donde están todas las filas nuevas. Sin sesión el <c>authGuard</c>
        /// del frontend manda al login con esta URL como <c>returnUrl</c> y devuelve al usuario acá.
        /// </summary>
        private string ConstruirLinkReclutamiento(IReadOnlyList<AprobacionGgVacanteDto> vacantes)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return vacantes.Count == 1
                ? $"{frontendUrl}/gestion-gth/reclutamiento/requerimiento/{vacantes[0].RequerimientoId}"
                : $"{frontendUrl}/gestion-gth/reclutamiento";
        }

        // ── Pantalla «Aprobaciones» y decisión ────────────────────────────────
        public async Task<AprobacionGgBandejaDto> GetBandeja(int? userId) =>
            await _repo.GetBandeja(await _scopes.ResolveAsync(userId));

        public async Task<AprobacionGgDetalleDto> GetDetalle(int aprobacionId, int? userId)
        {
            var scope = await _scopes.ResolveAsync(userId);

            var dto = await _repo.GetDetalle(aprobacionId, scope);
            if (dto == null)
                throw new AbrilException("La solicitud por aprobar no existe o ya no está disponible.", 404);

            // Aviso "a quién le llegará esta decisión" del modal. Son exactamente los destinatarios
            // que se usan al confirmar y, como allá, sin área — ninguno de estos correos depende del
            // área del solicitante. Va en la misma petición que el detalle.
            //
            // Cuáles se consultan depende del nivel de quien abre, porque cada decisión dispara
            // otros correos: la de Gerencia General manda el aviso a GTH (SOLICITUD) y el de TI
            // (TI_VACANTES); la del gerente del área, el pedido de firma a GTH
            // (APROBACION_REEMPLAZO_GTH), que es lo único que sale con la primera de las dos
            // firmas; y la de GTH, que es la segunda, el de reemplazos aprobados
            // (REEMPLAZO_APROBADO) más el de TI.
            //
            // Los correos de una misma decisión se muestran juntos: al gerente le importa a quién le
            // llega su decisión, no cuántos correos salen por detrás.
            //
            // Solo se consultan cuando el usuario aún no decidió: en lectura no va a salir nada.
            if (dto.PuedeDecidir && dto.Nivel != AprobacionNivel.Ninguno)
            {
                try
                {
                    var fuentes = new List<SolicitudDestinatariosDto>();
                    if (dto.Nivel == AprobacionNivel.GerenteGeneral)
                    {
                        fuentes.Add(await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.Solicitud));
                        fuentes.Add(await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.Ti));
                        // Con vacantes FFT sale además su propio aviso a GTH, así que sus
                        // destinatarios entran en la misma lista: el gerente tiene que ver a quién
                        // le llega TODO lo que dispara su decisión, no una parte.
                        if (dto.Vacantes.Any(v => v.EsFft))
                            fuentes.Add(await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.FftAprobacionGg));
                    }
                    else if (dto.Nivel == AprobacionNivel.GerenteArea)
                    {
                        fuentes.Add(await _destinatarios.ResolverAsync(
                            CorreoTipoReclutamiento.AprobacionReemplazoGth));
                    }
                    else
                    {
                        fuentes.Add(await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.ReemplazoAprobado));
                        fuentes.Add(await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.Ti));
                    }

                    dto.Destinatarios = Fusionar(fuentes.ToArray());
                }
                catch (Exception ex)
                {
                    // El aviso es informativo: si no se puede resolver, el modal abre sin él. La
                    // decisión tiene que poder tomarse igual.
                    _logger.LogWarning(ex,
                        "No se pudieron resolver los destinatarios de la decisión para el detalle de la aprobación {AprobacionId}",
                        aprobacionId);
                }
            }

            return dto;
        }

        /// <summary>
        /// Une los destinatarios de los correos que dispara una misma decisión en una sola lista
        /// para el aviso del modal. Un buzón configurado en los dos correos aparece una sola vez, y
        /// si está como principal en uno y en copia en otro gana "Para" — el mismo criterio que usa
        /// el resolver dentro de cada correo.
        /// </summary>
        private static SolicitudDestinatariosDto Fusionar(params SolicitudDestinatariosDto[] fuentes)
        {
            var fusion = new SolicitudDestinatariosDto();
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Los principales primero, para que ganen sobre las copias.
            foreach (var d in fuentes.SelectMany(f => f.Para))
                if (vistos.Add(d.Email)) fusion.Para.Add(d);

            foreach (var d in fuentes.SelectMany(f => f.Copias))
                if (vistos.Add(d.Email)) fusion.Copias.Add(d);

            return fusion;
        }

        public async Task<AprobacionGgDecisionResultDto> RegistrarDecision(
            int aprobacionId, AprobacionGgDecisionDto dto, int? userId)
        {
            if (!userId.HasValue)
                throw new AbrilException("No se pudo identificar al usuario.", 401);
            if (dto?.Decisiones == null || dto.Decisiones.Count == 0)
                throw new AbrilException("Debes aprobar o rechazar las vacantes antes de enviar la decisión.", 400);

            // El nivel sale de la categoría del usuario, nunca del payload: así nadie puede pedir
            // que su firma cuente como la de Gerencia General.
            var scope = await _scopes.ResolveAsync(userId);

            var ctx = await _repo.RegistrarDecision(aprobacionId, dto, userId.Value, scope);
            var res = ctx.Resultado;

            if (res.Nivel == AprobacionNivel.GerenteGeneral)
            {
                // Solo las vacantes aprobadas por el GG salen, y solo si hay alguna.
                // Best-effort los tres: la decisión ya quedó registrada.
                //
                // Los dos avisos a GTH se reparten las vacantes aprobadas y no se pisan: el de
                // SOLICITUD lleva las que hay que publicar y reclutar, el de FFT_APROBACION_GG las
                // que ya vienen con candidato. Una solicitud puede traer de las dos.
                if (ctx.Aprobadas.Count > 0)
                {
                    await NotificarAGthAsync(ctx, userId);
                    await NotificarFftAprobadoAGthAsync(ctx, userId);
                    await NotificarATiAsync(ctx);
                }

                // El mensaje habla del correo a GTH y no del de TI a propósito: lo que le importa
                // al gerente es si la solicitud continúa. El aviso a TI es interno del proceso.
                res.Message = res.Aprobados == 0
                    ? "Decisión registrada: rechazaste todas las vacantes. La solicitud no continúa y no se envió a Gestión de Talento Humano."
                    : res.Rechazados == 0
                        ? $"Decisión registrada: aprobaste {res.Aprobados} vacante(s). Ya se enviaron a Gestión de Talento Humano para iniciar el reclutamiento."
                        : $"Decisión registrada: aprobaste {res.Aprobados} vacante(s) y rechazaste {res.Rechazados}. Las aprobadas ya se enviaron a Gestión de Talento Humano.";
            }
            else
            {
                // Ruta de reemplazo, que se firma en DOS tiempos:
                //   • Gerente del área (primero) → lo que aprueba no se mueve todavía, pero le abre
                //     el turno a GTH: sale el correo que le pide su firma, con esas vacantes.
                //   • GTH (segundo) → completa el reemplazo: sale el aviso a GTH con lo aprobado
                //     (para reclutarlo) y el de TI, que necesita la misma anticipación que en la
                //     ruta de Gerencia General — un reemplazo también es alguien que entra.
                //
                // ctx.Aprobadas trae lo que quedó completo con esta decisión y
                // ctx.EsperandoSiguienteFirma lo que quedó aprobado esperando la que sigue, así que
                // cada correo sale con exactamente las vacantes que le corresponden.
                //
                // Best-effort todos, como en la ruta de Gerencia General: la decisión ya quedó
                // registrada y no se revierte porque un correo falle.
                if (ctx.EsperandoSiguienteFirma.Count > 0)
                    await PedirFirmaDeGthAsync(ctx, userId);

                if (ctx.Aprobadas.Count > 0)
                {
                    await NotificarAGthAsync(ctx, userId, tipoCorreo: CorreoTipoReclutamiento.ReemplazoAprobado);
                    await NotificarATiAsync(ctx);
                }

                // Lo que aprobó ESTA decisión puede haber salido ya a GTH (la firma de GTH la
                // completa) o quedar esperándola (la del área la deja en manos de GTH), así que el
                // mensaje se arma con lo que realmente se movió y no con lo que el usuario marcó.
                var salieron  = ctx.Aprobadas.Count;
                var esperando = res.Aprobados - salieron;

                if (res.Aprobados == 0)
                {
                    res.Message = "Decisión registrada: rechazaste todas las vacantes. Ninguna continúa.";
                }
                else
                {
                    var partes = new List<string> { $"Decisión registrada: aprobaste {res.Aprobados} vacante(s)" };
                    if (res.Rechazados > 0) partes.Add($" y rechazaste {res.Rechazados}");
                    partes.Add(". ");
                    partes.Add(salieron > 0
                        ? $"{salieron} ya se enviaron a Gestión de Talento Humano"
                        : string.Empty);
                    if (salieron > 0 && esperando > 0) partes.Add(" y ");
                    partes.Add(esperando > 0
                        ? $"{esperando} pasaron a Gestión de Talento Humano para su aprobación"
                        : string.Empty);
                    partes.Add(".");
                    res.Message = string.Concat(partes);
                }
            }

            return res;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Aprobar (o rechazar) desde la lista es exactamente el mismo acto que abrir el modal de
        /// cada solicitud y marcar todas sus vacantes igual, así que se apoya en las mismas reglas:
        /// el nivel lo resuelve el scope, cada firma dispara los correos de su turno y solo la
        /// última de la ruta manda lo aprobado a GTH y a TI. Lo único propio del bloque es que la
        /// escritura ocurre en un solo lote y que los destinatarios de los correos se resuelven una
        /// vez para todo él.
        /// </remarks>
        public async Task<AprobacionGgDecisionMasivaResultDto> RegistrarDecisionMasiva(
            AprobacionGgDecisionMasivaDto dto, int? userId)
        {
            if (!userId.HasValue)
                throw new AbrilException("No se pudo identificar al usuario.", 401);

            var ids = dto?.AprobacionIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
                throw new AbrilException("Selecciona al menos una solicitud para registrar tu decisión.", 400);

            // El nivel sale de la categoría del usuario, nunca del payload: igual que en la decisión
            // de una sola, nadie puede pedir que su firma cuente como la de Gerencia General.
            var scope = await _scopes.ResolveAsync(userId);

            var ctx = await _repo.RegistrarDecisionMasiva(ids, dto!.Aprobado, dto.Comentario, userId.Value, scope);

            var res = new AprobacionGgDecisionMasivaResultDto
            {
                Nivel       = ctx.Nivel,
                Aprobado    = dto.Aprobado,
                Solicitudes = ctx.Registradas.Count,
                Vacantes    = ctx.Registradas.Sum(c => c.Resultado.Aprobados + c.Resultado.Rechazados),
                Omitidas    = ctx.Omitidas,
            };

            // Los correos salen como en la decisión de una: UNO por solicitud con vacantes que se
            // hayan movido. GTH y TI trabajan por solicitud (cada correo lleva sus códigos de
            // vacante y su justificación), así que fusionarlas en un solo correo cambiaría lo que
            // reciben.
            //
            // Qué sale depende del nivel: la decisión de Gerencia General manda lo aprobado a GTH y
            // a TI; la del gerente del área o la de GTH avisa a GTH solo los reemplazos que ESTA
            // firma dejó completos (los que ya tenían la otra). En las dos, `Aprobadas` es lo que
            // realmente se movió, así que la lista de solicitudes con algo que mandar se calcula
            // igual.
            // El pedido de firma a GTH va aparte de lo anterior: no lo dispara haber completado una
            // vacante sino haberla dejado esperando la segunda firma, que es justo lo contrario.
            // Solo puede pasar en la decisión del gerente del área.
            var conEsperando = ctx.Registradas.Where(c => c.EsperandoSiguienteFirma.Count > 0).ToList();
            if (conEsperando.Count > 0)
            {
                var destFirma = await ResolverDestinatariosDelLote(
                    CorreoTipoReclutamiento.AprobacionReemplazoGth);
                if (destFirma != null)
                    foreach (var c in conEsperando)
                        await PedirFirmaDeGthAsync(c, userId, destFirma);
            }

            var conAprobadas = ctx.Registradas.Where(c => c.Aprobadas.Count > 0).ToList();
            if (conAprobadas.Count > 0)
            {
                if (ctx.Nivel == AprobacionNivel.GerenteGeneral)
                {
                    var destGth = await ResolverDestinatariosDelLote(CorreoTipoReclutamiento.Solicitud);
                    var destTi  = await ResolverDestinatariosDelLote(CorreoTipoReclutamiento.Ti);
                    // Los destinatarios del aviso FFT solo se resuelven si el lote trae alguna
                    // vacante FFT: en un lote sin FFT sería un roundtrip para nada.
                    var destFft = conAprobadas.Any(c => c.Aprobadas.Any(v => v.EsFft))
                        ? await ResolverDestinatariosDelLote(CorreoTipoReclutamiento.FftAprobacionGg)
                        : null;

                    // Best-effort, como en la decisión de una: la decisión ya quedó registrada y no
                    // se revierte porque un correo falle.
                    foreach (var c in conAprobadas)
                    {
                        if (destGth != null) await NotificarAGthAsync(c, userId, destGth);
                        if (destFft != null) await NotificarFftAprobadoAGthAsync(c, userId, destFft);
                        if (destTi  != null) await NotificarATiAsync(c, destTi);
                    }
                }
                else
                {
                    // Un lote de reemplazos no puede traer vacantes FFT: siempre van por la ruta de
                    // Gerencia General. El aviso a TI sí sale, igual que en esa ruta.
                    var destGth = await ResolverDestinatariosDelLote(CorreoTipoReclutamiento.ReemplazoAprobado);
                    var destTi  = await ResolverDestinatariosDelLote(CorreoTipoReclutamiento.Ti);

                    foreach (var c in conAprobadas)
                    {
                        if (destGth != null)
                            await NotificarAGthAsync(
                                c, userId, destGth, CorreoTipoReclutamiento.ReemplazoAprobado);
                        if (destTi != null) await NotificarATiAsync(c, destTi);
                    }
                }
            }

            res.Message = ConstruirMensajeMasivo(res);
            return res;
        }

        /// <summary>
        /// Destinatarios de un correo resueltos una sola vez para todo el lote. Null si no se
        /// pudieron resolver: en ese caso ese correo no sale, pero la decisión ya está registrada.
        /// </summary>
        private async Task<SolicitudDestinatariosDto?> ResolverDestinatariosDelLote(string tipoCodigo)
        {
            try
            {
                return await _destinatarios.ResolverAsync(tipoCodigo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudieron resolver los destinatarios del correo {Tipo} de la decisión en bloque", tipoCodigo);
                return null;
            }
        }

        /// <summary>
        /// Mensaje de la decisión en bloque. Como en la decisión de una, dice si la solicitud avanza
        /// —lo único que el gerente necesita saber—: el Gerente General manda lo aprobado a GTH, el
        /// gerente del área solo deja constancia. Si hubo solicitudes omitidas se dice cuántas, para
        /// que el conteo no quede sin explicar.
        /// </summary>
        private static string ConstruirMensajeMasivo(AprobacionGgDecisionMasivaResultDto res)
        {
            if (res.Solicitudes == 0)
                return "No se registró ninguna decisión: las solicitudes seleccionadas ya no admitían tu decisión.";

            var omitidas = res.Omitidas.Count == 0
                ? string.Empty
                : $" {res.Omitidas.Count} solicitud(es) quedaron fuera porque ya no admitían tu decisión.";

            if (res.Nivel == AprobacionNivel.GerenteGeneral)
            {
                return res.Aprobado
                    ? $"Aprobaste {res.Solicitudes} solicitud(es) ({res.Vacantes} vacante(s)). Ya se enviaron a Gestión de Talento Humano para iniciar el reclutamiento.{omitidas}"
                    : $"Rechazaste {res.Solicitudes} solicitud(es) ({res.Vacantes} vacante(s)). Ninguna continúa y no se enviaron a Gestión de Talento Humano.{omitidas}";
            }

            // El reemplazo se firma en dos tiempos: la del gerente del área manda las vacantes a la
            // bandeja de GTH para que ponga la segunda; la de GTH ya las deja listas para reclutar.
            var siguen = res.Nivel == AprobacionNivel.Gth
                ? "Ya se enviaron a Gestión de Talento Humano para iniciar el reclutamiento."
                : "Pasaron a Gestión de Talento Humano para su aprobación.";
            return res.Aprobado
                ? $"Aprobaste {res.Solicitudes} solicitud(es) ({res.Vacantes} vacante(s)). {siguen}{omitidas}"
                : $"Rechazaste {res.Solicitudes} solicitud(es) ({res.Vacantes} vacante(s)). Ninguna continúa.{omitidas}";
        }

        /// <summary>
        /// Correo + campanita a GTH con las vacantes que quedaron aprobadas en esta decisión: las
        /// que hay que publicar y reclutar. Es el correo de "nueva solicitud de personal" que antes
        /// salía al registrar la solicitud — ahora espera las firmas y solo lleva lo aprobado. No
        /// bloquea.
        /// </summary>
        /// <param name="tipoCorreo">
        /// Cuál de los dos avisos a GTH es, que es lo único que cambia entre las dos rutas: el
        /// trabajo del otro lado es el mismo (reclutar), pero lo dispara otra decisión y cada uno
        /// tiene su propia configuración de destinatarios.
        /// <list type="bullet">
        ///   <item><see cref="CorreoTipoReclutamiento.Solicitud"/> — lo aprobado por Gerencia General.</item>
        ///   <item><see cref="CorreoTipoReclutamiento.ReemplazoAprobado"/> — los reemplazos que
        ///   juntaron las firmas del gerente del área y de GTH.</item>
        /// </list>
        /// </param>
        private async Task NotificarAGthAsync(
            AprobacionGgDecisionContextoDto ctx, int? userId, SolicitudDestinatariosDto? destinatarios = null,
            string tipoCorreo = CorreoTipoReclutamiento.Solicitud)
        {
            // Las vacantes FFT tienen su propio correo (NotificarFftAprobadoAGthAsync): este es el
            // de las que sí hay que publicar y reclutar. Una solicitud puede traer solo FFT, y
            // entonces acá no queda nada que mandar. (Por la ruta del reemplazo nunca llega una:
            // un FFT siempre lo aprueba Gerencia General.)
            var vacantes = ctx.Aprobadas.Where(v => !v.EsFft).ToList();
            if (vacantes.Count == 0) return;

            var esReemplazo = tipoCorreo == CorreoTipoReclutamiento.ReemplazoAprobado;

            // En la decisión de UNA solicitud los destinatarios se resuelven acá. En la decisión en
            // bloque llegan ya resueltos, una sola vez para todo el lote: no dependen de la
            // solicitud ni de su área, así que repetir la consulta por cada una sería un roundtrip
            // por solicitud sin ninguna diferencia en el resultado.
            var dest = destinatarios;
            if (dest == null)
            {
                try
                {
                    dest = await _destinatarios.ResolverAsync(tipoCorreo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudieron resolver los destinatarios del correo {Tipo} a GTH de la solicitud {SolicitudId}",
                        tipoCorreo, ctx.SolicitudId);
                    return;
                }
            }

            // 1) Correo.
            try
            {
                if (dest.Para.Count > 0) // sin destinatario principal → no se envía
                {
                    var titulo = esReemplazo ? "Reemplazo aprobado" : "Nueva solicitud de personal aprobada";
                    var subject = vacantes.Count == 1
                        ? $"[Reclutamiento] {titulo} — {vacantes[0].Codigo}"
                        : $"[Reclutamiento] {(esReemplazo ? "Reemplazos aprobados" : titulo)} — {vacantes.Count} vacantes";

                    await _email.SendAsync(
                        to:      dest.EmailsPara,
                        subject: subject,
                        body:    ConstruirCuerpoGth(ctx, vacantes, esReemplazo),
                        isHtml:  true,
                        cc:      dest.Copias.Count > 0 ? dest.EmailsCopias : null);
                }
                else
                {
                    // También entra acá cuando el correo está apagado con su interruptor maestro:
                    // en ese caso es una decisión de la Configuración, no una falla.
                    _logger.LogWarning(
                        "No hay destinatarios principales activos para el correo {Tipo} a GTH (solicitud {SolicitudId}); no se envía.",
                        tipoCorreo, ctx.SolicitudId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar el correo de la solicitud de personal {SolicitudId} a GTH", ctx.SolicitudId);
            }

            // 2) Notificación in-app (campanita) — una por vacante aprobada, mismos destinatarios.
            try
            {
                var items = vacantes.Select(v => new NuevaNotificacionDto
                {
                    Titulo      = "Nuevo requerimiento de personal",
                    Subtitulo   = string.IsNullOrWhiteSpace(ctx.Area) ? v.Puesto : $"{v.Puesto} — {ctx.Area}",
                    Descripcion = ctx.Justificacion,
                    Referencia  = v.Codigo,
                }).ToList();

                await _notificaciones.CrearPorCorreosAsync(
                    NotificacionTipoCodigo.GthSolicitudPersonal,
                    dest.EmailsPara.Concat(dest.EmailsCopias).ToList(),
                    userId, // quien aprobó desde «Aprobaciones»
                    items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo crear la notificación in-app de la solicitud de personal {SolicitudId}", ctx.SolicitudId);
            }
        }

        /// <summary>
        /// Correo + campanita a GTH pidiéndole SU firma sobre los reemplazos que el gerente del
        /// área acaba de aprobar (tipo APROBACION_REEMPLAZO_GTH). Es el segundo tiempo de la ruta
        /// <see cref="RutaAprobacion.AreaYGth"/>: hasta esta decisión GTH no sabía nada de estas
        /// vacantes —no le aparecían en «Aprobaciones» ni le había llegado ningún correo—, así que
        /// este es el que le abre el turno.
        ///
        /// No confundirlo con <see cref="NotificarAGthAsync"/> con tipo REEMPLAZO_APROBADO, que
        /// sale después y avisa que la vacante ya juntó las dos firmas: este pide la segunda, ese
        /// anuncia que hay que reclutar. Van a la misma área pero piden cosas distintas, y por eso
        /// tienen configuración de destinatarios propia cada uno.
        ///
        /// No bloquea ni lanza: la decisión del gerente ya quedó registrada y la vacante ya está en
        /// la bandeja de GTH aunque el correo falle.
        /// </summary>
        private async Task PedirFirmaDeGthAsync(
            AprobacionGgDecisionContextoDto ctx, int? userId, SolicitudDestinatariosDto? destinatarios = null)
        {
            var vacantes = ctx.EsperandoSiguienteFirma;
            if (vacantes.Count == 0) return;

            SolicitudDestinatariosDto dest;
            try
            {
                dest = destinatarios
                    ?? await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.AprobacionReemplazoGth);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudieron resolver los destinatarios del pedido de firma a GTH de la solicitud {SolicitudId}",
                    ctx.SolicitudId);
                return;
            }

            if (dest.Para.Count == 0)
            {
                // También entra acá cuando el correo está apagado con su interruptor maestro: en
                // ese caso es una decisión de la Configuración, no una falla. Las vacantes ya
                // aparecen en «Aprobaciones» de GTH igual.
                _logger.LogWarning(
                    "No hay destinatarios principales activos para el pedido de firma a GTH " +
                    "(solicitud {SolicitudId}); no se envía.", ctx.SolicitudId);
                return;
            }

            try
            {
                var subject = vacantes.Count == 1
                    ? $"[Reclutamiento] Aprobación de reemplazo — {vacantes[0].Codigo}"
                    : $"[Reclutamiento] Aprobación de {vacantes.Count} reemplazos — {ctx.Area}";

                await _email.SendAsync(
                    to:      dest.EmailsPara,
                    subject: subject,
                    body:    ConstruirCuerpoFirmaGth(ctx, vacantes),
                    isHtml:  true,
                    cc:      dest.Copias.Count > 0 ? dest.EmailsCopias : null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo enviar el pedido de firma a GTH de la solicitud {SolicitudId}", ctx.SolicitudId);
            }

            // Campanita: es un pendiente igual que el del primer correo de aprobación, así que usa
            // el mismo tipo de notificación y una sola entrada con el resumen.
            try
            {
                var resumen = vacantes.Count == 1 ? vacantes[0].Puesto : $"{vacantes.Count} reemplazos";
                await _notificaciones.CrearPorCorreosAsync(
                    NotificacionTipoCodigo.GthAprobacionGg,
                    dest.EmailsPara.Concat(dest.EmailsCopias).Distinct().ToList(),
                    userId,
                    new List<NuevaNotificacionDto>
                    {
                        new()
                        {
                            Titulo      = "Reemplazo por aprobar",
                            Subtitulo   = string.IsNullOrWhiteSpace(ctx.Area) ? resumen : $"{resumen} — {ctx.Area}",
                            Descripcion = ctx.Justificacion,
                            Referencia  = string.Join(", ", vacantes.Select(v => v.Codigo)),
                        },
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo crear la notificación in-app del pedido de firma a GTH (solicitud {SolicitudId})",
                    ctx.SolicitudId);
            }
        }

        /// <summary>
        /// Cuerpo del pedido de firma a GTH: los reemplazos que el gerente del área aprobó, con su
        /// visto bueno a la vista (es lo que abre el turno) y el acceso a «Aprobaciones», donde GTH
        /// pone la segunda firma. La solicitud completa no viaja: los reemplazos que el área
        /// rechazó ya no continúan y no hay nada que GTH decida sobre ellos.
        /// </summary>
        private string ConstruirCuerpoFirmaGth(
            AprobacionGgDecisionContextoDto ctx, IReadOnlyList<AprobacionGgVacanteDto> vacantes)
        {
            var l    = Layout.Desde(_configuration);
            var link = ConstruirLink(ctx.AprobacionId);

            var datos = new List<Layout.Fila>
            {
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
            };
            if (!string.IsNullOrWhiteSpace(ctx.SolicitanteNombre))
                datos.Add(new("req-solicitante", "Solicitante", Layout.Esc(ctx.SolicitanteNombre)));

            var contexto = new List<Layout.Fila>();
            if (!string.IsNullOrWhiteSpace(ctx.Justificacion))
                contexto.Add(new("req-justificacion", "Justificación", Layout.EscMultilinea(ctx.Justificacion)));
            if (!string.IsNullOrWhiteSpace(ctx.Comentario))
                contexto.Add(new("req-comentario", "Comentario del área", Layout.EscMultilinea(ctx.Comentario)));
            if (!string.IsNullOrWhiteSpace(ctx.GerenteAreaResumen))
                contexto.Add(new("req-vistobueno", "Visto bueno del área", Layout.Esc(ctx.GerenteAreaResumen)));
            if (!string.IsNullOrWhiteSpace(ctx.SustentoUrl))
                contexto.Add(new("req-sustento", "Sustento adjunto",
                    Textos.Enlace(ctx.SustentoUrl!, ctx.SustentoNombre ?? "Ver documento")));

            var uno        = vacantes.Count == 1;
            var conSalario = ConSalario(vacantes);

            return l.Documento(
                new Layout.Cabecera(
                    "req-solicitud", "Aprobación de Reemplazo",
                    $"El gerente del área aprobó {(uno ? "este reemplazo" : "estos reemplazos")} y espera tu firma:"),
                l.Tarjeta(datos),
                l.Seccion("req-vacantes", $"Vacantes por aprobar ({vacantes.Count})"),
                l.Tabla(
                    conSalario ? Textos.ColumnasVacantesConSalario : Textos.ColumnasVacantes,
                    FilasVacantes(vacantes, conSalario)),
                l.Tarjeta(contexto),
                l.Boton("Revisar y aprobar", link),
                l.EnlaceDirecto(link));
        }

        /// <summary>
        /// Correo + campanita a GTH con las vacantes <b>FFT</b> que Gerencia General aprobó (tipo
        /// FFT_APROBACION_GG). Es la contraparte de <see cref="NotificarAGthAsync"/> para el ingreso
        /// directo: mismo momento y mismo destinatario, pero otro trabajo del otro lado — no hay
        /// vacante que publicar, hay un candidato al que mandarle el formulario. No bloquea.
        /// </summary>
        private async Task NotificarFftAprobadoAGthAsync(
            AprobacionGgDecisionContextoDto ctx, int? userId, SolicitudDestinatariosDto? destinatarios = null)
        {
            var vacantes = ctx.Aprobadas.Where(v => v.EsFft).ToList();
            if (vacantes.Count == 0) return;

            SolicitudDestinatariosDto dest;
            try
            {
                dest = destinatarios ?? await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.FftAprobacionGg);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudieron resolver los destinatarios del aviso FFT a GTH de la solicitud {SolicitudId}",
                    ctx.SolicitudId);
                return;
            }

            if (dest.Para.Count == 0)
            {
                // También entra acá cuando el correo está apagado con su interruptor maestro.
                _logger.LogWarning(
                    "No hay destinatarios principales activos para el aviso FFT a GTH " +
                    "(solicitud {SolicitudId}); no se envía.", ctx.SolicitudId);
                return;
            }

            // Un correo por vacante, igual que el del ingreso directo recién registrado: el botón
            // tiene que llevar al detalle de SU requerimiento.
            foreach (var vacante in vacantes)
            {
                await EnviarFftPorVacanteAsync(
                    vacante, dest, ctx.Area, ctx.SolicitanteNombre, ctx.Justificacion,
                    ctx.SustentoUrl, ctx.SustentoNombre, esPedido: false);
            }

            await CrearNotificacionFftAsync(vacantes, ctx.Area, ctx.Justificacion, dest, userId);
        }

        /// <summary>
        /// Correo a TI con las vacantes que la decisión dejó aprobadas (tipo TI_VACANTES). Sale por
        /// las dos rutas —con la firma de Gerencia General, y con la segunda firma de un reemplazo—
        /// porque lo que le importa a TI es que alguien va a entrar, no quién lo autorizó. Va en la
        /// misma decisión que el de GTH pero es otro correo y otra configuración: TI no participa
        /// del reclutamiento, lo que necesita es la anticipación para alistar equipo, usuario y
        /// accesos de cada ingreso.
        ///
        /// El buzón no está cableado acá: sale del destinatario dinámico TI_AREA, que lee
        /// <c>area_scope.email</c> del área de Tecnología de la Información. No bloquea ni lanza —
        /// la decisión ya quedó registrada y no se revierte porque un correo falle.
        /// </summary>
        private async Task NotificarATiAsync(
            AprobacionGgDecisionContextoDto ctx, SolicitudDestinatariosDto? destinatarios = null)
        {
            try
            {
                // Igual que en el correo a GTH: en bloque llegan ya resueltos para todo el lote.
                var dest = destinatarios ?? await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.Ti);
                if (dest.Para.Count == 0)
                {
                    // También entra acá cuando el correo está apagado con su interruptor maestro:
                    // en ese caso es una decisión de la Configuración, no una falla.
                    _logger.LogWarning(
                        "No hay destinatarios principales activos para el correo de vacantes aprobadas a TI " +
                        "(solicitud {SolicitudId}); no se envía.",
                        ctx.SolicitudId);
                    return;
                }

                var subject = ctx.Aprobadas.Count == 1
                    ? $"[Reclutamiento] Vacante aprobada — {ctx.Aprobadas[0].Codigo}"
                    : $"[Reclutamiento] {ctx.Aprobadas.Count} vacantes aprobadas — {ctx.Area}";

                await _email.SendAsync(
                    to:      dest.EmailsPara,
                    subject: subject,
                    body:    ConstruirCuerpoTi(ctx),
                    isHtml:  true,
                    cc:      dest.Copias.Count > 0 ? dest.EmailsCopias : null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo enviar el correo de vacantes aprobadas a TI de la solicitud {SolicitudId}",
                    ctx.SolicitudId);
            }
        }

        /// <summary>
        /// Filas de la tabla de vacantes aprobadas. La línea "Reemplaza a {trabajador}" va bajo el
        /// tipo, en la misma celda, para no agregar una columna a una tabla que ya se lee bien.
        /// </summary>
        /// <param name="conSalario">
        /// false en el correo a TI: no participa del reclutamiento ni arma la oferta, así que la
        /// banda salarial no es asunto suyo y su tabla tiene una columna menos.
        /// </param>
        private static List<IReadOnlyList<Layout.Celda>> FilasVacantes(
            IReadOnlyList<AprobacionGgVacanteDto> vacantes, bool conSalario)
        {
            var filas = new List<IReadOnlyList<Layout.Celda>>(vacantes.Count);
            foreach (var v in vacantes)
            {
                var celdas = new List<Layout.Celda>
                {
                    new(Layout.Esc(v.Codigo), Negrita: true, Color: Layout.Azul, NoWrap: true),
                    new(Layout.Esc(v.Puesto)),
                    new(Layout.Esc(v.TipoRequerimiento) + Textos.Reemplaza(v.TrabajadorReemplazado)),
                    new(Textos.OGuion(v.ProyectoObra)),
                };
                if (conSalario) celdas.Add(new(Textos.OGuion(SalarioTexto(v)), Negrita: true, NoWrap: true));
                filas.Add(celdas);
            }
            return filas;
        }

        /// <summary>
        /// Cuerpo del correo a TI: las vacantes aprobadas, para que puedan ir previendo lo que
        /// necesitará cada puesto. No lleva justificación, comentario ni las vacantes rechazadas:
        /// eso es contexto de la decisión y del reclutamiento, no del trabajo de TI. La fecha de
        /// ingreso no se conoce en este punto — la confirma GTH al cerrar el proceso, y eso ya no
        /// se dice en el correo: era una nota al pie que nadie accionaba.
        /// </summary>
        private string ConstruirCuerpoTi(AprobacionGgDecisionContextoDto ctx)
        {
            var l = Layout.Desde(_configuration);

            var datos = new List<Layout.Fila>
            {
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
            };
            if (!string.IsNullOrWhiteSpace(ctx.SolicitanteNombre))
                datos.Add(new("req-solicitante", "Solicitante", Layout.Esc(ctx.SolicitanteNombre)));

            // Quién aprobó cambia con la ruta de la vacante, y el correo puede traer las dos: se
            // dice "Se aprobaron" en vez de atribuirlo a quien no fue.
            return l.Documento(
                new Layout.Cabecera(
                    "req-ti", "Vacantes Aprobadas", "Se aprobaron estas vacantes:"),
                l.Tarjeta(datos),
                l.Seccion("req-vacantes", $"Vacantes aprobadas ({ctx.Aprobadas.Count})"),
                l.Tabla(Textos.ColumnasVacantes, FilasVacantes(ctx.Aprobadas, conSalario: false)));
        }

        /// <summary>
        /// Cuerpo de los dos correos a GTH con vacantes aprobadas: el de Gerencia General y el de
        /// los reemplazos que juntaron las dos firmas. Son el mismo correo con otra bajada —lo que
        /// cambia es de dónde viene la aprobación, no lo que GTH tiene que hacer con ella— así que
        /// comparten cuerpo en vez de duplicarse.
        ///
        /// Lleva las vacantes aprobadas, el contexto de la decisión (justificación, comentario de
        /// quien decidió, visto bueno del gerente del área y sustento) y el botón que lleva a
        /// «Reclutamiento» a trabajarlas. Lo que no se aprobó no se menciona: no genera trabajo para
        /// GTH y el conteo del asunto ya se refiere solo a lo aprobado.
        /// </summary>
        /// <param name="vacantes">
        /// Las aprobadas que le toca reclutar a GTH: las FFT quedan fuera porque van en su propio
        /// correo, con su propio cuerpo y su propio siguiente paso.
        /// </param>
        /// <param name="esReemplazo">
        /// true = el correo de la ruta del gerente del área + GTH. Cambia la bajada y la etiqueta
        /// del comentario: esa decisión no la firmó Gerencia General.
        /// </param>
        private string ConstruirCuerpoGth(
            AprobacionGgDecisionContextoDto ctx, IReadOnlyList<AprobacionGgVacanteDto> vacantes,
            bool esReemplazo = false)
        {
            var l    = Layout.Desde(_configuration);
            var link = ConstruirLinkReclutamiento(vacantes);

            var datos = new List<Layout.Fila>
            {
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
            };
            if (!string.IsNullOrWhiteSpace(ctx.SolicitanteNombre))
                datos.Add(new("req-solicitante", "Solicitante", Layout.Esc(ctx.SolicitanteNombre)));

            // El visto bueno del gerente del área no condiciona nada en la ruta de Gerencia General,
            // pero saber que opinó (y qué opinó) le da contexto a GTH. En la del reemplazo es una de
            // las dos firmas que movieron la vacante, así que ahí vale todavía más. Vacío si nunca
            // llegó a registrarlo.
            var contexto = new List<Layout.Fila>();
            if (!string.IsNullOrWhiteSpace(ctx.Justificacion))
                contexto.Add(new("req-justificacion", "Justificación", Layout.EscMultilinea(ctx.Justificacion)));
            if (!string.IsNullOrWhiteSpace(ctx.Comentario))
                contexto.Add(new("req-comentario", esReemplazo ? "Comentario" : "Comentario de GG",
                    Layout.EscMultilinea(ctx.Comentario)));
            if (!string.IsNullOrWhiteSpace(ctx.GerenteAreaResumen))
                contexto.Add(new("req-vistobueno", "Visto bueno del área", Layout.Esc(ctx.GerenteAreaResumen)));
            if (!string.IsNullOrWhiteSpace(ctx.SustentoUrl))
                contexto.Add(new("req-sustento", "Sustento adjunto",
                    Textos.Enlace(ctx.SustentoUrl!, ctx.SustentoNombre ?? "Ver documento")));

            var uno = vacantes.Count == 1;
            var conSalario = ConSalario(vacantes);

            return l.Documento(
                new Layout.Cabecera(
                    "req-aprobada",
                    esReemplazo ? "Vacantes Aprobadas" : "Solicitud Aprobada",
                    esReemplazo
                        ? $"El gerente del área y GTH aprobaron {(uno ? "este reemplazo" : "estos reemplazos")}:"
                        : "Gerencia General aprobó estas vacantes:"),
                l.Tarjeta(datos),
                l.Seccion("req-vacantes", $"Vacantes aprobadas ({vacantes.Count})"),
                l.Tabla(
                    conSalario ? Textos.ColumnasVacantesConSalario : Textos.ColumnasVacantes,
                    FilasVacantes(vacantes, conSalario)),
                l.Tarjeta(contexto),
                // «Iniciar el reclutamiento» y no «Publicar la vacante»: al aprobar, la vacante
                // queda en VALIDACION_GTH, así que lo primero que abre el modal es la asignación
                // interna (responsable, SLA, prioridad, razón social) y la publicación viene
                // después. El texto tampoco cambia entre una vacante y varias — nombra el paso, que
                // es el mismo, y el enlace es el que se acomoda.
                l.Boton("Iniciar el reclutamiento", link),
                l.EnlaceDirecto(link));
        }
    }
}
