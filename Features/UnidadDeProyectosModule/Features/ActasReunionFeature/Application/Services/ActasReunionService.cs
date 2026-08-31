using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Repositories;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services.Notificaciones.Dtos;
using Abril_Backend.Shared.Services.Notificaciones.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Services
{
    public class ActasReunionService : IActasReunionService
    {
        private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB por archivo
        private const int MaxFilesPorSubida = 10;

        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".txt", ".csv", ".zip", ".rar",
        };

        private readonly IActasReunionRepository _repository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IStorageContainerResolver _containerResolver;
        private readonly IGraphSharePointService _sharePointService;
        private readonly IEmailService _emailService;
        private readonly INotificacionesService _notificacionesService;
        private readonly ILogger<ActasReunionService> _logger;
        private readonly string[] _allowedHosts;
        private readonly string _frontendUrl;
        private readonly string[] _logoPaths;

        public ActasReunionService(
            IActasReunionRepository repository,
            IFileStorageService fileStorageService,
            IStorageContainerResolver containerResolver,
            IGraphSharePointService sharePointService,
            IEmailService emailService,
            INotificacionesService notificacionesService,
            ILogger<ActasReunionService> logger,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _repository = repository;
            _fileStorageService = fileStorageService;
            _containerResolver = containerResolver;
            _sharePointService = sharePointService;
            _emailService = emailService;
            _notificacionesService = notificacionesService;
            _logger = logger;
            _frontendUrl = configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            // Mismos candidatos que usa Convalidación/Amonestaciones para el logo del PDF.
            _logoPaths = new[]
            {
                Path.Combine(env.WebRootPath, "images", "abril-logo.png"),
                Path.Combine(env.WebRootPath, "images", "logo-abril.jpg"),
                Path.Combine(env.ContentRootPath, "Templates", "logo-abril.jpg"),
            };

            // Hosts permitidos del tenant, derivados del sitio ya configurado (mismo criterio
            // que la carpeta de facturas de Contabilidad).
            var siteHost = SharePointSiteRef.FromConfig(configuration, "CostosYPresupuestos").Hostname.ToLowerInvariant();
            var tenant = siteHost.Split('.')[0].Replace("-my", "");
            _allowedHosts = new[] { $"{tenant}.sharepoint.com", $"{tenant}-my.sharepoint.com" };
        }

        public Task<ReunionPaginaInicialDto> GetPaginaInicial(ReunionFiltroRequest filtro, int userId)
            => _repository.GetPaginaInicial(filtro, userId);

        public Task<PagedResultDto<ReunionListItemDto>> GetReuniones(ReunionFiltroRequest filtro, int userId)
            => _repository.GetReuniones(filtro, userId);

        public Task<ReunionDetalleDto> GetDetalle(int reunionId, int userId)
            => _repository.GetDetalle(reunionId, userId);

        public async Task<int> Create(ReunionCreateRequest request, int userId)
        {
            if (string.IsNullOrWhiteSpace(request.Tema))
                throw new AbrilException("El tema de la reunión es obligatorio.", 400);
            if (request.ProjectId.HasValue && request.AreaScopeId.HasValue)
                throw new AbrilException("Una reunión no puede pertenecer a un proyecto y a un área/gerencia a la vez.", 400);
            // Toda reunión requiere agenda. Un tema del catálogo ya trae la suya (fija o dinámica);
            // una reunión puntual (tema personalizado, sin guardarse como recurrente) debe traer al
            // menos un punto de agenda definido aquí mismo.
            if (!request.ReunionTemaId.HasValue && string.IsNullOrWhiteSpace(request.AgendaTexto))
                throw new AbrilException("Debe indicar al menos un punto de agenda para esta reunión.", 400);
            ValidarHoras(request.HoraInicio, request.HoraFin);
            var reunionId = await _repository.Create(request, userId);

            try
            {
                await EnviarConvocatoria(reunionId);
            }
            catch (Exception ex)
            {
                // El correo de convocatoria no debe bloquear el agendado de la reunión.
                _logger.LogError(ex, "Error enviando convocatoria de la reunión {ReunionId}", reunionId);
            }

            return reunionId;
        }

        /// <summary>Correo inmediato a los participantes al agendar o al sumarse a una reunión existente
        /// (distinto del recordatorio de agenda, que solo aplica a temas con agenda dinámica y llega más
        /// cerca de la fecha). soloWorkerIds filtra a solo esos workers; null = todos los participantes.</summary>
        private async Task EnviarConvocatoria(int reunionId, List<int>? soloWorkerIds = null)
        {
            var info = await _repository.GetInfoParaConvocatoria(reunionId, soloWorkerIds);
            if (info == null || info.Destinatarios.Count == 0) return;

            // Va directo a la agenda (fija: la ve; dinámica: puede cargar sus temas ahí mismo),
            // en vez del acta general — es lo primero que el convocado necesita hacer/ver.
            var link = $"{_frontendUrl}/projects/actas-reunion/{info.ReunionId}/agenda";
            var emails = info.Destinatarios.Select(d => d.Email).Distinct().ToList();

            await _emailService.SendAsync(
                to: emails,
                subject: $"Convocatoria — Reunión N° {info.Numero}: {info.Tema}",
                body: BuildCuerpoConvocatoria(info, link),
                isHtml: true);
        }

        public Task<List<TrabajadorAbrilDto>> BuscarTrabajadoresPorFiltro(int? areaScopeId, List<int>? puestoIds, int? projectId)
            => _repository.BuscarTrabajadoresPorFiltro(areaScopeId, puestoIds, projectId);

        public Task<List<CatalogoDto>> GetPuestos()
            => _repository.GetPuestos();

        public Task<List<CatalogoDto>> GetPuestosPorArea(int? areaScopeId)
            => _repository.GetPuestosPorArea(areaScopeId);

        public Task<CatalogoDto> AgregarTema(string descripcion, int userId)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new AbrilException("El tema es obligatorio.", 400);
            return _repository.AgregarTema(descripcion, userId);
        }

        public Task<List<ReunionTemaOpcionDto>> GetTemasCatalogo()
            => _repository.GetTemasCatalogo();

        public Task<TemaConvocatoriaDto> GetConvocatoriaTema(int reunionTemaId)
            => _repository.GetConvocatoriaTema(reunionTemaId);

        public Task GuardarConvocatoriaTema(int reunionTemaId, TemaConvocatoriaSaveRequest request, int userId)
            => _repository.GuardarConvocatoriaTema(reunionTemaId, request, userId);

        public Task<int> EliminarTema(int reunionTemaId)
            => _repository.EliminarTema(reunionTemaId);

        public Task<ReunionAgendaDto> GetAgenda(int reunionId, int userId)
            => _repository.GetAgenda(reunionId, userId);

        public Task GuardarMisTemas(int reunionId, int userId, GuardarMisTemasRequest request)
        {
            var temas = request.Temas
                .Select(t => t.Descripcion?.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t!)
                .ToList();
            return _repository.GuardarMisTemas(reunionId, userId, temas);
        }

        public Task<ReunionAgendaItemDto> AgregarTemaPuntual(int reunionId, int userId, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new AbrilException("El tema es obligatorio.", 400);
            return _repository.AgregarTemaPuntual(reunionId, userId, descripcion.Trim());
        }

        public Task EliminarTemaPuntual(int reunionId, int reunionAgendaItemId, int userId)
            => _repository.EliminarTemaPuntual(reunionId, reunionAgendaItemId, userId);

        public Task<List<MisAcuerdoDto>> GetMisAcuerdos(int userId)
            => _repository.GetMisAcuerdos(userId);

        public Task<PagedResultDto<AcuerdoBusquedaItemDto>> GetAcuerdos(AcuerdoBusquedaFiltroRequest filtro, int userId)
            => _repository.GetAcuerdos(filtro, userId);

        public Task<List<AcuerdoPendienteAnteriorDto>> GetAcuerdosPendientesAnteriores(int reunionId)
            => _repository.GetAcuerdosPendientesAnteriores(reunionId);

        public Task ReprogramarAcuerdo(int reunionAcuerdoId, AcuerdoReprogramarRequest request, int userId)
            => _repository.ReprogramarAcuerdo(reunionAcuerdoId, request, userId);

        public Task MarcarAcuerdoCumplido(int reunionAcuerdoId, AcuerdoMarcarCumplidoRequest request, int userId)
            => _repository.MarcarAcuerdoCumplido(reunionAcuerdoId, request, userId);

        // ── Recurrencia ────────────────────────────────────────────────────
        /// <summary>0 = generado automáticamente por el job de recurrencia, no por un usuario.</summary>
        private const int SistemaUserId = 0;

        public Task<TemaRecurrenciaDto> GetRecurrenciaTema(int reunionTemaId)
            => _repository.GetRecurrenciaTema(reunionTemaId);

        public Task GuardarRecurrenciaTema(int reunionTemaId, TemaRecurrenciaSaveRequest request, int userId)
            => _repository.GuardarRecurrenciaTema(reunionTemaId, request, userId);

        public async Task<ProcesarGeneracionRecurrenteResultDto> ProcesarGeneracionRecurrente()
        {
            var resultado = new ProcesarGeneracionRecurrenteResultDto();
            var temas = await _repository.GetTemasRecurrentesActivos();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var tema in temas)
            {
                var siguiente = tema.UltimaFechaGenerada.HasValue
                    ? tema.UltimaFechaGenerada.Value.AddDays(tema.IntervaloDias!.Value)
                    : tema.FechaAncla!.Value;

                var reglas = await _repository.GetReglasTemaParaGeneracion(tema.ReunionTemaId);
                var horizonte = hoy.AddDays(tema.DiasAnticipacion);

                // Tope de seguridad: si el job estuvo caído mucho tiempo, se ponen al día las
                // ocurrencias atrasadas sin generar de golpe una cantidad descontrolada.
                var iteraciones = 0;
                while (siguiente <= horizonte && iteraciones < 20)
                {
                    var participantes = await ResolverParticipantesDeReglas(reglas);

                    var request = new ReunionCreateRequest
                    {
                        ProjectId = null,
                        AreaScopeId = tema.AreaScopeId,
                        Tema = tema.Descripcion,
                        ReunionTemaId = tema.ReunionTemaId,
                        ConvocadoPor = null,
                        Lugar = tema.Lugar,
                        Fecha = siguiente,
                        HoraInicio = tema.HoraInicio,
                        HoraFin = tema.HoraFin,
                        ReunionAnteriorId = tema.UltimaReunionGeneradaId,
                        AgendaTexto = null,
                        Participantes = participantes,
                    };

                    int nuevaReunionId;
                    try
                    {
                        nuevaReunionId = await Create(request, SistemaUserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generando reunión recurrente del tema {ReunionTemaId} para {Fecha}", tema.ReunionTemaId, siguiente);
                        break; // no sigue de largo si esta ocurrencia falló, para no perder el orden de la cadena
                    }

                    await _repository.AvanzarRecurrenciaTema(tema.ReunionTemaId, siguiente, nuevaReunionId);
                    tema.UltimaFechaGenerada = siguiente;
                    tema.UltimaReunionGeneradaId = nuevaReunionId;

                    resultado.ReunionesGeneradas++;
                    resultado.Detalle.Add($"Tema {tema.ReunionTemaId} ({tema.Descripcion}): reunión {nuevaReunionId} para {siguiente:yyyy-MM-dd}");

                    siguiente = siguiente.AddDays(tema.IntervaloDias!.Value);
                    iteraciones++;
                }
            }

            return resultado;
        }

        /// <summary>Igual criterio que la convocatoria masiva manual: une (sin duplicar) los
        /// trabajadores que califican en cada regla independiente del tema.</summary>
        private async Task<List<ReunionParticipanteInput>> ResolverParticipantesDeReglas(
            List<(int? AreaScopeId, int? ProjectId, List<int> PuestoIds, List<int> WorkerIdsExcluidos)> reglas)
        {
            var vistos = new HashSet<int>();
            var participantes = new List<ReunionParticipanteInput>();

            foreach (var (areaScopeId, projectId, puestoIds, workerIdsExcluidos) in reglas)
            {
                var trabajadores = await _repository.BuscarTrabajadoresPorFiltro(
                    areaScopeId, puestoIds.Count > 0 ? puestoIds : null, projectId);
                var excluidos = workerIdsExcluidos.Count > 0 ? workerIdsExcluidos.ToHashSet() : null;

                foreach (var t in trabajadores)
                {
                    if (excluidos != null && excluidos.Contains(t.WorkerId)) continue;
                    if (!vistos.Add(t.WorkerId)) continue;
                    participantes.Add(new ReunionParticipanteInput
                    {
                        ReunionParticipanteId = null,
                        WorkerId = t.WorkerId,
                        Nombre = t.FullName,
                        Cargo = t.Cargo,
                        Iniciales = null,
                        Asistio = false,
                    });
                }
            }

            return participantes;
        }

        public async Task Update(int reunionId, ReunionUpdateRequest request, int userId)
        {
            if (string.IsNullOrWhiteSpace(request.Tema))
                throw new AbrilException("El tema de la reunión es obligatorio.", 400);
            ValidarHoras(request.HoraInicio, request.HoraFin);
            var nuevosWorkerIds = await _repository.Update(reunionId, request, userId);

            if (nuevosWorkerIds.Count > 0)
            {
                try
                {
                    await EnviarConvocatoria(reunionId, nuevosWorkerIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando convocatoria a participantes nuevos de la reunión {ReunionId}", reunionId);
                }
            }
        }

        public Task Reprogramar(int reunionId, ReunionReprogramarRequest request, int userId)
        {
            ValidarHoras(request.HoraInicio, request.HoraFin);
            return _repository.Reprogramar(reunionId, request, userId);
        }

        public async Task CambiarEstado(int reunionId, ReunionCambiarEstadoRequest request, int userId)
        {
            if (string.IsNullOrWhiteSpace(request.Estado))
                throw new AbrilException("Debe indicar el estado destino.", 400);

            var estado = request.Estado.Trim().ToUpperInvariant();
            await _repository.CambiarEstado(reunionId, estado, userId);

            if (estado == ActasReunionRepository.EstadoRealizada)
            {
                try
                {
                    await EnviarActaRealizada(reunionId);
                }
                catch (Exception ex)
                {
                    // El envío del acta no debe bloquear el cambio de estado, ya guardado.
                    _logger.LogError(ex, "Error enviando el acta de la reunión {ReunionId} tras marcarla realizada", reunionId);
                }
            }
        }

        /// <summary>Al marcar la reunión Realizada: genera el PDF del acta, adjunta los archivos ya
        /// subidos (descargándolos cuando viven en el SharePoint configurado; si no se puede o el
        /// total supera el límite razonable de adjuntos, se listan como link en el cuerpo), y envía
        /// un correo a cada asistente y a cada responsable de acuerdo. Quien tiene acuerdos propios
        /// que requieren aceptación recibe además, en su mismo correo, el link personal para
        /// aceptarlos o rechazarlos.</summary>
        private async Task<byte[]?> CargarLogoAsync()
        {
            var logoPath = _logoPaths.FirstOrDefault(File.Exists);
            return logoPath != null ? await File.ReadAllBytesAsync(logoPath) : null;
        }

        private async Task EnviarActaRealizada(int reunionId)
        {
            var destinatarios = await _repository.GetDestinatariosActaRealizada(reunionId);
            if (destinatarios.Count == 0) return;

            var detalle = await _repository.GetDetalle(reunionId, SistemaUserId);
            var pdfActa = ActasReunionPdfService.GenerarPdf(detalle, await CargarLogoAsync());

            const long maxAdjuntosBytes = 15 * 1024 * 1024; // 15 MB: margen razonable para no rebotar por tamaño.
            var attachments = new List<EmailAttachment>
            {
                new() { FileName = $"Acta_Reunion_N{detalle.Numero}.pdf", ContentType = "application/pdf", Content = pdfActa },
            };
            long totalBytes = pdfActa.LongLength;
            var archivosNoAdjuntados = new List<ReunionArchivoDto>();

            foreach (var archivo in detalle.Archivos)
            {
                if (totalBytes >= maxAdjuntosBytes) { archivosNoAdjuntados.Add(archivo); continue; }
                try
                {
                    byte[]? bytes = null;
                    if (Uri.TryCreate(archivo.ArchivoUrl, UriKind.Absolute, out var uri)
                        && _allowedHosts.Contains(uri.Host.ToLowerInvariant()))
                    {
                        bytes = await _sharePointService.DownloadOneDriveFileByWebUrlAsync(archivo.ArchivoUrl);
                    }

                    if (bytes is null || totalBytes + bytes.LongLength > maxAdjuntosBytes)
                    {
                        archivosNoAdjuntados.Add(archivo);
                        continue;
                    }

                    totalBytes += bytes.LongLength;
                    attachments.Add(new EmailAttachment
                    {
                        FileName = archivo.OriginalFileName ?? $"adjunto_{archivo.ReunionArchivoId}",
                        ContentType = "application/octet-stream",
                        Content = bytes,
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo descargar el adjunto {Url} para el correo del acta de la reunión {ReunionId}", archivo.ArchivoUrl, reunionId);
                    archivosNoAdjuntados.Add(archivo);
                }
            }

            foreach (var dest in destinatarios)
            {
                await _emailService.SendAsync(
                    to: new List<string> { dest.Email },
                    subject: $"Acta de Reunión N° {detalle.Numero}: {detalle.Tema}",
                    body: BuildCuerpoActaRealizada(detalle, dest, archivosNoAdjuntados, _frontendUrl),
                    isHtml: true,
                    attachments: attachments);
            }
        }

        private static string BuildCuerpoActaRealizada(ReunionDetalleDto d, ActaEnvioDestinatarioDto dest, List<ReunionArchivoDto> archivosNoAdjuntados, string frontendUrl)
        {
            var linksHtml = archivosNoAdjuntados.Count == 0 ? "" : $@"
    <p style='margin-top:16px'><strong>Archivos adicionales (ver en el sistema):</strong></p>
    <ul>
      {string.Join("", archivosNoAdjuntados.Select(a => $"<li><a href='{a.ArchivoUrl}'>{a.OriginalFileName ?? "Archivo"}</a></li>"))}
    </ul>";

            var acuerdosHtml = dest.AcuerdosPendientes.Count == 0 ? "" : $@"
  <div style='background:#fff8e6;border:1px solid #ffe6a3;border-radius:8px;padding:16px;margin-top:16px'>
    <p style='margin:0 0 8px;font-weight:bold;color:#7a5b00'>Acuerdos que requieren tu aceptación</p>
    <ul style='margin:0;padding-left:18px'>
      {string.Join("", dest.AcuerdosPendientes.Select(a => $@"
      <li style='margin-bottom:10px'>
        {a.Descripcion}<br/>
        <a href='{frontendUrl}/projects/actas-reunion/acuerdo/{a.ReunionAcuerdoResponsableId}'
           style='display:inline-block;margin-top:4px;background:#0F6E56;color:#fff;padding:6px 14px;border-radius:6px;text-decoration:none;font-size:13px'>
          Aceptar / Rechazar
        </a>
      </li>"))}
    </ul>
  </div>";

            return $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px'>
  <div style='background:#0F6E56;padding:16px 24px;border-radius:8px 8px 0 0'>
    <h2 style='color:#fff;margin:0;font-size:18px'>Acta de Reunión N° {d.Numero}</h2>
  </div>
  <div style='background:#f8fafc;padding:24px;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p><strong>{d.Tema}</strong></p>
    <p>{d.Fecha:dd/MM/yyyy}{(d.HoraInicio.HasValue ? $" a las {d.HoraInicio:HH\\:mm}" : "")}</p>
    <p>Se adjunta el acta completa en PDF, con los acuerdos y la asistencia registrada.</p>
    {linksHtml}
    {acuerdosHtml}
  </div>
</div>";
        }

        public Task Eliminar(int reunionId, int userId)
            => _repository.Eliminar(reunionId, userId);

        public Task<int> CrearAcuerdo(int reunionId, ReunionAcuerdoRequest request, int userId)
        {
            ValidarAcuerdo(request);
            return _repository.CrearAcuerdo(reunionId, request, userId);
        }

        public Task ActualizarAcuerdo(int reunionAcuerdoId, ReunionAcuerdoRequest request, int userId)
        {
            ValidarAcuerdo(request);
            return _repository.ActualizarAcuerdo(reunionAcuerdoId, request, userId);
        }

        public Task EliminarAcuerdo(int reunionAcuerdoId, int userId)
            => _repository.EliminarAcuerdo(reunionAcuerdoId, userId);

        public async Task<List<ReunionArchivoDto>> SubirArchivos(int reunionId, IFormFileCollection files, int userId)
        {
            if (files is null || files.Count == 0)
                throw new AbrilException("No se adjuntó ningún archivo.", 400);
            if (files.Count > MaxFilesPorSubida)
                throw new AbrilException($"Solo se pueden subir hasta {MaxFilesPorSubida} archivos por vez.", 400);

            foreach (var file in files)
            {
                if (file.Length == 0)
                    throw new AbrilException($"El archivo \"{file.FileName}\" está vacío.", 400);
                if (file.Length > MaxFileSizeBytes)
                    throw new AbrilException($"El archivo \"{file.FileName}\" supera el tamaño máximo permitido (25 MB).", 400);
                var extension = Path.GetExtension(file.FileName);
                if (!ExtensionesPermitidas.Contains(extension))
                    throw new AbrilException($"El tipo de archivo \"{extension}\" no está permitido.", 400);
            }

            // Si hay una carpeta de SharePoint configurada, los adjuntos van ahí (dentro de una
            // subcarpeta por reunión); si no, se usa el storage por defecto (Azure Blob).
            var destino = await _repository.GetFolderDestination();
            if (destino != null)
                return await SubirArchivosASharePoint(reunionId, files, destino.Value, userId);

            var container = _containerResolver.GetActasReunionContainerName();

            var streams = new List<Stream>();
            try
            {
                var toUpload = new List<(Stream Stream, string FileName)>();
                foreach (var file in files)
                {
                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    var extension = Path.GetExtension(file.FileName);
                    toUpload.Add((stream, $"{Guid.NewGuid()}{extension}"));
                }

                var urls = await _fileStorageService.UploadFilesAsync(toUpload, container);

                var archivos = urls
                    .Select((url, i) => (Url: url, OriginalFileName: (string?)files[i].FileName))
                    .ToList();

                return await _repository.AgregarArchivos(reunionId, archivos, userId);
            }
            finally
            {
                foreach (var stream in streams)
                    stream.Dispose();
            }
        }

        /// <summary>
        /// Sube los adjuntos a la carpeta de SharePoint configurada, dentro de una subcarpeta
        /// "{PROYECTO} - REUNIÓN N° {numero}" (se crea si no existe). Guarda el webUrl como URL.
        /// </summary>
        private async Task<List<ReunionArchivoDto>> SubirArchivosASharePoint(
            int reunionId,
            IFormFileCollection files,
            (string DriveId, string FolderId) destino,
            int userId)
        {
            var (projectDescription, numero) = await _repository.GetDatosCarpetaReunion(reunionId);
            var nombreSubcarpeta = SanitizeSharePointName($"{projectDescription} - REUNIÓN N° {numero}");

            var subcarpetaId = await _sharePointService.EnsureChildFolderAsync(
                destino.DriveId, destino.FolderId, nombreSubcarpeta);

            var archivos = new List<(string Url, string? OriginalFileName)>();
            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                var resultado = await _sharePointService.UploadToOneDriveFolderAsync(
                    destino.DriveId,
                    subcarpetaId,
                    SanitizeSharePointName(file.FileName),
                    stream,
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    autoRenameOnLock: true);

                if (resultado?.WebUrl == null)
                    throw new AbrilException($"No se pudo subir el archivo \"{file.FileName}\" a SharePoint.", 500);

                archivos.Add((resultado.WebUrl, file.FileName));
            }

            return await _repository.AgregarArchivos(reunionId, archivos, userId);
        }

        /// <summary>Reemplaza los caracteres no permitidos por SharePoint en nombres de carpeta/archivo.</summary>
        private static string SanitizeSharePointName(string name)
        {
            var invalid = new[] { '"', '*', ':', '<', '>', '?', '/', '\\', '|' };
            var sanitized = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(sanitized) ? "archivo" : sanitized;
        }

        // ── Carpeta de SharePoint para adjuntos ──────────────────────────────
        public Task<ReunionFolderDto?> GetFolder()
            => _repository.GetFolderSingleton();

        public async Task<ReunionFolderDto> SaveFolder(ReunionFolderSaveDto dto, int userId)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.LinkUrl))
                throw new AbrilException("Debe ingresar el link de la carpeta.");

            var link = dto.LinkUrl.Trim();

            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new AbrilException("El link no es una URL válida.");

            if (!_allowedHosts.Contains(uri.Host.ToLowerInvariant()))
                throw new AbrilException(
                    $"El link no pertenece a la organización. Solo se permiten enlaces de: {string.Join(", ", _allowedHosts)}.");

            var resolved = await _sharePointService.ResolveSharePointFolderUrlAsync(link)
                ?? throw new AbrilException(
                    "No se pudo acceder a la carpeta del link. Verifique que el enlace apunte a una carpeta/biblioteca y que la aplicación tenga acceso.");

            if (!resolved.IsFolder)
                throw new AbrilException("El link debe apuntar a una carpeta, no a un archivo.");

            await _repository.UpsertFolder(link, resolved.DriveId, resolved.ItemId, resolved.Name, resolved.WebUrl, userId);

            return await _repository.GetFolderSingleton()
                ?? throw new AbrilException("No se pudo guardar la carpeta.", 500);
        }

        public Task EliminarArchivo(int reunionArchivoId, int userId)
            => _repository.EliminarArchivo(reunionArchivoId, userId);

        public Task<AcuerdoResponsableInfoDto> GetAcuerdoResponsableInfo(int reunionAcuerdoResponsableId, int userId)
            => _repository.GetAcuerdoResponsableInfo(reunionAcuerdoResponsableId, userId);

        public Task ResponderAcuerdo(int reunionAcuerdoResponsableId, int userId, AcuerdoResponsableDecisionRequest request)
            => _repository.ResponderAcuerdo(reunionAcuerdoResponsableId, userId, request);

        private static void ValidarAcuerdo(ReunionAcuerdoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Descripcion))
                throw new AbrilException("La descripción del acuerdo es obligatoria.", 400);
        }

        private static void ValidarHoras(TimeOnly? inicio, TimeOnly? fin)
        {
            if (inicio.HasValue && fin.HasValue && fin.Value <= inicio.Value)
                throw new AbrilException("La hora de término debe ser mayor a la hora de inicio.", 400);
        }

        // ── Recordatorio de agenda (job) ──────────────────────────────────────
        private static readonly TimeOnly HorarioLaboralInicio = new(7, 0);
        private static readonly TimeOnly HorarioLaboralFin = new(19, 0);

        public async Task<object> ProcesarRecordatoriosAgenda()
        {
            var candidatos = await _repository.GetCandidatosRecordatorioAgenda();
            // Fecha/HoraInicio de la reunión se ingresan en hora Perú (naive, sin zona horaria).
            var ahora = DateTime.UtcNow.AddHours(-5);

            var enviados = 0;
            foreach (var c in candidatos)
            {
                if (c.Destinatarios.Count == 0) continue;

                var horaEnvio = await ResolverHoraEnvioRecordatorio(c.Fecha, c.HoraInicio, c.RecordatorioHorasAntes);
                if (ahora < horaEnvio) continue;

                var link = $"{_frontendUrl}/projects/actas-reunion/{c.ReunionId}/agenda";
                var emails = c.Destinatarios.Select(d => d.Email).Distinct().ToList();

                try
                {
                    await _emailService.SendAsync(
                        to: emails,
                        subject: $"Carga tu agenda — Reunión N° {c.Numero}: {c.Tema}",
                        body: BuildCuerpoRecordatorioAgenda(c, link),
                        isHtml: true);

                    await _notificacionesService.CrearPorCorreosAsync(
                        NotificacionTipoCodigo.ActasReunionAgenda,
                        emails,
                        null,
                        new[]
                        {
                            new NuevaNotificacionDto
                            {
                                Titulo = $"Carga tu agenda: {c.Tema}",
                                Subtitulo = $"Reunión N° {c.Numero} — {c.AmbitoDescripcion}",
                                Descripcion = $"Se realiza el {c.Fecha:dd/MM/yyyy} a las {c.HoraInicio:HH:mm}.",
                                Referencia = link,
                            },
                        });

                    await _repository.RegistrarRecordatorioEnviado(c.ReunionId);
                    enviados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando recordatorio de agenda de la reunión {ReunionId}", c.ReunionId);
                }
            }

            return new { revisados = candidatos.Count, enviados };
        }

        /// <summary>
        /// Hora a la que se debe avisar: hora de inicio menos las horas de anticipación
        /// configuradas, acotada a horario laboral (07:00–19:00) y día hábil. Si el resultado
        /// crudo cae fuera de esa ventana, se retrocede al cierre (19:00) del día hábil anterior
        /// — así una reunión a las 8:00am con 15.5h de anticipación avisa a las 16:30 del día hábil
        /// anterior, sin necesitar una regla aparte para "el día antes".
        /// </summary>
        private async Task<DateTime> ResolverHoraEnvioRecordatorio(DateOnly fecha, TimeOnly horaInicio, decimal horasAntes)
        {
            var target = fecha.ToDateTime(horaInicio).AddHours(-(double)horasAntes);

            for (var i = 0; i < 30; i++)
            {
                var targetDate = DateOnly.FromDateTime(target);
                var esDiaHabil = target.DayOfWeek != DayOfWeek.Saturday
                    && target.DayOfWeek != DayOfWeek.Sunday
                    && !await _repository.EsFeriado(targetDate);

                if (!esDiaHabil)
                {
                    target = targetDate.AddDays(-1).ToDateTime(HorarioLaboralFin);
                    continue;
                }
                if (TimeOnly.FromDateTime(target) < HorarioLaboralInicio)
                {
                    target = targetDate.AddDays(-1).ToDateTime(HorarioLaboralFin);
                    continue;
                }
                if (TimeOnly.FromDateTime(target) > HorarioLaboralFin)
                {
                    target = targetDate.ToDateTime(HorarioLaboralFin);
                    continue;
                }
                break;
            }

            return target;
        }

        private static string BuildCuerpoConvocatoria(ReunionConvocatoriaInfoDto c, string link)
        {
            var hora = c.HoraInicio.HasValue ? c.HoraInicio.Value.ToString("HH:mm") : "por confirmar";
            var lugarHtml = string.IsNullOrWhiteSpace(c.Lugar) ? "" : $"<p>Lugar: {c.Lugar}</p>";
            return $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px'>
  <div style='background:#0F6E56;padding:16px 24px;border-radius:8px 8px 0 0'>
    <h2 style='color:#fff;margin:0;font-size:18px'>Convocatoria — Reunión N° {c.Numero}</h2>
  </div>
  <div style='background:#f8fafc;padding:24px;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p><strong>{c.Tema}</strong></p>
    <p>{c.AmbitoDescripcion} — {c.Fecha:dd/MM/yyyy} a las {hora}</p>
    {lugarHtml}
    <p>Fuiste agregado como participante de esta reunión.</p>
    <div style='margin:24px 0;text-align:center'>
      <a href='{link}'
         style='background:#0F6E56;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:bold'>
        Ver detalle de la reunión
      </a>
    </div>
  </div>
</div>";
        }

        private static string BuildCuerpoRecordatorioAgenda(ReunionRecordatorioCandidatoDto c, string link)
        {
            return $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px'>
  <div style='background:#0F6E56;padding:16px 24px;border-radius:8px 8px 0 0'>
    <h2 style='color:#fff;margin:0;font-size:18px'>Recordatorio de Agenda — Reunión N° {c.Numero}</h2>
  </div>
  <div style='background:#f8fafc;padding:24px;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p><strong>{c.Tema}</strong></p>
    <p>{c.AmbitoDescripcion} — {c.Fecha:dd/MM/yyyy} a las {c.HoraInicio:HH:mm}</p>
    <p>Antes de la reunión, por favor carga los temas que quieres tratar.</p>
    <div style='margin:24px 0;text-align:center'>
      <a href='{link}'
         style='background:#0F6E56;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:bold'>
        Cargar mi agenda
      </a>
    </div>
  </div>
</div>";
        }
    }
}
