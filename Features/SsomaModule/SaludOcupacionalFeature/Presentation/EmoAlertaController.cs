using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/alertas")]
    public class EmoAlertaController : ControllerBase
    {
        private readonly IEmoAlertaService _service;
        private readonly IEmoAutoProgramacionService _autoProgramacionService;
        private readonly IEmoResumenDiarioService _resumenDiarioService;
        private readonly IProgramacionEmoService _programacionService;
        private readonly IAlertaLoginSsomaService _alertaLoginService;
        private readonly IConfiguration _configuration;

        public EmoAlertaController(
            IEmoAlertaService service,
            IEmoAutoProgramacionService autoProgramacionService,
            IEmoResumenDiarioService resumenDiarioService,
            IProgramacionEmoService programacionService,
            IAlertaLoginSsomaService alertaLoginService,
            IConfiguration configuration)
        {
            _service = service;
            _autoProgramacionService = autoProgramacionService;
            _resumenDiarioService = resumenDiarioService;
            _programacionService = programacionService;
            _alertaLoginService = alertaLoginService;
            _configuration = configuration;
        }

        /// <summary>
        /// Aviso al ingresar: interconsultas pendientes + EMOs vencidos de los proyectos donde el
        /// usuario logueado es Administrador o Coordinador SSOMA. Cualquier usuario autenticado
        /// puede llamarlo — si no coincide con ningún proyecto, simplemente devuelve vacío.
        /// Calculado en vivo (sin cron ni persistencia).
        /// </summary>
        [HttpGet("mi-resumen")]
        [Authorize]
        public async Task<IActionResult> MiResumen()
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
                if (!userId.HasValue)
                    return Unauthorized(new { message = "No se pudo identificar al usuario." });

                return Ok(await _alertaLoginService.GetResumen(userId.Value));
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

        [HttpGet("procesar")]
        public async Task<IActionResult> Procesar()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var result = await _service.ProcesarAlertas();
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("procesar-7dias")]
        public async Task<IActionResult> Procesar7Dias()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var result = await _service.ProcesarAlertas7DiasCalendario();
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("auto-programar")]
        public async Task<IActionResult> AutoProgramar()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var result = await _autoProgramacionService.ProcesarAutoProgramacion();
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("cerrar-inasistencias")]
        public async Task<IActionResult> CerrarInasistencias()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var cerradas = await _programacionService.CerrarInasistenciasVencidasAsync();
                return Ok(new { cerradas });
            }
            catch (AbrilException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("resumen-diario")]
        public async Task<IActionResult> ResumenDiario()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var result = await _resumenDiarioService.EnviarResumenDiario();
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
