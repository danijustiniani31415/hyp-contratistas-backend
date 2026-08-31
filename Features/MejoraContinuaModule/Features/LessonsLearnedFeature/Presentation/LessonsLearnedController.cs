using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Presentation
{
    [ApiController]
    [Route("api/v1/lesson")]
    public class LessonsLearnedController : ControllerBase
    {
        private readonly ILessonService _lessonService;
        private readonly IScopeService _scopeService;
        private readonly ExcelService _excelService;

        public LessonsLearnedController(
            ILessonService lessonService,
            IScopeService scopeService,
            ExcelService excelService)
        {
            _lessonService = lessonService;
            _scopeService = scopeService;
            _excelService = excelService;
        }

        /// <summary>Parsea un CSV "5,12,34" a List&lt;int&gt; (null si vacío).</summary>
        private static List<int>? ParseCsvInts(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return null;
            var list = csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList();
            return list.Count > 0 ? list : null;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetLessonsUsingFilter(
            [FromQuery] DateTimeOffset? periodDate,
            [FromQuery] int? stateId,
            [FromQuery] int? projectId,
            [FromQuery] int? areaId,
            [FromQuery] int? userId,
            [FromQuery] int? reviewerWorkerId,
            [FromQuery] string? catalogItemIds,
            [FromQuery] string? lessonAreaIds,
            [FromQuery] int? obraOficinaStaffId,
            [FromQuery] string? approvalStatus,
            [FromQuery] bool onlyMyPendingReview,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                List<int>? parsedCatalogIds = null;
                if (!string.IsNullOrWhiteSpace(catalogItemIds))
                {
                    parsedCatalogIds = catalogItemIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                        .Where(n => n.HasValue)
                        .Select(n => n!.Value)
                        .ToList();
                }

                // Reusa la sobrecarga interna del service via filter DTO (incluye catalogItemIds).
                var filter = new LessonFilterDTO
                {
                    PeriodDate = periodDate,
                    StateId = stateId,
                    ProjectId = projectId,
                    AreaId = areaId,
                    UserId = userId,
                    ReviewerWorkerId = reviewerWorkerId,
                    CatalogItemIds = parsedCatalogIds,
                    LessonAreaIds = ParseCsvInts(lessonAreaIds),
                    ObraOficinaStaffId = obraOficinaStaffId,
                    ApprovalStatus = approvalStatus,
                    OnlyMyPendingReview = onlyMyPendingReview,
                    CurrentUserId = int.Parse(userIdClaim.Value),
                    Page = page,
                    PageSize = pageSize
                };
                var pagedResult = await _lessonService.GetPagedWithFilters(filter);
                return Ok(pagedResult.Paged);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("paged-with-filters")]
        public async Task<IActionResult> GetPagedWithFilters(
            [FromQuery] DateTimeOffset? periodDate,
            [FromQuery] int? stateId,
            [FromQuery] int? projectId,
            [FromQuery] int? areaId,
            [FromQuery] int? userId,
            [FromQuery] int? reviewerWorkerId,
            [FromQuery] string? catalogItemIds,
            [FromQuery] string? lessonAreaIds,
            [FromQuery] int? obraOficinaStaffId,
            [FromQuery] string? approvalStatus,
            [FromQuery] bool onlyMyPendingReview,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                // catalogItemIds llega como CSV (ej. "5,12,34") para mantener simple
                // el contrato HTTP. Lo parseamos a List<int>.
                List<int>? parsedCatalogIds = null;
                if (!string.IsNullOrWhiteSpace(catalogItemIds))
                {
                    parsedCatalogIds = catalogItemIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                        .Where(n => n.HasValue)
                        .Select(n => n!.Value)
                        .ToList();
                }

                var filter = new LessonFilterDTO
                {
                    PeriodDate = periodDate,
                    StateId = stateId,
                    ProjectId = projectId,
                    AreaId = areaId,
                    UserId = userId,
                    ReviewerWorkerId = reviewerWorkerId,
                    CatalogItemIds = parsedCatalogIds,
                    LessonAreaIds = ParseCsvInts(lessonAreaIds),
                    ObraOficinaStaffId = obraOficinaStaffId,
                    ApprovalStatus = approvalStatus,
                    OnlyMyPendingReview = onlyMyPendingReview,
                    CurrentUserId = int.Parse(userIdClaim.Value),
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _lessonService.GetPagedWithFilters(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace, inner = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Estado de la ventana de subida de hoy. El frontend lo consulta al cargar
        /// la página para deshabilitar "Nuevo registro" durante la ventana de
        /// revisión de la jefatura (últimos 2 días hábiles del mes + fines de
        /// semana/feriados intermedios).
        /// </summary>
        [Authorize]
        [HttpGet("upload-window")]
        public async Task<IActionResult> GetUploadWindow()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var window = await _lessonService.GetUploadWindowAsync();
                return Ok(window);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] LessonCreateDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var lessonId = await _lessonService.CreateAsync(dto, userId);

                if (lessonId == null)
                    return BadRequest(new { message = "Escoger una relación válida" });

                return Ok(new { message = "Lección creada exitosamente" });
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

        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] LessonUpdateDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _lessonService.UpdateAsync(id, dto, userId);
                return Ok(new { message = "Lección actualizada exitosamente" });
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

        [Authorize]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _lessonService.ApproveAsync(id, userId);
                return Ok(new { message = "Lección aprobada exitosamente" });
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

        [Authorize]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] LessonRejectDTO? body)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _lessonService.RejectAsync(id, userId, body?.Comment);
                return Ok(new { message = "Lección rechazada exitosamente" });
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

        [Authorize]
        [HttpGet("filters/create")]
        public async Task<IActionResult> GetFiltersCreate([FromQuery] int lessonAreaId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                if (lessonAreaId <= 0)
                    return BadRequest(new { message = "Debe seleccionar un área" });

                var tree = await _scopeService.GetScopeForLessonAsync(lessonAreaId);
                if (tree == null || !tree.Any()) return NoContent();
                return Ok(tree);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetLessonsFilter(
            [FromQuery] string? period,
            [FromQuery] int? stateId,
            [FromQuery] int? projectId,
            [FromQuery] int? areaId,
            [FromQuery] int? userId,
            [FromQuery] string? lessonAreaIds,
            [FromQuery] int? obraOficinaStaffId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var result = await _lessonService.GetLessonsFilterAsync(period, stateId, projectId, areaId, userId, ParseCsvInts(lessonAreaIds), obraOficinaStaffId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var currentUserId = int.Parse(userIdClaim.Value);
                var data = await _lessonService.GetByIdAsync(id, currentUserId);
                if (data == null) return NotFound(new { message = "Lección no encontrada" });
                return Ok(data);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _lessonService.DeleteSoftAsync(id, userId);
                if (!result) return NotFound(new { message = "Lección no encontrada." });
                return Ok(new { message = "Lección eliminada exitosamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(
            [FromQuery] string? period,
            [FromQuery] int? stateId,
            [FromQuery] int? projectId,
            [FromQuery] int? areaId,
            [FromQuery] int? userId,
            [FromQuery] string? lessonAreaIds,
            [FromQuery] int? obraOficinaStaffId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "Inicie sesión" });

                var lessons = await _lessonService.GetLessonsFilterAsync(period, stateId, projectId, areaId, userId, ParseCsvInts(lessonAreaIds), obraOficinaStaffId);
                var fileBytes = await _excelService.GenerateLessonsExcel(lessons);

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Lecciones_Aprendidas.xlsx");
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
