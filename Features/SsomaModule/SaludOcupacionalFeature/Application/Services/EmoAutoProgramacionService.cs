using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Shared;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class EmoAutoProgramacionService : IEmoAutoProgramacionService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IEmoDestinatariosResolver _destinatarios;
        private readonly ILogger<EmoAutoProgramacionService> _logger;

        public EmoAutoProgramacionService(
            IDbContextFactory<AppDbContext> factory,
            IEmailService emailService,
            IConfiguration configuration,
            IEmoDestinatariosResolver destinatarios,
            ILogger<EmoAutoProgramacionService> logger)
        {
            _factory = factory;
            _emailService = emailService;
            _configuration = configuration;
            _destinatarios = destinatarios;
            _logger = logger;
        }

        public async Task<EmoAutoProgramacionResultDto> ProcesarAutoProgramacion()
        {
            var result = new EmoAutoProgramacionResultDto();
            using var ctx = _factory.CreateDbContext();

            // Parámetros configurables (defaults si no están en appsettings).
            // VentanaDias sube de 14 a 18: con la regla de "2 sábados antes" para vencimientos
            // Domingo/Lunes/Martes (ver SabadoObjetivo), la cita puede caer hasta 10 días antes
            // del vencimiento — con ventana 14 el resumen a la clínica salía con apenas 4 días
            // de aviso para ese grupo. Con 18 vuelve a quedar en ~7-10 días de aviso para todos
            // los grupos (la fila se crea el mismo día en que el trabajador entra a la ventana).
            var ventanaDias      = _configuration.GetValue<int?>("EmoProgramacion:VentanaDias") ?? 18;
            // DiasHabilesAntes solo define la fecha para Oficina Central (Obra/Staff usan
            // SabadoObjetivo, que no depende de este valor salvo como respaldo si no hay ningún
            // sábado hábil en la ventana). 7 días hábiles en su calendario L-V da entre 9 y 11
            // días calendario de anticipación según el día de vencimiento — mismo orden de
            // magnitud que los ~10 días que ya se dejan para Obra/Staff.
            var diasHabilesAntes = _configuration.GetValue<int?>("EmoProgramacion:DiasHabilesAntes") ?? 7;
            var pisoDiasHabiles  = _configuration.GetValue<int?>("EmoProgramacion:PisoDiasHabiles") ?? 2;

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5).Date);
            var ventanaFin = hoy.AddDays(ventanaDias);

            // Feriados/días no laborables para que el cálculo de días hábiles los salte.
            var cal = await CargarCalendarioHabilAsync(ctx);

            // "Ingreso" solo aplica una vez (el primer examen del trabajador); su renovación
            // debe programarse como "Periódico Anual", nunca repetir "Ingreso" (bug real
            // reportado: trabajadores con años en la empresa seguían saliendo programados como
            // "Ingreso" en cada renovación, porque la cita nueva heredaba literalmente el tipo
            // del EMO que vencía). Se resuelve por nombre, no por id: el catálogo ss_emo_tipos
            // no viene de una migración versionada (se carga a mano en cada ambiente), así que
            // el id no está garantizado a ser el mismo en dev/prod.
            var periodicoAnualTipoId = await ctx.SsEmoTipo
                .Where(t => t.Activo && t.Nombre.ToLower() == "periódico anual")
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();
            if (periodicoAnualTipoId == null)
                _logger.LogWarning("Auto-programación EMO: no se encontró el tipo 'Periódico Anual' en ss_emo_tipos — las renovaciones de Ingreso seguirán saliendo como Ingreso.");

            var candidatosRaw = await (
                from e in ctx.WorkerEmo
                join w in ctx.Worker on e.WorkerId equals w.Id
                join t in ctx.SsEmoTipo on e.TipoEmoId equals t.Id
                join v in ctx.WorkerVinculacion on w.Id equals v.WorkerId
                join contrib in ctx.Contributor on v.EmpresaId equals contrib.ContributorId
                join proj in ctx.Project on v.ProyectoId equals (int?)proj.ProjectId into projg
                from proyecto in projg.DefaultIfEmpty()
                where e.Activo
                    && t.RequiereNuevo
                    && t.VigenciaMeses != null
                    && !t.Nombre.ToLower().Contains("retiro")
                    && v.FechaFin == null
                    && contrib.EsAbril
                    && (e.FechaVencimientoCalculada ?? e.FechaVencimiento) != null
                    && (e.FechaVencimientoCalculada ?? e.FechaVencimiento) >= hoy.AddDays(1)
                    && (e.FechaVencimientoCalculada ?? e.FechaVencimiento) <= ventanaFin
                select new
                {
                    Emo = e,
                    Worker = w,
                    TipoEmo = t,
                    Vinculacion = v,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    ContribNombre = contrib.ContributorName,
                    TipoEmoNombre = t.Nombre,
                    ProyectoNombre = proyecto != null ? proyecto.ProjectDescription : null
                }
            ).AsNoTracking().ToListAsync();

            if (candidatosRaw.Count == 0) return result;

            var candidatos = candidatosRaw
                .GroupBy(x => (x.Emo.WorkerId, x.Emo.TipoEmoId))
                .Select(g => g.OrderByDescending(x => x.Vinculacion.CreatedAt).First())
                .ToList();

            var workerIds = candidatos.Select(x => x.Emo.WorkerId).Distinct().ToList();

            // Sin filtro de fecha: una programación vieja que quedó colgada en un estado no
            // terminal (p. ej. "Programado" de una fecha ya pasada porque nadie la cerró) debe
            // seguir bloqueando al auto-programador igual que una vigente — de lo contrario se
            // generaba una segunda fila para el mismo trabajador/tipo EMO (bug real reportado).
            var programacionesExistentes = await ctx.SsProgramacionEmo
                .AsNoTracking()
                .Where(p =>
                    p.State
                    && workerIds.Contains(p.WorkerId)
                    && p.Estado != "Completado"
                    && p.Estado != "Cancelado"
                    && p.Estado != "Rechazado por Clínica"
                    && p.Estado != "No se presentó")
                .Select(p => new { p.WorkerId, p.TipoEmoId })
                .ToListAsync();

            var existentesSet = new HashSet<(int, int)>(
                programacionesExistentes.Select(p => (p.WorkerId, p.TipoEmoId)));

            var programados = new List<(int WorkerId, string Nombre, string RazonSocial, string? Proyecto, string TipoEmo, DateOnly Fecha)>();

            foreach (var c in candidatos)
            {
                try
                {
                    var tipoEmoIdOriginal = c.Emo.TipoEmoId!.Value;
                    var tipoEmoId = tipoEmoIdOriginal;
                    var tipoEmoNombre = c.TipoEmoNombre;
                    if (periodicoAnualTipoId.HasValue && string.Equals(c.TipoEmoNombre?.Trim(), "Ingreso", StringComparison.OrdinalIgnoreCase))
                    {
                        tipoEmoId = periodicoAnualTipoId.Value;
                        tipoEmoNombre = "Periódico Anual";
                    }

                    var clave = (c.Emo.WorkerId, tipoEmoId);
                    // Si el Ingreso original todavía tiene una programación activa (p.ej.
                    // "Aceptado por Clínica"), también hay que omitir: de lo contrario el
                    // chequeo de arriba solo mira la clave remapeada (Periódico Anual) y crea
                    // una segunda fila para el mismo trabajador mientras la de Ingreso sigue
                    // abierta (bug real reportado — se detectaron ~16 casos en producción).
                    var claveOriginal = (c.Emo.WorkerId, tipoEmoIdOriginal);

                    if (existentesSet.Contains(clave) || existentesSet.Contains(claveOriginal))
                    {
                        result.YaTenianProgramacion++;
                        result.Detalle.Add($"Worker {c.Worker.Id} ({c.WorkerNombre}) / TipoEMO {tipoEmoId} — ya tiene programación activa. Omitido.");
                        continue;
                    }

                    var fv = (c.Emo.FechaVencimientoCalculada ?? c.Emo.FechaVencimiento)!.Value;
                    var esOficina = EsCalendarioOficina(c.Worker);
                    var fechaDesdeVencimiento = cal.RestarDiasHabiles(fv, diasHabilesAntes, esOficina);
                    var fechaMinima = cal.SumarDiasHabiles(hoy, pisoDiasHabiles, esOficina);
                    var fechaProg = fechaDesdeVencimiento > fechaMinima ? fechaDesdeVencimiento : fechaMinima;

                    // Obra y Staff solo pueden asistir al EMO en sábado (Oficina Central sigue en
                    // día hábil normal). El sábado objetivo NO es "el más cercano a N días hábiles
                    // antes": para vencimientos Domingo/Lunes/Martes ese criterio dejaba el examen
                    // a 0-1 días hábiles del vencimiento, sin margen para levantar una
                    // interconsulta antes de que venza (bug real reportado). SabadoObjetivo fija
                    // 2 sábados antes para Dom/Lun/Mar y 1 sábado antes para Mié-Sáb — este último
                    // grupo ya tenía margen suficiente y da exactamente el mismo sábado que antes.
                    // Se ajusta igual al sábado hábil (no feriado) más cercano a ese objetivo,
                    // dentro de la ventana [fechaMinima, fv]; si no hay ninguno, se deja la fecha
                    // calculada por días hábiles como respaldo (mejor programar tarde que no
                    // programar).
                    if (c.Worker.ObraOficinaStaffId == ObraOficinaStaffIds.Obra
                        || c.Worker.ObraOficinaStaffId == ObraOficinaStaffIds.Staff)
                    {
                        var objetivoSabado = SabadoObjetivo(fv);
                        var sabado = MejorSabadoEnRango(objetivoSabado, fechaMinima, fv, cal);
                        if (sabado != null) fechaProg = sabado.Value;
                    }

                    var nueva = new SsProgramacionEmo
                    {
                        WorkerId          = c.Emo.WorkerId,
                        EmpresaId         = c.Vinculacion.EmpresaId,
                        TipoEmoId         = tipoEmoId,
                        ClinicaId         = 1,
                        FechaProgramada   = fechaProg,
                        Estado            = "Programado",
                        Origen            = "Automatico",
                        Motivo            = "Programación automática por vencimiento de EMO",
                        RegistradoPorId   = null,
                        FechaNotificacion = DateTimeOffset.UtcNow,
                        CreatedAt         = DateTimeOffset.UtcNow,
                        UpdatedAt         = DateTimeOffset.UtcNow
                    };

                    ctx.SsProgramacionEmo.Add(nueva);
                    await ctx.SaveChangesAsync();

                    programados.Add((
                        c.Worker.Id,
                        c.WorkerNombre ?? $"Worker {c.Worker.Id}",
                        c.ContribNombre,
                        c.ProyectoNombre,
                        tipoEmoNombre,
                        fechaProg));

                    result.Procesados++;
                    result.Detalle.Add($"Worker {c.Worker.Id} ({c.WorkerNombre}) / TipoEMO {tipoEmoId} — programado para {fechaProg:yyyy-MM-dd}.");
                }
                catch (Exception ex)
                {
                    result.Errores++;
                    _logger.LogError(ex, "Error procesando auto-programación para Worker {WorkerId}", c.Worker.Id);
                    result.Detalle.Add($"Worker {c.Worker.Id} ({c.WorkerNombre}) — error: {ex.Message}");
                }
            }

            if (programados.Count > 0)
                await EnviarResumenClinicaAsync(ctx, programados);

            return result;
        }

        private async Task EnviarResumenClinicaAsync(
            AppDbContext ctx,
            List<(int WorkerId, string Nombre, string RazonSocial, string? Proyecto, string TipoEmo, DateOnly Fecha)> programados)
        {
            try
            {
                const int clinicaId = 1;

                // Destinatarios según la matriz de Configuración de EMOs → sección
                // "Programación automática". Como el resumen agrupa a varios trabajadores,
                // se manda a la unión de lo que le corresponde a cada uno según su perfil.
                var destinatarios = await _destinatarios.ResolverLoteAsync(
                    EmoCorreoEventoCodigo.ProgramacionAutomatica,
                    programados.Select(p => p.WorkerId).ToList(),
                    clinicaId);

                var to = destinatarios.Para.Select(d => d.Email).ToList();
                var cc = destinatarios.Copias.Select(d => d.Email).ToList();

                // Sin destinatarios principales activos no se envía nada — es la forma
                // de silenciar el resumen desde la pantalla de Configuración de EMOs.
                if (to.Count == 0)
                {
                    _logger.LogWarning("Auto-programación EMO: sin destinatarios principales activos, no se envía el resumen.");
                    return;
                }

                var filas = string.Join("", programados
                    .OrderBy(p => p.Fecha)
                    .ThenBy(p => p.RazonSocial)
                    .ThenBy(p => p.Nombre)
                    .Select(p => $@"
                <tr>
                    <td style='border:1px solid #ddd;padding:8px;'>{p.Nombre}</td>
                    <td style='border:1px solid #ddd;padding:8px;'>{p.RazonSocial}</td>
                    <td style='border:1px solid #ddd;padding:8px;'>{p.Proyecto ?? "—"}</td>
                    <td style='border:1px solid #ddd;padding:8px;'>{p.TipoEmo}</td>
                    <td style='border:1px solid #ddd;padding:8px;text-align:center;'>{p.Fecha:dd/MM/yyyy}</td>
                </tr>"));

                var body = $@"
            <p>Estimados,</p>
            <p>Se han programado automáticamente los siguientes <strong>{programados.Count} Exámenes Médicos Ocupacionales (EMO)</strong> para los próximos días:</p>
            <table style='border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;'>
                <thead>
                    <tr>
                        <th style='border:1px solid #ddd;padding:8px;background:#f3f4f6;'>Trabajador</th>
                        <th style='border:1px solid #ddd;padding:8px;background:#f3f4f6;'>Empresa</th>
                        <th style='border:1px solid #ddd;padding:8px;background:#f3f4f6;'>Proyecto</th>
                        <th style='border:1px solid #ddd;padding:8px;background:#f3f4f6;'>Tipo EMO</th>
                        <th style='border:1px solid #ddd;padding:8px;background:#f3f4f6;'>Fecha programada</th>
                    </tr>
                </thead>
                <tbody>{filas}</tbody>
            </table>
            <p style='margin-top:16px;'>Por favor revisar y confirmar cada programación en el sistema.</p>
            <p style='font-size:12px;color:#666;margin-top:24px;'>
                Esta notificación se generó automáticamente por el sistema Abril.
            </p>";

                var subject = $"[EMO Programados] {programados.Count} trabajadores — {DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)):dd/MM/yyyy}";

                await _emailService.SendAsync(
                    to: to,
                    subject: subject,
                    body: body,
                    isHtml: true,
                    cc: cc.Count > 0 ? cc : null,
                    sender: SaludOcupacionalEmailConstants.SenderKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando resumen de auto-programación a clínica.");
            }
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

        private static bool EsCalendarioOficina(Worker worker)
        {
            return string.Equals(worker.ContrataCasa, "Casa", StringComparison.OrdinalIgnoreCase)
                && ObraOficinaStaffIds.StaffUOficinaCentral.Contains(worker.ObraOficinaStaffId ?? 0);
        }

        /// <summary>
        /// Sábado no feriado dentro de [minima, maxima] más cercano a <paramref name="objetivo"/>.
        /// Ante empate en distancia, prefiere el sábado más temprano (deja más margen antes del
        /// vencimiento). Devuelve null si no hay ningún sábado válido en la ventana.
        /// </summary>
        private static DateOnly? MejorSabadoEnRango(DateOnly objetivo, DateOnly minima, DateOnly maxima, CalendarioHabil cal)
        {
            if (minima > maxima) return null;

            var diffAlPrimerSabado = ((int)DayOfWeek.Saturday - (int)minima.DayOfWeek + 7) % 7;
            var sabado = minima.AddDays(diffAlPrimerSabado);

            DateOnly? mejor = null;
            var mejorDistancia = int.MaxValue;

            while (sabado <= maxima)
            {
                if (!cal.EsFeriado(sabado))
                {
                    var distancia = Math.Abs(sabado.DayNumber - objetivo.DayNumber);
                    if (distancia < mejorDistancia)
                    {
                        mejor = sabado;
                        mejorDistancia = distancia;
                    }
                }
                sabado = sabado.AddDays(7);
            }

            return mejor;
        }

        /// <summary>
        /// Sábado "ideal" antes de <paramref name="fv"/> según el día de la semana del
        /// vencimiento: 2 sábados antes para Domingo/Lunes/Martes (deja al menos un par de días
        /// hábiles libres después del examen para levantar una interconsulta antes de que venza
        /// el EMO); 1 sábado antes para Miércoles-Sábado (con ese margen ya alcanza). Nunca es
        /// el propio sábado de vencimiento: si <paramref name="fv"/> cae sábado, el objetivo es
        /// el sábado de la semana anterior, no el mismo día.
        /// </summary>
        private static DateOnly SabadoObjetivo(DateOnly fv)
        {
            var diasHastaSabadoAnterior = ((int)fv.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
            if (diasHastaSabadoAnterior == 0) diasHastaSabadoAnterior = 7;
            var sabadoInmediatoAnterior = fv.AddDays(-diasHastaSabadoAnterior);

            var necesitaDosSemanas = fv.DayOfWeek is DayOfWeek.Sunday or DayOfWeek.Monday or DayOfWeek.Tuesday;
            return necesitaDosSemanas ? sabadoInmediatoAnterior.AddDays(-7) : sabadoInmediatoAnterior;
        }
    }
}
