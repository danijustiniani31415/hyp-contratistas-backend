using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    /// <summary>Vista proyecto-primero de la pantalla Empresa: qué empresas están habilitadas
    /// en un proyecto y cómo están sus entregables SSOMA/Administración.</summary>
    [ApiController]
    [Route("api/v1/habilitacion/proyectos/{proyectoId:int}/empresas")]
    [Authorize]
    public class HabEmpresasPorProyectoController : ControllerBase
    {
        private readonly IHabEmpresaRepository _repo;
        private readonly ILogger<HabEmpresasPorProyectoController> _logger;

        public HabEmpresasPorProyectoController(IHabEmpresaRepository repo, ILogger<HabEmpresasPorProyectoController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmpresasPorProyecto(int proyectoId)
        {
            try
            {
                var result = await _repo.GetEmpresasPorProyectoAsync(proyectoId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en HabEmpresasPorProyectoController.GetEmpresasPorProyecto");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
