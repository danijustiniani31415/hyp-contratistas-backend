using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Shared;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class EmoAlertaService : IEmoAlertaService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmoAlertaService> _logger;

        public EmoAlertaService(
            IDbContextFactory<AppDbContext> factory,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<EmoAlertaService> logger)
        {
            _factory = factory;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public Task<EmoAlertaResultDto> ProcesarAlertas() =>
            ProcesarVentana(diasCalendarioFijos: null, tipoAlerta: "VENCIMIENTO");

        /// <summary>Ver <see cref="IEmoAlertaService.ProcesarAlertas7DiasCalendario"/>.</summary>
        public Task<EmoAlertaResultDto> ProcesarAlertas7DiasCalendario() =>
            ProcesarVentana(diasCalendarioFijos: 7, tipoAlerta: "VENCIMIENTO_7D");

        /// <param name="diasCalendarioFijos">
        /// Null: usa el criterio original (días HÁBILES configurables, <c>EmoProgramacion:DiasHabilesAntes</c>).
        /// Con valor: dispara cuando faltan exactamente esa cantidad de días CALENDARIO (sin
        /// saltar sábados/feriados) — es el aviso fijo de "vence en 7 días" pedido para que
        /// residentes y administradores lo vean con anticipación previsible, sin que se corra
        /// por feriados de por medio como sí corre el aviso por días hábiles.
        /// </param>
        private async Task<EmoAlertaResultDto> ProcesarVentana(int? diasCalendarioFijos, string tipoAlerta)
        {
            var result = new EmoAlertaResultDto();
            using var ctx = _factory.CreateDbContext();

            var ventanaDias      = _configuration.GetValue<int?>("EmoProgramacion:VentanaDias") ?? 14;
            var diasHabilesAntes = _configuration.GetValue<int?>("EmoProgramacion:DiasHabilesAntes") ?? 4;

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5).Date);

            // Ventana amplia en SQL para cubrir cualquier combinación de días hábiles antes
            // del vencimiento (con feriados de por medio el tramo en calendario se estira).
            var ventanaInicio = hoy.AddDays(1);
            var ventanaFin = hoy.AddDays(ventanaDias);

            // Feriados/días no laborables para que el cálculo de días hábiles los salte.
            var cal = await CargarCalendarioHabilAsync(ctx);

            // Bug 1 fix: COALESCE(fecha_vencimiento_calculada, fecha_vencimiento)
            // Bug 3 fix: JOIN con SsEmoTipo para leer VigenciaMeses real
            // Bug 2 fix: JOIN con Worker para determinar calendario hábil en memoria
            var candidatosRaw = await (
                from e in ctx.WorkerEmo
                join w in ctx.Worker on e.WorkerId equals w.Id
                join t in ctx.SsEmoTipo on e.TipoEmoId equals t.Id into tj
                from t in tj.DefaultIfEmpty()
                where e.Activo
                    && (e.FechaVencimientoCalculada ?? e.FechaVencimiento) != null
                    && (e.FechaVencimientoCalculada ?? e.FechaVencimiento) >= ventanaInicio
                    && (e.FechaVencimientoCalculada ?? e.FechaVencimiento) <= ventanaFin
                select new
                {
                    Emo = e,
                    Worker = w,
                    TipoEmo = t,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    WorkerDni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                    // El puesto se resuelve acá porque el correo solo lo muestra: es el
                    // campo de presentación (workers.puesto_id), no decide nada.
                    WorkerPuesto = w.PuestoCatalogo != null ? w.PuestoCatalogo.Nombre : null
                }
            ).AsNoTracking().ToListAsync();

            // Disparar alerta cuando hoy == fechaVenc - N días. Con diasCalendarioFijos: N días
            // calendario exactos (fecha objetivo fija, no se corre por feriados/fin de semana).
            // Sin él: criterio original de N días HÁBILES antes del vencimiento.
            var candidatos = candidatosRaw
                .Where(x =>
                {
                    var fv = (x.Emo.FechaVencimientoCalculada ?? x.Emo.FechaVencimiento)!.Value;
                    var fechaAlerta = diasCalendarioFijos.HasValue
                        ? fv.AddDays(-diasCalendarioFijos.Value)
                        : cal.RestarDiasHabiles(fv, diasHabilesAntes, EsCalendarioOficina(x.Worker));
                    return fechaAlerta == hoy;
                })
                .ToList();

            if (candidatos.Count == 0) return result;

            var emoIds = candidatos.Select(x => x.Emo.Id).ToList();
            var workerIds = candidatos.Select(x => x.Worker.Id).Distinct().ToList();

            var vinculaciones = await ctx.WorkerVinculacion
                .AsNoTracking()
                .Where(v => workerIds.Contains(v.WorkerId) && (v.FechaFin == null || v.FechaFin >= hoy))
                .OrderByDescending(v => v.CreatedAt)
                .ThenByDescending(v => v.Id)
                .ToListAsync();

            var vinculacionPorWorker = vinculaciones
                .GroupBy(v => v.WorkerId)
                .ToDictionary(g => g.Key, g => g.First());

            var proyectoIds = vinculaciones
                .Where(v => v.ProyectoId.HasValue)
                .Select(v => v.ProyectoId!.Value)
                .Distinct().ToList();

            var empresaIds = vinculaciones
                .Where(v => v.EmpresaId.HasValue)
                .Select(v => v.EmpresaId!.Value)
                .Distinct().ToList();

            var proyectosDict = await ctx.Project
                .AsNoTracking()
                .Where(p => proyectoIds.Contains(p.ProjectId))
                .ToDictionaryAsync(p => p.ProjectId);

            // Correo del residente de cada proyecto: sale de su ficha de trabajador
            // (project.residente_workers_id → workers.email_corporativo), no de una copia
            // guardada en el proyecto.
            var residentePorProyecto = await (
                from p in ctx.Project.AsNoTracking()
                where proyectoIds.Contains(p.ProjectId) && p.ResidenteWorkersId != null
                join w in ctx.Worker.AsNoTracking() on p.ResidenteWorkersId equals w.Id
                where w.EmailCorporativo != null && w.EmailCorporativo != ""
                select new { p.ProjectId, w.EmailCorporativo })
                .ToDictionaryAsync(x => x.ProjectId, x => x.EmailCorporativo!);

            var empresasDict = await ctx.Contributor
                .AsNoTracking()
                .Where(em => empresaIds.Contains(em.ContributorId))
                .ToDictionaryAsync(em => em.ContributorId);

            var alertasYaEnviadasHoy = await ctx.SsAlertaEmo
                .AsNoTracking()
                .Where(a => a.EmoId != null && emoIds.Contains(a.EmoId.Value) && a.FechaAlerta == hoy && a.TipoAlerta == tipoAlerta)
                .Select(a => a.EmoId!.Value)
                .ToListAsync();
            var alertasSet = new HashSet<int>(alertasYaEnviadasHoy);

            // Guard de "ya alertado hoy" — igual que antes, solo que ahora separado del envío
            // para poder agrupar por proyecto más abajo.
            result.TotalProcesados += candidatos.Count;
            foreach (var c in candidatos.Where(c => alertasSet.Contains(c.Emo.Id)))
                result.Detalles.Add($"EMO {c.Emo.Id} — alerta ya registrada hoy. Omitido.");

            var candidatosNuevos = candidatos.Where(c => !alertasSet.Contains(c.Emo.Id)).ToList();

            // Candidatos ya resueltos con su proyecto/empresa/residente.
            var resueltos = candidatosNuevos.Select(c =>
            {
                vinculacionPorWorker.TryGetValue(c.Worker.Id, out var vinculacion);

                Project? proyecto = null;
                if (vinculacion?.ProyectoId != null)
                    proyectosDict.TryGetValue(vinculacion.ProyectoId.Value, out proyecto);

                Contributor? empresa = null;
                if (vinculacion?.EmpresaId != null)
                    empresasDict.TryGetValue(vinculacion.EmpresaId.Value, out empresa);

                string? residenteEmail = null;
                if (proyecto != null) residentePorProyecto.TryGetValue(proyecto.ProjectId, out residenteEmail);

                return new { C = c, Proyecto = proyecto, Empresa = empresa, ResidenteEmail = residenteEmail };
            }).ToList();

            if (diasCalendarioFijos.HasValue)
            {
                // Aviso de "vence en 7 días": UN solo correo por proyecto/residente con TODOS los
                // trabajadores de ese proyecto que crucen el umbral el mismo día — antes se
                // mandaba un correo por trabajador y si 5 vencían el mismo día el residente
                // recibía 5 correos separados en vez de una sola lista.
                var grupos = resueltos.GroupBy(r => r.Proyecto?.ProjectId ?? 0);

                foreach (var grupo in grupos)
                {
                    var proyecto = grupo.First().Proyecto;
                    var residenteEmail = grupo.First().ResidenteEmail;
                    var destinatarios = BuildDestinatariosLista(proyecto, residenteEmail);

                    if (destinatarios.Count == 0)
                    {
                        foreach (var r in grupo)
                            result.Detalles.Add($"EMO {r.C.Emo.Id} ({r.C.WorkerNombre}) — sin destinatarios (proyecto {proyecto?.ProjectDescription ?? "sin proyecto"}). Omitido.");
                        continue;
                    }

                    var filas = grupo.Select(r => (
                        Nombre: (string)(r.C.WorkerNombre ?? $"Worker {r.C.Worker.Id}"),
                        Dni: (string?)r.C.WorkerDni,
                        Empresa: r.Empresa?.ContributorName ?? "—",
                        FechaVenc: (r.C.Emo.FechaVencimientoCalculada ?? r.C.Emo.FechaVencimiento)!.Value
                    )).OrderBy(f => f.FechaVenc).ThenBy(f => f.Nombre).ToList();

                    var subject = $"[Vencen en {diasCalendarioFijos.Value} días] {filas.Count} EMO(s) próximos a vencer — {proyecto?.ProjectDescription ?? "Sin proyecto"}";
                    var body = BuildBodyLista(proyecto, diasCalendarioFijos.Value, filas);

                    try
                    {
                        await _emailService.SendAsync(
                            to: destinatarios,
                            subject: subject,
                            body: body,
                            isHtml: true,
                            sender: SaludOcupacionalEmailConstants.SenderKey);

                        foreach (var r in grupo)
                        {
                            ctx.SsAlertaEmo.Add(new SsAlertaEmo
                            {
                                WorkerId = r.C.Worker.Id,
                                EmoId = r.C.Emo.Id,
                                TipoAlerta = tipoAlerta,
                                FechaAlerta = hoy,
                                EnviadoEmail = true,
                                FechaEnvio = DateTimeOffset.UtcNow,
                                Destinatarios = string.Join(",", destinatarios),
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                            result.TotalEnviados++;
                        }
                        result.Detalles.Add($"Proyecto {proyecto?.ProjectDescription ?? "sin proyecto"} — 1 correo con {filas.Count} trabajador(es) a {destinatarios.Count} destinatario(s).");
                    }
                    catch (Exception ex)
                    {
                        result.TotalErrores += grupo.Count();
                        _logger.LogError(ex, "Error enviando alerta 7 días del proyecto {ProyectoId}", proyecto?.ProjectId);
                        result.Detalles.Add($"Proyecto {proyecto?.ProjectDescription ?? "sin proyecto"} — error al enviar: {ex.Message}");
                    }
                }

                await ctx.SaveChangesAsync();
                return result;
            }

            foreach (var r in resueltos)
            {
                var c = r.C;
                var proyecto = r.Proyecto;
                var empresa = r.Empresa;
                var residenteEmail = r.ResidenteEmail;
                var destinatarios = BuildDestinatarios(c.Worker, proyecto, residenteEmail);
                if (destinatarios.Count == 0)
                {
                    result.Detalles.Add($"EMO {c.Emo.Id} ({c.WorkerNombre}) — sin destinatarios. Omitido.");
                    continue;
                }

                // Bug 3 fix: leer vigencia real de SsEmoTipo.VigenciaMeses
                var vigenciaMeses = c.TipoEmo?.VigenciaMeses ?? 12;
                var vigenciaTexto = vigenciaMeses % 12 == 0
                    ? $"{vigenciaMeses / 12} año(s)"
                    : $"{vigenciaMeses} mes(es)";

                var fv = (c.Emo.FechaVencimientoCalculada ?? c.Emo.FechaVencimiento)!.Value;
                var subject = $"Vencimiento de EMO - {c.WorkerNombre} - {fv:yyyy-MM-dd}";
                var body = BuildBody(c.Worker, c.WorkerNombre, c.WorkerDni, c.WorkerPuesto, c.Emo, fv, proyecto, empresa, vigenciaTexto);

                try
                {
                    await _emailService.SendAsync(
                        to: destinatarios,
                        subject: subject,
                        body: body,
                        isHtml: true,
                        sender: SaludOcupacionalEmailConstants.SenderKey);

                    ctx.SsAlertaEmo.Add(new SsAlertaEmo
                    {
                        WorkerId = c.Worker.Id,
                        EmoId = c.Emo.Id,
                        TipoAlerta = tipoAlerta,
                        FechaAlerta = hoy,
                        EnviadoEmail = true,
                        FechaEnvio = DateTimeOffset.UtcNow,
                        Destinatarios = string.Join(",", destinatarios),
                        CreatedAt = DateTimeOffset.UtcNow
                    });

                    result.TotalEnviados++;
                    result.Detalles.Add($"EMO {c.Emo.Id} ({c.WorkerNombre}) — enviado a {destinatarios.Count} destinatario(s).");
                }
                catch (Exception ex)
                {
                    result.TotalErrores++;
                    _logger.LogError(ex, "Error enviando alerta de EMO {EmoId}", c.Emo.Id);
                    result.Detalles.Add($"EMO {c.Emo.Id} ({c.WorkerNombre}) — error al enviar: {ex.Message}");
                }
            }

            await ctx.SaveChangesAsync();
            return result;
        }

        private static async Task<CalendarioHabil> CargarCalendarioHabilAsync(AppDbContext ctx)
        {
            var feriados = await ctx.Holiday
                .AsNoTracking()
                .Where(h => h.Active && h.State)
                .Select(h => new { h.HolidayDate, h.RecurringYearly })
                .ToListAsync();

            return new CalendarioHabil(feriados.Select(f => (f.HolidayDate, f.RecurringYearly)));
        }

        // Casa + Staff u Oficina Central → Mon-Fri (excluye sáb y dom).
        // Contratista u Obra → Mon-Sat (excluye solo dom).
        private static bool EsCalendarioOficina(Worker worker)
        {
            return string.Equals(worker.ContrataCasa, "Casa", StringComparison.OrdinalIgnoreCase)
                && ObraOficinaStaffIds.StaffUOficinaCentral.Contains(worker.ObraOficinaStaffId ?? 0);
        }

        private static List<string> BuildDestinatarios(Worker worker, Project? proyecto, string? residenteEmail)
        {
            var raw = new List<string?> { worker.EmailCorporativo };

            if (proyecto != null)
            {
                raw.Add(residenteEmail);
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

        /// <summary>Destinatarios del aviso consolidado de 7 días: solo residente/contactos del proyecto (sin el correo individual del trabajador — es una lista para el proyecto, no un aviso personal).</summary>
        private static List<string> BuildDestinatariosLista(Project? proyecto, string? residenteEmail)
        {
            if (proyecto == null) return new List<string>();

            var raw = new List<string?> { residenteEmail, proyecto.EmailResponsable, proyecto.EmailRrhh, proyecto.EmailCoordSsoma, proyecto.EmailCoordAdmin };

            return raw
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildBodyLista(
            Project? proyecto,
            int dias,
            List<(string Nombre, string? Dni, string Empresa, DateOnly FechaVenc)> filas)
        {
            var filasHtml = string.Join("", filas.Select(f => $@"
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{f.Nombre}</td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{f.Dni}</td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{f.Empresa}</td>
                    <td style='border: 1px solid #ddd; padding: 8px; color: #b00020;'><strong>{f.FechaVenc:dd/MM/yyyy}</strong></td>
                </tr>"));

            return $@"
            <p>Estimados,</p>

            <p>
                Los siguientes trabajadores de <strong>{proyecto?.ProjectDescription ?? "—"}</strong>
                tienen su <strong>Examen Médico Ocupacional (EMO)</strong> próximo a vencer, dentro de
                los siguientes <strong>{dias} días</strong>:
            </p>

            <table style='border-collapse: collapse; font-family: Arial, sans-serif; font-size: 14px; width: 100%;'>
                <thead>
                    <tr>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6; text-align:left;'>Trabajador</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6; text-align:left;'>DNI</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6; text-align:left;'>Empresa</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6; text-align:left;'>Fecha de vencimiento</th>
                    </tr>
                </thead>
                <tbody>{filasHtml}</tbody>
            </table>

            <p>
                Por favor coordinar la programación de estos EMOs antes de la fecha de vencimiento
                para mantener la habilitación de cada trabajador.
            </p>

            <p style='font-size: 12px; color: #666;'>
                Este aviso se genera automáticamente, en un solo correo por proyecto, cuando faltan
                {dias} días calendario para el vencimiento.
            </p>";
        }

        private static string BuildBody(
            Worker worker,
            string? workerNombre,
            string? workerDni,
            string? workerPuesto,
            WorkerEmo emo,
            DateOnly fechaVencimiento,
            Project? proyecto,
            Contributor? empresa,
            string vigenciaTexto)
        {
            return $@"
            <p>Estimados,</p>

            <p>
                Se notifica el <strong>próximo vencimiento del Examen Médico Ocupacional (EMO)</strong>
                del siguiente trabajador:
            </p>

            <table style='border-collapse: collapse; font-family: Arial, sans-serif; font-size: 14px;'>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Trabajador</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{workerNombre}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>DNI</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{workerDni}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Puesto</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{workerPuesto ?? "—"}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Modalidad</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{ObraOficinaStaffIds.Nombre(worker.ObraOficinaStaffId)}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Empresa</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{empresa?.ContributorName ?? "—"}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Proyecto</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{proyecto?.ProjectDescription ?? "—"}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Fecha del EMO</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{emo.FechaEmo:dd/MM/yyyy}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Fecha de vencimiento</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px; color: #b00020;'><strong>{fechaVencimiento:dd/MM/yyyy}</strong></td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Vigencia de EMO</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{vigenciaTexto}</td>
                </tr>
            </table>

            <p>
                Por favor coordinar la programación del nuevo EMO antes de la fecha de vencimiento
                para mantener la habilitación del trabajador.
            </p>

            <p style='font-size: 12px; color: #666;'>
                Esta alerta se genera automáticamente cuando un EMO está próximo a vencer.
            </p>
            ";
        }
    }
}
