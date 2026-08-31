using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Dtos;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.AuthModule.ContractorCredentials.Presentation
{
    [ApiController]
    [Route("api/v1/auth/contractor-credentials")]
    [AllowAnonymous]
    public class ContractorCredentialsController : ControllerBase
    {
        private readonly IContractorCredentialsService _service;

        public ContractorCredentialsController(IContractorCredentialsService service)
        {
            _service = service;
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateToken([FromQuery] string token)
        {
            try
            {
                var result = await _service.ValidateToken(token);
                return Ok(result);
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContractorCredentialsCreateDto dto)
        {
            try
            {
                await _service.Create(dto);
                return Ok(new { message = "Credenciales registradas correctamente." });
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
