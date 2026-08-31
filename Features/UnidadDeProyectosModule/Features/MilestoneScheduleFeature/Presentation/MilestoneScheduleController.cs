using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Presentation
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [RequireFeature("mejora-continua.milestone-schedule")]
    public class MilestoneScheduleController : ControllerBase
    {
        private readonly IMilestoneScheduleService _service;

        public MilestoneScheduleController(IMilestoneScheduleService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllByMilestoneScheduleHistoryIdFactory([FromQuery] int milestoneScheduleHistoryId)
        {
            try
            {
                var result = await _service.GetAllByMilestoneScheduleHistoryId(milestoneScheduleHistoryId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("fake-data")]
        public async Task<IActionResult> BuildFakeSchedule()
        {
            try
            {
                var result = await _service.BuildFakeSchedule();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPatch("{milestoneScheduleId:int}/culminar")]
        [RequireFeature("mejora-continua.milestone-schedule.editar")]
        public async Task<IActionResult> Culminar(int milestoneScheduleId, [FromBody] MilestoneScheduleCulminarRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _service.CulminarAsync(milestoneScheduleId, request.FechaRealFin, userId);
                var message = request.FechaRealFin.HasValue
                    ? "Hito marcado como culminado."
                    : "Hito desmarcado como culminado.";
                return Ok(new { message });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Marca o desmarca un hito como "crítico" (corte real de etapa constructiva). Los hitos
        /// críticos son los que usa SSOMA para segmentar consumo de materiales y dotación de
        /// personal por fase; los no críticos son informativos (entregas, comercial, etc.).
        /// </summary>
        [Authorize]
        [HttpPatch("{milestoneScheduleId:int}/marcar-critico")]
        [RequireFeature("mejora-continua.milestone-schedule.editar")]
        public async Task<IActionResult> MarcarCritico(int milestoneScheduleId, [FromBody] MilestoneScheduleMarcarCriticoRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _service.MarcarCriticoAsync(milestoneScheduleId, request.EsHitoCritico, userId);
                var message = request.EsHitoCritico
                    ? "Hito marcado como crítico."
                    : "Hito desmarcado como crítico.";
                return Ok(new { message });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
