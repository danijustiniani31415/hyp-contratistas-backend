using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Presentation
{
    [ApiController]
    [Route("api/v1/project")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectsService _service;

        public ProjectsController(IProjectsService service)
        {
            _service = service;
        }

        [HttpGet("paged-with-residents")]
        public async Task<IActionResult> GetPagedWithResidents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var result = await _service.GetPagedWithResidents(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{projectId:int}/foto")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFoto(int projectId, IFormFile foto)
        {
            try
            {
                if (foto == null || foto.Length == 0)
                    return BadRequest(new { message = "Debe adjuntar una imagen." });

                var fotoUrl = await _service.UploadFotoAsync(projectId, foto);
                return Ok(new { message = "Foto actualizada exitosamente.", fotoUrl });
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
