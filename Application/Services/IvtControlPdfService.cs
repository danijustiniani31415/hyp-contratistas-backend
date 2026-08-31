using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Application.Interfaces;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;

namespace Abril_Backend.Application.Services
{
    public class IvtControlPdfService : IIvtControlPdfService
    {
        private readonly IIvtControlPdfRepository _repository;
        private readonly IStorageContainerResolver _containerResolver;
        private readonly IFileStorageService _fileStorageService;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProjectResidentRepository _projecResidentRepository;
        public IvtControlPdfService(
            IIvtControlPdfRepository repository, 
            IStorageContainerResolver containerResolver, 
            IFileStorageService fileStorageService, 
            IProjectRepository projectRepository, 
            IUserRepository userRepository, 
            IProjectResidentRepository projectResidentRepository)
        {
            _containerResolver = containerResolver;
            _repository = repository;
            _fileStorageService = fileStorageService;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _projecResidentRepository = projectResidentRepository;
        }

        public async Task<bool> Create(IvtControlPdfCreateDTO dto, int userId)
        {
            if (dto.Pdfs == null || !dto.Pdfs.Any())
                throw new AbrilException("No se pusieron archivos.");

            if (dto.Pdfs.Count() > 4)
                throw new AbrilException("Máximo 4 archivos por subida");

            var existingCount = await _repository.CountByScheduleAndPeriod(
                dto.ProjectId,
                dto.PeriodDate
            );

            if (existingCount + dto.Pdfs.Count() > 4)
                throw new AbrilException("Máximo 4 archivos por mes por proyecto. Archivos subidos: " + existingCount);

            var projectName = await _projectRepository.GetProjectNameByProjectId(dto.ProjectId);

            if (string.IsNullOrWhiteSpace(projectName))
                throw new Exception("Project name not found.");

            projectName = projectName
                .Trim()
                .ToUpper()
                .Replace(" ", "")
                .Replace("-", "");

            var container = _containerResolver.GetIvtContainerName();

            var filesToUpload = new List<(Stream Stream, string FileName)>();
            var streams = new List<Stream>();
            var originalFileNames = new List<string>();

            int counter = existingCount;

            foreach (var item in dto.Pdfs)
            {
                if (item.Length == 0)
                    throw new Exception("Empty file detected.");

                counter++;

                var extension = Path.GetExtension(item.FileName);

                var fileName = $"IVT-{dto.PeriodDate.Year}-{dto.PeriodDate.Month:D2}-{projectName}-{counter}{extension}";

                var stream = item.OpenReadStream();

                streams.Add(stream);
                filesToUpload.Add((stream, fileName));
                originalFileNames.Add(fileName);
            }

            List<string> uploadedUrls;

            try
            {
                uploadedUrls = await _fileStorageService.UploadFilesAsync(filesToUpload, container);

                await _repository.Create(dto, uploadedUrls, userId, originalFileNames);
            }
            finally
            {
                foreach (var stream in streams)
                    stream.Dispose();
            }

            return true;
        }

        public async Task<PagedResult<IvtControlPdfGetDTO>> GetPaged(int page, DateOnly? periodDate, int? userId)
        {
            var resultado = await _repository.GetPaged(page, periodDate, userId);
            return resultado;
        }

        public async Task<IvtControlPdfFiltersDTO> GetFiltersData()
        {
            var residents = await _userRepository.GetResidentsFullName();
            var projects = await _projecResidentRepository.GetProjectsDescription();
            var periods = await _repository.GetIvtControlPeriods();

            var response = new IvtControlPdfFiltersDTO
            {
                Projects = projects,
                Residents = residents,
                Periods = periods,
            };

            return response;
        }
    }
}