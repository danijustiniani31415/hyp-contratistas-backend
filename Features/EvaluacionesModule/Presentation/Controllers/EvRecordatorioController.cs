using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/evaluaciones/recordatorios")]
    public class EvRecordatorioController : ControllerBase
    {
        private readonly IEvRecordatorioService _service;
        private readonly IConfiguration _configuration;

        public EvRecordatorioController(
            IEvRecordatorioService service,
            IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        [HttpGet("enviar")]
        public async Task<IActionResult> EnviarRecordatorios()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var resultado = await _service.ProcesarRecordatoriosAsync();
                return Ok(resultado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("descargo")]
        public async Task<IActionResult> EnviarDescargo()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var resultado = await _service.ProcesarDescargoAsync();
                return Ok(resultado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Punto de entrada único para un solo cronjob diario: procesa recordatorios
        /// (residentes + contratistas) y descargo en una sola llamada.
        /// </summary>
        [HttpGet("procesar-diario")]
        public async Task<IActionResult> ProcesarDiario()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var resultado = await _service.ProcesarDiarioAsync();
                return Ok(resultado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
