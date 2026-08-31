using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/reglas")]
    [Authorize]
    public class ReglasController : ControllerBase
    {
        private readonly IReglasTrabajadorRepository _repo;
        private readonly ILogger<ReglasController> _logger;

        public ReglasController(IReglasTrabajadorRepository repo, ILogger<ReglasController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try { return Ok(await _repo.GetAllAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ReglasController.GetAll"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SsItemTrabajadorRegla regla)
        {
            try
            {
                var creada = await _repo.CreateAsync(regla);
                return StatusCode(201, creada);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ReglasController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SsItemTrabajadorRegla regla)
        {
            try
            {
                if (regla.Id != id) regla.Id = id;
                var actualizada = await _repo.UpdateAsync(regla);
                return Ok(actualizada);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ReglasController.Update"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                if (!roles.Any(r => r.Equals(Roles.AdministradorSsoma, StringComparison.OrdinalIgnoreCase)))
                    return StatusCode(403, new { message = "Solo ADMINISTRADOR SSOMA puede eliminar reglas." });

                await _repo.DeleteAsync(id);
                return Ok(new { message = "Regla eliminada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ReglasController.Delete"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
