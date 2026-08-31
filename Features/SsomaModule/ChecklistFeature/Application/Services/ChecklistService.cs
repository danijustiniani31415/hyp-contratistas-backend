using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Services
{
    public class ChecklistService : IChecklistService
    {
        private readonly IChecklistRepository _repo;
        private readonly IEmailService _emailService;
        private readonly ILogger<ChecklistService> _logger;

        public ChecklistService(
            IChecklistRepository repo,
            IEmailService emailService,
            ILogger<ChecklistService> logger)
        {
            _repo         = repo;
            _emailService = emailService;
            _logger       = logger;
        }

        // ── PLANTILLAS ───────────────────────────────────────────────────

        public Task<List<ChecklistPlantillaListDto>> GetPlantillasAsync()
            => _repo.GetPlantillasAsync();

        public Task<ChecklistPlantillaDetalleDto?> GetPlantillaDetalleAsync(int plantillaId)
            => _repo.GetPlantillaDetalleAsync(plantillaId);

        public async Task<ChecklistPlantillaDetalleDto> CreatePlantillaAsync(ChecklistPlantillaUpsertDto dto, int userId)
        {
            var entity = await _repo.CreatePlantillaAsync(dto, userId);
            return (await _repo.GetPlantillaDetalleAsync(entity.Id))!;
        }

        public Task UpdatePlantillaAsync(int plantillaId, ChecklistPlantillaUpsertDto dto)
            => _repo.UpdatePlantillaAsync(plantillaId, dto);

        public async Task<ChecklistPlantillaItemDto> AddItemToPlantillaAsync(int plantillaId, ChecklistPlantillaItemCreateDto dto)
        {
            var item = await _repo.AddItemToPlantillaAsync(plantillaId, dto);
            return new ChecklistPlantillaItemDto
            {
                Id              = item.Id,
                Descripcion     = item.Descripcion,
                Orden           = item.Orden,
                TieneAdjuntoRef = item.TieneAdjuntoRef,
                Activo          = item.Activo
            };
        }

        public Task UpdatePlantillaItemAsync(int itemId, ChecklistPlantillaItemEditDto dto)
            => _repo.UpdatePlantillaItemAsync(itemId, dto);

        // ── PROYECTO ─────────────────────────────────────────────────────

        public Task<ChecklistProyectoResumenDto> GetResumenProyectoAsync(int proyectoId)
            => _repo.GetResumenProyectoAsync(proyectoId);

        public Task<ChecklistProyectoDetalleDto?> GetChecklistDetalleAsync(int checklistProyectoId)
            => _repo.GetChecklistDetalleAsync(checklistProyectoId);

        public async Task<ChecklistProyectoDetalleDto> ActivarChecklistAsync(int proyectoId, int plantillaId, int userId)
        {
            var entity = await _repo.ActivarChecklistAsync(proyectoId, plantillaId, userId);
            return (await _repo.GetChecklistDetalleAsync(entity.Id))!;
        }

        public Task SeedChecklistsObligatoriosAsync(int proyectoId, int userId)
            => _repo.SeedChecklistsObligatoriosAsync(proyectoId, userId);

        // ── ITEMS ────────────────────────────────────────────────────────

        public async Task<(decimal porcentaje, string estado)> ToggleItemAsync(
            int checklistProyectoItemId, ChecklistItemToggleDto dto, int userId)
        {
            var checklistProyectoId = await _repo.GetChecklistProyectoIdByItemAsync(checklistProyectoItemId);
            var (porcentaje, recienCompletado) = await _repo.ToggleItemAsync(checklistProyectoItemId, dto, userId);

            if (recienCompletado)
                await EnviarNotificacionCompletadoAsync(checklistProyectoId);

            var estado = porcentaje == 0 ? "pendiente"
                       : porcentaje < 100 ? "en_progreso"
                       : "completado";

            return (porcentaje, estado);
        }

        public async Task EnviarNotificacionCompletadoAsync(int checklistProyectoId, bool force = false)
        {
            try
            {
                var (email, proyecto, checklist) = await _repo.GetDatosNotificacionAsync(checklistProyectoId);

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("ChecklistId {Id}: sin email de Gerente configurado en el proyecto.", checklistProyectoId);
                    return;
                }

                var asunto = $"✅ Checklist completado: {checklist} — {proyecto}";
                var cuerpo = $@"
                    <h2>Checklist completado al 100%</h2>
                    <p>El checklist <strong>{checklist}</strong> del proyecto <strong>{proyecto}</strong>
                    ha sido completado en su totalidad.</p>
                    <p>Fecha: {DateTimeOffset.UtcNow.AddHours(-5):dd/MM/yyyy HH:mm} (hora Lima)</p>
                    <hr/>
                    <p style='color:#888;font-size:12px'>Mensaje automático del Sistema SSOMA — Abril Grupo Inmobiliario</p>";

                await _emailService.SendAsync(
                    to: new List<string> { email },
                    subject: asunto,
                    body: cuerpo,
                    isHtml: true);

                await _repo.MarcarNotificacionEnviadaAsync(checklistProyectoId);
                _logger.LogInformation("Notificación enviada para checklist {Id} al gerente {Email}.", checklistProyectoId, email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación de checklist {Id}.", checklistProyectoId);
            }
        }
    }
}
