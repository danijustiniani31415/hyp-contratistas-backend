using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Services
{
    public class ProjectsService : IProjectsService
    {
        private readonly IProjectsRepository _repository;
        private readonly IGraphSharePointService _sharePoint;
        private readonly SharePointSiteRef _site;

        private const string Library   = "Fotos de Proyectos";
        private const string FolderPath = "fotos-proyectos";

        public ProjectsService(
            IProjectsRepository repository,
            IGraphSharePointService sharePoint,
            IConfiguration configuration)
        {
            _repository = repository;
            _sharePoint = sharePoint;
            _site = SharePointSiteRef.FromConfig(configuration, "ProyectosAbril");
        }

        public Task<PagedResult<ProjectDTO>> GetPagedWithResidents(int page, int pageSize = 10, string? search = null)
            => _repository.GetPagedWithResidents(page, pageSize, search);

        public async Task<string> UploadFotoAsync(int projectId, IFormFile foto)
        {
            var extension   = Path.GetExtension(foto.FileName).TrimStart('.');
            var fileName    = $"proyecto-{projectId}.{extension}";
            var contentType = foto.ContentType ?? "application/octet-stream";

            using var stream = foto.OpenReadStream();
            var result = await _sharePoint.UploadToSharePointLibraryAsync(
                site:        _site,
                libraryName: Library,
                folderPath:  FolderPath,
                fileName:    fileName,
                fileStream:  stream,
                contentType: contentType);

            var fotoUrl = result?.WebUrl
                ?? throw new InvalidOperationException("SharePoint no devolvió una URL para la foto.");

            await _repository.UpdateFotoUrlAsync(projectId, fotoUrl);
            return fotoUrl;
        }
    }
}
