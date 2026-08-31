using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos;
using Abril_Backend.Features.Contractors.ContractorManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Contractors.ContractorManagement.Presentation
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ContractorManagementController : ControllerBase
    {
        private readonly IContractorManagementService _service;

        public ContractorManagementController(IContractorManagementService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] ContributorFilterDto filter)
        {
            try
            {
                var result = await _service.GetPaged(filter);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{contractorId}/approve")]
        public async Task<IActionResult> Approve(int contractorId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _service.Approve(contractorId, userId);
                return Ok(new { message = "Contratista aprobado exitosamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{contractorId}/reject")]
        public async Task<IActionResult> Reject(int contractorId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _service.Reject(contractorId, userId);
                return Ok(new { message = "Contratista rechazado exitosamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{contractorId}/update-request/approve")]
        public async Task<IActionResult> ApproveUpdate(int contractorId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _service.ApproveUpdate(contractorId, userId);
                return Ok(new { message = "Actualización de datos aprobada exitosamente." });
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

        [HttpPatch("{contractorId}/update-request/reject")]
        public async Task<IActionResult> RejectUpdate(int contractorId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _service.RejectUpdate(contractorId, userId);
                return Ok(new { message = "Actualización de datos rechazada. Se conservan los datos anteriores." });
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

        [HttpPut("{contractorId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int contractorId, [FromForm] ContractorUpdateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _service.Update(contractorId, dto, userId);
                return Ok(new { message = "Contratista actualizado exitosamente." });
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

        [HttpPost("{contractorId}/send-credentials")]
        public async Task<IActionResult> SendCredentials(int contractorId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _service.SendCredentials(contractorId, userId);
                return Ok(new { message = "Credenciales enviadas exitosamente." });
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
