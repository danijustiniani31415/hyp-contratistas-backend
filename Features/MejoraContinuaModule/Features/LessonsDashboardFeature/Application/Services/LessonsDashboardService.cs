using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Services
{
    public class LessonsDashboardService : ILessonsDashboardService
    {
        private readonly ILessonsDashboardRepository _repository;
        private readonly IEmailService _emailService;

        public LessonsDashboardService(ILessonsDashboardRepository repository, IEmailService emailService)
        {
            _repository = repository;
            _emailService = emailService;
        }

        public Task<LessonsDashboardDataDTO> GetData(DateTimeOffset? periodDate, int? userId, List<int>? lessonAreaIds, int? obraOficinaStaffId, List<int>? projectIds, string? approvalStatus)
            => _repository.GetDataAsync(periodDate, userId, lessonAreaIds, obraOficinaStaffId, projectIds, approvalStatus);

        public Task<LessonsDashboardFiltersDTO> GetFilters() => _repository.GetFiltersAsync();

        public async Task SendPdfAsync(IFormFile pdf)
        {
            if (pdf == null || pdf.Length == 0)
                throw new AbrilException("Debe adjuntar un PDF");
            if (pdf.ContentType != "application/pdf")
                throw new AbrilException("El archivo debe ser un PDF");

            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await pdf.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            await _emailService.SendAsync(
                to: new List<string> { "calvarez@abril.pe" },
                subject: "Dashboard de Lecciones Aprendidas",
                body: "Adjunto PDF del dashboard de lecciones aprendidas.",
                isHtml: false,
                attachments: new List<EmailAttachment>
                {
                    new EmailAttachment
                    {
                        FileName = pdf.FileName,
                        ContentType = pdf.ContentType,
                        Content = fileBytes
                    }
                });
        }
    }
}
