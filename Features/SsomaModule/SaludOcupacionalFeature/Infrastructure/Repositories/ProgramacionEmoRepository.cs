using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Shared;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Helpers;
using Abril_Backend.Shared.Services;
using Abril_Backend.Shared.Services.ReclutamientoEmoIngreso.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class ProgramacionEmoRepository : IProgramacionEmoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IEmoDestinatariosResolver _destinatarios;
        private readonly IReclutamientoEmoIngresoService _reclutamiento;
        private readonly ILogger<ProgramacionEmoRepository> _logger;

        public ProgramacionEmoRepository(
            IDbContextFactory<AppDbContext> factory,
            IEmailService emailService,
            IConfiguration configuration,
            IEmoDestinatariosResolver destinatarios,
            IReclutamientoEmoIngresoService reclutamiento,
            ILogger<ProgramacionEmoRepository> logger)
        {
            _factory = factory;
            _emailService = emailService;
            _configuration = configuration;
            _destinatarios = destinatarios;
            _reclutamiento = reclutamiento;
            _logger = logger;
        }

        public async Task<PagedResponseDto<ProgramacionListDto>> List(ProgramacionFilterDto filter)
        {
            try
            {
                using var ctx = _factory.CreateDbContext();

                var q =
                    from p in ctx.SsProgramacionEmo
                    join w in ctx.Worker on p.WorkerId equals w.Id
                    join per in ctx.Person on w.PersonId equals per.PersonId into perj
                    from per in perj.DefaultIfEmpty()
                    join em in ctx.Contributor on p.EmpresaId equals em.ContributorId into ej
                    from em in ej.DefaultIfEmpty()
                    join t in ctx.SsEmoTipo on p.TipoEmoId equals t.Id into tj
                    from t in tj.DefaultIfEmpty()
                    join c in ctx.SsClinica on p.ClinicaId equals c.Id into cj
                    from c in cj.DefaultIfEmpty()
                    join m in ctx.SsMedicoOcupacional on p.MedicoId equals m.Id into mj
                    from m in mj.DefaultIfEmpty()
                    select new { p, w, per, em, t, c, m };

                q = q.Where(x => x.p.State);
                q = q.Where(x => x.em != null && x.em.EsAbril);
                // Se excluyen los estados de salida (retirado y el finalista que no llego a
                // ingresar), no "todo lo que no sea trabajador": un FINALISTA_APROBADO tiene
                // que verse aca, porque su EMO de Ingreso es justamente lo que GTH le programa
                // antes de que firme. La empresa la trae la propia programacion (p.EmpresaId),
                // no la vinculacion, asi que la fila aparece aunque todavia no tenga contrato.
                q = q.Where(x => x.w.WorkersEstadoId != WorkersEstadoIds.Retirado
                              && x.w.WorkersEstadoId != WorkersEstadoIds.NoIngreso);

                // La clínica no puede procesar trabajadores con interconsulta pendiente.
                // El médico SSOMA (IncluirConInterconsulta = true) ve todas sin excepción.
                if (!filter.IncluirConInterconsulta)
                {
                    q = q.Where(x => x.p.Estado != "En Interconsulta");
                    q = q.Where(x => !ctx.SsInterconsulta
                        .Any(i => i.WorkerId == x.p.WorkerId && i.Estado == "Pendiente"));
                }

                if (filter.Desde.HasValue)
                    q = q.Where(x => x.p.FechaProgramada >= filter.Desde.Value);
                if (filter.Hasta.HasValue)
                    q = q.Where(x => x.p.FechaProgramada <= filter.Hasta.Value);
                if (!string.IsNullOrWhiteSpace(filter.Estado))
                    q = q.Where(x => x.p.Estado == filter.Estado);
                if (filter.WorkerId.HasValue)
                    q = q.Where(x => x.p.WorkerId == filter.WorkerId.Value);
                if (filter.ClinicaId.HasValue)
                    q = q.Where(x => x.p.ClinicaId == filter.ClinicaId.Value);
                if (filter.AreaScopeId.HasValue)
                {
                    var idsArea = await ctx.ResolveDescendantsAsync(filter.AreaScopeId.Value);
                    q = q.Where(x => x.w.AreaScopeId != null && idsArea.Contains(x.w.AreaScopeId.Value));
                }
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var term = filter.Search.Trim().ToLower();
                    q = q.Where(x =>
                        (x.per != null && x.per.FullName != null && x.per.FullName.ToLower().Contains(term)) ||
                        (x.per != null && x.per.DocumentIdentityCode != null && x.per.DocumentIdentityCode.Contains(term)));
                }

                var totalRecords = await q.CountAsync();
                var page = Math.Max(filter.Page, 1);
                // La Agenda pide pageSize=500 para traer todo sin paginar; el tope viejo de 200
                // cortaba silenciosamente el resto de registros (los mas antiguos por el ORDER BY).
                var pageSize = Math.Clamp(filter.PageSize, 1, 2000);
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                // Con el historico creciendo, "traer todo sin filtro de fecha" (como hace la Agenda
                // de clinica) puede superar pageSize y el ORDER BY antiguo (solo por fecha ascendente)
                // cortaba las programaciones activas mas recientes dejandolas fuera de la pagina 1.
                // Mostrar primero las no terminales garantiza que Programado/Aceptado/En Atencion
                // nunca desaparezcan por el corte de pagina, sin cambiar el filtrado ni el conteo.
                var estadosTerminales = new HashSet<string> { "Completado", "Cancelado", "Rechazado por Clínica", "No se presentó" };

                var data = await q
                    .OrderBy(x => estadosTerminales.Contains(x.p.Estado) ? 1 : 0)
                    .ThenByDescending(x => x.p.FechaProgramada)
                    .ThenBy(x => x.p.HoraProgramada)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new ProgramacionListDto
                    {
                        Id = x.p.Id,
                        WorkerId = x.p.WorkerId,
                        WorkerNombre = x.per != null ? x.per.FullName : null,
                        WorkerDni = x.per != null ? x.per.DocumentIdentityCode : null,
                        Empresa = x.em != null ? x.em.ContributorName : null,
                        Proyecto = (from v in ctx.WorkerVinculacion
                                    join pr in ctx.Project on v.ProyectoId equals (int?)pr.ProjectId
                                    where v.WorkerId == x.p.WorkerId && v.FechaFin == null
                                    orderby v.CreatedAt descending
                                    select (string?)pr.ProjectDescription)
                                   .FirstOrDefault(),
                        TipoEmoId = x.p.TipoEmoId,
                        TipoEmo = x.t != null ? x.t.Nombre : null,
                        FechaProgramada = x.p.FechaProgramada,
                        HoraProgramada = x.p.HoraProgramada,
                        Clinica = x.c != null ? x.c.Nombre : null,
                        Medico = x.m != null ? x.m.ApellidoNombre : null,
                        Estado = x.p.Estado,
                        Motivo = x.p.Motivo,
                        EmoResultadoId = x.p.EmoResultadoId,
                        Origen = x.p.Origen,
                        CheckInHora = x.p.CheckInHora,
                        MotivoRechazo = x.p.MotivoRechazo,
                        FechaNotificacion = x.p.FechaNotificacion,
                        Puesto = x.w.PuestoCatalogo == null ? null : x.w.PuestoCatalogo.Nombre,
                        Categoria = x.w.PuestoCatalogo == null || x.w.PuestoCatalogo.Categoria == null ? null : x.w.PuestoCatalogo.Categoria.Nombre,
                        TipoTrabajador = x.w.ContrataCasa == "Casa" && x.w.ObraOficinaStaffId == ObraOficinaStaffIds.OficinaCentral
                            ? "Oficina Central"
                            : x.w.ContrataCasa == "Casa" && x.w.ObraOficinaStaffId == ObraOficinaStaffIds.Staff
                                ? "Staff Obra"
                                : "Obrero",
                        FechaVencimientoEmo = ctx.WorkerEmo
                            .Where(e => e.WorkerId == x.p.WorkerId && e.Activo)
                            .OrderByDescending(e => e.FechaVencimientoCalculada ?? e.FechaVencimiento)
                            .Select(e => (DateOnly?)(e.FechaVencimientoCalculada ?? e.FechaVencimiento))
                            .FirstOrDefault(),
                        InterconsultaEstado = ctx.SsInterconsulta
                            .Where(i => i.WorkerId == x.p.WorkerId)
                            .OrderByDescending(i => i.FechaDerivacion)
                            .Select(i => (string?)i.Estado)
                            .FirstOrDefault(),
                        TieneInterconsulta = ctx.SsInterconsulta
                            .Any(i => i.WorkerId == x.p.WorkerId && i.Estado == "Pendiente")
                    })
                    .ToListAsync();

                return new PagedResponseDto<ProgramacionListDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages == 0 ? 1 : totalPages,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("PROGRAMACION_LIST_ERROR estado={Estado} | {Ex}", filter.Estado, ex.ToString());
                throw;
            }
        }

        public async Task<ProgramacionResumenDto> GetResumen(ProgramacionFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();

            var q =
                from p in ctx.SsProgramacionEmo
                join w in ctx.Worker on p.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals per.PersonId into perj
                from per in perj.DefaultIfEmpty()
                join em in ctx.Contributor on p.EmpresaId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                select new { p, per, em };

            q = q.Where(x => x.p.State);
            q = q.Where(x => x.em != null && x.em.EsAbril);

            if (!filter.IncluirConInterconsulta)
            {
                q = q.Where(x => x.p.Estado != "En Interconsulta");
                q = q.Where(x => !ctx.SsInterconsulta
                    .Any(i => i.WorkerId == x.p.WorkerId && i.Estado == "Pendiente"));
            }

            // El resumen ignora filter.Estado a propósito: debe mostrar el desglose
            // por estado sobre el resto de filtros, no solo el estado seleccionado.
            if (filter.Desde.HasValue)
                q = q.Where(x => x.p.FechaProgramada >= filter.Desde.Value);
            if (filter.Hasta.HasValue)
                q = q.Where(x => x.p.FechaProgramada <= filter.Hasta.Value);
            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.p.WorkerId == filter.WorkerId.Value);
            if (filter.ClinicaId.HasValue)
                q = q.Where(x => x.p.ClinicaId == filter.ClinicaId.Value);
            if (filter.AreaScopeId.HasValue)
            {
                var idsArea = await ctx.ResolveDescendantsAsync(filter.AreaScopeId.Value);
                q = q.Where(x => x.p.Worker != null && x.p.Worker.AreaScopeId != null && idsArea.Contains(x.p.Worker.AreaScopeId.Value));
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                q = q.Where(x =>
                    (x.per != null && x.per.FullName != null && x.per.FullName.ToLower().Contains(term)) ||
                    (x.per != null && x.per.DocumentIdentityCode != null && x.per.DocumentIdentityCode.Contains(term)));
            }

            var estadoCounts = await q
                .GroupBy(x => x.p.Estado)
                .Select(g => new { Estado = g.Key, Count = g.Count() })
                .ToListAsync();

            var automaticos = await q.CountAsync(x => x.p.Origen == "Automatico");

            int CountFor(string estado) => estadoCounts.FirstOrDefault(e => e.Estado == estado)?.Count ?? 0;

            return new ProgramacionResumenDto
            {
                Programados = CountFor("Programado"),
                Aceptados = CountFor("Aceptado por Clínica"),
                EnAtencion = CountFor("En Atención"),
                Completados = CountFor("Completado"),
                Rechazados = CountFor("Rechazado por Clínica"),
                NoPresento = CountFor("No se presentó"),
                Automaticos = automaticos,
                Total = estadoCounts.Sum(e => e.Count),
            };
        }

        public async Task<int> Create(ProgramacionCreateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            // WorkersEstado va en el Include porque el EMO de Ingreso que viene de Reclutamiento
            // necesita `esta_adentro` (ver más abajo) y no vale gastar un roundtrip aparte.
            var worker = await ctx.Worker
                    .Include(w => w.Person)
                    .Include(w => w.WorkersEstado)
                    .FirstOrDefaultAsync(w => w.Id == dto.WorkerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            if (dto.FechaProgramada == default)
                throw new AbrilException("La fecha es obligatoria.", 400);

            // Programar es reservar una cita a futuro con la clínica: una fecha pasada no es una
            // cita sino un dato mal tipeado, y además dispararía el correo de programación con un
            // día que ya no existe. El calendario del modal ya las deshabilita; esto cubre el
            // resto de vías (el portal de la clínica, un POST directo). "Hoy" en hora de Lima,
            // que es la del usuario que agenda — en UTC, después de las 19:00 sería mañana.
            var hoyLima = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            if (dto.FechaProgramada < hoyLima)
                throw new AbrilException(
                    "La fecha programada no puede ser anterior a hoy.", 400);

            // Evita duplicados: si ya hay una programación activa para este trabajador
            // y este tipo de EMO, no crear otra (antes solo el auto-programador validaba esto).
            // Una programación dada de baja (State = false) no bloquea: justamente se da de
            // baja para poder rehacerla.
            //
            // Excepción: "En Interconsulta" (InterconsultaRepository.Create la deja en ese
            // estado mientras se espera el levantamiento — ver ese archivo) puede quedar
            // atascada semanas si la interconsulta se demora. Acá no se bloquea: se cierra esa
            // fila vieja como "Cancelado" (con nota) y se sigue con la programación nueva, para
            // no dejar dos filas "activas" del mismo trabajador/tipo EMO compitiendo entre sí.
            // La interconsulta original (ss_interconsulta) no se toca — sigue como historial,
            // solo deja de tener una programación "en curso" que la sostenga.
            var progBloqueante = await ctx.SsProgramacionEmo.FirstOrDefaultAsync(p =>
                p.State &&
                p.WorkerId == dto.WorkerId &&
                p.TipoEmoId == dto.TipoEmoId &&
                p.Estado != "Completado" &&
                p.Estado != "Cancelado" &&
                p.Estado != "Rechazado por Clínica" &&
                p.Estado != "No se presentó");

            if (progBloqueante != null && progBloqueante.Estado != "En Interconsulta")
                throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);

            if (progBloqueante != null && progBloqueante.Estado == "En Interconsulta")
            {
                progBloqueante.Estado = "Cancelado";
                progBloqueante.Motivo = string.IsNullOrWhiteSpace(progBloqueante.Motivo)
                    ? "Reprogramado: la interconsulta pendiente se demoró demasiado."
                    : $"{progBloqueante.Motivo} — Reprogramado: la interconsulta pendiente se demoró demasiado.";
                progBloqueante.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Ficha de pre-ingreso (finalista aprobado de Reclutamiento): el unico examen que
            // tiene sentido antes de firmar es el de Ingreso. Se valida en el backend y no solo
            // ocultando opciones en el desplegable, porque el endpoint es publico a la app.
            var esFinalistaAprobado = worker.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado;

            // El tipo se resuelve una sola vez: lo usan la regla del finalista y el freno de abajo.
            // Es un lookup por PK, y la alternativa (resolverlo dentro de cada rama) lo consultaba
            // dos veces en el caso del finalista.
            var tipoEsIngreso = await ctx.SsEmoTipo
                .AnyAsync(t => t.Id == dto.TipoEmoId && t.Nombre.ToLower() == "ingreso");

            if (esFinalistaAprobado && !tipoEsIngreso)
                throw new AbrilException(
                    "A un finalista aprobado solo se le puede programar el EMO de Ingreso: "
                    + "todavia no tiene contrato firmado.", 400);

            // Freno "porsiacaso" del EMO de Ingreso que viene de Reclutamiento: nadie que ya
            // trabaje en Abril deberia poder llegar hasta aca. La aprobacion del formulario del
            // postulante bloquea los documentos de trabajadores que estan adentro
            // (CoincidenciaPersonaQuery), asi que sin ficha en `person` no hay finalista y sin
            // finalista no hay este paso. Si igual pasa, se corta: programarle un EMO de INGRESO a
            // alguien que ya esta dentro de la empresa significa que el proceso se cruzo con la
            // ficha equivocada, y seguir dejaria la cita colgada del worker de otra persona.
            //
            // El freno esta acotado a los dos lados a proposito, para no romper flujos legitimos:
            //   • solo si el examen es de Ingreso — un EMO periodico o de retiro de un trabajador
            //     activo es lo normal y no se toca;
            //   • solo si este worker es el finalista de un requerimiento que sigue en la fase
            //     EMO_INGRESO — o sea, solo cuando la cita es el paso de Reclutamiento. Un
            //     reingreso, que deja al trabajador ACTIVO y si necesita EMO de Ingreso, no cae
            //     aca porque no tiene requerimiento en esa fase.
            if (tipoEsIngreso && worker.WorkersEstado?.EstaAdentro == true
                && await EsEmoIngresoDeReclutamientoAsync(ctx, worker.PersonId))
                throw new AbrilException(
                    "Escenario no contemplado por el sistema: esta ficha corresponde a alguien que "
                    + "ya trabaja en Abril (" + (worker.WorkersEstado?.Nombre ?? "activo") + "), asi "
                    + "que no se le puede programar el EMO de Ingreso del proceso de reclutamiento. "
                    + "Reporta el caso al administrador antes de continuar.", 409);

            // La vinculacion vigente se consulta en dos casos: cuando el modal no mando empresa
            // (hay que resolverla), y cuando la ficha no tiene razon social propia — ahi hace falta
            // para saber si lo que llego del modal es un dato que el trabajador ya tenia o la razon
            // social que el usuario le acaba de ELEGIR (ver mas abajo).
            int? empresaVinculacion = null;
            if (dto.EmpresaId == null || worker.ContributorId == null)
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                empresaVinculacion = await ctx.WorkerVinculacion
                    .Where(v => v.WorkerId == dto.WorkerId && (v.FechaFin == null || v.FechaFin >= hoy))
                    .OrderByDescending(v => v.FechaInicio)
                    .Select(v => (int?)v.EmpresaId)
                    .FirstOrDefaultAsync();
            }

            var empresaId = dto.EmpresaId ?? empresaVinculacion;
            // Un finalista no tiene vinculacion todavia, asi que la empresa sale del contributor
            // que le copio el requerimiento. Sin esto la programacion quedaria sin empresa y no
            // aparecia en la pantalla de Programaciones, que filtra por empresa Abril.
            empresaId ??= worker.ContributorId;

            // Sin razon social no hay cita: la programacion quedaria fuera de la pantalla de
            // Programaciones (filtra por empresa Abril) y del correo a la clinica. Le pasa al
            // ingreso directo FFT, que va de la solicitud al EMO sin pasar por la asignacion de
            // Reclutamiento, y por eso el modal le ofrece un desplegable en lugar del campo de
            // solo lectura. Se corta en vez de crearla muda: es lo unico que falta y quien programa
            // lo puede resolver ahi mismo.
            if (empresaId == null)
                throw new AbrilException(
                    "Este trabajador no tiene razón social asignada: elígele una antes de "
                    + "programarle el EMO.", 400);

            // Ficha sin razon social y sin vinculacion de la que sacarla: lo que llego del modal no
            // es un dato que ya tuviera, es la razon social que el usuario le acaba de elegir. Y
            // elegirla es ASIGNARSELA, no usarla solo para esta cita: la ficha se queda con ella y
            // el resto del proceso (contrato, onboarding, correos de EMO) la encuentra.
            if (worker.ContributorId == null && empresaVinculacion == null && dto.EmpresaId != null)
            {
                // Se revalida contra la MISMA lista que se le ofrecio: lo que no esta ahi tampoco
                // se acepta, venga de donde venga el id.
                if (!await RazonSocialCuposHelper.EsValidaAsync(ctx, dto.EmpresaId.Value))
                    throw new AbrilException("La razón social seleccionada no es válida.", 400);

                worker.ContributorId = dto.EmpresaId;
                worker.UpdatedAt     = DateTimeOffset.UtcNow;

                // Y se le baja al requerimiento del que salio la ficha, si viene de uno: la pantalla
                // de Reclutamiento lee la razon social de ahi, y dejarla vacia haria que asignar
                // otra despues le pisara a la ficha la que se acaba de elegir aca.
                await _reclutamiento.SincronizarRazonSocialAsync(ctx, worker, dto.EmpresaId.Value, userId);
            }

            // El modal "Programar EMO con clinica" ya no pide elegir medico (lo pidio GTH), pero el
            // listado de Programaciones y la agenda siguen mostrando esa columna: sin medico
            // quedarian en "—". Se toma el que este marcado como por defecto en el catalogo, para no
            // dejar el id de un medico concreto escrito en el codigo. Si nadie esta marcado se queda
            // en null, que es lo que la columna admitia siempre.
            var medicoId = dto.MedicoId;
            if (medicoId == null)
                medicoId = await ctx.SsMedicoOcupacional
                    .Where(m => m.EsPorDefecto && m.Activo)
                    .Select(m => (int?)m.Id)
                    .FirstOrDefaultAsync();

            var ent = new SsProgramacionEmo
            {
                WorkerId = dto.WorkerId,
                EmpresaId = empresaId,
                TipoEmoId = dto.TipoEmoId,
                FechaProgramada = dto.FechaProgramada,
                HoraProgramada = dto.HoraProgramada,
                ClinicaId = dto.ClinicaId,
                MedicoId = medicoId,
                Motivo = dto.Motivo,
                Notas = dto.Notas,
                Origen = string.IsNullOrWhiteSpace(dto.Origen) ? "Manual" : dto.Origen,
                Estado = "Programado",
                RegistradoPorId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            ctx.SsProgramacionEmo.Add(ent);

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // El chequeo de progBloqueante de arriba no cierra la ventana de carrera entre
                // el SELECT y este INSERT (cron + manual casi simultáneos, doble click, etc.).
                // El índice único (ux_programacion_emo_worker_tipo_activa) es la garantía real.
                throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);
            }

            // Agendar la cita YA NO cierra el requerimiento de Reclutamiento: eso lo decide la
            // aptitud del examen (ver ReclutamientoEmoIngresoService, que corre al registrarse el
            // resultado del EMO). El proceso se queda en EMO_INGRESO hasta que la clínica lo
            // califique.
            await EnviarNotificacionCreacionAsync(ctx, ent, worker);

            return ent.Id;
        }

        /// <summary>
        /// true si esta persona es el finalista de un requerimiento de Reclutamiento que sigue
        /// esperando su EMO de Ingreso (fase EMO_INGRESO). Es lo que distingue "esta cita es el
        /// paso de Reclutamiento" de "esta cita es un EMO de Ingreso cualquiera".
        ///
        /// Se paga solo en el caso raro que la necesita (EMO de Ingreso de alguien que ya esta
        /// adentro), no en cada programacion.
        /// </summary>
        private static async Task<bool> EsEmoIngresoDeReclutamientoAsync(AppDbContext ctx, int? personId)
        {
            if (personId == null) return false;

            return await (
                from f in ctx.GthPostulanteFormulario
                where f.State && f.PersonId == personId
                join c in ctx.GthCandidato on f.GthCandidatoId equals c.GthCandidatoId
                where c.State
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                where r.State
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                where e.Codigo == "EMO_INGRESO"
                select r.GthRequerimientoId).AnyAsync();
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("ux_programacion_emo_worker_tipo_activa", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true;

        public async Task Update(int id, ProgramacionUpdateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsProgramacionEmo.FirstOrDefaultAsync(p => p.Id == id && p.State)
                ?? throw new AbrilException("Programación no encontrada.", 404);

            // Igual que en Create: si se está cambiando el tipo de EMO, no puede coincidir con
            // otra programación activa del mismo trabajador (bug real: esto no se validaba y
            // permitía crear duplicados editando el tipo de una fila existente).
            if (dto.TipoEmoId != ent.TipoEmoId)
                await ValidarSinDuplicadoActivoAsync(ctx, ent.WorkerId, dto.TipoEmoId, excluirId: ent.Id);

            ent.EmpresaId = dto.EmpresaId;
            ent.TipoEmoId = dto.TipoEmoId;
            ent.FechaProgramada = dto.FechaProgramada;
            ent.HoraProgramada = dto.HoraProgramada;
            ent.ClinicaId = dto.ClinicaId;
            ent.MedicoId = dto.MedicoId;
            ent.Motivo = dto.Motivo;
            ent.Notas = dto.Notas;
            ent.EmoResultadoId = dto.EmoResultadoId;
            ent.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);
            }
        }

        /// <summary>
        /// Misma regla que el índice único ux_programacion_emo_worker_tipo_activa, aplicada
        /// antes de un UPDATE (el índice solo protege INSERT/UPDATE del propio índice, pero acá
        /// conviene el mensaje 409 explícito en vez de esperar la excepción de Postgres).
        /// </summary>
        private static async Task ValidarSinDuplicadoActivoAsync(AppDbContext ctx, int workerId, int tipoEmoId, int excluirId)
        {
            var yaExiste = await ctx.SsProgramacionEmo.AnyAsync(p =>
                p.Id != excluirId &&
                p.State &&
                p.WorkerId == workerId &&
                p.TipoEmoId == tipoEmoId &&
                p.Estado != "Completado" &&
                p.Estado != "Cancelado" &&
                p.Estado != "Rechazado por Clínica" &&
                p.Estado != "No se presentó");

            if (yaExiste)
                throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);
        }

        // Estados que solo debe asignar el flujo de la clínica (Agenda → ClinicaAccion),
        // el cual siempre completa ClinicaId/EmpresaId (y CheckInHora para "En Atención").
        // Sin esta validación, UpdateEstado permitía dejar programaciones "En Atención"
        // sin clínica ni empresa: invisibles en la Agenda (que exige empresa) pero
        // visibles como huérfanas en Habilitación/Trabajadores (ver programación #798).
        private static readonly HashSet<string> EstadosRequierenClinica = new()
        {
            "Aceptado por Clínica", "En Atención"
        };

        private static readonly HashSet<string> EstadosTerminales = new()
        {
            "Completado", "Cancelado", "Rechazado por Clínica", "No se presentó"
        };

        public async Task UpdateEstado(int id, string estado, int? emoResultadoId, int? userId)
        {
            if (estado == "Completado")
                throw new AbrilException("El estado 'Completado' solo puede asignarse al registrar el resultado del EMO.", 400);

            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsProgramacionEmo.FirstOrDefaultAsync(p => p.Id == id && p.State)
                ?? throw new AbrilException("Programación no encontrada.", 404);

            if (EstadosRequierenClinica.Contains(estado) && (ent.ClinicaId is null || ent.EmpresaId is null))
                throw new AbrilException(
                    $"No se puede asignar el estado '{estado}' sin clínica y empresa asignadas. Use el flujo de la clínica (Agenda).",
                    400);

            // Solo hace falta chequear duplicados al REABRIR una fila (terminal → no terminal):
            // moverse entre estados no terminales de esta misma fila no puede crear un choque
            // nuevo, porque esta fila ya era la que ocupaba esa llave worker+tipo.
            if (EstadosTerminales.Contains(ent.Estado) && !EstadosTerminales.Contains(estado))
                await ValidarSinDuplicadoActivoAsync(ctx, ent.WorkerId, ent.TipoEmoId, excluirId: ent.Id);

            ent.Estado = estado;
            if (emoResultadoId.HasValue) ent.EmoResultadoId = emoResultadoId;
            ent.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);
            }
        }

        public async Task ClinicaAccion(int id, ProgramacionClinicaAccionDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsProgramacionEmo.FirstOrDefaultAsync(p => p.Id == id && p.State)
                ?? throw new AbrilException("Programación no encontrada.", 404);

            var worker = await ctx.Worker.Include(w => w.Person)
                .FirstOrDefaultAsync(w => w.Id == ent.WorkerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            switch (dto.Accion.Trim())
            {
                case "Aceptar":
                    // Corrección de Tipo de EMO antes de aceptar (o al reprogramar, que reusa
                    // esta misma acción): la clínica a veces programa el tipo equivocado y hoy
                    // no había forma de arreglarlo sin cancelar y crear de cero.
                    var tipoDestino = ent.TipoEmoId;
                    if (dto.TipoEmoId.HasValue && dto.TipoEmoId.Value > 0 && dto.TipoEmoId.Value != ent.TipoEmoId)
                    {
                        var tipoExiste = await ctx.SsEmoTipo.AnyAsync(t => t.Id == dto.TipoEmoId.Value);
                        if (!tipoExiste)
                            throw new AbrilException("El tipo de EMO seleccionado no existe.", 400);
                        tipoDestino = dto.TipoEmoId.Value;
                        ent.TipoEmoId = dto.TipoEmoId.Value;
                    }

                    // "Aceptar" también se reusa para reprogramar una fila que ya estaba cerrada
                    // (Rechazada/No se presentó/Cancelada): al reabrirla como no terminal, puede
                    // chocar con otra programación activa del mismo trabajador/tipo que se haya
                    // creado mientras tanto. Solo hace falta chequear si se está reabriendo — si
                    // ya era no terminal, esta misma fila ya ocupaba esa llave.
                    if (EstadosTerminales.Contains(ent.Estado))
                        await ValidarSinDuplicadoActivoAsync(ctx, ent.WorkerId, tipoDestino, excluirId: ent.Id);

                    ent.Estado = "Aceptado por Clínica";
                    ent.MotivoRechazo = null;
                    if (dto.HoraNueva.HasValue) ent.HoraProgramada = dto.HoraNueva.Value;
                    else if (dto.CheckInHora.HasValue) ent.HoraProgramada = dto.CheckInHora.Value;
                    if (dto.NuevaFecha.HasValue) ent.FechaProgramada = dto.NuevaFecha.Value;
                    ent.UpdatedAt = DateTimeOffset.UtcNow;

                    try
                    {
                        await ctx.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                    {
                        throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);
                    }

                    await EnviarNotificacionAceptacionAsync(ctx, ent, worker);
                    return;
                case "Rechazar":
                    ent.Estado = "Rechazado por Clínica";
                    ent.MotivoRechazo = dto.MotivoRechazo;
                    ent.UpdatedAt = DateTimeOffset.UtcNow;
                    await ctx.SaveChangesAsync();
                    if (dto.EnviarCorreo ?? true)
                        await EnviarNotificacionRechazoAsync(ctx, ent, worker, dto.MotivoRechazo);
                    return;
                case "CheckIn":
                    // Mismo caso que "Aceptar": si esta fila estaba cerrada, reabrirla en "En
                    // Atención" puede chocar con otra programación activa del trabajador/tipo.
                    if (EstadosTerminales.Contains(ent.Estado))
                        await ValidarSinDuplicadoActivoAsync(ctx, ent.WorkerId, ent.TipoEmoId, excluirId: ent.Id);

                    ent.Estado = "En Atención";
                    ent.CheckInHora = dto.CheckInHora ?? TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
                    break;
                case "Completar":
                    if (dto.EmoResultadoId.HasValue) ent.EmoResultadoId = dto.EmoResultadoId;
                    break;
                case "No Asistió":
                    ent.Estado = "No se presentó";
                    ent.UpdatedAt = DateTimeOffset.UtcNow;

                    var habCert = await ctx.SsHabTrabajador
                        .FirstOrDefaultAsync(h => h.WorkerId == ent.WorkerId && h.ItemId == 4);
                    if (habCert != null)
                    {
                        var hoyNoAsistio = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
                        var emoActivo = await ctx.WorkerEmo
                            .Where(e => e.WorkerId == ent.WorkerId && e.Activo)
                            .OrderByDescending(e => e.FechaVencimiento)
                            .FirstOrDefaultAsync();

                        if (emoActivo == null)
                        {
                            habCert.Estado = "Falta";
                        }
                        else if (emoActivo.FechaVencimiento < hoyNoAsistio)
                        {
                            habCert.Estado = "Vencido";
                        }
                        else
                        {
                            habCert.Estado = "Aprobado";
                            var venc = emoActivo.FechaVencimientoCalculada ?? emoActivo.FechaVencimiento;
                            if (venc.HasValue)
                                habCert.Vigencia = DateTime.SpecifyKind(venc.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                        }
                        habCert.UpdatedAt = DateTime.UtcNow;
                    }

                    await ctx.SaveChangesAsync();
                    return;
            }

            ent.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new AbrilException("Este trabajador ya tiene una programación activa para este tipo de EMO.", 409);
            }
        }

        public async Task UndoCheckInAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsProgramacionEmo.FirstOrDefaultAsync(p => p.Id == id && p.State)
                ?? throw new AbrilException("Programación no encontrada.", 404);

            if (ent.Estado != "En Atención")
                throw new AbrilException("Solo se puede deshacer el ingreso cuando el estado es 'En Atención'.", 409);

            ent.Estado     = "Aceptado por Clínica";
            ent.CheckInHora = null;
            ent.UpdatedAt  = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Vista previa del modal "Programar EMO con clínica": los DOS correos que dispara el
        /// flujo — el de ahora (programación manual) y el que sale después si la clínica acepta.
        /// Ambos salen del mismo resolver que el envío real, así que lo que el usuario ve antes
        /// de guardar es exactamente lo que se va a enviar en cada momento.
        ///
        /// Secuencial a propósito: cada Resolver abre su propio contexto y la convención del
        /// proyecto es no paralelizar accesos a la base de datos.
        /// </summary>
        public async Task<ProgramacionDestinatariosPreviewDto> GetDestinatarios(int workerId, int? clinicaId)
        {
            var manual = await _destinatarios.ResolverAsync(
                EmoCorreoEventoCodigo.ProgramacionManual, workerId, clinicaId);
            var aceptada = await _destinatarios.ResolverAsync(
                EmoCorreoEventoCodigo.Aceptada, workerId, clinicaId);

            return new ProgramacionDestinatariosPreviewDto { Manual = manual, Aceptada = aceptada };
        }

        /// <summary>
        /// Razones sociales del grupo con sus cupos, para el desplegable que el modal muestra
        /// cuando el trabajador llegó sin ninguna. La cuenta es la misma que ofrece Reclutamiento:
        /// vive en Shared para que las dos pantallas no puedan discrepar (ver
        /// <see cref="RazonSocialCuposHelper"/>).
        /// </summary>
        public async Task<List<RazonSocialCupoDto>> GetRazonesSociales()
        {
            using var ctx = _factory.CreateDbContext();
            return await RazonSocialCuposHelper.ListarAsync(ctx);
        }

        private async Task EnviarNotificacionCreacionAsync(
            AppDbContext ctx,
            SsProgramacionEmo prog,
            Worker worker)
        {
            var toRaw = new List<string>();
            try
            {
                // Destinatarios según la matriz de Configuración de EMOs → sección
                // "Programación manual", para el perfil del trabajador.
                var destinatarios = await _destinatarios.ResolverAsync(
                    EmoCorreoEventoCodigo.ProgramacionManual, worker.Id, prog.ClinicaId);

                var to = destinatarios.Para.Select(d => d.Email).ToList();
                var cc = destinatarios.Copias.Select(d => d.Email).ToList();
                toRaw = to;

                // Sin ningún destinatario principal activo no se envía nada (ni las
                // copias): es justamente la forma de silenciar el correo desde la
                // pantalla de Configuración de EMOs para hacer pruebas.
                if (to.Count == 0)
                {
                    _logger.LogWarning("Programación {Id}: sin destinatarios principales activos, no se envía notificación de creación.", prog.Id);
                    return;
                }

                SsClinica? clinica = prog.ClinicaId.HasValue
                    ? await ctx.SsClinica.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == prog.ClinicaId.Value)
                    : null;

                var tipoEmo = await ctx.SsEmoTipo.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == prog.TipoEmoId);

                var vinculacion = await ctx.WorkerVinculacion.AsNoTracking()
                    .Where(v => v.WorkerId == worker.Id && v.FechaFin == null)
                    .OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)
                    .FirstOrDefaultAsync();

                Project? proyecto = null;
                if (vinculacion?.ProyectoId.HasValue == true)
                    proyecto = await ctx.Project.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProjectId == vinculacion.ProyectoId.Value);

                var workerNombre = worker.Person?.FullName ?? worker.Id.ToString();
                var fechaStr = prog.FechaProgramada.ToString("dd/MM/yyyy");
                var horaStr = prog.HoraProgramada.HasValue ? prog.HoraProgramada.Value.ToString("HH:mm") : "—";
                var proyectoStr = proyecto?.ProjectDescription ?? "—";
                var tipoStr = tipoEmo?.Nombre ?? "—";
                var clinicaNombre = clinica?.Nombre ?? "—";

                var html = $@"<h2>Nueva programación EMO</h2>
