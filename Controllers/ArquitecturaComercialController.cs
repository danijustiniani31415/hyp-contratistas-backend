using System.Security.Claims;
using System.Text.Json;
using Abril_Backend.Application.DTOs.ArquitecturaComercial;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/arquitectura-comercial")]
    public class ArquitecturaComercialController : ControllerBase
    {
        private readonly IArquitecturaComercialService _service;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ArquitecturaComercialController> _logger;

        public ArquitecturaComercialController(IArquitecturaComercialService service, IConfiguration configuration, ILogger<ArquitecturaComercialController> logger)
        {
            _service = service;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] string? semana,
            [FromQuery] string? mes,
            [FromQuery] int? proyectoId)
        {
            try
            {
                var result = await _service.GetDashboardData(semana, mes, proyectoId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            try
            {
                var result = await _service.GetFilters();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("proyectos-con-actividades")]
        public async Task<IActionResult> GetProyectosConActividades()
        {
            try
            {
                var result = await _service.GetProyectosConActividades();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("supervisores-ac")]
        public async Task<IActionResult> GetSupervisoresAc([FromQuery] bool soloObreros = false)
        {
            try
            {

                var result = await _service.GetSupervisoresAc(soloObreros);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("actividades")]
        public async Task<IActionResult> GetActividades(
            [FromQuery] int? proyectoId,
            [FromQuery] string? tipo,
            [FromQuery] int? etapaId,
            [FromQuery] string? search,
            [FromQuery] bool? soloActivas,
            [FromQuery] int? filtroUserId,
            [FromQuery] int pagina = 1,
            [FromQuery] int porPagina = 100)
        {
            var rolesUsuario = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var esGestor = rolesUsuario.Contains(Roles.GestorArquitecturaComercial, StringComparer.OrdinalIgnoreCase);
            bool esUsuarioAc;
            if (esGestor)
                esUsuarioAc = false;
            else if (rolesUsuario.Contains(Roles.UsuarioArquitecturaComercial, StringComparer.OrdinalIgnoreCase))
                esUsuarioAc = true;
            else
                return Forbid();

            try
            {
                int? userId;
                if (esGestor)
                    userId = filtroUserId; // null = ve todo; valor = filtra por ese supervisor
                else
                    userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;

                var result = await _service.GetActividades(proyectoId, tipo, etapaId, search, soloActivas, pagina, porPagina, userId, esUsuarioAc);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("actividades")]
        public async Task<IActionResult> CreateActividad([FromBody] AcActividadCreateDTO dto)
        {
            try
            {
                var result = await _service.CreateActividad(dto);
                return StatusCode(201, result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("actividades/{id:int}")]
        public async Task<IActionResult> UpdateActividad(int id, [FromBody] AcActividadUpdateDTO dto)
        {
            try
            {
                var result = await _service.UpdateActividad(id, dto);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("actividades/{id:int}")]
        public async Task<IActionResult> DeleteActividad(int id)
        {
            try
            {
                await _service.DeleteActividad(id);
                return NoContent();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("actividades/{id:int}")]
        public async Task<IActionResult> PatchActividad(int id, [FromBody] Dictionary<string, JsonElement>? body)
        {
            try
            {
                if (body == null || body.Count == 0)
                    return BadRequest(new { message = "El body está vacío." });

                var result = await _service.PatchActividad(id, body);
                if (result == null)
                    return NotFound(new { message = "Actividad no encontrada." });

                return Ok(result);
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Formato de fecha inválido. Use YYYY-MM-DD." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("actividades/reasignar-encargado")]
        public async Task<IActionResult> ReasignarEncargado([FromBody] ReasignarEncargadoDTO? body)
        {
            try
            {
                if (body == null || body.ProyectoId <= 0)
                    return BadRequest(new { message = "proyectoId es requerido." });

                var result = await _service.ReasignarEncargado(body.ProyectoId);
                if (result == null)
                    return NotFound(new { message = "Proyecto no encontrado." });

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("plantilla")]
        public async Task<IActionResult> GetPlantilla()
        {
            try
            {
                var result = await _service.GetPlantilla();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("plantilla")]
        public async Task<IActionResult> CreatePlantilla([FromBody] CreatePlantillaDTO? body)
        {
            try
            {
                if (body == null)
                    return BadRequest(new { message = "El body está vacío." });

                var result = await _service.CreatePlantilla(body);
                return CreatedAtAction(nameof(GetPlantilla), new { id = result.Id }, result);
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

        [HttpPatch("plantilla/{id:int}")]
        public async Task<IActionResult> PatchPlantilla(int id, [FromBody] Dictionary<string, JsonElement>? body)
        {
            try
            {
                if (body == null || body.Count == 0)
                    return BadRequest(new { message = "El body está vacío." });

                var result = await _service.PatchPlantilla(id, body);
                if (result == null)
                    return NotFound(new { message = "Plantilla no encontrada." });

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

        [HttpGet("categorias")]
        public async Task<IActionResult> GetCategorias()
        {
            try { return Ok(await _service.GetCategorias()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("especialidades")]
        public async Task<IActionResult> GetEspecialidades()
        {
            try { return Ok(await _service.GetEspecialidades()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("etapas")]
        public async Task<IActionResult> GetEtapas()
        {
            try { return Ok(await _service.GetEtapas()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("gantt")]
        public async Task<IActionResult> GetGantt(
            [FromQuery] int? proyectoId,
            [FromQuery] string? tipo,
            [FromQuery] string? etapa,
            [FromQuery] bool? soloActivas)
        {
            try
            {
                var result = await _service.GetGantt(proyectoId, tipo, etapa, soloActivas);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("proyectos/{id:int}")]
        public async Task<IActionResult> PatchProyecto(int id, [FromBody] PatchProyectoDTO? body)
        {
            try
            {
                if (body == null)
                    return BadRequest(new { message = "El body está vacío." });

                var result = await _service.PatchProyecto(id, body);
                if (result == null)
                    return NotFound(new { message = "Proyecto no encontrado." });

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

        [HttpPost("actividades/generar")]
        public async Task<IActionResult> GenerarActividades([FromBody] GenerarActividadesDTO? body)
        {
            try
            {
                if (body == null || body.ProyectoId <= 0)
                    return BadRequest(new { message = "proyectoId es requerido." });

                var result = await _service.GenerarActividades(body.ProyectoId);
                if (result == null)
                    return NotFound(new { message = "Proyecto no encontrado." });

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

        [HttpGet("dashboard-v2")]
        public async Task<IActionResult> GetDashboardV2([FromQuery] DashboardFiltroDTO filtro)
        {
            try
            {
                var rolesUsuario = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var esGestor = rolesUsuario.Contains(Roles.GestorArquitecturaComercial, StringComparer.OrdinalIgnoreCase);
                if (!esGestor)
                {
                    if (rolesUsuario.Contains(Roles.UsuarioArquitecturaComercial, StringComparer.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
                            filtro.UserId = uid;
                    }
                    else return Forbid();
                }
                var result = await _service.GetDashboardDataFiltrado(filtro);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error dashboard AC v2"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("dashboard/supervisor/{userId}/historico")]
        public async Task<IActionResult> GetSupervisorHistorico(int userId)
        {
            try
            {
                var result = await _service.GetSupervisorHistorico(userId);
                if (result == null) return NotFound(new { message = "Supervisor no encontrado." });
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error histórico de supervisor AC"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("alertas/{tipoAlerta}")]
        public async Task<IActionResult> GetActividadesPorAlerta(
            string tipoAlerta, [FromQuery] DashboardFiltroDTO filtro)
        {
            try
            {
                var result = await _service.GetActividadesPorAlerta(tipoAlerta, filtro);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error alertas AC"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("alertas/enviar")]
        public async Task<IActionResult> EnviarAlertas([FromBody] EnviarAlertaRequestDTO request)
        {
            try
            {
                await _service.EnviarAlertasActividades(request);
                return Ok(new { message = "Alertas enviadas correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error envío alertas AC"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [AllowAnonymous]
        [HttpPost("avance-semanal/snapshot")]
        public async Task<IActionResult> SnapshotAvanceSemanal()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != $"Bearer {_configuration["CronSecret"]}") return Unauthorized();

            try
            {
                var result = await _service.SnapshotAvanceSemanal();
                await _service.SnapshotRankingSemanal();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("recalcular-spi")]
        public async Task<IActionResult> RecalcularSpi()
        {
            try
            {
                await _service.RecalcularTodosSpi();
                return Ok(new { message = "SPI recalculado correctamente." });
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
