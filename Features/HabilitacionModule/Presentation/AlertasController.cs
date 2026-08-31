using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/alertas")]
    public class AlertasController : ControllerBase
    {
        private readonly IVigenciaRevisionService _vigenciaService;
        private readonly IRetiroAutomaticoService _retiroService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AlertasController> _logger;

        public AlertasController(
            IVigenciaRevisionService vigenciaService,
            IRetiroAutomaticoService retiroService,
            IConfiguration configuration,
            ILogger<AlertasController> logger)
        {
            _vigenciaService = vigenciaService;
            _retiroService = retiroService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("revisar-vigencias")]
        [AllowAnonymous]
        public async Task<IActionResult> RevisarVigencias()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var result = await _vigenciaService.RevisarVigencias();
                return Ok(new
                {
                    trabajadores = result.Trabajadores,
                    empresas = result.Empresas,
                    equipos = result.Equipos,
                    emos = result.Emos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AlertasController.RevisarVigencias");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("retiro-automatico")]
        [AllowAnonymous]
        public async Task<IActionResult> RetiroAutomatico()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var result = await _retiroService.EjecutarAsync();
                return Ok(new
                {
                    retirados = result.TotalRetirados,
                    avisados = result.TotalAvisados,
                    detalles = result.Detalles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AlertasController.RetiroAutomatico");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
