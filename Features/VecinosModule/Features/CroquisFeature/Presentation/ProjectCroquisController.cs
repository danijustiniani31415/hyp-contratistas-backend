using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Dtos;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Presentation
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProjectCroquisController : ControllerBase
    {
        private readonly ICroquisService _service;
        private readonly ILogger<ProjectCroquisController> _logger;

        public ProjectCroquisController(ICroquisService service, ILogger<ProjectCroquisController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Lista de proyectos con su croquis asignado (si tienen), para la pantalla de asignación.</summary>
        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] string? search)
        {
            try
            {
                var result = await _service.GetProjectsWithCroquis(search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CROQUIS GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Asigna o reemplaza el croquis de un proyecto.</summary>
        [HttpPost("{projectId:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Assign(int projectId, IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var imageUrl = await _service.AssignCroquis(projectId, file, userId);
                return Ok(new { imageUrl, message = "Croquis asignado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CROQUIS ASSIGN: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Lotes dibujados sobre un croquis.</summary>
        [HttpGet("{projectCroquisId:int}/lotes")]
        public async Task<IActionResult> GetLotes(int projectCroquisId)
        {
            try
            {
                var result = await _service.GetLotes(projectCroquisId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CROQUIS LOTES GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Vista de Gestión: todos los croquis registrados con sus lotes y vecinos del proyecto.</summary>
        [HttpGet("gestion")]
        public async Task<IActionResult> GetGestion()
        {
            try
            {
                var result = await _service.GetGestion();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CROQUIS GESTION: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Guarda el conjunto completo de lotes de un croquis (reemplaza los existentes).</summary>
        [HttpPut("{projectCroquisId:int}/lotes")]
        public async Task<IActionResult> SaveLotes(int projectCroquisId, [FromBody] SaveCroquisLotesDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.SaveLotes(projectCroquisId, dto.Lotes, userId);
                return Ok(new { message = "Lotes guardados exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CROQUIS LOTES SAVE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
