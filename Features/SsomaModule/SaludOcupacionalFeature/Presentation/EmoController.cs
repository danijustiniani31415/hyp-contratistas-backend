using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.emos", "clinica.agenda")]
    public class EmoController : ControllerBase
    {
        private readonly IEmoService _service;
        private readonly ILogger<EmoController> _logger;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ISharePointHabService _sharePoint;

        public EmoController(
            IEmoService service,
            ILogger<EmoController> logger,
            IDbContextFactory<AppDbContext> factory,
            ISharePointHabService sharePoint)
        {
            _service = service;
            _logger = logger;
            _factory = factory;
            _sharePoint = sharePoint;
        }

        private int? CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        /// <summary>
        /// True solo si el token viene del portal de clínicas (ClinicaAuthController, tabla
        /// ss_clinica_usuario), que es el único que emite los claims tipo=CLINICA y clinicaId.
        /// No basta con <c>User.IsInRole(Roles.Clinica)</c>: el personal de Abril que tiene el rol
        /// 14 asignado solo para poder entrar a la agenda de clínica (es el único rol con acceso
        /// a la feature clinica.agenda) entra por el login normal, cuyo token no lleva clinicaId.
        /// Mismo criterio que <c>tipo == "CONTRATISTA"</c> en el módulo de Habilitación.
        /// </summary>
        private bool EsTokenDeClinica() => User.FindFirst("tipo")?.Value == "CLINICA";

        [HttpGet("emos")]
        public async Task<IActionResult> GetList([FromQuery] EmoFilterDto filter)
        {
            try { return Ok(await _service.ListPaged(filter)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("emos/por-trabajador")]
        public async Task<IActionResult> GetPorTrabajador([FromQuery] EmoPorTrabajadorFilterDto filter)
        {
            try { return Ok(await _service.ListPorTrabajador(filter)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Exporta a Excel la misma lista de "por-trabajador", respetando los filtros
        /// y el orden aplicados en pantalla (sin paginar).
        /// </summary>
        [HttpGet("emos/por-trabajador/excel")]
        public async Task<IActionResult> GetPorTrabajadorExcel([FromQuery] EmoPorTrabajadorFilterDto filter)
        {
            try
            {
                filter.Page = 1;
                filter.PageSize = int.MaxValue;
                var result = await _service.ListPorTrabajador(filter);

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var ws = workbook.AddWorksheet("EMOs por trabajador");

                var headers = new[]
                {
                    "Trabajador", "DNI", "Tipo EMO", "Empresa Actual", "Empresa Origen",
                    "Proyecto", "Fecha EMO", "Vencimiento", "Aptitud", "Estado", "Días"
                };
                for (int col = 1; col <= headers.Length; col++)
                {
                    var cell = ws.Cell(1, col);
                    cell.Value = headers[col - 1];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#003366");
                    cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                    cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                }

                int row = 2;
                foreach (var x in result.Data)
                {
                    ws.Cell(row, 1).Value = x.NombreCompleto;
                    ws.Cell(row, 2).Value = x.Dni;
                    ws.Cell(row, 3).Value = x.TipoEmo ?? string.Empty;
                    ws.Cell(row, 4).Value = x.Empresa ?? string.Empty;
                    ws.Cell(row, 5).Value = x.EmpresaOrigenNombre ?? string.Empty;
                    ws.Cell(row, 6).Value = x.ProyectoNombre ?? string.Empty;
                    ws.Cell(row, 7).Value = x.FechaEmo.HasValue ? x.FechaEmo.Value.ToString("dd/MM/yyyy") : string.Empty;
                    ws.Cell(row, 8).Value = x.FechaVencimiento.HasValue ? x.FechaVencimiento.Value.ToString("dd/MM/yyyy") : string.Empty;
                    ws.Cell(row, 9).Value = x.Aptitud ?? string.Empty;
                    ws.Cell(row, 10).Value = x.TieneEmo ? (x.Estado ?? string.Empty) : "Sin EMO";
                    ws.Cell(row, 11).Value = x.DiasRestantes.HasValue ? x.DiasRestantes.Value.ToString() : string.Empty;

                    if (row % 2 == 0)
                        ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#F5F5F5");
                    row++;
                }

                ws.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                var fileName = $"EMOs_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
                return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error exportando EMOs a Excel"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("emos/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try { return Ok(await _service.GetById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("workers/{workerId:int}/historial-emo")]
        public async Task<IActionResult> GetHistorial(int workerId)
        {
            try { return Ok(await _service.GetHistorialByWorker(workerId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("emos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create(
            [FromForm] EmoCreateDto dto,
            [FromForm] IFormFile? documentoInterconsulta,
            [FromForm] IFormFile? archivoLectura)
        {
            try
            {
                dto.DocumentoInterconsulta = documentoInterconsulta;
                dto.ArchivoLectura = archivoLectura;

                // Si quien registra es una clinica y no mando clinicaId explicito,
                // se liga el EMO a su propia clinica desde el inicio (evita que las
                // subidas de Aptitud/EMO Completo que siguen a la creacion choquen
                // con el chequeo de propiedad de SubirDocumento).
                if (dto.ClinicaId == null && EsTokenDeClinica())
                {
                    var clinicaIdClaim = User.FindFirst("clinicaId")?.Value;
                    if (int.TryParse(clinicaIdClaim, out var clinicaIdActual))
                        dto.ClinicaId = clinicaIdActual;
                }

                var result = await _service.Create(dto, CurrentUserId());
                return Ok(new { id = result.EmoId, interconsultaId = result.InterconsultaId, message = "EMO registrado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("emos/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmoUpdateDto dto)
        {
            try
            {
                await _service.Update(id, dto, CurrentUserId());
                return Ok(new { message = "EMO actualizado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("emos/{id:int}/estado")]
        public async Task<IActionResult> PatchEstado(int id, [FromBody] EmoEstadoPatchDto dto)
        {
            try
            {
                await _service.UpdateEstado(id, dto.Estado, CurrentUserId());
                return Ok(new { message = "Estado del EMO actualizado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Completa la lectura de un EMO marcado como "requiere lectura del médico de Abril"
        /// (subtab "Pendientes de lectura"): sube el adjunto y guarda la fecha, siguiendo el
        /// mismo proceso de aprobación que cuando la lectura la sube la clínica.
        /// </summary>
        [HttpPost("emos/{emoId:int}/lectura-abril")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompletarLecturaAbril(
            int emoId,
            [FromForm] IFormFile archivo,
            [FromForm] DateOnly fechaLectura)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                    throw new AbrilException("El archivo de lectura es obligatorio.", 400);

                using var ctx = _factory.CreateDbContext();
                var emo = await ctx.WorkerEmo
                    .Include(e => e.Worker).ThenInclude(w => w!.Person)
                    .FirstOrDefaultAsync(e => e.Id == emoId)
                    ?? throw new AbrilException("EMO no encontrado.", 404);

                var dni = emo.Worker?.Person?.DocumentIdentityCode ?? emo.WorkerId.ToString();
                var fecha = DateTime.UtcNow.ToString("yyyyMMdd");
                var fileName = $"{dni}_Lectura_{fecha}.pdf";

                string path;
                using (var stream = archivo.OpenReadStream())
                    path = await _sharePoint.SubirArchivoAsync(stream, fileName, "lectura-emo");

                await _service.CompletarLecturaAbril(emoId, fechaLectura, path, CurrentUserId());
                return Ok(new { url = path, message = "Lectura registrada y aprobada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController.CompletarLecturaAbril"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("emos/{emoId:int}/documentos")]
        public async Task<IActionResult> SubirDocumento(int emoId, [FromForm] IFormFile file, [FromForm] string tipo)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new AbrilException("El archivo es obligatorio.", 400);

                var tipoNorm = tipo?.Trim() ?? string.Empty;
                if (tipoNorm != "Aptitud" && tipoNorm != "EMO" && tipoNorm != "Lectura")
                    throw new AbrilException("El tipo debe ser 'Aptitud', 'EMO' o 'Lectura'.", 400);

                using var ctx = _factory.CreateDbContext();

                var emo = await ctx.WorkerEmo
                    .Include(e => e.Worker).ThenInclude(w => w!.Person)
                    .FirstOrDefaultAsync(e => e.Id == emoId)
                    ?? throw new AbrilException("EMO no encontrado.", 404);

                // Si el solicitante es una clínica, validar que el EMO le pertenezca. El alcance por
                // clínica solo aplica a quien entra por el portal de clínicas: el personal de Abril
                // que llega a la misma pantalla con el rol 14 no tiene una clínica "propia" contra
                // la que comparar, y ya ve la agenda completa de todas las clínicas.
                if (EsTokenDeClinica())
                {
                    var clinicaIdClaim = User.FindFirst("clinicaId")?.Value;
                    if (!int.TryParse(clinicaIdClaim, out var clinicaId))
                        throw new AbrilException("No tiene permiso para subir documentos de este EMO.", 403);

                    if (emo.ClinicaId == null)
                        // EMO sin clínica asignada todavía (dato legado o registrado sin vínculo):
                        // se liga a la clínica que sube el primer documento, en vez de bloquearla.
                        emo.ClinicaId = clinicaId;
                    else if (emo.ClinicaId != clinicaId)
                        throw new AbrilException("No tiene permiso para subir documentos de este EMO.", 403);
                }

                var dni = emo.Worker?.Person?.DocumentIdentityCode ?? emo.WorkerId.ToString();
                var fecha = DateTime.UtcNow.ToString("yyyyMMdd");
                var contexto = tipoNorm == "Aptitud" ? "emo-aptitud"
                             : tipoNorm == "Lectura" ? "lectura-emo"
                             : "emo-completo";
                var fileName = $"{dni}_{tipoNorm}_{fecha}.pdf";

                string path;
                using (var stream = file.OpenReadStream())
                    path = await _sharePoint.SubirArchivoAsync(stream, fileName, contexto);

                if (tipoNorm == "Aptitud")
                    emo.UrlAptitud = path;
                else if (tipoNorm == "Lectura")
                    emo.UrlResultado = path;
                else
                    emo.UrlEmoCompleto = path;

                await ctx.SaveChangesAsync();

                return Ok(new { url = path });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoController.SubirDocumento"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
