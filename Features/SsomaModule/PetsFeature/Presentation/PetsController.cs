using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.SsomaModule.PetsFeature.Presentation;

[ApiController]
[Route("api/v1/pets")]
[Authorize]
public class PetsController : ControllerBase
{
    private readonly IPetsService _service;
    private readonly IPetsImportService _importService;
    private readonly ILogger<PetsController> _logger;

    public PetsController(IPetsService service, IPetsImportService importService, ILogger<PetsController> logger)
    {
        _service = service;
        _importService = importService;
        _logger = logger;
    }

    [HttpGet]
    [RequireFeature("ssoma.gestion.pets", "ssoma.gestion.opt")]
    public async Task<IActionResult> GetList()
    {
        try { return Ok(await _service.GetListAsync()); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.GetList"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try { return Ok(await _service.GetDetalleAsync(id)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.GetDetalle"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // Consumida por OPT (y a futuro otras herramientas) para jalar automáticamente
    // los pasos vigentes del PETS seleccionado — sin este endpoint no hay forma de
    // "seleccionar PETS y que traiga los pasos".
    [HttpGet("{id:int}/pasos")]
    [RequireFeature("ssoma.gestion.pets", "ssoma.gestion.opt")]
    public async Task<IActionResult> GetPasos(int id)
    {
        try { return Ok(await _service.GetPasosAsync(id)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.GetPasos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> Crear([FromBody] CrearPetRequest request)
    {
        try { return StatusCode(201, new { id = await _service.CrearAsync(request) }); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.Crear"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPut("{id:int}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarPetRequest request)
    {
        try { await _service.ActualizarAsync(id, request); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.Actualizar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/pasos")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> AgregarPaso(int id, [FromBody] CrearPetPasoRequest request)
    {
        try { return StatusCode(201, new { id = await _service.AgregarPasoAsync(id, request) }); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.AgregarPaso"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPut("{id:int}/pasos/{pasoId:int}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> ActualizarPaso(int id, int pasoId, [FromBody] ActualizarPetPasoRequest request)
    {
        try { await _service.ActualizarPasoAsync(id, pasoId, request); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.ActualizarPaso"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpDelete("{id:int}/pasos/{pasoId:int}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> EliminarPaso(int id, int pasoId)
    {
        try { await _service.EliminarPasoAsync(id, pasoId); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.EliminarPaso"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPut("{id:int}/pasos/reordenar")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> ReordenarPasos(int id, [FromBody] ReordenarPasosRequest request)
    {
        try { await _service.ReordenarPasosAsync(id, request); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.ReordenarPasos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // Sube un .docx de un PETS ya existente y devuelve una vista previa de los pasos
    // detectados en su "PROCEDIMIENTO DE TRABAJO" (con imagen si el párrafo tenía una).
    // No guarda nada — el usuario revisa/edita en pantalla y recién confirma.
    [HttpPost("importar-docx/preview")]
    [RequireFeature("ssoma.gestion.pets")]
    [RequestSizeLimit(30_000_000)]
    [Consumes("multipart/form-data")]
    public IActionResult PreviewImportarDocx([FromForm] IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var preview = _importService.PreviewDesdeDocx(stream);
            return Ok(preview);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PetsController.PreviewImportarDocx");
            return StatusCode(500, new { message = "No se pudo leer el documento. Verifica que sea un .docx válido." });
        }
    }

    [HttpPost("{id:int}/importar-docx/confirmar")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> ConfirmarImportarDocx(int id, [FromBody] ConfirmarImportacionRequest request)
    {
        try
        {
            await _importService.ConfirmarImportacionAsync(id, request);
            return NoContent();
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.ConfirmarImportarDocx"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/pasos/{pasoId:int}/imagen")]
    [RequireFeature("ssoma.gestion.pets")]
    [RequestSizeLimit(20_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubirImagenPaso(int id, int pasoId, [FromForm] IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var url = await _service.SubirImagenPasoAsync(id, pasoId, stream, file.FileName);
            return Ok(new { imagenUrl = url });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.SubirImagenPaso"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // ── Secciones de texto único (Introducción / Alcance / Objetivo / Definiciones / Restricciones) ──

    [HttpPut("{id:int}/secciones-texto/{seccion}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> ActualizarSeccionTexto(int id, string seccion, [FromBody] ActualizarSeccionTextoRequest request)
    {
        try { await _service.UpsertSeccionTextoAsync(id, seccion, request.Contenido); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.ActualizarSeccionTexto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // ── Catálogo (Marco Legal / EPP / Recursos) ──────────────────────────────────

    [HttpGet("catalogo")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> GetCatalogo([FromQuery] string grupo, [FromQuery] string? tipo)
    {
        try { return Ok(await _service.GetCatalogoAsync(grupo, tipo)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.GetCatalogo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("catalogo")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> CrearCatalogoItem([FromBody] CrearCatalogoItemRequest request)
    {
        try { return StatusCode(201, new { id = await _service.CrearCatalogoItemAsync(request) }); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.CrearCatalogoItem"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // "Eliminar" del catálogo global: lo desactiva, deja de ofrecerse para futuras
    // selecciones sin romper los PETS que ya lo tenían seleccionado.
    [HttpDelete("catalogo/{catalogoItemId:int}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> DesactivarCatalogoItem(int catalogoItemId)
    {
        try { await _service.DesactivarCatalogoItemAsync(catalogoItemId); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.DesactivarCatalogoItem"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/seleccion")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> SeleccionarCatalogoItem(int id, [FromBody] SeleccionarItemCatalogoRequest request)
    {
        try { return StatusCode(201, new { id = await _service.SeleccionarCatalogoItemAsync(id, request) }); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.SeleccionarCatalogoItem"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/seleccion/personalizado")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> AgregarItemPersonalizado(int id, [FromBody] AgregarItemPersonalizadoRequest request)
    {
        try { return StatusCode(201, new { id = await _service.AgregarItemPersonalizadoAsync(id, request) }); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.AgregarItemPersonalizado"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // "Eliminar" para este PETS puntual: desactiva solo esta selección, sin tocar el catálogo global.
    [HttpDelete("{id:int}/seleccion/{seleccionId:int}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> EliminarSeleccion(int id, int seleccionId)
    {
        try { await _service.EliminarSeleccionAsync(id, seleccionId); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.EliminarSeleccion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // ── Anexos ────────────────────────────────────────────────────────────────

    [HttpPost("{id:int}/anexos")]
    [RequireFeature("ssoma.gestion.pets")]
    [RequestSizeLimit(30_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubirAnexo(int id, [FromForm] IFormFile file, [FromForm] string nombre)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var url = await _service.SubirAnexoAsync(id, nombre, stream, file.FileName);
            return StatusCode(201, new { archivoUrl = url });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.SubirAnexo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpDelete("{id:int}/anexos/{anexoId:int}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> EliminarAnexo(int id, int anexoId)
    {
        try { await _service.EliminarAnexoAsync(id, anexoId); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.EliminarAnexo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // ── Firmas (Elaborado por / Revisado por / Aprobado por) ──────────────────────

    [HttpPut("{id:int}/firmas/{rol}")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> ActualizarFirma(int id, string rol, [FromBody] ActualizarFirmaRequest request)
    {
        try { await _service.UpsertFirmaAsync(id, rol, request.Nombre, request.Cargo, request.Fecha); return NoContent(); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.ActualizarFirma"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/firmas/{rol}/imagen")]
    [RequireFeature("ssoma.gestion.pets")]
    [RequestSizeLimit(10_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubirFirma(int id, string rol, [FromForm] IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var url = await _service.SubirFirmaAsync(id, rol, stream, file.FileName);
            return Ok(new { firmaUrl = url });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.SubirFirma"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    // ── Exportar ──────────────────────────────────────────────────────────────

    [HttpGet("{id:int}/exportar-pdf")]
    [RequireFeature("ssoma.gestion.pets")]
    public async Task<IActionResult> ExportarPdf(int id)
    {
        try
        {
            var bytes = await _service.ExportarPdfAsync(id);
            // Sin fileDownloadName a propósito: eso fuerza "Content-Disposition: attachment"
            // (descarga). Así el navegador lo abre inline con su visor de PDF nativo.
            return File(bytes, "application/pdf");
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PetsController.ExportarPdf"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }
}
