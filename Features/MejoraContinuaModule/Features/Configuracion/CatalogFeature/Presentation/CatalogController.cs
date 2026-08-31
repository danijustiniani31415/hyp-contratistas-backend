using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Presentation
{
    [ApiController]
    [Route("api/v1/mejora-continua/catalog")]
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogService _service;
        public CatalogController(ICatalogService service) => _service = service;

        [Authorize]
        [HttpGet("types")]
        public async Task<IActionResult> GetTypes()
        {
            try { return Ok(await _service.GetAllTypesAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpGet("items/{catalogTypeId}")]
        public async Task<IActionResult> GetItems(int catalogTypeId)
        {
            try { return Ok(await _service.GetItemsByTypeAsync(catalogTypeId)); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpGet("full-tree")]
        public async Task<IActionResult> GetFullTree()
        {
            try { return Ok(await _service.GetFullTreeAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPost("types")]
        public async Task<IActionResult> CreateType([FromBody] CatalogTypeCreateDTO dto)
        {
            try
            {
                await _service.CreateTypeAsync(dto);
                return Ok(new { message = "Tipo de catálogo creado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPut("types")]
        public async Task<IActionResult> UpdateType([FromBody] CatalogTypeEditDTO dto)
        {
            try
            {
                await _service.UpdateTypeAsync(dto);
                return Ok(new { message = "Tipo de catálogo actualizado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpDelete("types/{catalogTypeId}")]
        public async Task<IActionResult> DeleteType(int catalogTypeId)
        {
            try
            {
                await _service.DeleteTypeAsync(catalogTypeId);
                return Ok(new { message = "Tipo de catálogo eliminado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPost("items")]
        public async Task<IActionResult> CreateItem([FromBody] CatalogItemCreateDTO dto)
        {
            try
            {
                await _service.CreateItemAsync(dto);
                return Ok(new { message = "Ítem creado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPut("items")]
        public async Task<IActionResult> UpdateItem([FromBody] CatalogItemEditDTO dto)
        {
            try
            {
                await _service.UpdateItemAsync(dto);
                return Ok(new { message = "Ítem actualizado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpDelete("items/{catalogItemId}")]
        public async Task<IActionResult> DeleteItem(int catalogItemId)
        {
            try
            {
                await _service.DeleteItemAsync(catalogItemId);
                return Ok(new { message = "Ítem eliminado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
