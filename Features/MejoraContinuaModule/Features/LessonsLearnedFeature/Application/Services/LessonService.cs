using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Helpers;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Services
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly IEmailService _emailService;
        private readonly string _platformUrl;

        // Mensaje único de bloqueo de subida (lo usan tanto el rechazo del POST como
        // el endpoint que consulta el estado de la ventana para el frontend).
        private const string ReviewWindowMessage =
            "Estamos en la ventana de revisión de la jefatura (los últimos 2 días hábiles del mes). " +
            "Durante estos días no se pueden registrar nuevas lecciones aprendidas.";

        public LessonService(ILessonRepository lessonRepository, IEmailService emailService, IConfiguration configuration)
        {
            _lessonRepository = lessonRepository;
            _emailService = emailService;
            _platformUrl = $"{configuration["App:FrontendUrl"]?.TrimEnd('/')}/auth/login";
        }

        public Task<LessonDetailDTO?> GetByIdAsync(int id, int currentUserId)
            => _lessonRepository.GetByIdAsync(id, currentUserId);

        public Task<PagedResult<LessonListDTO>> GetLessonsFilterPaged(
            DateTimeOffset? periodDate, int? stateId, int? projectId,
            int? areaId, int? userId, int page, int pageSize)
            => _lessonRepository.GetLessonsFilterPaged(periodDate, stateId, projectId, areaId, userId, page, pageSize);

        public Task<LessonsPagedWithFiltersDTO> GetPagedWithFilters(LessonFilterDTO filter)
        {
            if (filter.Page < 1) filter.Page = 1;
            return _lessonRepository.GetPagedWithFiltersAsync(filter);
        }

        public Task<List<LessonListDTO>> GetLessonsFilterAsync(
            string? period, int? stateId, int? projectId, int? areaId, int? userId,
            List<int>? lessonAreaIds = null, int? obraOficinaStaffId = null)
            => _lessonRepository.GetLessonsFilterAsync(period, stateId, projectId, areaId, userId, lessonAreaIds, obraOficinaStaffId);

        public async Task<object?> CreateAsync(LessonCreateDTO dto, int userId)
        {
            // ── Fecha "hoy" en hora Lima (UTC-5) ─────────────────────────────
            // Para PROBAR la ventana de revisión: comenta la línea de hoy y
            // descomenta la de fecha fija con un 4.º/5.º día hábil final del mes.
            var today = DateTime.UtcNow.AddHours(-5);
            //var today = new DateTime(2026, 6, 29); // 4.º día hábil de jun-2026 → debe BLOQUEAR
            // var today = new DateTime(2026, 6, 30); // 5.º día hábil de jun-2026 → debe BLOQUEAR

            // Días 4–5 de los últimos 5 hábiles (incluidos los fines de semana/feriados
            // intermedios) = ventana de revisión de la jefatura: nadie puede registrar
            // nuevas lecciones aprendidas. Los feriados se descartan como días hábiles.
            var holidays = await _lessonRepository.GetHolidayDatesAsync(today.Year, today.Month);
            if (LessonUploadWindow.IsReviewWindow(today, holidays))
                throw new AbrilException(ReviewWindowMessage, 403);

            return await _lessonRepository.CreateAsync(dto, userId);
        }

        public async Task<LessonUploadWindowDTO> GetUploadWindowAsync()
        {
            var today = DateTime.UtcNow.AddHours(-5);
            //var today = new DateTime(2026, 6, 29); // 4.º día hábil de jun-2026 → debe BLOQUEAR

            var holidays = await _lessonRepository.GetHolidayDatesAsync(today.Year, today.Month);
            var (start, end) = LessonUploadWindow.ReviewWindowRange(today, holidays);
            var isReview = LessonUploadWindow.IsReviewWindow(today, holidays);

            return new LessonUploadWindowDTO
            {
                CanUpload = !isReview,
                IsReviewWindow = isReview,
                ReviewStart = start,
                ReviewEnd = end,
                Message = isReview ? ReviewWindowMessage : null,
            };
        }

        public Task<bool> UpdateAsync(int lessonId, LessonUpdateDTO dto, int currentUserId)
            => _lessonRepository.UpdateAsync(lessonId, dto, currentUserId);

        public Task<bool> DeleteSoftAsync(int lessonId, int userId)
            => _lessonRepository.DeleteSoftAsync(lessonId, userId);

        public async Task ApproveAsync(int lessonId, int currentUserId)
        {
            var result = await _lessonRepository.ApproveAsync(lessonId, currentUserId);
            await NotifyAuthorAsync(result, approved: true, comment: null);
        }

        public async Task RejectAsync(int lessonId, int currentUserId, string? comment)
        {
            var result = await _lessonRepository.RejectAsync(lessonId, currentUserId, comment);
            await NotifyAuthorAsync(result, approved: false, comment: comment);
        }

        // ── Correos al autor ─────────────────────────────────────────────────

        private async Task NotifyAuthorAsync(LessonReviewResultDTO result, bool approved, string? comment)
        {
            if (string.IsNullOrWhiteSpace(result.CreatorEmail)) return;

            var saludo = !string.IsNullOrWhiteSpace(result.CreatorFullName)
                ? $"Estimado(a) <strong>{result.CreatorFullName}</strong>,"
                : "Estimado(a),";

            string subject;
            string body;

            if (approved)
            {
                subject = "✅ Tu lección aprendida fue aprobada";
                body = $@"
                <p>{saludo}</p>
                <p>Tu <strong>lección aprendida</strong> fue revisada y <strong>aprobada</strong> por tu jefatura.</p>
                <p>No necesitas hacer nada más. ¡Gracias por tu aporte a la mejora continua!</p>
                <p>👉 <a href='{_platformUrl}' target='_blank'>Acceder a la plataforma</a></p>";
            }
            else
            {
                var comentarioHtml = string.IsNullOrWhiteSpace(comment)
                    ? ""
                    : $"<p><strong>Comentario de tu jefatura:</strong><br/>{comment}</p>";
                subject = "⚠️ Tu lección aprendida fue rechazada";
                body = $@"
                <p>{saludo}</p>
                <p>Tu <strong>lección aprendida</strong> fue <strong>rechazada</strong> por tu jefatura.</p>
                {comentarioHtml}
                <p>Por favor ingresa a la plataforma, <strong>edítala</strong> y vuelve a enviarla para una nueva revisión.</p>
                <p>👉 <a href='{_platformUrl}' target='_blank'>Acceder a la plataforma</a></p>";
            }

            await _emailService.SendAsync(
                to: new List<string> { result.CreatorEmail! },
                subject: subject,
                body: body,
                isHtml: true,
                bcc: new List<string> { "calvarez@abril.pe" });
        }
    }
}
