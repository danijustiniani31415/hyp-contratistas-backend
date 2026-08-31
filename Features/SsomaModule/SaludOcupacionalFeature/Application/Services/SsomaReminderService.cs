using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Shared;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class SsomaReminderResult
    {
        public int TotalEnviados { get; set; }
        public int TotalErrores  { get; set; }
        public List<string> Detalles { get; set; } = [];
    }

    public interface ISsomaReminderService
    {
        Task<SsomaReminderResult> ProcesarAlertasAsync();
    }

    public class SsomaReminderService : ISsomaReminderService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _emailService;
        private readonly ILogger<SsomaReminderService> _logger;

        // Cada alerta se reenvía como máximo una vez cada REENVIO_DIAS días.
        // Si ya existe un registro de la misma (tipo, referencia) dentro de esa ventana → omitir.
        private const int ReenvioAlertaDias = 7;

        // Umbrales para considerar una situación como "pendiente de atención"
        private const int DiasAccidenteSinAlta        = 14; // accidente abierto sin alta médica
        private const int DiasDescansoVencidoSinAlta  = 1;  // descanso cuya fecha_fin ya pasó sin alta
        private const int DiasReinduccionPendiente    = 5;  // reinducción no completada post-accidente
        private const int DiasCasoSocialSinSeguimiento = 15; // caso social sin ningún seguimiento reciente

        public SsomaReminderService(
            IDbContextFactory<AppDbContext> factory,
            IEmailService emailService,
            ILogger<SsomaReminderService> logger)
        {
            _factory = factory;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<SsomaReminderResult> ProcesarAlertasAsync()
        {
            var result = new SsomaReminderResult();
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5).Date);

            // Cargar alertas enviadas en la ventana de reenvío para evitar duplicados
            var ventana = hoy.AddDays(-ReenvioAlertaDias);
            var alertasRecientes = await ctx.SsAlertaSsoma
                .AsNoTracking()
                .Where(a => a.FechaAlerta >= ventana && a.EnviadoEmail)
                .Select(a => new { a.TipoAlerta, a.ReferenciaId })
                .ToListAsync();
            var alertasSet = alertasRecientes
                .Select(a => $"{a.TipoAlerta}:{a.ReferenciaId}")
                .ToHashSet();

            var nuevasAlertas = new List<SsAlertaSsoma>();

            await ProcesarAccidentesSinAlta(ctx, hoy, alertasSet, nuevasAlertas, result);
            await ProcesarDescansosVencidos(ctx, hoy, alertasSet, nuevasAlertas, result);
            await ProcesarReinduccionesPendientes(ctx, hoy, alertasSet, nuevasAlertas, result);
            await ProcesarCasosSocialesSinSeguimiento(ctx, hoy, alertasSet, nuevasAlertas, result);

            if (nuevasAlertas.Count > 0)
            {
                ctx.SsAlertaSsoma.AddRange(nuevasAlertas);
                await ctx.SaveChangesAsync();
            }

            return result;
        }

        // ── 1. Accidentes abiertos sin alta médica ───────────────────────────
        private async Task ProcesarAccidentesSinAlta(
            AppDbContext ctx, DateOnly hoy,
            HashSet<string> alertasSet, List<SsAlertaSsoma> nuevas,
            SsomaReminderResult result)
        {
            var limite = hoy.AddDays(-DiasAccidenteSinAlta);

            var candidatos = await (
                from a in ctx.SsAccidenteTrabajo
                join w in ctx.Worker on a.WorkerId equals w.Id
                where a.Estado == "Abierto"
                    && a.FechaAlta == null
                    && a.FechaAccidente <= limite
                select new { Accidente = a, Worker = w }
            ).AsNoTracking().ToListAsync();

            var proyectoIds = candidatos
                .Where(c => c.Accidente.ProyectoId.HasValue)
                .Select(c => c.Accidente.ProyectoId!.Value)
                .Distinct().ToList();

            var proyectosDict = await BuildProyectosDestinatariosAsync(ctx, proyectoIds);

            foreach (var c in candidatos)
            {
                var key = $"ACCIDENTE_SIN_ALTA:{c.Accidente.Id}";
                if (alertasSet.Contains(key)) continue;

                proyectosDict.TryGetValue(c.Accidente.ProyectoId ?? 0, out var proyecto);
                var destinatarios = BuildDestinatarios(c.Worker, proyecto);
                if (destinatarios.Count == 0)
                {
                    result.Detalles.Add($"Accidente #{c.Accidente.Id} — sin destinatarios, omitido.");
                    continue;
                }

                var workerNombre = c.Worker.Person?.FullName ?? $"ID {c.Worker.Id}";
                var diasSinAlta  = hoy.DayNumber - c.Accidente.FechaAccidente.DayNumber;
                var subject      = $"⚠️ Accidente sin alta médica ({diasSinAlta} días) — {workerNombre}";
                var body         = BuildBodyAccidenteSinAlta(c.Accidente.Id, workerNombre, c.Accidente.FechaAccidente, diasSinAlta, c.Accidente.TipoAccidente);

                await EnviarYRegistrar(key, hoy, destinatarios, subject, body, nuevas, result,
                    $"Accidente #{c.Accidente.Id} ({workerNombre}, {diasSinAlta}d sin alta)");
            }
        }

        // ── 2. Descansos aprobados con fecha_fin vencida sin alta ─────────────
        private async Task ProcesarDescansosVencidos(
            AppDbContext ctx, DateOnly hoy,
            HashSet<string> alertasSet, List<SsAlertaSsoma> nuevas,
            SsomaReminderResult result)
        {
            var candidatos = await (
                from d in ctx.SsDescansoMedico
                join w in ctx.Worker on d.WorkerId equals w.Id
                where d.Estado == "Aprobado"
                    && d.FechaAlta == null
                    && d.FechaFin < hoy
                    && d.State
                select new { Descanso = d, Worker = w }
            ).AsNoTracking().ToListAsync();

            // Filtrar: fecha_fin vencida hace al menos DiasDescansoVencidoSinAlta
            candidatos = candidatos
                .Where(c => hoy.DayNumber - c.Descanso.FechaFin.DayNumber >= DiasDescansoVencidoSinAlta)
                .ToList();

            var proyectoIds = candidatos
                .Where(c => c.Descanso.ProyectoId.HasValue)
                .Select(c => c.Descanso.ProyectoId!.Value)
                .Distinct().ToList();

            var proyectosDict = await BuildProyectosDestinatariosAsync(ctx, proyectoIds);

            foreach (var c in candidatos)
            {
                var key = $"DESCANSO_VENCIDO:{c.Descanso.Id}";
                if (alertasSet.Contains(key)) continue;

                proyectosDict.TryGetValue(c.Descanso.ProyectoId ?? 0, out var proyecto);
                var destinatarios = BuildDestinatarios(c.Worker, proyecto);
                if (destinatarios.Count == 0)
                {
                    result.Detalles.Add($"Descanso #{c.Descanso.Id} — sin destinatarios, omitido.");
                    continue;
                }

                var workerNombre = c.Worker.Person?.FullName ?? $"ID {c.Worker.Id}";
                var diasVencido  = hoy.DayNumber - c.Descanso.FechaFin.DayNumber;
                var subject      = $"⚠️ Descanso médico vencido sin alta ({diasVencido}d) — {workerNombre}";
                var body         = BuildBodyDescansoVencido(c.Descanso.Id, workerNombre, c.Descanso.FechaInicio, c.Descanso.FechaFin, diasVencido, c.Descanso.Diagnostico);

                await EnviarYRegistrar(key, hoy, destinatarios, subject, body, nuevas, result,
                    $"Descanso #{c.Descanso.Id} ({workerNombre}, vencido hace {diasVencido}d)");
            }
        }

        // ── 3. Reinducción post-accidente pendiente ──────────────────────────
        private async Task ProcesarReinduccionesPendientes(
            AppDbContext ctx, DateOnly hoy,
            HashSet<string> alertasSet, List<SsAlertaSsoma> nuevas,
            SsomaReminderResult result)
        {
            var limite = hoy.AddDays(-DiasReinduccionPendiente);

            var candidatos = await (
                from a in ctx.SsAccidenteTrabajo
                join w in ctx.Worker on a.WorkerId equals w.Id
                where a.RequiereReinduccion
                    && !a.ReinduccionCompletada
                    && a.FechaAccidente <= limite
                select new { Accidente = a, Worker = w }
            ).AsNoTracking().ToListAsync();

            var proyectoIds = candidatos
                .Where(c => c.Accidente.ProyectoId.HasValue)
                .Select(c => c.Accidente.ProyectoId!.Value)
                .Distinct().ToList();

            var proyectosDict = await BuildProyectosDestinatariosAsync(ctx, proyectoIds);

            foreach (var c in candidatos)
            {
                var key = $"REINDUCCION_PENDIENTE:{c.Accidente.Id}";
                if (alertasSet.Contains(key)) continue;

                proyectosDict.TryGetValue(c.Accidente.ProyectoId ?? 0, out var proyecto);
                var destinatarios = BuildDestinatarios(c.Worker, proyecto);
                if (destinatarios.Count == 0)
                {
                    result.Detalles.Add($"Accidente #{c.Accidente.Id} reinducción — sin destinatarios, omitido.");
                    continue;
                }

                var workerNombre  = c.Worker.Person?.FullName ?? $"ID {c.Worker.Id}";
                var diasPendiente = hoy.DayNumber - c.Accidente.FechaAccidente.DayNumber;
                var subject       = $"⚠️ Reinducción pendiente ({diasPendiente}d) — {workerNombre}";
                var body          = BuildBodyReinduccion(c.Accidente.Id, workerNombre, c.Accidente.FechaAccidente, diasPendiente);

                await EnviarYRegistrar(key, hoy, destinatarios, subject, body, nuevas, result,
                    $"Reinducción accidente #{c.Accidente.Id} ({workerNombre}, {diasPendiente}d pendiente)");
            }
        }

        // ── 4. Casos sociales abiertos sin seguimiento reciente ──────────────
        private async Task ProcesarCasosSocialesSinSeguimiento(
            AppDbContext ctx, DateOnly hoy,
            HashSet<string> alertasSet, List<SsAlertaSsoma> nuevas,
            SsomaReminderResult result)
        {
            var limiteApertura    = hoy.AddDays(-DiasCasoSocialSinSeguimiento);
            var limiteSeguimiento = DateTimeOffset.UtcNow.AddHours(-5).AddDays(-DiasCasoSocialSinSeguimiento);

            var candidatos = await (
                from c in ctx.SsCasoSocial
                join w in ctx.Worker on c.WorkerId equals w.Id
                where c.Estado != "Cerrado"
                    && c.State
                    && c.FechaApertura <= limiteApertura
                select new
                {
                    Caso   = c,
                    Worker = w,
                    UltimoSeguimiento = c.Seguimientos
                        .OrderByDescending(s => s.CreatedAt)
                        .Select(s => (DateTimeOffset?)s.CreatedAt)
                        .FirstOrDefault(),
                }
            ).AsNoTracking().ToListAsync();

            var sinSeguimiento = candidatos
                .Where(x => x.UltimoSeguimiento == null || x.UltimoSeguimiento < limiteSeguimiento)
                .ToList();

            var proyectoIds = sinSeguimiento
                .Where(c => c.Caso.ProyectoId.HasValue)
                .Select(c => c.Caso.ProyectoId!.Value)
                .Distinct().ToList();

            var proyectosDict = await BuildProyectosDestinatariosAsync(ctx, proyectoIds);

            foreach (var c in sinSeguimiento)
            {
                var key = $"CASO_SOCIAL_SIN_SEGUIMIENTO:{c.Caso.Id}";
                if (alertasSet.Contains(key)) continue;

                proyectosDict.TryGetValue(c.Caso.ProyectoId ?? 0, out var proyecto);
                var destinatarios = BuildDestinatarios(c.Worker, proyecto);
                if (destinatarios.Count == 0)
                {
                    result.Detalles.Add($"Caso social {c.Caso.Id} — sin destinatarios, omitido.");
                    continue;
                }

                var workerNombre = c.Worker.Person?.FullName ?? $"ID {c.Worker.Id}";
                var diasSinSeg   = c.UltimoSeguimiento.HasValue
                    ? (int)(DateTimeOffset.UtcNow.AddHours(-5) - c.UltimoSeguimiento.Value).TotalDays
                    : hoy.DayNumber - c.Caso.FechaApertura.DayNumber;
                var subject = $"⚠️ Caso social sin seguimiento ({diasSinSeg}d) — {workerNombre}";
                var body    = BuildBodyCasoSocial(c.Caso.Id, workerNombre, c.Caso.TipoCaso, c.Caso.Prioridad, diasSinSeg);

                await EnviarYRegistrar(key, hoy, destinatarios, subject, body, nuevas, result,
                    $"Caso social {c.Caso.Id} ({workerNombre}, {diasSinSeg}d sin seguimiento)");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Emails de un proyecto para efectos de destinatarios de alertas SSOMA. El
        /// residente sale de su ficha de trabajador (project.residente_workers_id →
        /// workers.email_corporativo), igual que EmoDestinatariosResolver, en vez de la
        /// columna Project.EmailResidente (deprecada, se desactualiza cuando cambia el
        /// residente).
        /// </summary>
        private sealed class ProyectoDestinatarios
        {
            public int ProjectId { get; set; }
            public string? EmailResidente { get; set; }
            public string? EmailResponsable { get; set; }
            public string? EmailRrhh { get; set; }
            public string? EmailCoordSsoma { get; set; }
            public string? EmailCoordAdmin { get; set; }
        }

        private static async Task<Dictionary<int, ProyectoDestinatarios>> BuildProyectosDestinatariosAsync(
            AppDbContext ctx, List<int> proyectoIds)
        {
            if (proyectoIds.Count == 0) return new Dictionary<int, ProyectoDestinatarios>();

            return await (
                from p in ctx.Project.AsNoTracking()
                where proyectoIds.Contains(p.ProjectId)
                join rw in ctx.Worker.AsNoTracking() on p.ResidenteWorkersId equals rw.Id into rwj
                from residente in rwj.DefaultIfEmpty()
                select new ProyectoDestinatarios
                {
                    ProjectId        = p.ProjectId,
                    EmailResidente   = residente != null ? residente.EmailCorporativo : null,
                    EmailResponsable = p.EmailResponsable,
                    EmailRrhh        = p.EmailRrhh,
                    EmailCoordSsoma  = p.EmailCoordSsoma,
                    EmailCoordAdmin  = p.EmailCoordAdmin,
                }
            ).ToDictionaryAsync(p => p.ProjectId);
        }

        /// <summary>
        /// Igual que EmoAlertaService.BuildDestinatarios:
        /// email personal del trabajador + todos los emails del proyecto vinculado.
        /// </summary>
        private static List<string> BuildDestinatarios(Worker worker, ProyectoDestinatarios? proyecto)
        {
            var raw = new List<string?> { worker.EmailCorporativo };

            if (proyecto != null)
            {
                raw.Add(proyecto.EmailResidente);
                raw.Add(proyecto.EmailResponsable);
                raw.Add(proyecto.EmailRrhh);
                raw.Add(proyecto.EmailCoordSsoma);
                raw.Add(proyecto.EmailCoordAdmin);
            }

            return raw
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task EnviarYRegistrar(
            string key, DateOnly hoy,
            List<string> destinatarios, string subject, string body,
            List<SsAlertaSsoma> nuevas, SsomaReminderResult result,
            string descripcion)
        {
            var parts  = key.Split(':', 2);
            var alerta = new SsAlertaSsoma
            {
                TipoAlerta   = parts[0],
                ReferenciaId = parts[1],
                FechaAlerta  = hoy,
                CreatedAt    = DateTimeOffset.UtcNow,
            };

            try
            {
                await _emailService.SendAsync(
                    to: destinatarios,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    sender: SaludOcupacionalEmailConstants.SenderKey);

                alerta.EnviadoEmail  = true;
                alerta.FechaEnvio    = DateTimeOffset.UtcNow;
                alerta.Destinatarios = string.Join(",", destinatarios);
                result.TotalEnviados++;
                result.Detalles.Add($"{descripcion} — enviado a {destinatarios.Count} destinatario(s).");
            }
            catch (Exception ex)
            {
                alerta.EnviadoEmail = false;
                result.TotalErrores++;
                _logger.LogError(ex, "Error enviando alerta SSOMA {Key}", key);
                result.Detalles.Add($"{descripcion} — error: {ex.Message}");
            }

            nuevas.Add(alerta);
        }

        // ── Cuerpos de email ─────────────────────────────────────────────────

        private static string BuildBodyAccidenteSinAlta(int id, string nombre, DateOnly fecha, int dias, string? tipoAccidente)
        {
            return $@"
            <p>Estimados,</p>
            <p>Se notifica que el siguiente accidente de trabajo lleva <strong>{dias} días</strong> registrado sin que se haya registrado el alta médica:</p>
            <table style='border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;'>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Accidente #</strong></td><td style='border:1px solid #ddd;padding:8px;'>{id}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Trabajador</strong></td><td style='border:1px solid #ddd;padding:8px;'>{nombre}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Fecha accidente</strong></td><td style='border:1px solid #ddd;padding:8px;'>{fecha:dd/MM/yyyy}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Tipo</strong></td><td style='border:1px solid #ddd;padding:8px;'>{tipoAccidente ?? "—"}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Días sin alta</strong></td><td style='border:1px solid #ddd;padding:8px;color:#b00020;'><strong>{dias} días</strong></td></tr>
            </table>
            <p>Por favor registrar el alta médica o actualizar el estado del accidente en el sistema SSOMA.</p>
            <p style='font-size:12px;color:#666;'>Alerta automática — Sistema SSOMA · Abril Grupo Inmobiliario.</p>";
        }

        private static string BuildBodyDescansoVencido(int id, string nombre, DateOnly inicio, DateOnly fin, int diasVencido, string? diagnostico)
        {
            return $@"
            <p>Estimados,</p>
            <p>El siguiente descanso médico ha vencido hace <strong>{diasVencido} día(s)</strong> sin registrar alta médica:</p>
            <table style='border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;'>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Descanso #</strong></td><td style='border:1px solid #ddd;padding:8px;'>{id}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Trabajador</strong></td><td style='border:1px solid #ddd;padding:8px;'>{nombre}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Período</strong></td><td style='border:1px solid #ddd;padding:8px;'>{inicio:dd/MM/yyyy} → {fin:dd/MM/yyyy}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Diagnóstico</strong></td><td style='border:1px solid #ddd;padding:8px;'>{diagnostico ?? "—"}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Días vencido sin alta</strong></td><td style='border:1px solid #ddd;padding:8px;color:#b00020;'><strong>{diasVencido} días</strong></td></tr>
            </table>
            <p>Registrar el alta médica o crear una prórroga del descanso en el sistema SSOMA.</p>
            <p style='font-size:12px;color:#666;'>Alerta automática — Sistema SSOMA · Abril Grupo Inmobiliario.</p>";
        }

        private static string BuildBodyReinduccion(int id, string nombre, DateOnly fechaAccidente, int dias)
        {
            return $@"
            <p>Estimados,</p>
            <p>El trabajador <strong>{nombre}</strong> tiene una reinducción pendiente desde hace <strong>{dias} días</strong> tras su accidente de trabajo:</p>
            <table style='border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;'>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Accidente #</strong></td><td style='border:1px solid #ddd;padding:8px;'>{id}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Trabajador</strong></td><td style='border:1px solid #ddd;padding:8px;'>{nombre}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Fecha accidente</strong></td><td style='border:1px solid #ddd;padding:8px;'>{fechaAccidente:dd/MM/yyyy}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Días pendiente</strong></td><td style='border:1px solid #ddd;padding:8px;color:#b00020;'><strong>{dias} días</strong></td></tr>
            </table>
            <p>Coordinar la reinducción y marcarla como completada en el sistema SSOMA.</p>
            <p style='font-size:12px;color:#666;'>Alerta automática — Sistema SSOMA · Abril Grupo Inmobiliario.</p>";
        }

        private static string BuildBodyCasoSocial(Guid id, string nombre, string tipo, string prioridad, int dias)
        {
            return $@"
            <p>Estimados,</p>
            <p>El siguiente caso social lleva <strong>{dias} días sin seguimiento</strong>:</p>
            <table style='border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;'>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Caso Social</strong></td><td style='border:1px solid #ddd;padding:8px;'>{id.ToString()[..8]}…</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Trabajador</strong></td><td style='border:1px solid #ddd;padding:8px;'>{nombre}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Tipo</strong></td><td style='border:1px solid #ddd;padding:8px;'>{tipo}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Prioridad</strong></td><td style='border:1px solid #ddd;padding:8px;'>{prioridad}</td></tr>
              <tr><td style='border:1px solid #ddd;padding:8px;'><strong>Días sin seguimiento</strong></td><td style='border:1px solid #ddd;padding:8px;color:#b00020;'><strong>{dias} días</strong></td></tr>
            </table>
            <p>Registrar un seguimiento o cerrar el caso si ya fue resuelto.</p>
            <p style='font-size:12px;color:#666;'>Alerta automática — Sistema SSOMA · Abril Grupo Inmobiliario.</p>";
        }
    }
}
