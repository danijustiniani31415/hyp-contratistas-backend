using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;
using Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GuiasRemisionModule.Presentation
{
    /// <summary>Transportistas, vehículos y conductores reutilizables al crear guías de remisión.</summary>
    [ApiController]
    [Route("api/v1/guias-remision/maestros")]
    [Authorize]
    public class GuiaMaestrosController : ControllerBase
    {
        private readonly IGuiaMaestrosService _service;

        public GuiaMaestrosController(IGuiaMaestrosService service)
        {
            _service = service;
        }

        private static readonly string[] PermisosDeLectura =
            { "GUIA_REMISION_VER", "GUIA_REMISION_CREAR", "GUIA_REMISION_ENVIAR", "GUIA_REMISION_CONFIRMAR" };

        private const string ErrorServidor = "Error del servidor. Por favor contactar al administrador del sistema.";

        private async Task<IActionResult> Leer<T>(Func<Task<T>> accion)
        {
            if (!PermisosDeLectura.Any(User.HasLbPermiso))
                return StatusCode(403, new { message = "No tienes permiso para ver guías de remisión." });
            try { return Ok(await accion()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = ErrorServidor }); }
        }

        private async Task<IActionResult> Escribir<T>(Func<Task<T>> accion)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para editar los datos de transporte." });
            try { return Ok(await accion()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = ErrorServidor }); }
        }

        [HttpGet("transportistas")]
        public Task<IActionResult> ListarTransportistas() => Leer(_service.ListarTransportistas);

        [HttpPost("transportistas")]
        public Task<IActionResult> CrearTransportista([FromBody] TransportistaDto dto) => Escribir(() => _service.GuardarTransportista(null, dto));

        [HttpPut("transportistas/{id:long}")]
        public Task<IActionResult> EditarTransportista(long id, [FromBody] TransportistaDto dto) => Escribir(() => _service.GuardarTransportista(id, dto));

        [HttpGet("vehiculos")]
        public Task<IActionResult> ListarVehiculos() => Leer(_service.ListarVehiculos);

        [HttpPost("vehiculos")]
        public Task<IActionResult> CrearVehiculo([FromBody] VehiculoDto dto) => Escribir(() => _service.GuardarVehiculo(null, dto));

        [HttpPut("vehiculos/{id:long}")]
        public Task<IActionResult> EditarVehiculo(long id, [FromBody] VehiculoDto dto) => Escribir(() => _service.GuardarVehiculo(id, dto));

        [HttpGet("conductores")]
        public Task<IActionResult> ListarConductores() => Leer(_service.ListarConductores);

        [HttpPost("conductores")]
        public Task<IActionResult> CrearConductor([FromBody] ConductorDto dto) => Escribir(() => _service.GuardarConductor(null, dto));

        [HttpPut("conductores/{id:long}")]
        public Task<IActionResult> EditarConductor(long id, [FromBody] ConductorDto dto) => Escribir(() => _service.GuardarConductor(id, dto));
    }
}
