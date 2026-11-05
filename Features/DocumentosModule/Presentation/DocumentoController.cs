using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.DocumentosModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.DocumentosModule.Presentation
{
    /// <summary>Búsqueda de personas (DNI) y empresas (RUC) para autocompletar nombres en cualquier formulario.</summary>
    [ApiController]
    [Route("api/v1/documentos")]
    [Authorize]
    public class DocumentoController : ControllerBase
    {
        private readonly IDocumentoService _service;

        public DocumentoController(IDocumentoService service)
        {
            _service = service;
        }

        private const string ErrorServidor = "Error del servidor. Por favor contactar al administrador del sistema.";

        [HttpGet("dni/{dni}")]
        public async Task<IActionResult> BuscarDni(string dni)
        {
            try
            {
                var r = await _service.BuscarDni(dni);
                return r is null ? NotFound(new { message = "No se encontró ese DNI." }) : Ok(r);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = ErrorServidor }); }
        }

        [HttpGet("ruc/{ruc}")]
        public async Task<IActionResult> BuscarRuc(string ruc)
        {
            try
            {
                var r = await _service.BuscarRuc(ruc);
                return r is null ? NotFound(new { message = "No se encontró ese RUC en SUNAT." }) : Ok(r);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = ErrorServidor }); }
        }
    }
}
