using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Interfaces;
using Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Interfaces;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos;
using Abril_Backend.Shared.Services.Email.Interfaces;
using Abril_Backend.Shared.Services.Email.Dtos;
using Abril_Backend.Shared.Services.Graph.Interfaces;
using Abril_Backend.Shared.Services.Graph.Dtos;
using Abril_Backend.Shared.Services.SharePoint.Dtos;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;
using Microsoft.Extensions.Options;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Helpers;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Interfaces;
using ClosedXML.Excel;
using System.Text;
using Humanizer;
using System.Globalization;

namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Services
{
    public class ProjectSubContractorService : IProjectSubContractorService
    {
        private readonly IProjectSubContractorRepository _projectSubContractorRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IStorageContainerResolver _containerResolver;
        private readonly IProjectRepository _projectRepository;
        private readonly IDelegatedMailService _delegatedMailService;
        private readonly IEmailService _emailService;
        private readonly IGraphUserService _graphUserService;
        private readonly IGraphSharePointService _sharePointService;
        private readonly IAdjudicacionOneDriveStorage _oneDriveStorage;
        private readonly OneDriveOptions _oneDriveOptions;
        private readonly IProjectLinkRepository _projectLinkRepository;
        private readonly ICostosPresupuestosEmailService _costosPresupuestosEmailService;
        private readonly SharePointSiteRef _site;
        private readonly string _frontendUrl;

        private const string BccEmail = "calvarez@abril.pe";

        // ── Firma de correo ──────────────────────────────────────────────────
        private const string SignatureGifContentId = "abril-firma-logo";

        private static readonly Lazy<byte[]?> _signatureGifBytes = new(() =>
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "Shared", "Services", "Graph", "Resources", "abril-correo.gif");
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        });

        public ProjectSubContractorService(
            IProjectSubContractorRepository projectSubContractorRepository,
            IFileStorageService fileStorageService,
            IStorageContainerResolver containerResolver,
            IProjectRepository projectRepository,
            IDelegatedMailService delegatedMailService,
            IEmailService emailService,
            IGraphUserService graphUserService,
            IGraphSharePointService sharePointService,
            IAdjudicacionOneDriveStorage oneDriveStorage,
            IOptions<OneDriveOptions> oneDriveOptions,
            IProjectLinkRepository projectLinkRepository,
            ICostosPresupuestosEmailService costosPresupuestosEmailService,
            IConfiguration configuration)
        {
            _projectSubContractorRepository = projectSubContractorRepository;
            _fileStorageService = fileStorageService;
            _containerResolver = containerResolver;
            _projectRepository = projectRepository;
            _delegatedMailService = delegatedMailService;
            _emailService = emailService;
            _graphUserService = graphUserService;
            _sharePointService = sharePointService;
            _oneDriveStorage = oneDriveStorage;
            _oneDriveOptions = oneDriveOptions.Value;
            _projectLinkRepository = projectLinkRepository;
            _costosPresupuestosEmailService = costosPresupuestosEmailService;
            _site = SharePointSiteRef.FromConfig(configuration, "CostosYPresupuestos");
            _frontendUrl = configuration["App:FrontendUrl"]?.TrimEnd('/', '.') ?? string.Empty;
        }

        /// <summary>
        /// Oficina Técnica solo ve adjudicaciones de sus proyectos: aquellos donde su
        /// correo está registrado en Configuración → Correo de staff por proyecto
        /// (misma lógica que en Links de proyecto).
        /// </summary>
        private async Task ApplyOwnProjectsRestrictionAsync(ProjectSubContractorFilterDTO filter, int userId, bool restrictToOwnProjects)
        {
            if (!restrictToOwnProjects) return;
            filter.AllowedProjectIds = await _projectLinkRepository.GetUserProjectIdsAsync(userId);
        }

        public async Task<PagedResult<ProjectSubContractorDTO>> GetPaged(ProjectSubContractorFilterDTO filter, int userId, bool restrictToOwnProjects)
        {
            if (filter.Page < 1) filter.Page = 1;
            await ApplyOwnProjectsRestrictionAsync(filter, userId, restrictToOwnProjects);
            return await _projectSubContractorRepository.GetPaged(filter);
        }

        public async Task<ProjectSubContractorPagedWithFiltersDTO> GetPagedWithFilters(ProjectSubContractorFilterDTO filter, int userId, bool restrictToOwnProjects)
        {
            // Combina GetPaged + GetFormDataAsync en una sola llamada al repositorio.
            // Las operaciones se ejecutan en paralelo aprovechando el connection pooling.
            if (filter.Page < 1) filter.Page = 1;
            await ApplyOwnProjectsRestrictionAsync(filter, userId, restrictToOwnProjects);
            return await _projectSubContractorRepository.GetPagedWithFiltersAsync(filter);
        }

        public async Task<AdjudicacionDashboardDto> GetDashboard(ProjectSubContractorFilterDTO filter, bool includeFilterOptions, int userId, bool restrictToOwnProjects)
        {
            filter.AllowedProjectIds = restrictToOwnProjects
                ? await _projectLinkRepository.GetUserProjectIdsAsync(userId)
                : null;
            return await _projectSubContractorRepository.GetDashboardAsync(filter, includeFilterOptions);
        }

        /// <summary>
        /// Datos del paso 1 sin los que la adjudicación queda inservible más adelante. Se validan acá
        /// además del frontend porque la falla aparecía tarde y con un mensaje que no señalaba al dato:
        /// <list type="bullet">
        /// <item>Modalidad de contrato: elige la plantilla .docx del contrato y las cláusulas
        /// especiales de la partida de control. Sin ella se caía en una plantilla genérica inexistente
        /// y el paso 3 respondía "No se encontró la plantilla del contrato en el servidor".</item>
        /// <item>Especialidad y partida: son dos niveles de la ruta en OneDrive
        /// ({Carpeta}/{Especialidad}/{Partida}/...), así que sin ellas no se puede guardar ningún
        /// documento. Sin archivos iniciales el alta ni siquiera lo detectaba (el preflight de la ruta
        /// solo corre cuando hay algo que subir) y el error saltaba recién al generar el primer
        /// documento.</item>
        /// <item>Partida de control: de ella cuelgan el instructivo y las cláusulas especiales.</item>
        /// </list>
        /// </summary>
        private static void ValidateStep1Data(
            int? contractModalityId, int? workSpecialtyId, int workItemCategoryId, int workItemId)
        {
            var missing = new List<string>();

            if (!contractModalityId.HasValue || contractModalityId.Value <= 0) missing.Add("Modalidad de contrato");
            if (!workSpecialtyId.HasValue    || workSpecialtyId.Value    <= 0) missing.Add("Especialidad");
            if (workItemCategoryId <= 0)                                       missing.Add("Partida de control");
            if (workItemId <= 0)                                               missing.Add("Partida");

            if (missing.Count > 0)
                throw new AbrilException(
                    $"Faltan datos obligatorios: {string.Join(", ", missing)}.", 400);
        }

        public async Task Create(ProjectSubContractorCreateDTO dto, int userId)
        {
            ValidateStep1Data(dto.ContractModalityId, dto.WorkSpecialtyId, dto.WorkItemCategoryId, dto.WorkItemId);

            var hasInitialFiles = (dto.QuotationFiles?.Count ?? 0) > 0
                               || (dto.ComparativeFiles?.Count ?? 0) > 0;

            // Phase 0: si hay archivos que subir, se valida ANTES del INSERT que la ruta de OneDrive
            // se pueda resolver (carpeta 04_OBRAS configurada + especialidad asignada). Sin esto el
            // registro quedaba creado y visible en la lista, pero sin ningún archivo, porque el 422
            // de "Falta configuración" recién salta en la fase 2, con el INSERT ya committeado.
            if (hasInitialFiles)
            {
                var preflight = await _projectSubContractorRepository.GetCreatePathDataAsync(
                    dto.ProjectId, dto.WorkSpecialtyId);
                _oneDriveStorage.ValidateUploadPath(preflight);
            }

            // Phase 1: persist the record and get the new ID (needed for the folder path).
            var newId = await _projectSubContractorRepository.Create(dto, userId);

            try
            {
                // Phase 2: fetch path data and upload files to SharePoint.
                var pathData = await _projectSubContractorRepository.GetPathDataAsync(newId);

                var quotationFiles   = await UploadFilesToSharePoint(dto.QuotationFiles,   pathData, AdjudicacionDocumentType.InitialQuotation);
                var comparativeFiles = await UploadFilesToSharePoint(dto.ComparativeFiles, pathData, AdjudicacionDocumentType.InitialComparative);

                // Phase 3: save file records.
                await _projectSubContractorRepository.SaveInitialFilesAsync(newId, quotationFiles, comparativeFiles, userId);
            }
            catch
            {
                // El alta falló con el INSERT ya committeado (OneDrive caído, token vencido, archivo
                // vacío...). Se compensa con soft delete para no dejar una adjudicación sin archivos:
                // cada llamada al repositorio abre su propio contexto, así que nada se revierte solo.
                await TrySoftDeleteAfterFailedCreateAsync(newId, userId);
                throw;
            }
        }

        /// <summary>
        /// Compensación del alta. Nunca debe tapar la excepción original que la disparó, así que si
        /// el propio soft delete falla solo se registra y se deja propagar la excepción de arriba.
        /// </summary>
        private async Task TrySoftDeleteAfterFailedCreateAsync(int projectSubContractorId, int userId)
        {
            try
            {
                await _projectSubContractorRepository.SoftDeleteAsync(projectSubContractorId, userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Create] No se pudo revertir la adjudicación {projectSubContractorId} " +
                    $"tras fallar el alta: {ex.Message}");
            }
        }

        public async Task<ProjectSubContractorFormDataDTO> GetFormData()
        {
            // Usa una sola conexión a la BD compartida entre todas las queries del catálogo,
            // en vez de abrir 9 contextos paralelos. Ver ProjectSubContractorRepository.GetFormDataAsync.
            return await _projectSubContractorRepository.GetFormDataAsync();
        }

        private async Task<List<(string Url, string OriginalFileName, string? ItemId)>> UploadFilesToSharePoint(
            List<IFormFile>? files,
            AdjudicacionPathDataDto pathData,
            AdjudicacionDocumentType documentType)
        {
            if (files == null || files.Count == 0)
                return new List<(string, string, string?)>();

            var results = new List<(string Url, string OriginalFileName, string? ItemId)>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                    throw new AbrilException("Se detectó un archivo vacío.");

                using var stream = file.OpenReadStream();
                var spResult = await _oneDriveStorage.UploadAsync(
                    pathData, documentType, file.FileName, stream, file.ContentType);

                results.Add((spResult.WebUrl!, file.FileName, spResult.ItemId));
            }

            return results;
        }

        /// <summary>
        /// 422 — el proyecto no tiene asociado el tipo de correo requerido en
        /// Configuración → Correo de staff por proyecto. El frontend muestra este
        /// código como advertencia de configuración, no como error del sistema.
        /// </summary>
        private static AbrilException MissingStaffEmailConfig(string emailType) =>
            new(
                $"Falta asociar un correo de tipo \"{emailType}\" al proyecto de esta adjudicación. " +
                "Regístrelo en Configuración → Correo de staff por proyecto y vuelva a intentarlo.", 422);

        public async Task SendNotification(SendAdjudicacionNotificationDto dto, int userId)
        {
            var data = await _projectSubContractorRepository.GetNotificationData(dto.ProjectSubContractorId);

            // La cotización es opcional al crear la adjudicación, pero obligatoria para notificar:
            // es el adjunto del correo que se envía al pasar al paso 2. El cuadro comparativo sigue
            // siendo opcional en todos los pasos.
            if (data.QuotationFiles.Count == 0)
                throw new AbrilException(
                    "La adjudicación no tiene ningún archivo de cotización. Adjunte la cotización en el " +
                    "paso 1 antes de notificar al subcontratista.", 400);

            if (data.StaffEmails.Count == 0)
                throw MissingStaffEmailConfig("Staff de obra");

            // Expandir grupos: staff de obra (van a la matriz) y oficina central (solo CC).
            var staffProfiles          = await _graphUserService.GetResolvedProfilesAsync(data.StaffEmails);
            var oficinaCentralProfiles = await _graphUserService.GetResolvedProfilesAsync(data.OficinaCentralEmails);

            Console.WriteLine($"[SendNotification] StaffEmails ({data.StaffEmails.Count}): {string.Join(", ", data.StaffEmails)}");
            Console.WriteLine($"[SendNotification] Perfiles staff resueltos ({staffProfiles.Count}):");
            foreach (var p in staffProfiles)
                Console.WriteLine($"  - {p.Mail} → Nombre: {p.DisplayName}, Puesto: {p.JobTitle}, Teléfono: {p.Phone}");

            Console.WriteLine($"[SendNotification] OficinaCentralEmails ({data.OficinaCentralEmails.Count}): {string.Join(", ", data.OficinaCentralEmails)}");
            Console.WriteLine($"[SendNotification] Perfiles oficina central resueltos ({oficinaCentralProfiles.Count}):");
            foreach (var p in oficinaCentralProfiles)
                Console.WriteLine($"  - {p.Mail}");

            if (staffProfiles.Count == 0 && oficinaCentralProfiles.Count == 0)
                Console.WriteLine("  [ADVERTENCIA] No se obtuvo ningún perfil. Verificar token y permisos User.Read.All / GroupMember.Read.All.");

            var quotationAttachments = await DownloadAttachmentsAsync(data.QuotationFiles);

            var subject = $"{data.ProjectDescription} // {data.WorkItemDescription} // {data.ContributorName}";

            // CC = staff de obra (expandidos) + oficina central (expandidos) + equipo costos y presupuestos.
            var costosEmails            = await _costosPresupuestosEmailService.GetActiveEmails();
            var expandedStaff          = staffProfiles.Select(p => p.Mail).Where(m => !string.IsNullOrWhiteSpace(m));
            var expandedOficinaCentral = oficinaCentralProfiles.Select(p => p.Mail).Where(m => !string.IsNullOrWhiteSpace(m));
            var internalRecipients     = expandedStaff
                .Concat(expandedOficinaCentral)
                .Concat(costosEmails)
                .Distinct()
                .ToList();

            // --- Correo 1: interno — staff de obra + costos y presupuestos ---
            // TODO: descomentar cuando se defina el cuerpo y los destinatarios correctos
            // var firstEmailBody = BuildInternalEmailBody(data);
            // await _delegatedMailService.SendAsync(
            //     graphAccessToken: dto.GraphAccessToken,
            //     to: internalRecipients,
            //     subject: subject,
            //     body: firstEmailBody,
            //     isHtml: true,
            //     attachments: quotationAttachments.Concat(await DownloadAttachmentsAsync(data.ComparativeFiles)).ToList()
            // );

            // --- Correo único: subcontratista ---
            // TO: subcontratista | CC: todos los internos (staff + oficina central + costos) | Adjunto: cotización
            // Matriz de comunicaciones: solo staff de obra (staffProfiles).
            var senderProfile = await _graphUserService.GetCurrentUserProfileAsync(dto.GraphAccessToken);
            var signature     = BuildEmailSignature(senderProfile);
            var emailBody     = BuildSubcontractorEmailBody(data, staffProfiles) + signature;

            await _delegatedMailService.SendAsync(
                graphAccessToken: dto.GraphAccessToken,
                to:          data.ContractorEmails,
                subject:     subject,
                body:        emailBody,
                isHtml:      true,
                cc:          internalRecipients,
                attachments: WithSignatureAttachment(quotationAttachments)
            );

            // Actualizar estado de la adjudicación a 2 (notificada)
            await _projectSubContractorRepository.UpdateStatusToSent(dto.ProjectSubContractorId, userId);
        }

        public async Task<AdjudicacionRecipientsPreviewDto> GetNotificationRecipients(int projectSubContractorId)
        {
            var data = await _projectSubContractorRepository.GetNotificationData(projectSubContractorId);

            // CC = staff de obra + oficina central + equipo de costos y presupuestos (mismas
            // fuentes que el envío real). No se expanden grupos vía Graph ni se incluye el BCC.
            var costosEmails = await _costosPresupuestosEmailService.GetActiveEmails();
            var cc = data.StaffEmails
                .Concat(data.OficinaCentralEmails)
                .Concat(costosEmails)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new AdjudicacionRecipientsPreviewDto
            {
                To = data.ContractorEmails,
                Cc = cc
            };
        }

        public async Task UpdateStatusAsync(int projectSubContractorId, int statusId, int userId)
        {
            await _projectSubContractorRepository.UpdateStatus(projectSubContractorId, statusId, userId);
        }

        public async Task AdvanceToStep4Async(int projectSubContractorId, string graphAccessToken, int userId)
        {
            var data = await _projectSubContractorRepository.GetStep3ApprovalDataAsync(projectSubContractorId);

            if (data.OficinaTecnicaEmails.Count == 0)
                throw MissingStaffEmailConfig("Oficina Técnica");

            await _projectSubContractorRepository.UpdateStatus(projectSubContractorId, 4, userId);

            if (data.OficinaTecnicaEmails.Count > 0)
            {
                var costosEmailsCc = await _costosPresupuestosEmailService.GetActiveEmails();
                var senderProfile  = await _graphUserService.GetCurrentUserProfileAsync(graphAccessToken);
                var signature      = BuildEmailSignature(senderProfile);

                var body = new StringBuilder();
                body.AppendLine("<div style=\"font-family:Arial,sans-serif; font-size:13px; color:#333;\">");
                body.AppendLine("<p>Estimado equipo de Oficina Técnica,</p>");
                body.AppendLine("<p>Se le informa que los documentos han sido revisados, aprobados y la adjudicación avanza a la siguiente etapa. A continuación se detallan los datos:</p>");
                body.AppendLine("<ul>");
                body.AppendLine($"  <li><strong>Proyecto:</strong> {data.ProjectDescription}</li>");
                body.AppendLine($"  <li><strong>Contratista:</strong> {data.ContributorName}</li>");
                body.AppendLine($"  <li><strong>Partida:</strong> {data.WorkItemDescription}</li>");
                body.AppendLine("</ul>");
                body.AppendLine("<p>Por favor, acceda al sistema para revisar el detalle de la adjudicación.</p>");
                body.AppendLine("</div>");

                // Se envía vía Graph como el usuario autenticado (emisor = usuario actual),
                // no a través de un proveedor externo.
                await _delegatedMailService.SendAsync(
                    graphAccessToken: graphAccessToken,
                    to:               data.OficinaTecnicaEmails,
                    subject:          $"Adjudicación aprobada - {data.ProjectDescription} / {data.ContributorName}",
                    body:             body.ToString() + signature,
                    isHtml:           true,
                    cc:               costosEmailsCc,
                    attachments:      WithSignatureAttachment());
            }
        }

        public async Task SendScNotificationAsync(int projectSubContractorId, string graphAccessToken, IFormFile? file, int userId)
        {
            var data     = await _projectSubContractorRepository.GetScNotificationDataAsync(projectSubContractorId);
            var pathData = await _projectSubContractorRepository.GetPathDataAsync(projectSubContractorId);

            if (data.OficinaTecnicaEmails.Count == 0)
                throw MissingStaffEmailConfig("Oficina Técnica");

            byte[] fileBytes;
            string fileName;
            string contentType;

            if (file != null)
            {
                // Caso normal: el usuario subió o generó el archivo en esta sesión
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                fileBytes   = ms.ToArray();
                fileName    = file.FileName;
                contentType = file.ContentType ?? "application/pdf";
            }
            else
            {
                // Caso reapertura: el paquete ya estaba guardado en SharePoint — descargarlo por URL
                var pkgInfo = await _projectSubContractorRepository.GetPackageFileInfoAsync(projectSubContractorId)
                    ?? throw new AbrilException("No hay paquete de contrato generado. Por favor genere o seleccione el archivo antes de enviar.", 400);

                fileBytes   = await _oneDriveStorage.DownloadByWebUrlAsync(pkgInfo.FileUrl);
                fileName    = pkgInfo.OriginalFileName;
                contentType = "application/pdf";
            }

            // Subir a OneDrive del proyecto
            await _oneDriveStorage.UploadAsync(
                pathData, AdjudicacionDocumentType.ScPackage, fileName,
                new MemoryStream(fileBytes), contentType);

            // Construir y enviar el correo
            var subject    = $"{data.ProjectDescription} : {fileName}";
            var senderProfile = await _graphUserService.GetCurrentUserProfileAsync(graphAccessToken);
            var signature     = BuildEmailSignature(senderProfile);
            var body          = BuildScEmailBody(fileName, data.WorkItemDescription) + signature;

            var attachment = new MailAttachmentDto
            {
                FileName    = fileName,
                ContentType = contentType,
                Content     = fileBytes
            };

            var costosEmailsSc = await _costosPresupuestosEmailService.GetActiveEmails();
            var ccEmails = data.OficinaTecnicaEmails
                .Concat(costosEmailsSc)
                .Distinct()
                .ToList();

            await _delegatedMailService.SendAsync(
                graphAccessToken: graphAccessToken,
                to:          data.ContractorEmails,
                subject:     subject,
                body:        body,
                isHtml:      true,
                cc:          ccEmails,
                attachments: WithSignatureAttachment(new List<MailAttachmentDto> { attachment }));

            // Avanzar al estado 5
            await _projectSubContractorRepository.UpdateStatus(projectSubContractorId, 5, userId);
        }

        public async Task SetArrivalOptionAsync(int projectSubContractorId, bool arrivedWithObservations, int userId)
        {
            await _projectSubContractorRepository.SetArrivalOptionAsync(projectSubContractorId, arrivedWithObservations, userId);
        }

        public async Task ConfirmStep5Async(int projectSubContractorId, bool arrivedWithObservations, string? arrivalObservation, string graphAccessToken, int userId)
        {
            // Validar antes de cambiar el estado: el correo del paso 6 va a Oficina Técnica.
            var emailCheck = await _projectSubContractorRepository.GetStep5EmailDataAsync(projectSubContractorId);
            if (emailCheck.OficinaTecnicaEmails.Count == 0)
                throw MissingStaffEmailConfig("Oficina Técnica");

            await _projectSubContractorRepository.ConfirmStep5Async(projectSubContractorId, arrivedWithObservations, arrivalObservation, userId);

            var data = await _projectSubContractorRepository.GetStep6NotificationDataAsync(projectSubContractorId);

            var costosEmailsStep5 = await _costosPresupuestosEmailService.GetActiveEmails();
            var toEmails = data.OficinaTecnicaEmails
                .Concat(costosEmailsStep5)
                .Distinct()
                .ToList();

            if (toEmails.Count > 0)
            {
                var senderProfile = await _graphUserService.GetCurrentUserProfileAsync(graphAccessToken);
                var signature     = BuildEmailSignature(senderProfile);
                var subject       = $"PROCESO DE FIRMA / {data.ProjectDescription}";
                var body          = BuildStep6EmailBody(data) + signature;

                await _delegatedMailService.SendAsync(
                    graphAccessToken: graphAccessToken,
                    to:               toEmails,
                    subject:          subject,
                    body:             body,
                    isHtml:           true,
                    attachments:      WithSignatureAttachment());
            }
        }

        public async Task SendStep6NotificationAsync(int projectSubContractorId, int userId)
        {
            await _projectSubContractorRepository.UpdateStatus(projectSubContractorId, 7, userId);
        }

        public async Task UpdateStep6ChecksAsync(int projectSubContractorId, UpdateStep6ChecksDTO dto, int userId)
        {
            await _projectSubContractorRepository.UpdateStep6ChecksAsync(
                projectSubContractorId,
                dto.SignedCostos,
                dto.SignedGerenteInmobiliario,
                dto.SignedGerenteGeneral,
                userId);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Paso 5 — Correos de observaciones (Of. Central → Of. Técnica) y
        //          levantamiento (Of. Técnica → Of. Central)
        // ─────────────────────────────────────────────────────────────────────────

        public async Task SendStep5ObservationsEmailAsync(int projectSubContractorId, SendStep5ObservationsEmailDto dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Observation))
                throw new AbrilException("Debe registrar una observación antes de enviar el correo.", 400);

            // Persistir la observación para que Of. Técnica la vea al abrir el detalle.
            await _projectSubContractorRepository.SaveArrivalObservationAsync(projectSubContractorId, dto.Observation, userId);

            var data = await _projectSubContractorRepository.GetStep5EmailDataAsync(projectSubContractorId);

            if (data.OficinaTecnicaEmails.Count == 0)
                throw MissingStaffEmailConfig("Oficina Técnica");

            var costosEmailsCc = await _costosPresupuestosEmailService.GetActiveEmails();
            var senderProfile  = await _graphUserService.GetCurrentUserProfileAsync(dto.GraphAccessToken);
            var signature      = BuildEmailSignature(senderProfile);
            var subject        = $"OBSERVACIONES EN LLEGADA DE EXPEDIENTE / {data.ProjectDescription} / {data.ContributorName}";
            var body           = BuildStep5ObservationsEmailBody(data, dto.Observation) + signature;

            await _delegatedMailService.SendAsync(
                graphAccessToken: dto.GraphAccessToken,
                to:               data.OficinaTecnicaEmails,
                subject:          subject,
                body:             body,
                isHtml:           true,
                cc:               costosEmailsCc,
                attachments:      WithSignatureAttachment());
        }

        public async Task SendStep5LevantamientoEmailAsync(int projectSubContractorId, SendStep5LevantamientoEmailDto dto, int userId)
        {
            var data = await _projectSubContractorRepository.GetStep5EmailDataAsync(projectSubContractorId);

            // El levantamiento de observaciones ya no va a Oficina Central: se envía
            // directamente al equipo de Costos y Presupuestos como destinatario principal.
            var costosEmails = await _costosPresupuestosEmailService.GetActiveEmails();
            if (costosEmails.Count == 0)
                throw new AbrilException(
                    "No hay correos de Costos y Presupuestos configurados para enviar el levantamiento de observaciones. " +
                    "Regístrelos en Configuración → Correos de Costos y Presupuestos y vuelva a intentarlo.", 422);

            var senderProfile  = await _graphUserService.GetCurrentUserProfileAsync(dto.GraphAccessToken);
            var signature      = BuildEmailSignature(senderProfile);
            var subject        = $"LEVANTAMIENTO DE OBSERVACIONES / {data.ProjectDescription} / {data.ContributorName}";
            var body           = BuildStep5LevantamientoEmailBody(data, dto.Message) + signature;

            await _delegatedMailService.SendAsync(
                graphAccessToken: dto.GraphAccessToken,
                to:               costosEmails,
                subject:          subject,
                body:             body,
                isHtml:           true,
                attachments:      WithSignatureAttachment());
        }

        private static string BuildStep5ObservationsEmailBody(Step5EmailDataDto data, string observation)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"font-family:Arial,sans-serif; font-size:13px; color:#333;\">");
            sb.AppendLine("<p>Estimado equipo de Oficina Técnica,</p>");
            sb.AppendLine("<p>Se les informa que el contrato llegó a Oficina Central con observaciones:</p>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"  <li><strong>Proyecto:</strong> {data.ProjectDescription}</li>");
            sb.AppendLine($"  <li><strong>Contratista:</strong> {data.ContributorName}</li>");
            sb.AppendLine($"  <li><strong>Partida:</strong> {data.WorkItemDescription}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p><strong>Observaciones:</strong></p>");
            sb.AppendLine($"<p style=\"background:#FFF7ED; border:1px solid #FDBA74; border-radius:6px; padding:10px; white-space:pre-line;\">{System.Net.WebUtility.HtmlEncode(observation)}</p>");
            sb.AppendLine("<p>Por favor, gestionen el levantamiento de las observaciones indicadas.</p>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string BuildStep5LevantamientoEmailBody(Step5EmailDataDto data, string? message)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"font-family:Arial,sans-serif; font-size:13px; color:#333;\">");
            sb.AppendLine("<p>Estimado equipo de Costos y Presupuestos,</p>");
            sb.AppendLine("<p>Se les informa que las observaciones registradas en la llegada del expediente del siguiente contrato han sido levantadas:</p>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"  <li><strong>Proyecto:</strong> {data.ProjectDescription}</li>");
            sb.AppendLine($"  <li><strong>Contratista:</strong> {data.ContributorName}</li>");
            sb.AppendLine($"  <li><strong>Partida:</strong> {data.WorkItemDescription}</li>");
            sb.AppendLine("</ul>");
            if (!string.IsNullOrWhiteSpace(data.ArrivalObservation))
            {
                sb.AppendLine("<p><strong>Observaciones originales:</strong></p>");
                sb.AppendLine($"<p style=\"background:#FFF7ED; border:1px solid #FDBA74; border-radius:6px; padding:10px; white-space:pre-line;\">{System.Net.WebUtility.HtmlEncode(data.ArrivalObservation)}</p>");
            }
            if (!string.IsNullOrWhiteSpace(message))
            {
                sb.AppendLine("<p><strong>Mensaje de Oficina Técnica:</strong></p>");
                sb.AppendLine($"<p style=\"background:#F0FAE5; border:1px solid #A3D977; border-radius:6px; padding:10px; white-space:pre-line;\">{System.Net.WebUtility.HtmlEncode(message)}</p>");
            }
            sb.AppendLine("<p>Por favor, verifiquen y continúen con el proceso de la adjudicación.</p>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string BuildStep6EmailBody(Step6NotificationDataDto data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<p>Estimados,</p>");
            sb.AppendLine("<p>Se les informa que los documentos del siguiente contrato se encuentran en proceso de firma:</p>");
            sb.AppendLine("<table style=\"border-collapse:collapse; font-family:Arial,sans-serif; font-size:13px;\">");
            sb.AppendLine("  <tr><td style=\"padding:4px 12px 4px 0; color:#888;\">Proyecto</td>"        + $"<td style=\"padding:4px 0;\"><strong>{data.ProjectDescription}</strong></td></tr>");
            sb.AppendLine("  <tr><td style=\"padding:4px 12px 4px 0; color:#888;\">Subcontratista</td>" + $"<td style=\"padding:4px 0;\">{data.ContributorName}</td></tr>");
            sb.AppendLine("  <tr><td style=\"padding:4px 12px 4px 0; color:#888;\">Partida</td>"        + $"<td style=\"padding:4px 0;\">{data.WorkItemDescription}</td></tr>");
            if (data.ContractNumber.HasValue)
                sb.AppendLine("  <tr><td style=\"padding:4px 12px 4px 0; color:#888;\">N° de contrato</td>" + $"<td style=\"padding:4px 0;\">{data.ContractNumber}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<br/><p>Saludos.</p>");
            return sb.ToString();
        }

        public async Task SendStep8NotificationAsync(int projectSubContractorId, string graphAccessToken, int userId)
        {
            var data = await _projectSubContractorRepository.GetStep8NotificationDataAsync(projectSubContractorId);

            if (data.ScannedDocs.Count == 0)
                throw new AbrilException("No hay documentos escaneados adjuntos para enviar.");

            var toEmails = data.OficinaTecnicaEmails
                .Distinct()
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            if (toEmails.Count == 0)
                throw MissingStaffEmailConfig("Oficina Técnica");

            var attachments    = await DownloadAttachmentsAsync(data.ScannedDocs);
            var senderProfile  = await _graphUserService.GetCurrentUserProfileAsync(graphAccessToken);
            var signature      = BuildEmailSignature(senderProfile);
            var subject        = $"CONTRATOS FIRMADOS / {data.ProjectDescription}";
            var body           = BuildStep8EmailBody(data.ContributorName) + signature;
            var costosEmailsCc = await _costosPresupuestosEmailService.GetActiveEmails();

            await _delegatedMailService.SendAsync(
                graphAccessToken: graphAccessToken,
                to:               toEmails,
                subject:          subject,
                body:             body,
                isHtml:           true,
                cc:               costosEmailsCc,
                attachments:      WithSignatureAttachment(attachments));

            await _projectSubContractorRepository.UpdateStatus(projectSubContractorId, 9, userId);
        }

        private static string BuildStep8EmailBody(string contributorName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<p>Estimados buenas tardes,</p>");
            sb.AppendLine("<p>Adjunto contratos escaneados y firmados. Se encuentran en recepción para su recojo.</p>");
            sb.AppendLine("<p><strong>CONTRATOS:</strong></p>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"  <li>{contributorName}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Saludos.</p>");
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Paso 3 — Correo de observaciones a Costos y Staff de Obra
        // ─────────────────────────────────────────────────────────────────────────

        public async Task SendObservationEmailAsync(
            int projectSubContractorId,
            AdjudicacionDocumentType documentType,
            SendObservationEmailDto dto,
            int userId)
        {
            var data = await _projectSubContractorRepository.GetStep3ApprovalDataAsync(projectSubContractorId);

            // Destinatarios: Costos y Presupuestos + Oficina Técnica del proyecto
            var costosEmailsObs = await _costosPresupuestosEmailService.GetActiveEmails();
            var toEmails = costosEmailsObs
                .Concat(data.OficinaTecnicaEmails)
                .Distinct()
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            if (data.OficinaTecnicaEmails.Count == 0)
                throw MissingStaffEmailConfig("Oficina Técnica");

            var senderProfile = await _graphUserService.GetCurrentUserProfileAsync(dto.GraphAccessToken);
            var signature     = BuildEmailSignature(senderProfile);
            var subject       = $"OBSERVACIÓN EN DOCUMENTOS / {data.ProjectDescription} / {data.ContributorName}";
            var body          = BuildObservationEmailBody(data, dto.DocumentLabel, dto.Observation) + signature;

            await _delegatedMailService.SendAsync(
                graphAccessToken: dto.GraphAccessToken,
                to:               toEmails,
                subject:          subject,
                body:             body,
                isHtml:           true,
                attachments:      WithSignatureAttachment());
        }

        public async Task SendAllObservationsEmailAsync(
            int projectSubContractorId,
            SendAllObservationsEmailDto dto,
            int userId)
        {
            var data = await _projectSubContractorRepository.GetStep3ApprovalDataAsync(projectSubContractorId);

            var observations = await _projectSubContractorRepository
                .GetStep3DocumentObservationsAsync(projectSubContractorId);

            if (observations.Count == 0)
                throw new AbrilException(
                    "No hay documentos con observaciones registradas en este momento. " +
                    "Marque al menos un documento como 'Con observaciones' antes de enviar el correo.", 400);

            // Destinatarios: Costos y Presupuestos + Oficina Técnica del proyecto
            var costosEmailsObs = await _costosPresupuestosEmailService.GetActiveEmails();
            var toEmails = costosEmailsObs
                .Concat(data.OficinaTecnicaEmails)
                .Distinct()
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            if (data.OficinaTecnicaEmails.Count == 0)
                throw MissingStaffEmailConfig("Oficina Técnica");

            var senderProfile = await _graphUserService.GetCurrentUserProfileAsync(dto.GraphAccessToken);
            var signature     = BuildEmailSignature(senderProfile);
            var subject       = $"OBSERVACIÓN EN DOCUMENTOS / {data.ProjectDescription} / {data.ContributorName}";
            var body          = BuildAllObservationsEmailBody(data, observations) + signature;

            await _delegatedMailService.SendAsync(
                graphAccessToken: dto.GraphAccessToken,
                to:               toEmails,
                subject:          subject,
                body:             body,
                isHtml:           true,
                attachments:      WithSignatureAttachment());
        }

        private static string BuildAllObservationsEmailBody(
            Step3ApprovalDataDto data,
            List<DocumentObservationDto> observations)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"font-family:Arial,sans-serif; font-size:13px; color:#333;\">");
            sb.AppendLine("<p>Estimados,</p>");

            var docList = string.Join(", ", observations.Select(o => $"<strong>{o.DocumentLabel}</strong>"));
            sb.AppendLine(
                $"<p>Se comunica que los siguientes documentos correspondientes a la adjudicación presentan " +
                $"observaciones que requieren atención: {docList}.</p>");

            sb.AppendLine("<table style=\"border-collapse:collapse; font-size:13px; margin-bottom:16px;\">");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Proyecto</td>"
                        + $"<td style=\"padding:4px 0;\"><strong>{data.ProjectDescription}</strong></td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Subcontratista</td>"
                        + $"<td style=\"padding:4px 0;\">{data.ContributorName}</td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Partida</td>"
                        + $"<td style=\"padding:4px 0;\">{data.WorkItemDescription}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<p><strong>Detalle de observaciones:</strong></p>");

            foreach (var obs in observations)
            {
                sb.AppendLine(
                    "<div style=\"margin-bottom:14px; border-left:4px solid #f9a825; padding:10px 14px; background:#fff8e1;\">");
                sb.AppendLine(
                    $"<p style=\"margin:0 0 6px; font-size:13px; font-weight:bold; color:#333;\">{obs.DocumentLabel}</p>");

                if (!string.IsNullOrWhiteSpace(obs.Observation))
                    sb.AppendLine(
                        $"<p style=\"margin:0; font-size:13px; color:#444; line-height:1.5;\">" +
                        $"{System.Net.WebUtility.HtmlEncode(obs.Observation)}</p>");
                else
                    sb.AppendLine(
                        "<p style=\"margin:0; font-size:13px; color:#999; font-style:italic;\">Sin detalle de observación.</p>");

                sb.AppendLine("</div>");
            }

            sb.AppendLine("<p>Por favor, tome las acciones necesarias para la corrección de los documentos indicados.</p>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        /// <summary>
        /// Paso 3 — Oficina Técnica solicita a Costos el visto bueno / comentarios del
        /// contrato preparado. El correo va dirigido únicamente a los miembros de Costos
        /// y Presupuestos e incluye el enlace de ingreso a la plataforma (no adjunta archivos).
        /// </summary>
        public async Task SendContractReviewEmailAsync(
            int projectSubContractorId,
            SendAllObservationsEmailDto dto,
            int userId)
        {
            var data = await _projectSubContractorRepository.GetStep3ApprovalDataAsync(projectSubContractorId);

            // Destinatarios: únicamente los miembros de Costos y Presupuestos.
            var costosEmails = await _costosPresupuestosEmailService.GetActiveEmails();
            var toEmails = costosEmails
                .Distinct()
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            if (toEmails.Count == 0)
                throw new AbrilException(
                    "No hay correos de Costos y Presupuestos configurados para enviar la revisión.", 422);

            var senderProfile = await _graphUserService.GetCurrentUserProfileAsync(dto.GraphAccessToken);
            var signature     = BuildEmailSignature(senderProfile);
            var subject       = $"{data.ProjectDescription} // CONTRATO PARA REVISIÓN // {data.ContributorName}";
            var platformUrl   = $"{_frontendUrl}/costs/adjudicaciones";
            var body          = BuildContractReviewEmailBody(data, platformUrl) + signature;

            await _delegatedMailService.SendAsync(
                graphAccessToken: dto.GraphAccessToken,
                to:               toEmails,
                subject:          subject,
                body:             body,
                isHtml:           true,
                attachments:      WithSignatureAttachment());
        }

        private static string BuildContractReviewEmailBody(Step3ApprovalDataDto data, string platformUrl)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"font-family:Arial,sans-serif; font-size:13px; color:#333;\">");
            sb.AppendLine("<p>Buen día,</p>");
            sb.AppendLine(
                $"<p>Por favor su apoyo con el visto bueno y/o comentarios de los documentos correspondiente a la " +
                $"adjudicación del subcontratista <strong>{data.ContributorName}</strong>:</p>");

            sb.AppendLine("<table style=\"border-collapse:collapse; font-size:13px; margin-bottom:16px;\">");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Proyecto</td>"
                        + $"<td style=\"padding:4px 0;\"><strong>{data.ProjectDescription}</strong></td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Subcontratista</td>"
                        + $"<td style=\"padding:4px 0;\">{data.ContributorName}</td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Partida</td>"
                        + $"<td style=\"padding:4px 0;\">{data.WorkItemDescription}</td></tr>");
            sb.AppendLine("</table>");

            if (!string.IsNullOrWhiteSpace(platformUrl))
                sb.AppendLine(
                    $"<p>Puede revisar los documentos ingresando a la plataforma: " +
                    $"<a href=\"{platformUrl}\">{platformUrl}</a></p>");

            sb.AppendLine("<p>Gracias.</p>");
            sb.AppendLine("<p>Saludos.</p>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string BuildObservationEmailBody(
            Step3ApprovalDataDto data,
            string documentLabel,
            string? observation)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"font-family:Arial,sans-serif; font-size:13px; color:#333;\">");
            sb.AppendLine("<p>Estimados,</p>");
            sb.AppendLine(
                $"<p>Se comunica que el documento <strong>{documentLabel}</strong> correspondiente a la siguiente " +
                "adjudicación presenta observaciones que requieren atención:</p>");

            sb.AppendLine("<table style=\"border-collapse:collapse; font-size:13px; margin-bottom:16px;\">");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Proyecto</td>"
                        + $"<td style=\"padding:4px 0;\"><strong>{data.ProjectDescription}</strong></td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Subcontratista</td>"
                        + $"<td style=\"padding:4px 0;\">{data.ContributorName}</td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Partida</td>"
                        + $"<td style=\"padding:4px 0;\">{data.WorkItemDescription}</td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Documento</td>"
                        + $"<td style=\"padding:4px 0;\">{documentLabel}</td></tr>");
            sb.AppendLine("</table>");

            if (!string.IsNullOrWhiteSpace(observation))
            {
                sb.AppendLine("<p><strong>Detalle de la observación:</strong></p>");
                sb.AppendLine(
                    "<p style=\"" +
                    "background:#fff8e1; border-left:4px solid #f9a825; " +
                    "padding:10px 14px; margin:0 0 16px; " +
                    "font-family:Arial,sans-serif; font-size:13px; color:#444; line-height:1.5;\">" +
                    $"{System.Net.WebUtility.HtmlEncode(observation)}" +
                    "</p>");
            }

            sb.AppendLine("<p>Por favor, tome las acciones necesarias para la corrección del documento indicado.</p>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string BuildScEmailBody(string fileName, string workItemDescription)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<p>Buenos días estimados,</p>");
            sb.AppendLine($"<p>Adjunto contrato <strong>{fileName}</strong> por <strong>{workItemDescription}</strong>.</p>");
            sb.AppendLine("<p>Se solicita la impresión y firma de <strong>TODOS LOS DOCUMENTOS ADJUNTOS</strong> en 03 juegos. " +
                          "Asimismo, estos documentos deberán ser entregados en el orden de la numeración de cada documento. " +
                          "<strong>Entregarlos en oficina central: Cal. Mama Ocllo N°2647</strong> Urb. Risso Lima- Lima-Lince (L-V de 8:00am-5:30 pm).</p>");
            sb.AppendLine("<p>Tener en cuenta;</p>");
            sb.AppendLine("<ul>");
            sb.AppendLine("  <li>Las hojas del contrato <strong>no deberán estar engrampadas.</strong></li>");
            sb.AppendLine("  <li>No imprimir a doble cara</li>");
            sb.AppendLine("  <li>Firmar con lapicero azul todas las hojas adjuntas, no solo basta con un VB</li>");
            sb.AppendLine("  <li><span style=\"background-color: yellow;\">Llevarlo cuanto antes para que no restrinjan el pago de las valorizaciones</span></li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Quedo atenta.</p>");
            sb.AppendLine("<p>Saludos.</p>");
            return sb.ToString();
        }

        /// <summary>
        /// Construye la oración de valorización (numeral 5.1.x) a partir de las "Formas de
        /// valorización" de la partida. Ej.: "Las valorizaciones serán de acuerdo con los
        /// siguientes porcentajes de valorización: 60% por instalación, 30% por acabado y
        /// 10% por levantamiento de observaciones." Devuelve null si la partida no tiene
        /// formas registradas (en ese caso el contrato usa el texto por defecto).
        /// </summary>
        private static string? BuildValorizationFormsSentence(List<(decimal Percentage, string Concept)> forms)
        {
            if (forms is null || forms.Count == 0) return null;

            var partes = forms
                .Select(f => $"{f.Percentage.ToString("0.##", CultureInfo.InvariantCulture)}% {f.Concept.Trim()}")
                .ToList();

            var listado = partes.Count == 1
                ? partes[0]
                : string.Join(", ", partes.Take(partes.Count - 1)) + " y " + partes[^1];

            return $"Las valorizaciones serán de acuerdo con los siguientes porcentajes de valorización: {listado}.";
        }

        private static string BuildInternalEmailBody(AdjudicacionNotificationDataDto data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<p>Estimados,</p>");
            sb.AppendLine($"<p>Se adjuntan los archivos de cotización y cuadro comparativo correspondientes a la adjudicación de " +
                          $"<strong>{data.WorkItemDescription}</strong> para el proyecto <strong>{data.ProjectDescription}</strong> " +
                          $"con la empresa <strong>{data.ContributorName}</strong>.</p>");
            return sb.ToString();
        }

        // ── Firma de correo ──────────────────────────────────────────────────

        private static MailAttachmentDto? GetSignatureAttachment()
        {
            var bytes = _signatureGifBytes.Value;
            if (bytes is null) return null;
            return new MailAttachmentDto
            {
                FileName    = "abril-correo.gif",
                ContentType = "image/gif",
                Content     = bytes,
                IsInline    = true,
                ContentId   = SignatureGifContentId,
            };
        }

        /// <summary>
        /// Devuelve una nueva lista que incluye todos los adjuntos existentes
        /// más el GIF de firma (si está disponible).
        /// </summary>
        private static List<MailAttachmentDto> WithSignatureAttachment(List<MailAttachmentDto>? existing = null)
        {
            var list = existing != null
                ? new List<MailAttachmentDto>(existing)
                : new List<MailAttachmentDto>();
            var sig = GetSignatureAttachment();
            if (sig is not null) list.Add(sig);
            return list;
        }

        /// <summary>
        /// Genera el bloque HTML de firma con el logo inline y los datos del remitente.
        /// Omite cada campo si no está disponible en el perfil.
        /// </summary>
        private static string BuildEmailSignature(GraphUserProfileDto? profile)
        {
            var sb = new StringBuilder();
            sb.Append("<table style=\"margin-top:24px; border-top:1px solid #e0e0e0; padding-top:12px; ");
            sb.Append("font-family:Arial,sans-serif; font-size:12px; color:#333; border-collapse:collapse;\">");
            sb.Append("<tr>");
            sb.Append("<td style=\"padding-right:16px; vertical-align:top;\">");
            sb.Append($"<img src=\"cid:{SignatureGifContentId}\" alt=\"Abril\" style=\"width:130px;\" />");
            sb.Append("</td>");
            sb.Append("<td style=\"vertical-align:top; border-left:2px solid #e0e0e0; padding-left:16px; line-height:1.7;\">");

            if (!string.IsNullOrWhiteSpace(profile?.DisplayName))
                sb.Append($"<div><strong>{profile.DisplayName}</strong></div>");
            if (!string.IsNullOrWhiteSpace(profile?.JobTitle))
                sb.Append($"<div style=\"color:#64BC04;\">{profile.JobTitle}</div>");
            if (!string.IsNullOrWhiteSpace(profile?.Phone))
                sb.Append($"<div>{profile.Phone}</div>");
            if (!string.IsNullOrWhiteSpace(profile?.Mail))
                sb.Append($"<div><a href=\"mailto:{profile.Mail}\" style=\"color:#333; text-decoration:none;\">{profile.Mail}</a></div>");

            sb.Append("<div><a href=\"https://abril.pe\" style=\"color:#333; text-decoration:none;\">abril.pe</a></div>");
            sb.Append("</td>");
            sb.Append("</tr>");
            sb.Append("</table>");
            return sb.ToString();
        }

        private static string BuildSubcontractorEmailBody(
            AdjudicacionNotificationDataDto data,
            List<GraphUserProfileDto> userProfiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<p>Estimado,</p>");
            sb.AppendLine($"<p>Se procede a la adjudicación de <strong>{data.WorkItemDescription}</strong> " +
                          $"para el PROYECTO <strong>{data.ProjectDescription}</strong>. " +
                          "En un siguiente correo, el área de oficina técnica enviará el contrato y la OS. " +
                          "Se remite la matriz de comunicaciones del proyecto a fin de coordinar:</p>");

            sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;\">");
            sb.AppendLine($"  <caption><strong>Edificio Multifamiliar {data.ProjectDescription}</strong></caption>");
            sb.AppendLine("  <thead><tr>");
            sb.AppendLine("    <th>N</th><th>Nombre</th><th>Puesto</th><th>Número</th><th>Correo</th>");
            sb.AppendLine("  </tr></thead>");
            sb.AppendLine("  <tbody>");

            for (int i = 0; i < userProfiles.Count; i++)
            {
                var profile = userProfiles[i];
                sb.AppendLine("    <tr>");
                sb.AppendLine($"      <td>{i + 1}</td>");
                sb.AppendLine($"      <td>{profile.DisplayName ?? "-"}</td>");
                sb.AppendLine($"      <td>{profile.JobTitle ?? "-"}</td>");
                sb.AppendLine($"      <td>{profile.Phone ?? "-"}</td>");
                sb.AppendLine($"      <td>{profile.Mail}</td>");
                sb.AppendLine("    </tr>");
            }

            sb.AppendLine("  </tbody></table>");
            return sb.ToString();
        }

        public async Task SaveDates(int projectSubContractorId, UpdateDatesDTO dto, int userId)
        {
            await _projectSubContractorRepository.SaveDates(projectSubContractorId, dto, userId);
        }

        public async Task UpdateInfo(int projectSubContractorId, ProjectSubContractorUpdateInfoDTO dto, int userId)
        {
            ValidateStep1Data(dto.ContractModalityId, dto.WorkSpecialtyId, dto.WorkItemCategoryId, dto.WorkItemId);

            var hasNewFiles = (dto.NewQuotationFiles?.Count ?? 0) > 0
                           || (dto.NewComparativeFiles?.Count ?? 0) > 0;

            // Mismo pre-flight que en el alta, pero contra el proyecto y la especialidad que van a
            // quedar guardados (la edición puede estar cambiándolos): si falta la carpeta 04_OBRAS
            // se corta antes de escribir nada.
            if (hasNewFiles)
            {
                var preflight = await _projectSubContractorRepository.GetCreatePathDataAsync(
                    dto.ProjectId, dto.WorkSpecialtyId);
                _oneDriveStorage.ValidateUploadPath(preflight);
            }

            await _projectSubContractorRepository.UpdateInfo(projectSubContractorId, dto, userId);

            if ((dto.RemovedQuotationFileIds?.Count ?? 0) > 0 || (dto.RemovedComparativeFileIds?.Count ?? 0) > 0)
            {
                await _projectSubContractorRepository.RemoveInitialFilesAsync(
                    projectSubContractorId, dto.RemovedQuotationFileIds, dto.RemovedComparativeFileIds, userId);
            }

            if (!hasNewFiles) return;

            // La ruta se resuelve DESPUÉS del update para que los archivos nuevos caigan en la carpeta
            // de la especialidad/partida ya actualizada, no en la que tenía antes de editar.
            var pathData = await _projectSubContractorRepository.GetPathDataAsync(projectSubContractorId);

            var quotationFiles   = await UploadFilesToSharePoint(dto.NewQuotationFiles,   pathData, AdjudicacionDocumentType.InitialQuotation);
            var comparativeFiles = await UploadFilesToSharePoint(dto.NewComparativeFiles, pathData, AdjudicacionDocumentType.InitialComparative);

            await _projectSubContractorRepository.SaveInitialFilesAsync(
                projectSubContractorId, quotationFiles, comparativeFiles, userId);
        }

        public async Task UpdateDocumentStatusAsync(
            int projectSubContractorId,
            AdjudicacionDocumentType documentType,
            int? statusId,
            string? observation,
            int userId)
        {
            await _projectSubContractorRepository.UpdateDocumentStatusAsync(
                projectSubContractorId, documentType, statusId, observation, userId);
        }

        public async Task SendAllLevantamientoEmailAsync(
            int projectSubContractorId,
            SendAllObservationsEmailDto dto,
            int userId)
        {
            var data     = await _projectSubContractorRepository.GetStep3ApprovalDataAsync(projectSubContractorId);
            var levDocs  = await _projectSubContractorRepository.GetLevantamientoDocumentsAsync(projectSubContractorId);

            if (levDocs.Count == 0)
                throw new AbrilException(
                    "No hay documentos en estado 'Levantamiento de observación'. Marque al menos un documento antes de enviar el correo.", 400);

            var senderProfile = await _graphUserService.GetCurrentUserProfileAsync(dto.GraphAccessToken);
            var signature     = BuildEmailSignature(senderProfile);
            var subject       = $"LEVANTAMIENTO DE OBSERVACIÓN / {data.ProjectDescription} / {data.ContributorName}";
            var body          = BuildAllLevantamientoEmailBody(data, levDocs) + signature;

            var costosEmailsLev = await _costosPresupuestosEmailService.GetActiveEmails();
            await _delegatedMailService.SendAsync(
                graphAccessToken: dto.GraphAccessToken,
                to:               costosEmailsLev,
                subject:          subject,
                body:             body,
                isHtml:           true,
                attachments:      WithSignatureAttachment());
        }

        private static string BuildLiftObservationEmailBody(Step3ApprovalDataDto data, string documentLabel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"font-family:Arial,sans-serif; font-size:13px; color:#333;\">");
            sb.AppendLine("<p>Estimados,</p>");
            sb.AppendLine(
                $"<p>Se comunica que el documento <strong>{documentLabel}</strong> de la siguiente " +
                "adjudicación ha sido subsanado por Staff de Obra:</p>");
            sb.AppendLine("<table style=\"border-collapse:collapse; font-size:13px; margin-bottom:16px;\">");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Proyecto</td>"
                        + $"<td style=\"padding:4px 0;\"><strong>{data.ProjectDescription}</strong></td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Subcontratista</td>"
                        + $"<td style=\"padding:4px 0;\">{data.ContributorName}</td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Partida</td>"
                        + $"<td style=\"padding:4px 0;\">{data.WorkItemDescription}</td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Documento</td>"
                        + $"<td style=\"padding:4px 0;\">{documentLabel}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<p>Por favor, proceda con la revisión correspondiente.</p>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string BuildAllLevantamientoEmailBody(
            Step3ApprovalDataDto data,
            List<DocumentObservationDto> docs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"font-family:Arial,sans-serif; font-size:13px; color:#333;\">");
            sb.AppendLine("<p>Estimados,</p>");
            sb.AppendLine(
                "<p>Se comunica que Staff de Obra ha levantado la observación de los siguientes documentos:</p>");
            sb.AppendLine("<table style=\"border-collapse:collapse; font-size:13px; margin-bottom:16px;\">");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Proyecto</td>"
                        + $"<td style=\"padding:4px 0;\"><strong>{data.ProjectDescription}</strong></td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Subcontratista</td>"
                        + $"<td style=\"padding:4px 0;\">{data.ContributorName}</td></tr>");
            sb.AppendLine($"  <tr><td style=\"padding:4px 16px 4px 0; color:#666; white-space:nowrap;\">Partida</td>"
                        + $"<td style=\"padding:4px 0;\">{data.WorkItemDescription}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<p style=\"margin-bottom:6px;\"><strong>Documentos levantados:</strong></p>");
            sb.AppendLine("<ul style=\"margin:0; padding-left:20px;\">");
            foreach (var doc in docs)
                sb.AppendLine($"  <li style=\"margin-bottom:4px;\">{doc.DocumentLabel}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p style=\"margin-top:14px;\">Por favor, proceda con la revisión correspondiente.</p>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        /// <summary>
        /// Formatea el periodo de validez de garantía (en días) a texto legible:
        ///  · 0 o null            → "0 días"
        ///  · ≤ 364               → "{n} días" (ej. 364 → "364 días")
        ///  · múltiplo de 365     → "1 año" / "2 años" …
        ///  · resto               → "1 año 5 días" (años + días restantes)
        /// Maneja singular/plural ("1 día" / "1 año").
        /// </summary>
        private static string FormatGuaranteeValidity(int? days)
        {
            var d = days ?? 0;
            if (d <= 0) return "0 días";

            int years   = d / 365;
            int remDays = d % 365;

            if (years == 0)
                return $"{remDays} {(remDays == 1 ? "día" : "días")}";

            var yearPart = $"{years} {(years == 1 ? "año" : "años")}";
            if (remDays == 0) return yearPart;

            return $"{yearPart} {remDays} {(remDays == 1 ? "día" : "días")}";
        }

        private static string GetDocumentLabel(AdjudicacionDocumentType documentType) => documentType switch
        {
            AdjudicacionDocumentType.Contract           => "Contrato",
            AdjudicacionDocumentType.SummarySheet       => "Hoja Resumen",
            AdjudicacionDocumentType.Budget             => "Presupuesto",
            AdjudicacionDocumentType.Schedule           => "Cronograma",
            AdjudicacionDocumentType.AttachedQuotation  => "Cotización Adjunta",
            AdjudicacionDocumentType.ServiceOrder       => "Orden de Servicio",
            AdjudicacionDocumentType.PromissoryNote     => "Pagaré",
            AdjudicacionDocumentType.Instructivo        => "Instructivo",
            AdjudicacionDocumentType.NonConformingOutput => "Causales de Conformidad",
            AdjudicacionDocumentType.ToleranceChart     => "Cuadro de Tolerancias",
            AdjudicacionDocumentType.FinishProtection   => "Protección de Acabados",
            AdjudicacionDocumentType.FichaTecnica       => "Ficha Técnica",
            AdjudicacionDocumentType.Anexo              => "Anexos",
            _                                           => documentType.ToString(),
        };

        public async Task<DocumentUploadResponseDto> UploadDocumentAsync(
            int projectSubContractorId,
            AdjudicacionDocumentType documentType,
            IFormFile file,
            int userId)
        {
            if (file is null || file.Length == 0)
                throw new AbrilException("El archivo no puede estar vacío.");

            // Causales de No Conformidad, Cuadro de Tolerancias y Protección de Acabados no se suben:
            // usan un PDF de plantilla fijo. Solo se controla su estado (Sí aplica / No aplica).
            if (documentType is AdjudicacionDocumentType.NonConformingOutput
                             or AdjudicacionDocumentType.ToleranceChart
                             or AdjudicacionDocumentType.FinishProtection)
                throw new AbrilException(
                    "Este documento no se sube: se usa el documento estándar de Abril. Solo configure su estado (Sí aplica / No aplica).",
                    400);

            // La cotización adjunta debe ser PDF — se inserta dentro del contrato (paso 4)
            // y para eso se mergea a nivel PDF.
            if (documentType == AdjudicacionDocumentType.AttachedQuotation)
            {
                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (ext != ".pdf")
                    throw new AbrilException("La cotización adjunta solo acepta archivos PDF (.pdf).", 400);
            }

            var pathData   = await _projectSubContractorRepository.GetPathDataAsync(projectSubContractorId);
            var fileName   = file.FileName;

            string fileUrl;
            string? sharepointItemId;
            using (var stream = file.OpenReadStream())
            {
                var spResult = await _oneDriveStorage.UploadAsync(
                    pathData, documentType, fileName, stream, file.ContentType);

                fileUrl          = spResult.WebUrl!;
                sharepointItemId = spResult.ItemId;
            }

            await _projectSubContractorRepository.SaveDocumentAsync(
                projectSubContractorId, documentType, fileUrl, fileName, userId, sharepointItemId);

            return new DocumentUploadResponseDto
            {
                FileUrl          = fileUrl,
                OriginalFileName = fileName,
            };
        }

        // ── Generación de documentos ─────────────────────────────────────────

        public async Task<DocumentUploadResponseDto> GenerateDocumentAsync(
            int projectSubContractorId,
            AdjudicacionDocumentType documentType,
            int userId)
        {
            return documentType switch
            {
                AdjudicacionDocumentType.SummarySheet =>
                    await GenerateSummarySheetAsync(projectSubContractorId, userId),
                AdjudicacionDocumentType.Contract =>
                    await GenerateContractAsync(projectSubContractorId, userId),
                AdjudicacionDocumentType.PromissoryNote =>
                    await GeneratePromissoryNoteAsync(projectSubContractorId, userId),
                AdjudicacionDocumentType.Instructivo =>
                    await GenerateInstructivoAsync(projectSubContractorId, userId),
                _ => throw new AbrilException(
                    $"La generación del documento '{documentType}' aún no está implementada.")
            };
        }

        /// <summary>
        /// Valida que estén presentes todos los datos que la plantilla del documento necesita.
        /// Si falta alguno, lanza <see cref="AbrilException"/> con la lista de campos pendientes
        /// para evitar generar archivos con campos en blanco.
        /// </summary>
        private static void ValidateGenerationData(
            AdjudicacionSummarySheetDataDto data, AdjudicacionDocumentType documentType)
        {
            var missing = new List<string>();

            void ReqText(string? value, string label)
            {
                if (string.IsNullOrWhiteSpace(value)) missing.Add(label);
            }

            switch (documentType)
            {
                case AdjudicacionDocumentType.Contract:
                    ReqText(data.ContributorName,                  "Razón social del contratista");
                    ReqText(data.ContributorRuc,                   "RUC del contratista");
                    ReqText(data.ContributorAddress,               "Dirección del contratista");
                    ReqText(data.ContributorDistrict,              "Distrito del contratista");
                    ReqText(data.ContributorProvince,              "Provincia del contratista");
                    ReqText(data.ContributorDepartment,            "Departamento del contratista");
                    ReqText(data.LegalRepresentativeFullName,      "Representante legal del contratista");
                    ReqText(data.LegalRepresentativeDni,           "DNI del representante legal del contratista");
                    ReqText(data.LegalEntityRegistryNumber,        "Partida registral del contratista");
                    ReqText(data.ProjectRazonSocial,               "Razón social del proyecto");
                    ReqText(data.ProjectContributorRuc,            "RUC del proyecto");
                    // Ubicación de la obra: los 4 campos vienen del proyecto (Configuración → Proyectos),
                    // no del contribuyente. Son obligatorios porque el contrato los imprime todos.
                    ReqText(data.ProjectDistrict,                  "Distrito del proyecto");
                    ReqText(data.ProjectProvince,                  "Provincia del proyecto");
                    ReqText(data.ProjectDepartment,                "Departamento del proyecto");
                    ReqText(data.ProjectLocation,                  "Ubicación de la obra del proyecto");
                    ReqText(data.LevelDescription,                 "Desc. de pisos del proyecto");
                    ReqText(data.ProjectLegalEntityRegistryNumber, "Partida registral del proyecto");
                    if (!data.StartDate.HasValue)      missing.Add("Fecha de inicio del contrato");
                    if (!data.EndDate.HasValue)        missing.Add("Fecha de fin del contrato");
                    if (!data.ContractNumber.HasValue) missing.Add("Número de contrato");
                    // La modalidad ya es obligatoria al crear/editar, pero las adjudicaciones antiguas
                    // pueden no tenerla: sin ella no hay plantilla que elegir.
                    if (!data.ContractModalityId.HasValue) missing.Add("Modalidad de contrato");
                    break;

                case AdjudicacionDocumentType.PromissoryNote:
                    ReqText(data.ContributorName,             "Razón social del contratista");
                    ReqText(data.ContributorRuc,              "RUC del contratista");
                    ReqText(data.ContributorAddress,          "Dirección del contratista");
                    ReqText(data.ContributorDistrict,         "Distrito del contratista");
                    ReqText(data.ContributorProvince,         "Provincia del contratista");
                    ReqText(data.ContributorDepartment,       "Departamento del contratista");
                    ReqText(data.LegalRepresentativeFullName, "Representante legal del contratista");
                    ReqText(data.ProjectRazonSocial,          "Razón social del proyecto");
                    ReqText(data.ProjectContributorRuc,       "RUC del proyecto");
                    ReqText(data.ProjectDistrict,             "Distrito del proyecto");
                    if (!data.EndDate.HasValue)              missing.Add("Fecha de fin del contrato");
                    if (!data.ContractNumber.HasValue)       missing.Add("Número de contrato");
                    if (!data.PromissoryNoteNumber.HasValue) missing.Add("Número de pagaré");
                    // El adelanto es obligatorio solo en "Contrato con adelanto" (2). En "Pago a cuenta" (4)
                    // el % / monto del pagaré es opcional.
                    if ((data.PaymentMethodId == 2 || data.PaymentMethodId == 4) && !data.AdvancePercentage.HasValue && !data.AdvanceAmount.HasValue)
                        missing.Add("Adelanto");
                    break;

                case AdjudicacionDocumentType.SummarySheet:
                    if (!data.ContractNumber.HasValue)          missing.Add("Número de contrato");
                    if (!data.SigningDate.HasValue)             missing.Add("Fecha de firma");
                    if (!data.StartDate.HasValue)               missing.Add("Fecha de inicio del contrato");
                    if (!data.EndDate.HasValue)                 missing.Add("Fecha de fin del contrato");
                    if (!data.GuaranteeFundPercentage.HasValue) missing.Add("% de fondo de garantía");
                    if (!data.GuaranteeFundDays.HasValue)       missing.Add("Días de fondo de garantía");
                    break;
            }

            if (missing.Count > 0)
                throw new AbrilException(
                    "No se puede generar el documento. Complete primero los siguientes datos: " +
                    $"{string.Join(", ", missing)}.",
                    400);
        }

        private async Task<DocumentUploadResponseDto> GenerateInstructivoAsync(
            int projectSubContractorId, int userId)
        {
            var folderInfo = await _projectSubContractorRepository.GetInstructivosFolderAsync(projectSubContractorId)
                ?? throw new AbrilException("No se encontró la adjudicación.");

            if (string.IsNullOrEmpty(folderInfo.FolderId))
                throw new AbrilException(
                    "Esta partida de control no tiene instructivo sincronizado. " +
                    "Ejecute la sincronización desde Configuración → Partidas de control o suba el archivo manualmente.");

            byte[] content;
            string? contentType;
            string fileName;

            if (folderInfo.SyncStatus == 2)
            {
                // Instructivo subido manualmente: FolderId contiene la URL de SharePoint
                content     = await _sharePointService.DownloadFromSharePointAsync(_site, folderInfo.FolderId);
                contentType = "application/octet-stream";
                fileName    = folderInfo.FolderName ?? "instructivo";
            }
            else
            {
                // Instructivo sincronizado automáticamente: FolderId es un folder ID de OneDrive
                var driveId    = _oneDriveOptions.AdjudicacionesFeature.Instructivos.DriveId;
                var folderPath = $"{_oneDriveOptions.AdjudicacionesFeature.Instructivos.FolderPath}/{folderInfo.FolderName}";

                var children = await _sharePointService.GetFolderChildrenAsync(
                    driveId, folderPath, excludedFolderNames: ["OBSOLETOS"]);

                var file = children.FirstOrDefault(c => !c.IsFolder)
                    ?? throw new AbrilException(
                        $"No se encontró ningún instructivo vigente en la carpeta '{folderInfo.FolderName}'. " +
                        "Verifique que el área de Calidad haya publicado el archivo.");

                (content, contentType) = await _sharePointService.DownloadFromOneDriveByItemIdAsync(driveId, file.Id);
                fileName = file.Name;
            }

            var pathData  = await _projectSubContractorRepository.GetPathDataAsync(projectSubContractorId);

            using var stream = new MemoryStream(content);
            var spResult = await _oneDriveStorage.UploadAsync(
                pathData, AdjudicacionDocumentType.Instructivo, fileName, stream,
                contentType ?? "application/octet-stream");

            await _projectSubContractorRepository.SaveDocumentAsync(
                projectSubContractorId,
                AdjudicacionDocumentType.Instructivo,
                spResult.WebUrl!,
                fileName,
                userId,
                spResult.ItemId);

            return new DocumentUploadResponseDto
            {
                FileUrl = spResult.WebUrl!,
                OriginalFileName = fileName,
            };
        }

        private async Task<DocumentUploadResponseDto> GenerateSummarySheetAsync(
            int projectSubContractorId, int userId)
        {
            var data = await _projectSubContractorRepository.GetSummarySheetDataAsync(projectSubContractorId);
            ValidateGenerationData(data, AdjudicacionDocumentType.SummarySheet);

            var abreviaturaProyecto = !string.IsNullOrWhiteSpace(data.Abbreviation)
                ? data.Abbreviation
                : (data.ProjectDescription.Length >= 3
                    ? data.ProjectDescription[..3].ToUpperInvariant()
                    : data.ProjectDescription.ToUpperInvariant());

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("1.HOJA RESUMEN");
            BuildSummarySheet(ws, data);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Position = 0;

            var pathData = await _projectSubContractorRepository.GetPathDataAsync(projectSubContractorId);

            var fileName   = $"HOJA RESUMEN N°{data.ContractNumber?.ToString("D3") ?? "000"}{abreviaturaProyecto} – {DateTime.UtcNow.Year}.xlsx";
            const string xlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            var spResult = await _oneDriveStorage.UploadAsync(
                pathData, AdjudicacionDocumentType.SummarySheet, fileName, ms, xlsxMime);

            var fileUrl = spResult.WebUrl!;

            await _projectSubContractorRepository.SaveDocumentAsync(
                projectSubContractorId, AdjudicacionDocumentType.SummarySheet, fileUrl, fileName, userId, spResult.ItemId);

            return new DocumentUploadResponseDto { FileUrl = fileUrl, OriginalFileName = fileName };
        }

        private async Task<DocumentUploadResponseDto> GenerateContractAsync(
            int projectSubContractorId, int userId)
        {
            var data = await _projectSubContractorRepository.GetSummarySheetDataAsync(projectSubContractorId);
            ValidateGenerationData(data, AdjudicacionDocumentType.Contract);

            // Solo hay plantilla para las 3 modalidades del catálogo. Cualquier otro valor es dato
            // inconsistente: se avisa explícitamente en vez de buscar un archivo que no existe.
            var templateFileName = data.ContractModalityId switch
            {
                1 => "plantilla_suministro_e_instalacion_con_placeholders.docx",
                2 => "plantilla_suministro_con_placeholders.docx",
                3 => "plantilla_instalacion_con_placeholders.docx",
                _ => throw new AbrilException(
                        "La modalidad de contrato de esta adjudicación no es válida. " +
                        "Corríjala en el paso 1 antes de generar el contrato.", 400),
            };

            var templatePath = Path.Combine(
                AppContext.BaseDirectory,
                "Features", "CostsModule", "Features", "AdjudicacionesFeature",
                "Templates", templateFileName);

            if (!File.Exists(templatePath))
                throw new AbrilException(
                    "No se encontró la plantilla del contrato en el servidor. " +
                    "Contacte al administrador del sistema.");

            // PLAZO: preferir el valor guardado en BD (TermDays); si aún no existe, calcularlo al vuelo
            var plazo = data.TermDays
                ?? ((data.StartDate.HasValue && data.EndDate.HasValue)
                    ? (int)(data.EndDate.Value.ToDateTime(TimeOnly.MinValue)
                          - data.StartDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays
                    : 0);

            var currencySymbol = data.CurrencyCode == "USD" ? "US$" : "S/";

            var abreviaturaProyecto = !string.IsNullOrWhiteSpace(data.Abbreviation)
                ? data.Abbreviation
                : (data.ProjectDescription.Length >= 3
                    ? data.ProjectDescription[..3].ToUpperInvariant()
                    : data.ProjectDescription.ToUpperInvariant());

            var advanceAmount = data.AdvanceAmount
                ?? (data.AdvancePercentage.HasValue
                    ? Math.Round(data.AdvancePercentage.Value / 100m * data.Amount, 2)
                    : 0m);

            var esCulture = new CultureInfo("es");
            var entero = (long)Math.Truncate(data.Amount);
            var centavos = (int)Math.Round((data.Amount - entero) * 100);
            var palabras = entero.ToWords(esCulture);
            palabras = char.ToUpper(palabras[0]) + palabras[1..];
            var moneda = data.CurrencyCode == "USD" ? "dólares" : "soles";
            var monedaMayuscula = moneda.ToUpperInvariant();
            var montoEnPalabras = $"{palabras} con {centavos:D2}/100 {moneda}";

            // Adelanto en palabras
            var advanceEntero   = (long)Math.Truncate(advanceAmount);
            var advanceCentavos = (int)Math.Round((advanceAmount - advanceEntero) * 100);
            var advancePalabras = advanceEntero.ToWords(esCulture);
            advancePalabras = char.ToUpper(advancePalabras[0]) + advancePalabras[1..];
            var advanceAmountEnPalabras = $"{advancePalabras} con {advanceCentavos:D2}/100 {moneda}";

            // Diferencia (monto total − adelanto)
            var diferencia          = data.Amount - advanceAmount;
            var diferenciaEntero    = (long)Math.Truncate(diferencia);
            var diferenciaCentavos  = (int)Math.Round((diferencia - diferenciaEntero) * 100);
            var diferenciaPalabras  = diferenciaEntero.ToWords(esCulture);
            diferenciaPalabras = char.ToUpper(diferenciaPalabras[0]) + diferenciaPalabras[1..];
            var diferenciaEnPalabras = $"{diferenciaPalabras} con {diferenciaCentavos:D2}/100 {moneda}";
            var diferenciaFormato    = data.CurrencyCode == "USD"
                ? $"US$. {diferencia:N2}"
                : $"S/. {diferencia:N2}";

            // Fondo de garantía — valores registrados en el paso 2 (con fallback a los anteriores por defecto)
            var fondoPorc  = data.GuaranteeFundPercentage ?? 5;
            var fondoDias  = data.GuaranteeFundDays       ?? 365;
            var fondoAnios = fondoDias / 365;
            var fondoMeses = (int)Math.Round(fondoDias / 30.0);
            var fondoPorcPalabras  = ((long)fondoPorc).ToWords(esCulture);
            var fondoMesesPalabras = ((long)fondoMeses).ToWords(esCulture);
            // {{HOJA_RESUMEN_FONDO_GARANTÍA_PORCENTAJE}}: "{x}% Fondo de Garantía"; si es 0 o null → "-".
            var hasFondoGarantia = data.GuaranteeFundPercentage.HasValue && data.GuaranteeFundPercentage.Value > 0;
            var hojaResumenFondoGarantiaPorcentaje = hasFondoGarantia
                ? $"{data.GuaranteeFundPercentage}% Fondo de Garantía"
                : "-";
            // Si el plazo es menor a un año, mostrar el plazo en días en lugar de "0 año".
            // 1 año → singular; 2 o más → "años".
            var fondoAniosTexto = fondoAnios >= 1
                ? $"{fondoAnios} {(fondoAnios >= 2 ? "años" : "año")}"
                : $"{fondoDias} días";

            // Plazo en palabras
            var plazoPalabras = plazo > 0 ? ((long)plazo).ToWords(esCulture) : "";
            if (!string.IsNullOrEmpty(plazoPalabras))
                plazoPalabras = char.ToUpper(plazoPalabras[0]) + plazoPalabras[1..];

            // Tipo de documento de garantía:
            //   • Con adelanto (PaymentMethodId == 2) Y de Suministro (ContractModalityId == 2):
            //     Pagaré + Letra de Garantía + Carta de Fianza.
            //   • Solo con adelanto (otra modalidad): Pagaré + Letra de Garantía.
            //   • Sin adelanto: "-".
            var numPagareStr = data.PromissoryNoteNumber.HasValue
                ? data.PromissoryNoteNumber.Value.ToString("D3")
                : "";
            var pagareRef = $"PAGARÉ N°{numPagareStr}{abreviaturaProyecto}-{DateTime.UtcNow.Year}";
            // La carta de fianza solo aplica en Suministro (ContractModalityId == 2) + contrato con adelanto
            // (PaymentMethodId == 2) y únicamente cuando se marcó el toggle IncludesCartaFianza.
            var includeCartaFianza = (data.PaymentMethodId == 2 || data.PaymentMethodId == 4) && data.ContractModalityId == 2 && data.IncludesCartaFianza;
            var tipoDocumentoGarantia = data.PaymentMethodId == 2 || data.PaymentMethodId == 4
                ? (includeCartaFianza
                    ? $"{pagareRef}, LETRA DE GARANTÍA Y CARTA DE FIANZA"
                    : $"{pagareRef} Y LETRA DE GARANTÍA")
                : "-";

            // Cláusulas del numeral 5.1.x según la forma de pago (PaymentMethodId).
            // Los valores se insertan ya resueltos y la negrita inline se marca con **…**.
            // El párrafo {{CLÁUSULAS_ADELANTO}} se sustituye por estas cláusulas (auto-numeradas por Word).
            var advancePercentageStr = data.AdvancePercentage.HasValue ? $"{data.AdvancePercentage:N2}%" : "";
            var advanceAmountStr     = $"{currencySymbol} {advanceAmount:N2}";

            // Frecuencia de valorización tomada de la "Forma de Pago" (Semanal / Quincenal).
            // Por defecto "semanales" si no se registró.
            var frecuenciaValorizacion =
                (data.PaymentFormDescription ?? "").Trim().Equals("Quincenal", StringComparison.OrdinalIgnoreCase)
                    ? "quincenales"
                    : "semanales";

            // Modalidad SUMINISTRO (id 2): en el flujo sin adelanto, la cláusula de valorización se traslada
            // al cierre de la 5.1 vía {{CLAUSULA_MONTO_TOTAL}}, por lo que 5.1.1 desaparece (lista vacía).
            // Para las demás modalidades (Suministro e Instalación / Instalación) se mantiene el comportamiento previo.
            var esSuministro = data.ContractModalityId == 2;

            // Oración de cierre de la valorización (numeral 5.1.x). Si la partida tiene
            // "Formas de valorización" registradas (Configuración → Partidas), se reemplaza el
            // texto por defecto "Las valorizaciones se determinan a partir del inicio de los
            // trabajos…" por el desglose de porcentajes de la partida (ej.: "Las valorizaciones
            // serán de acuerdo con los siguientes porcentajes de valorización: 60% por
            // instalación, 30% por acabado y 10% por levantamiento de observaciones.").
            var formasValorizacionSentence = BuildValorizationFormsSentence(data.ValorizationForms);
            var cierreSaldoValorizacion = formasValorizacionSentence
                ?? "Las valorizaciones se determinan a partir del inicio de los trabajos de obra.";
            var cierreValorizacion = formasValorizacionSentence
                ?? "Las valorizaciones se determinan a partir del inicio de los trabajos en obra.";

            List<string> clausulasAdelanto;
            if (data.PaymentMethodId == 2 || data.PaymentMethodId == 4)
            {
                // Contrato con adelanto → 5.1.1 (adelanto) y 5.1.2 (saldo). Igual para todas las modalidades.
                clausulasAdelanto = new List<string>
                {
                    $"Un adelanto **equivalente al {advancePercentageStr} del monto contractual**, que se otorgará en el mes de julio, " +
                    $"es decir la suma de **{advanceAmountStr} ({advanceAmountEnPalabras})** incluido el I.G.V. previa entrega de un pagaré " +
                    "irrevocable incondicionada por el mismo importe; la misma que deberá encontrarse vigente por todo el plazo de ejecución de la Obra. " +
                    "**EL CONTRATANTE** entregará a **EL CONTRATISTA** el presente adelanto dentro de los 7 días hábiles de presentada la factura por este concepto, " +
                    "siempre que la referida factura sea emitida de acuerdo con las normas tributarias.",

                    $"El **saldo** equivalente a la suma de **{diferenciaFormato} ({diferenciaEnPalabras})** será cancelado mediante valorizaciones semanales, " +
                    "pagaderas a los 7 días hábiles siguientes de recepcionada la factura y/o valorización correspondiente, debidamente emitida, " +
                    "con la respectiva retención del fondo de garantía. " + cierreSaldoValorizacion
                };
            }
            else if (esSuministro)
            {
                // Suministro sin adelanto → 5.1.1 NO aplica (su texto vive ahora en el cierre de {{CLAUSULA_MONTO_TOTAL}}).
                clausulasAdelanto = new List<string>();
            }
            else
            {
                // Otras modalidades sin adelanto (Suministro e Instalación / Instalación) → única cláusula 5.1.1 de valorización.
                clausulasAdelanto = new List<string>
                {
                    $"Pago mediante valorizaciones {frecuenciaValorizacion}, pagaderas a los 7 días hábiles siguientes de recepcionada la factura " +
                    "y/o valorización correspondiente, debidamente emitida, con la respectiva retención del fondo de garantía. " +
                    cierreValorizacion
                };
            }

            // {{CLAUSULA_MONTO_TOTAL}} — texto completo de la cláusula 5.1 (solo SUMINISTRO).
            // Se pasa como multiParagraphReplacements (1 párrafo) para que los marcadores **negrita** funcionen.
            // Para otras modalidades, lista vacía (el placeholder no existe en esas plantillas → no-op).
            List<string> clausulaMontoTotalParagraphs;
            if (esSuministro)
            {
                var palabrasSinMoneda = $"{palabras} con {centavos:D2}/100";

                var cierre = data.PaymentMethodId == 2 || data.PaymentMethodId == 4
                    ? "de la siguiente forma:"
                    : "acorde a los despachos establecidos en el Cronograma, mediante valorizaciones previa entrega del suministro, " +
                      "lo cual deberá ser cancelado a los siete (7) días hábiles siguientes de brindada la conformidad de " +
                      "**EL SUMINISTRADO** del producto entregado, siempre que no hubiese observaciones sobre el suministro.";

                clausulaMontoTotalParagraphs = new List<string>
                {
                    $"El monto total del **CONTRATO DE SUMINISTRO** por el concepto de **{data.WorkItemDescription} EN {monedaMayuscula}** " +
                    $"es de **{currencySymbol} {data.Amount:N2} ({palabrasSinMoneda})** incluido el I.G.V. " +
                    $"(en adelante, **EL PRECIO**) que será cancelado {cierre}"
                };
            }
            else
            {
                clausulaMontoTotalParagraphs = new List<string>();
            }

            // ── Valores de adelanto para la hoja resumen del contrato ─────────────
            // Si no hay adelanto efectivo (0 o null) los placeholders resuelven a "-" en lugar de "0.00%" / "S/ 0.00".
            var hasAdvance = data.AdvancePercentage.HasValue && data.AdvancePercentage.Value > 0;
            var advancePercentageDisplay = hasAdvance ? $"{data.AdvancePercentage:N2}%" : "-";
            var advanceAmountDisplay = hasAdvance
                ? $"{currencySymbol} {advanceAmount:N2} {(data.HasIgv ? "Inc. IGV" : "No Inc. IGV")}"
                : "-";

            // {{HOJA_RESUMEN_FORMA_DE_PAGO}}: solo descripción del método de pago + valorización (semanal/quincenal)
            // con pago a los días hábiles configurados en el paso 2 (PaymentDays, default 7). No repite el %
            // ni el monto del adelanto (eso ya lo muestra {{HOJA_RESUMEN_ADELANTO_MONTO}} en la fila de arriba).
            var pagoDiasHabiles = data.PaymentDays > 0 ? data.PaymentDays : 7;
            var valorizacionTexto = !string.IsNullOrWhiteSpace(data.PaymentFormDescription)
                ? $"valorización {data.PaymentFormDescription.Trim().ToLower(esCulture)} con pago a {pagoDiasHabiles} días hábiles"
                : $"valorizaciones con pago a {pagoDiasHabiles} días hábiles";
            var hojaResumenFormaDePago = $"{data.PaymentMethodDescription}, {valorizacionTexto}";

            // {{HOJA_RESUMEN_ADELANTO_MONTO}}: con adelanto → "% - monto"; sin adelanto → única "-".
            // Se usa hasAdvance (no un método de pago específico) porque el adelanto puede darse
            // tanto en "Contrato con adelanto" (2) como en "Pago a cuenta" (4).
            var hojaResumenAdelantoMonto = hasAdvance
                ? $"{advancePercentageDisplay} - {advanceAmountDisplay}"
                : "-";

            var replacements = new Dictionary<string, string>
            {
                // Contratista
                { "{{CONTRATISTA_RAZON_SOCIAL}}",      data.ContributorName },
                { "{{CONTRATISTA_RUC}}",               data.ContributorRuc },
                { "{{CONTRATISTA_UBICACION}}",         data.ContributorAddress ?? "" },
                { "{{CONTRATISTA_DISTRITO}}",          data.ContributorDistrict ?? "" },
                { "{{CONTRATISTA_PROVINCIA}}",         data.ContributorProvince ?? "" },
                { "{{CONTRATISTA_DEPARTAMENTO}}",      data.ContributorDepartment ?? "" },
                { "{{CONTRATISTA_REPRESENTANTE_NOMBRE}}", data.LegalRepresentativeFullName ?? "" },
                { "{{CONTRATISTA_REPRESENTANTE_DNI}}", data.LegalRepresentativeDni ?? "" },
                { "{{CONTRATISTA_PARTIDA_REGISTRAL}}", data.LegalEntityRegistryNumber ?? "" },
                // Proyecto
                { "{{PROYECTO_NOMBRE}}",               data.ProjectDescription },
                { "{{PROYECTO_ABREVIATURA}}",          abreviaturaProyecto },
                { "{{PROYECTO_RAZON_SOCIAL}}",         (data.ProjectRazonSocial ?? "").ToUpper() },
                { "{{PROYECTO_RUC}}",                  data.ProjectContributorRuc ?? "" },
                // Ubicación de la obra y desc. de pisos, tomadas SIEMPRE del proyecto (project.project_*),
                // nunca del contribuyente asociado — ese va en los {{CONTRATISTA_*}}. {{PROYECTO_UBICACION_OBRA}}
                // es solo la dirección (calle/av.); distrito, provincia y departamento van aparte.
                // Los 5 van en mayúscula porque en BD conviven "LIMA" y "Lima" (se tipean a mano en
                // Configuración → Proyectos) y el contrato debe imprimirlos uniformes.
                { "{{PROYECTO_DISTRITO}}",             (data.ProjectDistrict   ?? "").ToUpper() },
                { "{{PROYECTO_PROVINCIA}}",            (data.ProjectProvince   ?? "").ToUpper() },
                { "{{PROYECTO_DEPARTAMENTO}}",         (data.ProjectDepartment ?? "").ToUpper() },
                { "{{PROYECTO_UBICACION_OBRA}}",       (data.ProjectLocation   ?? "").ToUpper() },
                { "{{DESCRIPCIÓN_DE_PISOS}}",          (data.LevelDescription  ?? "").ToUpper() },
                { "{{PROYECTO_PARTIDA_REGISTRAL}}",    data.ProjectLegalEntityRegistryNumber ?? "" },
                // Contrato
                { "{{PAGO_A_CUENTA}}",                 data.PaymentMethodId == 4 ? "mediante Pago a cuenta, " : "" },
                { "{{FORMA_DE_PAGO}}",                 data.PaymentMethodDescription },
                { "{{FORMA_DE_VALORIZACIÓN}}",         data.PaymentFormDescription ?? "" },
                { "{{MONTO}}",                         $"{currencySymbol} {data.Amount:N2}" },
                { "{{MONTO_CON_IGV}}",                 $"{currencySymbol} {data.Amount:N2} {(data.HasIgv ? "incluido IGV" : "sin IGV")}" },
                { "{{MONTO_EN_PALABRAS}}",             montoEnPalabras },
                { "{{MONEDA}}",                        $"EN {monedaMayuscula}" },
                { "{{FECHA_INICIO}}",                  data.StartDate?.ToString("dd/MM/yyyy") ?? "" },
                { "{{FECHA_FIN}}",                     data.EndDate?.ToString("dd/MM/yyyy")   ?? "" },
                // Fecha de firma del contrato formateada como "10 de julio del 2025" (es-PE; "del" en lugar de "de").
                { "{{FECHA_FIRMA_DEL_CONTRATO}}",
                    data.SigningDate.HasValue
                        ? data.SigningDate.Value
                              .ToDateTime(TimeOnly.MinValue)
                              .ToString("d 'de' MMMM 'del' yyyy", esCulture)
                        : "" },
                { "{{PLAZO_NUM}}",                     plazo.ToString() },
                { "{{PLAZO_EN_PALABRAS}}",             plazoPalabras },
                { "{{ADVANCE_PERCENTAGE}}",            advancePercentageDisplay },
                { "{{FORMA_DE_PAGO_ADVANCE_PERCENTAGE}}", (data.PaymentMethodId == 2 || data.PaymentMethodId == 4) && data.AdvancePercentage.HasValue ? $"{data.AdvancePercentage:N2}%" : "" },
                { "{{ADVANCE_AMOUNT}}",                advanceAmountDisplay },
                { "{{HOJA_RESUMEN_FORMA_DE_PAGO}}",    hojaResumenFormaDePago },
                { "{{HOJA_RESUMEN_ADELANTO_MONTO}}",   hojaResumenAdelantoMonto },
                { "{{ADVANCE_AMOUNT_EN_PALABRAS}}",    advanceAmountEnPalabras },
                { "{{DIFERENCIA_MONTO}}",              diferenciaFormato },
                { "{{DIFERENCIA_MONTO_EN_PALABRAS}}", diferenciaEnPalabras },
                { "{{PERIODO_VALIDEZ_GARANTIA}}",      FormatGuaranteeValidity(data.GuaranteeValidityDays) },
                { "{{FONDO_GARANTÍA_PORCENTAJE}}",     $"{fondoPorc}%" },
                { "{{HOJA_RESUMEN_FONDO_GARANTÍA_PORCENTAJE}}", hojaResumenFondoGarantiaPorcentaje },
                { "{{FONDO_GARANTÍA_EN_PALABRAS}}",    $"{fondoPorcPalabras} por ciento" },
                { "{{FONDO_GARANTÍA_PLAZO_EN_DÍAS}}",  $"{fondoDias} días" },
                { "{{FONDO_GARANTÍA_PLAZO_EN_AÑOS}}",  fondoAniosTexto },
                { "{{FONDO_GARANTÍA_PLAZO_NUM_PALABRA}}", $"{fondoMeses} ({fondoMesesPalabras})" },
                { "{{TIPO_CONTRATO}}",                 data.ContractTypeDescription },
                { "{{TIPO_CONTRATO_MAYÚSCULA}}",       (data.ContractTypeDescription ?? "").ToUpper() },
                { "{{PARTIDA}}",                       string.IsNullOrWhiteSpace(data.ContractWorkItemName) ? data.WorkItemDescription : data.ContractWorkItemName },
                // El año se toma de la fecha de firma (definida en el paso 2), no del año actual.
                { "{{AÑO_ACTUAL}}",                    (data.SigningDate?.Year ?? DateTime.UtcNow.Year).ToString() },
                { "{{NUM_CONTRATO}}",                  data.ContractNumber.HasValue ? data.ContractNumber.Value.ToString("D3") : "" },
                { "{{NUM_PAGARE}}",                    data.PromissoryNoteNumber.HasValue ? data.PromissoryNoteNumber.Value.ToString("D3") : "" },
                { "{{TIPO_DOCUMENTO_GARANTÍA}}",       tipoDocumentoGarantia },
            };

            // Las cláusulas se insertan como texto puro: el auto-numerado de Word genera el número
            // y el tabulador de posición automáticamente. El nivel/posición depende ÚNICAMENTE del
            // párrafo {{CLÁUSULAS}} en cada plantilla (p. ej. sección 9 en instalación/contrato,
            // sección 7 en suministro); el código clona ese formato sin asumir una posición fija.
            var clauseParagraphs = data.SpecialClauses.ToList();

            // Links de planos del proyecto (Planos de especialidades = type 1, Planos de detalles = type 2)
            // La plantilla ya tiene el texto estático ("ENLACE DE ACCESO", "Se adjunta…") y
            // solo expone {{LINK1}} y {{LINK2}} como marcadores de URL.
            var projectLinks = await _projectLinkRepository.GetByProjectIdAsync(data.ProjectId);
            var linkEspecialidades = projectLinks.FirstOrDefault(l => l.ProjectLinkTypeId == 1 && l.Active);
            var linkDetalles       = projectLinks.FirstOrDefault(l => l.ProjectLinkTypeId == 2 && l.Active);

            // La modalidad Suministro (id 2) no incluye planos en su contrato, por lo que
            // se omite la validación de links. Las demás modalidades (Suministro e Instalación /
            // Instalación) sí los requieren.
            if (!esSuministro)
            {
                var missingLinks = new List<string>();
                if (linkEspecialidades == null) missingLinks.Add("Planos de Especialidades");
                if (linkDetalles       == null) missingLinks.Add("Planos de Detalles");

                if (missingLinks.Count > 0)
                    throw new AbrilException(
                        $"Para generar el contrato falta registrar el link de: {string.Join(" y ", missingLinks)}.",
                        400);
            }

            replacements["{{LINK1}}"] = linkEspecialidades?.LinkUrl ?? "";
            replacements["{{LINK2}}"] = linkDetalles?.LinkUrl ?? "";

            // Cláusula del Anexo 3 (Pagaré) — solo aplica cuando hay adelanto (PaymentMethodId == 2).
            // Se pasa como multi-párrafo: si la lista está vacía, el helper elimina el párrafo entero
            // (incluido el bullet "•") para que no quede una viñeta huérfana en el documento.
            var clausulaAnexo3Pagare = data.PaymentMethodId == 2 || data.PaymentMethodId == 4
                ? new List<string> { $"• {advancePercentageStr} de adelanto del monto total con la firma de este contra letra de garantía y pagaré." }
                : new List<string>();

            // Cláusula 10.2 "De la carta fianza" — SOLO en contratos con adelanto (PaymentMethodId == 2).
            // Se separa en dos marcadores para respetar el formato de la plantilla:
            //   {{CARTA_FIANZA}}         → título "De la carta fianza:" (párrafo numerado 10.2, subrayado).
            //   {{CARTA_FIANZA_DETALLE}} → contenido como VIÑETA real (mismo estilo de lista que la garantía).
            // El detalle NO lleva "•" literal: la viñeta la pone Word desde el párrafo de la plantilla.
            // Subrayado con __…__, negrita con **…**. Si la lista está vacía, el helper elimina el párrafo.
            var clausulaCartaFianza = includeCartaFianza
                ? new List<string> { "__De la carta fianza:__" }
                : new List<string>();

            var clausulaCartaFianzaDetalle = includeCartaFianza
                ? new List<string>
                {
                    "A efectos de garantizar cualquier gasto adicional en el que tenga que incurrir " +
                    "**EL SUMINISTRADO** a causa de daños a terceros que hayan sido originados por observaciones " +
                    "de **EL SUMINISTRO**, dentro del periodo de vigencia de la garantía, pudiendo ser multas " +
                    "ante INDECOPI, trabajos de cambio total o parcial de **EL SUMINISTRO**, entre otros; " +
                    "**EL SUMINISTRANTE** extenderá una carta fianza por el 20% por ciento del valor total de " +
                    "**EL SUMINISTRO** equivalente al monto del adelanto, a favor de **EL SUMINISTRADO**; la misma " +
                    "que podrá ser ejecutada de manera inmediata, en caso de acreditarse cualquiera de los supuestos " +
                    "previstos para ello en el presente contrato."
                }
                : new List<string>();

            byte[] docBytes;
            using (var templateStream = File.OpenRead(templatePath))
                docBytes = WordTemplateHelper.FillTemplate(
                    templateStream,
                    replacements,
                    multiParagraphReplacements: new Dictionary<string, List<string>>
                    {
                        { "{{CLÁUSULAS}}", clauseParagraphs },
                        { "{{CLÁUSULAS_ADELANTO}}", clausulasAdelanto },
                        { "{{CLÁUSULA_ANEXO_3_PAGARÉ}}", clausulaAnexo3Pagare },
                        { "{{CLAUSULA_MONTO_TOTAL}}", clausulaMontoTotalParagraphs },
                        // Cláusulas de Anexo 3 y Anexo 4 (Suministro). Solo existen en la plantilla de
                        // Suministro; en las demás el marcador no aparece y simplemente se ignora.
                        { "{{CLÁUSULAS_ANEXO_3_SUMINISTRO}}", data.SpecialClausesAnexo3.ToList() },
                        { "{{CLÁUSULAS_ANEXO_4_SUMINISTRO}}", data.SpecialClausesAnexo4.ToList() },
                        { "{{CARTA_FIANZA}}", clausulaCartaFianza },
                        { "{{CARTA_FIANZA_DETALLE}}", clausulaCartaFianzaDetalle }
                    },
                    // El texto de {{LINK1}}/{{LINK2}} se reemplaza arriba, pero el DESTINO del clic
                    // vive como relación aparte en la plantilla (horneada a otro proyecto). Se reescribe
                    // aquí para que el hipervínculo apunte a la URL registrada en Planos por proyecto.
                    hyperlinkTargets: new Dictionary<string, string>
                    {
                        { "{{LINK1}}", linkEspecialidades?.LinkUrl ?? "" },
                        { "{{LINK2}}", linkDetalles?.LinkUrl ?? "" },
                    });

            var pathData = await _projectSubContractorRepository.GetPathDataAsync(projectSubContractorId);

            var fileName   = $"CONTRATO N°{data.ContractNumber?.ToString("D3") ?? "000"}{abreviaturaProyecto} – {DateTime.UtcNow.Year}.docx";
            const string docxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            string fileUrl;
            string? contractItemId;
            string finalFileName;
            using (var ms = new MemoryStream(docBytes))
            {
                // autoRenameOnLock: si ya existe un contrato con el mismo nombre abierto/en uso
                // (HTTP 423), se sube con un nombre alterno ("… (2).docx") en lugar de fallar.
                var spResult = await _oneDriveStorage.UploadAsync(
                    pathData, AdjudicacionDocumentType.Contract, fileName, ms, docxMime,
                    autoRenameOnLock: true);

                fileUrl        = spResult.WebUrl!;
                contractItemId = spResult.ItemId;
                finalFileName  = spResult.FileName ?? fileName;   // puede diferir si hubo renombrado
            }

            await _projectSubContractorRepository.SaveDocumentAsync(
                projectSubContractorId, AdjudicacionDocumentType.Contract, fileUrl, finalFileName, userId, contractItemId);

            return new DocumentUploadResponseDto { FileUrl = fileUrl, OriginalFileName = finalFileName };
        }

        private async Task<DocumentUploadResponseDto> GeneratePromissoryNoteAsync(
            int projectSubContractorId, int userId)
        {
            var data = await _projectSubContractorRepository.GetSummarySheetDataAsync(projectSubContractorId);
            ValidateGenerationData(data, AdjudicacionDocumentType.PromissoryNote);

            var templatePath = Path.Combine(
                AppContext.BaseDirectory,
                "Features", "CostsModule", "Features", "AdjudicacionesFeature",
                "Templates", "plantilla_pagare_con_placeholders.docx");

            if (!File.Exists(templatePath))
                throw new AbrilException(
                    "No se encontró la plantilla del pagaré en el servidor. " +
                    "Contacte al administrador del sistema.");

            var esCulture   = new CultureInfo("es");
            var currencySymbol = data.CurrencyCode == "USD" ? "US$" : "S/";

            var advanceAmount = data.AdvanceAmount
                ?? (data.AdvancePercentage.HasValue
                    ? Math.Round(data.AdvancePercentage.Value / 100m * data.Amount, 2)
                    : 0m);

            // Adelanto en palabras
            var advanceEntero   = (long)Math.Truncate(advanceAmount);
            var advanceCentavos = (int)Math.Round((advanceAmount - advanceEntero) * 100);
            var advancePalabras = advanceEntero.ToWords(esCulture);
            advancePalabras = char.ToUpper(advancePalabras[0]) + advancePalabras[1..];
            var moneda = data.CurrencyCode == "USD" ? "dólares" : "soles";
            var advanceAmountEnPalabras = $"{advancePalabras} con {advanceCentavos:D2}/100 {moneda}";

            // ADVANCE_FECHA_FIN: end_date + 3 meses
            var advanceFechaFin = data.EndDate.HasValue
                ? data.EndDate.Value.AddMonths(3).ToString("dd/MM/yyyy")
                : "";

            // FECHA_ACTUAL: "09 DE FEBRERO 2026"
            var mesesEs = new[] {
                "ENERO","FEBRERO","MARZO","ABRIL","MAYO","JUNIO",
                "JULIO","AGOSTO","SEPTIEMBRE","OCTUBRE","NOVIEMBRE","DICIEMBRE"
            };
            var hoy = DateTime.UtcNow;
            var fechaActual = $"{hoy.Day:D2} DE {mesesEs[hoy.Month - 1]} {hoy.Year}";

            var abreviaturaProyecto = !string.IsNullOrWhiteSpace(data.Abbreviation)
                ? data.Abbreviation
                : (data.ProjectDescription.Length >= 3
                    ? data.ProjectDescription[..3].ToUpperInvariant()
                    : data.ProjectDescription.ToUpperInvariant());

            var replacements = new Dictionary<string, string>
            {
                { "{{PROYECTO_ABREVIATURA}}",             abreviaturaProyecto },
                { "{{PROYECTO_RAZON_SOCIAL}}",            (data.ProjectRazonSocial ?? "").ToUpper() },
                { "{{PROYECTO_RUC}}",                     data.ProjectContributorRuc ?? "" },
                { "{{PROYECTO_NOMBRE}}",                  data.ProjectDescription },
                // Ubicación de la obra, tomada SIEMPRE del proyecto (project.project_*), nunca del
                // contribuyente asociado — ese va en los {{CONTRATISTA_*}} de más abajo.
                { "{{PROYECTO_DISTRITO}}",                data.ProjectDistrict ?? "" },
                { "{{PROYECTO_PROVINCIA}}",               data.ProjectProvince ?? "" },
                { "{{PROYECTO_DEPARTAMENTO}}",            data.ProjectDepartment ?? "" },
                { "{{PROYECTO_UBICACION_OBRA}}",          data.ProjectLocation ?? "" },
                { "{{DESCRIPCIÓN_DE_PISOS}}",             data.LevelDescription ?? "" },
                // El año se toma de la fecha de firma (definida en el paso 2), no del año actual.
                { "{{AÑO_ACTUAL}}",                       (data.SigningDate?.Year ?? hoy.Year).ToString() },
                { "{{NUM_PAGARE}}",                       data.PromissoryNoteNumber.HasValue ? data.PromissoryNoteNumber.Value.ToString("D3") : "" },
                { "{{NUM_CONTRATO}}",                     data.ContractNumber.HasValue ? data.ContractNumber.Value.ToString("D3") : "" },
                { "{{ADVANCE_AMOUNT}}",                   $"{currencySymbol} {advanceAmount:N2}" },
                { "{{ADVANCE_AMOUNT_EN_PALABRAS}}",       advanceAmountEnPalabras },
                { "{{ADVANCE_FECHA_FIN}}",                advanceFechaFin },
                { "{{MONEDA}}",                           moneda.ToUpperInvariant() },
                { "{{PARTIDA}}",                          string.IsNullOrWhiteSpace(data.ContractWorkItemName) ? data.WorkItemDescription : data.ContractWorkItemName },
                { "{{FECHA_ACTUAL}}",                     fechaActual },
                { "{{CONTRATISTA_RAZON_SOCIAL}}",         data.ContributorName },
                { "{{CONTRATISTA_RUC}}",                  data.ContributorRuc },
                { "{{CONTRATISTA_REPRESENTANTE_NOMBRE}}", data.LegalRepresentativeFullName ?? "" },
                { "{{CONTRATISTA_UBICACION}}",            data.ContributorAddress ?? "" },
                { "{{CONTRATISTA_DISTRITO}}",             data.ContributorDistrict ?? "" },
                { "{{CONTRATISTA_PROVINCIA}}",            data.ContributorProvince ?? "" },
                { "{{CONTRATISTA_DEPARTAMENTO}}",         data.ContributorDepartment ?? "" },
            };

            byte[] docBytes;
            using (var templateStream = File.OpenRead(templatePath))
                docBytes = WordTemplateHelper.FillTemplate(templateStream, replacements);

            var pathData = await _projectSubContractorRepository.GetPathDataAsync(projectSubContractorId);

            var fileName   = $"PAGARE N°{data.PromissoryNoteNumber?.ToString("D3") ?? "000"}{abreviaturaProyecto} – {DateTime.UtcNow.Year}.docx";
            const string docxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            string fileUrl;
            string? pagareItemId;
            using (var ms = new MemoryStream(docBytes))
            {
                var spResult = await _oneDriveStorage.UploadAsync(
                    pathData, AdjudicacionDocumentType.PromissoryNote, fileName, ms, docxMime);

                fileUrl      = spResult.WebUrl!;
                pagareItemId = spResult.ItemId;
            }

            await _projectSubContractorRepository.SaveDocumentAsync(
                projectSubContractorId, AdjudicacionDocumentType.PromissoryNote, fileUrl, fileName, userId, pagareItemId);

            return new DocumentUploadResponseDto { FileUrl = fileUrl, OriginalFileName = fileName };
        }

        public async Task<(byte[] Bytes, string FileUrl, string OriginalFileName)> GenerateContractPackageAsync(int projectSubContractorId, int userId)
        {
            var docs = await _projectSubContractorRepository.GetContractPackageUrlsAsync(projectSubContractorId);

            // Validar solo el caso legacy: archivo existe pero le falta el ItemId de SharePoint.
            // Si no hay archivo (marcado como "No aplica" en paso 3) se omite sin error.
            if (!string.IsNullOrEmpty(docs.SummarySheetUrl) && string.IsNullOrEmpty(docs.SummarySheetItemId))
                throw new AbrilException("La hoja resumen debe ser regenerada antes de generar el paquete. Vaya al paso 3 y presione 'Generar'.");
            if (!string.IsNullOrEmpty(docs.ContractUrl) && string.IsNullOrEmpty(docs.ContractItemId))
                throw new AbrilException("El contrato debe ser regenerado antes de generar el paquete. Vaya al paso 3 y presione 'Generar'.");
            // Causales de No Conformidad y Cuadro de Tolerancias ya no se suben: usan PDF de plantilla
            // y solo dependen del estado (Aprobado / No aplica), por lo que no requieren validación de archivo.
            if (!string.IsNullOrEmpty(docs.InstructivoUrl) && string.IsNullOrEmpty(docs.InstructivoItemId))
                throw new AbrilException("El instructivo debe ser recargado antes de generar el paquete. Vaya al paso 3 y vuelva a subir el archivo.");
            if (!string.IsNullOrEmpty(docs.PromissoryNoteUrl) && string.IsNullOrEmpty(docs.PromissoryNoteItemId))
                throw new AbrilException("El pagaré debe ser regenerado antes de generar el paquete. Vaya al paso 3 y presione 'Generar'.");
            if (!string.IsNullOrEmpty(docs.AnexoUrl) && string.IsNullOrEmpty(docs.AnexoItemId))
                throw new AbrilException("El anexo debe ser recargado antes de generar el paquete. Vaya al paso 3 y vuelva a subir el archivo.");

            // Orden: 1-Resumen, 2-Contrato (con cotización/ficha técnica/orden de servicio/cronograma
            // embebidos en sus respectivos marcadores), 3-Salidas no conforme, 4-Cuadro de tolerancias,
            // 5-Protección de acabados, 6-Instructivo, 7-Pagaré, 8-Anexo (Vigencia de Poder, siempre al
            // final del paquete). Los docs sin archivo (No aplica) se omiten.
            //
            // Todas las descargas se hacen en UNA sola llamada a Graph mediante $batch, en lugar de
            // N requests secuenciales. Para cada archivo se decide si ya es PDF (descarga directa)
            // o necesita conversión (?format=pdf) según la extensión de su OriginalFileName.
            static bool IsPdf(string? fileName) =>
                (fileName ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            var downloads = new List<(string ItemId, bool AlreadyPdf)>();
            if (!string.IsNullOrEmpty(docs.SummarySheetItemId))         downloads.Add((docs.SummarySheetItemId,         AlreadyPdf: false));
            if (!string.IsNullOrEmpty(docs.ContractItemId))             downloads.Add((docs.ContractItemId,             AlreadyPdf: false));
            if (!string.IsNullOrEmpty(docs.AttachedQuotationItemId))    downloads.Add((docs.AttachedQuotationItemId,    AlreadyPdf: IsPdf(docs.AttachedQuotationFileName)));
            if (!string.IsNullOrEmpty(docs.FichaTecnicaItemId))         downloads.Add((docs.FichaTecnicaItemId,         AlreadyPdf: IsPdf(docs.FichaTecnicaFileName)));
            if (!string.IsNullOrEmpty(docs.ServiceOrderItemId))         downloads.Add((docs.ServiceOrderItemId,         AlreadyPdf: IsPdf(docs.ServiceOrderFileName)));
            if (!string.IsNullOrEmpty(docs.ScheduleItemId))             downloads.Add((docs.ScheduleItemId,             AlreadyPdf: IsPdf(docs.ScheduleFileName)));
            if (!string.IsNullOrEmpty(docs.InstructivoItemId))          downloads.Add((docs.InstructivoItemId,          AlreadyPdf: false));
            if (!string.IsNullOrEmpty(docs.PromissoryNoteItemId))       downloads.Add((docs.PromissoryNoteItemId,       AlreadyPdf: false));
            if (!string.IsNullOrEmpty(docs.AnexoItemId))                downloads.Add((docs.AnexoItemId,                AlreadyPdf: IsPdf(docs.AnexoFileName)));

            if (downloads.Count == 0 && !docs.NonConformingOutputApproved && !docs.ToleranceChartApproved && !docs.FinishProtectionApproved)
                throw new AbrilException("No hay documentos para incluir en el paquete. Todos los documentos están marcados como 'No aplica'.");

            var downloaded = await _oneDriveStorage.DownloadMultipleAsPdfAsync(projectSubContractorId, downloads);

            var pdfBytesList = new List<byte[]>();

            if (!string.IsNullOrEmpty(docs.SummarySheetItemId))
                pdfBytesList.Add(RotatePdfPages(downloaded[docs.SummarySheetItemId]));

            if (!string.IsNullOrEmpty(docs.ContractItemId))
            {
                var contractPdf = downloaded[docs.ContractItemId];

                // Inserciones DENTRO del contrato — cada una se aplica si el archivo correspondiente existe.
                // Si el marcador no aparece en el contrato, InsertPdfAfterMarker hace fallback al final.
                //
                // 'insertsVisualOrder' = orden en que deben aparecer de ARRIBA hacia ABAJO en el PDF final:
                //   1) Cotización  2) Orden de Servicio  3) Ficha Técnica   (todos en Anexo 1)  4) Cronograma (Anexo 2)
                //
                // OJO: cuando varios marcadores están en la MISMA página, InsertPdfAfterMarker inserta el PDF
                // justo después de esa página, por lo que cada inserción empuja hacia abajo a la anterior:
                // la ÚLTIMA insertada queda PRIMERA. Por eso se procesa en orden INVERSO al orden visual deseado.
                var insertsVisualOrder = new (string ItemId, string Marker)[]
                {
                    (docs.AttachedQuotationItemId ?? "", ContractQuotationMarker),
                    (docs.ServiceOrderItemId      ?? "", ContractServiceOrderMarker),
                    (docs.FichaTecnicaItemId      ?? "", ContractFichaTecnicaMarker),
                    (docs.ScheduleItemId          ?? "", ContractScheduleMarker),
                };

                for (int i = insertsVisualOrder.Length - 1; i >= 0; i--)
                {
                    var (itemId, marker) = insertsVisualOrder[i];
                    if (string.IsNullOrEmpty(itemId)) continue;
                    contractPdf = InsertPdfAfterMarker(contractPdf, downloaded[itemId], marker);
                }

                pdfBytesList.Add(contractPdf);
            }

            // Causales de No Conformidad, Cuadro de Tolerancias y Protección de Acabados: van JUSTO
            // DESPUÉS del contrato (en ese orden). Ya no se suben: se usa un PDF de plantilla fijo y
            // solo se incluyen cuando su estado es "Sí aplica" (Aprobado).
            if (docs.NonConformingOutputApproved)
                pdfBytesList.Add(ReadTemplatePdf("causales_de_no_conformidad.pdf"));
            if (docs.ToleranceChartApproved)
                pdfBytesList.Add(ReadTemplatePdf("cuadro_de_tolerancias.pdf"));
            if (docs.FinishProtectionApproved)
                pdfBytesList.Add(ReadTemplatePdf("proteccion_de_acabados.pdf"));

            if (!string.IsNullOrEmpty(docs.InstructivoItemId))         pdfBytesList.Add(downloaded[docs.InstructivoItemId]);
            if (!string.IsNullOrEmpty(docs.PromissoryNoteItemId))      pdfBytesList.Add(downloaded[docs.PromissoryNoteItemId]);

            // El anexo (Vigencia de Poder) cierra el paquete: va después de todo lo anterior.
            if (!string.IsNullOrEmpty(docs.AnexoItemId))               pdfBytesList.Add(downloaded[docs.AnexoItemId]);

            var mergedBytes = MergePdfs(pdfBytesList);

            // ── Subir el paquete a SharePoint y persistir en BD ──────────────────
            var pathData = await _projectSubContractorRepository.GetPathDataAsync(projectSubContractorId);

            // Abreviatura del proyecto: usa el campo Abbreviation; si está vacío, primeras 3 letras.
            var proyAbrev = !string.IsNullOrWhiteSpace(pathData.Abbreviation)
                ? pathData.Abbreviation
                : pathData.ProjectDescription[..Math.Min(3, pathData.ProjectDescription.Length)].ToUpperInvariant();

            // Abreviatura del contratista: primeras 4 letras alfanuméricas del nombre, en mayúsculas.
            var contratAbbrev = new string(
                pathData.ContributorName
                    .Where(char.IsLetterOrDigit)
                    .Take(4)
                    .ToArray()
            ).ToUpperInvariant();

            // Nombre: {AbrevProyecto}{NumContrato:D3}-{AbrevContratista4}
            // Si no hay número de contrato, usamos el ID de la adjudicación como fallback.
            string filePrefix;
            if (docs.ContractNumber.HasValue)
                filePrefix = $"{proyAbrev}{docs.ContractNumber.Value:D3}-{contratAbbrev}";
            else
                filePrefix = $"{proyAbrev}ADJ{projectSubContractorId:D4}-{contratAbbrev}";

            var fileName   = $"{filePrefix}.pdf";

            using var ms = new MemoryStream(mergedBytes);
            var spResult = await _oneDriveStorage.UploadAsync(
                pathData, AdjudicacionDocumentType.ContractPackage, fileName, ms, "application/pdf");

            var fileUrl = spResult.WebUrl!;

            await _projectSubContractorRepository.SaveDocumentAsync(
                projectSubContractorId, AdjudicacionDocumentType.ContractPackage, fileUrl, fileName, userId, spResult.ItemId);

            return (mergedBytes, fileUrl, fileName);
        }

        /// <summary>
        /// Lee un PDF de plantilla fijo desde Features/CostsModule/Features/AdjudicacionesFeature/Templates.
        /// Se usa para Causales de No Conformidad y Cuadro de Tolerancias (documentos estándar de Abril).
        /// </summary>
        private static byte[] ReadTemplatePdf(string fileName)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "Features", "CostsModule", "Features", "AdjudicacionesFeature",
                "Templates", fileName);

            if (!File.Exists(path))
                throw new AbrilException(
                    $"No se encontró la plantilla '{fileName}' en el servidor. Contacte al administrador del sistema.");

            return File.ReadAllBytes(path);
        }

        private static byte[] MergePdfs(List<byte[]> pdfBytesList)
        {
            var outputDoc = new PdfDocument();
            foreach (var pdfBytes in pdfBytesList)
            {
                using var inputStream = new MemoryStream(pdfBytes);
                var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
                foreach (var page in inputDoc.Pages)
                    outputDoc.AddPage(page);
            }
            using var resultStream = new MemoryStream();
            outputDoc.Save(resultStream, false);
            return resultStream.ToArray();
        }

        /// <summary>
        /// Marcadores que deben estar presentes en la plantilla del contrato (.docx) — en color
        /// blanco o tamaño 1pt para que no se vean — para indicar el punto de inserción de cada
        /// documento adjunto del paso 3 dentro del PDF final del contrato.
        /// </summary>
        private const string ContractQuotationMarker    = "<<INSERTAR_COTIZACION_AQUI>>";
        private const string ContractFichaTecnicaMarker = "<<INSERTAR_FICHA_TÉCNICA_AQUI>>";
        private const string ContractServiceOrderMarker = "<<INSERTAR_ORDEN_DE_SERVICIO_AQUI>>";
        private const string ContractScheduleMarker     = "<<INSERTAR_CRONOGRAMA_AQUI>>";

        /// <summary>
        /// Construye un PDF nuevo que es <paramref name="basePdf"/> con <paramref name="insertPdf"/>
        /// embutido justo después de la primera página de <paramref name="basePdf"/> que contenga
        /// <paramref name="markerText"/>. La página del marcador se conserva (sigue conteniendo
        /// el título del ANEXO 1). Si el marcador no aparece, se hace fallback concatenando
        /// <paramref name="insertPdf"/> al final.
        /// </summary>
        private static byte[] InsertPdfAfterMarker(byte[] basePdf, byte[] insertPdf, string markerText)
        {
            int? markerPageIndex = null;

            // PdfPig sirve solo para leer texto; no toca la estructura del PDF.
            using (var pigDoc = UglyToad.PdfPig.PdfDocument.Open(basePdf))
            {
                int idx = 0;
                foreach (var page in pigDoc.GetPages())
                {
                    if (page.Text.Contains(markerText, StringComparison.OrdinalIgnoreCase))
                    {
                        markerPageIndex = idx;
                        break;
                    }
                    idx++;
                }
            }

            var outputDoc = new PdfDocument();

            using var baseStream   = new MemoryStream(basePdf);
            using var insertStream = new MemoryStream(insertPdf);
            var baseDoc   = PdfReader.Open(baseStream,   PdfDocumentOpenMode.Import);
            var insertDoc = PdfReader.Open(insertStream, PdfDocumentOpenMode.Import);

            if (markerPageIndex.HasValue)
            {
                // 1) Páginas del contrato hasta la del marcador (incluida)
                for (int i = 0; i <= markerPageIndex.Value && i < baseDoc.PageCount; i++)
                    outputDoc.AddPage(baseDoc.Pages[i]);

                // 2) Páginas de la cotización
                foreach (var page in insertDoc.Pages)
                    outputDoc.AddPage(page);

                // 3) Páginas restantes del contrato
                for (int i = markerPageIndex.Value + 1; i < baseDoc.PageCount; i++)
                    outputDoc.AddPage(baseDoc.Pages[i]);
            }
            else
            {
                // Fallback: marcador ausente → contrato + cotización al final
                foreach (var page in baseDoc.Pages)
                    outputDoc.AddPage(page);
                foreach (var page in insertDoc.Pages)
                    outputDoc.AddPage(page);
            }

            using var resultStream = new MemoryStream();
            outputDoc.Save(resultStream, false);
            return resultStream.ToArray();
        }

        private static byte[] RotatePdfPages(byte[] pdfBytes)
        {
            using var inputStream = new MemoryStream(pdfBytes);
            using var outputStream = new MemoryStream();

            var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

            foreach (var page in document.Pages)
            {
                if (page.Width > page.Height)
                {
                    page.Rotate = 270;
                }
            }

            document.Save(outputStream);
            return outputStream.ToArray();
        }

        private static void BuildSummarySheet(IXLWorksheet ws, AdjudicacionSummarySheetDataDto data)
        {
            // ── Column widths ──────────────────────────────────────────────────
            ws.Column("A").Width = 2;
            ws.Column("B").Width = 28;
            ws.Column("C").Width = 15;
            ws.Column("D").Width = 12;
            ws.Column("E").Width = 17;
            ws.Column("F").Width = 18;
            ws.Column("G").Width = 16;
            ws.Column("H").Width = 11;
            ws.Column("I").Width = 17;
            ws.Column("J").Width = 17;
            ws.Column("K").Width = 12;
            ws.Column("L").Width = 12;
            ws.Column("M").Width = 24;
            ws.Column("N").Width = 2;

            // ── Row heights ────────────────────────────────────────────────────
            ws.Row(2).Height  = 32;
            ws.Row(12).Height = 42;
            ws.Row(13).Height = 38;
            ws.Row(14).Height = 18;
            ws.Row(15).Height = 20;

            // ── Calculations ──────────────────────────────────────────────────
            var advance = (data.AdvancePercentage ?? 0) / 100m * data.Amount;
            var saldo   = data.Amount - advance;
            var plazo   = (data.StartDate.HasValue && data.EndDate.HasValue)
                ? (int)(data.EndDate.Value.ToDateTime(TimeOnly.MinValue)
                      - data.StartDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays
                : 0;
            var currencySymbol = data.CurrencyCode == "USD" ? "US$" : "S/";

            // Los montos se escriben como TEXTO con separadores fijos (coma para miles, punto para
            // decimales), p. ej. "S/ 1,258,621.42" o "US$ 1,258,621.42". Se hace así porque Excel
            // SIEMPRE usa los separadores del locale del visor para celdas numéricas (ignora el locale
            // del formato), por lo que un formato numérico saldría como "1.258.621,42" en es-PE.
            // Escribirlo como texto garantiza el formato en cualquier visor o al convertir a PDF.
            // El cero se muestra como guion, estilo contable.
            var inv = CultureInfo.InvariantCulture;
            string FormatMoney(decimal v) =>
                v == 0m
                    ? $"{currencySymbol} -"
                    : $"{currencySymbol} {v.ToString("#,##0.00", inv)}";

            // Número de documento del contrato: {N°:D3}{ABREV}-{AÑO}, p. ej. "011MAX-2026".
            // Mismo formato que aparece en el N° DOC y en el título.
            var abrevProyecto = !string.IsNullOrWhiteSpace(data.Abbreviation)
                ? data.Abbreviation
                : (data.ProjectDescription.Length >= 3
                    ? data.ProjectDescription[..3].ToUpperInvariant()
                    : data.ProjectDescription.ToUpperInvariant());
            // El año se toma de la fecha de firma (definida en el paso 2), no del año actual.
            var anioContrato = data.SigningDate?.Year ?? DateTime.UtcNow.Year;
            var docNumber = data.ContractNumber.HasValue
                ? $"{data.ContractNumber.Value:D3}{abrevProyecto}-{anioContrato}"
                : data.ProjectSubContractorId.ToString("D4");

            // ── Row 2: Title ───────────────────────────────────────────────────
            ws.Range("B2:N2").Merge();
            ws.Cell("B2").Value = $"RESUMEN DEL CONTRATO N°{docNumber} " +
                                  $"{data.ContractTypeDescription.ToUpper()} POR EL SERVICIO DE " +
                                  $"{data.WorkItemDescription.ToUpper()}";
            ws.Range("B2:N2").Style.Font.Bold       = true;
            ws.Range("B2:N2").Style.Font.FontSize   = 11;
            ws.Range("B2:N2").Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
            ws.Range("B2:N2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range("B2:N2").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Range("B2:N2").Style.Alignment.WrapText   = true;
            ws.Range("B2:N2").Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // ── Rows 5–8: Info block ───────────────────────────────────────────
            ws.Cell("B5").Value = "Proyecto :";  ws.Cell("B5").Style.Font.Bold = true;
            ws.Cell("C5").Value = data.ProjectDescription;

            ws.Cell("B6").Value = "Contratista:"; ws.Cell("B6").Style.Font.Bold = true;
            ws.Cell("C6").Value = data.ContributorName;

            ws.Cell("B7").Value = "N° de niveles:"; ws.Cell("B7").Style.Font.Bold = true;
            if (!string.IsNullOrWhiteSpace(data.Niveles))
                ws.Cell("C7").Value = data.Niveles;

            ws.Cell("B8").Value = "Fecha:"; ws.Cell("B8").Style.Font.Bold = true;
            // Fecha como TEXTO (no valor de fecha): evita la indentación/derecha del formato fecha
            // y que el locale del visor cambie el formato (p. ej. "1/12/2026" en vez de "12/01/2026").
            if (data.SigningDate.HasValue)
                ws.Cell("C8").Value = data.SigningDate.Value.ToString("dd/MM/yyyy", inv);

            // ── Row 11: Sub-header ─────────────────────────────────────────────
            ws.Range("B11:D11").Merge();
            ws.Cell("B11").Value = $"EDIFICIO RESIDENCIAL \"{data.ProjectDescription.ToUpper()}\"";
            ws.Range("B11:D11").Style.Font.Bold      = true;
            ws.Range("B11:D11").Style.Font.FontColor = XLColor.FromHtml("#0070C0");
            ws.Range("B11:D11").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range("B11:D11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Range("K11:L11").Merge();
            ws.Cell("K11").Value = "PLAZO";
            ws.Range("K11:L11").Style.Font.Bold = true;
            ws.Range("K11:L11").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range("K11:L11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ── Row 12: Column headers ─────────────────────────────────────────
            void SetHeader(string address, string text)
            {
                var r = ws.Range(address);
                if (address.Contains(':')) r.Merge();
                r.Style.Font.Bold = true;
                r.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
                r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                r.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                r.Style.Alignment.WrapText   = true;
                r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                r.FirstCell().Value = text;
            }

            SetHeader("B12:C12", "DESCRIPCIÓN");
            SetHeader("D12:D12", "FECHA DE\nCONTRATO");
            SetHeader("E12:E12", "MONTO CONTRATADO\n(inc IGV)");
            SetHeader("F12:F12", "N° DOC");
            // La carta de fianza solo aplica en Suministro (ContractModalityId == 2) + contrato con
            // adelanto (PaymentMethodId == 2) y únicamente cuando se marcó el toggle IncludesCartaFianza.
            var conCartaFianza = (data.PaymentMethodId == 2 || data.PaymentMethodId == 4) && data.ContractModalityId == 2 && data.IncludesCartaFianza;
            // El encabezado es siempre "CHEQUE / RECIBO"; el detalle del documento de garantía
            // (pagaré, letra, carta de fianza) va en la celda de datos G13.
            SetHeader("G12:G12", "CHEQUE / RECIBO");
            SetHeader("H12:H12", "% ADELANTO");
            SetHeader("I12:I12", $"IMPORTE ADELANTO\n{currencySymbol}");
            SetHeader("J12:J12", "SALDO");
            SetHeader("K12:K12", "INICIO");
            SetHeader("L12:L12", "FIN");
            SetHeader("M12:M12", "OBSERVACION");

            // ── Rows 13–14: Data row ───────────────────────────────────────────
            ws.Range("B13:C14").Merge();
            ws.Cell("B13").Value = $"{data.WorkItemDescription.ToUpper()} (MONTO CONTRACTUAL)";
            ws.Range("B13:C14").Style.Font.Bold = true;
            ws.Range("B13:C14").Style.Alignment.WrapText   = true;
            ws.Range("B13:C14").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Range("B13:C14").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range("B13:C14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // D – Fecha contrato (texto)
            ws.Range("D13:D14").Merge();
            if (data.SigningDate.HasValue)
                ws.Cell("D13").Value = data.SigningDate.Value.ToString("dd/MM/yyyy", inv);
            ws.Cell("D13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("D13").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Range("D13:D14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // E – Monto contratado
            ws.Range("E13:E14").Merge();
            ws.Cell("E13").Value = FormatMoney(data.Amount);
            ws.Cell("E13").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Cell("E13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range("E13:E14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // F – N° DOC
            ws.Range("F13:F14").Merge();
            ws.Cell("F13").Value = $"CONTRATO N°{docNumber}";
            ws.Cell("F13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("F13").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Cell("F13").Style.Alignment.WrapText   = true;
            ws.Range("F13:F14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // G – Documentos de garantía: solo en contratos CON adelanto (PaymentMethodId == 2 o 4) se indica
            // "PAGARÉ N°{N°}{ABREV}-{AÑO} Y LETRA DE GARANTÍA", añadiendo la carta de fianza si aplica
            // (igual que el contrato).
            ws.Range("G13:G14").Merge();
            if (data.PaymentMethodId == 2 || data.PaymentMethodId == 4)
            {
                var numPagare = data.PromissoryNoteNumber.HasValue
                    ? data.PromissoryNoteNumber.Value.ToString("D3")
                    : "";
                var pagareRef = $"PAGARÉ N°{numPagare}{abrevProyecto}-{anioContrato}";
                ws.Cell("G13").Value = conCartaFianza
                    ? $"{pagareRef}, LETRA DE GARANTÍA Y CARTA DE FIANZA"
                    : $"{pagareRef} Y LETRA DE GARANTÍA";
            }
            ws.Cell("G13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("G13").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Cell("G13").Style.Alignment.WrapText   = true;
            ws.Range("G13:G14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // H – % Adelanto
            ws.Range("H13:H14").Merge();
            ws.Cell("H13").Value = $"{(data.AdvancePercentage ?? 0).ToString("0.00", inv)}%";
            ws.Cell("H13").Style.Font.Bold            = true;
            ws.Cell("H13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("H13").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Range("H13:H14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // I – Importe adelanto
            ws.Range("I13:I14").Merge();
            ws.Cell("I13").Value = FormatMoney(advance);
            ws.Cell("I13").Style.Alignment.Vertical  = XLAlignmentVerticalValues.Center;
            ws.Cell("I13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range("I13:I14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // J – Saldo
            ws.Range("J13:J14").Merge();
            ws.Cell("J13").Value = FormatMoney(saldo);
            ws.Cell("J13").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Cell("J13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range("J13:J14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // K – Inicio (texto)
            ws.Range("K13:K14").Merge();
            if (data.StartDate.HasValue)
                ws.Cell("K13").Value = data.StartDate.Value.ToString("dd/MM/yyyy", inv);
            ws.Cell("K13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("K13").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Range("K13:K14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // L – Fin (texto)
            ws.Range("L13:L14").Merge();
            if (data.EndDate.HasValue)
                ws.Cell("L13").Value = data.EndDate.Value.ToString("dd/MM/yyyy", inv);
            ws.Cell("L13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("L13").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Range("L13:L14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // M – Observación
            ws.Range("M13:M14").Merge();
            ws.Cell("M13").Value = data.PaymentMethodDescription;
            ws.Cell("M13").Style.Alignment.WrapText = true;
            ws.Cell("M13").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range("M13:M14").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ── Row 15: Totales ────────────────────────────────────────────────
            ws.Range("B15:D15").Merge();
            ws.Cell("B15").Value = "MONTO TOTAL CONTRATADO";
            ws.Range("B15:D15").Style.Font.Bold         = true;
            ws.Range("B15:D15").Style.Font.Underline    = XLFontUnderlineValues.Single;
            ws.Range("B15:D15").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range("B15:D15").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Range("B15:D15").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell("E15").Value = FormatMoney(data.Amount);
            ws.Cell("E15").Style.Font.Bold            = true;
            ws.Cell("E15").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Cell("E15").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell("E15").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            foreach (var col in new[] { "F", "G", "H", "I", "J" })
                ws.Cell($"{col}15").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Range("K15:L15").Merge();
            if (plazo > 0) ws.Cell("K15").Value = $"{plazo} días";
            ws.Range("K15:L15").Style.Font.Bold = true;
            ws.Range("K15:L15").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range("K15:L15").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Range("K15:L15").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Cell("M15").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ── Rows 17–18: Pie de garantías ──────────────────────────────────
            // Valores registrados en el paso 2 (con fallback a los anteriores por defecto)
            var fondoPorc = data.GuaranteeFundPercentage ?? 5;
            var fondoDias = data.GuaranteeFundDays ?? 360;

            ws.Cell("B17").Value = "% DE RETENCIÓN FONDO DE GARANTIA:";
            ws.Cell("B17").Style.Font.Bold = true;
            ws.Cell("D17").Value = $"{fondoPorc}%";

            ws.Cell("B18").Value = "DEVOLUCIÓN DE FONDO DE GARANTÍA";
            ws.Cell("B18").Style.Font.Bold = true;
            ws.Range("D18:M18").Merge();
            ws.Cell("D18").Value =
                $"{fondoDias} días después de entregada la obra con acta Recepción Definitiva suscrita por el contratante y el cliente";
            ws.Range("D18:M18").Style.Alignment.WrapText = true;

            // ── Borde exterior general ─────────────────────────────────────────
            ws.Range("B2:N18").Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0);          // 1 página de ancho, alto libre
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.Margins.Left = 0.5;
            ws.PageSetup.Margins.Right = 0.5;
            ws.PageSetup.Margins.Top = 0.5;
            ws.PageSetup.Margins.Bottom = 0.5;
        }

        private async Task<List<MailAttachmentDto>> DownloadAttachmentsAsync(
            List<ProjectSubContractorFileDto> files)
        {
            if (files == null || files.Count == 0)
                return new List<MailAttachmentDto>();

            var downloadTasks = files.Select(async file =>
            {
                try
                {
                    var bytes = await _oneDriveStorage.DownloadByWebUrlAsync(file.FileUrl);
                    var fileName = !string.IsNullOrWhiteSpace(file.OriginalFileName)
                        ? file.OriginalFileName
                        : Path.GetFileName(new Uri(file.FileUrl).LocalPath);

                    return new MailAttachmentDto
                    {
                        FileName = fileName,
                        ContentType = "application/octet-stream",
                        Content = bytes
                    };
                }
                catch
                {
                    return null;
                }
            });

            var results = await Task.WhenAll(downloadTasks);
            return results.Where(r => r is not null).Cast<MailAttachmentDto>().ToList();
        }
    }
}
