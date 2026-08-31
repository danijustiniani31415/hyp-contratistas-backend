using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models;
using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Shared.Services;
using Abril_Backend.Features.GestionAdministrativa.Shared.Email;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Services
{
    public class SolicitudSalidaService : ISolicitudSalidaService
    {
        private readonly ISolicitudSalidaRepository    _repo;
        private readonly IJefeRevisorResolver          _revisorResolver;
        private readonly ICorreoSalidaRecipientResolver _correoResolver;
        private readonly ISolicitudSalidaTokenService  _tokenService;
        private readonly IEmailService                 _emailService;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IConfiguration                _configuration;
        private readonly ILogger<SolicitudSalidaService> _logger;
        private readonly IGraphSharePointService        _sharePointService;
        private readonly IConsolidadoS10Service         _consolidadoService;

        public SolicitudSalidaService(
            ISolicitudSalidaRepository repo,
            IJefeRevisorResolver revisorResolver,
            ICorreoSalidaRecipientResolver correoResolver,
            ISolicitudSalidaTokenService tokenService,
            IEmailService emailService,
            IDbContextFactory<AppDbContext> factory,
            IConfiguration configuration,
            ILogger<SolicitudSalidaService> logger,
            IGraphSharePointService sharePointService,
            IConsolidadoS10Service consolidadoService)
        {
            _repo             = repo;
            _revisorResolver  = revisorResolver;
            _correoResolver   = correoResolver;
            _tokenService     = tokenService;
            _emailService     = emailService;
            _factory          = factory;
            _configuration    = configuration;
            _logger           = logger;
            _sharePointService = sharePointService;
            _consolidadoService = consolidadoService;
        }

        public async Task<SolicitudSalidaFormDataDto> GetFormData(int? userId)
        {
            var data = await _repo.GetFormData();

            if (userId.HasValue)
            {
                try
                {
                    using var ctx = _factory.CreateDbContext();
                    var solicitante = await ctx.Worker
                        .Where(w => w.Person != null && w.Person.UserId == userId.Value)
                        .FirstOrDefaultAsync();
                    if (solicitante != null)
                    {
                        // Correo del revisor (best-effort): workers_revisores → fallback GTH.
                        var revisor = await _revisorResolver.ResolveAsync(solicitante.Id);
                        data.AprobadorEmail = revisor?.Email;

                        // Si el trabajador es TI, exponer el catálogo de trayectos para que el
                        // frontend muestre el monto automático al seleccionar origen+destino.
                        if (string.Equals(solicitante.Subarea, "Tecnología de la Información", StringComparison.OrdinalIgnoreCase))
                        {
                            data.EsTI = true;
                            data.TrayectosCatalogo = await ctx.GaTrayecto
                                .Where(g => g.Activo)
                                .Select(g => new TrayectoCatalogoOptionDto
                                {
                                    LugarOrigenId  = g.LugarOrigenId,
                                    LugarDestinoId = g.LugarDestinoId,
                                    Monto          = g.Monto,
                                })
                                .ToListAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo resolver el aprobador/catálogo para userId {UserId}", userId);
                }
            }

            return data;
        }

        public Task<List<SolicitudSalidaListItemDto>> GetByUserId(int userId, SolicitudSalidaFiltersDto? filters = null) =>
            _repo.GetByUserId(userId, filters);

        public Task<SolicitudSalidaFilterDataDto> GetFilterData(int userId) => _repo.GetFilterData(userId);

        public async Task<int> Create(SolicitudSalidaCreateDto dto, int? userId, IReadOnlyList<(int TrayectoIndex, IFormFile File)>? adjuntos = null)
        {
            if (dto.Trayectos == null || dto.Trayectos.Count == 0)
                throw new AbrilException("Debe registrar al menos un trayecto.", 400);

            // Validar cada trayecto
            for (int i = 0; i < dto.Trayectos.Count; i++)
            {
                var t = dto.Trayectos[i];
                var pos = i + 1;
                if (t.HoraRetorno.HasValue && t.HoraRetorno.Value <= t.HoraSalida)
                    throw new AbrilException($"Trayecto {pos}: la hora de retorno debe ser posterior a la hora de salida.", 400);

                var tieneMotivoId    = t.MotivoId.HasValue;
                var tieneMotivoLibre = !string.IsNullOrWhiteSpace(t.MotivoLibre);
                if (!tieneMotivoId && !tieneMotivoLibre)
                    throw new AbrilException($"Trayecto {pos}: debe indicar un motivo.", 400);

                var tieneOrigenId    = t.LugarOrigenId.HasValue;
                var tieneOrigenLibre = !string.IsNullOrWhiteSpace(t.LugarOrigenLibre);
                if (!tieneOrigenId && !tieneOrigenLibre)
                    throw new AbrilException($"Trayecto {pos}: debe indicar un lugar de origen.", 400);

                var tieneDestinoId    = t.LugarDestinoId.HasValue;
                var tieneDestinoLibre = !string.IsNullOrWhiteSpace(t.LugarDestinoLibre);
                if (!tieneDestinoId && !tieneDestinoLibre)
                    throw new AbrilException($"Trayecto {pos}: debe indicar un lugar de destino.", 400);

                if (tieneOrigenId && tieneDestinoId && t.LugarOrigenId == t.LugarDestinoId)
                    throw new AbrilException($"Trayecto {pos}: el lugar de origen y el lugar de destino no pueden ser iguales.", 400);
            }

            // 1. Exigencias de los motivos elegidos (un solo roundtrip): qué trayectos
            //    necesitan documento adjunto y cuáles necesitan motivo adicional.
            var exigencias = await CargarExigenciasMotivosAsync(dto);

            // 2. Motivo adicional: se valida antes de subir nada a SharePoint para que una
            //    solicitud inválida no deje archivos huérfanos en la carpeta.
            for (int i = 0; i < dto.Trayectos.Count; i++)
            {
                var t = dto.Trayectos[i];
                var exige = t.MotivoId.HasValue
                         && exigencias.TryGetValue(t.MotivoId.Value, out var e)
                         && e.RequiereMotivoAdicional;

                if (exige && string.IsNullOrWhiteSpace(t.MotivoAdicional))
                    throw new AbrilException($"Trayecto {i + 1}: el motivo seleccionado requiere que indiques un motivo adicional.", 400);

                // El detalle pertenece al motivo que lo exige: si el motivo no lo pide, no se
                // guarda nada aunque el cliente lo haya mandado (ej. cambió de motivo y no limpió).
                t.MotivoAdicional = exige ? t.MotivoAdicional!.Trim() : null;
            }

            // 3. Subir los documentos adjuntos ANTES de persistir: los motivos con
            //    requiere_adjunto exigen archivo, y así una falla de subida no deja
            //    solicitudes creadas sin su adjunto obligatorio.
            var adjuntosPorIndice = await SubirAdjuntosAsync(dto, adjuntos, exigencias);

            // 4. Persistir solicitud + trayectos (con las referencias de los adjuntos)
            var (solicitud, trayectos, solicitante) = await _repo.Create(dto, userId, adjuntosPorIndice);

            // 5. Resolver aprobador + enviar emails (best-effort)
            //    Hacemos UNA sola resolución de trayectos en memoria y la compartimos
            //    entre los dos correos (al aprobador y de confirmación al solicitante).
            try
            {
                using var ctx = _factory.CreateDbContext();
                var nombreSolicitante = await ctx.Person
                    .Where(p => p.PersonId == solicitante.PersonId)
                    .Select(p => p.FullName)
                    .FirstOrDefaultAsync() ?? "Trabajador";

                var (trayectosResueltos, mostrarRecordatorio) = await ResolveTrayectosForEmailAsync(ctx, trayectos);

                // 5a. Email al revisor resuelto (workers_revisores → fallback GTH). Se guarda
                //     el correo al que se envió (enviado_a_correo); el aprobador real
                //     (aprobador_worker_id / aprobador_area_scope_id) se llena recién al
                //     momento de la decisión. enviado_a_correo se guarda apenas se resuelve el
                //     revisor aunque su interruptor esté apagado y no le llegue el correo: sigue
                //     siendo el revisor asignado y de ahí sale su visibilidad en Gestión de Salidas.
                var revisor = await _revisorResolver.ResolveAsync(solicitante.Id);
                var aprobadorEmail = revisor?.Email;
                if (!string.IsNullOrWhiteSpace(aprobadorEmail))
                    await _repo.SetEnviadoACorreo(solicitud.Id, aprobadorEmail);
                else
                    _logger.LogWarning(
                        "No se pudo resolver revisor para solicitud {SolicitudId} (worker {WorkerId}): sin revisores en workers_revisores y sin correo GTH configurado en area_scope.email",
                        solicitud.Id, solicitante.Id);

                var enviadoRevisorA = await SendNotificacionAprobadorAsync(
                    solicitud, trayectosResueltos, mostrarRecordatorio, aprobadorEmail, nombreSolicitante, adjuntos);

                // 5b. Email de confirmación al solicitante (al mismo usuario que registró la solicitud)
                if (userId.HasValue)
                {
                    try
                    {
                        await SendConfirmacionSolicitanteAsync(ctx, solicitud, trayectosResueltos, mostrarRecordatorio, nombreSolicitante, userId.Value, aprobadorEmail, enviadoRevisorA);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enviando confirmación al solicitante para solicitud {SolicitudId}", solicitud.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando emails para solicitud {SolicitudId}", solicitud.Id);
            }

            return solicitud.Id;
        }

        /// <summary>
        /// Lee de una sola vez lo que exigen los motivos del catálogo elegidos en la solicitud:
        /// documento adjunto y/o motivo adicional. Devuelve un diccionario motivoId → exigencias
        /// (vacío si todos los trayectos usan "Otro motivo").
        /// </summary>
        private async Task<Dictionary<int, (bool RequiereAdjunto, bool RequiereMotivoAdicional)>>
            CargarExigenciasMotivosAsync(SolicitudSalidaCreateDto dto)
        {
            var motivoIds = dto.Trayectos.Where(t => t.MotivoId.HasValue).Select(t => t.MotivoId!.Value).Distinct().ToList();
            if (motivoIds.Count == 0) return new();

            using var ctx = _factory.CreateDbContext();
            var filas = await ctx.GaMotivoSalida
                .Where(m => motivoIds.Contains(m.Id))
                .Select(m => new { m.Id, m.RequiereAdjunto, m.RequiereMotivoAdicional })
                .ToListAsync();

            return filas.ToDictionary(m => m.Id, m => (m.RequiereAdjunto, m.RequiereMotivoAdicional));
        }

        /// <summary>
        /// Valida y sube los documentos adjuntos de la solicitud (N por trayecto cuyo motivo
        /// tenga requiere_adjunto = true) a la carpeta configurada de SharePoint/OneDrive
        /// (ga_adjunto_folder). Devuelve el resultado por índice de trayecto (0-based) —
        /// cada trayecto puede tener varios— o null si la solicitud no trae ningún adjunto ni lo necesita.
        /// </summary>
        private async Task<Dictionary<int, List<TrayectoAdjuntoSubidoDto>>?> SubirAdjuntosAsync(
            SolicitudSalidaCreateDto dto,
            IReadOnlyList<(int TrayectoIndex, IFormFile File)>? adjuntos,
            IReadOnlyDictionary<int, (bool RequiereAdjunto, bool RequiereMotivoAdicional)> exigencias)
        {
            var files = (adjuntos ?? Array.Empty<(int, IFormFile)>())
                .Where(a => a.File != null && a.File.Length > 0)
                .ToList();

            if (files.Any(a => a.TrayectoIndex < 0 || a.TrayectoIndex >= dto.Trayectos.Count))
                throw new AbrilException("Adjunto asociado a un trayecto inexistente.", 400);

            // Agrupamos por índice de trayecto: un trayecto puede traer N documentos.
            var porIndice = files
                .GroupBy(a => a.TrayectoIndex)
                .ToDictionary(g => g.Key, g => g.Select(a => a.File).ToList());

            // Motivos que exigen adjunto: todo trayecto con uno de esos motivos debe traer al menos un archivo.
            for (int i = 0; i < dto.Trayectos.Count; i++)
            {
                var t = dto.Trayectos[i];
                if (t.MotivoId.HasValue &&
                    exigencias.TryGetValue(t.MotivoId.Value, out var e) && e.RequiereAdjunto &&
                    (!porIndice.TryGetValue(i, out var lst) || lst.Count == 0))
                    throw new AbrilException($"Trayecto {i + 1}: el motivo seleccionado requiere al menos un documento adjunto.", 400);
            }

            if (porIndice.Count == 0) return null;

            // Validar tipos permitidos (documento de prueba: PDF o imagen).
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
            foreach (var f in porIndice.Values.SelectMany(l => l))
            {
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                    throw new AbrilException($"Tipo de archivo no permitido: {f.FileName}. Solo PDF/JPG/PNG/WEBP.", 400);
            }

            // Carpeta destino configurada (Configuración → Carpeta Adjuntos).
            string driveId, folderId;
            using (var ctx = _factory.CreateDbContext())
            {
                var carpeta = await ctx.GaAdjuntoFolder
                    .Where(f => f.State && f.Active)
                    .OrderBy(f => f.GaAdjuntoFolderId)
                    .Select(f => new { f.DriveId, f.FolderId })
                    .FirstOrDefaultAsync()
                    ?? throw new AbrilException(
                        "No se ha configurado la carpeta donde guardar los documentos adjuntos. " +
                        "Pide al administrador configurarla en Configuración → Carpeta Adjuntos.", 409);
                driveId = carpeta.DriveId;
                folderId = carpeta.FolderId;
            }

            var resultado = new Dictionary<int, List<TrayectoAdjuntoSubidoDto>>();
            try
            {
                foreach (var (idx, lista) in porIndice.OrderBy(kv => kv.Key))
                {
                    var subidos = new List<TrayectoAdjuntoSubidoDto>(lista.Count);
                    for (int n = 0; n < lista.Count; n++)
                    {
                        var f        = lista[n];
                        var ext      = Path.GetExtension(f.FileName).ToLowerInvariant();
                        var safeName = SanitizeFilename(Path.GetFileNameWithoutExtension(f.FileName));
                        var stamp    = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                        var filename = $"adjunto_{stamp}_t{idx + 1}_{n + 1}_{safeName}{ext}";

                        using var stream = f.OpenReadStream();
                        var result = await _sharePointService.UploadToOneDriveFolderAsync(
                            driveId, folderId, filename, stream,
                            f.ContentType ?? "application/octet-stream",
                            autoRenameOnLock: true);

                        if (result?.WebUrl is null)
                            throw new AbrilException($"No se pudo subir el documento {f.FileName}.", 502);

                        subidos.Add(new TrayectoAdjuntoSubidoDto
                        {
                            Url      = result.WebUrl,
                            ItemId   = result.ItemId,
                            DriveId  = driveId,
                            Filename = result.FileName ?? filename,
                        });
                    }
                    resultado[idx] = subidos;
                }
            }
            catch (AbrilException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la subida de adjuntos de la solicitud de salida");
                throw new AbrilException("Error al subir los documentos adjuntos a SharePoint.", 502);
            }

            return resultado;
        }

        // ── Aprobar / Rechazar desde el email ────────────────────────────────

        public async Task<string> ProcessAprobarFromEmail(string token)
        {
            var payload = _tokenService.Validate(token);
            if (payload == null || payload.Action != SolicitudSalidaAction.Aprobar)
                return RenderResultPage("Enlace inválido o expirado", "El enlace ya no es válido. Solicita uno nuevo al trabajador.", isSuccess: false);

            var s = await _repo.Aprobar(payload.SolicitudId);
            if (s == null)
                return RenderResultPage("Solicitud ya procesada", "Esta solicitud ya había sido aprobada o rechazada anteriormente.", isSuccess: false);

            // Notificar al solicitante (best-effort, no rompe la respuesta HTML)
            await NotifySolicitanteAprobada(s.Id);

            return RenderResultPage("Solicitud aprobada", $"Has aprobado la solicitud de salida #{s.Id}.", isSuccess: true);
        }

        public async Task<string> ProcessRechazarFromEmail(string token, string? motivoRechazo)
        {
            var payload = _tokenService.Validate(token);
            if (payload == null || payload.Action != SolicitudSalidaAction.Rechazar)
                return RenderResultPage("Enlace inválido o expirado", "El enlace ya no es válido. Solicita uno nuevo al trabajador.", isSuccess: false);

            var s = await _repo.Rechazar(payload.SolicitudId, motivoRechazo);
            if (s == null)
                return RenderResultPage("Solicitud ya procesada", "Esta solicitud ya había sido aprobada o rechazada anteriormente.", isSuccess: false);

            // Notificar al solicitante (best-effort, no rompe la respuesta HTML)
            await NotifySolicitanteRechazada(s.Id);

            return RenderResultPage("Solicitud rechazada", $"Has rechazado la solicitud de salida #{s.Id}.", isSuccess: true);
        }

        public string RenderRechazarForm(string token)
        {
            var payload = _tokenService.Validate(token);
            if (payload == null || payload.Action != SolicitudSalidaAction.Rechazar)
                return RenderResultPage("Enlace inválido o expirado", "El enlace ya no es válido.", isSuccess: false);

            var safeToken = WebUtility.HtmlEncode(token);
            return $@"<!DOCTYPE html>
<html lang=""es""><head><meta charset=""utf-8""><title>Rechazar solicitud</title>
<style>
  body {{ font-family: Segoe UI, Arial, sans-serif; background:#f5f5f5; margin:0; padding:40px; }}
  .card {{ max-width:520px; margin:0 auto; background:#fff; padding:32px; border-radius:12px; box-shadow:0 2px 12px rgba(0,0,0,.06); }}
  h1 {{ color:#D30000; margin-top:0; }}
  textarea {{ width:100%; min-height:120px; padding:12px; border:1px solid #E2E2E2; border-radius:8px; font:inherit; box-sizing:border-box; }}
  button {{ margin-top:16px; background:#D30000; color:#fff; border:0; padding:12px 24px; border-radius:8px; cursor:pointer; font-size:14px; }}
  button:hover {{ background:#a50000; }}
</style></head><body><div class=""card"">
  <h1>Rechazar solicitud #{payload.SolicitudId}</h1>
  <p>Indica el motivo del rechazo (opcional):</p>
  <form method=""post"" action=""rechazar"">
    <input type=""hidden"" name=""token"" value=""{safeToken}"" />
    <textarea name=""motivoRechazo"" placeholder=""Ej: La fecha coincide con una reunión importante...""></textarea>
    <button type=""submit"">Confirmar rechazo</button>
  </form>
</div></body></html>";
        }

        // ── Helpers internos ─────────────────────────────────────────────────

        /// <summary>
        /// Resuelve los trayectos de la entidad a tuplas listas para embeber en el cuerpo del email.
        /// Hace lookups por motivo y por lugar (origen/destino) para mostrar nombres legibles. Los
        /// motivos con motivo adicional se muestran como "Motivo — detalle": el correo tiene una
        /// sola fila de motivo y el detalle es justamente lo que el revisor necesita leer.
        /// Devuelve además si corresponde mostrar el recordatorio de recuperación de horas:
        /// solo se muestra cuando al menos un trayecto tiene un motivo del catálogo de hora
        /// exacta. Los motivos de hora estimada y el motivo libre (personalizado) quedan
        /// excluidos a propósito y nunca lo disparan — misma regla que el formulario.
        /// </summary>
        private static async Task<(List<(int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino)> Trayectos, bool MostrarRecordatorio)>
            ResolveTrayectosForEmailAsync(AppDbContext ctx, List<GaSolicitudTrayecto> trayectos)
        {
            var resueltos = new List<(int, string, string, string, string, string)>();
            var algunaHoraExacta = false;
            foreach (var t in trayectos.OrderBy(t => t.Orden))
            {
                string motivo;
                if (t.MotivoId.HasValue)
                {
                    var m = await ctx.GaMotivoSalida
                        .Where(m => m.Id == t.MotivoId.Value)
                        .Select(m => new { m.Descripcion, m.EsHoraEstimada })
                        .FirstOrDefaultAsync();
                    motivo = m?.Descripcion ?? "—";
                    if (!string.IsNullOrWhiteSpace(t.MotivoAdicional))
                        motivo = $"{motivo} — {t.MotivoAdicional}";
                    if (m == null || !m.EsHoraEstimada) algunaHoraExacta = true;
                }
                else
                {
                    // Motivo personalizado (libre): NO dispara el recordatorio de recuperación
                    // de horas — se omite tanto en el formulario como en los correos.
                    motivo = t.MotivoLibre ?? "—";
                }

                var origen  = await ResolveLugarDisplay(ctx, t.LugarOrigenId,  t.LugarOrigenLibre);
                var destino = await ResolveLugarDisplay(ctx, t.LugarDestinoId, t.LugarDestinoLibre);
                var horaRet = t.HoraRetorno.HasValue ? t.HoraRetorno.Value.ToString("HH:mm") : "Sin retorno";
                resueltos.Add((t.Orden + 1, t.HoraSalida.ToString("HH:mm"), horaRet, motivo, origen, destino));
            }
            return (resueltos, algunaHoraExacta);
        }

        /// <summary>
        /// Envía el correo con los botones de aprobar/rechazar. El revisor resuelto es su
        /// destinatario principal, pero la configuración manda: si el correo está apagado, o si
        /// el revisor está apagado y no hay ningún destinatario configurado, no se envía nada.
        /// Devuelve los correos a los que realmente se envió (vacío si no se envió).
        /// </summary>
        private async Task<List<string>> SendNotificacionAprobadorAsync(
            GaSolicitudSalida solicitud,
            List<(int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino)> trayectos,
            bool mostrarRecordatorio,
            string? aprobadorEmail,
            string nombreSolicitante,
            IReadOnlyList<(int TrayectoIndex, IFormFile File)>? adjuntos = null)
        {
            // El revisor (To) nunca se excluye; las exclusiones solo aplican a los correos en copia.
            var envio = await _correoResolver.ResolveEnvioAsync(
                CorreoEventoCodigos.Revisor,
                string.IsNullOrWhiteSpace(aprobadorEmail) ? null : new List<string> { aprobadorEmail });

            if (!envio.Enviar)
            {
                _logger.LogInformation(
                    "Correo al revisor no enviado para solicitud {SolicitudId}: el correo está apagado o no quedó ningún destinatario configurado.",
                    solicitud.Id);
                return new List<string>();
            }

            // Los documentos adjuntos de la solicitud van SOLO en este correo (revisor); la
            // confirmación al solicitante y demás correos no los llevan. Se arman recién acá
            // para no leer los archivos si al final no se envía nada.
            var adjuntosEmail = await BuildAdjuntosEmailAsync(adjuntos);

            var tokenAprobar  = _tokenService.Generate(solicitud.Id, SolicitudSalidaAction.Aprobar,  TimeSpan.FromDays(30));
            var tokenRechazar = _tokenService.Generate(solicitud.Id, SolicitudSalidaAction.Rechazar, TimeSpan.FromDays(30));

            var backendUrl = (_configuration["BackendSettings:PublicUrl"] ?? "http://localhost:5236").TrimEnd('/');
            var basePath   = "/api/v1/gestion-administrativa/solicitud-salidas";
            var urlAprobar  = $"{backendUrl}{basePath}/aprobar?token={WebUtility.UrlEncode(tokenAprobar)}";
            var urlRechazar = $"{backendUrl}{basePath}/rechazar?token={WebUtility.UrlEncode(tokenRechazar)}";

            var body    = BuildEmailBody(nombreSolicitante, solicitud.FechaSalida, trayectos, mostrarRecordatorio, urlAprobar, urlRechazar);
            var subject = $"Solicitud de salida - {nombreSolicitante} - {solicitud.FechaSalida:dd/MM/yyyy}";

            await _emailService.SendAsync(
                to: envio.Para,
                subject: subject,
                body: body,
                isHtml: true,
                cc: envio.Copia.Count > 0 ? envio.Copia : null,
                attachments: adjuntosEmail);

            return envio.Para;
        }

        /// <summary>
        /// Convierte los documentos adjuntos de la solicitud (motivos con requiere_adjunto)
        /// en adjuntos de correo. Null si la solicitud no trae ninguno.
        /// </summary>
        private static async Task<List<EmailAttachment>?> BuildAdjuntosEmailAsync(
            IReadOnlyList<(int TrayectoIndex, IFormFile File)>? adjuntos)
        {
            var files = (adjuntos ?? Array.Empty<(int, IFormFile)>())
                .Where(a => a.File != null && a.File.Length > 0)
                .OrderBy(a => a.TrayectoIndex)
                .ToList();
            if (files.Count == 0) return null;

            var result = new List<EmailAttachment>(files.Count);
            foreach (var (_, f) in files)
            {
                using var ms = new MemoryStream();
                using (var stream = f.OpenReadStream())
                    await stream.CopyToAsync(ms);
                result.Add(new EmailAttachment
                {
                    FileName    = Path.GetFileName(f.FileName),
                    ContentType = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType,
                    Content     = ms.ToArray(),
                });
            }
            return result;
        }

        private async Task SendConfirmacionSolicitanteAsync(
            AppDbContext ctx,
            GaSolicitudSalida solicitud,
            List<(int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino)> trayectos,
            bool mostrarRecordatorio,
            string nombreSolicitante,
            int userId,
            string? aprobadorEmail,
            List<string> enviadoRevisorA)
        {
            var emailSolicitante = await ctx.User
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(emailSolicitante))
            {
                _logger.LogWarning(
                    "No se envió email de confirmación para solicitud {SolicitudId}: el usuario {UserId} no tiene email registrado.",
                    solicitud.Id, userId);
                return;
            }

            var envio = await _correoResolver.ResolveEnvioAsync(
                CorreoEventoCodigos.Confirmacion,
                new List<string> { emailSolicitante },
                await GetRecepcionRole52Async(ctx));

            if (!envio.Enviar)
            {
                _logger.LogInformation(
                    "Correo de confirmación no enviado para solicitud {SolicitudId}: el correo está apagado o no quedó ningún destinatario configurado.",
                    solicitud.Id);
                return;
            }

            var numeroUsuario = await GetUserSolicitudNumeroAsync(ctx, solicitud.WorkerId, solicitud.Id);

            var body    = BuildEmailConfirmacionSolicitante(
                nombreSolicitante, numeroUsuario, solicitud.FechaSalida, trayectos, mostrarRecordatorio,
                enviadoRevisorA, aprobadorEmail);
            var subject = $"Tu solicitud de salida #{numeroUsuario} está en revisión - {solicitud.FechaSalida:dd/MM/yyyy}";

            await _emailService.SendAsync(
                to: envio.Para,
                subject: subject,
                body: body,
                isHtml: true,
                cc: envio.Copia.Count > 0 ? envio.Copia : null);
        }

        // ── CC recepción ────────────────────────────────────────────────────
        private const int RoleIdRecepcion = 52;

        /// <summary>
        /// Base dinámica del CC de los correos al solicitante: los <c>worker.email_corporativo</c> de
        /// todos los users con rol id 52 (USUARIO DE RECEPCIÓN). Los antiguos correos fijos
        /// (<c>recepcionnm@abril.pe</c> y GTH <c>gthnm@abril.pe</c>) ya no se hardcodean aquí: ahora son
        /// reglas editables en ga_correo_regla y los agrega <see cref="ICorreoSalidaRecipientResolver"/>.
        /// </summary>
        private static async Task<List<string>> GetRecepcionRole52Async(AppDbContext ctx)
        {
            return await (
                from ur in ctx.UserRole
                where ur.RoleId == RoleIdRecepcion && ur.State
                join p in ctx.Person  on (int?)ur.UserId  equals p.UserId
                join w in ctx.Worker  on (int?)p.PersonId equals w.PersonId
                where w.EmailCorporativo != null && w.EmailCorporativo != ""
                select w.EmailCorporativo!
            ).Distinct().ToListAsync();
        }

        /// <summary>
        /// Devuelve el número de orden de esta solicitud dentro del historial del propio
        /// solicitante (1 = primera solicitud del worker, 2 = segunda, ...). Se usa para
        /// mostrar un identificador estable y por-usuario en los correos del solicitante,
        /// en lugar del id global de la tabla.
        /// </summary>
        private static async Task<int> GetUserSolicitudNumeroAsync(AppDbContext ctx, int workerId, int solicitudId)
        {
            return await ctx.GaSolicitudSalida
                .Where(s => s.WorkerId == workerId && s.Id <= solicitudId)
                .CountAsync();
        }

        private static async Task<string> ResolveLugarDisplay(AppDbContext ctx, int? lugarId, string? lugarLibre)
        {
            if (!string.IsNullOrWhiteSpace(lugarLibre)) return lugarLibre.Trim();
            if (!lugarId.HasValue) return "—";

            var lugar = await ctx.GaLugar
                .Where(l => l.Id == lugarId.Value)
                .Select(l => new { l.Tipo, l.Nombre, l.ProjectId })
                .FirstOrDefaultAsync();
            if (lugar == null) return "—";

            if (lugar.Tipo == "proyecto" && lugar.ProjectId.HasValue)
            {
                var proj = await ctx.Project
                    .Where(p => p.ProjectId == lugar.ProjectId.Value)
                    .Select(p => p.ProjectDescription)
                    .FirstOrDefaultAsync();
                return proj ?? "[Sin proyecto]";
            }
            return lugar.Nombre ?? "—";
        }

        private static string BuildEmailBody(
            string nombre, DateOnly fechaSalida,
            List<(int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino)> trayectos,
            bool mostrarRecordatorio,
            string urlAprobar, string urlRechazar)
        {
            string esc(string s) => WebUtility.HtmlEncode(s);

            string trayectoBloque((int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino) t)
            {
                var titulo = trayectos.Count > 1 ? $"Trayecto {t.Orden}" : "Trayecto";
                return $@"<div style=""border:1px solid #E2E2E2;border-radius:8px;padding:12px 16px;margin-bottom:10px"">
                    <div style=""font-weight:600;color:#64BC04;margin-bottom:6px;font-size:13px"">{esc(titulo)}</div>
                    <table style=""width:100%;border-collapse:collapse;font-size:13px"">
                      <tr><td style=""padding:3px 0;color:#777;width:40%"">Hora de salida</td><td style=""padding:3px 0;color:#222"">{esc(t.HoraSalida)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Hora de retorno</td><td style=""padding:3px 0;color:#222"">{esc(t.HoraRetorno)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Motivo</td><td style=""padding:3px 0;color:#222"">{esc(t.Motivo)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Origen</td><td style=""padding:3px 0;color:#222"">{esc(t.Origen)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Destino</td><td style=""padding:3px 0;color:#222"">{esc(t.Destino)}</td></tr>
                    </table>
                  </div>";
            }

            var trayectosHtml = string.Concat(trayectos.Select(trayectoBloque));

            return $@"<!DOCTYPE html><html><body style=""font-family:Segoe UI,Arial,sans-serif;background:#f5f5f5;margin:0;padding:24px;color:#222"">
  <div style=""max-width:620px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.06)"">
    <div style=""background:#64BC04;padding:20px 24px;color:#fff"">
      <h2 style=""margin:0;font-size:18px"">Nueva solicitud de salida</h2>
    </div>
    <div style=""padding:24px"">
      <p style=""margin:0 0 12px""><b>{esc(nombre)}</b> ha registrado una solicitud de salida que requiere tu aprobación:</p>
      <p style=""margin:0 0 16px;color:#777;font-size:13px""><b>Fecha:</b> {esc(fechaSalida.ToString("dd/MM/yyyy"))}{(trayectos.Count > 1 ? $" — {trayectos.Count} trayectos" : "")}</p>
      {trayectosHtml}
      {(mostrarRecordatorio ? RecordatorioRecuperacionHtml : "")}
      <div style=""text-align:center;margin-top:18px"">
        <a href=""{urlAprobar}"" style=""display:inline-block;background:#009C87;color:#fff;text-decoration:none;padding:12px 28px;border-radius:8px;margin:0 8px;font-weight:600"">Aprobar</a>
        <a href=""{urlRechazar}"" style=""display:inline-block;background:#D30000;color:#fff;text-decoration:none;padding:12px 28px;border-radius:8px;margin:0 8px;font-weight:600"">Rechazar</a>
      </div>
      <p style=""color:#999;font-size:12px;margin-top:24px"">Los enlaces son válidos por 30 días.</p>
    </div>
  </div>
</body></html>";
        }

        /// <summary>
        /// Recordatorio que va en los correos de solicitud/aprobación (al solicitante y al revisor).
        /// Solo aplica a solicitudes con al menos un trayecto de hora exacta: si TODOS los motivos
        /// son de hora estimada, el bloque se omite (ver ResolveTrayectosForEmailAsync).
        /// </summary>
        private const string RecordatorioRecuperacionHtml =
            @"<p style=""margin:14px 0 0;color:#92400E;font-size:13px;background:#FEF9C3;padding:10px 14px;border-radius:8px"">
                 <b>Recuerda:</b> no olvides coordinar la recuperación de las horas dentro del mes calendario.
               </p>";

        /// <summary>
        /// Cuerpo del correo de confirmación al solicitante. <paramref name="enviadoRevisorA"/> son
        /// los correos a los que realmente salió la solicitud para revisión (vacío si ese correo
        /// está apagado) y <paramref name="aprobadorAsignado"/> el revisor que la tiene asignada
        /// aunque no le haya llegado el correo: sin eso el aviso diría que se envió a alguien que
        /// nunca lo recibió.
        /// </summary>
        private static string BuildEmailConfirmacionSolicitante(
            string nombre, int numeroUsuario, DateOnly fechaSalida,
            List<(int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino)> trayectos,
            bool mostrarRecordatorio,
            List<string> enviadoRevisorA,
            string? aprobadorAsignado)
        {
            string esc(string s) => WebUtility.HtmlEncode(s);

            string trayectoBloque((int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino) t)
            {
                var titulo = trayectos.Count > 1 ? $"Trayecto {t.Orden}" : "Trayecto";
                return $@"<div style=""border:1px solid #E2E2E2;border-radius:8px;padding:12px 16px;margin-bottom:10px"">
                    <div style=""font-weight:600;color:#0086A5;margin-bottom:6px;font-size:13px"">{esc(titulo)}</div>
                    <table style=""width:100%;border-collapse:collapse;font-size:13px"">
                      <tr><td style=""padding:3px 0;color:#777;width:40%"">Hora de salida</td><td style=""padding:3px 0;color:#222"">{esc(t.HoraSalida)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Hora de retorno</td><td style=""padding:3px 0;color:#222"">{esc(t.HoraRetorno)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Motivo</td><td style=""padding:3px 0;color:#222"">{esc(t.Motivo)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Origen</td><td style=""padding:3px 0;color:#222"">{esc(t.Origen)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Destino</td><td style=""padding:3px 0;color:#222"">{esc(t.Destino)}</td></tr>
                    </table>
                  </div>";
            }

            var trayectosHtml = string.Concat(trayectos.Select(trayectoBloque));

            var aprobadorBloque =
                enviadoRevisorA.Count > 0
                    ? $@"<p style=""margin:14px 0 0;color:#444;font-size:13px"">
                          Tu solicitud fue enviada a
                          <b style=""color:#0086A5"">{esc(string.Join(", ", enviadoRevisorA))}</b>
                          para su revisión.
                        </p>"
                : !string.IsNullOrWhiteSpace(aprobadorAsignado)
                    ? $@"<p style=""margin:14px 0 0;color:#444;font-size:13px"">
                          Tu solicitud quedó asignada a
                          <b style=""color:#0086A5"">{esc(aprobadorAsignado)}</b>
                          para su revisión.
                        </p>"
                    : @"<p style=""margin:14px 0 0;color:#92400E;font-size:13px;background:#FEF9C3;padding:10px 14px;border-radius:8px"">
                         Aún no se identificó a tu jefatura inmediata. El equipo administrativo será notificado para asignarla.
                       </p>";

            return $@"<!DOCTYPE html><html><body style=""font-family:Segoe UI,Arial,sans-serif;background:#f5f5f5;margin:0;padding:24px;color:#222"">
  <div style=""max-width:620px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.06)"">
    <div style=""background:#0086A5;padding:20px 24px;color:#fff"">
      <h2 style=""margin:0;font-size:18px"">Tu solicitud está en revisión</h2>
    </div>
    <div style=""padding:24px"">
      <p style=""margin:0 0 12px"">Hola <b>{esc(nombre)}</b>,</p>
      <p style=""margin:0 0 16px;color:#444;font-size:14px"">
        Recibimos tu solicitud de salida <b>#{numeroUsuario}</b> y está pendiente de aprobación.
        Te notificaremos por correo cuando sea aprobada o rechazada.
      </p>
      <p style=""margin:0 0 16px;color:#777;font-size:13px""><b>Fecha:</b> {esc(fechaSalida.ToString("dd/MM/yyyy"))}{(trayectos.Count > 1 ? $" — {trayectos.Count} trayectos" : "")}</p>
      {trayectosHtml}
      {aprobadorBloque}
      {(mostrarRecordatorio ? RecordatorioRecuperacionHtml : "")}
      <p style=""color:#999;font-size:12px;margin-top:24px"">Este es un correo automático, no respondas a este mensaje.</p>
    </div>
  </div>
</body></html>";
        }

        private static string RenderResultPage(string title, string message, bool isSuccess)
        {
            var color = isSuccess ? "#009C87" : "#D30000";
            return $@"<!DOCTYPE html><html lang=""es""><head><meta charset=""utf-8""><title>{WebUtility.HtmlEncode(title)}</title>
<style>
  body {{ font-family:Segoe UI,Arial,sans-serif;background:#f5f5f5;margin:0;padding:60px 20px;text-align:center; }}
  .card {{ max-width:500px;margin:0 auto;background:#fff;padding:40px;border-radius:12px;box-shadow:0 2px 12px rgba(0,0,0,.06); }}
  h1 {{ color:{color};margin:0 0 16px; }}
  p {{ color:#444;line-height:1.6;margin:0; }}
</style></head><body><div class=""card"">
  <h1>{WebUtility.HtmlEncode(title)}</h1><p>{WebUtility.HtmlEncode(message)}</p>
</div></body></html>";
        }

        // ── Detalle + capturas ──────────────────────────────────────────────

        public async Task<SolicitudSalidaDetalleDto> GetDetalle(int solicitudId, int userId)
        {
            var detalle = await _repo.GetDetalleForUser(solicitudId, userId);
            return detalle ?? throw new AbrilException("Solicitud no encontrada o no te pertenece.", 404);
        }

        public Task Cancelar(int solicitudId, int userId) => _repo.Cancelar(solicitudId, userId);

        public async Task<List<SolicitudSalidaCapturaDto>> UploadCapturasToTrayecto(int trayectoId, IEnumerable<(IFormFile File, decimal Monto)> items, int userId)
        {
            var trayecto = await _repo.GetTrayectoForUploadingCapturas(trayectoId, userId)
                ?? throw new AbrilException("No se pueden subir capturas: el trayecto no existe, no te pertenece, no está aprobado, o ya fue rendido.", 404);

            var lista = items?
                .Where(it => it.File != null && it.File.Length > 0)
                .ToList()
                ?? new();
            if (lista.Count == 0)
                throw new AbrilException("No se recibieron capturas.", 400);

            if (lista.Any(it => it.Monto < 0))
                throw new AbrilException("El monto no puede ser negativo.", 400);

            // Destino configurable desde BD (ga_captura_folder): se guarda el link de SharePoint tal
            // cual y se resuelve a driveId/folderId vía Graph. Editable sin redeploy y cada entorno
            // apunta a su propia biblioteca porque la config vive en su propia base de datos. Se
            // resuelve una sola vez para todo el lote. Mismo patrón que gth_sustento_folder.
            string? folderUrl;
            using (var ctx = _factory.CreateDbContext())
            {
                folderUrl = await ctx.GaCapturaFolder
                    .Where(c => c.State && c.Active)
                    .OrderBy(c => c.GaCapturaFolderId)
                    .Select(c => c.LinkUrl)
                    .FirstOrDefaultAsync();
            }
            if (string.IsNullOrWhiteSpace(folderUrl))
                throw new AbrilException(
                    "No se ha configurado la carpeta de SharePoint donde guardar las capturas de movilidad. " +
                    "Pide al administrador registrarla en la tabla ga_captura_folder.", 409);

            var carpeta = await _sharePointService.ResolveSharePointFolderUrlAsync(folderUrl);
            if (carpeta == null || !carpeta.IsFolder)
                throw new AbrilException("No se pudo resolver la carpeta de capturas en SharePoint.", 502);

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var subidos = new List<(string Url, string? ItemId, string Filename, decimal Monto)>();
            try
            {
                foreach (var it in lista)
                {
                    var f = it.File;
                    var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                    if (!allowed.Contains(ext))
                        throw new AbrilException($"Tipo de archivo no permitido: {f.FileName}. Solo JPG/PNG/WEBP/GIF.", 400);

                    var safeName = SanitizeFilename(Path.GetFileNameWithoutExtension(f.FileName));
                    var stamp    = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                    var filename = $"s{trayecto.SolicitudId}_t{trayecto.Id}_{stamp}_{safeName}{ext}";

                    using var stream = f.OpenReadStream();
                    var result = await _sharePointService.UploadToOneDriveFolderAsync(
                        carpeta.DriveId, carpeta.ItemId, filename, stream,
                        f.ContentType ?? "application/octet-stream",
                        autoRenameOnLock: true);

                    if (result?.WebUrl is null)
                        throw new AbrilException($"No se pudo subir el archivo {f.FileName}.", 502);

                    subidos.Add((result.WebUrl, result.ItemId, filename, it.Monto));
                }
            }
            catch (AbrilException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló subida de capturas para trayecto {TrayectoId}", trayectoId);
                throw new AbrilException("Error al subir las capturas a SharePoint.", 502);
            }

            return await _repo.InsertCapturas(trayectoId, subidos, userId);
        }

        public async Task<List<int>> GetIdsRendiblesMesAnterior(int userId)
        {
            var (desde, hasta) = MesAnteriorPeru.Rango();

            // GetByUserId ya acota al worker del usuario y calcula PuedeRendirse (captura por
            // trayecto, o catálogo para TI), así que la elegibilidad sale de ahí sin duplicar reglas.
            var candidatas = await _repo.GetByUserId(userId, new SolicitudSalidaFiltersDto
            {
                EstadoAprobacion = EstadosSalida.Aprobacion.NombreAprobado,
                EstadoRendicion  = EstadosSalida.Rendicion.NombreNoRendido,
                FechaSalidaDesde = desde,
                FechaSalidaHasta = hasta,
            });

            var ids = candidatas.Where(x => x.PuedeRendirse).Select(x => x.Id).ToList();
            if (ids.Count == 0)
                throw new AbrilException(
                    $"No tienes salidas listas para rendir entre el {desde:dd/MM/yyyy} y el {hasta:dd/MM/yyyy}. " +
                    "Deben estar aprobadas, sin rendir y con las capturas de todos sus trayectos.", 400);

            return ids;
        }

        public Task<ConsolidadoS10Dto> UploadConsolidadoS10(int solicitudId, ConsolidadoS10Ambito ambito, IFormFile file, int userId)
            // ownerUserId = userId: en el autoservicio solo se adjunta a salidas propias.
            => _consolidadoService.Upload(solicitudId, ambito, file, userId, ownerUserId: userId);

        private static string SanitizeFilename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "captura";
            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '#', '%', '&', '+' }).ToHashSet();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return clean.Length > 60 ? clean.Substring(0, 60) : clean;
        }

        // ── Aviso al revisor de que el S10 ya está adjunto ───────────────────

        public async Task<string> NotificarRevisorS10(int solicitudId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var info = await (
                from s in ctx.GaSolicitudSalida
                join w in ctx.Worker on s.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals (int?)per.PersonId into perGroup
                from per in perGroup.DefaultIfEmpty()
                join r in ctx.GaRendicion on s.RendicionId equals (int?)r.Id into rGroup
                from r in rGroup.DefaultIfEmpty()
                where s.Id == solicitudId
                select new
                {
                    s.Id, s.WorkerId, WorkerInternalId = w.Id, w.AreaScopeId,
                    s.FechaSalida, s.EstadoRendicionId, s.EstadoReembolsoId, s.RendicionId,
                    Trabajador = per != null ? (per.FullName ?? "Trabajador") : "Trabajador",
                    EsPropia   = per != null && per.UserId == userId,
                    NumeroPlanilla = r != null ? r.NumeroPlanilla : null,
                }
            ).FirstOrDefaultAsync()
              ?? throw new AbrilException("La solicitud de salida no existe.", 404);

            if (!info.EsPropia)
                throw new AbrilException("Solo puedes avisar al revisor de tus propias salidas.", 403);

            if (info.EstadoRendicionId != EstadosSalida.Rendicion.Rendido)
                throw new AbrilException("La salida todavía no está rendida.", 400);

            if (info.EstadoReembolsoId != EstadosSalida.Reembolso.Pendiente
                && info.EstadoReembolsoId != EstadosSalida.Reembolso.Rechazado)
                throw new AbrilException(
                    "El reembolso de esta salida ya fue revisado: no hace falta volver a avisar.", 400);

            var consolidado = await _consolidadoService.GetForSolicitud(solicitudId);
            if (consolidado == null)
                throw new AbrilException(
                    "Primero adjunta el Consolidado del S10: es lo que el revisor tiene que mirar.", 400);

            var revisor = await _revisorResolver.ResolveAsync(info.WorkerInternalId);
            if (string.IsNullOrWhiteSpace(revisor?.Email))
                throw new AbrilException(
                    "No se pudo determinar el correo de tu jefe/revisor. Avisa a Gestión del Talento Humano.", 409);

            var envio = await _correoResolver.ResolveEnvioAsync(
                CorreoEventoCodigos.S10Revisor,
                new List<string> { revisor!.Email! });

            if (!envio.Enviar)
                throw new AbrilException(
                    "El aviso al revisor está desactivado en la configuración de correos de Gestión Administrativa.",
                    409);

            // Monto rendido y cantidad de trayectos, para que el correo diga de cuánto se trata.
            var trayectoIds = await ctx.GaSolicitudTrayecto
                .Where(t => t.SolicitudId == solicitudId)
                .Select(t => t.Id)
                .ToListAsync();
            var monto = trayectoIds.Count == 0
                ? 0m
                : await ctx.GaSolicitudCaptura
                    .Where(c => trayectoIds.Contains(c.TrayectoId))
                    .SumAsync(c => (decimal?)c.Monto) ?? 0m;

            var datos = new ReembolsoCorreoDatos
            {
                SolicitudId    = info.Id,
                NumeroUsuario  = await GetUserSolicitudNumeroAsync(ctx, info.WorkerId, info.Id),
                Trabajador     = info.Trabajador,
                Area           = await ResolveAreaNombreAsync(ctx, info.AreaScopeId),
                FechaSalida    = info.FechaSalida,
                NumeroPlanilla = info.NumeroPlanilla.HasValue ? $"TI: {info.NumeroPlanilla.Value:D6}" : null,
                TrayectosCount = trayectoIds.Count,
                MontoTotal     = monto,
            };

            var url  = SalidaEnlaces.Gestion(_configuration, solicitudId);
            var body = ReembolsoEmailTemplates.RevisionPendiente(SalidaEmailLayout.Desde(_configuration), datos, url);

            await _emailService.SendAsync(
                to: envio.Para,
                subject: $"Reembolso por revisar - {info.Trabajador} - {info.FechaSalida:dd/MM/yyyy}",
                body: body,
                isHtml: true,
                cc: envio.Copia.Count > 0 ? envio.Copia : null);

            var solicitud = await ctx.GaSolicitudSalida.FirstAsync(x => x.Id == solicitudId);
            solicitud.RevisorNotificadoAt    = DateTimeOffset.UtcNow;
            solicitud.RevisorNotificadoPorId = userId;
            solicitud.UpdatedAt              = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            var nombre = string.IsNullOrWhiteSpace(revisor.Nombre) ? "tu revisor" : revisor.Nombre;
            return $"Se le avisó a {nombre}.";
        }

        /// <summary>Nombre del área a la que apunta el nodo (el más bajo del árbol). Null si no tiene.</summary>
        private static async Task<string?> ResolveAreaNombreAsync(AppDbContext ctx, int? areaScopeId)
        {
            if (!areaScopeId.HasValue) return null;
            return await (
                from sc in ctx.AreaScope
                join it in ctx.AreaItem on sc.AreaItemId equals it.AreaItemId
                where sc.AreaScopeId == areaScopeId.Value
                select it.AreaItemName
            ).FirstOrDefaultAsync();
        }

        // ── Notificación de aprobación al solicitante ─────────────────────────

        public async Task NotifySolicitanteAprobada(int solicitudId)
        {
            try
            {
                using var ctx = _factory.CreateDbContext();

                // 1. Datos de cabecera + worker + email del solicitante en una query
                var info = await (
                    from s in ctx.GaSolicitudSalida
                    join w in ctx.Worker on s.WorkerId equals w.Id
                    join p in ctx.Person on w.PersonId equals (int?)p.PersonId into pg
                    from p in pg.DefaultIfEmpty()
                    join u in ctx.User on (p != null ? p.UserId : null) equals (int?)u.UserId into ug
                    from u in ug.DefaultIfEmpty()
                    where s.Id == solicitudId
                    select new
                    {
                        s.Id, s.FechaSalida, s.EstadoAprobacionId, s.WorkerId,
                        Nombre = p != null ? (p.FullName ?? "Trabajador") : "Trabajador",
                        Email  = u != null ? u.Email : null,
                    }
                ).FirstOrDefaultAsync();

                if (info == null)
                {
                    _logger.LogWarning("NotifySolicitanteAprobada: solicitud {SolicitudId} no encontrada.", solicitudId);
                    return;
                }
                if (info.EstadoAprobacionId != EstadosSalida.Aprobacion.Aprobado)
                {
                    _logger.LogWarning("NotifySolicitanteAprobada: solicitud {SolicitudId} no está en estado Aprobado (estado actual: {Estado}). Email no enviado.", solicitudId, EstadosSalida.Aprobacion.Nombre(info.EstadoAprobacionId));
                    return;
                }
                if (string.IsNullOrWhiteSpace(info.Email))
                {
                    _logger.LogWarning("NotifySolicitanteAprobada: solicitud {SolicitudId} — el solicitante no tiene email registrado.", solicitudId);
                    return;
                }

                // 2. Trayectos para mostrar en el email
                var trayectos = await ctx.GaSolicitudTrayecto
                    .Where(t => t.SolicitudId == solicitudId)
                    .OrderBy(t => t.Orden)
                    .ToListAsync();

                var (resueltos, mostrarRecordatorio) = await ResolveTrayectosForEmailAsync(ctx, trayectos);

                var numeroUsuario = await GetUserSolicitudNumeroAsync(ctx, info.WorkerId, info.Id);

                var envio = await _correoResolver.ResolveEnvioAsync(
                    CorreoEventoCodigos.Aprobada,
                    new List<string> { info.Email },
                    await GetRecepcionRole52Async(ctx));

                if (!envio.Enviar)
                {
                    _logger.LogInformation(
                        "Correo de aprobación no enviado para solicitud {SolicitudId}: el correo está apagado o no quedó ningún destinatario configurado.",
                        solicitudId);
                    return;
                }

                var body    = BuildEmailAprobacionSolicitante(info.Nombre, numeroUsuario, info.FechaSalida, resueltos, mostrarRecordatorio);
                var subject = $"Solicitud de salida #{numeroUsuario} APROBADA - {info.FechaSalida:dd/MM/yyyy}";

                await _emailService.SendAsync(
                    to: envio.Para,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cc: envio.Copia.Count > 0 ? envio.Copia : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de aprobación al solicitante para solicitud {SolicitudId}", solicitudId);
            }
        }

        public async Task NotifySolicitanteRechazada(int solicitudId)
        {
            try
            {
                using var ctx = _factory.CreateDbContext();

                // 1. Datos de cabecera + worker + email del solicitante en una query
                var info = await (
                    from s in ctx.GaSolicitudSalida
                    join w in ctx.Worker on s.WorkerId equals w.Id
                    join p in ctx.Person on w.PersonId equals (int?)p.PersonId into pg
                    from p in pg.DefaultIfEmpty()
                    join u in ctx.User on (p != null ? p.UserId : null) equals (int?)u.UserId into ug
                    from u in ug.DefaultIfEmpty()
                    where s.Id == solicitudId
                    select new
                    {
                        s.Id, s.FechaSalida, s.EstadoAprobacionId, s.WorkerId, s.MotivoRechazo,
                        Nombre = p != null ? (p.FullName ?? "Trabajador") : "Trabajador",
                        Email  = u != null ? u.Email : null,
                    }
                ).FirstOrDefaultAsync();

                if (info == null)
                {
                    _logger.LogWarning("NotifySolicitanteRechazada: solicitud {SolicitudId} no encontrada.", solicitudId);
                    return;
                }
                if (info.EstadoAprobacionId != EstadosSalida.Aprobacion.Rechazado)
                {
                    _logger.LogWarning("NotifySolicitanteRechazada: solicitud {SolicitudId} no está en estado Rechazado (estado actual: {Estado}). Email no enviado.", solicitudId, EstadosSalida.Aprobacion.Nombre(info.EstadoAprobacionId));
                    return;
                }
                if (string.IsNullOrWhiteSpace(info.Email))
                {
                    _logger.LogWarning("NotifySolicitanteRechazada: solicitud {SolicitudId} — el solicitante no tiene email registrado.", solicitudId);
                    return;
                }

                // 2. Trayectos para mostrar en el email
                var trayectos = await ctx.GaSolicitudTrayecto
                    .Where(t => t.SolicitudId == solicitudId)
                    .OrderBy(t => t.Orden)
                    .ToListAsync();

                var (resueltos, _) = await ResolveTrayectosForEmailAsync(ctx, trayectos);

                var numeroUsuario = await GetUserSolicitudNumeroAsync(ctx, info.WorkerId, info.Id);

                var envio = await _correoResolver.ResolveEnvioAsync(
                    CorreoEventoCodigos.Rechazada,
                    new List<string> { info.Email },
                    await GetRecepcionRole52Async(ctx));

                if (!envio.Enviar)
                {
                    _logger.LogInformation(
                        "Correo de rechazo no enviado para solicitud {SolicitudId}: el correo está apagado o no quedó ningún destinatario configurado.",
                        solicitudId);
                    return;
                }

                var body    = BuildEmailRechazoSolicitante(info.Nombre, numeroUsuario, info.FechaSalida, resueltos, info.MotivoRechazo);
                var subject = $"Solicitud de salida #{numeroUsuario} RECHAZADA - {info.FechaSalida:dd/MM/yyyy}";

                await _emailService.SendAsync(
                    to: envio.Para,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cc: envio.Copia.Count > 0 ? envio.Copia : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de rechazo al solicitante para solicitud {SolicitudId}", solicitudId);
            }
        }

        private static string BuildEmailRechazoSolicitante(
            string nombre, int numeroUsuario, DateOnly fechaSalida,
            List<(int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino)> trayectos,
            string? motivoRechazo)
        {
            string esc(string s) => WebUtility.HtmlEncode(s);

            string trayectoBloque((int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino) t)
            {
                var titulo = trayectos.Count > 1 ? $"Trayecto {t.Orden}" : "Trayecto";
                return $@"<div style=""border:1px solid #E2E2E2;border-radius:8px;padding:12px 16px;margin-bottom:10px"">
                    <div style=""font-weight:600;color:#D30000;margin-bottom:6px;font-size:13px"">{esc(titulo)}</div>
                    <table style=""width:100%;border-collapse:collapse;font-size:13px"">
                      <tr><td style=""padding:3px 0;color:#777;width:40%"">Hora de salida</td><td style=""padding:3px 0;color:#222"">{esc(t.HoraSalida)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Hora de retorno</td><td style=""padding:3px 0;color:#222"">{esc(t.HoraRetorno)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Motivo</td><td style=""padding:3px 0;color:#222"">{esc(t.Motivo)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Origen</td><td style=""padding:3px 0;color:#222"">{esc(t.Origen)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Destino</td><td style=""padding:3px 0;color:#222"">{esc(t.Destino)}</td></tr>
                    </table>
                  </div>";
            }

            var trayectosHtml = string.Concat(trayectos.Select(trayectoBloque));

            var motivoBloque = string.IsNullOrWhiteSpace(motivoRechazo)
                ? ""
                : $@"<p style=""margin:14px 0 0;color:#991B1B;font-size:13px;background:#FEE2E2;padding:10px 14px;border-radius:8px"">
                      <b>Motivo del rechazo:</b> {esc(motivoRechazo.Trim())}
                    </p>";

            return $@"<!DOCTYPE html><html><body style=""font-family:Segoe UI,Arial,sans-serif;background:#f5f5f5;margin:0;padding:24px;color:#222"">
  <div style=""max-width:620px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.06)"">
    <div style=""background:#D30000;padding:20px 24px;color:#fff"">
      <h2 style=""margin:0;font-size:18px"">✕ Tu solicitud fue rechazada</h2>
    </div>
    <div style=""padding:24px"">
      <p style=""margin:0 0 12px"">Hola <b>{esc(nombre)}</b>,</p>
      <p style=""margin:0 0 16px;color:#444;font-size:14px"">
        Tu solicitud de salida <b>#{numeroUsuario}</b> fue <b style=""color:#D30000"">rechazada</b>.
        Si tienes dudas, coordina directamente con tu jefatura.
      </p>
      <p style=""margin:0 0 16px;color:#777;font-size:13px""><b>Fecha:</b> {esc(fechaSalida.ToString("dd/MM/yyyy"))}{(trayectos.Count > 1 ? $" — {trayectos.Count} trayectos" : "")}</p>
      {trayectosHtml}
      {motivoBloque}
      <p style=""color:#999;font-size:12px;margin-top:24px"">Este es un correo automático, no respondas a este mensaje.</p>
    </div>
  </div>
</body></html>";
        }

        private static string BuildEmailAprobacionSolicitante(
            string nombre, int numeroUsuario, DateOnly fechaSalida,
            List<(int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino)> trayectos,
            bool mostrarRecordatorio)
        {
            string esc(string s) => WebUtility.HtmlEncode(s);

            string trayectoBloque((int Orden, string HoraSalida, string HoraRetorno, string Motivo, string Origen, string Destino) t)
            {
                var titulo = trayectos.Count > 1 ? $"Trayecto {t.Orden}" : "Trayecto";
                return $@"<div style=""border:1px solid #E2E2E2;border-radius:8px;padding:12px 16px;margin-bottom:10px"">
                    <div style=""font-weight:600;color:#009C87;margin-bottom:6px;font-size:13px"">{esc(titulo)}</div>
                    <table style=""width:100%;border-collapse:collapse;font-size:13px"">
                      <tr><td style=""padding:3px 0;color:#777;width:40%"">Hora de salida</td><td style=""padding:3px 0;color:#222"">{esc(t.HoraSalida)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Hora de retorno</td><td style=""padding:3px 0;color:#222"">{esc(t.HoraRetorno)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Motivo</td><td style=""padding:3px 0;color:#222"">{esc(t.Motivo)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Origen</td><td style=""padding:3px 0;color:#222"">{esc(t.Origen)}</td></tr>
                      <tr><td style=""padding:3px 0;color:#777"">Destino</td><td style=""padding:3px 0;color:#222"">{esc(t.Destino)}</td></tr>
                    </table>
                  </div>";
            }

            var trayectosHtml = string.Concat(trayectos.Select(trayectoBloque));

            return $@"<!DOCTYPE html><html><body style=""font-family:Segoe UI,Arial,sans-serif;background:#f5f5f5;margin:0;padding:24px;color:#222"">
  <div style=""max-width:620px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.06)"">
    <div style=""background:#009C87;padding:20px 24px;color:#fff"">
      <h2 style=""margin:0;font-size:18px"">✓ Tu solicitud fue aprobada</h2>
    </div>
    <div style=""padding:24px"">
      <p style=""margin:0 0 12px"">Hola <b>{esc(nombre)}</b>,</p>
      <p style=""margin:0 0 16px;color:#444;font-size:14px"">
        Tu solicitud de salida <b>#{numeroUsuario}</b> fue <b style=""color:#009C87"">aprobada</b>.
        Recuerda subir las capturas de movilidad (imagen + monto) por cada trayecto para que la rendición pueda procesarse.
      </p>
      <p style=""margin:0 0 16px;color:#777;font-size:13px""><b>Fecha:</b> {esc(fechaSalida.ToString("dd/MM/yyyy"))}{(trayectos.Count > 1 ? $" — {trayectos.Count} trayectos" : "")}</p>
      {trayectosHtml}
      {(mostrarRecordatorio ? RecordatorioRecuperacionHtml : "")}
      <p style=""color:#999;font-size:12px;margin-top:24px"">Este es un correo automático, no respondas a este mensaje.</p>
    </div>
  </div>
</body></html>";
        }
    }
}