<p>Se ha programado un Examen Médico Ocupacional para el siguiente trabajador:</p>
<table style='border-collapse:collapse;width:100%;max-width:500px'>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Trabajador</td><td style='padding:6px 12px'>{workerNombre}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Tipo EMO</td><td style='padding:6px 12px'>{tipoStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Fecha</td><td style='padding:6px 12px'>{fechaStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Hora</td><td style='padding:6px 12px'>{horaStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Proyecto</td><td style='padding:6px 12px'>{proyectoStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Clínica</td><td style='padding:6px 12px'>{clinicaNombre}</td></tr>
</table>
<p style='margin-top:16px;color:#6b7280;font-size:0.9em'>Por favor confirmar la programación en el sistema.</p>";

                await _emailService.SendAsync(
                    to: to,
                    subject: $"[EMO Programado] {workerNombre} — {fechaStr}",
                    body: html,
                    isHtml: true,
                    cc: cc.Count > 0 ? cc : null,
                    sender: SaludOcupacionalEmailConstants.SenderKey);

                prog.FechaNotificacion = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Programación {Id}: error enviando notificación de creación. Provider={Provider} To={To} Error={Error}",
                    prog.Id,
                    _configuration["Email:EmailProvider"],
                    string.Join(",", toRaw),
                    ex.Message);
            }
        }

        private async Task EnviarNotificacionAceptacionAsync(
            AppDbContext ctx,
            SsProgramacionEmo prog,
            Worker worker)
        {
            try
            {
                // Destinatarios según la matriz de Configuración de EMOs → sección
                // "Programación aceptada por la clínica", para el perfil del trabajador.
                // Que los contratistas no reciban este correo también sale de ahí (su
                // columna viene sin destinatarios activos), ya no de un corte en el código.
                var destinatarios = await _destinatarios.ResolverAsync(
                    EmoCorreoEventoCodigo.Aceptada, worker.Id, prog.ClinicaId);

                var to = destinatarios.Para.Select(d => d.Email).ToList();
                var cc = destinatarios.Copias.Select(d => d.Email).ToList();

                if (to.Count == 0) return;

                var vinculacion = await ctx.WorkerVinculacion.AsNoTracking()
                    .Where(v => v.WorkerId == worker.Id && v.FechaFin == null)
                    .OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)
                    .FirstOrDefaultAsync();

                Project? proyecto = null;
                if (vinculacion?.ProyectoId.HasValue == true)
                    proyecto = await ctx.Project.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProjectId == vinculacion.ProyectoId.Value);

                var tipoEmo = await ctx.SsEmoTipo.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == prog.TipoEmoId);

                var clinica = prog.ClinicaId.HasValue
                    ? await ctx.SsClinica.AsNoTracking().FirstOrDefaultAsync(c => c.Id == prog.ClinicaId.Value)
                    : null;

                var workerNombre = worker.Person?.FullName ?? worker.Id.ToString();
                var fechaStr = prog.FechaProgramada.ToString("dd/MM/yyyy");
                var horaStr = prog.HoraProgramada.HasValue ? prog.HoraProgramada.Value.ToString("HH:mm") : "—";
                var proyectoStr = proyecto?.ProjectDescription ?? "—";
                var tipoStr = tipoEmo?.Nombre ?? "—";
                var clinicaNombre = clinica?.Nombre ?? "—";
                var clinicaDireccion = clinica?.Direccion;

                // El logo, los íconos y la imagen de recomendaciones se sirven desde los estáticos
                // del frontend (public/images/), no desde el wwwroot del backend: en producción
                // intranet.abril.pe es nginx, que solo proxea /api/** al contenedor. Cualquier otra
                // ruta cae en el fallback SPA y devolvía index.html (200 text/html) en vez de la
                // imagen, por eso salían rotas.
                //
                // El origen es una clave aparte de App:FrontendUrl a propósito: Outlook no descarga
                // las imágenes desde el cliente sino a través del proxy de imágenes de Microsoft,
                // que nunca puede alcanzar un localhost. Con App:FrontendUrl (que en dev tiene que
                // seguir apuntando a localhost para los links clicables de los otros correos) las
                // imágenes salen siempre rotas al probar en local; App:EmailAssetsUrl permite
                // apuntarlas a un host público sin tocar esos links.
                var assetsUrl = _configuration["App:EmailAssetsUrl"]
                    ?? _configuration["App:FrontendUrl"]
                    ?? "https://intranet.abril.pe";

                var html = EmoConfirmacionEmailTemplate.Construir(
                    new EmoConfirmacionEmailTemplate.Datos(
                        Trabajador: workerNombre,
                        TipoEmo: tipoStr,
                        Fecha: fechaStr,
                        Hora: horaStr,
                        Proyecto: proyectoStr,
                        Clinica: clinicaNombre,
                        Direccion: clinicaDireccion),
                    assetsUrl);

                await _emailService.SendAsync(
                    to: to,
                    subject: $"[EMO Confirmado] {workerNombre} — {fechaStr}",
                    body: html,
                    isHtml: true,
                    cc: cc.Count > 0 ? cc : null,
                    sender: SaludOcupacionalEmailConstants.SenderKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar notificación de aceptación de programación.");
            }
        }

        private async Task EnviarNotificacionRechazoAsync(
            AppDbContext ctx,
            SsProgramacionEmo prog,
            Worker worker,
            string? motivo)
        {
            try
            {
                // Destinatarios según la matriz de Configuración de EMOs → sección
                // "Programación rechazada por la clínica", para el perfil del trabajador.
                var destinatarios = await _destinatarios.ResolverAsync(
                    EmoCorreoEventoCodigo.Rechazada, worker.Id, prog.ClinicaId);

                var to = destinatarios.Para.Select(d => d.Email).ToList();
                var cc = destinatarios.Copias.Select(d => d.Email).ToList();

                if (to.Count == 0) return;

                var vinculacion = await ctx.WorkerVinculacion.AsNoTracking()
                    .Where(v => v.WorkerId == worker.Id && v.FechaFin == null)
                    .OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)
                    .FirstOrDefaultAsync();

                Project? proyecto = null;
                if (vinculacion?.ProyectoId.HasValue == true)
                    proyecto = await ctx.Project.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProjectId == vinculacion.ProyectoId.Value);

                var tipoEmo = await ctx.SsEmoTipo.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == prog.TipoEmoId);

                var clinica = prog.ClinicaId.HasValue
                    ? await ctx.SsClinica.AsNoTracking().FirstOrDefaultAsync(c => c.Id == prog.ClinicaId.Value)
                    : null;

                var workerNombre = worker.Person?.FullName ?? worker.Id.ToString();
                var fechaStr = prog.FechaProgramada.ToString("dd/MM/yyyy");
                var horaStr = prog.HoraProgramada.HasValue ? prog.HoraProgramada.Value.ToString("HH:mm") : "—";
                var proyectoStr = proyecto?.ProjectDescription ?? "—";
                var tipoStr = tipoEmo?.Nombre ?? "—";
                var clinicaNombre = clinica?.Nombre ?? "—";
                var motivoStr = !string.IsNullOrWhiteSpace(motivo) ? motivo : "—";

                var html = $@"<h2>EMO Rechazado por Clínica</h2>
