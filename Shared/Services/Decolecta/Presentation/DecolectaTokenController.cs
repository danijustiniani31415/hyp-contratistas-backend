using Abril_Backend.Application.Exceptions;
using Abril_Backend.Shared.Services.Decolecta.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Shared.Services.Decolecta.Presentation
{
    [ApiController]
    [Route("api/v1/decolecta-tokens")]
    public class DecolectaTokenController : ControllerBase
    {
        private readonly IDecolectaTokenStore _store;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DecolectaTokenController> _logger;

        public DecolectaTokenController(
            IDecolectaTokenStore store,
            IConfiguration configuration,
            ILogger<DecolectaTokenController> logger)
        {
            _store = store;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Renovación mensual de la cuota de los tokens de Decolecta. Lo llama cron-job.org
        /// el 1 de cada mes a las 00:00 (hora Perú) porque Decolecta resetea la cuota mensual
        /// de cada cuenta: aquí se desmarca el flag agotado de todos los tokens vigentes.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("cron/renovar")]
        public async Task<IActionResult> RenovarCuotaMensual()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}") return Unauthorized();

                var renovados = await _store.RenovarCuotaMensualAsync();
                return Ok(new { renovados, mensaje = $"Se renovó la cuota mensual de {renovados} token(s) de Decolecta." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DecolectaTokenController.RenovarCuotaMensual");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
