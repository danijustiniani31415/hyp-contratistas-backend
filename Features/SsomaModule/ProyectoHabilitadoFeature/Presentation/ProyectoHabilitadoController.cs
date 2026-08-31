using System.Security.Claims;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/proyectos")]
    [Authorize]
    public class ProyectoHabilitadoController : ControllerBase
    {
        private readonly IProyectoHabilitadoService _service;

        public ProyectoHabilitadoController(IProyectoHabilitadoService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Sesión inválida.");
            return int.Parse(claim.Value);
        }

        /// <summary>Lista todos los proyectos activos con su estado de habilitación SSOMA (pantalla de administración).</summary>
        [HttpGet]
        [RequireFeature("ssoma.gestion.proyectos-habilitados")]
        public async Task<IActionResult> GetTodos()
        {
            try
            {
                var result = await _service.GetTodosAsync();
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Lista solo los proyectos habilitados para SSOMA (para selects/filtros de cualquier feature del módulo).</summary>
        [HttpGet("habilitados")]
        public async Task<IActionResult> GetHabilitados()
        {
            try
            {
                var result = await _service.GetHabilitadosAsync();
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Habilita o deshabilita un proyecto para el módulo SSOMA.</summary>
        [HttpPatch("{proyectoId:int}")]
        [RequireFeature("ssoma.gestion.proyectos-habilitados")]
        public async Task<IActionResult> SetHabilitado(int proyectoId, [FromBody] ProyectoHabilitadoToggleDto dto)
        {
            try
            {
                var userId = GetUserId();
                await _service.SetHabilitadoAsync(proyectoId, dto.Habilitado, userId);
                return NoContent();
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }
    }
}