<p>La clínica ha rechazado la programación del Examen Médico Ocupacional:</p>
<table style='border-collapse:collapse;width:100%;max-width:500px'>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Trabajador</td><td style='padding:6px 12px'>{workerNombre}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Tipo EMO</td><td style='padding:6px 12px'>{tipoStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Fecha</td><td style='padding:6px 12px'>{fechaStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Hora</td><td style='padding:6px 12px'>{horaStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Proyecto</td><td style='padding:6px 12px'>{proyectoStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Clínica</td><td style='padding:6px 12px'>{clinicaNombre}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#fef2f2;color:#b91c1c'>Motivo de rechazo</td><td style='padding:6px 12px;color:#b91c1c'>{motivoStr}</td></tr>
</table>
<p style='margin-top:16px;color:#6b7280;font-size:0.9em'>Por favor coordinar una nueva fecha de programación con la clínica.</p>";

                await _emailService.SendAsync(
                    to: to,
                    subject: $"[EMO Rechazado] {workerNombre} — {fechaStr}",
                    body: html,
                    isHtml: true,
                    cc: cc.Count > 0 ? cc : null,
                    sender: SaludOcupacionalEmailConstants.SenderKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar notificación de rechazo de programación.");
            }
        }

        public async Task<List<ProgramacionHabilitacionDto>> GetHabilitacionAsync(ProgramacionHabilitacionFiltrosDto f)
        {
            using var ctx = _factory.CreateDbContext();
            var estados = new[] { "Programado", "Aceptado por Clínica", "En Atención", "En Interconsulta", "Aceptado" };

            var q = ctx.SsProgramacionEmo
                .Where(p => p.State && estados.Contains(p.Estado))
                .Include(p => p.Worker)
                    .ThenInclude(w => w!.Person)
                .AsQueryable();

            if (!string.IsNullOrEmpty(f.Estado))
                q = q.Where(p => p.Estado == f.Estado);

            if (!string.IsNullOrEmpty(f.Fecha))
                q = q.Where(p => p.FechaProgramada.ToString() == f.Fecha);

            if (f.SoloNoNotificados == true)
                q = q.Where(p => !p.Notificado);

            var list = await q
                .OrderBy(p => p.FechaProgramada)
                .ThenBy(p => p.HoraProgramada)
                .ToListAsync();

            var workerIds = list.Select(p => p.WorkerId).Distinct().ToList();

            var vinculaciones = await ctx.WorkerVinculacion
                .Where(v => workerIds.Contains(v.WorkerId) && v.FechaFin == null)
                .Include(v => v.Empresa)
                .Include(v => v.Proyecto)
                .ToListAsync();

            var vinMap = vinculaciones
                .GroupBy(v => v.WorkerId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.CreatedAt).First());

            var result = list
                .Where(p => !f.ProyectoId.HasValue || (vinMap.TryGetValue(p.WorkerId, out var vCheck) && vCheck.ProyectoId == f.ProyectoId.Value))
                .Select(p =>
                {
                    vinMap.TryGetValue(p.WorkerId, out var vin);
                    var person = p.Worker?.Person;

                    return new ProgramacionHabilitacionDto
                    {
                        Id            = p.Id,
                        Trabajador    = person?.FullName ?? "",
                        Dni           = person?.DocumentIdentityCode ?? "",
                        Proyecto      = vin?.Proyecto?.ProjectDescription ?? "",
                        RazonSocial   = vin?.Empresa?.ContributorName ?? "",
                        Estado        = p.Estado,
                        FechaProgramada = p.FechaProgramada.ToString("yyyy-MM-dd"),
                        Hora          = p.HoraProgramada?.ToString(@"hh\:mm"),
                        Notificado    = p.Notificado,
                    };
                })
                .ToList();

            if (result.Count > 0)
                _logger.LogInformation("[GetHabilitacion] primer item — Id={Id} Trabajador={Trabajador} RazonSocial={RazonSocial} Proyecto={Proyecto}",
                    result[0].Id, result[0].Trabajador, result[0].RazonSocial, result[0].Proyecto);

            return result;
        }

        public async Task PatchNotificadoAsync(int id, bool notificado)
        {
            using var ctx = _factory.CreateDbContext();
            var prog = await ctx.SsProgramacionEmo.FirstOrDefaultAsync(p => p.Id == id && p.State)
                ?? throw new AbrilException("Programación no encontrada.", 404);
            prog.Notificado = notificado;
            prog.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Correo de inasistencias del día (botón de la Agenda de Clínica): a los trabajadores
        /// con Estado = "No se presentó" en la fecha dada. Mismo criterio de agrupación que
        /// InterconsultaService.EnviarRecordatorios — Staff/Oficina Central con correo propio
        /// reciben individual, Obra se agrupa por proyecto en un solo correo al administrador —
        /// porque es exactamente el mismo problema (avisar a los responsables de gente sin
        /// correo propio) con otra fuente de datos.
        /// </summary>
        public async Task<ProgramacionInasistenciaEnviarCorreoResultDto> EnviarInasistencias(DateOnly fecha)
        {
            using var ctx = _factory.CreateDbContext();
            var result = new ProgramacionInasistenciaEnviarCorreoResultDto();

            var raw = await (
                from p in ctx.SsProgramacionEmo
                join w in ctx.Worker on p.WorkerId equals w.Id
                where p.State && p.Estado == "No se presentó" && p.FechaProgramada == fecha
                select new
                {
                    p.Id,
                    p.WorkerId,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    WorkerDni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                    w.ObraOficinaStaffId,
                    w.ContrataCasa,
                    w.Categoria,
                    w.Ocupacion,
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

            result.TotalSeleccionadas = raw.Count;
            if (raw.Count == 0) return result;

            var proyectoIds = raw.Select(x => x.ProyAsignada?.ProyectoId ?? x.VincActiva?.ProyectoId)
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var empresaIds = raw.Select(x => x.ProyAsignada?.EmpresaId ?? x.VincActiva?.EmpresaId)
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

            var proyectoMap = await ctx.Project
                .Where(pr => proyectoIds.Contains(pr.ProjectId))
                .ToDictionaryAsync(pr => pr.ProjectId, pr => pr);
            var empresaMap = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .ToDictionaryAsync(c => c.ContributorId, c => c);

            // CC fija: solo el buzón de médico ocupacional (remitente). Antes también copiaba a
            // TODOS los usuarios con el rol "Administrador de Administración" sin filtrar por
            // proyecto/empresa — llegaba a gente sin ninguna relación con el trabajador (ej.
            // Dayoris en un correo de BOSQUE REAL). El destinatario correcto (administrador del
            // proyecto/empresa específico) ya va en el "to"; no hace falta copiar a nadie más.
            var ccBase = new List<string> { SaludOcupacionalEmailConstants.Remitente };

            var conCorreoPropio = raw
                .Where(x => !string.IsNullOrWhiteSpace(x.WorkerEmail)
                         && x.ContrataCasa == "Casa"
                         && (x.ObraOficinaStaffId == ObraOficinaStaffIds.Staff
                          || x.ObraOficinaStaffId == ObraOficinaStaffIds.OficinaCentral))
                .ToList();
            var sinCorreoPropio = raw.Except(conCorreoPropio).ToList();

            foreach (var item in conCorreoPropio)
            {
                var proyectoId = item.ProyAsignada?.ProyectoId ?? item.VincActiva?.ProyectoId;
                var empresaId = item.ProyAsignada?.EmpresaId ?? item.VincActiva?.EmpresaId;
                Project? proyecto = proyectoId.HasValue && proyectoMap.TryGetValue(proyectoId.Value, out var p) ? p : null;
                Contributor? empresa = empresaId.HasValue && empresaMap.TryGetValue(empresaId.Value, out var e) ? e : null;

                var destinatariosRaw = new List<string?>
                {
                    item.WorkerEmail, proyecto?.EmailCoordAdmin, empresa?.EmailAdministrador
                };
                var destinatarios = destinatariosRaw
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (destinatarios.Count == 0)
                {
                    result.TotalErrores++;
                    result.Detalles.Add($"{item.WorkerNombre} — sin destinatarios. Omitido.");
                    continue;
                }

                var infoIndividual = new InasistenciaInfoDto(
                    item.WorkerNombre, item.WorkerDni,
                    TipoDisplay(item.ContrataCasa, item.ObraOficinaStaffId),
                    empresa?.ContributorName, proyecto?.ProjectDescription,
                    proyecto?.EmailCoordAdmin ?? empresa?.EmailAdministrador,
                    item.Categoria, item.Ocupacion);

                try
                {
                    await _emailService.SendAsync(
                        to: destinatarios,
                        subject: $"Inasistencia a EMO programado - {item.WorkerNombre}",
                        body: BuildBodyInasistenciaIndividual(infoIndividual, fecha),
                        isHtml: true,
                        cc: ccBase,
                        sender: SaludOcupacionalEmailConstants.SenderKey);
                    result.TotalEnviados++;
                    result.Detalles.Add($"{item.WorkerNombre} — enviado a {destinatarios.Count} destinatario(s).");
                }
                catch (Exception ex)
                {
                    result.TotalErrores++;
                    result.Detalles.Add($"{item.WorkerNombre} — error al enviar: {ex.Message}");
                }
            }

            var grupos = sinCorreoPropio.GroupBy(x => new
            {
                ProyectoId = x.ProyAsignada?.ProyectoId ?? x.VincActiva?.ProyectoId,
                Admin = (x.ProyAsignada?.ProyectoId ?? x.VincActiva?.ProyectoId) is int pid && proyectoMap.TryGetValue(pid, out var pr) ? pr.EmailCoordAdmin : null
            });

            foreach (var grupo in grupos)
            {
                var proyectoNombre = grupo.Key.ProyectoId.HasValue && proyectoMap.TryGetValue(grupo.Key.ProyectoId.Value, out var pr)
                    ? pr.ProjectDescription : "Sin proyecto asignado";
                var admin = grupo.Key.Admin;

                if (string.IsNullOrWhiteSpace(admin))
                {
                    result.TotalErrores += grupo.Count();
                    foreach (var it in grupo)
                        result.Detalles.Add($"{it.WorkerNombre} — sin administrador encargado en '{proyectoNombre}'. Omitido.");
                    continue;
                }

                var infosGrupo = grupo.Select(x =>
                {
                    var empresaIdItem = x.ProyAsignada?.EmpresaId ?? x.VincActiva?.EmpresaId;
                    var empresaItem = empresaIdItem.HasValue && empresaMap.TryGetValue(empresaIdItem.Value, out var e2) ? e2 : null;
                    return new InasistenciaInfoDto(
                        x.WorkerNombre, x.WorkerDni,
                        TipoDisplay(x.ContrataCasa, x.ObraOficinaStaffId),
                        empresaItem?.ContributorName, proyectoNombre, admin,
                        x.Categoria, x.Ocupacion);
                }).ToList();

                try
                {
                    await _emailService.SendAsync(
                        to: new List<string> { admin.Trim() },
                        subject: $"Inasistencias a EMO programado - {proyectoNombre}",
                        body: BuildBodyInasistenciaConsolidado(proyectoNombre, infosGrupo, fecha),
                        isHtml: true,
                        cc: ccBase,
                        sender: SaludOcupacionalEmailConstants.SenderKey);
                    result.TotalEnviados += grupo.Count();
                    result.Detalles.Add($"{proyectoNombre} — enviado a {admin} ({grupo.Count()} trabajador(es)).");
                }
                catch (Exception ex)
                {
                    result.TotalErrores += grupo.Count();
                    result.Detalles.Add($"{proyectoNombre} — error al enviar a {admin}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>Info del trabajador para el cuerpo del correo — de dónde es, para que el
        /// destinatario ubique de inmediato a quién le están avisando sin tener que buscarlo.</summary>
        private record InasistenciaInfoDto(
            string? Nombre, string? Dni, string Tipo, string? RazonSocial, string? Proyecto,
            string? Administrador, string? Categoria, string? Ocupacion);

        /// <summary>Obrero / Staff / Oficina Central — mismo criterio que ProgramacionListDto.TipoTrabajador.</summary>
        private static string TipoDisplay(string? contrataCasa, int? obraOficinaStaffId)
        {
            if (contrataCasa != "Casa") return "Contratista";
            return obraOficinaStaffId switch
            {
                ObraOficinaStaffIds.OficinaCentral => "Oficina Central",
                ObraOficinaStaffIds.Staff => "Staff Obra",
                _ => "Obrero",
            };
        }

        private static string BuildBodyInasistenciaIndividual(InasistenciaInfoDto info, DateOnly fecha)
        {
            return $@"
            <p>Estimados,</p>
            <p>
                Se informa que el trabajador <strong>{info.Nombre}</strong> (DNI {info.Dni})
                tenía un <strong>EMO programado el {fecha:dd/MM/yyyy}</strong> y no se presentó.
            </p>
            <table style='border-collapse: collapse; font-family: Arial, sans-serif; font-size: 14px;'>
                <tr><td style='padding: 4px 8px; color:#666;'>Tipo</td><td style='padding: 4px 8px;'><strong>{info.Tipo}</strong></td></tr>
                <tr><td style='padding: 4px 8px; color:#666;'>Razón social</td><td style='padding: 4px 8px;'>{info.RazonSocial ?? "—"}</td></tr>
                <tr><td style='padding: 4px 8px; color:#666;'>Proyecto actual</td><td style='padding: 4px 8px;'>{info.Proyecto ?? "—"}</td></tr>
                <tr><td style='padding: 4px 8px; color:#666;'>Categoría</td><td style='padding: 4px 8px;'>{info.Categoria ?? "—"}</td></tr>
                <tr><td style='padding: 4px 8px; color:#666;'>Ocupación</td><td style='padding: 4px 8px;'>{info.Ocupacion ?? "—"}</td></tr>
                <tr><td style='padding: 4px 8px; color:#666;'>Administrador responsable</td><td style='padding: 4px 8px;'>{info.Administrador ?? "—"}</td></tr>
            </table>
            <p>Se agradece coordinar su reprogramación a la brevedad.</p>
            <p style='font-size: 12px; color: #666; margin-top: 24px;'>
                Módulo de Programaciones — Salud Ocupacional.
            </p>
            ";
        }

        private static string BuildBodyInasistenciaConsolidado(string proyectoNombre, List<InasistenciaInfoDto> items, DateOnly fecha)
        {
            var filas = string.Join("", items.Select(it => $@"
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{it.Nombre}</td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{it.Dni}</td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{it.Tipo}</td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{it.RazonSocial ?? "—"}</td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{it.Categoria ?? "—"}</td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{it.Ocupacion ?? "—"}</td>
                </tr>"));

            return $@"
            <p>Estimados,</p>
            <p>
                Se informa que los siguientes trabajadores de <strong>{proyectoNombre}</strong>
                tenían un <strong>EMO programado el {fecha:dd/MM/yyyy}</strong> y no se presentaron.
                Se agradece coordinar su reprogramación a la brevedad:
            </p>
            <table style='border-collapse: collapse; font-family: Arial, sans-serif; font-size: 14px; width: 100%;'>
                <thead>
                    <tr>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>Trabajador</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>DNI</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>Tipo</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>Razón social</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>Categoría</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>Ocupación</th>
                    </tr>
                </thead>
                <tbody>{filas}</tbody>
            </table>
            <p style='font-size: 12px; color: #666; margin-top: 24px;'>
                Módulo de Programaciones — Salud Ocupacional.
            </p>
            ";
        }

        /// <summary>
        /// Cierra automáticamente las programaciones vencidas que nadie cerró a mano: si a
        /// alguien no se le marcó "No asistió" en Agenda, la fila quedaba viva para siempre en
        /// Programado/Confirmado/Aceptado por Clínica/Reprogramado, bloqueando su reprogramación
        /// (el Create ya valida "una activa por trabajador/tipo") y —peor— haciendo que el
        /// auto-programador la re-generara al día siguiente porque su propio chequeo de
        /// duplicados solo miraba filas con fecha futura. Este cierre corre a las 13:00 (hora
        /// Lima) vía cron externo y aplica sobre:
        ///   • cualquier fila de un día anterior a hoy en esos estados (backlog acumulado), y
        ///   • las de HOY cuya hora ya pasó las 13:00 (o sin hora registrada).
        /// No toca "En Atención" ni "En Interconsulta": ahí el trabajador sí se presentó.
        /// </summary>
        public async Task<int> CerrarInasistenciasVencidasAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var ahoraLima = DateTime.UtcNow.AddHours(-5);
            var hoy = DateOnly.FromDateTime(ahoraLima);
            var horaCorte = new TimeOnly(13, 0);

            var estadosPreAtencion = new[] { "Programado", "Confirmado", "Aceptado por Clínica", "Reprogramado" };

            var candidatas = await ctx.SsProgramacionEmo
                .Where(p => p.State && estadosPreAtencion.Contains(p.Estado))
                .Where(p =>
                    p.FechaProgramada < hoy ||
                    (p.FechaProgramada == hoy && (p.HoraProgramada == null || p.HoraProgramada < horaCorte) && TimeOnly.FromDateTime(ahoraLima) >= horaCorte))
                .ToListAsync();

            foreach (var p in candidatas)
            {
                p.Estado = "No se presentó";
                p.Motivo = string.IsNullOrWhiteSpace(p.Motivo)
                    ? "Cierre automático: no se registró asistencia hasta la hora de corte (13:00)."
                    : $"{p.Motivo} — Cierre automático: no se registró asistencia hasta la hora de corte (13:00).";
                p.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (candidatas.Count > 0)
                await ctx.SaveChangesAsync();

            return candidatas.Count;
        }
    }

}
