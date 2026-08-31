using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Dtos;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Presentation
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _service;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(IInvoiceService service, ILogger<InvoiceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Carga inicial: desplegables (proveedores, formas de pago) + primera página de facturas.</summary>
        [HttpGet("init")]
        public async Task<IActionResult> GetInit([FromQuery] InvoiceFilterDto filter)
        {
            try
            {
                if (filter.Page < 1) filter.Page = 1;
                var result = await _service.GetInit(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE INIT: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] InvoiceFilterDto filter)
        {
            try
            {
                var result = await _service.GetPaged(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE PAGED: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Carga inicial del dashboard: desplegables de filtros + datos de los gráficos.</summary>
        /// <summary>Facturas agrupadas por razón social de Abril (vista de bloques).</summary>
        [HttpGet("blocks")]
        public async Task<IActionResult> GetBlocks([FromQuery] InvoiceFilterDto filter)
        {
            try
            {
                var result = await _service.GetBlocks(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE BLOCKS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("dashboard/init")]
        public async Task<IActionResult> GetDashboardInit([FromQuery] InvoiceFilterDto filter)
        {
            try
            {
                var result = await _service.GetDashboardInit(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE DASHBOARD INIT: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] InvoiceFilterDto filter)
        {
            try
            {
                var result = await _service.GetDashboard(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE DASHBOARD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Sube/asocia el documento de una factura existente (multipart: file).</summary>
        [HttpPost("{invoiceId:int}/document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(int invoiceId, IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                var url = await _service.UploadDocument(invoiceId, file, userId.Value);
                return Ok(new { message = "Documento adjuntado exitosamente.", documentUrl = url });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE UPLOAD DOC: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("{invoiceId:int}/sign")]
        [Authorize(Roles = Roles.ContabilidadFirmante)]
        public async Task<IActionResult> Sign(int invoiceId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                var signedDocumentUrl = await _service.Sign(invoiceId, userId.Value);
                return Ok(new { message = "Documento firmado generado exitosamente.", signedDocumentUrl });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE SIGN: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("{invoiceId:int}")]
        public async Task<IActionResult> GetDetail(int invoiceId)
        {
            try
            {
                var result = await _service.GetDetail(invoiceId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE DETAIL: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update([FromForm] InvoiceUpdateDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                await _service.Update(dto, userId.Value);
                return Ok(new { message = "Factura actualizada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE UPDATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Importación masiva desde el Excel de órdenes de pago. Recibe los registros ya
        /// parseados por el frontend (recordsJson) y los archivos arrastrados (files).
        /// </summary>
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Import([FromForm] string recordsJson, [FromForm] IFormFileCollection files)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                List<InvoiceImportRowDto>? rows;
                try
                {
                    rows = System.Text.Json.JsonSerializer.Deserialize<List<InvoiceImportRowDto>>(
                        recordsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    return BadRequest(new { message = "Los registros enviados no son válidos." });
                }

                var result = await _service.Import(rows ?? new(), files, userId.Value);
                return Ok(new
                {
                    message = $"Se registraron {result.Inserted} facturas ({result.WithFile} con documento, {result.WithoutFile} sin documento).",
                    result
                });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE IMPORT: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] InvoiceCreateDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                await _service.Create(dto, userId.Value);
                return Ok(new { message = "Factura registrada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE CREATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Aprueba en bloque las facturas seleccionadas.</summary>
        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromBody] InvoiceBulkActionDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                var count = await _service.Approve(dto.InvoiceIds, userId.Value);
                return Ok(new { message = $"Se aprobó(aron) {count} factura(s).", count });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE APPROVE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Rechaza en bloque las facturas seleccionadas.</summary>
        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromBody] InvoiceBulkActionDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                var count = await _service.Reject(dto.InvoiceIds, userId.Value);
                return Ok(new { message = $"Se rechazó(aron) {count} factura(s).", count });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE REJECT: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Observa en bloque las facturas seleccionadas con un motivo del catálogo.</summary>
        [HttpPost("observe")]
        public async Task<IActionResult> Observe([FromBody] InvoiceBulkActionDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                var count = await _service.Observe(dto.InvoiceIds, dto.InvoiceObservationReasonId ?? 0, userId.Value);
                return Ok(new { message = $"Se observó(aron) {count} factura(s).", count });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE OBSERVE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Consulta de RUC a SUNAT para el modal de alta de proveedor.</summary>
        [HttpGet("ruc/{ruc}")]
        public async Task<IActionResult> GetByRuc(string ruc)
        {
            try
            {
                var result = await _service.GetByRuc(ruc);
                if (result is null)
                    return NotFound(new { message = "No se encontró información para el RUC proporcionado." });

                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE RUC: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Alta de un proveedor (contribuyente) desde el modal de consulta RUC.</summary>
        [HttpPost("supplier")]
        public async Task<IActionResult> CreateSupplier([FromBody] InvoiceSupplierCreateDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión." });

                var supplier = await _service.CreateSupplier(dto, userId.Value);
                return Ok(supplier);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR INVOICE SUPPLIER CREATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : null;
        }
    }
}
