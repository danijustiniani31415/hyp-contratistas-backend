using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Models;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class InterconsultaRepository : IInterconsultaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IJefeRevisorResolver _jefeResolver;

        public InterconsultaRepository(
            IDbContextFactory<AppDbContext> factory,
            IJefeRevisorResolver jefeResolver)
        {
            _factory = factory;
            _jefeResolver = jefeResolver;
        }

        public async Task<PagedResult<InterconsultaListDto>> List(InterconsultaFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var q =
                from i in ctx.SsInterconsulta
                join w in ctx.Worker on i.WorkerId equals w.Id
                join m in ctx.SsMedicoOcupacional on i.MedicoDerivaId equals m.Id into mj
                from m in mj.DefaultIfEmpty()
                // Esta pantalla es solo para personal de Abril activo: excluye contratistas
                // (contrata_casa != "Casa") y trabajadores retirados.
                where w.ContrataCasa == "Casa" && WorkersEstadoIds.NoRetirados.Contains(w.WorkersEstadoId)
                select new
                {
                    i,
                    w,
                    m,
                    // Fuente principal: ss_hab_worker_proyecto (la que administra Habilitación en
                    // "Trabajadores > Proyectos asignados", más confiable). Solo cuenta la
                    // asignación activa (fecha_fin null) — si no hay ninguna, se deja en blanco en
                    // vez de mostrar un proyecto del que el trabajador ya salió.
                    // Se cae a worker_vinculaciones solo si el trabajador no tiene ninguna fila ahí.
                    ProyAsignada = ctx.WorkerProyecto
                        .Where(wp => wp.WorkerId == w.Id && wp.FechaFin == null)
                        .OrderByDescending(wp => wp.FechaInicio)
                        .ThenByDescending(wp => wp.Id)
                        .FirstOrDefault(),
                    VincActiva = ctx.WorkerVinculacion
                        .Where(v => v.WorkerId == w.Id && v.FechaFin == null)
                        .OrderByDescending(v => v.CreatedAt)
                        .ThenByDescending(v => v.Id)
                        .FirstOrDefault()
                };

            if (!string.IsNullOrWhiteSpace(filter.Estado))
                q = q.Where(x => x.i.Estado == filter.Estado);
            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.i.WorkerId == filter.WorkerId.Value);
            if (filter.ProyectoId.HasValue)
                q = q.Where(x =>
                    (x.ProyAsignada != null && x.ProyAsignada.ProyectoId == filter.ProyectoId.Value) ||
                    (x.ProyAsignada == null && x.VincActiva != null && x.VincActiva.ProyectoId == filter.ProyectoId.Value));
            if (filter.ContributorId.HasValue)
                q = q.Where(x =>
                    (x.ProyAsignada != null && x.ProyAsignada.EmpresaId == filter.ContributorId.Value) ||
                    (x.ProyAsignada == null && x.VincActiva != null && x.VincActiva.EmpresaId == filter.ContributorId.Value));
            if (filter.ObraOficinaStaffId.HasValue)
            {
                // Si obra_oficina_staff_id viene nulo se asume "Obra": solo Staff/Oficina Central
                // se marcan explícitamente en el dato, el resto son obreros por defecto.
                var ooId = filter.ObraOficinaStaffId.Value;
                q = ooId == ObraOficinaStaffIds.Obra
                    ? q.Where(x => x.w.ObraOficinaStaffId == ObraOficinaStaffIds.Obra || x.w.ObraOficinaStaffId == null)
                    : q.Where(x => x.w.ObraOficinaStaffId == ooId);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                q = q.Where(x =>
                    (x.w.Person != null && x.w.Person.FullName != null &&
                     EF.Functions.ILike(x.w.Person.FullName, $"%{term}%"))
                    || (x.w.Person != null && x.w.Person.DocumentIdentityCode != null &&
                     EF.Functions.ILike(x.w.Person.DocumentIdentityCode, $"%{term}%")));
            }

            var total = await q.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 15 : Math.Min(filter.PageSize, 100);

            var raw = await q
                .OrderByDescending(x => x.i.FechaDerivacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.i.Id,
                    x.i.EmoId,
                    x.i.WorkerId,
                    WorkerNombre = x.w.Person != null ? x.w.Person.FullName : null,
                    WorkerDni = x.w.Person != null ? x.w.Person.DocumentIdentityCode : null,
                    x.i.Especialidad,
                    MedicoDeriva = x.m != null ? x.m.ApellidoNombre : null,
                    x.i.FechaDerivacion,
                    x.i.FechaAtencion,
                    x.i.CentroAtencion,
                    x.i.Diagnostico,
                    x.i.Resultado,
                    x.i.Estado,
                    x.i.RequiereSeguimiento,
                    x.i.UrlInforme,
                    x.w.ObraOficinaStaffId,
                    x.w.ContrataCasa,
                    Categoria = x.w.PuestoCatalogo == null || x.w.PuestoCatalogo.Categoria == null ? null : x.w.PuestoCatalogo.Categoria.Nombre,
                    Puesto = x.w.PuestoCatalogo == null ? null : x.w.PuestoCatalogo.Nombre,
                    WorkerEmail = x.w.EmailCorporativo,
                    ProyectoId = (x.ProyAsignada != null ? (int?)x.ProyAsignada.ProyectoId : null) ?? (x.VincActiva != null ? (int?)x.VincActiva.ProyectoId : null),
                    EmpresaId = (x.ProyAsignada != null ? x.ProyAsignada.EmpresaId : null) ?? (x.VincActiva != null ? x.VincActiva.EmpresaId : null)
                })
                .ToListAsync();

            var proyectoIds = raw.Where(x => x.ProyectoId.HasValue).Select(x => x.ProyectoId!.Value).Distinct().ToList();
            var empresaIds = raw.Where(x => x.EmpresaId.HasValue).Select(x => x.EmpresaId!.Value).Distinct().ToList();

            var proyectoMap = await ctx.Project
                .Where(p => proyectoIds.Contains(p.ProjectId))
                .ToDictionaryAsync(p => p.ProjectId, p => p);
            var empresaMap = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .ToDictionaryAsync(c => c.ContributorId, c => c);
            // Jefe de cada trabajador desde la configuración global de revisores
            // (revisor directo → revisor del área → fallback GTH), en un solo lote.
            var jefeMap = await _jefeResolver.ResolveManyAsync(
                raw.Select(x => x.WorkerId).Distinct().ToList());

            var items = raw.Select(x =>
            {
                Project? proyecto = x.ProyectoId.HasValue && proyectoMap.TryGetValue(x.ProyectoId.Value, out var p) ? p : null;
                Contributor? empresa = x.EmpresaId.HasValue && empresaMap.TryGetValue(x.EmpresaId.Value, out var e) ? e : null;
                var esOficinaCentral = x.ObraOficinaStaffId == ObraOficinaStaffIds.OficinaCentral;

                jefeMap.TryGetValue(x.WorkerId, out var jefe);
                var jefaturaNombre = jefe?.Nombre;
                var jefaturaEmail = jefe?.Email;

                return new InterconsultaListDto
                {
                    Id = x.Id,
                    EmoId = x.EmoId,
                    WorkerId = x.WorkerId,
                    WorkerNombre = x.WorkerNombre,
                    WorkerDni = x.WorkerDni,
                    // Oficina Central no pertenece a un proyecto de obra: su unidad organizativa es
                    // la jefatura, no la última vinculación (que puede quedar obsoleta si el trabajador
                    // pasó de obra a oficina central sin cerrarse en worker_vinculaciones).
                    ProyectoId = esOficinaCentral ? null : x.ProyectoId,
                    ProyectoNombre = esOficinaCentral ? "Oficina Central" : proyecto?.ProjectDescription,
                    ContributorId = x.EmpresaId,
                    RazonSocial = empresa?.ContributorName,
                    ObraOficinaStaffId = x.ObraOficinaStaffId,
                    ObraOficina = ObraOficinaStaffIds.Nombre(x.ObraOficinaStaffId),
                    ContrataCasa = x.ContrataCasa,
                    Categoria = x.Categoria,
                    Puesto = x.Puesto,
                    WorkerEmail = x.WorkerEmail,
                    AdministradorEmail = proyecto?.EmailCoordAdmin ?? empresa?.EmailAdministrador,
                    Jefatura = jefaturaNombre,
                    JefaturaEmail = jefaturaEmail,
                    Especialidad = x.Especialidad,
                    MedicoDeriva = x.MedicoDeriva,
                    FechaDerivacion = x.FechaDerivacion,
                    FechaAtencion = x.FechaAtencion,
                    CentroAtencion = x.CentroAtencion,
                    Diagnostico = x.Diagnostico,
                    Resultado = x.Resultado,
                    Estado = x.Estado,
                    RequiereSeguimiento = x.RequiereSeguimiento,
                    UrlInforme = x.UrlInforme
                };
            }).ToList();

            foreach (var it in items)
                if (it.Estado == "Pendiente")
                    it.DiasPendiente = hoy.DayNumber - it.FechaDerivacion.DayNumber;

            return new PagedResult<InterconsultaListDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = items
            };
        }

        public async Task<List<InterconsultaEnvioInfoDto>> GetForEnvioCorreo(List<int> ids)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var raw = await (
                from i in ctx.SsInterconsulta
                join w in ctx.Worker on i.WorkerId equals w.Id
                where ids.Contains(i.Id)
                select new
                {
                    i.Id,
                    i.WorkerId,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    WorkerDni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                    i.Especialidad,
                    i.FechaDerivacion,
                    w.ObraOficinaStaffId,
                    w.ContrataCasa,
                    WorkerEmail = w.EmailCorporativo,
                    ProyAsignada = ctx.WorkerProyecto
                        .Where(wp => wp.WorkerId == w.Id && wp.FechaFin == null)
                        .OrderByDescending(wp => wp.FechaInicio)
                        .ThenByDescending(wp => wp.Id)
                        .FirstOrDefault(),
                    VincActiva = ctx.WorkerVinculacion
                        .Where(v => v.WorkerId == w.Id && v.FechaFin == null)
                        .OrderByDescending(v => v.CreatedAt)
                        .ThenByDescending(v => v.Id)
                        .FirstOrDefault()
                }
            ).ToListAsync();

            var proyectoIds = raw
                .Select(x => x.ProyAsignada?.ProyectoId ?? x.VincActiva?.ProyectoId)
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var empresaIds = raw
                .Select(x => x.ProyAsignada?.EmpresaId ?? x.VincActiva?.EmpresaId)
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

            var proyectoMap = await ctx.Project
                .Where(p => proyectoIds.Contains(p.ProjectId))
                .ToDictionaryAsync(p => p.ProjectId, p => p);
            var empresaMap = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .ToDictionaryAsync(c => c.ContributorId, c => c);
            // Jefe de cada trabajador desde la configuración global de revisores
            // (revisor directo → revisor del área → fallback GTH), en un solo lote.
            var jefeMap = await _jefeResolver.ResolveManyAsync(
                raw.Select(x => x.WorkerId).Distinct().ToList());

            return raw.Select(x =>
            {
                var esOficinaCentral = x.ObraOficinaStaffId == ObraOficinaStaffIds.OficinaCentral;
                var proyectoId = esOficinaCentral ? null : (x.ProyAsignada?.ProyectoId ?? x.VincActiva?.ProyectoId);
                var empresaId = x.ProyAsignada?.EmpresaId ?? x.VincActiva?.EmpresaId;
                Project? proyecto = proyectoId.HasValue && proyectoMap.TryGetValue(proyectoId.Value, out var p) ? p : null;
                Contributor? empresa = empresaId.HasValue && empresaMap.TryGetValue(empresaId.Value, out var e) ? e : null;

                jefeMap.TryGetValue(x.WorkerId, out var jefe);
                var jefaturaNombre = jefe?.Nombre;
                var jefaturaEmail = jefe?.Email;

                return new InterconsultaEnvioInfoDto
                {
                    Id = x.Id,
                    WorkerId = x.WorkerId,
                    WorkerNombre = x.WorkerNombre,
                    WorkerDni = x.WorkerDni,
                    Especialidad = x.Especialidad,
                    FechaDerivacion = x.FechaDerivacion,
                    DiasPendiente = hoy.DayNumber - x.FechaDerivacion.DayNumber,
                    WorkerEmailCorporativo = x.WorkerEmail,
                    ObraOficinaStaffId = x.ObraOficinaStaffId,
                    ObraOficina = ObraOficinaStaffIds.Nombre(x.ObraOficinaStaffId),
                    ContrataCasa = x.ContrataCasa,
                    Jefatura = jefaturaNombre,
                    JefaturaEmail = jefaturaEmail,
                    ProyectoId = proyectoId,
                    ProyectoNombre = esOficinaCentral ? "Oficina Central" : proyecto?.ProjectDescription,
                    ProyectoEmailCoordAdmin = proyecto?.EmailCoordAdmin,
                    ProyectoEmailResidente = proyecto?.EmailResidente,
                    ProyectoEmailResponsable = proyecto?.EmailResponsable,
                    ProyectoEmailRrhh = proyecto?.EmailRrhh,
                    ProyectoEmailCoordSsoma = proyecto?.EmailCoordSsoma,
                    ContributorId = empresaId,
                    ContributorNombre = empresa?.ContributorName,
                    ContributorEmailAdministrador = empresa?.EmailAdministrador
                };
            }).ToList();
        }

        public async Task<InterconsultaDetalleDto> GetById(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var row = await (
                from i in ctx.SsInterconsulta
                join w in ctx.Worker on i.WorkerId equals w.Id
                join m in ctx.SsMedicoOcupacional on i.MedicoDerivaId equals m.Id into mj
                from m in mj.DefaultIfEmpty()
                where i.Id == id
                select new InterconsultaDetalleDto
                {
                    Id = i.Id,
                    EmoId = i.EmoId,
                    WorkerId = i.WorkerId,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    WorkerDni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                    Especialidad = i.Especialidad,
                    Medico = m != null ? m.ApellidoNombre : null,
                    FechaDerivacion = i.FechaDerivacion,
                    FechaAtencion = i.FechaAtencion,
                    CentroAtencion = i.CentroAtencion,
                    Diagnostico = i.Diagnostico,
                    Cie10 = i.Cie10,
                    Resultado = i.Resultado,
                    UrlInforme = i.UrlInforme,
                    Estado = i.Estado,
                    RequiereSeguimiento = i.RequiereSeguimiento
                }
            ).FirstOrDefaultAsync() ?? throw new AbrilException("Interconsulta no encontrada.", 404);

            if (row.Estado == "Pendiente")
                row.DiasPendiente = hoy.DayNumber - row.FechaDerivacion.DayNumber;

            return row;
        }

        public async Task<int> Create(InterconsultaCreateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            if (dto.EmoId.HasValue)
                _ = await ctx.WorkerEmo.FirstOrDefaultAsync(e => e.Id == dto.EmoId.Value)
                    ?? throw new AbrilException("EMO no encontrado.", 404);
            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == dto.WorkerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            var ent = new SsInterconsulta
            {
                EmoId = dto.EmoId,
                WorkerId = dto.WorkerId,
                Especialidad = dto.Especialidad,
                MedicoDerivaId = dto.MedicoDerivaId,
                FechaDerivacion = DateOnly.FromDateTime(DateTime.Today),
                CentroAtencion = dto.CentroAtencion,
                RequiereSeguimiento = dto.RequiereSeguimiento,
                UrlInforme = dto.UrlInforme,
                Estado = "Pendiente",
                RegistradoPorId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            ctx.SsInterconsulta.Add(ent);

            // Mover la programación activa a "En Interconsulta" para que
            // no aparezca en la agenda normal hasta que la clínica suba el levantamiento.
            // Excluye también "No se presentó": sin eso podía tomar una fila cerrada por
            // inasistencia y reabrirla, chocando con otra programación activa del mismo
            // trabajador/tipo creada mientras tanto (mismo bug de fondo que en
            // ProgramacionEmoRepository — ver ese archivo).
            var prog = await ctx.SsProgramacionEmo
                .Where(p => p.State
                         && p.WorkerId == dto.WorkerId
                         && p.Estado != "Completado"
                         && p.Estado != "Cancelado"
                         && p.Estado != "Rechazado por Clínica"
                         && p.Estado != "No se presentó"
                         && p.Estado != "En Interconsulta")
                .OrderByDescending(p => p.FechaProgramada)
                .FirstOrDefaultAsync();
            if (prog != null)
            {
                prog.Estado = "En Interconsulta";
                prog.UpdatedAt = DateTimeOffset.UtcNow;
            }

            var lecturaEmo = await ctx.SsHabTrabajador
                .FirstOrDefaultAsync(h => h.WorkerId == dto.WorkerId && h.ItemId == 25);
            if (lecturaEmo != null)
            {
                lecturaEmo.Estado = "En revision";
                lecturaEmo.ObsAbril = $"Interconsulta pendiente — {dto.Especialidad}";
                lecturaEmo.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolationProgramacion(ex))
            {
                throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);
            }

            return ent.Id;
        }

        public async Task Update(int id, InterconsultaUpdateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsInterconsulta.FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new AbrilException("Interconsulta no encontrada.", 404);
            ent.Especialidad = dto.Especialidad;
            ent.MedicoDerivaId = dto.MedicoDerivaId;
            ent.FechaDerivacion = dto.FechaDerivacion;
            ent.FechaAtencion = dto.FechaAtencion;
            ent.CentroAtencion = dto.CentroAtencion;
            ent.Diagnostico = dto.Diagnostico;
            ent.Cie10 = dto.Cie10;
            ent.Resultado = dto.Resultado;
            ent.UrlInforme = dto.UrlInforme;
            ent.RequiereSeguimiento = dto.RequiereSeguimiento;
            ent.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateResultado(int id, InterconsultaResultadoPatchDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsInterconsulta.FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new AbrilException("Interconsulta no encontrada.", 404);
            ent.Estado = dto.Estado;
            if (dto.FechaAtencion.HasValue) ent.FechaAtencion = dto.FechaAtencion;
            if (dto.Diagnostico != null) ent.Diagnostico = dto.Diagnostico;
            if (dto.Cie10 != null) ent.Cie10 = dto.Cie10;
            if (dto.Resultado != null) ent.Resultado = dto.Resultado;
            if (dto.UrlInforme != null) ent.UrlInforme = dto.UrlInforme;
            if (dto.RequiereSeguimiento.HasValue) ent.RequiereSeguimiento = dto.RequiereSeguimiento.Value;
            ent.UpdatedAt = DateTimeOffset.UtcNow;

            if (dto.Estado == "Atendida" || dto.Estado == "Completado")
            {
                WorkerEmo? emo = null;
                if (ent.EmoId.HasValue)
                {
                    emo = await ctx.WorkerEmo.FirstOrDefaultAsync(e => e.Id == ent.EmoId.Value);
                    if (emo != null)
                    {
                        emo.InterconsultaResuelta = true;
                        emo.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }

                var lecturaEmo = await ctx.SsHabTrabajador
                    .FirstOrDefaultAsync(h => h.WorkerId == ent.WorkerId && h.ItemId == 25);
                if (lecturaEmo != null)
                {
                    lecturaEmo.Estado = "En revision";
                    lecturaEmo.ObsAbril = $"Interconsulta levantada — pendiente EMO — {dto.FechaAtencion}";
                    lecturaEmo.UpdatedAt = DateTime.UtcNow;
                }

                // Se ubica la programación por el vínculo real (EmoResultadoId), no por heurística de
                // "la más reciente del trabajador" — con varias programaciones abiertas esa heurística
                // podía tocar la fila equivocada.
                // Una programación dada de baja (State = false) no se reabre: cuenta como
                // inexistente y el flujo cae al else de abajo, que crea una nueva.
                var prog = ent.EmoId.HasValue
                    ? await ctx.SsProgramacionEmo.FirstOrDefaultAsync(p => p.EmoResultadoId == ent.EmoId.Value && p.State)
                    : null;

                // Si el EMO ya tiene una aptitud TERMINAL (se leyó/calificó en algún momento,
                // incluso antes de que se levantara esta interconsulta), el caso ya está resuelto
                // de verdad: no hay nada más que "atender". "Pendiente" y "Observado" NO cuentan:
                // "Pendiente" es el valor por defecto sin evaluar, y "Observado" es justo el valor
                // que dispara la creación de la interconsulta (EmoService.ValidarComun exige
                // Aptitud == "Observado" para RequiereInterconsulta) — tratarlo como resuelto
                // cerraría el caso antes de que el médico dé un veredicto final. Tampoco cuenta un
                // EMO "Anulado" (registro invalidado/reemplazado).
                //
                // Sin este chequeo se creaba/reabría la programación en "En Atención" con un
                // resultado que ya tenía meses, quedando pegada ahí para siempre porque nadie en
                // clínica tiene un examen pendiente que completar (ver programación #798:
                // EmoResultadoId ya apuntaba a un WorkerEmo "Vigente" con aptitud "Apto con
                // Restricciones" desde mayo, pero la fila se creó en "En Atención" en julio).
                var aptitudesTerminales = new HashSet<string> { "Apto", "Apto con Restricciones", "No Apto" };
                var yaResuelto = emo != null
                    && emo.Estado != "Anulado"
                    && !string.IsNullOrWhiteSpace(emo.Aptitud)
                    && aptitudesTerminales.Contains(emo.Aptitud);
                var estadoResultante = yaResuelto ? "Completado" : "En Atención";

                if (prog != null)
                {
                    prog.Estado = estadoResultante;
                    prog.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else if (emo != null)
                {
                    // El EMO se registró sin pasar por una cita agendada (sin fila en
                    // ss_programacion_emos que reabrir), así que se crea una para que el caso
                    // aparezca en la bandeja de Atenciones (o directo en Completado si ya tiene
                    // aptitud) y se pueda subir lectura/EMO y dar aptitud.
                    //
                    // EmpresaId es obligatorio: Agenda (ProgramacionEmoRepository.List) descarta
                    // toda fila con empresa nula o EsAbril=false, así que sin esto la programación
                    // queda "En Atención" para siempre — invisible en Agenda, pero visible como
                    // huérfana en Habilitación/Trabajadores (no filtra por empresa). Se resuelve
                    // igual que ProgramacionEmoRepository.Create(): última vinculación activa.
                    var hoyVinc = DateOnly.FromDateTime(DateTime.Today);
                    var empresaId = await ctx.WorkerVinculacion
                        .Where(v => v.WorkerId == ent.WorkerId && (v.FechaFin == null || v.FechaFin >= hoyVinc))
                        .OrderByDescending(v => v.FechaInicio)
                        .Select(v => (int?)v.EmpresaId)
                        .FirstOrDefaultAsync();

                    ctx.SsProgramacionEmo.Add(new SsProgramacionEmo
                    {
                        WorkerId = ent.WorkerId,
                        EmpresaId = empresaId,
                        TipoEmoId = emo.TipoEmoId ?? 0,
                        FechaProgramada = DateOnly.FromDateTime(DateTime.UtcNow),
                        ClinicaId = emo.ClinicaId,
                        MedicoId = emo.MedicoId,
                        Estado = estadoResultante,
                        EmoResultadoId = emo.Id,
                        Origen = "Interconsulta",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            // Al resolver la interconsulta, prog/la nueva fila puede pasar a "En Atención" (no
            // terminal) — si por lo que sea el trabajador ya tiene otra programación activa del
            // mismo tipo, choca con ux_programacion_emo_worker_tipo_activa. Se traduce a un 409
            // en vez de un 500 crudo, igual que ProgramacionEmoRepository.
            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolationProgramacion(ex))
            {
                throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);
            }
        }

        private static bool IsUniqueViolationProgramacion(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("ux_programacion_emo_worker_tipo_activa", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true;

        public async Task UpdateDerivacion(int id, InterconsultaDerivacionPatchDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsInterconsulta.FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new AbrilException("Interconsulta no encontrada.", 404);
            ent.Especialidad = dto.Especialidad;
            ent.Diagnostico = dto.Diagnostico;
            ent.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        // La resolución del jefe del trabajador vive en IJefeRevisorResolver (Shared):
        // revisor directo (workers_revisores) → revisor del área (area_revisores) →
        // fallback GTH, todo desde la configuración global de /configuracion. Aquí había
        // tres mecanismos propios (worker_lesson_jefe_id/worker_salida_jefe_id, el cruce
        // por nombre contra cat_jefatura, y un recorrido del árbol area_scope buscando
        // trabajadores de categoría Jefe/Sub Gerente/Coordinador/Gerente) que quedaron
        // reemplazados por esa única fuente.
    }
}
