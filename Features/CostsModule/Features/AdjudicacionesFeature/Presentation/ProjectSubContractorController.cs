using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Interfaces;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos;

namespace Abril_Backend.Features.Adjudicaciones.Presentation
{

    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProjectSubContractorController : ControllerBase
    {
        IProjectSubContractorService _projectSubContractorService;
        public ProjectSubContractorController(IProjectSubContractorService projectSubContractorService)
        {
            _projectSubContractorService = projectSubContractorService;
        }

        private const string RolAdministrador  = Roles.CostosAdministrador;
        private const string RolOficinaCentral = Roles.CostosOficinaCentral;
        private const string RolOficinaTecnica = Roles.CostosOficinaTecnica;

        /// <summary>
        /// Oficina Técnica (sin Admin ni Of. Central) solo ve adjudicaciones de sus proyectos
        /// (aquellos donde su correo está registrado en staff_project_email).
        /// </summary>
        private bool RestrictToOwnProjects()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return roles.Contains(RolOficinaTecnica)
                && !roles.Contains(RolAdministrador)
                && !roles.Contains(RolOficinaCentral);
        }

        [Authorize]
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int? projectId,
            [FromQuery] string? contributorName,
            [FromQuery] string? contributorRuc,
            [FromQuery] int? contractTypeId,
            [FromQuery] int? contractModalityId,
            [FromQuery] int? paymentMethodId,
            [FromQuery] int? projectSubContractorStatusId,
            [FromQuery] int? createdUserId,
            [FromQuery] int page = 1)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var filter = new ProjectSubContractorFilterDTO
                {
                    ProjectId = projectId,
                    ContributorName = contributorName,
                    ContributorRuc = contributorRuc,
                    ContractTypeId = contractTypeId,
                    ContractModalityId = contractModalityId,
                    PaymentMethodId = paymentMethodId,
                    ProjectSubContractorStatusId = projectSubContractorStatusId,
                    CreatedUserId = createdUserId,
                    Page = page
                };

                var userId = int.Parse(userIdClaim.Value);
                var result = await _projectSubContractorService.GetPaged(filter, userId, RestrictToOwnProjects());
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] int? projectId,
            [FromQuery] int? contractTypeId,
            [FromQuery] int? contractModalityId,
            [FromQuery] int? paymentMethodId,
            [FromQuery] int? projectSubContractorStatusId,
            [FromQuery] bool includeFilters = true)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var filter = new ProjectSubContractorFilterDTO
                {
                    ProjectId = projectId,
                    ContractTypeId = contractTypeId,
                    ContractModalityId = contractModalityId,
                    PaymentMethodId = paymentMethodId,
                    ProjectSubContractorStatusId = projectSubContractorStatusId
                };

                var userId = int.Parse(userIdClaim.Value);
                var result = await _projectSubContractorService.GetDashboard(filter, includeFilters, userId, RestrictToOwnProjects());
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] ProjectSubContractorCreateDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                await _projectSubContractorService.Create(dto, userId);
                return Ok(new { message = "Adjudicación creada exitosamente" });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("form-data")]
        public async Task<IActionResult> GetFormData()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                var data = await _projectSubContractorService.GetFormData();

                return Ok(data);
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("paged-with-filters")]
        public async Task<IActionResult> GetPagedWithFilters(
            [FromQuery] int? projectId,
            [FromQuery] string? contributorName,
            [FromQuery] string? contributorRuc,
            [FromQuery] int? contractTypeId,
            [FromQuery] int? contractModalityId,
            [FromQuery] int? paymentMethodId,
            [FromQuery] int? projectSubContractorStatusId,
            [FromQuery] int? createdUserId,
            [FromQuery] int page = 1)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var filter = new ProjectSubContractorFilterDTO
                {
                    ProjectId = projectId,
                    ContributorName = contributorName,
                    ContributorRuc = contributorRuc,
                    ContractTypeId = contractTypeId,
                    ContractModalityId = contractModalityId,
                    PaymentMethodId = paymentMethodId,
                    ProjectSubContractorStatusId = projectSubContractorStatusId,
                    CreatedUserId = createdUserId,
                    Page = page
                };

                var userId = int.Parse(userIdClaim.Value);
                var result = await _projectSubContractorService.GetPagedWithFilters(filter, userId, RestrictToOwnProjects());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [Authorize]
        [HttpPatch("{id}/info")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateInfo(int id, [FromForm] ProjectSubContractorUpdateInfoDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.UpdateInfo(id, dto, userId);
                return Ok(new { message = "Información actualizada exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPatch("{id}/dates")]
        public async Task<IActionResult> SaveDates(int id, [FromBody] UpdateDatesDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.SaveDates(id, dto, userId);
                return Ok(new { message = "Fechas guardadas exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("{id}/documents/{documentType}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(int id, string documentType, IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                if (!Enum.TryParse<AdjudicacionDocumentType>(documentType, ignoreCase: true, out var docType))
                    return BadRequest(new { message = $"Tipo de documento inválido: '{documentType}'. Valores válidos: {string.Join(", ", Enum.GetNames<AdjudicacionDocumentType>())}" });

                var result = await _projectSubContractorService.UploadDocumentAsync(id, docType, file, userId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPatch("{id}/documents/{documentType}/status")]
        public async Task<IActionResult> UpdateDocumentStatus(int id, string documentType, [FromBody] UpdateDocumentStatusDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                if (!Enum.TryParse<AdjudicacionDocumentType>(documentType, ignoreCase: true, out var docType))
                    return BadRequest(new { message = $"Tipo de documento inválido: '{documentType}'." });

                await _projectSubContractorService.UpdateDocumentStatusAsync(id, docType, dto.StatusId, dto.Observation, userId);
                return Ok(new { message = "Estado actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("{id}/documents/{documentType}/send-observation-email")]
        public async Task<IActionResult> SendObservationEmail(int id, string documentType, [FromBody] SendObservationEmailDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                if (!Enum.TryParse<AdjudicacionDocumentType>(documentType, ignoreCase: true, out var docType))
                    return BadRequest(new { message = $"Tipo de documento inválido: '{documentType}'." });

                await _projectSubContractorService.SendObservationEmailAsync(id, docType, dto, userId);
                return Ok(new { message = "Correo de observaciones enviado exitosamente." });
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
        [HttpPost("{id}/send-all-observations-email")]
        public async Task<IActionResult> SendAllObservationsEmail(int id, [FromBody] SendAllObservationsEmailDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                await _projectSubContractorService.SendAllObservationsEmailAsync(id, dto, userId);
                return Ok(new { message = "Correo de observaciones enviado exitosamente." });
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
        [HttpPost("{id}/send-contract-review-email")]
        public async Task<IActionResult> SendContractReviewEmail(int id, [FromBody] SendAllObservationsEmailDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                await _projectSubContractorService.SendContractReviewEmailAsync(id, dto, userId);
                return Ok(new { message = "Correo de revisión enviado a Costos exitosamente." });
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
        [HttpPost("{id}/send-all-levantamiento-email")]
        public async Task<IActionResult> SendAllLevantamientoEmail(int id, [FromBody] SendAllObservationsEmailDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                await _projectSubContractorService.SendAllLevantamientoEmailAsync(id, dto, userId);
                return Ok(new { message = "Correo de levantamiento de observaciones enviado exitosamente." });
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
        [HttpPost("{id}/generate/{documentType}")]
        public async Task<IActionResult> GenerateDocument(int id, string documentType)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                if (!Enum.TryParse<AdjudicacionDocumentType>(documentType, ignoreCase: true, out var docType))
                    return BadRequest(new { message = $"Tipo de documento inválido: '{documentType}'. Valores válidos: {string.Join(", ", Enum.GetNames<AdjudicacionDocumentType>())}" });

                var result = await _projectSubContractorService.GenerateDocumentAsync(id, docType, userId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.UpdateStatusAsync(id, dto.ProjectSubContractorStatusId, userId);
                return Ok(new { message = "Estado actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPatch("{id}/arrival-option")]
        public async Task<IActionResult> SetArrivalOption(int id, [FromBody] SetArrivalOptionDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.SetArrivalOptionAsync(id, dto.ArrivedWithObservations, userId);
                return Ok(new { message = "Opción de llegada guardada." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("{id}/confirm-step5")]
        public async Task<IActionResult> ConfirmStep5(int id, [FromBody] ConfirmStep5DTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.ConfirmStep5Async(id, dto.ArrivedWithObservations, dto.ArrivalObservation, dto.GraphAccessToken, userId);
                return Ok(new { message = "Recepción confirmada exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("{id}/send-sc-notification")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendScNotification(int id, IFormFile? file, [FromForm] string graphAccessToken)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.SendScNotificationAsync(id, graphAccessToken, file, userId);
                return Ok(new { message = "Correo enviado al subcontratista exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPatch("{id}/step6-checks")]
        public async Task<IActionResult> UpdateStep6Checks(int id, [FromBody] UpdateStep6ChecksDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.UpdateStep6ChecksAsync(id, dto, userId);
                return Ok(new { message = "Firmas actualizadas exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("{id}/send-step5-observations-email")]
        public async Task<IActionResult> SendStep5ObservationsEmail(int id, [FromBody] SendStep5ObservationsEmailDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.SendStep5ObservationsEmailAsync(id, dto, userId);
                return Ok(new { message = "Correo de observaciones enviado a Oficina Técnica exitosamente." });
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
        [HttpPost("{id}/send-step5-levantamiento-email")]
        public async Task<IActionResult> SendStep5LevantamientoEmail(int id, [FromBody] SendStep5LevantamientoEmailDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.SendStep5LevantamientoEmailAsync(id, dto, userId);
                return Ok(new { message = "Correo de levantamiento de observaciones enviado a Costos exitosamente." });
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
        [HttpPost("{id}/send-step6-notification")]
        public async Task<IActionResult> SendStep6Notification(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.SendStep6NotificationAsync(id, userId);
                return Ok(new { message = "Paso 6 confirmado exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("{id}/send-step8-notification")]
        public async Task<IActionResult> SendStep8Notification(int id, [FromBody] SendStep8NotificationDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.SendStep8NotificationAsync(id, dto.GraphAccessToken, userId);
                return Ok(new { message = "Correo enviado a Staff de Obra exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("{id}/advance-to-step4")]
        public async Task<IActionResult> AdvanceToStep4(int id, [FromBody] AdvanceToStep4Dto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                if (string.IsNullOrWhiteSpace(dto?.GraphAccessToken))
                    return BadRequest(new { message = "Falta el token de Microsoft Graph del usuario autenticado." });

                var userId = int.Parse(userIdClaim.Value);
                await _projectSubContractorService.AdvanceToStep4Async(id, dto.GraphAccessToken, userId);
                return Ok(new { message = "Adjudicación aprobada y Staff de Obra notificado." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("{id}/generate-contract-package")]
        public async Task<IActionResult> GenerateContractPackage(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _projectSubContractorService.GenerateContractPackageAsync(id, userId);

                // Exponer la URL guardada en SharePoint para que el frontend la pueda leer
                Response.Headers["Access-Control-Expose-Headers"] = "X-Package-Url,X-Package-Filename";
                Response.Headers["X-Package-Url"]      = result.FileUrl;
                Response.Headers["X-Package-Filename"] = Uri.EscapeDataString(result.OriginalFileName);

                return File(result.Bytes, "application/pdf", result.OriginalFileName);
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log completo para diagnóstico
                Console.Error.WriteLine($"[GenerateContractPackage] Exception: {ex}");
                return StatusCode(500, new { message = $"Error generando paquete: {ex.Message}" });
            }
        }

        [Authorize]
        [HttpGet("{id}/notification-recipients")]
        public async Task<IActionResult> GetNotificationRecipients(int id)
        {
            try
            {
                var recipients = await _projectSubContractorService.GetNotificationRecipients(id);
                return Ok(recipients);
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
        [HttpPost("send-notification")]
        public async Task<IActionResult> SendNotification([FromBody] SendAdjudicacionNotificationDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                await _projectSubContractorService.SendNotification(dto, userId);
                return Ok(new { message = "Notificación enviada exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = falta configurar correos del proyecto).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}