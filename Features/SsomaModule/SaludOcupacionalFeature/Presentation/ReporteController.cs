using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/reportes")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.reportes")]
    public class ReporteController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<ReporteController> _logger;

        public ReporteController(IDbContextFactory<AppDbContext> factory, ILogger<ReporteController> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        [HttpGet("sunafil-mensual")]
        public async Task<IActionResult> SunafilMensual([FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                if (mes < 1 || mes > 12)
                    return BadRequest(new { message = "El mes debe estar entre 1 y 12." });
                if (anio < 2000 || anio > 2100)
                    return BadRequest(new { message = "El año es inválido." });

                using var ctx = _factory.CreateDbContext();

                // ── 1. Query principal ──────────────────────────────────────────
                var emos = await (
                    from e in ctx.WorkerEmo
                    join w in ctx.Worker on e.WorkerId equals w.Id
                    join emp in ctx.Contributor on e.EmpresaOrigenId equals emp.ContributorId into empj
                    from emp in empj.DefaultIfEmpty()
                    join tipo in ctx.SsEmoTipo on e.TipoEmoId equals tipo.Id into tipoj
                    from tipo in tipoj.DefaultIfEmpty()
                    join cli in ctx.SsClinica on e.ClinicaId equals cli.Id into clij
                    from cli in clij.DefaultIfEmpty()
                    join med in ctx.SsMedicoOcupacional on e.MedicoId equals med.Id into medj
                    from med in medj.DefaultIfEmpty()
                    where e.FechaEmo.Month == mes && e.FechaEmo.Year == anio
                    orderby e.FechaEmo, w.Person != null ? w.Person.FullName : null
                    select new
                    {
                        Emo = e,
                        Worker = w,
                        WorkerNombre = w.Person != null ? w.Person.FullName : null,
                        WorkerDni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                        EmpresaNombre = emp != null ? emp.ContributorName : null,
                        TipoNombre = tipo != null ? tipo.Nombre : null,
                        ClinicaNombre = cli != null ? cli.Nombre : null,
                        MedicoNombre = med != null ? med.ApellidoNombre : null
                    }
                ).AsNoTracking().ToListAsync();

                // ── 2. Restricciones por EMO ────────────────────────────────────
                var emoIds = emos.Select(x => x.Emo.Id).ToList();
                var restriccionesPorEmo = new Dictionary<int, List<string>>();
                if (emoIds.Count > 0)
                {
                    var restricciones = await ctx.SsEmoRestriccion
                        .Include(r => r.RestriccionTipo)
                        .Where(r => emoIds.Contains(r.EmoId) && r.Vigente)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var r in restricciones)
                    {
                        var texto = r.RestriccionTipo?.Descripcion ?? r.DescripcionLibre ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(texto)) continue;
                        if (!restriccionesPorEmo.ContainsKey(r.EmoId))
                            restriccionesPorEmo[r.EmoId] = new List<string>();
                        restriccionesPorEmo[r.EmoId].Add(texto);
                    }
                }

                // ── 3. Proyecto vía WorkerVinculacion ───────────────────────────
                var workerIds = emos.Select(x => x.Emo.WorkerId).Distinct().ToList();
                var proyectoPorWorker = new Dictionary<int, string>();
                if (workerIds.Count > 0)
                {
                    var vinculaciones = await ctx.WorkerVinculacion
                        .Where(v => workerIds.Contains(v.WorkerId) && v.FechaFin == null)
                        .OrderByDescending(v => v.CreatedAt)
                        .ThenByDescending(v => v.Id)
                        .AsNoTracking()
                        .ToListAsync();

                    var vincPorWorker = vinculaciones
                        .GroupBy(v => v.WorkerId)
                        .ToDictionary(g => g.Key, g => g.First());

                    var proyectoIds = vinculaciones
                        .Where(v => v.ProyectoId.HasValue)
                        .Select(v => v.ProyectoId!.Value)
                        .Distinct()
                        .ToList();

                    if (proyectoIds.Count > 0)
                    {
                        var proyectos = await ctx.Project
                            .Where(p => proyectoIds.Contains(p.ProjectId))
                            .AsNoTracking()
                            .ToDictionaryAsync(p => p.ProjectId);

                        foreach (var (wId, vinc) in vincPorWorker)
                        {
                            if (vinc.ProyectoId.HasValue && proyectos.TryGetValue(vinc.ProyectoId.Value, out var proy))
                                proyectoPorWorker[wId] = proy.ProjectDescription;
                        }
                    }
                }

                // ── 4. Generar Excel ────────────────────────────────────────────
                using var workbook = new XLWorkbook();
                var ws = workbook.AddWorksheet("Registro EMOs");

                var headers = new[]
                {
                    "N°", "Trabajador", "DNI", "Empresa", "Proyecto",
                    "Tipo EMO", "Fecha EMO", "Vencimiento", "Aptitud", "Estado",
                    "Clínica", "Médico", "Restricciones", "Observaciones"
                };

                for (int col = 1; col <= headers.Length; col++)
                {
                    var cell = ws.Cell(1, col);
                    cell.Value = headers[col - 1];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003366");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                int row = 2;
                foreach (var x in emos)
                {
                    var fv = x.Emo.FechaVencimientoCalculada ?? x.Emo.FechaVencimiento;
                    proyectoPorWorker.TryGetValue(x.Emo.WorkerId, out var proyNombre);
                    restriccionesPorEmo.TryGetValue(x.Emo.Id, out var restricList);
                    var restricText = restricList is { Count: > 0 } ? string.Join(", ", restricList) : string.Empty;

                    ws.Cell(row, 1).Value = row - 1;
                    ws.Cell(row, 2).Value = x.WorkerNombre ?? string.Empty;
                    ws.Cell(row, 3).Value = x.WorkerDni ?? string.Empty;
                    ws.Cell(row, 4).Value = x.EmpresaNombre ?? string.Empty;
                    ws.Cell(row, 5).Value = proyNombre ?? string.Empty;
                    ws.Cell(row, 6).Value = x.TipoNombre ?? string.Empty;
                    ws.Cell(row, 7).Value = x.Emo.FechaEmo.ToString("dd/MM/yyyy");
                    ws.Cell(row, 8).Value = fv.HasValue ? fv.Value.ToString("dd/MM/yyyy") : string.Empty;
                    ws.Cell(row, 9).Value = x.Emo.Aptitud ?? string.Empty;
                    ws.Cell(row, 10).Value = x.Emo.Estado;
                    ws.Cell(row, 11).Value = x.ClinicaNombre ?? string.Empty;
                    ws.Cell(row, 12).Value = x.MedicoNombre ?? string.Empty;
                    ws.Cell(row, 13).Value = restricText;
                    ws.Cell(row, 14).Value = x.Emo.Notas ?? string.Empty;

                    if (row % 2 == 0)
                    {
                        var dataRow = ws.Range(row, 1, row, headers.Length);
                        dataRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
                    }

                    row++;
                }

                ws.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                ws.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                var bytes = ms.ToArray();

                var fileName = $"ReporteEMO_{mes:D2}_{anio}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte SUNAFIL mensual mes={Mes} anio={Anio}", mes, anio);
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
