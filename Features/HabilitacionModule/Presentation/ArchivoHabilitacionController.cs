using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Archivos;
using Abril_Backend.Features.Habilitacion.Application.Dtos.HabEmpresa;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Helpers;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/archivos")]
    [Authorize]
    public class ArchivoHabilitacionController : ControllerBase
    {
        private readonly ISharePointHabService _sharePoint;
        private readonly IHabTrabajadorRepository _habTrabajadorRepo;
        private readonly IHabEmpresaRepository _habEmpresaRepo;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<ArchivoHabilitacionController> _logger;

        public ArchivoHabilitacionController(
            ISharePointHabService sharePoint,
            IHabTrabajadorRepository habTrabajadorRepo,
            IHabEmpresaRepository habEmpresaRepo,
            IDbContextFactory<AppDbContext> factory,
            ILogger<ArchivoHabilitacionController> logger)
        {
            _sharePoint = sharePoint;
            _habTrabajadorRepo = habTrabajadorRepo;
            _habEmpresaRepo = habEmpresaRepo;
            _factory = factory;
            _logger = logger;
        }

        [HttpGet("ver")]
        public async Task<IActionResult> Ver([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return BadRequest(new { message = "Parámetro 'url' requerido." });

                var path = Uri.UnescapeDataString(url);
                var libraryCtx = path.StartsWith("PASO/", StringComparison.OrdinalIgnoreCase) ? "paso-evidencias" : null;
                var downloadUrl = await _sharePoint.GetDownloadUrlAsync(path, libraryCtx);
                if (string.IsNullOrWhiteSpace(downloadUrl))
                    return NotFound(new { message = "Archivo no encontrado." });

                return Redirect(downloadUrl);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ArchivoHabilitacionController.Ver"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx", ".zip", ".txt"
        };

        private const long MaxFileSize = 10_000_000_000;

        [HttpPost("subir")]
        [RequestSizeLimit(MaxFileSize)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Subir([FromForm] SubirArchivoRequest request)
        {
            try
            {
                var file = request.File;
                var contexto = request.Contexto;
                _logger.LogInformation("Subir: FileName={FileName}, Length={Length}, ContentType={ContentType}",
                    file?.FileName, file?.Length, file?.ContentType);

                if (file is null || file.Length == 0)
                {
                    _logger.LogWarning("Subir 400: archivo nulo o vacío");
                    return BadRequest(new { message = "No se recibió ningún archivo." });
                }

                if (file.Length > MaxFileSize)
                {
                    _logger.LogWarning("Subir 400: tamaño {Length} supera límite", file.Length);
                    return BadRequest(new { message = "El archivo excede el tamaño máximo de 10 GB." });
                }

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(ext) || !ExtensionesPermitidas.Contains(ext))
                {
                    _logger.LogWarning("Subir 400: extensión no permitida '{Ext}'", ext);
                    return BadRequest(new { message = "Tipo de archivo no permitido. Use PDF, imágenes o documentos Office." });
                }

                using var stream = file.OpenReadStream();
                var path = await _sharePoint.SubirArchivoAsync(stream, file.FileName, contexto);
                var realUrl = await _sharePoint.GetDownloadUrlAsync(path);

                return Ok(new { path, url = realUrl });
            }
            catch (AbrilException ex) { _logger.LogError(ex, "Error en Subir (AbrilException {StatusCode})", ex.StatusCode); return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en Subir"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("url")]
        public async Task<IActionResult> GetUrl([FromQuery] string path, [FromQuery] string? ctx = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return BadRequest(new { message = "Parámetro 'path' requerido." });

                var decodedPath = Uri.UnescapeDataString(path);
                var libraryCtxUrl = !string.IsNullOrWhiteSpace(ctx) ? ctx
                    : decodedPath.StartsWith("PASO/", StringComparison.OrdinalIgnoreCase) ? "paso-evidencias"
                    : null;
                var downloadUrl = await _sharePoint.GetDownloadUrlAsync(decodedPath, libraryCtxUrl);
                if (string.IsNullOrWhiteSpace(downloadUrl))
                    return NotFound(new { message = "Archivo no encontrado." });

                return Ok(new { url = downloadUrl });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ArchivoHabilitacionController.GetUrl"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("subir-multiple")]
        [RequestSizeLimit(MaxFileSize)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubirMultiple([FromForm] SubirArchivoRequest request)
        {
            try
            {
                var file = request.File;
                if (file is null || file.Length == 0)
                    return BadRequest(new { message = "No se recibió ningún archivo." });
                if (file.Length > MaxFileSize)
                    return BadRequest(new { message = "El archivo excede el tamaño máximo." });
                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(ext) || !ExtensionesPermitidas.Contains(ext))
                    return BadRequest(new { message = "Tipo de archivo no permitido." });

                using var stream = file.OpenReadStream();
                var path = await _sharePoint.SubirArchivoAsync(stream, file.FileName, request.Contexto);

                string? zipContenido = null;
                if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var zipStream = file.OpenReadStream();
                        using var zip = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
                        var entradas = zip.Entries
                            .Select(e => new { nombre = e.FullName, tamaño = e.Length })
                            .ToList();
                        zipContenido = System.Text.Json.JsonSerializer.Serialize(entradas);
                    }
                    catch { /* si falla la extracción, no bloqueamos la subida */ }
                }

                return Ok(new
                {
                    path,
                    nombreArchivo = file.FileName,
                    esZip = ext.Equals(".zip", StringComparison.OrdinalIgnoreCase),
                    zipContenido
                });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SubirMultiple"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("enviar")]
        public async Task<IActionResult> Enviar([FromBody] EnviarDocumentoRequest request)
        {
            try
            {
                if (request.Archivos is null || request.Archivos.Count == 0)
                    return BadRequest(new { message = "Debes adjuntar al menos un archivo." });

                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int? userId = userIdClaim != null && int.TryParse(userIdClaim.Value, out var uid) ? uid : null;
                int? empresaIdClaim = null;
                if (esContratista)
                {
                    var ec = User.FindFirst("empresaId")?.Value;
                    if (int.TryParse(ec, out var eid)) empresaIdClaim = eid;
                    userId = null;
                }

                using var ctx = _factory.CreateDbContext();

                if (request.HabTrabajadorId.HasValue)
                {
                    var requiereVigencia = await ctx.SsHabTrabajador
                        .Where(h => h.Id == request.HabTrabajadorId.Value)
                        .Select(h => h.Item != null ? h.Item.RequiereVigencia : false)
                        .FirstOrDefaultAsync();
                    if (requiereVigencia && !request.Vigencia.HasValue)
                        return BadRequest(new { message = "Este documento requiere fecha de vigencia." });
                }

                if (request.HabEmpresaId.HasValue)
                {
                    var requiereVigencia = await ctx.SsHabEmpresa
                        .Where(h => h.Id == request.HabEmpresaId.Value)
                        .Select(h => h.Item != null ? h.Item.RequiereVigencia : false)
                        .FirstOrDefaultAsync();
                    if (requiereVigencia && !request.Vigencia.HasValue)
                        return BadRequest(new { message = "Este documento requiere fecha de vigencia." });
                }

                int? habTrabajadorId = request.HabTrabajadorId;
                int? habEmpresaId = request.HabEmpresaId;
                int? habEquipoId = request.HabEquipoId;

                var primerArchivo = request.Archivos[0];

                int versionActual = await ctx.SsHabDocumentoVersion
                    .CountAsync(v =>
                        (habTrabajadorId.HasValue && v.HabTrabajadorId == habTrabajadorId) ||
                        (habEmpresaId.HasValue    && v.HabEmpresaId    == habEmpresaId) ||
                        (habEquipoId.HasValue     && v.HabEquipoId     == habEquipoId));

                var version = new SsHabDocumentoVersion
                {
                    HabTrabajadorId = habTrabajadorId,
                    HabEmpresaId = habEmpresaId,
                    HabEquipoId = habEquipoId,
                    Version = versionActual + 1,
                    ArchivoUrl = primerArchivo.ArchivoUrl,
                    SubidoPorUserId = userId,
                    SubidoPorEmpresaId = empresaIdClaim,
                    EstadoAlSubir = "Enviado",
                    Enviado = true,
                    FechaEnvio = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                ctx.SsHabDocumentoVersion.Add(version);
                await ctx.SaveChangesAsync();

                for (int i = 0; i < request.Archivos.Count; i++)
                {
                    var a = request.Archivos[i];
                    ctx.SsHabDocumentoArchivo.Add(new SsHabDocumentoArchivo
                    {
                        VersionId = version.Id,
                        ArchivoUrl = a.ArchivoUrl,
                        NombreArchivo = a.NombreArchivo,
                        EsZip = a.EsZip,
                        ZipContenido = a.ZipContenido,
                        Orden = i,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (habTrabajadorId.HasValue)
                {
                    var ent = await ctx.SsHabTrabajador.FindAsync(habTrabajadorId.Value);
                    if (ent != null)
                    {
                        var nuevaVigencia = request.Vigencia.HasValue
                            ? DateTime.SpecifyKind(request.Vigencia.Value, DateTimeKind.Utc)
                            : (DateTime?)null;

                        // ¿Es una RENOVACIÓN? El documento ya estaba Aprobado y su vigencia sigue vigente.
                        // En ese caso NO se pisa el estado ni la vigencia anterior: pasa a "Renovando" y la
                        // nueva fecha se guarda como propuesta, para no dejar al trabajador sin habilitación
                        // mientras Abril revisa. En cualquier otro caso (primera subida, vencido) → "Enviado".
                        var esRenovacion = string.Equals(ent.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase)
                            && ent.Vigencia.HasValue
                            && ent.Vigencia.Value > DateTime.UtcNow;

                        ent.ArchivoUrl = primerArchivo.ArchivoUrl;
                        if (esRenovacion)
                        {
                            ent.Estado = "Renovando";
                            ent.VigenciaPropuesta = nuevaVigencia; // se aplicará a Vigencia al aprobar
                            _logger.LogInformation("[Enviar] Renovación: habTrabajadorId={Id} conserva vigencia {Vig}, propuesta {Prop}", habTrabajadorId, ent.Vigencia, nuevaVigencia);
                        }
                        else
                        {
                            ent.Estado = "Enviado";
                            if (nuevaVigencia.HasValue)
                            {
                                ent.Vigencia = nuevaVigencia;
                                _logger.LogInformation("[Enviar] Vigencia asignada: {Vigencia} para habTrabajadorId={Id}", ent.Vigencia, habTrabajadorId);
                            }
                            else
                            {
                                _logger.LogWarning("[Enviar] Vigencia NO recibida para habTrabajadorId={Id}", habTrabajadorId);
                            }
                        }
                        if (!string.IsNullOrEmpty(request.ObsContratista))
                            ent.ObsContratista = request.ObsContratista;
                        ent.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (habEmpresaId.HasValue)
                {
                    var source = await ctx.SsHabEmpresa.FindAsync(habEmpresaId.Value)
                        ?? throw new AbrilException("Entregable de empresa no encontrado.", 404);

                    SsHabEmpresa ent;

                    if (request.Mes.HasValue && request.Anio.HasValue)
                    {
                        // Item mensual: buscar o crear el registro del mes correcto
                        var mesVal = request.Mes.Value;
                        var anioVal = request.Anio.Value;
                        var vigenciaCalculada = HabilitacionDateHelper.ResolverVigenciaAlEnviar(
                            source.ItemId, true, mesVal, anioVal, request.Vigencia);
                        var updateDto = new EmpresaEntregableUpdateDto
                        {
                            Estado = "Enviado",
                            ArchivoUrl = primerArchivo.ArchivoUrl,
                            ObsContratista = request.ObsContratista,
                            Vigencia = vigenciaCalculada
                        };
                        ent = await _habEmpresaRepo.CrearOActualizarEntregableMesAsync(
                            source.EmpresaId, source.ProyectoId, source.ItemId,
                            mesVal, anioVal,
                            updateDto, userId, empresaIdClaim);
                    }
                    else
                    {
                        // Item no mensual: flujo original
                        ent = source;
                        ent.Estado = "Enviado";
                        ent.ArchivoUrl = primerArchivo.ArchivoUrl;
                        if (!string.IsNullOrEmpty(request.ObsContratista))
                            ent.ObsContratista = request.ObsContratista;
                        ent.Vigencia = HabilitacionDateHelper.ResolverVigenciaAlEnviar(
                            ent.ItemId, false, null, null, request.Vigencia);
                        ent.UpdatedAt = DateTime.UtcNow;
                    }

                    // La versión apunta al registro real (puede diferir de habEmpresaId si es mensual nuevo)
                    version.HabEmpresaId = ent.Id;
                }
                else if (habEquipoId.HasValue)
                {
                    var ent = await ctx.SsHabEquipo.FindAsync(habEquipoId.Value);
                    if (ent != null)
                    {
                        ent.Estado = "Enviado";
                        ent.ArchivoUrl = primerArchivo.ArchivoUrl;
                        if (!string.IsNullOrEmpty(request.ObsContratista))
                            ent.ObsContratista = request.ObsContratista;
                        ent.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await ctx.SaveChangesAsync();

                return Ok(new { versionId = version.Id, archivos = request.Archivos.Count });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en Enviar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Sirve el contenido de un archivo de SharePoint a partir de su `webUrl` (guardado
        /// tal cual en BD por SubirArchivoYObtenerUrlAsync), sin redirigir al navegador a
        /// SharePoint. Así funciona para cualquier usuario logueado en la intranet, sin
        /// depender de que tenga sesión interactiva de Microsoft 365 en el navegador.
        /// </summary>
        [HttpGet("proxy")]
        public async Task<IActionResult> Proxy([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return BadRequest(new { message = "Parámetro 'url' requerido." });

                var webUrl = Uri.UnescapeDataString(url);
                var archivo = await _sharePoint.DescargarPorWebUrlAsync(webUrl);
                if (archivo is null)
                    return NotFound(new { message = "Archivo no encontrado." });

                return File(archivo.Value.Bytes, archivo.Value.ContentType, archivo.Value.FileName);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ArchivoHabilitacionController.Proxy"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("descargar")]
        [AllowAnonymous]
        public async Task<IActionResult> Descargar([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return BadRequest(new { message = "Parámetro 'url' requerido." });

                var path = Uri.UnescapeDataString(url);
                var libraryCtxDl = path.StartsWith("PASO/", StringComparison.OrdinalIgnoreCase) ? "paso-evidencias" : null;
                var downloadUrl = await _sharePoint.GetDownloadUrlAsync(path, libraryCtxDl);
                if (string.IsNullOrWhiteSpace(downloadUrl))
                    return NotFound(new { message = "Archivo no encontrado." });

                Response.Headers["Content-Disposition"] = "attachment";
                return Redirect(downloadUrl);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ArchivoHabilitacionController.Descargar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
