using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Presentation
{
    [ApiController]
    [Route("api/v1/planeamiento-bim/dashboard")]
    [Authorize]
    [RequireFeature("planeamiento-bim.configuracion-inicial")]
    public class PlaneamientoBimDashboardController : ControllerBase
    {
        private readonly IPlaneamientoBimDashboardService _service;

        public PlaneamientoBimDashboardController(IPlaneamientoBimDashboardService service)
        {
            _service = service;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("oid");
            return claim != null ? int.Parse(claim.Value) : null;
        }

        [HttpGet("{projectId:int}/avance")]
        public async Task<IActionResult> GetAvance(int projectId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
        {
            try
            {
                return Ok(await _service.GetAvance(projectId, desde, hasta));
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

        [HttpGet("{projectId:int}/ppc")]
        public async Task<IActionResult> GetPpcHistorico(int projectId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
        {
            try
            {
                return Ok(await _service.GetPpcHistorico(projectId, desde, hasta));
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

        [HttpGet("{projectId:int}/metas-semanales")]
        public async Task<IActionResult> GetMetasSemanales(int projectId)
        {
            try
            {
                return Ok(await _service.GetMetasSemanales(projectId));
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

        [HttpPut("{projectId:int}/metas-semanales")]
        public async Task<IActionResult> GuardarMetasSemanales(int projectId, [FromBody] MetaSemanalUpdateDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                await _service.GuardarMetasSemanales(projectId, dto, userId.Value);
                return NoContent();
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

        [HttpGet("{projectId:int}/plan-maestro")]
        public async Task<IActionResult> GetPlanMaestro(int projectId)
        {
            try
            {
                return Ok(await _service.GetPlanMaestro(projectId));
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

        [HttpGet("{projectId:int}/causas-pareto")]
        public async Task<IActionResult> GetCausasPareto(int projectId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
        {
            try
            {
                return Ok(await _service.GetCausasPareto(projectId, desde, hasta));
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
