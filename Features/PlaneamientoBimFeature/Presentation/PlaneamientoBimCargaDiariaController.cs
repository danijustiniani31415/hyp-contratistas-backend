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
    [Route("api/v1/planeamiento-bim/carga-diaria")]
    [Authorize]
    [RequireFeature("planeamiento-bim.configuracion-inicial")]
    public class PlaneamientoBimCargaDiariaController : ControllerBase
    {
        private readonly IPlaneamientoBimCargaDiariaService _service;
        private readonly ILogger<PlaneamientoBimCargaDiariaController> _logger;

        public PlaneamientoBimCargaDiariaController(
            IPlaneamientoBimCargaDiariaService service,
            ILogger<PlaneamientoBimCargaDiariaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("oid");
            return claim != null ? int.Parse(claim.Value) : null;
        }

        [HttpGet("{projectId:int}")]
        public async Task<IActionResult> GetCargaDiaria(int projectId, [FromQuery] DateOnly fecha, [FromQuery] string categoria = "GENERAL")
        {
            try
            {
                return Ok(await _service.GetCargaDiaria(projectId, fecha, categoria));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetCargaDiaria (projectId={ProjectId}, fecha={Fecha}, categoria={Categoria})", projectId, fecha, categoria);
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("{projectId:int}")]
        public async Task<IActionResult> GuardarCargaDiaria(int projectId, [FromQuery] DateOnly fecha, [FromBody] CargaDiariaUpdateDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                await _service.GuardarCargaDiaria(projectId, fecha, dto, userId.Value);
                return NoContent();
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GuardarCargaDiaria (projectId={ProjectId}, fecha={Fecha})", projectId, fecha);
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("{projectId:int}/evidencias")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubirEvidencias(int projectId, [FromQuery] DateOnly fecha, [FromForm] IFormFileCollection files, [FromQuery] string categoria = "GENERAL")
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var evidencias = await _service.SubirEvidencias(projectId, fecha, files, userId.Value, categoria);
                return Ok(evidencias);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SubirEvidencias (projectId={ProjectId}, fecha={Fecha}, categoria={Categoria})", projectId, fecha, categoria);
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
