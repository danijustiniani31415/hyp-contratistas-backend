using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Interfaces;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Presentation
{
    [ApiController]
    [Route("api/v1/area-scope")]
    [Authorize]
    public class AreaScopeController : ControllerBase
    {
        private readonly IAreaScopeService _service;
        public AreaScopeController(IAreaScopeService service) => _service = service;

        [HttpGet("tree")]
        public async Task<IActionResult> GetTree()
        {
            try { return Ok(await _service.GetTreeAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{areaScopeId}/workers")]
        public async Task<IActionResult> GetWorkers(int areaScopeId)
        {
            try { return Ok(await _service.GetWorkersAsync(areaScopeId)); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("branch")]
        public async Task<IActionResult> CreateBranch([FromBody] AreaScopeBranchDto dto)
        {
            try
            {
                await _service.CreateBranchAsync(dto);
                return Ok(new { message = "Rama creada." });
            }
            catch (AbrilException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{areaScopeId}/parent")]
        public async Task<IActionResult> UpdateParent(int areaScopeId, [FromBody] AreaScopeUpdateParentDto dto)
        {
            try
            {
                await _service.UpdateParentAsync(areaScopeId, dto.NewParentAreaScopeId);
                return Ok(new { message = "Nodo padre actualizado." });
            }
            catch (AbrilException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("{areaScopeId}")]
        public async Task<IActionResult> Delete(int areaScopeId)
        {
            try
            {
                await _service.DeleteAsync(areaScopeId);
                return Ok(new { message = "Nodo eliminado." });
            }
            catch (AbrilException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
