using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Dtos;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Interfaces;

namespace Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Presentation
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class InvoiceFolderController : ControllerBase
    {
        private readonly IInvoiceFolderService _service;

        public InvoiceFolderController(IInvoiceFolderService service)
        {
            _service = service;
        }

        /// <summary>Carpeta única configurada para guardar las facturas (null si aún no se configuró).</summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                if (GetUserId() == null) return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetSingleton();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Configura/actualiza la carpeta única: recibe el link, lo detecta y lo guarda.</summary>
        [HttpPut]
        public async Task<IActionResult> Save([FromBody] InvoiceFolderSaveDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.Save(dto, userId.Value);
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

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : null;
        }
    }
}
