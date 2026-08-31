using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Contractors.ContractorRegistration.Application.Dtos;
using Abril_Backend.Features.Contractors.ContractorRegistration.Application.Interfaces;
using Abril_Backend.Features.Contractors.ContractorRegistration.Infrastructure.Interfaces;
using Abril_Backend.Features.ContractorsModule.Shared;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;
using Abril_Backend.Shared.Services.Sunat.Dtos;
using Abril_Backend.Shared.Services.Sunat.Interfaces;
using System.Text;

namespace Abril_Backend.Features.Contractors.ContractorRegistration.Application.Services
{
    public class ContractorRegistrationService : IContractorRegistrationService
    {
        private const int ApprovedContractorStateId      = 2;
        private const int PendingUpdateContractorStateId  = 4;

        private readonly IContractorRegistrationRepository _repository;
        private readonly ISunatService _sunatService;
        private readonly IGraphSharePointService _sharePointService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ICostosPresupuestosEmailService _costosPresupuestosEmailService;
        private readonly SharePointSiteRef _site;

        public ContractorRegistrationService(
            IContractorRegistrationRepository repository,
            ISunatService sunatService,
            IGraphSharePointService sharePointService,
            IConfiguration configuration,
            IEmailService emailService,
            ICostosPresupuestosEmailService costosPresupuestosEmailService)
        {
            _repository = repository;
            _sunatService = sunatService;
            _sharePointService = sharePointService;
            _configuration = configuration;
            _emailService = emailService;
            _costosPresupuestosEmailService = costosPresupuestosEmailService;
            _site = SharePointSiteRef.FromConfig(configuration, "CostosYPresupuestos");
        }

        public async Task<List<ContractorPersonTypeDto>> GetPersonTypes()
        {
            return await _repository.GetPersonTypes();
        }

        public Task<ContractorRucStatusDto> GetRucStatus(string ruc) => _repository.GetRucStatusAsync(ruc);

        public async Task Create(ContributorCreateDto dto, int? userId, string? accessToken = null)
        {
            // 0. Validar formato de los correos (solo letras, números, '@' y '.').
            ContractorEmailValidator.ValidateOrThrow(dto.ContributorEmails);

            // La obligatoriedad del logo es una regla de RUTA (pública = obligatorio,
            // interna = opcional) y se valida solo en el frontend, igual que el resto de
            // campos "obligatorios" (razón social, brochure, ficha RUC, referencias). No se
            // valida aquí porque el backend no distingue la ruta, solo si hay token: un
            // contratista logueado en la ruta pública tendría userId y se saltaría la regla.

            // 1. Determinar el estado del RUC para decidir la rama del flujo.
            var status = await _repository.GetRucStatusAsync(dto.ContributorRuc);

            // ── Caso A: RUC nuevo → registro normal ──────────────────────────────
            if (!status.Exists)
            {
                var (logo, bro, ficha, refs) = await UploadFiles(dto, "Solicitud 1");
                await _repository.CreateNew(dto, userId, logo, bro, ficha, refs);
                await SendNewContractorNotificationAsync(dto, isUpdate: false);
                return;
            }

            // ── El RUC ya existe: solo se permite avanzar como solicitud de actualización ──
            if (!dto.IsUpdateRequest)
                throw new AbrilException(
                    "Ya existe un contratista registrado con este RUC. Si deseas actualizar sus datos, " +
                    "confirma el envío de una solicitud de actualización.", 409);

            var stateId = status.ActiveContractorStateId;

            // ── Caso B: contratista APROBADO → staging (no se tocan los datos vigentes) ──
            if (stateId == ApprovedContractorStateId)
            {
                var (logo, bro, ficha, refs) = await UploadFiles(dto, $"Actualización {status.UpdateRequestCount + 1}");
                await _repository.CreateUpdateRequest(status.ActiveContractorId!.Value, dto, userId, logo, bro, ficha, refs);
                await SendNewContractorNotificationAsync(dto, isUpdate: true);
                return;
            }

            // ── Ya hay una actualización pendiente de revisión ───────────────────
            if (stateId == PendingUpdateContractorStateId)
                throw new AbrilException(
                    "Ya existe una solicitud de actualización pendiente de revisión para este contratista. " +
                    "Por favor espera a que el área de costos la procese.", 409);

            // ── Caso C: PENDIENTE (1) o solo RECHAZADOS → sobrescritura directa ──
            {
                var (logo, bro, ficha, refs) = await UploadFiles(dto, $"Solicitud {status.ContractorCount + 1}");
                await _repository.OverwriteOrCreateDirect(status.ContributorId!.Value, status.ActiveContractorId, dto, userId, logo, bro, ficha, refs);
                await SendNewContractorNotificationAsync(dto, isUpdate: true);
            }
        }

