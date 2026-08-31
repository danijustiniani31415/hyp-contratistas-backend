using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Dtos;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Services
{
    public class CroquisService : ICroquisService
    {
        private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".webp" };
        private const long MaxBytes = 15 * 1024 * 1024; // 15 MB

        private readonly ICroquisRepository _repository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IStorageContainerResolver _containerResolver;

        public CroquisService(
            ICroquisRepository repository,
            IFileStorageService fileStorageService,
            IStorageContainerResolver containerResolver)
        {
            _repository = repository;
            _fileStorageService = fileStorageService;
            _containerResolver = containerResolver;
        }

        public Task<List<ProjectCroquisItemDto>> GetProjectsWithCroquis(string? search)
            => _repository.GetProjectsWithCroquis(search);

        public async Task<string> AssignCroquis(int projectId, IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new AbrilException("No se adjuntó ninguna imagen.");

            if (file.Length > MaxBytes)
                throw new AbrilException("La imagen supera el tamaño máximo permitido (15 MB).");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new AbrilException("Formato no válido. Use PNG, JPG o WEBP.");

            var container = _containerResolver.GetProjectCroquisContainerName();

            string imageUrl;
            using (var stream = file.OpenReadStream())
            {
                var uploaded = await _fileStorageService.UploadFilesAsync(
                    new[] { (stream, $"{Guid.NewGuid()}{extension}") },
                    container);
                imageUrl = uploaded.First();
            }

            await _repository.UpsertCroquis(projectId, imageUrl, file.FileName, userId);
            return imageUrl;
        }

        public Task<List<CroquisLoteDto>> GetLotes(int projectCroquisId)
            => _repository.GetLotes(projectCroquisId);

        public Task SaveLotes(int projectCroquisId, List<CroquisLoteDto> lotes, int userId)
            => _repository.ReplaceLotes(projectCroquisId, lotes ?? new(), userId);

        public Task<CroquisGestionResponseDto> GetGestion()
            => _repository.GetGestion();
    }
}
