using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Habilitacion.Application.Dtos;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Helpers;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Services;

namespace Abril_Backend.Features.Habilitacion.Application.Services
{
    public class RetiroAutomaticoService : IRetiroAutomaticoService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _emailService;
        private readonly ILogger<RetiroAutomaticoService> _logger;

        public RetiroAutomaticoService(
            IDbContextFactory<AppDbContext> factory,
            IEmailService emailService,
            ILogger<RetiroAutomaticoService> logger)
        {
            _factory = factory;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<RetiroAutomaticoResultDto> EjecutarAsync()
        {
            var result = new RetiroAutomaticoResultDto();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var hoyDt = hoy.ToDateTime(TimeOnly.MinValue);
            var fechaLimite = hoy.AddDays(4);
            var fechaLimiteDt = fechaLimite.ToDateTime(TimeOnly.MaxValue);

            using var ctx = _factory.CreateDbContext();

            var contributorsDict = new Dictionary<int, Contributor>();
            var workersARetirar = new List<WorkerClasificado>();
            var workersAviso = new List<WorkerClasificado>();

            // ── Sección 1: workers con docs vencidos → clasificar y retirar ──

            var habVencidos = await ctx.SsHabTrabajador
                .Where(h => (h.Estado == "Vencido" || h.Estado == "Falta") &&
                            h.Vigencia != null && h.Vigencia.Value < hoyDt)
                .Join(ctx.SsItemTrabajador.Where(i => i.RequiereVigencia && i.Activo),
                      h => h.ItemId, i => i.Id,
                      (h, i) => new { h.WorkerId, h.Vigencia, i.Nombre })
                .ToListAsync();

            if (habVencidos.Count > 0)
            {
                var workerEntregables = habVencidos
                    .GroupBy(x => x.WorkerId)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            VigenciaMasAntigua = DateOnly.FromDateTime(g.Min(x => x.Vigencia!.Value)),
                            Nombres = g.Select(x => x.Nombre).Distinct().ToList()
                        });

                var workerIds = workerEntregables.Keys.ToList();

                var workers = await ctx.Worker
                    .Where(w => workerIds.Contains(w.Id) && w.WorkersEstadoId == WorkersEstadoIds.Activo)
                    .Include(w => w.Person)
                    .ToListAsync();

                if (workers.Count > 0)
                {
                    var activeWorkerIds = workers.Select(w => w.Id).ToList();

                    var vinculacionesActivas = await ctx.WorkerVinculacion
                        .Where(v => activeWorkerIds.Contains(v.WorkerId) && v.FechaFin == null)
                        .ToListAsync();

                    var vinculacionDict = vinculacionesActivas
                        .GroupBy(v => v.WorkerId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).First());

                    var empresaIds = vinculacionesActivas
                        .Where(v => v.EmpresaId.HasValue).Select(v => v.EmpresaId!.Value).Distinct().ToList();
                    var workerContribIds = workers
                        .Where(w => w.ContributorId.HasValue).Select(w => w.ContributorId!.Value).Distinct().ToList();
                    var allContribIds = empresaIds.Union(workerContribIds).Distinct().ToList();

                    contributorsDict = await ctx.Contributor
                        .Where(c => allContribIds.Contains(c.ContributorId))
                        .ToDictionaryAsync(c => c.ContributorId);

                    foreach (var worker in workers)
                    {
                        try
                        {
                            if (!workerEntregables.TryGetValue(worker.Id, out var entregablesInfo)) continue;

                            vinculacionDict.TryGetValue(worker.Id, out var vinc);
                            var empresaId = vinc?.EmpresaId;
                            var proyectoId = vinc?.ProyectoId;

                            bool esCasa = worker.ContrataCasa == "Casa";
                            if (!esCasa && empresaId.HasValue &&
                                contributorsDict.TryGetValue(empresaId.Value, out var empContrib))
                                esCasa = empContrib.EsAbril;
                            if (!esCasa && worker.ContributorId.HasValue &&
                                contributorsDict.TryGetValue(worker.ContributorId.Value, out var workerContrib))
                                esCasa = workerContrib.EsAbril;

                            int diasGracia = esCasa ? 7 : 2;
                            int diasEnMora = hoy.DayNumber - entregablesInfo.VigenciaMasAntigua.DayNumber;

                            var nombre = worker.Person?.FullName ?? $"Worker #{worker.Id}";
                            var dni = worker.Person?.DocumentIdentityCode;

                            var clasificado = new WorkerClasificado(
                                worker.Id, nombre, dni, empresaId, proyectoId, esCasa,
                                diasGracia, entregablesInfo.Nombres, diasEnMora);

                            if (diasEnMora >= diasGracia)
                                workersARetirar.Add(clasificado);
                            else if (diasEnMora == diasGracia - 1)
                                workersAviso.Add(clasificado);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[RetiroAutomatico] Error clasificando worker {WorkerId}", worker.Id);
                        }
                    }

                    foreach (var w in workersARetirar)
                    {
                        try
                        {
                            using var writeCtx = _factory.CreateDbContext();

                            var workerEnt = await writeCtx.Worker.FirstOrDefaultAsync(x => x.Id == w.WorkerId);
                            if (workerEnt == null || workerEnt.WorkersEstadoId != WorkersEstadoIds.Activo) continue;

                            workerEnt.WorkersEstadoId = WorkersEstadoIds.Retirado;
                            workerEnt.UpdatedAt = DateTimeOffset.UtcNow;

                            // Cierra el periodo laboral vigente, igual que el retiro manual
                            // (HabTrabajadorRepository.BajaAsync). Ver WorkersPeriodoLaboral.
                            await WorkersPeriodoLaboralHelper.CerrarAsync(
                                writeCtx, w.WorkerId, hoy, DateTimeOffset.UtcNow);

                            var vinc = await writeCtx.WorkerVinculacion
                                .Where(v => v.WorkerId == w.WorkerId && v.FechaFin == null)
                                .OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)
                                .FirstOrDefaultAsync();

                            if (vinc != null) { vinc.FechaFin = hoy; vinc.UpdatedAt = DateTimeOffset.UtcNow; }

                            var asignaciones = await writeCtx.WorkerProyecto
                                .Where(wp => wp.WorkerId == w.WorkerId && wp.FechaFin == null)
                                .ToListAsync();

                            var nowOffset = DateTimeOffset.UtcNow;
                            foreach (var ap in asignaciones) { ap.FechaFin = hoy; ap.UpdatedAt = nowOffset; }

                            writeCtx.WorkerEvento.Add(new WorkerEvento
                            {
                                WorkerId = w.WorkerId,
                                TipoEvento = WorkerTipoEvento.Baja,
                                Descripcion = "Retiro automático por documentación vencida.",
                                ProyectoAnteriorId = vinc?.ProyectoId,
                                EmpresaAnteriorId = vinc?.EmpresaId,
                                CreatedAt = DateTime.UtcNow
                            });

                            // Igual que en el retiro manual (HabTrabajadorRepository.BajaAsync): una
                            // interconsulta "Pendiente" no la va a completar nadie una vez retirado.
                            var interconsultasPendientes = await writeCtx.SsInterconsulta
                                .Where(i => i.WorkerId == w.WorkerId && i.Estado == "Pendiente")
                                .ToListAsync();
                            foreach (var ic in interconsultasPendientes)
                            {
                                ic.Estado = "Cancelada";
                                ic.Diagnostico = string.IsNullOrWhiteSpace(ic.Diagnostico)
                                    ? "Cancelada automáticamente: trabajador retirado."
                                    : ic.Diagnostico + " | Cancelada automáticamente: trabajador retirado.";
                                ic.UpdatedAt = DateTimeOffset.UtcNow;
                            }

                            writeCtx.SsRetiroAutomaticoLog.Add(new SsRetiroAutomaticoLog
                            {
                                WorkerId = w.WorkerId,
                                EmpresaId = w.EmpresaId,
                                Motivo = "Documentación vencida",
                                EjecutadoEn = DateTimeOffset.UtcNow,
                                TipoRetiro = "AUTOMATICO",
                                DiasGracia = w.DiasGracia,
                                EntregablesVencidos = string.Join(", ", w.EntregablesVencidos)
                            });

                            await writeCtx.SaveChangesAsync();

                            result.TotalRetirados++;
                            result.Detalles.Add(
                                $"[RETIRADO] {w.Nombre} (ID:{w.WorkerId}) Empresa:{w.EmpresaId} Docs:{string.Join(", ", w.EntregablesVencidos)}");

                            _logger.LogInformation(
                                "[RetiroAutomatico] Worker {WorkerId} retirado. Empresa={EmpresaId}",
                                w.WorkerId, w.EmpresaId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[RetiroAutomatico] Error retirando worker {WorkerId}", w.WorkerId);
                        }
                    }

                    foreach (var w in workersAviso)
                    {
                        result.TotalAvisados++;
                        result.Detalles.Add(
                            $"[AVISO] {w.Nombre} (ID:{w.WorkerId}) Se retirará mañana. Docs:{string.Join(", ", w.EntregablesVencidos)}");
                    }
                }
            }

            // ── Sección 2: documentos por vencer en 4 días ──

            var porVencerEquipo = new List<PorVencerEquipo>();
            var porVencerEmpresa = new List<PorVencerEmpresa>();

            // Equipos
            var habEquiposRaw = await ctx.SsHabEquipo
                .Where(h => h.Estado == "Aprobado" && h.Vigencia != null
                         && h.Vigencia.Value >= hoyDt && h.Vigencia.Value <= fechaLimiteDt)
                .Select(h => new
                {
                    h.Vigencia,
                    EmpresaId = h.Equipo != null ? h.Equipo.PropietarioEmpresaId : null,
                    NombreEquipo = h.Equipo != null
                        ? ((h.Equipo.TipoEquipo != null ? h.Equipo.TipoEquipo.Nombre : "") + (h.Equipo.Marca != null ? " " + h.Equipo.Marca : "") + (h.Equipo.Modelo != null ? " " + h.Equipo.Modelo : "")).Trim()
                        : "Equipo desconocido",
                    ItemNombre = h.Item != null ? h.Item.Nombre : "Documento desconocido"
                })
                .ToListAsync();

            foreach (var e in habEquiposRaw)
                porVencerEquipo.Add(new PorVencerEquipo(
                    e.NombreEquipo, e.ItemNombre,
                    DateOnly.FromDateTime(e.Vigencia!.Value), e.EmpresaId));

            // Empresas
            var habEmpresasRaw = await ctx.SsHabEmpresa
                .Where(h => h.Estado == "Aprobado" && h.Vigencia != null
                         && h.Vigencia.Value >= hoyDt && h.Vigencia.Value <= fechaLimiteDt
                         && h.Mes == null)
                .Select(h => new
                {
                    h.EmpresaId,
                    h.Vigencia,
                    ItemNombre = h.Item != null ? h.Item.Nombre : "Documento desconocido",
                    ProyectoNombre = h.Proyecto != null
                        ? (h.Proyecto.Abbreviation ?? h.Proyecto.Codigo ?? h.Proyecto.ProjectDescription)
                        : "-"
                })
                .ToListAsync();

            foreach (var e in habEmpresasRaw)
                porVencerEmpresa.Add(new PorVencerEmpresa(
                    e.ItemNombre, e.ProyectoNombre ?? "-",
                    DateOnly.FromDateTime(e.Vigencia!.Value), e.EmpresaId));

            // Trabajadores por vencer: cargar workers y vinculaciones primero
            var porVencerTrab = new List<PorVencerTrabajador>();

            var habProximosRaw = await ctx.SsHabTrabajador
                .Where(h => h.Estado == "Aprobado" && h.Vigencia != null
                         && h.Vigencia.Value >= hoyDt && h.Vigencia.Value <= fechaLimiteDt)
                .Join(ctx.SsItemTrabajador.Where(i => i.RequiereVigencia && i.Activo),
                      h => h.ItemId, i => i.Id,
                      (h, i) => new { h.WorkerId, h.Vigencia, ItemNombre = i.Nombre })
                .ToListAsync();

            Dictionary<int, Worker> workersProximos = [];
            Dictionary<int, WorkerVinculacion> vincsProximosDict = [];

            if (habProximosRaw.Count > 0)
            {
                var proximosWorkerIds = habProximosRaw.Select(x => x.WorkerId).Distinct().ToList();
                workersProximos = await ctx.Worker
                    .Where(w => proximosWorkerIds.Contains(w.Id) && w.WorkersEstadoId == WorkersEstadoIds.Activo)
                    .Include(w => w.Person)
                    .ToDictionaryAsync(w => w.Id);
                var vincsProximos = await ctx.WorkerVinculacion
                    .Where(v => proximosWorkerIds.Contains(v.WorkerId) && v.FechaFin == null)
                    .ToListAsync();
                vincsProximosDict = vincsProximos
                    .GroupBy(v => v.WorkerId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).First());
            }

            // Cargar todos los contributors nuevos (de proximos + equipos + empresas) antes de calcular EsCasa
            var nuevosEmpresaIds = vincsProximosDict.Values
                    .Where(v => v.EmpresaId.HasValue).Select(v => v.EmpresaId!.Value)
                .Union(porVencerEquipo.Where(e => e.EmpresaId.HasValue).Select(e => e.EmpresaId!.Value))
                .Union(porVencerEmpresa.Select(e => e.EmpresaId))
                .Distinct()
                .Where(id => !contributorsDict.ContainsKey(id))
                .ToList();

            if (nuevosEmpresaIds.Count > 0)
            {
                var nuevos = await ctx.Contributor
                    .Where(c => nuevosEmpresaIds.Contains(c.ContributorId))
                    .ToListAsync();
                foreach (var c in nuevos)
                    contributorsDict[c.ContributorId] = c;
            }

            // Ahora construir porVencerTrab con EsCasa ya computable
            foreach (var grp in habProximosRaw.GroupBy(x => x.WorkerId))
            {
                if (!workersProximos.TryGetValue(grp.Key, out var w)) continue;
                vincsProximosDict.TryGetValue(grp.Key, out var vinc);
                var empId = vinc?.EmpresaId;
                var proyId = vinc?.ProyectoId;

                bool esCasa = w.ContrataCasa == "Casa";
                if (!esCasa && empId.HasValue &&
                    contributorsDict.TryGetValue(empId.Value, out var empC))
                    esCasa = empC.EsAbril;
                if (!esCasa && w.ContributorId.HasValue &&
                    contributorsDict.TryGetValue(w.ContributorId.Value, out var wC))
                    esCasa = wC.EsAbril;

                var nombre = w.Person?.FullName ?? $"Worker #{w.Id}";
                var dni = w.Person?.DocumentIdentityCode;

                foreach (var row in grp)
                    porVencerTrab.Add(new PorVencerTrabajador(
                        nombre, dni, row.ItemNombre,
                        DateOnly.FromDateTime(row.Vigencia!.Value), empId, proyId, esCasa));
            }

            // ── Enviar emails solo si hay algo que reportar ──
            bool hayAlgo = workersARetirar.Count > 0 || workersAviso.Count > 0
                        || porVencerTrab.Count > 0 || porVencerEquipo.Count > 0 || porVencerEmpresa.Count > 0;

            if (hayAlgo)
                await EnviarEmailsAsync(
                    workersARetirar, workersAviso,
                    porVencerTrab, porVencerEquipo, porVencerEmpresa,
                    contributorsDict, hoy);

            return result;
        }

        private async Task EnviarEmailsAsync(
            List<WorkerClasificado> retirados,
            List<WorkerClasificado> avisos,
            List<PorVencerTrabajador> porVencerTrab,
            List<PorVencerEquipo> porVencerEquipo,
            List<PorVencerEmpresa> porVencerEmpresa,
            Dictionary<int, Contributor> contributorsDict,
            DateOnly hoy)
        {
            // Workers Casa → agrupa por proyecto; contratistas → por empresa
            // Equipos/Empresas por vencer → siempre por empresa (no tienen concepto Casa)
            var grupos = retirados.Concat(avisos)
                    .Select(w => w.EsCasa
                        ? new GrupoEmail(true, w.ProyectoId, null)
                        : new GrupoEmail(false, null, w.EmpresaId))
                .Concat(porVencerTrab
                    .Select(t => t.EsCasa
                        ? new GrupoEmail(true, t.ProyectoId, null)
                        : new GrupoEmail(false, null, t.EmpresaId)))
                .Concat(porVencerEquipo.Select(e => new GrupoEmail(false, null, e.EmpresaId)))
                .Concat(porVencerEmpresa.Select(e => new GrupoEmail(false, null, (int?)e.EmpresaId)))
                .Distinct()
                .ToList();

            // Cargar proyectos para grupos Casa
            var proyectoIds = grupos
                .Where(g => g.EsCasa && g.ProyectoId.HasValue)
                .Select(g => g.ProyectoId!.Value)
                .Distinct().ToList();

            Dictionary<int, Project> proyectosDict = [];
            if (proyectoIds.Count > 0)
            {
                using var pCtx = _factory.CreateDbContext();
                proyectosDict = await pCtx.Project
                    .Where(p => proyectoIds.Contains(p.ProjectId))
                    .ToDictionaryAsync(p => p.ProjectId);
            }

            foreach (var grupo in grupos)
            {
                try
                {
                    var retiradosGrupo = retirados.Where(w =>
                        w.EsCasa == grupo.EsCasa &&
                        (grupo.EsCasa ? w.ProyectoId == grupo.ProyectoId : w.EmpresaId == grupo.EmpresaId)).ToList();

                    var avisosGrupo = avisos.Where(w =>
                        w.EsCasa == grupo.EsCasa &&
                        (grupo.EsCasa ? w.ProyectoId == grupo.ProyectoId : w.EmpresaId == grupo.EmpresaId)).ToList();

                    var trabProx = porVencerTrab.Where(t =>
                        t.EsCasa == grupo.EsCasa &&
                        (grupo.EsCasa ? t.ProyectoId == grupo.ProyectoId : t.EmpresaId == grupo.EmpresaId)).ToList();

                    // Equipos y empresas solo van en grupos contratista
                    var equipoProx = grupo.EsCasa ? [] : porVencerEquipo
                        .Where(e => e.EmpresaId == grupo.EmpresaId).ToList();
                    var empresaProx = grupo.EsCasa ? (List<PorVencerEmpresa>)[] : porVencerEmpresa
                        .Where(e => (int?)e.EmpresaId == grupo.EmpresaId).ToList();

                    if (retiradosGrupo.Count == 0 && avisosGrupo.Count == 0
                        && trabProx.Count == 0 && equipoProx.Count == 0 && empresaProx.Count == 0)
                        continue;

                    List<string> emails;
                    string etiqueta;

                    if (grupo.EsCasa)
                    {
                        Project? proyecto = null;
                        if (grupo.ProyectoId.HasValue && proyectosDict.TryGetValue(grupo.ProyectoId.Value, out var pFound))
                            proyecto = pFound;

                        etiqueta = proyecto != null
                            ? (proyecto.Abbreviation ?? proyecto.Codigo ?? proyecto.ProjectDescription)
                            : "Abril (sin proyecto)";

                        emails = new List<string?> {
                            proyecto?.EmailCoordSsoma,
                            proyecto?.EmailCoordAdmin,
                            proyecto?.EmailRrhh
                        }
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Select(e => e!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    }
                    else
                    {
                        etiqueta = grupo.EmpresaId.HasValue &&
                            contributorsDict.TryGetValue(grupo.EmpresaId.Value, out var c)
                            ? c.ContributorName : "Empresa desconocida";

                        if (grupo.EmpresaId.HasValue)
                        {
                            using var emailCtx = _factory.CreateDbContext();
                            var usuarioEmails = await emailCtx.SsContratistaUsuarios
                                .Where(cu => cu.ContractorId == grupo.EmpresaId.Value && cu.Activo)
                                .Join(emailCtx.User,
                                    cu => cu.UserId, u => u.UserId,
                                    (cu, u) => u.Email)
                                .Where(e => !string.IsNullOrEmpty(e))
                                .ToListAsync();

                            emails = usuarioEmails.Where(e => e != null).Select(e => e!).ToList();

                            if (emails.Count == 0 &&
                                contributorsDict.TryGetValue(grupo.EmpresaId.Value, out var emp) &&
                                !string.IsNullOrEmpty(emp.EmailAdministrador))
                                emails = [emp.EmailAdministrador];
                        }
                        else
                        {
                            emails = [];
                        }
                    }

                    if (emails.Count == 0)
                    {
                        _logger.LogWarning(
                            "[RetiroAutomatico] Sin destinatarios para '{Etiqueta}' (EsCasa={EsCasa}, ProyectoId={ProyectoId}, EmpresaId={EmpresaId})",
                            etiqueta, grupo.EsCasa, grupo.ProyectoId, grupo.EmpresaId);
                        continue;
                    }

                    var asunto = $"Retiro automático de trabajadores — {etiqueta} — {hoy:dd/MM/yyyy}";
                    var body = BuildEmailHtml(etiqueta, retiradosGrupo, avisosGrupo,
                                             trabProx, equipoProx, empresaProx, hoy);

                    try
                    {
                        await _emailService.SendAsync(emails, asunto, body, isHtml: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "[RetiroAutomatico] Error enviando email para '{Etiqueta}'", etiqueta);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[RetiroAutomatico] Error procesando grupo {Grupo}", grupo);
                }
            }
        }

        private static string BuildEmailHtml(
            string etiqueta,
            List<WorkerClasificado> retirados,
            List<WorkerClasificado> avisos,
            List<PorVencerTrabajador> trabProx,
            List<PorVencerEquipo> equipoProx,
            List<PorVencerEmpresa> empresaProx,
            DateOnly hoy)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<html><body style=\"font-family:Arial,sans-serif;color:#333\">");
            sb.Append($"<h2>Retiro automático de trabajadores — {H(etiqueta)} — {hoy:dd/MM/yyyy}</h2>");

            sb.Append("<h3 style=\"color:#c00\">RETIRADOS HOY</h3>");
            sb.Append(retirados.Count == 0 ? "<p>Ninguno</p>" : BuildTablaWorkers(retirados));

            sb.Append("<h3 style=\"color:#e65c00\">SE RETIRARÁN MAÑANA (AVISO)</h3>");
            sb.Append(avisos.Count == 0 ? "<p>Ninguno</p>" : BuildTablaWorkers(avisos));

            bool hayPorVencer = trabProx.Count > 0 || equipoProx.Count > 0 || empresaProx.Count > 0;
            if (hayPorVencer)
            {
                sb.Append("<h3 style=\"color:#0066cc\">DOCUMENTOS POR VENCER EN 4 DÍAS</h3>");

                sb.Append("<h4>Trabajadores</h4>");
                sb.Append(trabProx.Count == 0 ? "<p>Ninguno</p>" : BuildTablaTrabProx(trabProx));

                sb.Append("<h4>Equipos</h4>");
                sb.Append(equipoProx.Count == 0 ? "<p>Ninguno</p>" : BuildTablaEquipoProx(equipoProx));

                sb.Append("<h4>Empresa</h4>");
                sb.Append(empresaProx.Count == 0 ? "<p>Ninguno</p>" : BuildTablaEmpresaProx(empresaProx));
            }

            sb.Append("</body></html>");
            return sb.ToString();
        }

        private static string BuildTablaWorkers(List<WorkerClasificado> workers)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%\">");
            sb.Append("<thead style=\"background:#f0f0f0\"><tr>");
            sb.Append("<th>Trabajador</th><th>DNI</th><th>Documentos Vencidos</th><th>Días de mora</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var w in workers)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{H(w.Nombre)}</td>");
                sb.Append($"<td>{H(w.Dni ?? "-")}</td>");
                sb.Append($"<td>{H(string.Join(", ", w.EntregablesVencidos))}</td>");
                sb.Append($"<td>{w.DiasEnMora}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static string BuildTablaTrabProx(List<PorVencerTrabajador> items)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%\">");
            sb.Append("<thead style=\"background:#e8f0fe\"><tr>");
            sb.Append("<th>Trabajador</th><th>DNI</th><th>Documento</th><th>Vence</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var t in items)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{H(t.Nombre)}</td>");
                sb.Append($"<td>{H(t.Dni ?? "-")}</td>");
                sb.Append($"<td>{H(t.ItemNombre)}</td>");
                sb.Append($"<td>{t.Vigencia:dd/MM/yyyy}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static string BuildTablaEquipoProx(List<PorVencerEquipo> items)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%\">");
            sb.Append("<thead style=\"background:#e8f0fe\"><tr>");
            sb.Append("<th>Nombre equipo</th><th>Documento</th><th>Vence</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var e in items)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{H(e.NombreEquipo)}</td>");
                sb.Append($"<td>{H(e.ItemNombre)}</td>");
                sb.Append($"<td>{e.Vigencia:dd/MM/yyyy}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static string BuildTablaEmpresaProx(List<PorVencerEmpresa> items)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%\">");
            sb.Append("<thead style=\"background:#e8f0fe\"><tr>");
            sb.Append("<th>Documento</th><th>Proyecto</th><th>Vence</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var e in items)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{H(e.ItemNombre)}</td>");
                sb.Append($"<td>{H(e.ProyectoNombre)}</td>");
                sb.Append($"<td>{e.Vigencia:dd/MM/yyyy}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static string H(string s) => System.Net.WebUtility.HtmlEncode(s);

        private record GrupoEmail(bool EsCasa, int? ProyectoId, int? EmpresaId);

        private record WorkerClasificado(
            int WorkerId,
            string Nombre,
            string? Dni,
            int? EmpresaId,
            int? ProyectoId,
            bool EsCasa,
            int DiasGracia,
            List<string> EntregablesVencidos,
            int DiasEnMora);

        private record PorVencerTrabajador(
            string Nombre,
            string? Dni,
            string ItemNombre,
            DateOnly Vigencia,
            int? EmpresaId,
            int? ProyectoId,
            bool EsCasa);

        private record PorVencerEquipo(
            string NombreEquipo,
            string ItemNombre,
            DateOnly Vigencia,
            int? EmpresaId);

        private record PorVencerEmpresa(
            string ItemNombre,
            string ProyectoNombre,
            DateOnly Vigencia,
            int EmpresaId);
    }
}
