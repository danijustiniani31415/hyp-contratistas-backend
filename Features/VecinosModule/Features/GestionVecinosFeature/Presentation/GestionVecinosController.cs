using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Dtos;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Interfaces;
using Abril_Backend.Shared.Services.Reniec.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Presentation
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GestionVecinosController : ControllerBase
    {
        private readonly IGestionVecinosService _service;
        private readonly IReniecService _reniecService;
        private readonly ILogger<GestionVecinosController> _logger;

        public GestionVecinosController(
            IGestionVecinosService service,
            IReniecService reniecService,
            ILogger<GestionVecinosController> logger)
        {
            _service = service;
            _reniecService = reniecService;
            _logger = logger;
        }

        /// <summary>Carga inicial: opciones de filtros/formulario + primera página de vecinos.</summary>
        [HttpGet]
        public async Task<IActionResult> GetPageData([FromQuery] VecinoFilterDto filter)
        {
            try
            {
                var result = await _service.GetPageData(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Listado paginado/filtrado (sin opciones, ya cargadas en el load inicial).</summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetList([FromQuery] VecinoFilterDto filter)
        {
            try
            {
                var result = await _service.GetList(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS LIST: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Consulta de DNI a RENIEC para autocompletar el nombre del propietario.</summary>
        [HttpGet("dni/{dni}")]
        public async Task<IActionResult> GetByDni(string dni)
        {
            try
            {
                var result = await _reniecService.GetByDniAsync(dni);
                if (result is null)
                    return NotFound(new { message = "No se encontró información para el DNI proporcionado." });

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Registra uno o varios vecinos/departamentos sobre un lote (polígono) del croquis.</summary>
        [HttpPost]
        public async Task<IActionResult> RegisterVecinos([FromBody] VecinoLoteRegisterDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var result = await _service.RegisterVecinos(dto, userId);
                return Ok(new { vecinoLoteId = result.VecinoLoteId, vecinoIds = result.VecinoIds, message = "Vecinos registrados exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS REGISTER: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Devuelve una casa/propiedad con sus personas e imágenes (para refrescar el detalle).</summary>
        [HttpGet("{vecinoId:int}")]
        public async Task<IActionResult> GetById(int vecinoId)
        {
            try
            {
                var result = await _service.GetById(vecinoId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS GET BY ID: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Edita los datos de la casa/propiedad (sección Detalle) y reconcilia sus personas.</summary>
        [HttpPut("{vecinoId:int}")]
        public async Task<IActionResult> Update(int vecinoId, [FromBody] VecinoUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.Update(vecinoId, dto, userId);
                return Ok(new { message = "Vecino actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS UPDATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Edita los datos a nivel de lote (dirección + observaciones).</summary>
        [HttpPut("lotes/{vecinoLoteId:int}")]
        public async Task<IActionResult> UpdateLote(int vecinoLoteId, [FromBody] VecinoLoteUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.UpdateLote(vecinoLoteId, dto, userId);
                return Ok(new { message = "Lote actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS LOTE UPDATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Elimina (soft delete) una imagen del estado de la propiedad.</summary>
        [HttpDelete("imagenes/{imagenId:int}")]
        public async Task<IActionResult> DeleteImagen(int imagenId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.DeleteImagen(imagenId, userId);
                return Ok(new { message = "Imagen eliminada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS IMAGEN DELETE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Sube una o varias imágenes del estado de la propiedad de una casa/lote.</summary>
        [HttpPost("{vecinoId:int}/imagenes")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImagenes(int vecinoId, [FromForm] IFormFileCollection files)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var imagenes = await _service.UploadImagenes(vecinoId, files, userId);
                return Ok(new { imagenes, message = "Imágenes subidas exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS IMAGENES UPLOAD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Dashboard: resumen general + métricas por proyecto (vecinos, solicitudes, compromisos).</summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var result = await _service.GetDashboard();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS DASHBOARD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Solicitudes ─────────────────────────────────────────────────────
        [HttpGet("{vecinoId:int}/solicitudes")]
        public async Task<IActionResult> GetSolicitudes(int vecinoId)
        {
            try
            {
                var result = await _service.GetSolicitudes(vecinoId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS SOLICITUDES GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("{vecinoId:int}/solicitudes")]
        public async Task<IActionResult> CreateSolicitud(int vecinoId, [FromBody] VecinoSolicitudCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var solicitudId = await _service.CreateSolicitud(vecinoId, dto, userId);
                return Ok(new { vecinoSolicitudId = solicitudId, message = "Solicitud registrada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS SOLICITUD CREATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("solicitudes/{solicitudId:int}/estado")]
        public async Task<IActionResult> UpdateSolicitudEstado(int solicitudId, [FromBody] VecinoSolicitudEstadoUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.UpdateSolicitudEstado(solicitudId, dto.VecinoSolicitudEstadoId, userId);
                return Ok(new { message = "Estado actualizado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS SOLICITUD ESTADO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Edita el texto de una solicitud ya registrada.</summary>
        [HttpPatch("solicitudes/{solicitudId:int}/descripcion")]
        public async Task<IActionResult> UpdateSolicitudDescripcion(int solicitudId, [FromBody] VecinoSolicitudDescripcionUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.UpdateSolicitudDescripcion(solicitudId, dto.Descripcion, userId);
                return Ok(new { message = "Solicitud actualizada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS SOLICITUD DESCRIPCION: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Compromisos ─────────────────────────────────────────────────────
        [HttpGet("solicitudes/{solicitudId:int}/compromisos")]
        public async Task<IActionResult> GetCompromisos(int solicitudId)
        {
            try
            {
                var result = await _service.GetCompromisos(solicitudId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS COMPROMISOS GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("solicitudes/{solicitudId:int}/compromisos")]
        public async Task<IActionResult> CreateCompromiso(int solicitudId, [FromBody] VecinoCompromisoCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var compromisoId = await _service.CreateCompromiso(solicitudId, dto, userId);
                return Ok(new { vecinoCompromisoId = compromisoId, message = "Compromiso registrado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS COMPROMISO CREATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("compromisos/{compromisoId:int}/estado")]
        public async Task<IActionResult> UpdateCompromisoEstado(int compromisoId, [FromBody] VecinoCompromisoEstadoUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.UpdateCompromisoEstado(compromisoId, dto.VecinoCompromisoEstadoId, userId);
                return Ok(new { message = "Estado actualizado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS COMPROMISO ESTADO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("compromisos/{compromisoId:int}/observaciones")]
        public async Task<IActionResult> UpdateCompromisoObservaciones(int compromisoId, [FromBody] VecinoCompromisoObservacionesUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.UpdateCompromisoObservaciones(compromisoId, dto.Observaciones, userId);
                return Ok(new { message = "Observaciones actualizadas." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS COMPROMISO OBSERVACIONES: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("compromisos/{compromisoId:int}/fecha-municipalidad")]
        public async Task<IActionResult> UpdateCompromisoFechaMunicipalidad(int compromisoId, [FromBody] VecinoCompromisoFechaMunicipalidadUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.UpdateCompromisoFechaMunicipalidad(compromisoId, dto.FechaFinMunicipalidad, userId);
                return Ok(new { message = "Fecha límite por municipalidad/fiscalización actualizada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS COMPROMISO FECHA MUNICIPALIDAD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("compromisos/entregables/{entregableId:int}/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadEntregable(int entregableId, IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var archivoUrl = await _service.UploadEntregable(entregableId, file, userId);
                return Ok(new { archivoUrl, message = "Entregable subido exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS ENTREGABLE UPLOAD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("compromisos/{compromisoId:int}/normativas")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadNormativas(int compromisoId, [FromForm] IFormFileCollection files)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var normativas = await _service.UploadNormativas(compromisoId, files, userId);
                return Ok(new { normativas, message = "Archivos subidos exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS NORMATIVAS UPLOAD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpDelete("compromisos/normativas/{normativaId:int}")]
        public async Task<IActionResult> DeleteNormativa(int normativaId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.DeleteNormativa(normativaId, userId);
                return Ok(new { message = "Archivo eliminado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS NORMATIVA DELETE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Calendario de limpiezas ────────────────────────────────────────────
        [HttpGet("proyectos/{projectId:int}/limpiezas")]
        public async Task<IActionResult> GetLimpiezas(int projectId, [FromQuery] int year, [FromQuery] int month)
        {
            try
            {
                var result = await _service.GetLimpiezas(projectId, year, month);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS LIMPIEZAS GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("proyectos/{projectId:int}/limpiezas")]
        public async Task<IActionResult> CreateLimpieza(int projectId, [FromBody] VecinoLimpiezaCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var limpieza = await _service.CreateLimpieza(projectId, dto, userId);
                return Ok(new { limpieza, message = "Limpieza registrada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS LIMPIEZA CREATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("proyectos/{projectId:int}/limpiezas/cumplimiento")]
        public async Task<IActionResult> GetLimpiezaCumplimiento(int projectId)
        {
            try
            {
                var result = await _service.GetCumplimiento(projectId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS LIMPIEZA CUMPLIMIENTO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Compromisos (de las solicitudes de un vecino) para relacionar la atención de limpieza.</summary>
        [HttpGet("{vecinoId:int}/compromisos-select")]
        public async Task<IActionResult> GetCompromisosSelect(int vecinoId)
        {
            try
            {
                var result = await _service.GetCompromisosSelect(vecinoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS COMPROMISOS SELECT: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("limpiezas/{limpiezaId:int}/atencion")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAtencion(int limpiezaId, [FromForm] IFormFile file, [FromForm] int? vecinoCompromisoId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var archivoUrl = await _service.UploadAtencion(limpiezaId, file, vecinoCompromisoId, userId);
                return Ok(new { archivoUrl, message = "Atención de limpieza registrada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS ATENCION UPLOAD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpDelete("limpiezas/{limpiezaId:int}")]
        public async Task<IActionResult> DeleteLimpieza(int limpiezaId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.DeleteLimpieza(limpiezaId, userId);
                return Ok(new { message = "Limpieza eliminada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS LIMPIEZA DELETE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Requisitos ───────────────────────────────────────────────────────
        [HttpGet("{vecinoId:int}/requisitos")]
        public async Task<IActionResult> GetRequisitos(int vecinoId)
        {
            try
            {
                var result = await _service.GetRequisitos(vecinoId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS REQUISITOS GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("{vecinoId:int}/requisitos/{tipoId:int}/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadRequisito(int vecinoId, int tipoId, IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                var archivoUrl = await _service.UploadRequisito(vecinoId, tipoId, file, userId);
                return Ok(new { archivoUrl, message = "Requisito subido exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS REQUISITO UPLOAD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{vecinoId:int}/requisitos/{tipoId:int}/no-aplica")]
        public async Task<IActionResult> SetRequisitoNoAplica(int vecinoId, int tipoId, [FromBody] VecinoRequisitoNoAplicaDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.SetRequisitoNoAplica(vecinoId, tipoId, dto.NoAplica, userId);
                return Ok(new { message = "Estado actualizado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS REQUISITO NO APLICA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("compromisos/entregables/{entregableId:int}/estado")]
        public async Task<IActionResult> UpdateEntregableEstado(int entregableId, [FromBody] VecinoEntregableEstadoUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

                await _service.UpdateEntregableEstado(entregableId, dto.VecinoEntregableEstadoId, userId);
                return Ok(new { message = "Estado actualizado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR GESTION VECINOS ENTREGABLE ESTADO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