        /// <summary>Sube los archivos presentes en el dto a la subcarpeta indicada y devuelve sus URLs.</summary>
        private async Task<(string? logo, string? brochure, string? fichaRuc, string? references)> UploadFiles(ContributorCreateDto dto, string subfolder)
        {
            string? logoUrl       = null;
            string? brochureUrl   = null;
            string? fichaRucUrl   = null;
            string? referencesUrl = null;

            if (dto.LogoFile is not null || dto.BrochureFile is not null || dto.FichaRucFile is not null || dto.ReferencesListFile is not null)
            {
                var listId          = _configuration["SharePoint:Sites:CostosYPresupuestos:ContractorLibraryId"]
                                      ?? throw new InvalidOperationException("SharePoint:Sites:CostosYPresupuestos:ContractorLibraryId no está configurado.");
                var desiredFolder   = Sanitize($"{dto.ContributorRuc} - {dto.ContributorName}");

                // Buscar carpeta existente para este RUC (independientemente de si la razón social cambió).
                // · Si existe y el nombre coincide → se reutiliza tal cual.
                // · Si existe pero el nombre cambió  → se renombra a la razón social actual.
                // · Si no existe                    → Graph la crea automáticamente al subir el primer archivo.
                var existing = await _sharePointService.FindContractorFolderAsync(_site, listId, dto.ContributorRuc);
                if (existing is not null
                    && !string.Equals(existing.Value.Name, desiredFolder, StringComparison.OrdinalIgnoreCase))
                {
                    await _sharePointService.RenameFolderInLibraryAsync(_site, listId, existing.Value.Id, desiredFolder);
                }

                var folderPath = $"{desiredFolder}/{subfolder}";

                if (dto.LogoFile is not null)
                    logoUrl = await UploadFile(listId, folderPath, "logo", dto.LogoFile);

                if (dto.BrochureFile is not null)
                    brochureUrl = await UploadFile(listId, folderPath, "brochure", dto.BrochureFile);

                if (dto.FichaRucFile is not null)
                    fichaRucUrl = await UploadFile(listId, folderPath, "ficha_ruc", dto.FichaRucFile);

                if (dto.ReferencesListFile is not null)
                    referencesUrl = await UploadFile(listId, folderPath, "lista_referencias", dto.ReferencesListFile);
            }

            return (logoUrl, brochureUrl, fichaRucUrl, referencesUrl);
        }

        private async Task SendNewContractorNotificationAsync(ContributorCreateDto dto, bool isUpdate)
        {
            // El correo a Costos y Presupuestos solo se envía cuando el registro viene de la
            // ruta interna /contractors/registro-interno. La ruta pública /contractors/registro
            // no notifica. Es una regla de RUTA informada por el frontend (ver IsInternalRegistration
            // en el dto): el backend no puede distinguir la ruta por el token.
            if (!dto.IsInternalRegistration)
                return;

            var subject = isUpdate
                ? $"Solicitud de actualización de datos de contratista: {dto.ContributorName}"
                : $"Nuevo contratista registrado para revisión: {dto.ContributorName}";

            var body = new StringBuilder();
            body.AppendLine("<p>Estimado equipo de Costos y Presupuestos,</p>");
            body.AppendLine(isUpdate
                ? "<p>Un contratista ya registrado ha enviado una <strong>solicitud de actualización de datos</strong> que requiere revisión. A continuación se detallan los datos propuestos:</p>"
                : "<p>Se ha registrado un nuevo contratista en el sistema y se encuentra pendiente de revisión. A continuación se detallan los datos registrados:</p>");
            body.AppendLine("<ul>");
            body.AppendLine($"  <li><strong>Razón social:</strong> {dto.ContributorName}</li>");
            body.AppendLine($"  <li><strong>RUC:</strong> {dto.ContributorRuc}</li>");
            if (!string.IsNullOrWhiteSpace(dto.ContributorAddress))
                body.AppendLine($"  <li><strong>Dirección:</strong> {dto.ContributorAddress}</li>");
            if (!string.IsNullOrWhiteSpace(dto.ContributorDistrict))
                body.AppendLine($"  <li><strong>Distrito:</strong> {dto.ContributorDistrict}</li>");
            if (!string.IsNullOrWhiteSpace(dto.ContributorProvince))
                body.AppendLine($"  <li><strong>Provincia:</strong> {dto.ContributorProvince}</li>");
            if (!string.IsNullOrWhiteSpace(dto.ContributorDepartment))
                body.AppendLine($"  <li><strong>Departamento:</strong> {dto.ContributorDepartment}</li>");
            if (!string.IsNullOrWhiteSpace(dto.LegalRepresentativeFullName))
                body.AppendLine($"  <li><strong>Representante legal:</strong> {dto.LegalRepresentativeFullName}</li>");
            if (!string.IsNullOrWhiteSpace(dto.LegalRepresentativeDni))
                body.AppendLine($"  <li><strong>DNI del representante:</strong> {dto.LegalRepresentativeDni}</li>");
            body.AppendLine("</ul>");
            body.AppendLine("<p>Por favor, acceda al sistema para completar la revisión del contratista.</p>");

            var costosEmails = await _costosPresupuestosEmailService.GetActiveEmails();
            await _emailService.SendAsync(
                to:     costosEmails,
                subject: subject,
                body:    body.ToString(),
                isHtml:  true);
        }

        public async Task<SunatContributorDto?> GetByRuc(string ruc)
        {
            return await _sunatService.GetByRucAsync(ruc);
        }

        private async Task<string?> UploadFile(string listId, string folderPath, string baseName, IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);
            var fileName  = $"{baseName}{extension}";

            using var stream = file.OpenReadStream();
            var result = await _sharePointService.UploadToSharePointLibraryAsync(
                site:        _site,
                libraryName: listId,
                folderPath:  folderPath,
                fileName:    fileName,
                fileStream:  stream,
                contentType: file.ContentType);
            return result?.WebUrl;
        }

        /// <summary>Elimina caracteres no permitidos en nombres de carpeta de SharePoint.</summary>
        private static string Sanitize(string name)
        {
            var invalid = new HashSet<char> { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '#', '%' };
            var result  = string.Concat(name.Select(c => invalid.Contains(c) ? '-' : c)).Trim();
            if (result.Length > 60) result = result[..60];
            // SharePoint no permite nombres que terminen en punto o espacio (las razones
            // sociales suelen terminar en "S.A.C.", "E.I.R.L.", etc.).
            return result.TrimEnd(' ', '.');
        }
    }
}
