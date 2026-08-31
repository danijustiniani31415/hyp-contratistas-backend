using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores;
using WorkerUpdateDto = Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers.WorkerUpdateDto;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/workers")]
    [Authorize]
    public class WorkersController : ControllerBase
    {
        private const string MensajeRestriccion =
            "No se puede ingresar o reingresar al trabajador. Comuníquese con el área de Administración o SSOMA.";

        private readonly IWorkerSearchService _service;
        private readonly IHabTrabajadorRepository _habRepo;
        private readonly ITrabajadorRestringidoService _restringidoService;
        private readonly ILogger<WorkersController> _logger;

        public WorkersController(
            IWorkerSearchService service,
            IHabTrabajadorRepository habRepo,
            ITrabajadorRestringidoService restringidoService,
            ILogger<WorkersController> logger)
        {
            _service = service;
            _habRepo = habRepo;
            _restringidoService = restringidoService;
            _logger = logger;
        }

        private int? GetEmpresaIdContratista() =>
            User.FindFirst("tipo")?.Value == "CONTRATISTA"
                && int.TryParse(User.FindFirst("empresaId")?.Value, out var id)
                ? id
                : null;

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 20)
        {
            try { return Ok(await _service.Search(q, limit, GetEmpresaIdContratista())); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.Search");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null || !int.TryParse(claim.Value, out var userId))
                    return Unauthorized(new { message = "Inicie sesión" });

                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";
                var worker = await _service.GetByUserId(userId, esContratista);
                if (worker == null) return NotFound(new { message = "Tu usuario no está vinculado a una ficha de trabajador." });

                return Ok(worker);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.GetMe");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("document-types")]
        public async Task<IActionResult> GetDocumentTypes()
        {
            try { return Ok(await _service.GetDocumentTypes()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.GetDocumentTypes");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("worker-categories")]
        public async Task<IActionResult> GetWorkerCategories()
        {
            try { return Ok(await _service.GetWorkerCategories()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.GetWorkerCategories");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Verifica un correo corporativo antes de guardar: formato, existencia en el directorio de
        /// Abril (tenant de Microsoft) y que no esté ya asignado a otro trabajador no retirado.
        /// Devuelve siempre 200 con el detalle; el bloqueo real lo hacen POST/PUT.
        /// </summary>
        /// <param name="email">Correo a verificar.</param>
        /// <param name="workerId">Trabajador que se está editando (se excluye del chequeo de duplicados).</param>
        /// <param name="corporativo">
        /// Lo envía el formulario de alta (Staff/Oficina Central = true) cuando aún no hay
        /// workerId. Si se omite, la clasificación se lee del trabajador.
        /// </param>
        [HttpGet("validar-email-corporativo")]
        public async Task<IActionResult> ValidarEmailCorporativo(
            [FromQuery] string? email,
            [FromQuery] int? workerId,
            [FromQuery] bool? corporativo)
        {
            try { return Ok(await _service.ValidarEmailCorporativo(email, workerId, corporativo)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.ValidarEmailCorporativo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WorkerCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ApellidoNombre))
                    return BadRequest(new { message = "apellidoNombre es obligatorio." });
                if (string.IsNullOrWhiteSpace(dto.Dni) ||
                    !DocumentoHelper.EsFormatoValido(dto.Dni, dto.TipoDocumento))
                {
                    var tipoMsg = dto.TipoDocumento?.ToUpper() == "CE"
                        ? "El CE debe tener entre 6 y 12 caracteres alfanuméricos sin espacios."
                        : dto.TipoDocumento?.ToUpper() == "DNI"
                        ? "El DNI debe tener exactamente 8 dígitos numéricos."
                        : "El documento debe ser un DNI (8 dígitos) o CE (6-12 caracteres alfanuméricos).";
                    return BadRequest(new { message = tipoMsg });
                }

                if (await _restringidoService.EstaRestringidoPorDniAsync(dto.Dni))
                    throw new AbrilException(MensajeRestriccion, 400);

                if (User.FindFirst("tipo")?.Value == "CONTRATISTA")
                {
                    if (!int.TryParse(User.FindFirst("empresaId")?.Value, out var empresaIdJwt))
                        return StatusCode(400, new { message = "No se pudo determinar la empresa del contratista." });
                    dto.EmpresaId = empresaIdJwt;
                    dto.ContrataCasa = "Contratista";
                }

                var id = await _service.Create(dto);

                if (dto.ProyectoId.HasValue)
                {
                    await _habRepo.AgregarProyectoAsync(id, new AgregarProyectoDto
                    {
                        ProyectoId  = dto.ProyectoId.Value,
                        EmpresaId   = dto.EmpresaId,
                        FechaInicio = dto.FechaIngreso,
                    });
                }

                await _habRepo.InicializarEntregablesAsync(id);
                return StatusCode(201, new { id, message = "Trabajador creado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.Create");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] WorkerUpdateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ApellidoNombre))
                    return BadRequest(new { message = "apellidoNombre es obligatorio." });

                await _service.Update(id, dto, User.IsInRole(Roles.AdministradorDeObra));
                return Ok(new { message = "Trabajador actualizado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.Update");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("{id:int}/datos-basicos")]
        public async Task<IActionResult> UpdateDatosBasicos(int id, [FromBody] WorkerDatosBasicosDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
                    return BadRequest(new { message = "El nombre completo es obligatorio." });

                await _service.UpdateDatosBasicos(id, dto, User.IsInRole(Roles.AdministradorDeObra));
                return Ok(new { message = "Trabajador actualizado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.UpdateDatosBasicos");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{id:int}/retirar")]
        public async Task<IActionResult> Retirar(int id)
        {
            try
            {
                await _service.Retirar(id);
                return Ok(new { message = "Trabajador retirado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en WorkersController.Retirar");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
