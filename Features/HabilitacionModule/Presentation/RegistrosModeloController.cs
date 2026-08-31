using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/registros-modelo")]
    [AllowAnonymous]
    public class RegistrosModeloController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<RegistrosModeloController> _logger;

        public RegistrosModeloController(
            IDbContextFactory<AppDbContext> factory,
            ILogger<RegistrosModeloController> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                using var ctx = _factory.CreateDbContext();
                var registros = await ctx.SsRegistroModelo
                    .Where(r => r.Activo)
                    .OrderBy(r => r.Orden)
                    .ToListAsync();
                return Ok(registros);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en RegistrosModeloController.GetAll"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
