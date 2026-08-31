using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.Email.Configuration;
using Abril_Backend.Shared.Services.SharePoint.Dtos;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Layout = Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared.ReclutamientoEmailLayout;
using Textos = Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared.ReclutamientoEmailTextos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Services
{
    public class ReclutamientoService : IReclutamientoService
    {
        private readonly IReclutamientoRepository _repo;
        private readonly ISolicitudPersonalScopeResolver _scopes;
        private readonly IAprobacionGgRepository  _aprobacionGgRepo;
        private readonly IAprobacionGgService     _aprobacionGg;
        private readonly ICorreoDestinatariosResolver _destinatarios;
        private readonly ICorreoConfigRepository  _correoConfig;
        private readonly IGraphSharePointService  _sharePoint;
        private readonly IReclutamientoArchivoStorage _archivos;
        private readonly IEmailService            _email;
        private readonly IConfiguration           _configuration;
        private readonly ILogger<ReclutamientoService> _logger;

        private const long MaxSustentoBytes = 10 * 1024 * 1024; // 10 MB
        private static readonly string[] AllowedSustentoExt = { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };

        /// <summary>
        /// Tope de la justificación general. La columna <c>gth_solicitud.justificacion</c> es text
        /// (sin límite), pero el texto va completo en el cuerpo de los correos a los gerentes y a
        /// GTH: el corte evita que un pegado accidental se convierta en un correo ilegible.
        /// </summary>
        private const int MaxJustificacionLength = 4000;

        /// <summary>
        /// Tope del salario bruto mensual declarado por vacante. Es el que aguanta la columna
        /// (<c>numeric(12,2)</c>) sin desbordar y, sobre todo, ataja el dedazo típico de escribir
        /// el sueldo con los céntimos pegados (3500 00 → 350000).
        /// </summary>
        private const decimal MaxSalarioBrutoMensual = 1_000_000m;

        /// <summary>
        /// Tope del nombre del candidato FFT. No hay columna que lo limite (es <c>text</c>), pero el
        /// nombre va en el asunto y en las tablas de los correos: el corte evita que un pegado
        /// accidental los deje ilegibles.
        /// </summary>
        private const int MaxFftNombreLength = 200;

        /// <summary>
        /// Correo válido para el candidato FFT. Misma expresión que valida el envío del formulario
        /// (<c>PostulanteFormularioService.EmailRegex</c>): es el mismo buzón, así que lo que se
        /// acepta acá tiene que ser exactamente lo que después se pueda usar para escribirle.
        /// </summary>
        private static readonly Regex FftCorreoRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        // El largo del documento del candidato FFT ya no es una expresión fija acá: lo decide su
        // tipo (DNI 8 exactos, CE de 8 a 12) y esa regla vive en FftDocumento, que la comparten
        // esta validación y el formulario. Tiene que ser la misma que la del formulario del
        // postulante porque los dos terminan en la misma columna (person.document_identity_code,
        // que tiene UNIQUE): con un formato más suelto de un lado, el mismo documento entraría dos
        // veces escrito distinto y quedarían dos personas donde hay una.

        public ReclutamientoService(
            IReclutamientoRepository repo,
            ISolicitudPersonalScopeResolver scopes,
            IAprobacionGgRepository aprobacionGgRepo,
            IAprobacionGgService aprobacionGg,
            ICorreoDestinatariosResolver destinatarios,
            ICorreoConfigRepository correoConfig,
            IGraphSharePointService sharePoint,
            IReclutamientoArchivoStorage archivos,
            IEmailService email,
            IConfiguration configuration,
            ILogger<ReclutamientoService> logger)
        {
            _repo             = repo;
            _scopes           = scopes;
            _aprobacionGgRepo = aprobacionGgRepo;
            _aprobacionGg     = aprobacionGg;
            _destinatarios    = destinatarios;
            _correoConfig     = correoConfig;
            _sharePoint       = sharePoint;
            _archivos         = archivos;
            _email            = email;
            _configuration    = configuration;
            _logger           = logger;
        }

        public async Task<ReclutamientoFormDataDto> GetFormData(int? userId)
        {
            var dto = await _repo.GetFormData(userId);

            // Aviso "a quién le llegará esta solicitud" del modal. Va en la misma petición que los
            // catálogos (una sola llamada al abrir el formulario) y sale del mismo resolver que usa
            // el envío, así que lo que se muestra es exactamente lo que se va a enviar.
            dto.Destinatarios = await _destinatarios.ResolverAsync(
                CorreoTipoReclutamiento.AprobacionGg, dto.AreaScopeId);

            // Una vacante de ingreso directo no la aprueba nadie: su aviso va derecho a GTH y con
            // otros destinatarios. Se resuelve siempre —cualquiera puede marcar la casilla FFT— para
            // que el formulario pueda decir a quién le llegará cada cosa en cuanto la marquen.
            dto.DestinatariosFft = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.FftSolicitudGg);

            return dto;
        }

        public async Task<SolicitantePanelDto> GetSolicitantePanel(int? userId) =>
            userId.HasValue
                ? await _repo.GetSolicitantePanel(await _scopes.ResolveAsync(userId.Value))
                : new SolicitantePanelDto();

        /// <summary>
        /// Alcance del usuario en la pantalla del solicitante. <paramref name="paraGestionar"/> =
        /// true en las acciones que mueven el requerimiento (registrar, decidir, reenviar): esas
        /// son de la jefatura del area, asi que se cortan aca con un 403 y un mensaje que dice por
        /// que. Las lecturas pasan con cualquier categoria: el requerimiento es del area y su gente
        /// tiene que poder seguirlo.
        /// </summary>
        private async Task<SolicitudPersonalScope> ResolverScope(int? userId, bool paraGestionar)
        {
            if (!userId.HasValue)
                throw new AbrilException("No se pudo identificar al usuario.", 401);

            var scope = await _scopes.ResolveAsync(userId.Value);
            if (paraGestionar && !scope.PuedeGestionar)
                throw new AbrilException(
                    "Solo las jefaturas y gerencias del area pueden avanzar los requerimientos. "
                    + "Puedes revisarlos y hacerles seguimiento.", 403);

            return scope;
        }

        public async Task<RevisionLongListDto> GetRevisionLongList(int requerimientoId, int? userId)
        {
            var scope = await ResolverScope(userId, paraGestionar: false);

            var revision = await _repo.GetRevisionLongList(requerimientoId, scope);
            if (revision == null)
                throw new AbrilException("No se encontró la long list del requerimiento.", 404);

            return revision;
        }

        public async Task<LongListDecisionResultDto> RegistrarDecisionLongList(
            int requerimientoId, LongListDecisionDto dto, int? userId)
        {
            var scope = await ResolverScope(userId, paraGestionar: true);
            if (dto?.Decisiones == null || dto.Decisiones.Count == 0)
                throw new AbrilException("Debes aprobar o rechazar a los candidatos antes de enviar la decisión.", 400);

            // 1) Persistir la decisión y avanzar el requerimiento (LONG_LIST_APROBADA o vuelta a LONG_LIST).
            var ctx = await _repo.RegistrarDecisionLongList(requerimientoId, dto.Decisiones, scope);

            // 2) Notificar a GTH por correo (tipo LONG_LIST_DECISION). Best-effort: la decisión ya quedó
            //    registrada; si el correo falla solo se registra el warning (no se revierte el estado).
            await NotificarDecisionAGthAsync(requerimientoId, ctx);

            return ctx.Resultado;
        }

        /// <summary>
        /// Envía a GTH el correo con la decisión del solicitante sobre la long list (tipo
        /// LONG_LIST_DECISION). To = principales configurados, CC = copias. Sin principales no se
        /// envía. No bloquea: cualquier fallo solo se registra como warning.
        /// </summary>
        private async Task NotificarDecisionAGthAsync(int requerimientoId, LongListDecisionContextoDto ctx)
        {
            try
            {
                var dest = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.LongListDecision);
                if (dest.Para.Count == 0)
                {
                    _logger.LogWarning(
                        "No hay destinatarios principales activos para el correo de decisión de long list ({Codigo}); no se envía.",
                        ctx.Codigo);
                    return;
                }

                var subject = $"[Reclutamiento] Decisión de long list — {ctx.Codigo} · {ctx.Puesto}";

                await _email.SendAsync(
                    to:      dest.EmailsPara,
                    subject: subject,
                    body:    ConstruirCuerpoDecision(requerimientoId, ctx),
                    isHtml:  true,
                    cc:      dest.Copias.Count > 0 ? dest.EmailsCopias : null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar el correo de decisión de long list del requerimiento {Codigo}", ctx.Codigo);
            }
        }

        /// <summary>
        /// Enlace al requerimiento dentro de la bandeja de GTH («Reclutamiento»). Abre el modal de
        /// detalle, que ya se acomoda solo a la fase en la que quedó el requerimiento tras la
        /// decisión: con candidatos aprobados muestra el envío del formulario del postulante, y si
        /// el solicitante rechazó a todos, la carga de una nueva long list. Sin sesión, el
        /// <c>authGuard</c> del frontend manda al login con esta URL como <c>returnUrl</c>.
        /// </summary>
        private string ConstruirLinkDetalleRequerimiento(int requerimientoId)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/gestion-gth/reclutamiento/requerimiento/{requerimientoId}";
        }

        /// <summary>
        /// Cuerpo del correo a GTH con la decisión del solicitante sobre la long list. El botón
        /// lleva siempre al detalle del requerimiento, pero se nombra por la acción que a GTH le
        /// toca hacer allí, que depende del resultado.
        /// </summary>
        private string ConstruirCuerpoDecision(int requerimientoId, LongListDecisionContextoDto ctx)
        {
            var l    = Layout.Desde(_configuration);
            var link = ConstruirLinkDetalleRequerimiento(requerimientoId);
            var r    = ctx.Resultado;

            var datos = new List<Layout.Fila>
            {
                new("req-codigo", "Requerimiento", Layout.Esc(ctx.Codigo)),
                new("req-puesto", "Puesto", Textos.OGuion(ctx.Puesto)),
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
                new("req-proyecto", "Proyecto / Obra", Textos.OGuion(ctx.ProyectoObra)),
            };
            if (!string.IsNullOrWhiteSpace(ctx.SolicitanteNombre))
                datos.Add(new("req-solicitante", "Solicitante", Layout.Esc(ctx.SolicitanteNombre)));

            var filas = new List<IReadOnlyList<Layout.Celda>>(ctx.Candidatos.Count);
            for (var i = 0; i < ctx.Candidatos.Count; i++)
            {
                var c = ctx.Candidatos[i];
                filas.Add(new List<Layout.Celda>
                {
                    new((i + 1).ToString()),
                    new(string.IsNullOrWhiteSpace(c.Nombre) ? $"Candidato {i + 1}" : Layout.Esc(c.Nombre), Negrita: true),
                    new(Textos.OGuion(c.Puesto)),
                    new(c.Aprobado ? "Aprobado" : "Rechazado", Negrita: true,
                        Color: c.Aprobado ? Layout.VerdeOk : Layout.RojoNo),
                });
            }

            return l.Documento(
                new Layout.Cabecera(
                    "req-decision", "Decisión de Long List", "Decisión del solicitante sobre la long list:"),
                l.Tarjeta(datos),
                r.TodosRechazados
                    ? l.Franja("req-rechazadas", Layout.Tono.Rojo,
                        "<b>Rechazó a todos los candidatos.</b> El requerimiento vuelve a la etapa de long list.")
                    : l.Franja("req-check", Layout.Tono.Verde,
                        $"<b>Aprobó {r.Aprobados} candidato(s)</b> de {ctx.Candidatos.Count}."),
                l.Seccion("req-candidatos", $"Candidatos revisados ({ctx.Candidatos.Count})"),
                l.Tabla(ColumnasDecisionCandidatos, filas),
                l.Boton(r.TodosRechazados ? "Preparar nueva long list" : "Continuar el proceso", link),
                l.EnlaceDirecto(link));
        }

        /// <summary>Columnas de la tabla de candidatos decididos (suman los 580px de la tarjeta).</summary>
        private static readonly IReadOnlyList<Layout.Columna> ColumnasDecisionCandidatos = new List<Layout.Columna>
        {
            new("#", 46, Layout.Alineacion.Centro),
            new("Candidato", 200),
            new("Puesto", 190),
            new("Decisión", 144, Layout.Alineacion.Centro),
        };

        public Task<BandejaReclutamientoDto> GetBandeja() => _repo.GetBandeja();

        public Task UpdatePrioridad(int requerimientoId, int prioridadId, int? userId) =>
            _repo.UpdatePrioridad(requerimientoId, prioridadId, userId);

        public async Task<DetalleRequerimientoGthDto> GetDetalleGth(int requerimientoId)
        {
            var detalle = await _repo.GetDetalleGth(requerimientoId);
            if (detalle == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);
            return detalle;
        }

        public Task UpdateAsignacionGth(int requerimientoId, AsignacionGthUpdateDto dto, int? userId)
        {
            if (dto == null)
                throw new AbrilException("Datos de la asignación no recibidos.", 400);
            return _repo.UpdateAsignacionGth(requerimientoId, dto, userId);
        }

        public Task<EstadoRequerimientoResultDto> ReplacePublicaciones(int requerimientoId, PublicacionesUpdateDto dto, int? userId)
        {
            // Publicar avanza el pipeline: se exige al menos un canal (ya no hay flujo de "despublicar").
            if (dto?.CanalIds == null || dto.CanalIds.Count == 0)
                throw new AbrilException("Selecciona al menos un canal de publicación.", 400);
            return _repo.ReplacePublicaciones(requerimientoId, dto.CanalIds, userId);
        }

        public Task<EstadoRequerimientoResultDto> IniciarRevisionCv(int requerimientoId, int? userId) =>
            _repo.IniciarRevisionCv(requerimientoId, userId);

        // ── Multitest y programación de entrevistas ───────────────────────────
        public Task SetMultitest(int candidatoId, MultitestUpdateDto dto, int? userId) =>
            _repo.SetMultitest(candidatoId, dto?.Realizado ?? false, userId);

        public Task<EstadoRequerimientoResultDto> ContinuarAEntrevistas(int requerimientoId, int? userId) =>
            _repo.ContinuarAEntrevistas(requerimientoId, userId);

        public async Task<RetomarCandidatoResultDto> RetomarCandidatoRechazado(
            int requerimientoId, int candidatoId, int? userId)
        {
            var ctx = await _repo.RetomarCandidatoRechazado(requerimientoId, candidatoId, userId);

            // Aviso al solicitante de con quién sigue el proceso. Best-effort: el candidato ya
            // quedó retomado y el requerimiento ya cambió de fase, así que un fallo del correo no
            // puede tumbar la operación ni mostrarle un error a GTH — lo ve igual en su pantalla.
            try
            {
                await NotificarCandidatoRetomadoAlSolicitanteAsync(ctx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo avisarle al solicitante que se retomó a un candidato del requerimiento {Codigo}",
                    ctx.Codigo);
            }

            return ctx.Resultado;
        }

        /// <summary>
        /// Le avisa al área solicitante que GTH retomó el proceso con un candidato del historial
        /// (tipo CANDIDATO_RETOMADO). El destinatario principal es SIEMPRE el solicitante que
        /// registró la vacante; la configuración solo aporta principales adicionales y copias.
        ///
        /// Le importa por dos motivos: el candidato que había quedado seleccionado ya no va (su EMO
        /// de ingreso salió No Apto) y, según en qué etapa se lo hubiera descartado, la pelota
        /// puede volver a su lado — un rechazado en la decisión final vuelve a esperar SU decisión.
        /// Por eso el botón lleva al seguimiento del requerimiento y no a una pantalla de GTH.
        /// </summary>
        private async Task NotificarCandidatoRetomadoAlSolicitanteAsync(RetomarCandidatoContextoDto ctx)
        {
            var dest = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.CandidatoRetomado);
            var (principales, copias) = CorreoDestinatariosCombinador.Combinar(ctx.SolicitanteEmail, dest);

            if (principales.Count == 0)
            {
                // También entra acá cuando el correo está apagado con su interruptor maestro: en
                // ese caso es una decisión de la Configuración, no una falla.
                _logger.LogWarning(
                    "Candidato retomado del requerimiento {Codigo}: el solicitante no tiene correo cargado y el "
                    + "correo CANDIDATO_RETOMADO no tiene destinatarios principales activos, así que el aviso no sale.",
                    ctx.Codigo);
                return;
            }

            await _email.SendAsync(
                to:      principales,
                subject: $"[Reclutamiento] El proceso continúa con otro candidato — {ctx.Codigo} · {ctx.Puesto}",
                body:    ConstruirCuerpoCandidatoRetomado(ctx),
                isHtml:  true,
                cc:      copias.Count > 0 ? copias : null,
                // Sale del buzón de GTH, como el resto de los correos que GTH le manda al área
                // solicitante (long list, finalista): es el área la que le está contando algo.
                sender:  EmailSenders.Gth);
        }

        /// <summary>
        /// Cuerpo del aviso de candidato retomado: quién entra en carrera, desde qué etapa y qué
        /// sigue, con el botón al seguimiento del requerimiento. La franja da el desenlace en una
        /// línea, igual que el resto de correos internos del módulo.
        /// </summary>
        private string ConstruirCuerpoCandidatoRetomado(RetomarCandidatoContextoDto ctx)
        {
            var l    = Layout.Desde(_configuration);
            var link = ConstruirLinkSeguimiento(ctx.RequerimientoId);
            var res  = ctx.Resultado;

            var nombre = string.IsNullOrWhiteSpace(res.CandidatoNombre)
                ? "Un candidato"
                : Layout.Esc(res.CandidatoNombre);

            var datos = new List<Layout.Fila>
            {
                new("req-codigo", "Requerimiento", Textos.OGuion(ctx.Codigo)),
                new("req-puesto", "Puesto", Textos.OGuion(ctx.Puesto)),
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
                new("req-proyecto", "Proyecto / Obra", Textos.OGuion(ctx.ProyectoObra)),
                new("req-candidato", "Candidato", Textos.OGuion(res.CandidatoNombre)),
                new("req-estado", "El proceso vuelve a", Textos.OGuion(res.EstadoNombre)),
            };

            return l.Documento(
                new Layout.Cabecera(
                    "req-candidatos", "El Proceso Continúa",
                    $"<b>{nombre}</b> vuelve al proceso de <b>{Layout.Esc(ctx.Puesto)}</b>."),
                l.Franja("req-aviso", Layout.Tono.Info,
                    $"El candidato seleccionado no pasó su examen médico de ingreso. El proceso se retoma "
                    + $"desde la etapa <b>{Layout.Esc(res.EtapaNombre)}</b>."),
                l.Tarjeta(datos),
                l.Boton("Ver el requerimiento", link),
                l.EnlaceDirecto(link));
        }

        /// <summary>
        /// Enlace al seguimiento del requerimiento dentro de la pantalla del solicitante, con el
        /// modal ya abierto. Es el acceso de los correos que le hablan al área solicitante sin
        /// pedirle una decisión concreta: no lo manda a decidir una long list ni un finalista, lo
        /// manda a ver en qué anda su vacante. Sin sesión, el <c>authGuard</c> del frontend lo
        /// devuelve acá después del login.
        /// </summary>
        private string ConstruirLinkSeguimiento(int requerimientoId)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/gestion-gth/solicitud-personal/seguimiento/{requerimientoId}";
        }

        public Task<EstadoRequerimientoResultDto> VolverALongListDesdeEmoNoApto(
            int requerimientoId, int? userId) =>
            _repo.VolverALongListDesdeEmoNoApto(requerimientoId, userId);

        public Task<EstadoRequerimientoResultDto> CerrarProcesoDesdeEmoApto(
            int requerimientoId, int? userId) =>
            _repo.CerrarProcesoDesdeEmoApto(requerimientoId, userId);

        public async Task<EntrevistaAccionResultDto> GuardarEntrevista(
            int candidatoId, EntrevistaGuardarDto dto, int? userId)
        {
            if (dto == null || dto.Fecha == default)
                throw new AbrilException("Selecciona la fecha de la entrevista.", 400);
            if (!TimeOnly.TryParseExact(dto.Hora ?? "", "HH:mm", out var hora))
                throw new AbrilException("Selecciona la hora de la entrevista.", 400);
            if (dto.LugarId <= 0)
                throw new AbrilException("Selecciona el lugar de la entrevista.", 400);

            // Token de acceso público de los botones Confirmar / Rechazar del correo (hex,
            // url-safe). Es nuevo en cada envío: los del correo anterior dejan de responder por
            // una cita que ya cambió de fecha. Mismo formato que el del formulario del postulante.
            var nuevoToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

            var ctx = await _repo.GuardarEntrevista(
                candidatoId, dto.Fecha, hora, dto.LugarId, userId, nuevoToken);

            // Destinatarios: el principal (Para) es SIEMPRE el postulante citado; la configuración
            // del correo de ENTREVISTA solo aporta principales adicionales y copias.
            var dest = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.Entrevista);
            var (principales, copias) = CorreoDestinatariosCombinador.Combinar(ctx.Resumen.CorreoEnvio, dest);

            // Sin nadie a quien mandársela no hay envío que intentar: pasa cuando el postulante
            // quedó apagado como destinatario en Configuración y el correo no tiene principales
            // configurados. La cita igual quedó programada.
            if (principales.Count == 0)
            {
                _logger.LogWarning(
                    "Entrevista del candidato {CandidatoId}: el correo ENTREVISTA no tiene destinatarios "
                    + "principales activos (el postulante está apagado), así que la invitación no sale.",
                    candidatoId);
                return new EntrevistaAccionResultDto
                {
                    Message = "La entrevista quedó programada, pero la invitación no se envió: "
                              + "no hay a quién enviársela. Revísalo en Configuración de correos.",
                    Entrevista = ctx.Resumen,
                };
            }

            // El correo es best-effort: la entrevista ya quedó programada, así que un fallo del
            // envío se informa en el mensaje en vez de tumbar la operación.
            var message = $"Invitación enviada a {ctx.Resumen.CorreoEnvio}.";
            try
            {
                await _email.SendAsync(
                    to:      principales,
                    subject: $"Entrevista — {ctx.Puesto} · Abril Grupo Inmobiliario",
                    body:    ConstruirCuerpoEntrevista(ctx),
                    isHtml:  true,
                    cc:      copias.Count > 0 ? copias : null,
                    sender:  EmailSenders.Gth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar la invitación a la entrevista del candidato {CandidatoId}", candidatoId);
                message = "La entrevista quedó programada, pero no se pudo enviar la invitación por correo. Vuelve a intentarlo.";
            }

            return new EntrevistaAccionResultDto { Message = message, Entrevista = ctx.Resumen };
        }

        /// <summary>
        /// Citación a entrevista para el postulante: la fecha, la hora y el lugar, y los dos
        /// botones con los que responde. La respuesta se pide con botones y no pidiéndole que
        /// conteste el correo porque así queda registrada en el proceso: GTH la ve en el modal del
        /// requerimiento en lugar de tener que revisar la bandeja del buzón.
        ///
        /// Los botones van solos, sin una franja que explique qué hacen: dicen «Confirmar» y
        /// «Rechazar» y eso es todo lo que hay que saber (mismo criterio editorial que el resto de
        /// la familia, ver <see cref="Layout"/>).
        ///
        /// El lugar lleva debajo el enlace al mapa cuando el lugar lo tiene cargado. Es un enlace y
        /// no un mapa embebido a propósito: Outlook bloquea las imágenes remotas de terceros y una
        /// imagen estática de Google Maps necesita una API key, así que el mapa saldría roto justo
        /// donde más importa.
        /// </summary>
        private string ConstruirCuerpoEntrevista(EntrevistaEnvioContextoDto ctx)
        {
            var l = Layout.Desde(_configuration);
            var nombre = string.IsNullOrWhiteSpace(ctx.CandidatoNombre) ? "postulante" : ctx.CandidatoNombre;

            var lugar = Textos.OGuion(ctx.Resumen.LugarNombre);
            if (!string.IsNullOrWhiteSpace(ctx.LugarMapsUrl))
                lugar += $"<br />{Textos.Enlace(ctx.LugarMapsUrl!, "Ver en Google Maps")}";

            return l.Documento(
                new Layout.Cabecera(
                    "req-entrevista", "Invitación a Entrevista",
                    $"Estimado(a) {Layout.Esc(nombre)}: te esperamos para la posición <b>{Layout.Esc(ctx.Puesto)}</b>."),
                l.Tarjeta(new List<Layout.Fila>
                {
                    new("req-fecha", "Fecha", ctx.Resumen.Fecha.ToString("dd/MM/yyyy")),
                    new("req-hora", "Hora", Layout.Esc(ctx.Resumen.Hora)),
                    new("req-lugar", "Lugar", lugar),
                }),
                l.BotonesRespuesta(
                    "Confirmar", ConstruirLinkRespuestaEntrevista(ctx.Token, "confirmar"),
                    "Rechazar",  ConstruirLinkRespuestaEntrevista(ctx.Token, "rechazar")));
        }

        // ── Respuesta del candidato a su citación (pública, por token) ────────
        /// <summary>
        /// Enlace de un botón del correo: la página pública que registra la respuesta del
        /// candidato. Es el mismo mecanismo que el enlace del formulario del postulante — sin
        /// login, el token identifica la entrevista.
        /// </summary>
        private string ConstruirLinkRespuestaEntrevista(string token, string respuesta)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/postulante/entrevista?token={Uri.EscapeDataString(token)}&r={respuesta}";
        }

        public async Task<EntrevistaRespuestaPublicaDto> ResponderEntrevista(string token, string respuesta)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new AbrilException("Enlace de la entrevista no válido.", 400);

            var codigo = EntrevistaRespuestaCodigo.Normalizar(respuesta)
                ?? throw new AbrilException("La respuesta a la entrevista no es válida.", 400);

            var ctx = await _repo.RegistrarRespuestaEntrevista(token.Trim(), codigo);

            // Aviso a GTH. Best-effort a propósito: la respuesta del candidato ya quedó registrada
            // y visible en el modal del requerimiento, así que un fallo del correo interno no tiene
            // por qué mostrarle un error a quien solo pulsó un botón desde su correo. Tampoco se
            // reenvía cuando el candidato abre dos veces el mismo enlace: no cambió nada.
            if (!ctx.YaHabiaRespondidoLoMismo)
            {
                try
                {
                    await NotificarRespuestaEntrevistaAGthAsync(ctx);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "No se pudo avisar a GTH la respuesta del candidato a la entrevista del requerimiento {Codigo}",
                        ctx.Codigo);
                }

                // Y al solicitante, pero SOLO si confirmó: es una cita a la que él tiene que ir, y
                // lo que necesita es el día, la hora y el lugar. Un rechazo no le genera nada —
                // reprogramar es trabajo de GTH, que ya se enteró con el correo de arriba.
                if (ctx.Resumen.RespuestaCodigo == EntrevistaRespuestaCodigo.Confirmada)
                {
                    try
                    {
                        await NotificarEntrevistaConfirmadaAlSolicitanteAsync(ctx);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "No se pudo avisarle al solicitante la entrevista confirmada del requerimiento {Codigo}",
                            ctx.Codigo);
                    }
                }
            }

            return new EntrevistaRespuestaPublicaDto
            {
                RespuestaCodigo = codigo,
                CandidatoNombre = ctx.CandidatoNombre,
                Puesto          = ctx.Puesto,
                Fecha           = ctx.Resumen.Fecha,
                Hora            = ctx.Resumen.Hora,
                LugarNombre     = ctx.Resumen.LugarNombre,
            };
        }

        /// <summary>
        /// Avisa a GTH que el candidato respondió su citación (tipo ENTREVISTA_RESPUESTA). El
        /// destinatario principal es SIEMPRE el buzón del área de Gestión del Talento Humano, que
        /// sale de <c>area_scope.email</c> de su nodo — el mismo que resuelve el destinatario
        /// dinámico GTH_AREA, así que si el área cambia de correo en Configuración → Áreas, este
        /// aviso lo sigue sin tocar código. La configuración del correo solo aporta principales
        /// adicionales y copias.
        /// </summary>
        private async Task NotificarRespuestaEntrevistaAGthAsync(EntrevistaRespuestaContextoDto ctx)
        {
            var emailGth = await _correoConfig.GetEmailAreaGthAsync();
            var dest     = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.EntrevistaRespuesta);
            var (principales, copias) = CorreoDestinatariosCombinador.Combinar(emailGth, dest);

            if (principales.Count == 0)
            {
                _logger.LogWarning(
                    "Respuesta de entrevista del requerimiento {Codigo}: el área de GTH no tiene correo "
                    + "cargado en el árbol de áreas y el correo ENTREVISTA_RESPUESTA no tiene destinatarios "
                    + "activos, así que el aviso no sale.", ctx.Codigo);
                return;
            }

            var confirmo = ctx.Resumen.RespuestaCodigo == EntrevistaRespuestaCodigo.Confirmada;
            var accion   = confirmo ? "confirmó" : "rechazó";

            await _email.SendAsync(
                to:      principales,
                subject: $"[Reclutamiento] Entrevista {accion} — {ctx.Codigo} · {ctx.Puesto}",
                body:    ConstruirCuerpoRespuestaEntrevista(ctx, confirmo),
                isHtml:  true,
                cc:      copias.Count > 0 ? copias : null,
                // Sale de aprobaciones@abril.pe y no del buzón de GTH: es un aviso INTERNO que
                // llega justamente al buzón de GTH, y mandarlo desde ese mismo buzón lo dejaba
                // como un correo que el área se escribe a sí misma. Los correos que van al
                // candidato (invitación, correcciones, fin de proceso) sí siguen saliendo de GTH.
                sender:  EmailSenders.Aprobaciones);
        }

        /// <summary>
        /// Aviso a GTH de la respuesta del candidato. Mismo criterio que el resto de correos
        /// internos del módulo: la franja da el desenlace en una línea, la tarjeta trae los datos
        /// de la cita y el botón lleva al requerimiento en la bandeja.
        /// </summary>
        private string ConstruirCuerpoRespuestaEntrevista(EntrevistaRespuestaContextoDto ctx, bool confirmo)
        {
            var l = Layout.Desde(_configuration);
            var nombre = string.IsNullOrWhiteSpace(ctx.CandidatoNombre)
                ? "El candidato"
                : Layout.Esc(ctx.CandidatoNombre);

            var filas = new List<Layout.Fila>
            {
                new("req-codigo", "Requerimiento", Textos.OGuion(ctx.Codigo)),
                new("req-puesto", "Puesto", Textos.OGuion(ctx.Puesto)),
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
                new("req-candidato", "Candidato", Textos.OGuion(ctx.CandidatoNombre)),
                new("req-correo", "Correo", Textos.OGuion(ctx.CorreoCandidato)),
                new("req-fecha", "Fecha de la cita", ctx.Resumen.Fecha.ToString("dd/MM/yyyy")),
                new("req-hora", "Hora", Textos.OGuion(ctx.Resumen.Hora)),
                new("req-lugar", "Lugar", Textos.OGuion(ctx.Resumen.LugarNombre)),
            };
            if (ctx.Resumen.RespondidoEn.HasValue)
                filas.Add(new("req-estado", "Respondió el",
                    ctx.Resumen.RespondidoEn.Value.ToString("dd/MM/yyyy HH:mm")));

            var franja = confirmo
                ? l.Franja("req-check", Layout.Tono.Verde,
                    $"<b>{nombre}</b> confirmó que asistirá a la entrevista.")
                : l.Franja("req-rechazadas", Layout.Tono.Rojo,
                    $"<b>{nombre}</b> avisó que no podrá asistir a la entrevista.");

            var link = ctx.RequerimientoId > 0 ? ConstruirLinkDetalleRequerimiento(ctx.RequerimientoId) : "";

            return l.Documento(
                new Layout.Cabecera(
                    "req-entrevista", "Respuesta a la Entrevista",
                    $"<b>{nombre}</b> {(confirmo ? "confirmó" : "rechazó")} su entrevista para "
                    + $"<b>{Layout.Esc(ctx.Puesto)}</b>."),
                franja,
                l.Tarjeta(filas),
                string.IsNullOrEmpty(link) ? "" : l.Boton("Ver el requerimiento", link),
                string.IsNullOrEmpty(link) ? "" : l.EnlaceDirecto(link));
        }

        /// <summary>
        /// Le avisa al área solicitante que el candidato confirmó su entrevista (tipo
        /// ENTREVISTA_CONFIRMADA_SOLICITANTE): día, hora y lugar, que es lo que necesita para
        /// acudir. El destinatario principal es SIEMPRE el solicitante que registró la vacante; la
        /// configuración solo aporta principales adicionales y copias.
        ///
        /// Es otro correo que el de <see cref="NotificarRespuestaEntrevistaAGthAsync"/> aunque lo
        /// dispare el mismo clic: aquel le cuenta a GTH cómo respondió el candidato (confirmó o no)
        /// y este cita a alguien de casa a una reunión. Solo sale con la confirmación.
        /// </summary>
        private async Task NotificarEntrevistaConfirmadaAlSolicitanteAsync(EntrevistaRespuestaContextoDto ctx)
        {
            var dest = await _destinatarios.ResolverAsync(
                CorreoTipoReclutamiento.EntrevistaConfirmadaSolicitante);
            var (principales, copias) = CorreoDestinatariosCombinador.Combinar(ctx.SolicitanteEmail, dest);

            if (principales.Count == 0)
            {
                // También entra acá cuando el correo está apagado con su interruptor maestro.
                _logger.LogWarning(
                    "Entrevista confirmada del requerimiento {Codigo}: el solicitante no tiene correo cargado y el "
                    + "correo ENTREVISTA_CONFIRMADA_SOLICITANTE no tiene destinatarios principales activos, así que "
                    + "el aviso no sale.", ctx.Codigo);
                return;
            }

            await _email.SendAsync(
                to:      principales,
                subject: $"[Reclutamiento] Entrevista confirmada — {ctx.Codigo} · {ctx.Puesto}",
                body:    ConstruirCuerpoEntrevistaConfirmadaSolicitante(ctx),
                isHtml:  true,
                cc:      copias.Count > 0 ? copias : null,
                // Buzón de GTH: es el área que lleva el proceso la que lo está citando, igual que
                // en la long list y en el finalista.
                sender:  EmailSenders.Gth);
        }

        /// <summary>
        /// Cuerpo del aviso de entrevista confirmada al solicitante. Lo importante es la cita, así
        /// que la fecha, la hora y el lugar van en la franja (se leen de un vistazo) y repetidos en
        /// la tarjeta con el resto del contexto. El botón lleva al seguimiento del requerimiento.
        /// </summary>
        private string ConstruirCuerpoEntrevistaConfirmadaSolicitante(EntrevistaRespuestaContextoDto ctx)
        {
            var l    = Layout.Desde(_configuration);
            var link = ConstruirLinkSeguimiento(ctx.RequerimientoId);
            var cita = ctx.Resumen;

            var nombre = string.IsNullOrWhiteSpace(ctx.CandidatoNombre)
                ? "El candidato"
                : Layout.Esc(ctx.CandidatoNombre);

            var filas = new List<Layout.Fila>
            {
                new("req-codigo", "Requerimiento", Textos.OGuion(ctx.Codigo)),
                new("req-puesto", "Puesto", Textos.OGuion(ctx.Puesto)),
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
                new("req-candidato", "Candidato", Textos.OGuion(ctx.CandidatoNombre)),
                new("req-fecha", "Fecha", cita.Fecha.ToString("dd/MM/yyyy")),
                new("req-hora", "Hora", Textos.OGuion(cita.Hora)),
                // El lugar con su enlace al mapa cuando lo tiene cargado: quien va a la entrevista
                // puede no conocer la dirección.
                new("req-lugar", "Lugar", string.IsNullOrWhiteSpace(ctx.LugarMapsUrl)
                    ? Textos.OGuion(cita.LugarNombre)
                    : Textos.Enlace(ctx.LugarMapsUrl!, string.IsNullOrWhiteSpace(cita.LugarNombre)
                        ? "Ver ubicación"
                        : cita.LugarNombre)),
            };

            return l.Documento(
                new Layout.Cabecera(
                    "req-entrevista", "Entrevista Confirmada",
                    $"<b>{nombre}</b> confirmó su entrevista para <b>{Layout.Esc(ctx.Puesto)}</b>."),
                l.Franja("req-check", Layout.Tono.Verde,
                    $"<b>{cita.Fecha:dd/MM/yyyy}</b> a las <b>{Layout.Esc(cita.Hora)}</b>"
                    + (string.IsNullOrWhiteSpace(cita.LugarNombre)
                        ? ""
                        : $" · {Layout.Esc(cita.LugarNombre)}")),
                l.Tarjeta(filas),
                l.Boton("Ver el requerimiento", link),
                l.EnlaceDirecto(link));
        }

        // ── Evaluación de la entrevista y no continuidad ──────────────────────
        /// <summary>
        /// Formatos aceptados en los archivos del informe. Más amplio que el del CV porque los
        /// resultados de una evaluación de conocimientos suelen venir en Excel o escaneados.
        /// </summary>
        private static readonly string[] AllowedEvaluacionExt =
            { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".webp" };

        public async Task<EvaluacionAccionResultDto> GuardarEvaluacion(
            int candidatoId, EvaluacionGuardarDto dto, List<EvaluacionArchivoSubidaDto> archivos, int? userId)
        {
            if (dto == null)
                throw new AbrilException("Datos de la evaluación no recibidos.", 400);

            // Los tres comentarios son obligatorios: guardar el informe es enviarlo como finalista,
            // y el área solicitante decide con ese informe completo. Los dos archivos NO lo son.
            if (string.IsNullOrWhiteSpace(dto.ComentarioEntrevista) ||
                string.IsNullOrWhiteSpace(dto.ComentarioPsicotecnico) ||
                string.IsNullOrWhiteSpace(dto.ComentarioRecomendacion))
                throw new AbrilException(
                    "El resultado de entrevista, el informe psicotécnico y la recomendación GTH son obligatorios.", 400);

            archivos ??= new List<EvaluacionArchivoSubidaDto>();
            ValidarArchivosEvaluacion(archivos);

            var guardada = await _repo.GuardarEvaluacion(candidatoId, dto, userId);

            // Los archivos se suben DESPUÉS de guardar porque la carpeta de SharePoint lleva el
            // código del requerimiento, que sale de ese guardado. Si la subida falla, el informe ya
            // quedó registrado: se corta acá con un mensaje claro y GTH vuelve a guardar (los
            // comentarios se reescriben igual y el correo sale en ese reintento).
            if (archivos.Count > 0)
            {
                var subidos = await SubirArchivosEvaluacionAsync(guardada.Codigo, candidatoId, archivos);
                var lista   = await _repo.GuardarEvaluacionArchivos(guardada.EvaluacionId, subidos, userId);

                guardada.Evaluacion.Archivos       = lista;
                guardada.Envio.Evaluacion.Archivos = lista;
            }

            // Guardar el informe ES enviar al finalista, así que acá sale el aviso al solicitante
            // (tipo FINALISTA_ENVIO), con los archivos adjuntos. Best-effort: el finalista ya quedó
            // enviado en la base y el solicitante lo ve igual en su panel, así que un fallo del
            // correo se informa en el mensaje en vez de tumbar la operación.
            var message = "Evaluación guardada.";
            var envio   = await EnviarFinalistaAlSolicitanteAsync(guardada.Envio, archivos);
            if (envio != null) message += " " + envio;

            return new EvaluacionAccionResultDto
            {
                Message      = message,
                Evaluacion   = guardada.Evaluacion,
                EstadoCodigo = guardada.EstadoCodigo,
                EstadoNombre = guardada.EstadoNombre,
            };
        }

        /// <summary>
        /// Valida los archivos del informe antes de tocar nada: formato, y el peso conjunto contra
        /// lo que acepta el proveedor de correo con los adjuntos adentro (van adjuntos al aviso del
        /// finalista, igual que los CVs en la long list). Se valida acá, antes de subir, para que
        /// GTH vea qué archivo achicar en vez de un 502 al final del flujo.
        /// </summary>
        private static void ValidarArchivosEvaluacion(List<EvaluacionArchivoSubidaDto> archivos)
        {
            long total = 0;
            foreach (var archivo in archivos)
            {
                if (archivo.Content == null || archivo.Content.Length == 0)
                    throw new AbrilException("Un archivo del informe llegó vacío. Vuelve a cargarlo.", 400);

                var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (!AllowedEvaluacionExt.Contains(ext))
                    throw new AbrilException(
                        $"El archivo «{Path.GetFileName(archivo.FileName)}» tiene un formato no permitido. Solo " +
                        $"{string.Join(", ", AllowedEvaluacionExt.Select(e => e.TrimStart('.').ToUpperInvariant()))}.", 400);

                total += archivo.Content.Length;
            }

            if (total > MaxLongListCorreoBytes)
                throw new AbrilException(
                    $"Los archivos del informe pesan {FormatearMb(total)} y el correo admite hasta " +
                    $"{FormatearMb(MaxLongListCorreoBytes)}. Reduce o quita alguno antes de enviar al finalista.", 400);
        }

        /// <summary>
        /// Sube los archivos del informe a la carpeta del requerimiento en SharePoint y devuelve lo
        /// que hay que persistir. El nombre lleva el candidato para que dos finalistas del mismo
        /// requerimiento no se pisen.
        /// </summary>
        private async Task<List<EvaluacionArchivoPersistDto>> SubirArchivosEvaluacionAsync(
            string codigo, int candidatoId, List<EvaluacionArchivoSubidaDto> archivos)
        {
            var carpeta = await ResolverCarpetaRequerimientoAsync(codigo);
            var persist = new List<EvaluacionArchivoPersistDto>(archivos.Count);

            foreach (var archivo in archivos)
            {
                var prefijo = archivo.TipoCodigo == EvaluacionArchivoCodigo.InformeFinal
                    ? "informe"
                    : "conocimientos";

                var subida = await SubirArchivoRequerimientoAsync(
                    carpeta, prefijo, codigo, $"{candidatoId}",
                    archivo.FileName, archivo.Content, archivo.ContentType);

                persist.Add(new EvaluacionArchivoPersistDto
                {
                    TipoCodigo     = archivo.TipoCodigo,
                    Nombre         = subida.FileName,
                    NombreOriginal = Path.GetFileName(archivo.FileName),
                    Url            = subida.WebUrl,
                    ItemId         = subida.ItemId,
                    DriveId        = carpeta.DriveId,
                });
            }

            return persist;
        }

        /// <summary>
        /// Avisa al área solicitante que tiene un finalista por decidir (tipo FINALISTA_ENVIO). El
        /// destinatario principal es SIEMPRE el solicitante que registró la solicitud; la
        /// configuración solo aporta principales adicionales y copias.
        ///
        /// Los archivos del informe viajan adjuntos: el solicitante decide leyéndolos, así que
        /// tenerlos en el correo le evita entrar a la pantalla solo para abrirlos (igual quedan
        /// enlazados ahí).
        ///
        /// Devuelve la frase que se le agrega al mensaje de la pantalla, o null si no hay nada que
        /// contar. Nunca lanza: el finalista ya quedó enviado en la base.
        /// </summary>
        private async Task<string?> EnviarFinalistaAlSolicitanteAsync(
            FinalistaEnvioContextoDto ctx, List<EvaluacionArchivoSubidaDto>? archivos = null)
        {
            try
            {
                var dest = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.FinalistaEnvio);
                var (principales, copias) = CorreoDestinatariosCombinador.Combinar(ctx.SolicitanteEmail, dest);

                if (principales.Count == 0)
                {
                    _logger.LogWarning(
                        "Finalista enviado del requerimiento {Codigo}: el solicitante no tiene correo cargado "
                        + "y el correo FINALISTA_ENVIO no tiene destinatarios principales activos, así que el "
                        + "aviso no sale.", ctx.Codigo);
                    return "El solicitante no tiene un correo al que avisarle: revísalo en Configuración.";
                }

                var adjuntos = (archivos ?? new List<EvaluacionArchivoSubidaDto>())
                    .Select(a => new EmailAttachment
                    {
                        FileName    = string.IsNullOrWhiteSpace(a.FileName) ? "informe" : Path.GetFileName(a.FileName),
                        ContentType = string.IsNullOrWhiteSpace(a.ContentType) ? "application/octet-stream" : a.ContentType,
                        Content     = a.Content,
                    })
                    .ToList();

                await _email.SendAsync(
                    to:      principales,
                    subject: $"[Reclutamiento] Finalista por revisar — {ctx.Codigo} · {ctx.Puesto}",
                    body:    ConstruirCuerpoFinalistaEnvio(ctx),
                    isHtml:  true,
                    cc:      copias.Count > 0 ? copias : null,
                    attachments: adjuntos.Count > 0 ? adjuntos : null,
                    sender:  EmailSenders.Gth);

                return $"Se le avisó al solicitante ({principales[0]}).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo enviar el aviso de finalista al solicitante del requerimiento {Codigo}", ctx.Codigo);
                return "No se pudo enviar el aviso al solicitante; vuelve a guardar el informe para reintentarlo.";
            }
        }

        /// <summary>
        /// Enlace al informe de finalistas dentro de la pantalla del solicitante, con el modal ya
        /// abierto. Mismo mecanismo que el resto de correos del módulo: sin sesión, el
        /// <c>authGuard</c> del frontend manda al login con esta URL como <c>returnUrl</c>.
        /// </summary>
        private string ConstruirLinkRevisionFinalistas(int requerimientoId)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/gestion-gth/solicitud-personal/finalistas/{requerimientoId}";
        }

        /// <summary>
        /// Cuerpo del correo de finalista al solicitante: quién es, de qué requerimiento y el
        /// informe que GTH registró tras la entrevista. Los tres comentarios van en su propia
        /// tarjeta porque son el motivo del correo — es con eso que el solicitante decide — y el
        /// botón lleva a la pantalla donde aprueba o rechaza.
        /// </summary>
        private string ConstruirCuerpoFinalistaEnvio(FinalistaEnvioContextoDto ctx)
        {
            var l    = Layout.Desde(_configuration);
            var link = ConstruirLinkRevisionFinalistas(ctx.RequerimientoId);
            var ev   = ctx.Evaluacion;

            var datos = new List<Layout.Fila>
            {
                new("req-codigo", "Requerimiento", Textos.OGuion(ctx.Codigo)),
                new("req-puesto", "Puesto", Textos.OGuion(ctx.Puesto)),
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
                new("req-proyecto", "Proyecto / Obra", Textos.OGuion(ctx.ProyectoObra)),
                new("req-candidato", "Finalista", Textos.OGuion(ctx.CandidatoNombre)),
            };

            // Los comentarios se escriben en textareas: se conservan los saltos de línea. Los
            // íconos son de fila (28px), no los de cabecera: en una tarjeta el aro grande se ve
            // reescalado y desalineado respecto del resto de filas.
            var informe = new List<Layout.Fila>();
            if (!string.IsNullOrWhiteSpace(ev.ComentarioEntrevista))
                informe.Add(new("req-comentario", "Resultado de entrevista",
                    Layout.EscMultilinea(ev.ComentarioEntrevista)));
            if (!string.IsNullOrWhiteSpace(ev.ComentarioPsicotecnico))
                informe.Add(new("req-justificacion", "Informe psicotécnico",
                    Layout.EscMultilinea(ev.ComentarioPsicotecnico)));
            if (!string.IsNullOrWhiteSpace(ev.ComentarioRecomendacion))
                informe.Add(new("req-vistobueno", "Recomendación GTH",
                    Layout.EscMultilinea(ev.ComentarioRecomendacion)));

            // Los archivos van adjuntos, pero también como fila con su enlace: el adjunto se pierde
            // al reenviar el correo y el enlace sigue abriendo el documento desde SharePoint.
            foreach (var archivo in ev.Archivos)
                informe.Add(new("req-sustento", archivo.TipoNombre,
                    string.IsNullOrWhiteSpace(archivo.Url)
                        ? Textos.OGuion(archivo.Nombre)
                        : Textos.Enlace(archivo.Url!, archivo.Nombre)));

            var nombre = string.IsNullOrWhiteSpace(ctx.CandidatoNombre)
                ? "Un candidato"
                : Layout.Esc(ctx.CandidatoNombre);

            return l.Documento(
                new Layout.Cabecera(
                    "req-finalista", "Finalista por Revisar",
                    $"<b>{nombre}</b> pasó a finalista para <b>{Layout.Esc(ctx.Puesto)}</b>."),
                l.Tarjeta(datos),
                // Los tres comentarios son obligatorios, pero la sección se condiciona igual para
                // que un informe vacío no deje un título colgado sin tarjeta debajo.
                informe.Count > 0 ? l.Seccion("req-candidatos", "Informe de GTH") : "",
                l.Tarjeta(informe),
                l.Boton("Revisar y decidir", link),
                l.EnlaceDirecto(link));
        }

        public async Task<EvaluacionAccionResultDto> EnviarAgradecimiento(int candidatoId, int? userId)
        {
            var ctx = await _repo.RegistrarAgradecimiento(candidatoId, userId);

            // Mismo criterio que la invitación a la entrevista: el resultado ya quedó registrado,
            // así que un fallo del correo se informa en el mensaje (GTH puede reintentar) en vez
            // de tumbar la operación.
            var message = $"Correo de agradecimiento enviado a {ctx.Correo}. El candidato ya no continúa en el proceso.";
            try
            {
                if (!await EnviarAgradecimientoAsync(ctx)) message = FinDeProcesoSinDestinatarios;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo de agradecimiento del candidato {CandidatoId}", candidatoId);
                message = "El candidato quedó registrado como no continúa, pero no se pudo enviar el correo de agradecimiento. Vuelve a intentarlo.";
            }

            return new EvaluacionAccionResultDto { Message = message, Evaluacion = ctx.Resumen };
        }

        public async Task<EvaluacionAccionResultDto> RechazarPostulante(int candidatoId, int? userId)
        {
            var ctx = await _repo.RegistrarRechazoPostulante(candidatoId, userId);

            // Mismo criterio que el agradecimiento tras la entrevista: el candidato ya quedó fuera
            // del proceso en la base, así que un fallo del correo se informa en el mensaje (GTH
            // puede reintentar) en vez de tumbar la operación.
            var message = $"Se envió el correo de fin de proceso a {ctx.Correo}. El postulante ya no continúa.";
            try
            {
                if (!await EnviarAgradecimientoAsync(ctx)) message = FinDeProcesoSinDestinatarios;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo de fin de proceso del candidato {CandidatoId}", candidatoId);
                message = "El postulante quedó fuera del proceso, pero no se pudo enviar el correo de fin de proceso. Vuelve a intentarlo.";
            }

            return new EvaluacionAccionResultDto { Message = message, Evaluacion = ctx.Resumen };
        }

        /// <summary>
        /// Envía el correo de fin de proceso (tipo AGRADECIMIENTO). El destinatario principal es
        /// SIEMPRE el candidato; la configuración de Reclutamiento solo aporta principales extra y
        /// copias, por si GTH quiere quedarse con el registro de cada cierre.
        ///
        /// Lo comparten los cuatro lados desde los que sale el mismo correo: cuando GTH rechaza al
        /// postulante tras rechazarle el formulario, cuando lo marca como "no continúa" tras la
        /// entrevista, cuando el solicitante rechaza a un finalista y cuando aprueba a uno y los
        /// demás quedan sin elegir. No atrapa excepciones a propósito — cada llamador ya decide
        /// qué hacer si el envío falla.
        ///
        /// Devuelve false cuando no hay a quién enviárselo (el candidato quedó apagado como
        /// destinatario en Configuración y el correo no tiene principales configurados): no es un
        /// error del proveedor, así que no se lanza — el llamador lo dice en su mensaje.
        /// </summary>
        private async Task<bool> EnviarAgradecimientoAsync(AgradecimientoEnvioContextoDto ctx)
        {
            var dest = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.Agradecimiento);
            var (principales, copias) = CorreoDestinatariosCombinador.Combinar(ctx.Correo, dest);

            if (principales.Count == 0)
            {
                _logger.LogWarning(
                    "Fin de proceso de «{Candidato}»: el correo AGRADECIMIENTO no tiene destinatarios "
                    + "principales activos (el candidato está apagado), así que no sale.", ctx.CandidatoNombre);
                return false;
            }

            await _email.SendAsync(
                to:      principales,
                subject: "Gracias por participar",
                body:    ConstruirCuerpoAgradecimiento(ctx),
                isHtml:  true,
                cc:      copias.Count > 0 ? copias : null,
                sender:  EmailSenders.Gth);

            return true;
        }

        /// <summary>
        /// Mensaje para GTH cuando el fin de proceso no salió por configuración y no por un fallo
        /// del proveedor: reintentar no cambia nada, hay que prender al candidato en Configuración.
        /// </summary>
        private const string FinDeProcesoSinDestinatarios =
            "El candidato quedó fuera del proceso, pero el correo de fin de proceso no se envió: "
            + "no hay a quién enviárselo. Revísalo en Configuración de correos.";

        /// <summary>
        /// Cierre del proceso para el candidato que no continúa. No menciona motivos: informa que
        /// el proceso concluyó, agradece la participación y deja abierta la puerta a futuros
        /// procesos.
        ///
        /// Es el único correo del módulo que sigue siendo texto corrido, y a propósito: una carta
        /// de no continuidad resuelta con una tabla de datos se lee como un rechazo automático.
        ///
        /// El texto es el que redactó GTH y se transcribe tal cual (incluido el trato mezclado de
        /// tú y usted): es una comunicación institucional al candidato, no una cadena de la app.
        /// Lo único variable es el puesto, que ya viene en mayúsculas desde el catálogo.
        /// </summary>
        private string ConstruirCuerpoAgradecimiento(AgradecimientoEnvioContextoDto ctx)
        {
            var l = Layout.Desde(_configuration);

            return l.Documento(
                new Layout.Cabecera("req-gracias", "Gracias por participar"),
                l.Parrafo("Estimado postulante,"),
                l.Parrafo(
                    "Le informamos que el proceso de selección para el puesto de "
                    + $"<b>{Layout.Esc(ctx.Puesto)}</b> ha concluido y, en esta oportunidad, no has sido "
                    + "seleccionado. Agradecemos mucho tu interés, el tiempo dedicado y la disposición "
                    + "mostrada durante todo el proceso. Con tu autorización, nos gustaría conservar su "
                    + "información en nuestra base de datos para futuras oportunidades que se ajusten a "
                    + "su perfil."),
                l.Parrafo("Le deseamos el mayor de los éxitos en su desarrollo personal y profesional."),
                l.Parrafo("¡Saludos cordiales!"),
                l.Parrafo("Atentamente,<br /><b>Equipo de Gestión del Talento Humano</b>"));
        }

        public async Task<RevisionFinalistasDto> GetRevisionFinalistas(int requerimientoId, int? userId)
        {
            var scope = await ResolverScope(userId, paraGestionar: false);

            var revision = await _repo.GetRevisionFinalistas(requerimientoId, scope);
            if (revision == null)
                throw new AbrilException("No se encontró el informe de finalistas del requerimiento.", 404);

            return revision;
        }

        // ── Decisión final del solicitante sobre un finalista ─────────────────
        public async Task<FinalistaDecisionResultDto> RegistrarDecisionFinalista(
            int requerimientoId, FinalistaDecisionDto dto, int? userId)
        {
            var scope = await ResolverScope(userId, paraGestionar: true);
            if (dto == null || dto.CandidatoId <= 0)
                throw new AbrilException("Selecciona al finalista sobre el que quieres decidir.", 400);

            // El área a la que entra el seleccionado no se pregunta: la resuelve el repositorio
            // desde el puesto del requerimiento, que es quien lo conoce.
            var ctx = await _repo.RegistrarDecisionFinalista(
                requerimientoId, dto.CandidatoId, dto.Aprobado, scope);
            var res = ctx.Resultado;

            // 1) Al rechazar, el finalista recibe el mismo correo de fin de proceso que le envía GTH
            //    a quien no supera la entrevista. Best-effort: la decisión ya quedó registrada.
            if (!res.Aprobado)
                await EnviarFinDeProcesoAsync(ctx, res.CandidatoNombre, ctx.CandidatoCorreo, dto.CandidatoId);

            // 2) Al aprobar, el puesto queda cubierto: los finalistas que seguían en carrera ya
            //    quedaron cerrados como rechazados en la misma transacción, así que se les avisa
            //    con el mismo correo. También best-effort, uno por uno: un fallo del proveedor con
            //    un candidato no puede dejar sin aviso a los demás ni tumbar la decisión.
            foreach (var noElegido in ctx.NoElegidos)
                await EnviarFinDeProcesoAsync(ctx, noElegido.Nombre, noElegido.Correo, noElegido.CandidatoId);

            // 3) Notificar la decisión a GTH (tipo FINALISTA_DECISION), igual que la de long list.
            await NotificarDecisionFinalistaAGthAsync(requerimientoId, ctx);

            var otros = ctx.NoElegidos.Count == 0
                ? ""
                : $" Se le avisó el fin del proceso a {ctx.NoElegidos.Count} postulante(s) que no fueron elegidos.";

            res.Message = res.Aprobado
                ? $"{res.CandidatoNombre} quedó seleccionado. GTH le programará su examen médico de ingreso y el proceso se cierra cuando el examen salga apto.{otros}"
                : res.TodosRechazados
                    ? $"{res.CandidatoNombre} fue rechazado y se le envió el correo de fin de proceso. Al no quedar finalistas, GTH preparará y enviará una nueva long list."
                    : $"{res.CandidatoNombre} fue rechazado y se le envió el correo de fin de proceso.";

            return res;
        }

        /// <summary>
        /// Correo de fin de proceso a un candidato de este requerimiento, sin motivo y sin
        /// bloquear: la decisión ya quedó registrada, así que un fallo del proveedor solo se
        /// registra. No hace nada si el candidato no tiene correo cargado.
        /// </summary>
        private async Task EnviarFinDeProcesoAsync(
            FinalistaDecisionContextoDto ctx, string? nombre, string correo, int candidatoId)
        {
            if (string.IsNullOrWhiteSpace(correo)) return;

            try
            {
                await EnviarAgradecimientoAsync(new AgradecimientoEnvioContextoDto
                {
                    CandidatoNombre = nombre ?? string.Empty,
                    Puesto          = ctx.Puesto,
                    Codigo          = ctx.Codigo,
                    Correo          = correo,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo enviar el correo de fin de proceso al candidato {CandidatoId} del requerimiento {Codigo}",
                    candidatoId, ctx.Codigo);
            }
        }

        /// <summary>
        /// Envía a GTH el correo con la decisión final del solicitante sobre un finalista (tipo
        /// FINALISTA_DECISION). To = principales configurados, CC = copias. Sin principales no se
        /// envía. No bloquea: cualquier fallo solo se registra como warning.
        /// </summary>
        private async Task NotificarDecisionFinalistaAGthAsync(int requerimientoId, FinalistaDecisionContextoDto ctx)
        {
            try
            {
                var dest = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.FinalistaDecision);
                if (dest.Para.Count == 0)
                {
                    _logger.LogWarning(
                        "No hay destinatarios principales activos para el correo de decisión de finalista ({Codigo}); no se envía.",
                        ctx.Codigo);
                    return;
                }

                var res     = ctx.Resultado;
                var accion  = res.Aprobado ? "aprobó" : "rechazó";
                var subject = $"[Reclutamiento] Decisión de finalista — {ctx.Codigo} · {ctx.Puesto}";

                await _email.SendAsync(
                    to:      dest.EmailsPara,
                    subject: subject,
                    body:    ConstruirCuerpoDecisionFinalista(requerimientoId, ctx, accion),
                    isHtml:  true,
                    cc:      dest.Copias.Count > 0 ? dest.EmailsCopias : null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar el correo de decisión de finalista del requerimiento {Codigo}", ctx.Codigo);
            }
        }

        /// <summary>
        /// Enlace a SSOMA · Salud Ocupacional · EMOs con la ficha del seleccionado enfocada y el
        /// modal de programación ya abierto: exactamente el mismo salto que hace el botón
        /// «Programar EMO de ingreso» de la pantalla de Reclutamiento (ver <c>detalle.ts</c>). El
        /// EMO se programa a mano, así que el correo solo tiene que dejar a GTH parado ahí.
        /// Sin sesión, el <c>authGuard</c> del frontend manda al login con esta URL como
        /// <c>returnUrl</c> y lo devuelve acá al entrar.
        /// </summary>
        private string ConstruirLinkProgramarEmoIngreso(int workerId)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/ssoma/salud-ocupacional/emos?workerId={workerId}&programar=1";
        }

        /// <summary>
        /// Cuerpo del correo a GTH con la decisión final del solicitante sobre un finalista. Lo que
        /// sigue después de la decisión va en la franja, que es una línea: el detalle del proceso
        /// se ve en la pantalla.
        ///
        /// Al aprobar, el botón lleva directo a la programación del EMO de ingreso del
        /// seleccionado, que es lo único que le queda por hacer a GTH para cerrar el proceso. Si el
        /// seleccionado no llegó a tener ficha (sin formulario del postulante aprobado no hay a
        /// quién programarle nada) y en los rechazos, el botón cae al detalle del requerimiento.
        /// </summary>
        private string ConstruirCuerpoDecisionFinalista(
            int requerimientoId, FinalistaDecisionContextoDto ctx, string accion)
        {
            var l   = Layout.Desde(_configuration);
            var res = ctx.Resultado;
            var solicitante = string.IsNullOrWhiteSpace(ctx.SolicitanteNombre)
                ? "El área solicitante"
                : ctx.SolicitanteNombre;

            var siguiente = res.Aprobado
                ? "El seleccionado pasa a la programación de su EMO."
                : res.TodosRechazados
                    ? "No quedan finalistas en carrera: el requerimiento volvió a Long list / CVs."
                    : "El proceso continúa con los finalistas que aún están pendientes de decisión.";

            var programarEmo = res.Aprobado && res.WorkerId.HasValue;
            var link = programarEmo
                ? ConstruirLinkProgramarEmoIngreso(res.WorkerId!.Value)
                : ConstruirLinkDetalleRequerimiento(requerimientoId);

            return l.Documento(
                new Layout.Cabecera(
                    "req-finalista", "Decisión de Finalista",
                    $"{Layout.Esc(solicitante)} {Layout.Esc(accion)} a un finalista:"),
                l.Tarjeta(new List<Layout.Fila>
                {
                    new("req-codigo", "Requerimiento", Layout.Esc(ctx.Codigo)),
                    new("req-puesto", "Puesto", Textos.OGuion(ctx.Puesto)),
                    new("req-candidato", "Finalista", Textos.OGuion(res.CandidatoNombre)),
                    new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
                    new("req-proyecto", "Proyecto / Obra", Textos.OGuion(ctx.ProyectoObra)),
                    new("req-estado", "Estado", Textos.OGuion(res.EstadoNombre)),
                }),
                res.Aprobado
                    ? l.Franja("req-check", Layout.Tono.Verde, $"<b>Aprobado.</b> {siguiente}")
                    : l.Franja("req-rechazadas", Layout.Tono.Rojo, $"<b>Rechazado.</b> {siguiente}"),
                l.Boton(programarEmo ? "Programar EMO de ingreso" : "Ver requerimiento", link),
                l.EnlaceDirecto(link));
        }

        // ── Envío de la long list al solicitante ──────────────────────────────
        // Topes de tamaño de la long list. Antes eran 20 MB (tanto el total como cada archivo); se
        // subieron a 3 GB para el total de la petición y a 3 GB para cada archivo individual. Los
        // topes de request de Kestrel/FormOptions (Program.cs) ya están en 10 GB, así que no limitan.
        // OJO: usar el sufijo L (long) — 3 * 1024^3 desborda un int.
        private const long MaxLongListTotalBytes = 3L * 1024 * 1024 * 1024; // 3 GB en total (CVs)
        private const long MaxLongListFileBytes  = 3L * 1024 * 1024 * 1024; // 3 GB por archivo individual
        private static readonly string[] AllowedLongListExt = { ".pdf", ".doc", ".docx" };

        /// <summary>
        /// Formatos aceptados en el "Portafolio/Anexos" del candidato. Es más amplio que el del CV
        /// porque un portafolio suele venir en imágenes o en una presentación, no solo en PDF. No
        /// se aceptan comprimidos (.zip/.rar): los filtros de correo de la organización los
        /// bloquean y el envío de la long list es bloqueante, así que tumbarían todo el envío.
        /// </summary>
        private static readonly string[] AllowedLongListAnexoExt =
            { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>
        /// Tope de lo que puede pesar el conjunto de archivos que viaja ADJUNTO en el correo de la
        /// long list (CVs + anexos). No es una política nuestra sino el límite del proveedor:
        /// Graph rechaza el <c>sendMail</c> cuando el mensaje completo pasa de 4 MB y los adjuntos
        /// van en base64, que infla ~4/3. Se valida acá, antes de subir nada, para que GTH vea qué
        /// archivo achicar en vez del 502 genérico "no se pudo enviar el correo" al final del flujo.
        /// </summary>
        private const long MaxLongListCorreoBytes = 2_800_000; // ~2.8 MB reales ≈ 3.7 MB en base64

        public async Task<EstadoRequerimientoResultDto> EnviarLongList(
            int requerimientoId, List<LongListCandidatoArchivoDto> candidatos, int? userId)
        {
            if (candidatos == null || candidatos.Count == 0)
                throw new AbrilException("Debes cargar al menos un candidato para enviar la long list.", 400);

            // Validar archivos: cada candidato debe traer su CV, en un formato permitido. Los
            // anexos del portafolio son opcionales y admiten más formatos que el CV.
            long total = 0;
            for (int i = 0; i < candidatos.Count; i++)
            {
                var c = candidatos[i];
                var pos = i + 1;
                if (c.CvContent == null || c.CvContent.Length == 0)
                    throw new AbrilException($"Candidato {pos}: falta adjuntar el CV.", 400);
                ValidarLongListArchivo($"CV del candidato {pos}", c.CvFileName, c.CvContent.Length, AllowedLongListExt);
                total += c.CvContent.Length;

                foreach (var anexo in c.Anexos)
                {
                    if (anexo.Content == null || anexo.Content.Length == 0)
                        throw new AbrilException($"Candidato {pos}: un anexo del portafolio llegó vacío.", 400);
                    ValidarLongListArchivo(
                        $"anexo «{anexo.FileName}» del candidato {pos}", anexo.FileName,
                        anexo.Content.Length, AllowedLongListAnexoExt);
                    total += anexo.Content.Length;
                }
            }
            if (total > MaxLongListTotalBytes)
                throw new AbrilException("El tamaño total de los archivos supera el máximo permitido (3 GB).", 400);

            // Tope real del envío: lo que acepta el proveedor de correo con los adjuntos adentro.
            if (total > MaxLongListCorreoBytes)
                throw new AbrilException(
                    $"Los archivos pesan {FormatearMb(total)} en total y el correo admite hasta " +
                    $"{FormatearMb(MaxLongListCorreoBytes)} entre CVs y anexos. Reduce o quita " +
                    "algún anexo del portafolio antes de enviar la long list.", 400);

            // 1) Contexto (valida fase LONG_LIST) — no cambia estado todavía.
            var ctx = await _repo.GetLongListEnvioContexto(requerimientoId);

            // 2) Destinatarios del correo de long list.
            //    El destinatario PRINCIPAL (Para/To) es SIEMPRE el solicitante que registró la
            //    solicitud; la configuración (tipo LONG_LIST) solo aporta principales/copias extra.
            var dest = await _destinatarios.ResolverAsync(CorreoTipoReclutamiento.LongList);

            // Para = solicitante primero + principales configurados; CC = copias que no estén en Para.
            var (principales, copias) = CorreoDestinatariosCombinador.Combinar(ctx.SolicitanteEmail, dest);

            if (principales.Count == 0)
                throw new AbrilException(
                    "No se pudo determinar el correo del solicitante de la long list y no hay " +
                    "destinatarios principales configurados. Verifica que el solicitante tenga " +
                    "un correo registrado o configúralos con el botón «Configuración».", 409);

            // 3) Enviar el correo con los CVs y los anexos adjuntos. Es BLOQUEANTE y va ANTES de
            //    avanzar el estado: si el correo falla, el requerimiento sigue en LONG_LIST y GTH
            //    puede reintentar. Los anexos de cada candidato van detrás de su CV para que en la
            //    lista de adjuntos del correo queden agrupados por candidato.
            var adjuntos = new List<EmailAttachment>(candidatos.Count);
            foreach (var c in candidatos)
            {
                adjuntos.Add(new EmailAttachment
                {
                    FileName    = string.IsNullOrWhiteSpace(c.CvFileName) ? "cv.pdf" : c.CvFileName,
                    ContentType = string.IsNullOrWhiteSpace(c.CvContentType) ? "application/octet-stream" : c.CvContentType,
                    Content     = c.CvContent!,
                });

                foreach (var anexo in c.Anexos)
                {
                    adjuntos.Add(new EmailAttachment
                    {
                        FileName    = string.IsNullOrWhiteSpace(anexo.FileName) ? "anexo" : Path.GetFileName(anexo.FileName),
                        ContentType = string.IsNullOrWhiteSpace(anexo.ContentType) ? "application/octet-stream" : anexo.ContentType,
                        Content     = anexo.Content!,
                    });
                }
            }

            try
            {
                await _email.SendAsync(
                    to:      principales,
                    subject: $"[Reclutamiento] Long list de CVs — {ctx.Codigo} · {ctx.Puesto}",
                    body:    ConstruirCuerpoLongList(requerimientoId, ctx, candidatos),
                    isHtml:  true,
                    cc:      copias.Count > 0 ? copias : null,
                    attachments: adjuntos,
                    sender:  EmailSenders.Gth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló el correo de long list del requerimiento {RequerimientoId}", requerimientoId);
                throw new AbrilException(
                    "No se pudo enviar el correo de la long list. El requerimiento no cambió de estado; reintenta.", 502);
            }

            // 4) Correo enviado: subir los CVs y los anexos a SharePoint y persistir la long list
            //    para que el solicitante pueda revisarla. Se reutiliza la carpeta de reclutamiento
            //    (gth_sustento_folder), organizada en una subcarpeta por requerimiento.
            var carpeta = await ResolverCarpetaRequerimientoAsync(ctx.Codigo);

            var persist = new List<LongListCandidatoPersistDto>(candidatos.Count);
            var indice = 0;
            foreach (var c in candidatos)
            {
                indice++;
                var cvSubida = await SubirArchivoRequerimientoAsync(
                    carpeta, "cv", ctx.Codigo, $"{indice}", c.CvFileName, c.CvContent!, c.CvContentType);

                var item = new LongListCandidatoPersistDto
                {
                    Nombre     = c.Nombre,
                    Comentario = c.Comentario,
                    CvNombre   = cvSubida.FileName,
                    CvUrl      = cvSubida.WebUrl,
                    CvItemId   = cvSubida.ItemId,
                    CvDriveId  = carpeta.DriveId,
                };

                var posAnexo = 0;
                foreach (var anexo in c.Anexos)
                {
                    posAnexo++;
                    var subida = await SubirArchivoRequerimientoAsync(
                        carpeta, "anexo", ctx.Codigo, $"{indice}_{posAnexo}",
                        anexo.FileName, anexo.Content!, anexo.ContentType);

                    item.Anexos.Add(new LongListAnexoPersistDto
                    {
                        Nombre         = subida.FileName,
                        NombreOriginal = Path.GetFileName(anexo.FileName),
                        Url            = subida.WebUrl,
                        ItemId         = subida.ItemId,
                        DriveId        = carpeta.DriveId,
                    });
                }

                persist.Add(item);
            }

            // 5) Persistir los candidatos (reemplazando la long list previa) y avanzar a LONG_LIST_ENVIADA.
            return await _repo.GuardarLongListCandidatos(requerimientoId, persist, userId);
        }

        /// <summary>
        /// Carpeta de SharePoint del requerimiento, donde van TODOS sus archivos: los CVs y anexos
        /// de la long list, los del informe de la entrevista y el CV documentado que sube el
        /// postulante desde su formulario. La resuelve el servicio compartido de archivos, que es
        /// el mismo que usa la página pública del postulante.
        /// </summary>
        private Task<ShareLinkResolveDto> ResolverCarpetaRequerimientoAsync(string codigo) =>
            _archivos.ResolverCarpetaRequerimientoAsync(codigo);

        /// <summary>
        /// Sube un archivo del requerimiento (CV, anexo o archivo del informe) a la carpeta
        /// indicada y devuelve el resultado. <paramref name="pos"/> va como texto porque el CV se
        /// numera por candidato ("3"), el anexo por candidato y posición ("3_2") y los del informe
        /// por id de candidato.
        /// </summary>
        private Task<SharePointUploadResultDto> SubirArchivoRequerimientoAsync(
            ShareLinkResolveDto carpeta, string prefijo, string codigo, string pos,
            string origFileName, byte[] content, string contentType) =>
            _archivos.SubirArchivoRequerimientoAsync(
                carpeta, prefijo, codigo, pos, origFileName, content, contentType);

        private static void ValidarLongListArchivo(
            string etiqueta, string fileName, long length, string[] extensionesPermitidas)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (!extensionesPermitidas.Contains(ext))
                throw new AbrilException(
                    $"El {etiqueta} tiene un formato no permitido. Solo " +
                    $"{string.Join(", ", extensionesPermitidas.Select(e => e.TrimStart('.').ToUpperInvariant()))}.", 400);
            if (length > MaxLongListFileBytes)
                throw new AbrilException($"El {etiqueta} supera el tamaño máximo permitido (3 GB).", 400);
        }

        /// <summary>Tamaño en MB con un decimal, para los mensajes de error de los adjuntos.</summary>
        private static string FormatearMb(long bytes) =>
            $"{bytes / 1024d / 1024d:0.#} MB";

        /// <summary>
        /// Enlace a la revisión de la long list dentro de «Solicitud de personal». Mismo mecanismo
        /// que el correo de aprobación a Gerencia: si el solicitante no tiene sesión, el
        /// <c>authGuard</c> del frontend lo manda al login con esta URL como <c>returnUrl</c> y lo
        /// devuelve acá al entrar. La ruta abre el modal «Revisar long list y CVs» directamente.
        /// </summary>
        private string ConstruirLinkRevisionLongList(int requerimientoId)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/gestion-gth/solicitud-personal/long-list/{requerimientoId}";
        }

        /// <summary>
        /// Cuerpo del correo de la long list al solicitante. Los CVs y los anexos van adjuntos al
        /// correo; la tabla es el índice de lo que trae y el botón lleva a la pantalla donde se
        /// aprueba o rechaza candidato por candidato.
        ///
        /// Bajo cada candidato van los nombres de sus anexos: en el correo todos los adjuntos caen
        /// en una sola lista plana, así que es la única forma de saber de quién es cada archivo.
        /// </summary>
        private string ConstruirCuerpoLongList(
            int requerimientoId, LongListEnvioContextoDto ctx, List<LongListCandidatoArchivoDto> candidatos)
        {
            var l    = Layout.Desde(_configuration);
            var link = ConstruirLinkRevisionLongList(requerimientoId);

            var datos = new List<Layout.Fila>
            {
                new("req-codigo", "Requerimiento", Layout.Esc(ctx.Codigo)),
                new("req-puesto", "Puesto", Textos.OGuion(ctx.Puesto)),
                new("req-area", "Área solicitante", Textos.OGuion(ctx.Area)),
                new("req-proyecto", "Proyecto / Obra", Textos.OGuion(ctx.ProyectoObra)),
            };
            if (ctx.SlaDias.HasValue)
                datos.Add(new("req-plazo", "Plazo estimado", $"{ctx.SlaDias} días"));

            var filas = new List<IReadOnlyList<Layout.Celda>>(candidatos.Count);
            for (var i = 0; i < candidatos.Count; i++)
            {
                var c = candidatos[i];
                var nombre = string.IsNullOrWhiteSpace(c.Nombre) ? $"Candidato {i + 1}" : Layout.Esc(c.Nombre);
                var anexos = c.Anexos.Count == 0
                    ? ""
                    : Textos.Subtexto("Anexos: " + string.Join(", ", c.Anexos.Select(a => Path.GetFileName(a.FileName))));

                filas.Add(new List<Layout.Celda>
                {
                    new((i + 1).ToString()),
                    new(nombre + anexos, Negrita: true),
                    new(Textos.OGuion(c.Comentario)),
                });
            }

            return l.Documento(
                new Layout.Cabecera(
                    "req-longlist", "Long List de CVs",
                    "GTH culminó el filtro de CVs. Los adjuntos van en este correo."),
                l.Tarjeta(datos),
                l.Seccion("req-candidatos", $"Candidatos ({candidatos.Count})"),
                l.Tabla(ColumnasLongList, filas),
                l.Boton("Revisar long list y CVs", link),
                l.EnlaceDirecto(link));
        }

        /// <summary>Columnas de la tabla de la long list (suman los 580px de la tarjeta).</summary>
        private static readonly IReadOnlyList<Layout.Columna> ColumnasLongList = new List<Layout.Columna>
        {
            new("#", 46, Layout.Alineacion.Centro),
            new("Candidato", 200),
            new("Comentario de GTH", 334),
        };

        public async Task<SeguimientoDto> GetSeguimiento(int requerimientoId, int? userId)
        {
            var scope = await ResolverScope(userId, paraGestionar: false);

            var seguimiento = await _repo.GetSeguimiento(requerimientoId, scope);
            if (seguimiento == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);

            // Tarjeta "Aprobación GG" del modal: la consulta vive en el repositorio dueño de
            // gth_aprobacion_gg. Es una lectura chica e indexada; null en los requerimientos
            // anteriores a esta funcionalidad (no pasaron por el paso del GG).
            seguimiento.AprobacionGg = await _aprobacionGgRepo.GetResumenByRequerimiento(requerimientoId);

            return seguimiento;
        }

        // ── Configuración de destinatarios del correo (por tipo: SOLICITUD / LONG_LIST) ─
        public Task<CorreoDestinatariosDto> GetCorreoDestinatarios(string tipoCodigo) =>
            _repo.GetCorreoDestinatarios(tipoCodigo);

        public async Task SaveCorreoDestinatarios(string tipoCodigo, CorreoDestinatariosDto dto, int? userId)
        {
            // Normaliza (trim + minúsculas), valida formato y quita duplicados. Un correo que
            // aparezca en ambas listas se toma como principal (gana Para sobre CC).
            var principales = NormalizarCorreos(dto?.Principales, "principales");
            var copias      = NormalizarCorreos(dto?.Copias, "en copia")
                                .Where(e => !principales.Contains(e))
                                .ToList();

            await _repo.ReplaceCorreoDestinatarios(tipoCodigo, principales, copias, userId);
        }

        private static readonly Regex EmailRegex =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private static List<string> NormalizarCorreos(List<string>? correos, string listaNombre)
        {
            var resultado = new List<string>();
            if (correos == null) return resultado;
            foreach (var raw in correos)
            {
                var email = raw?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email)) continue;
                if (!EmailRegex.IsMatch(email))
                    throw new AbrilException($"El correo «{raw}» (destinatarios {listaNombre}) no es válido.", 400);
                if (!resultado.Contains(email)) resultado.Add(email);
            }
            return resultado;
        }

        public async Task<SolicitudPersonalCreateResultDto> Create(SolicitudPersonalCreateDto dto, int? userId, IFormFile? sustento)
        {
            // Pedir personal es de la jefatura del área: misma regla que para avanzar el proceso.
            await ResolverScope(userId, paraGestionar: true);

            if (dto?.Vacantes == null || dto.Vacantes.Count == 0)
                throw new AbrilException("Debe registrar al menos una vacante.", 400);
            if (dto.Vacantes.Count > 10)
                throw new AbrilException("Una solicitud permite un máximo de 10 vacantes.", 400);

            // La justificación es el sustento que leen el gerente del área y Gerencia General para
            // aprobar, así que sin ella la solicitud no se registra.
            var justificacion = dto.Justificacion?.Trim();
            if (string.IsNullOrWhiteSpace(justificacion))
                throw new AbrilException("Debe escribir la justificación general de la solicitud.", 400);
            if (justificacion.Length > MaxJustificacionLength)
                throw new AbrilException(
                    $"La justificación no puede superar los {MaxJustificacionLength} caracteres.", 400);

            // Catálogo de tipos de documento, solo si hay algún ingreso directo que validar: es lo
            // que dice cuántos dígitos admite cada uno (ver FftDocumento). Una solicitud sin FFT no
            // paga este roundtrip.
            var tiposDocumento = dto.Vacantes.Any(v => v.EsFft)
                ? (await _repo.GetTiposDocumento()).ToDictionary(t => t.Id, t => t.Codigo)
                : new Dictionary<int, string>();

            for (int i = 0; i < dto.Vacantes.Count; i++)
            {
                var v = dto.Vacantes[i];
                var pos = i + 1;

                // Único origen del puesto: el catálogo. El alta de puestos nuevos es tarea de GTH
                // (catálogo de puestos), no de este formulario.
                if (v.PuestoId is null or <= 0)
                    throw new AbrilException($"Vacante {pos}: debe seleccionar un puesto.", 400);

                if (v.TipoRequerimientoId <= 0)   throw new AbrilException($"Vacante {pos}: debe seleccionar el tipo de requerimiento.", 400);
                if (v.ProjectId <= 0)             throw new AbrilException($"Vacante {pos}: debe seleccionar un proyecto/obra.", 400);

                // Salario bruto mensual: acá solo se valida el rango de lo que venga. Si es
                // OBLIGATORIO o no depende del tipo de requerimiento —los reemplazos ya no lo
                // piden— y el código del tipo lo resuelve el repositorio, que ya lo trae para la
                // regla del trabajador reemplazado. El tope es el de la columna (numeric(12,2)) y
                // ataja el dedazo de escribir el sueldo con los céntimos pegados.
                if (v.SalarioBrutoMensual.HasValue)
                {
                    if (v.SalarioBrutoMensual <= 0)
                        throw new AbrilException(
                            $"Vacante {pos}: el salario bruto mensual debe ser mayor que cero.", 400);
                    if (v.SalarioBrutoMensual > MaxSalarioBrutoMensual)
                        throw new AbrilException(
                            $"Vacante {pos}: el salario bruto mensual no puede superar S/ {MaxSalarioBrutoMensual:N2}.", 400);

                    // Se guarda con 2 decimales: la columna es numeric(12,2) y redondear acá deja
                    // el dato igual en la BD y en el correo que ve el gerente.
                    v.SalarioBrutoMensual = Math.Round(v.SalarioBrutoMensual.Value, 2, MidpointRounding.AwayFromZero);
                }

                // FFT: el solicitante ya sabe a quién quiere, así que la vacante no vale sin ese
                // nombre, ese DNI y ese correo — el correo es el único destinatario posible del
                // formulario (el siguiente y casi único paso del flujo) y el DNI es la llave con la
                // que el candidato entra a la base maestra apenas se registra el pedido. Lo que
                // llegue en una vacante que NO es FFT se descarta: el formulario no lo muestra y
                // guardarlo dejaría un candidato fantasma en un proceso que sí va a publicar la
                // vacante.
                if (!v.EsFft)
                {
                    v.FftCandidatoNombre    = null;
                    v.FftCandidatoCorreo    = null;
                    v.FftCandidatoDocumento = null;
                    v.FftTipoDocumentoId    = null;
                    continue;
                }

                var fftNombre = v.FftCandidatoNombre?.Trim();
                if (string.IsNullOrWhiteSpace(fftNombre))
                    throw new AbrilException(
                        $"Vacante {pos}: debe indicar el nombre completo del candidato FFT.", 400);
                if (fftNombre.Length > MaxFftNombreLength)
                    throw new AbrilException(
                        $"Vacante {pos}: el nombre del candidato FFT no puede superar los {MaxFftNombreLength} caracteres.", 400);

                // El tipo decide cuántos dígitos admite el número, así que se valida primero.
                if (v.FftTipoDocumentoId is null or <= 0
                    || !tiposDocumento.TryGetValue(v.FftTipoDocumentoId.Value, out var tipoDocCodigo))
                    throw new AbrilException(
                        $"Vacante {pos}: debe indicar el tipo de documento del candidato FFT.", 400);

                // Se limpian los separadores que se copian junto con el número ("12.345.678",
                // "12 345 678") antes de exigir el largo: es un dedazo de tipeo, no un documento
                // mal declarado, y rechazarlo obligaría al solicitante a adivinar qué está mal.
                var fftDocumento = FftDocumento.SoloDigitos(v.FftCandidatoDocumento);
                if (!FftDocumento.EsValido(tipoDocCodigo, fftDocumento))
                    throw new AbrilException(
                        $"Vacante {pos}: el {tipoDocCodigo} del candidato FFT debe tener "
                        + $"{FftDocumento.ReglaTexto(tipoDocCodigo)}.", 400);

                var fftCorreo = v.FftCandidatoCorreo?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(fftCorreo) || !FftCorreoRegex.IsMatch(fftCorreo))
                    throw new AbrilException(
                        $"Vacante {pos}: debe indicar un correo personal válido del candidato FFT.", 400);

                v.FftCandidatoNombre    = fftNombre;
                v.FftCandidatoDocumento = fftDocumento;
                v.FftCandidatoCorreo    = fftCorreo;
            }

            // Área del solicitante: se deriva del usuario autenticado (no se confía en el cliente).
            string? areaNombre = null;
            int? areaScopeId = null, workerId = null;
            if (userId.HasValue)
                (areaNombre, areaScopeId, workerId) = await _repo.ResolveSolicitante(userId.Value);

            var solicitud = new GthSolicitud
            {
                AreaNombre          = areaNombre,
                AreaScopeId         = areaScopeId,
                SolicitanteUserId   = userId,
                SolicitanteWorkerId = workerId,
                Justificacion       = justificacion,
            };

            // Sustento (opcional): validar y subir a SharePoint ANTES de persistir.
            if (sustento != null && sustento.Length > 0)
                await SubirSustentoAsync(sustento, solicitud);

            // Qué hay que arrancar lo decide cada VACANTE, no la solicitud: un ingreso directo no
            // lo aprueba nadie —quien pide ya nombró a la persona, no hay nada que decidir— y una
            // solicitud puede mezclar los dos. Con las dos clases adentro salen los dos correos:
            // el de aprobación con las vacantes normales y el aviso a GTH con los ingresos directos.
            var hayFft        = dto.Vacantes.Any(v => v.EsFft);
            var hayAprobables = dto.Vacantes.Any(v => !v.EsFft);

            var result = await _repo.Create(solicitud, dto.Vacantes, userId);
            result.AprobacionGgOmitida = !hayAprobables;
            result.HayIngresoDirecto   = hayFft;

            // Los correos que arrancan el flujo. Ninguno bloquea la creación: si falla, la solicitud
            // ya quedó registrada y el solicitante la reenvía desde su panel (el de aprobación) o la
            // ve igual en la bandeja de GTH (el del ingreso directo).
            //
            // La solicitud va primero a quien tenga que aprobarla, NO a GTH: de las vacantes
            // normales GTH se entera recién cuando estén aprobadas. Las FFT no esperan a nadie, así
            // que su aviso a GTH sale ya.
            var correoAprobacion = !hayAprobables
                || await _aprobacionGg.EnviarSolicitudAGerencia(result.SolicitudId, userId);
            var correoIngresoDirecto = !hayFft
                || await _aprobacionGg.EnviarIngresoDirectoAGth(result.SolicitudId, userId);

            result.CorreoGerenciaEnviado = correoAprobacion && correoIngresoDirecto;

            return result;
        }

        private async Task SubirSustentoAsync(IFormFile sustento, GthSolicitud solicitud)
        {
            var ext = Path.GetExtension(sustento.FileName).ToLowerInvariant();
            if (!AllowedSustentoExt.Contains(ext))
                throw new AbrilException("Formato de sustento no permitido. Solo PDF, DOC, DOCX, XLS y XLSX.", 400);
            if (sustento.Length > MaxSustentoBytes)
                throw new AbrilException("El sustento supera el tamaño máximo permitido (10 MB).", 400);

            // Carpeta destino: link de SharePoint definido en BD (gth_sustento_folder).
            // Se configura por base de datos: dev y prod apuntan a bibliotecas distintas.
            var folderUrl = await _repo.GetSustentoFolderUrl();
            if (string.IsNullOrWhiteSpace(folderUrl))
                throw new AbrilException("No está configurada la carpeta de sustentos de reclutamiento.", 500);

            var carpeta = await _sharePoint.ResolveSharePointFolderUrlAsync(folderUrl);
            if (carpeta == null || !carpeta.IsFolder)
                throw new AbrilException("No se pudo resolver la carpeta de sustentos en SharePoint.", 502);

            var safeName = SanitizeFilename(Path.GetFileNameWithoutExtension(sustento.FileName));
            var stamp    = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var filename = $"sustento_{stamp}_{safeName}{ext}";

            try
            {
                using var stream = sustento.OpenReadStream();
                var result = await _sharePoint.UploadToOneDriveFolderAsync(
                    carpeta.DriveId, carpeta.ItemId, filename, stream,
                    sustento.ContentType ?? "application/octet-stream",
                    autoRenameOnLock: true);

                if (result?.WebUrl is null)
                    throw new AbrilException("No se pudo subir el sustento a SharePoint.", 502);

                solicitud.SustentoNombre  = result.FileName ?? filename;
                solicitud.SustentoUrl     = result.WebUrl;
                solicitud.SustentoItemId  = result.ItemId;
                solicitud.SustentoDriveId = carpeta.DriveId;
            }
            catch (AbrilException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la subida del sustento de la solicitud de personal");
                throw new AbrilException("Error al subir el sustento a SharePoint.", 502);
            }
        }

        private static string SanitizeFilename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "sustento";
            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '#', '%', '&', '+' }).ToHashSet();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return clean.Length > 60 ? clean.Substring(0, 60) : clean;
        }
    }
}
