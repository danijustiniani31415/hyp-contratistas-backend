using System.Security.Cryptography;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos;
using Abril_Backend.Features.Contractors.ContractorManagement.Application.Interfaces;
using Abril_Backend.Features.Contractors.ContractorManagement.Application;
using Abril_Backend.Features.Contractors.ContractorManagement.Infrastructure.Interfaces;
using Abril_Backend.Features.ContractorsModule.Shared;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.Contractors.ContractorManagement.Application.Services
{
    public class ContractorManagementService : IContractorManagementService
    {
        private const int ApprovedContractorStateId = 2;

        private readonly IContractorManagementRepository _repository;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;
        private readonly IGraphSharePointService _sharePointService;
        private readonly IConfiguration _configuration;
        private readonly SharePointSiteRef _site;

        public ContractorManagementService(
            IContractorManagementRepository repository,
            IEmailService emailService,
            IOptions<FrontendSettings> frontendSettings,
            IGraphSharePointService sharePointService,
            IConfiguration configuration)
        {
            _repository = repository;
            _emailService = emailService;
            _frontendSettings = frontendSettings.Value;
            _sharePointService = sharePointService;
            _configuration = configuration;
            _site = SharePointSiteRef.FromConfig(configuration, "CostosYPresupuestos");
        }

        public async Task<PagedResult<ContributorPagedDto>> GetPaged(ContributorFilterDto filter)
        {
            return await _repository.GetPaged(filter);
        }

        public async Task Approve(int contractorId, int userId)
        {
            // Solo se aprueba al contratista. El envío de credenciales se hace manualmente
            // desde el botón "Enviar credenciales" (SendCredentials), no automáticamente al aprobar.
            await _repository.Approve(contractorId, userId);
        }

        public async Task Reject(int contractorId, int userId)
        {
            await _repository.Reject(contractorId, userId);
        }

        public async Task ApproveUpdate(int contractorId, int userId)
        {
            await _repository.ApproveUpdate(contractorId, userId);
        }

        public async Task RejectUpdate(int contractorId, int userId)
        {
            await _repository.RejectUpdate(contractorId, userId);
        }

        public async Task SendCredentials(int contractorId, int adminUserId)
        {
            var contractor = await _repository.GetWithEmails(contractorId);
            if (contractor == null)
                throw new AbrilException("Contratista no encontrado.", 404);

            if (contractor.ContractorStateId != ApprovedContractorStateId)
                throw new AbrilException("Solo se pueden enviar credenciales a contratistas aprobados.", 400);

            if (contractor.Emails.Count == 0)
                throw new AbrilException("El contratista no tiene correos de contacto activos a los cuales enviar las credenciales.", 400);

            var token = GenerateToken();
            var expiry = DateTime.UtcNow.AddHours(24);
            await _repository.SetActivationToken(contractorId, token, expiry);

            var link = $"{_frontendSettings.ContractorCredentialsUrl}?token={token}";

            var body = $@"
                <p>Estimado representante de <strong>{contractor.ContributorName}</strong>,</p>
                <p>Su empresa ha sido aprobada en el proceso de homologación de contratistas de <strong>Abril Grupo Inmobiliario</strong>.</p>
                <p>Para activar su acceso al sistema, haga clic en el siguiente enlace y registre sus credenciales:</p>
                <p>
                    <a href='{link}' target='_blank'
                    style='display:inline-block; padding:10px 20px; background-color:#64BC04; color:#ffffff; text-decoration:none; border-radius:6px; font-weight:bold;'>
                        Registrar credenciales
                    </a>
                </p>
                <p style='font-size: 12px; color: #666;'>
                    Este enlace expirará en 24 horas. Si no solicitó este acceso, puede ignorar este correo.
                </p>
            ";

            await _emailService.SendAsync(
                to: contractor.Emails,
                subject: "Activa tu cuenta de contratista — Abril Grupo Inmobiliario",
                body: body,
                isHtml: true
            );
        }

        public async Task Update(int contractorId, ContractorUpdateDto dto, int userId)
        {
            // Validar formato de los correos (solo letras, números, '@' y '.').
            var incomingEmails = ContractorEmailParser.Parse(dto.EmailsJson);
            ContractorEmailValidator.ValidateOrThrow(incomingEmails.Select(e => e.Email));

            var existing = await _repository.GetWithEmails(contractorId)
                ?? throw new AbrilException("Contratista no encontrado.", 404);

            string? logoUrl       = null;
            string? brochureUrl   = null;
            string? fichaRucUrl   = null;
            string? referencesUrl = null;

            var listId = _configuration["SharePoint:Sites:CostosYPresupuestos:ContractorLibraryId"]
                ?? throw new InvalidOperationException("SharePoint:Sites:CostosYPresupuestos:ContractorLibraryId no está configurado.");

            // La carpeta sigue siendo {ruc} - {nombre_original} para no romper archivos previos
            var folderPath = Sanitize($"{existing.ContributorRuc} - {existing.ContributorName}");

            if (dto.LogoFile is not null)
                logoUrl = await UploadFile(listId, folderPath, "logo", dto.LogoFile);

            if (dto.BrochureFile is not null)
                brochureUrl = await UploadFile(listId, folderPath, "brochure", dto.BrochureFile);

            if (dto.FichaRucFile is not null)
                fichaRucUrl = await UploadFile(listId, folderPath, "ficha_ruc", dto.FichaRucFile);

            if (dto.ReferencesListFile is not null)
                referencesUrl = await UploadFile(listId, folderPath, "lista_referencias", dto.ReferencesListFile);

            await _repository.Update(contractorId, dto, logoUrl, brochureUrl, fichaRucUrl, referencesUrl, userId);
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

        private static string Sanitize(string name)
        {
            var invalid = new HashSet<char> { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '#', '%' };
            var result  = string.Concat(name.Select(c => invalid.Contains(c) ? '-' : c)).Trim();
            return result.Length > 60 ? result[..60].TrimEnd() : result;
        }

        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
