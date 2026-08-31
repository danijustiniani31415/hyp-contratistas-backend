using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Restringidos;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/restringidos")]
    [Authorize]
    public class TrabajadorRestringidoController : ControllerBase
    {
        private static readonly string[] RolesPermitidos = [Roles.AdministradorSsoma, Roles.AdministradorAdministracion];
        private const string EmailAutorizadoParaBorrar = "sjustiniani@abril.pe";

        private readonly ITrabajadorRestringidoService _service;
        private readonly ILogger<TrabajadorRestringidoController> _logger;

        public TrabajadorRestringidoController(
            ITrabajadorRestringidoService service,
            ILogger<TrabajadorRestringidoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool soloActivos = true,
            [FromQuery] string? dni = null)
        {
            try
            {
                var items = await _service.GetAllAsync(soloActivos, dni);
                return Ok(items);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TrabajadorRestringidoController.GetAll"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TrabajadorRestringidoCreateDto dto)
        {
            try
            {
                if (!UsuarioTienePermiso())
                    return StatusCode(403, new { message = "No tiene permisos para realizar esta acción." });

                // No basta con "no vacío": exigimos un mínimo de contenido real para que el motivo
                // no quede en una frase genérica sin la razón real de la restricción.
                if (string.IsNullOrWhiteSpace(dto.Motivo) || dto.Motivo.Trim().Length < 15)
                    return BadRequest(new { message = "El motivo es requerido y debe describir la razón real (mínimo 15 caracteres), no solo una clasificación genérica." });

                if (string.IsNullOrWhiteSpace(dto.Dni) && string.IsNullOrWhiteSpace(dto.ApellidoNombre))
                    return BadRequest(new { message = "Debe proporcionar DNI o nombre del trabajador." });

                dto.Tipo ??= "MANUAL";
                var result = await _service.CreateAsync(dto, GetUserId());
                return StatusCode(201, result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TrabajadorRestringidoController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                if (!PuedeBorrar())
                    return StatusCode(403, new { message = "No tiene permisos para realizar esta acción." });

                await _service.DesactivarAsync(id);
                return Ok(new { message = "Restricción desactivada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TrabajadorRestringidoController.Desactivar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private bool UsuarioTienePermiso()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return roles.Any(r => RolesPermitidos.Contains(r, StringComparer.OrdinalIgnoreCase));
        }

        private bool PuedeBorrar()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            return string.Equals(email, EmailAutorizadoParaBorrar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
