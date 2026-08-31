using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Shared.Email;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.Firma.Interfaces;
using Abril_Backend.Shared.Services.Pdf;
using Abril_Backend.Features.GestionAdministrativa.Shared.Services;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using ClosedXML.Excel;
using Humanizer;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Services
{
    public class GestionSalidaService : IGestionSalidaService
    {
        private readonly IGestionSalidaRepository _repo;
        private readonly IGraphSharePointService _sharePointService;
        private readonly ISolicitudSalidaService _solicitudSalidaService;
        private readonly ISalidaVisibilityResolver _visibilityResolver;
        private readonly IConsolidadoS10Service _consolidadoService;
        private readonly IFirmaPersonalRepository _firmaRepository;
        private readonly ICorreoSalidaRecipientResolver _correoResolver;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GestionSalidaService> _logger;

        public GestionSalidaService(
            IGestionSalidaRepository repo,
            IGraphSharePointService sharePointService,
            ISolicitudSalidaService solicitudSalidaService,
            ISalidaVisibilityResolver visibilityResolver,
            IConsolidadoS10Service consolidadoService,
            IFirmaPersonalRepository firmaRepository,
            ICorreoSalidaRecipientResolver correoResolver,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<GestionSalidaService> logger)
        {
            _repo = repo;
            _sharePointService = sharePointService;
            _solicitudSalidaService = solicitudSalidaService;
            _visibilityResolver = visibilityResolver;
            _consolidadoService = consolidadoService;
            _firmaRepository = firmaRepository;
            _correoResolver = correoResolver;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<GestionSalidaListItemDto>> GetAll(GestionSalidaFiltersDto filters)
        {
            await ApplyVisibilityAsync(filters);
            return await _repo.GetAll(filters);
        }

        public async Task<Abril_Backend.Application.DTOs.PagedResult<GestionSalidaListItemDto>> GetPaged(GestionSalidaFiltersDto filters)
        {
            await ApplyVisibilityAsync(filters);
            return await _repo.GetPaged(filters);
        }

        public async Task<GestionSalidaFilterDataDto> GetFilterData(int? currentUserId, bool seesAllOverride, bool tieneRolTesorero = false)
        {
            // Resuelve el alcance del usuario y recorta trabajadores + árbol de áreas a ese alcance
            // (área del usuario hacia abajo). Recepción / GTH / sin usuario → ve todo, sin recorte.
            //   • El árbol: los nodos tope del conjunto visible (cuyo padre queda fuera) los toma el
            //     frontend como raíces del cascada, así un jefe arranca en su área y un gerente en su
            //     gerencia sin lógica adicional en el cliente.
            //   • Los trabajadores: solo los de las áreas visibles (más el propio usuario).
            bool seesAll = seesAllOverride || !currentUserId.HasValue;
            var visibleIds = new List<int>();
            var esTesorero = false;

            // Con el rol de tesorero hay que resolver igual aunque ya vea todo: la categoría del
            // puesto es lo que decide el modo, y de ahí sale el flag que arma la pantalla.
            if (!seesAll || (tieneRolTesorero && currentUserId.HasValue))
            {
                var vis = await _visibilityResolver.ResolveAsync(currentUserId!.Value);
                esTesorero = tieneRolTesorero && vis.EsCategoriaTesorero;
                // El tesorero filtra sobre TODA la organización (su recorte es por estado, no por
                // área), así que sus desplegables tienen que traer el árbol completo.
                seesAll = seesAll || vis.SeesAll || esTesorero;
                visibleIds = vis.AreaScopeIds.ToList();
            }

            var data = await _repo.GetFilterData(seesAll, visibleIds, currentUserId);
            data.EsTesorero = esTesorero;
            return data;
        }

        /// <summary>
        /// Resuelve el alcance de visibilidad del usuario actual y lo escribe en el filtro
        /// (SeesAll / VisibleAreaScopeIds). Sin CurrentUserId no se aplica restricción.
        /// </summary>
        private async Task ApplyVisibilityAsync(GestionSalidaFiltersDto filters)
        {
            if (!filters.CurrentUserId.HasValue) return;

            // USUARIO DE RECEPCIÓN se sobrepone al alcance por área: ve todo, sin resolver. No
            // aplica si además trae el rol de tesorero: ahí sí hay que resolver, porque la
            // categoría del puesto es lo que decide si entra en modo tesorería.
            if (filters.SeesAllOverride && !filters.TieneRolTesorero)
            {
                filters.SeesAll = true;
                return;
            }

            var vis = await _visibilityResolver.ResolveAsync(filters.CurrentUserId.Value);

            // Modo TESORERÍA: rol + categoría del puesto, las dos cosas. Ve todas las áreas pero
            // solo lo firmado y lo pagado (el recorte de estados lo aplica el repositorio).
            filters.EsTesorero = filters.TieneRolTesorero && vis.EsCategoriaTesorero;

            filters.SeesAll = filters.SeesAllOverride || vis.SeesAll || filters.EsTesorero;
            filters.VisibleAreaScopeIds = vis.AreaScopeIds.ToList();
        }

        public async Task<byte[]> GetExcel(GestionSalidaFiltersDto filters)
        {
            await ApplyVisibilityAsync(filters);
            var salidas = await _repo.GetAll(filters);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Gestión de Salidas");

            string[] headers =
            [
                "#", "Trabajador", "Área", "Revisor", "Fecha salida", "Hora salida", "Hora retorno",
                "Motivo", "Origen", "Destino", "Aprobación", "Rendición", "Reembolso", "Registrada",
            ];

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.FromHtml("#64BC04");
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5F7D1");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            for (int r = 0; r < salidas.Count; r++)
            {
                var s   = salidas[r];
                int row = r + 2;

                ws.Cell(row, 1).Value  = r + 1;
                ws.Cell(row, 2).Value  = s.Trabajador;
                ws.Cell(row, 3).Value  = s.Area          ?? "—";
                ws.Cell(row, 4).Value  = s.RevisorNombre ?? "—";
                ws.Cell(row, 5).Value  = s.FechaSalida.ToString("dd/MM/yyyy");
                ws.Cell(row, 6).Value  = s.HoraSalida.ToString("HH:mm");
                ws.Cell(row, 7).Value  = s.HoraRetorno.HasValue ? s.HoraRetorno.Value.ToString("HH:mm") : "—";
                ws.Cell(row, 8).Value  = s.Motivo;
                ws.Cell(row, 9).Value  = s.LugarOrigen  ?? "—";
                ws.Cell(row, 10).Value = s.LugarDestino ?? "—";
                ws.Cell(row, 11).Value = s.EstadoAprobacion;
                ws.Cell(row, 12).Value = s.EstadoRendicion;
                // El reembolso solo tiene sentido cuando ya hay algo que revisar (salida rendida y
                // Consolidado del S10 adjunto): antes de eso la celda va vacía en vez de decir
                // "Pendiente", que se leería como si estuviera esperando a alguien.
                ws.Cell(row, 13).Value = s.ReembolsoRevisable || s.EstadoReembolso != "Pendiente"
                    ? s.EstadoReembolso
                    : "";
                ws.Cell(row, 14).Value = s.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm");

                var rowRange = ws.Range(row, 1, row, headers.Length);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
                rowRange.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;

                if (r % 2 == 0)
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FAFAFA");

                var aprobacionCell = ws.Cell(row, 11);
                aprobacionCell.Style.Font.Bold = true;
                aprobacionCell.Style.Font.FontColor = s.EstadoAprobacion switch
                {
                    "Aprobado"  => XLColor.FromHtml("#009C87"),
                    "Rechazado" => XLColor.FromHtml("#D30000"),
                    _           => XLColor.FromHtml("#92400E"),
                };

                var rendicionCell = ws.Cell(row, 12);
                rendicionCell.Style.Font.Bold = true;
                rendicionCell.Style.Font.FontColor = s.EstadoRendicion == "Rendido"
                    ? XLColor.FromHtml("#0086A5")
                    : XLColor.FromHtml("#9CA3AF");

                var reembolsoCell = ws.Cell(row, 13);
                reembolsoCell.Style.Font.Bold = true;
                reembolsoCell.Style.Font.FontColor = s.EstadoReembolso switch
                {
                    "Aprobado"  => XLColor.FromHtml("#009C87"),
                    "Rechazado" => XLColor.FromHtml("#D30000"),
                    "Firmado"   => XLColor.FromHtml("#4338CA"),
                    "Pagado"    => XLColor.FromHtml("#15803D"),
                    _           => XLColor.FromHtml("#92400E"),
                };
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task Aprobar(int id, int reviewerUserId)
        {
            await _repo.Aprobar(id, reviewerUserId);
            // Email de confirmación al solicitante (best-effort, no rompe el flujo si falla)
            await _solicitudSalidaService.NotifySolicitanteAprobada(id);
        }

        public async Task Rechazar(int id, int reviewerUserId)
        {
            await _repo.Rechazar(id, reviewerUserId);
            // Email de rechazo al solicitante (best-effort, no rompe el flujo si falla)
            await _solicitudSalidaService.NotifySolicitanteRechazada(id);
        }

        // El solicitante cancela su propia salida. Misma lógica que el autoservicio de Solicitud
        // de Salidas: no duplicamos el guard de propiedad/estado, lo delegamos.
        public Task Cancelar(int id, int userId) => _solicitudSalidaService.Cancelar(id, userId);

        public Task SetHoraSalidaReal(int id, TimeOnly? hora, int registradaPorUserId)
            => _repo.SetHoraSalidaReal(id, hora, registradaPorUserId);

        public Task SetHoraRetornoReal(int id, TimeOnly? hora, int registradaPorUserId)
            => _repo.SetHoraRetornoReal(id, hora, registradaPorUserId);

        public async Task<(byte[] Pdf, int Count)> RendirYGenerarPlanilla(IEnumerable<int> ids, int userId, int? ownerUserId = null)
        {
            var idsList = ids?.Distinct().ToList() ?? new List<int>();

            // 0. Guard de propiedad: cuando la rendición la dispara el propio trabajador desde su
            //    autoservicio, solo puede rendir solicitudes suyas.
            if (ownerUserId.HasValue)
            {
                var ajenas = await _repo.GetIdsNotOwnedByUser(idsList, ownerUserId.Value);
                if (ajenas.Count > 0)
                    throw new AbrilException("Solo puedes rendir tus propias solicitudes de salida.", 403);
            }

            // 1. Pre-flight: ¿cuáles serían marcables? — sin tocar BD.
            var elegiblesIds = await _repo.GetEligibleIdsForRendicion(idsList);
            if (elegiblesIds.Count == 0)
                throw new AbrilException("No hay solicitudes elegibles para rendir (deben estar aprobadas y no rendidas).", 400);

            // 1.b. Bloqueo: cada trayecto de cada solicitud debe estar cubierto.
            //       Regla normal: trayecto con al menos 1 captura.
            //       Regla TI (Tecnología de la Información): captura O match contra ga_trayecto.
            var sinCapturas = await _repo.GetIdsConTrayectosSinCapturas(elegiblesIds);
            if (sinCapturas.Count > 0)
                throw new AbrilException(
                    $"No se puede rendir: {sinCapturas.Count} solicitud(es) tienen trayectos sin cubrir (IDs: {string.Join(", ", sinCapturas)}). " +
                    "Cada trayecto debe tener al menos una captura con monto, o (para trabajadores de Tecnología de la Información) un trayecto registrado en el catálogo.",
                    400);

            // 2. Cargar info, consumir el correlativo de planilla y generar PDF en memoria.
            var datos          = await _repo.GetRendicionData(elegiblesIds);
            var numeroPlanilla = await _repo.GetNextNumeroPlanillaAsync();
            var numeroLabel    = $"TI: {numeroPlanilla:D6}";
            var pdf            = GenerarPlanillaPdf(datos, numeroLabel);

            // 3. Subir a SharePoint ANTES de marcar como rendidas.
            //    Si el upload falla, no se modifica nada en BD (estricto).
            //    Carpeta destino configurable desde BD (ga_rendicion_folder): se guarda el link tal
            //    cual y se resuelve a driveId/folderId vía Graph. Editable sin redeploy y cada
            //    entorno apunta a su propia biblioteca. Mismo patrón que ga_captura_folder.
            var folderUrl = await _repo.GetRendicionFolderUrl();
            if (string.IsNullOrWhiteSpace(folderUrl))
                throw new AbrilException(
                    "No se ha configurado la carpeta de SharePoint donde guardar las planillas de rendición. " +
                    "Pide al administrador registrarla en la tabla ga_rendicion_folder.", 409);

            var carpeta = await _sharePointService.ResolveSharePointFolderUrlAsync(folderUrl);
            if (carpeta == null || !carpeta.IsFolder)
                throw new AbrilException("No se pudo resolver la carpeta de planillas de rendición en SharePoint.", 502);

            var filename = $"Planilla_Rendicion_{DateTime.Now:yyyyMMdd_HHmmss}_u{userId}.pdf";
            string pdfUrl;
            string? pdfItemId;
            try
            {
                using var pdfStream = new MemoryStream(pdf);
                var result = await _sharePointService.UploadToOneDriveFolderAsync(
                    carpeta.DriveId, carpeta.ItemId, filename, pdfStream,
                    "application/pdf",
                    autoRenameOnLock: true);

                if (result?.WebUrl is null)
                    throw new AbrilException("No se pudo subir la planilla a SharePoint (respuesta vacía).", 502);

                pdfUrl    = result.WebUrl;
                pdfItemId = result.ItemId;
            }
            catch (AbrilException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló upload de planilla a SharePoint (filename={Filename}). Rendición abortada.", filename);
                throw new AbrilException(
                    "No se pudo guardar la planilla en SharePoint. La rendición fue cancelada — vuelve a intentarlo.",
                    502);
            }

            // 4. Persistir GaRendicion + marcar solicitudes (transacción interna).
            var rendidasIds = await _repo.CrearRendicionYMarcarBulk(
                elegiblesIds, userId, pdfUrl, pdfItemId, filename, numeroPlanilla);

            return (pdf, rendidasIds.Count);
        }

        public async Task<(byte[] Pdf, int Count)> RendirMesAnterior(GestionSalidaFiltersDto filters, int userId)
        {
            // El estado y el rango los fija la acción; los filtros de trabajador/área/proyecto que
            // trae la pantalla se respetan tal cual (se rinde lo que el usuario está viendo).
            var (desde, hasta) = MesAnteriorPeru.Rango();
            filters.SoloHoy          = false;
            filters.FechaSalidaDesde = desde;
            filters.FechaSalidaHasta = hasta;
            filters.EstadoAprobacion = EstadosSalida.Aprobacion.NombreAprobado;
            filters.EstadoRendicion  = EstadosSalida.Rendicion.NombreNoRendido;

            // GetAll ya resuelve la visibilidad y calcula PuedeRendirse (capturas / catálogo TI),
            // así que la elegibilidad sale de ahí sin duplicar reglas.
            var candidatas = await GetAll(filters);
            var ids = candidatas.Where(x => x.PuedeRendirse).Select(x => x.Id).ToList();
            if (ids.Count == 0)
                throw new AbrilException(
                    $"No hay salidas listas para rendir entre el {desde:dd/MM/yyyy} y el {hasta:dd/MM/yyyy}. " +
                    "Deben estar aprobadas, sin rendir y con las capturas de todos sus trayectos.", 400);

            return await RendirYGenerarPlanilla(ids, userId);
        }

        public Task<ConsolidadoS10Dto> UploadConsolidadoS10(int solicitudId, ConsolidadoS10Ambito ambito, IFormFile file, int userId)
            // Sin guard de propiedad: en Gestión de Salidas se administran las salidas de otros.
            => _consolidadoService.Upload(solicitudId, ambito, file, userId);

        public Task<GestionSalidaDetalleDto?> GetDetalle(int id)
            => _repo.GetDetalle(id);

        // ══ Reembolso ═══════════════════════════════════════════════════════

        public async Task<ReembolsoBulkResultDto> DecidirReembolso(
            IEnumerable<int> ids, bool aprobar, string? observacion, int reviewerUserId)
        {
            var decididas = await _repo.DecidirReembolso(ids, aprobar, observacion, reviewerUserId);

            // El aviso al solicitante es best-effort: la decisión ya está guardada y no se revierte
            // porque un correo falle (mismo criterio que la aprobación de la salida).
            foreach (var id in decididas)
                await NotificarDecisionReembolsoAsync(id, aprobar);

            return new ReembolsoBulkResultDto
            {
                Procesadas = decididas.Count,
                Message = aprobar
                    ? $"{decididas.Count} reembolso(s) aprobado(s)."
                    : $"{decididas.Count} reembolso(s) rechazado(s).",
            };
        }

        public async Task<ReembolsoBulkResultDto> FirmarPlanillas(IEnumerable<int> ids, int userId)
        {
            var firma = await _firmaRepository.GetActiveBytesByUserId(userId)
                // 409 y no 400: la pantalla lo distingue para abrir el modal donde el usuario dibuja
                // su firma en el momento en vez de mandarlo a Configuración.
                ?? throw new AbrilException(
                    "Todavía no registraste tu firma. Dibújala una vez y vuelve a firmar.", 409);

            var planillas = await _repo.GetRendicionesPorFirmar(ids);
            if (planillas.Count == 0)
                throw new AbrilException(
                    "Ninguna de las salidas seleccionadas se puede firmar: primero hay que aprobar su reembolso.",
                    400);

            var folderUrl = await _repo.GetRendicionFolderUrl();
            if (string.IsNullOrWhiteSpace(folderUrl))
                throw new AbrilException(
                    "No se ha configurado la carpeta de SharePoint donde guardar las planillas de rendición. " +
                    "Pide al administrador registrarla en la tabla ga_rendicion_folder.", 409);

            var carpeta = await _sharePointService.ResolveSharePointFolderUrlAsync(folderUrl);
            if (carpeta == null || !carpeta.IsFolder)
                throw new AbrilException("No se pudo resolver la carpeta de planillas de rendición en SharePoint.", 502);

            var totalSalidas = 0;
            var totalPlanillas = 0;

            foreach (var planilla in planillas)
            {
                string? pdfUrl = null, pdfItemId = null, pdfFilename = null;

                // Si la planilla ya está firmada no se vuelve a estampar: el documento es uno solo
                // y ya lleva la firma. Solo se mueven de estado las salidas que faltaban.
                if (string.IsNullOrWhiteSpace(planilla.PdfFirmadoUrl))
                {
                    byte[] original;
                    try
                    {
                        original = await _sharePointService.DownloadOneDriveFileByWebUrlAsync(planilla.PdfUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo descargar la planilla {RendicionId} para firmarla.", planilla.RendicionId);
                        throw new AbrilException(
                            "No se pudo descargar la planilla de rendición desde SharePoint para firmarla.", 502);
                    }

                    byte[] firmado;
                    try
                    {
                        // Todas las páginas: una planilla agrupa a varios trabajadores y cada grupo
                        // termina con su propia línea de firma de jefatura, así que firmar solo la
                        // última hoja dejaría sin firma a todos los grupos menos el último.
                        firmado = SignaturePdfStamper.Stamp(original, firma.Bytes, SignatureStampScope.AllPages);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo estampar la firma en la planilla {RendicionId}.", planilla.RendicionId);
                        throw new AbrilException("No se pudo generar la planilla firmada.", 500);
                    }

                    pdfFilename = Path.GetFileNameWithoutExtension(planilla.PdfFilename) + "-FIRMADO.pdf";
                    try
                    {
                        using var stream = new MemoryStream(firmado);
                        var subido = await _sharePointService.UploadToOneDriveFolderAsync(
                            carpeta.DriveId, carpeta.ItemId, pdfFilename, stream,
                            "application/pdf", autoRenameOnLock: true);

                        if (subido?.WebUrl is null)
                            throw new AbrilException("No se pudo subir la planilla firmada a SharePoint (respuesta vacía).", 502);

                        pdfUrl    = subido.WebUrl;
                        pdfItemId = subido.ItemId;
                    }
                    catch (AbrilException) { throw; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falló la subida de la planilla firmada {RendicionId}.", planilla.RendicionId);
                        throw new AbrilException("No se pudo guardar la planilla firmada en SharePoint.", 502);
                    }

                    totalPlanillas++;
                }

                await _repo.MarcarFirmadas(
                    planilla.RendicionId, planilla.SolicitudIds, userId, pdfUrl, pdfItemId, pdfFilename);
                totalSalidas += planilla.SolicitudIds.Count;
            }

            return new ReembolsoBulkResultDto
            {
                Procesadas        = totalSalidas,
                PlanillasFirmadas = totalPlanillas,
                Message           = $"{totalSalidas} salida(s) firmada(s).",
            };
        }

        public async Task<ReembolsoBulkResultDto> MarcarPagadas(IEnumerable<int> ids, int tesoreroUserId)
        {
            // Las dos condiciones de tesorero: el rol lo verificó el controller contra el token;
            // acá se verifica la otra mitad, que el puesto sea de categoría Tesorero. Se paga poco
            // y de a lotes, así que la consulta extra no pesa.
            var vis = await _visibilityResolver.ResolveAsync(tesoreroUserId);
            if (!vis.EsCategoriaTesorero)
                throw new AbrilException(
                    "Marcar como pagado es de Tesorería: tu puesto no es de categoría Tesorero.", 403);

            var pagadas = await _repo.MarcarPagadas(ids, tesoreroUserId);
            return new ReembolsoBulkResultDto
            {
                Procesadas = pagadas.Count,
                Message    = $"{pagadas.Count} reembolso(s) marcado(s) como pagado(s).",
            };
        }

        /// <summary>
        /// Avisa al solicitante que su reembolso quedó aprobado o rechazado. Respeta la
        /// configuración de correos (Gestión Administrativa → Configuración → Correos): si el
        /// correo está apagado o no queda ningún destinatario, no se envía nada.
        /// </summary>
        private async Task NotificarDecisionReembolsoAsync(int solicitudId, bool aprobado)
        {
            try
            {
                var info = await _repo.GetReembolsoCorreoInfo(solicitudId);
                if (info == null) return;

                if (string.IsNullOrWhiteSpace(info.SolicitanteEmail))
                {
                    _logger.LogWarning(
                        "Reembolso {SolicitudId}: el solicitante no tiene correo registrado, no se avisó la decisión.",
                        solicitudId);
                    return;
                }

                var codigo = aprobado
                    ? CorreoEventoCodigos.ReembolsoAprobado
                    : CorreoEventoCodigos.ReembolsoRechazado;

                var envio = await _correoResolver.ResolveEnvioAsync(
                    codigo, new List<string> { info.SolicitanteEmail });

                if (!envio.Enviar)
                {
                    _logger.LogInformation(
                        "Correo {Codigo} no enviado para la salida {SolicitudId}: está apagado o sin destinatarios.",
                        codigo, solicitudId);
                    return;
                }

                var layout = SalidaEmailLayout.Desde(_configuration);
                var datos  = ToCorreoDatos(info);
                var url    = SalidaEnlaces.Autoservicio(_configuration, solicitudId);

                var body = aprobado
                    ? ReembolsoEmailTemplates.Aprobado(layout, datos, url)
                    : ReembolsoEmailTemplates.Rechazado(layout, datos, url);

                var subject = aprobado
                    ? $"Reembolso APROBADO - salida del {info.FechaSalida:dd/MM/yyyy}"
                    : $"Reembolso RECHAZADO - salida del {info.FechaSalida:dd/MM/yyyy}";

                await _emailService.SendAsync(
                    to: envio.Para,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cc: envio.Copia.Count > 0 ? envio.Copia : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error avisando la decisión del reembolso de la salida {SolicitudId}", solicitudId);
            }
        }

        /// <summary>Pasa los datos del repositorio al shape que consumen las plantillas.</summary>
        private static ReembolsoCorreoDatos ToCorreoDatos(ReembolsoCorreoInfoDto info) =>
            new()
            {
                SolicitudId     = info.SolicitudId,
                NumeroUsuario   = info.NumeroUsuario,
                Trabajador      = info.Trabajador,
                TrabajadorEmail = info.SolicitanteEmail,
                Area            = info.Area,
                FechaSalida     = info.FechaSalida,
                NumeroPlanilla  = info.NumeroPlanilla,
                TrayectosCount  = info.TrayectosCount,
                MontoTotal      = info.MontoTotal,
                DecididoPor     = info.DecididoPor,
                Observacion     = info.ObservacionReembolso,
            };

        // ── Generación de la planilla de gasto por movilidad (QuestPDF) ──────

        private const int FilasPorPagina = 15;

        /// <summary>Tamaño de letra de las celdas de la tabla — reducido para que entren 15 filas/página.</summary>
        private const float TablaFontSize = 7.5f;

        /// <summary>Máximo de líneas que puede ocupar el texto de una celda de la tabla.</summary>
        private const int TablaMaxLineas = 2;

        /// <summary>
        /// Texto de la columna MOTIVO de la planilla: el motivo del catálogo con su detalle pegado
        /// cuando el motivo lo exige (requiere_motivo_adicional), que es lo que justifica el gasto.
        /// La celda recorta a <see cref="TablaMaxLineas"/> líneas, así que el detalle se captura
        /// acotado en el formulario para que entre junto al motivo.
        /// </summary>
        private static string MotivoConDetalle(string? motivo, string? motivoAdicional) =>
            string.IsNullOrWhiteSpace(motivoAdicional)
                ? (motivo ?? "")
                : $"{motivo} — {motivoAdicional}";

        private static string LogoPath() => Path.Combine(
            AppContext.BaseDirectory,
            "Features", "GestionAdministrativaModule", "Features", "GestionSalidasFeature",
            "Templates", "logo-abril.jpg");

        private static byte[]? _logoBytes;
        private static byte[]? GetLogoBytes()
        {
            if (_logoBytes != null) return _logoBytes;
            var path = LogoPath();
            if (!File.Exists(path)) return null;
            _logoBytes = File.ReadAllBytes(path);
            return _logoBytes;
        }

        private static byte[] GenerarPlanillaPdf(List<RendicionItemDto> items, string numeroLabel)
        {
            var grupos = items.GroupBy(x => x.WorkerId).Select(g => g.ToList()).ToList();
            if (grupos.Count == 0) grupos.Add(new List<RendicionItemDto>());

            var paginas = new List<(List<RendicionItemDto> trabajadorItems, List<RendicionItemDto> pageItems, bool isLast, int pageNum, int totalPages)>();
            foreach (var g in grupos)
            {
                int totalPages = g.Count == 0 ? 1 : (int)Math.Ceiling(g.Count / (double)FilasPorPagina);
                for (int p = 0; p < totalPages; p++)
                {
                    var pageItems = g.Skip(p * FilasPorPagina).Take(FilasPorPagina).ToList();
                    paginas.Add((g, pageItems, p == totalPages - 1, p + 1, totalPages));
                }
            }

            var logo = GetLogoBytes();

            var doc = Document.Create(container =>
            {
                foreach (var pag in paginas)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(25);
                        page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));

                        page.Content().Element(c => RenderPagina(c, pag.trabajadorItems, pag.pageItems, pag.isLast, pag.pageNum, pag.totalPages, logo, numeroLabel));

                        // Pie de página común a todas las páginas — número de registro (izq)
                        // y "Página X de Y" (der) cuando aplique.
                        var pageNum_   = pag.pageNum;
                        var totalPages_ = pag.totalPages;
                        page.Footer().PaddingTop(4).Row(footerRow =>
                        {
                            footerRow.RelativeItem().AlignLeft()
                                .Text(numeroLabel).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            footerRow.RelativeItem().AlignRight()
                                .Text(totalPages_ > 1 ? $"Página {pageNum_} de {totalPages_}" : "")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });
                }
            });

            return doc.GeneratePdf();
        }

        private static void RenderPagina(
            IContainer container,
            List<RendicionItemDto> trabajadorItems,
            List<RendicionItemDto> pageItems,
            bool isLastPage,
            int pageNum,
            int totalPages,
            byte[]? logo,
            string numeroLabel)
        {
            var first       = trabajadorItems.FirstOrDefault();
            var trabajador  = first?.TrabajadorNombre ?? "";
            var dni         = first?.TrabajadorDni    ?? "";
            var area        = first?.Area             ?? "";
            var razonSocial = first?.RazonSocial      ?? "";
            var ruc         = first?.Ruc              ?? "";
            // Label del documento: DNI (tipo 1) | CE (tipo 2) | DNI por defecto.
            var documentoLabel = first?.TrabajadorDocumentTypeId switch
            {
                2 => "CE:",
                _ => "DNI:",
            };
            string periodo  = trabajadorItems.Count > 0
                ? $"{trabajadorItems.Min(i => i.FechaSalida):dd/MM/yyyy}   AL   {trabajadorItems.Max(i => i.FechaSalida):dd/MM/yyyy}"
                : "";

            container.Column(col =>
            {
                col.Spacing(6);

                // ── Header: logo izquierda + caja título derecha ─────────────
                col.Item().Row(row =>
                {
                    row.ConstantItem(160).Height(45).Element(c =>
                    {
                        if (logo != null)
                            c.AlignLeft().AlignMiddle().Image(logo).FitArea();
                    });

                    row.RelativeItem(); // spacer

                    row.ConstantItem(380).Height(25).Row(titleRow =>
                    {
                        titleRow.RelativeItem(3).Border(1).AlignCenter().AlignMiddle()
                            .Text("PLANILLA DE GASTO POR MOVILIDAD Nº")
                            .FontSize(11).Bold();
                        titleRow.RelativeItem(1).Border(1).AlignCenter().AlignMiddle()
                            .Text(numeroLabel).FontSize(10).Bold();
                    });
                });

                // ── Info section ─────────────────────────────────────────────
                col.Item().PaddingTop(3).Column(info =>
                {
                    info.Spacing(2);

                    info.Item().Element(c => InfoLine(c, "RAZÓN SOCIAL:", razonSocial));

                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Element(c => InfoLine(c, "RUC:", ruc));
                        r.RelativeItem().Element(c => InfoLine(c, "NOMBRE DEL ÁREA/PROYECTO:", area));
                    });

                    info.Item().Element(c => InfoLine(c, "NOMBRES Y APELLIDOS:", trabajador));

                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Element(c => InfoLine(c, documentoLabel, dni));
                        r.RelativeItem().Element(c => InfoLine(c, "PERIODO DEL:", periodo));
                    });
                });

                // ── Tabla ────────────────────────────────────────────────────
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(70);   // FECHA
                        c.ConstantColumn(200);  // MOTIVO
                        c.ConstantColumn(180);  // ORIGEN
                        c.ConstantColumn(200);  // DESTINO
                        c.ConstantColumn(90);   // IMPORTE S/
                    });

                    table.Header(h =>
                    {
                        static IContainer Th(IContainer c) => c.Border(1).Background(Colors.Grey.Lighten4)
                            .PaddingVertical(3).AlignCenter().AlignMiddle();
                        h.Cell().Element(Th).Text("FECHA").Bold().FontSize(TablaFontSize);
                        h.Cell().Element(Th).Text("MOTIVO").Bold().FontSize(TablaFontSize);
                        h.Cell().Element(Th).Text("ORIGEN").Bold().FontSize(TablaFontSize);
                        h.Cell().Element(Th).Text("DESTINO").Bold().FontSize(TablaFontSize);
                        h.Cell().Element(Th).Text("IMPORTE S/").Bold().FontSize(TablaFontSize);
                    });

                    static IContainer Td(IContainer c) => c.Border(1).PaddingVertical(2).PaddingHorizontal(4).AlignMiddle();

                    // Texto de celda recortado a un máximo de 2 líneas (evita "textazos").
                    static void CeldaTexto(IContainer c, string value, bool center = false) =>
                        (center ? c.AlignCenter() : c)
                            .Text(value ?? "").FontSize(TablaFontSize).ClampLines(TablaMaxLineas);

                    foreach (var it in pageItems)
                    {
                        table.Cell().Element(Td).Element(c => CeldaTexto(c, it.FechaSalida.ToString("dd/MM/yyyy"), center: true));
                        table.Cell().Element(Td).Element(c => CeldaTexto(c, MotivoConDetalle(it.Motivo, it.MotivoAdicional)));
                        table.Cell().Element(Td).Element(c => CeldaTexto(c, it.LugarOrigen ?? ""));
                        table.Cell().Element(Td).Element(c => CeldaTexto(c, it.LugarDestino ?? ""));
                        // Importe: mostrar siempre que venga del catálogo (incluso si es 0.00)
                        // o cuando la suma de capturas sea > 0. Si el trayecto no tiene
                        // ninguna fuente, dejar la celda vacía.
                        table.Cell().Element(Td).AlignRight().Text(
                            (it.EsCatalogo || it.Importe > 0)
                                ? it.Importe.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("es-PE"))
                                : "").FontSize(TablaFontSize);
                    }

                    if (isLastPage)
                    {
                        var totalGeneral = trabajadorItems.Sum(i => i.Importe);
                        var totalEnLetras = MontoEnLetrasSoles(totalGeneral);
                        table.Cell().ColumnSpan(4).Border(1).PaddingVertical(7).PaddingHorizontal(8).AlignMiddle()
                            .Text(text =>
                            {
                                text.Span("TOTAL EN LETRAS: ").Bold();
                                text.Span(totalEnLetras);
                            });
                        table.Cell().Border(1).PaddingVertical(7).PaddingHorizontal(4).AlignMiddle().AlignRight()
                            .Text(totalGeneral.ToString("N2", CultureInfo.GetCultureInfo("es-PE"))).Bold();
                    }
                });

                // ── Firmas (solo última página del trabajador) ───────────────
                if (isLastPage)
                {
                    col.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem(); // mitad izquierda vacía: empuja la firma a la derecha
                        row.ConstantItem(60); // spacer
                        row.RelativeItem().AlignCenter().Column(fc =>
                        {
                            fc.Item().LineHorizontal(0.7f);
                            fc.Item().PaddingTop(2).AlignCenter()
                                .Text("Firma de Jefatura / Gerencia").FontSize(9).Italic();
                        });
                    });
                }

                // (El indicador "Página X de Y" se muestra ahora en el footer global de la página.)
            });
        }

        private static void InfoLine(IContainer container, string label, string value)
        {
            container.Row(r =>
            {
                r.ConstantItem(155).AlignMiddle().Text(label).Bold().FontSize(9);
                r.RelativeItem().BorderBottom(0.6f).BorderColor(Colors.Grey.Darken1)
                    .PaddingBottom(1).AlignMiddle()
                    .Text(value ?? "").FontSize(9);
            });
        }

        /// <summary>
        /// Convierte un monto a su representación en letras estilo peruano
        /// (ej. 350.50 → "TRESCIENTOS CINCUENTA CON 50/100 SOLES").
        /// </summary>
        private static string MontoEnLetrasSoles(decimal monto)
        {
            var abs       = Math.Abs(monto);
            var entero    = (long)Math.Truncate(abs);
            var centavos  = (int)Math.Round((abs - entero) * 100m);
            if (centavos == 100) { entero++; centavos = 0; }

            var esCulture = new CultureInfo("es");
            var palabras  = entero.ToWords(esCulture);
            var signo     = monto < 0 ? "MENOS " : string.Empty;
            return $"{signo}{palabras} CON {centavos:D2}/100 SOLES".ToUpperInvariant();
        }
    }
}
