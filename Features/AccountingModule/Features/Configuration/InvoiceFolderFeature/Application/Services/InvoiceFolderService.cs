using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Dtos;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Interfaces;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;

namespace Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Services
{
    public class InvoiceFolderService : IInvoiceFolderService
    {
        private readonly IInvoiceFolderRepository _repository;
        private readonly IGraphSharePointService _sharePointService;
        private readonly string[] _allowedHosts;

        public InvoiceFolderService(
            IInvoiceFolderRepository repository,
            IGraphSharePointService sharePointService,
            IConfiguration configuration)
        {
            _repository = repository;
            _sharePointService = sharePointService;

            // Hosts permitidos del tenant. Se derivan del sitio ya configurado (mismo tenant
            // para toda la organización): "abrilinmob.sharepoint.com" → tenant "abrilinmob"
            // → se aceptan el de sitios y el de OneDrive personal "-my".
            var siteHost = SharePointSiteRef.FromConfig(configuration, "CostosYPresupuestos").Hostname.ToLowerInvariant();
            var tenant = siteHost.Split('.')[0].Replace("-my", "");
            _allowedHosts = new[] { $"{tenant}.sharepoint.com", $"{tenant}-my.sharepoint.com" };
        }

        public Task<InvoiceFolderDto?> GetSingleton() => _repository.GetSingleton();

        public async Task<InvoiceFolderDto> Save(InvoiceFolderSaveDto dto, int userId)
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

            await _repository.Upsert(link, resolved.DriveId, resolved.ItemId, resolved.Name, resolved.WebUrl, userId);

            return await _repository.GetSingleton()
                ?? throw new AbrilException("No se pudo guardar la carpeta.", 500);
        }
    }
}
