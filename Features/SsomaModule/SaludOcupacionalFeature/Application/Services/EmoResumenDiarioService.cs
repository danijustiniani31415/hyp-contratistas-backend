using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Shared;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class EmoResumenDiarioService : IEmoResumenDiarioService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmoResumenDiarioService> _logger;

        public EmoResumenDiarioService(
            IDbContextFactory<AppDbContext> factory,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<EmoResumenDiarioService> logger)
        {
            _factory = factory;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<EmoResumenDiarioResultDto> EnviarResumenDiario()
        {
            var result = new EmoResumenDiarioResultDto();
            using var ctx = _factory.CreateDbContext();

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5).Date);
            result.Fecha = hoy;

            // Query única: todas las programaciones de hoy con sus relaciones.
            // Excluye las dadas de baja (State = false) y las canceladas: el resumen es de
            // la vigilancia médica del día, y una cancelada no se atendió ni se dejó de
            // atender — antes se colaba como fila suelta y sumaba al "Total programados".
            var filas = await (
                from p in ctx.SsProgramacionEmo
                join w in ctx.Worker on p.WorkerId equals w.Id
                join t in ctx.SsEmoTipo on p.TipoEmoId equals t.Id into tj
                from t in tj.DefaultIfEmpty()
                join em in ctx.Contributor on p.EmpresaId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                join c in ctx.SsClinica on p.ClinicaId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                where p.FechaProgramada == hoy && p.State && p.Estado != "Cancelado"
                select new FilaProgramacion
                {
                    WorkerNombre = (w.Person != null ? w.Person.FullName : null) ?? "—",
                    WorkerDni = (w.Person != null ? w.Person.DocumentIdentityCode : null) ?? "—",
                    Empresa = em != null ? em.ContributorName : "—",
                    TipoEmo = t != null ? t.Nombre : "—",
                    Estado = p.Estado,
                    Clinica = c != null ? c.Nombre : "—",
                    EmoResultadoId = p.EmoResultadoId
                }
            ).AsNoTracking().ToListAsync();

            // Obtener aptitudes de WorkerEmo referenciados
            var emoIds = filas
                .Where(f => f.EmoResultadoId.HasValue)
                .Select(f => f.EmoResultadoId!.Value)
                .Distinct().ToList();

            if (emoIds.Count > 0)
            {
                var aptitudes = await ctx.WorkerEmo
                    .AsNoTracking()
                    .Where(e => emoIds.Contains(e.Id))
                    .ToDictionaryAsync(e => e.Id, e => e.Aptitud);

                foreach (var f in filas.Where(f => f.EmoResultadoId.HasValue))
                {
                    if (aptitudes.TryGetValue(f.EmoResultadoId!.Value, out var apt))
                        f.Aptitud = apt;
                }
            }

            // KPIs
            result.Atendidos = filas.Count(f => f.Estado == "Completado");
            result.Aptos = filas.Count(f => f.Estado == "Completado"
                && (f.Aptitud == "Apto" || f.Aptitud == "Apto con Restricciones"));
            result.ConInterconsulta = filas.Count(f => f.Estado == "Completado" && f.Aptitud == "Observado");
            result.NoAptos = filas.Count(f => f.Estado == "Completado" && f.Aptitud == "No Apto");
            result.NoSePresentaron = filas.Count(f => f.Estado == "No se presentó");
            result.Programados = filas.Count(f =>
                f.Estado == "Programado" || f.Estado == "Confirmado" ||
                f.Estado == "Aceptado por Clínica" || f.Estado == "En Atención");

            // Envío de email
            var destinatariosConfig = _configuration["EmoResumen:Destinatarios"];
            var emailEnviado = false;
            string? destinatariosStr = null;

            if (string.IsNullOrWhiteSpace(destinatariosConfig))
            {
                _logger.LogWarning("EmoResumen:Destinatarios no configurado. No se enviará resumen diario.");
            }
            else
            {
                var destinatarios = destinatariosConfig
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                destinatariosStr = string.Join(",", destinatarios);

                try
                {
                    var subject = $"Resumen Vigilancia Médica — {hoy:dd/MM/yyyy}";
                    var body = BuildBody(hoy, result, filas);
                    await _emailService.SendAsync(to: destinatarios, subject: subject, body: body, isHtml: true, sender: SaludOcupacionalEmailConstants.SenderKey);
                    emailEnviado = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando resumen diario de vigilancia médica");
                }
            }

            result.EmailEnviado = emailEnviado;

            // Log en ss_alertas_emo
            ctx.SsAlertaEmo.Add(new SsAlertaEmo
            {
                WorkerId = null,
                EmoId = null,
                TipoAlerta = "RESUMEN_DIARIO",
                FechaAlerta = hoy,
                EnviadoEmail = emailEnviado,
                FechaEnvio = emailEnviado ? DateTimeOffset.UtcNow : null,
                Destinatarios = destinatariosStr,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await ctx.SaveChangesAsync();

            return result;
        }

        private static string BuildBody(DateOnly fecha, EmoResumenDiarioResultDto kpis, List<FilaProgramacion> filas)
        {
            var rechazados = filas.Count(f => f.Estado == "Rechazado por Clínica");
            var total = filas.Count;

            var filasDet = string.Join("\n", filas.Select(f => $@"
                <tr>
                    <td style='border:1px solid #ddd;padding:7px;'>{f.WorkerNombre}</td>
                    <td style='border:1px solid #ddd;padding:7px;'>{f.WorkerDni}</td>
                    <td style='border:1px solid #ddd;padding:7px;'>{f.Empresa}</td>
                    <td style='border:1px solid #ddd;padding:7px;'>{f.TipoEmo}</td>
                    <td style='border:1px solid #ddd;padding:7px;'>{f.Estado}</td>
                    <td style='border:1px solid #ddd;padding:7px;'>{f.Aptitud ?? "—"}</td>
                    <td style='border:1px solid #ddd;padding:7px;'>{f.Clinica}</td>
                </tr>"));

            return $@"
            <p style='font-family:Arial,sans-serif;'>Estimados,</p>
            <p style='font-family:Arial,sans-serif;'>
                A continuación el <strong>resumen de vigilancia médica</strong> del día <strong>{fecha:dd/MM/yyyy}</strong>.
            </p>

            <table style='border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;margin-bottom:20px;'>
                <tr style='background:#1565C0;color:#fff;'>
                    <td style='padding:10px 16px;'><strong>Total programados</strong></td>
                    <td style='padding:10px 16px;'>{total}</td>
                </tr>
                <tr style='background:#f5f5f5;'>
                    <td style='padding:8px 16px;'>Atendidos</td>
                    <td style='padding:8px 16px;'>{kpis.Atendidos}</td>
                </tr>
                <tr>
                    <td style='padding:8px 16px;'>✔ Aptos</td>
                    <td style='padding:8px 16px;color:#2e7d32;'><strong>{kpis.Aptos}</strong></td>
                </tr>
                <tr style='background:#f5f5f5;'>
                    <td style='padding:8px 16px;'>⚠ Con interconsulta (Observado)</td>
                    <td style='padding:8px 16px;color:#e65100;'><strong>{kpis.ConInterconsulta}</strong></td>
                </tr>
                <tr>
                    <td style='padding:8px 16px;'>✘ No aptos</td>
                    <td style='padding:8px 16px;color:#b00020;'><strong>{kpis.NoAptos}</strong></td>
                </tr>
                <tr style='background:#f5f5f5;'>
                    <td style='padding:8px 16px;'>No se presentaron</td>
                    <td style='padding:8px 16px;'>{kpis.NoSePresentaron}</td>
                </tr>
                <tr>
                    <td style='padding:8px 16px;'>Pendientes al cierre</td>
                    <td style='padding:8px 16px;'>{kpis.Programados}</td>
                </tr>
                <tr style='background:#f5f5f5;'>
                    <td style='padding:8px 16px;'>Rechazados por clínica</td>
                    <td style='padding:8px 16px;'>{rechazados}</td>
                </tr>
            </table>

            {(filas.Count == 0 ? "<p style='font-family:Arial,sans-serif;color:#666;'>No hubo programaciones para este día.</p>" : $@"
            <table style='border-collapse:collapse;font-family:Arial,sans-serif;font-size:13px;width:100%;'>
                <thead>
                    <tr style='background:#1565C0;color:#fff;'>
                        <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Trabajador</th>
                        <th style='border:1px solid #ddd;padding:8px;text-align:left;'>DNI</th>
                        <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Empresa</th>
                        <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Tipo EMO</th>
                        <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Estado</th>
                        <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Aptitud</th>
                        <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Clínica</th>
                    </tr>
                </thead>
                <tbody>
                    {filasDet}
                </tbody>
            </table>")}

            <p style='font-size:12px;color:#666;font-family:Arial,sans-serif;margin-top:20px;'>
                Este resumen se genera automáticamente a las 4:30 pm (hora Lima).
            </p>";
        }

        private sealed class FilaProgramacion
        {
            public string WorkerNombre { get; set; } = string.Empty;
            public string WorkerDni { get; set; } = string.Empty;
            public string Empresa { get; set; } = string.Empty;
            public string TipoEmo { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public string Clinica { get; set; } = string.Empty;
            public int? EmoResultadoId { get; set; }
            public string? Aptitud { get; set; }
        }
    }
}
