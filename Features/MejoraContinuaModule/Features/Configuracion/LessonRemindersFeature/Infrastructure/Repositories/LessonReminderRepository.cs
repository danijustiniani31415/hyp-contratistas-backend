using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Models;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Repositories
{
    public class LessonReminderRepository : ILessonReminderRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILessonJefeResolver _jefeResolver;

        // Categorías consideradas "jefatura" para la sección de recordatorios y el
        // aviso del 4.º día. OJO: es independiente de LessonJefeResolver, que solo usa
        // Jefe para decidir quién PUEDE aprobar/rechazar lecciones (jerarquía de
        // revisión). Aquí solo definimos quién aparece/puede activarse en la sección
        // Jefaturas.
        private static readonly int[] CategoriasJefatura = CategoriaIds.Jefaturas;

        public LessonReminderRepository(
            IDbContextFactory<AppDbContext> factory,
            ILessonJefeResolver jefeResolver)
        {
            _factory = factory;
            _jefeResolver = jefeResolver;
        }

        public async Task<HashSet<DateOnly>> GetHolidayDatesAsync(int year, int month)
        {
            using var ctx = _factory.CreateDbContext();

            // Lista pequeña: traemos los registros vivos/activos y resolvemos en memoria.
            var holidays = await ctx.Holiday
                .Where(h => h.State && h.Active)
                .Select(h => new { h.HolidayDate, h.RecurringYearly })
                .ToListAsync();

            var result = new HashSet<DateOnly>();
            foreach (var h in holidays)
            {
                if (h.RecurringYearly)
                {
                    // Se repite cada año: resolvemos al año solicitado por mes/día.
                    // Guardamos contra fechas inválidas (ej. 29-feb en año no bisiesto).
                    if (h.HolidayDate.Month != month) continue;
                    var day = Math.Min(h.HolidayDate.Day, DateTime.DaysInMonth(year, month));
                    result.Add(new DateOnly(year, month, day));
                }
                else if (h.HolidayDate.Year == year && h.HolidayDate.Month == month)
                {
                    result.Add(h.HolidayDate);
                }
            }
            return result;
        }

        public async Task<object> GetPaged(int page, int pageSize, string? subarea = null, int? workerId = null, bool includeWorkers = false, int? obraOficinaStaffId = null)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            using var ctx = _factory.CreateDbContext();

            // Asignaciones por TRABAJADOR (user_project.worker_id). Nombre/área desde
            // person/worker; correo desde worker.email_corporativo. NO requiere usuario.
            var baseQuery =
                from up in ctx.UserProject
                join w in ctx.Worker on up.WorkerId equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId
                join pj in ctx.Project on up.ProjectId equals pj.ProjectId
                where up.State == true
                select new { up, w, p, pj };

            // Filtro opcional por subárea del trabajador.
            if (!string.IsNullOrWhiteSpace(subarea))
            {
                var sa = subarea.Trim();
                baseQuery = baseQuery.Where(x => x.w.Subarea == sa);
            }

            // Filtro opcional por Obra / Staff / Oficina Central del trabajador
            // (workers.obra_oficina_staff_id; antes esto se deducia del ultimo nodo del
            // arbol de areas, de tipo Obra_Oficina).
            if (obraOficinaStaffId.HasValue && obraOficinaStaffId.Value > 0)
            {
                baseQuery = baseQuery.Where(x => x.w.ObraOficinaStaffId == obraOficinaStaffId.Value);
            }

            // Filtro opcional por trabajador.
            if (workerId.HasValue && workerId.Value > 0)
            {
                baseQuery = baseQuery.Where(x => x.w.Id == workerId.Value);
            }

            var query = baseQuery
                .OrderByDescending(x => x.up.UserProjectId)
                .Select(x => new LessonReminderDTO
                {
                    UserProjectId = x.up.UserProjectId,
                    WorkerId = x.w.Id,
                    WorkerFullName = x.p.FullName,
                    Email = x.w.EmailCorporativo,
                    ProjectId = x.up.ProjectId,
                    ProjectDescription = x.pj.ProjectDescription ?? string.Empty,
                    CreatedDateTime = x.up.CreatedDateTime,
                    CreatedUserId = x.up.CreatedUserId,
                    UpdatedDateTime = x.up.UpdatedDateTime,
                    UpdatedUserId = x.up.UpdatedUserId,
                    Active = x.up.Active
                });

            var totalRecords = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Opciones del filtro por trabajador: trabajadores distintos con algún
            // recordatorio vivo (independiente de la subárea seleccionada, para que el
            // desplegable sea estable). Solo se devuelve en la carga inicial.
            // Opciones del filtro Obra / Oficina (solo en la carga inicial).
            List<LessonReminderObraOficinaDTO>? obraOficinaStaff = null;
            if (includeWorkers)
            {
                obraOficinaStaff = await ctx.WorkersObraOficinaStaff
                    .Where(c => c.State && c.Active)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new LessonReminderObraOficinaDTO
                    {
                        ObraOficinaStaffId = c.WorkersObraOficinaStaffId,
                        Name = c.Name
                    })
                    .ToListAsync();
            }

            List<LessonReminderWorkerDTO>? workers = null;
            if (includeWorkers)
            {
                workers = await (
                    from up in ctx.UserProject
                    join w in ctx.Worker on up.WorkerId equals w.Id
                    join p in ctx.Person on w.PersonId equals p.PersonId
                    where up.State == true
                    orderby p.FullName
                    select new LessonReminderWorkerDTO
                    {
                        WorkerId = w.Id,
                        FullName = p.FullName,
                        Email = w.EmailCorporativo
                    }
                ).Distinct().ToListAsync();
            }

            return new
            {
                page,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                data,
                workers,
                obraOficinaStaff
            };
        }

        public async Task<LessonReminderCreateDataDTO> GetCreateData()
        {
            using var ctx = _factory.CreateDbContext();

            // IMPORTANTE: ejecutar SECUENCIALMENTE sobre una sola instancia de DbContext.
            // DbContext no es thread-safe; lanzar ambas queries con Task.WhenAll sobre el
            // mismo ctx provoca "A second operation was started on this context instance"
            // (Npgsql/PostgreSQL no soporta operaciones concurrentes en la misma conexión).
            // Trabajadores (worker + person) con correo corporativo @abril. NO se exige
            // usuario en app_user: pueden existir trabajadores nunca registrados en la app.
            var workers = await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                where w.EmailCorporativo != null
                      && w.EmailCorporativo.ToLower().Contains("@abril")
                      && p.State == true
                orderby p.FullName
                select new LessonReminderWorkerDTO
                {
                    WorkerId = w.Id,
                    FullName = p.FullName,
                    Email = w.EmailCorporativo
                }
            ).ToListAsync();

            var projects = await ctx.Project
                .Where(p => p.State && p.Active)
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new LessonReminderProjectDTO
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription ?? string.Empty
                })
                .ToListAsync();

            return new LessonReminderCreateDataDTO
            {
                Workers = workers,
                Projects = projects
            };
        }

        public async Task Create(LessonReminderCreateDTO dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            // Validar trabajador (worker + person) y resolver su usuario si lo tiene
            // (para compatibilidad: si existe app_user, se guarda user_id también).
            var workerInfo = await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                where w.Id == dto.WorkerId
                select new { w.Id, UserId = p.UserId }
            ).FirstOrDefaultAsync();
            if (workerInfo == null)
                throw new AbrilException("El trabajador no existe.", 404);

            var existing = await ctx.UserProject
                .FirstOrDefaultAsync(up => up.WorkerId == dto.WorkerId && up.ProjectId == dto.ProjectId);

            if (existing != null && existing.State && existing.Active)
                throw new AbrilException("El trabajador ya está asignado a este proyecto");

            if (existing != null && existing.State && !existing.Active)
                throw new AbrilException("El trabajador ya está asignado a este proyecto, pero se encuentra inactivo. Reactívelo para continuar.");

            if (existing != null && !existing.State)
            {
                existing.State = true;
                existing.Active = dto.Active;
                existing.UserId = workerInfo.UserId;
                existing.UpdatedDateTime = DateTime.UtcNow;
                existing.UpdatedUserId = userId;
                await ctx.SaveChangesAsync();
                return;
            }

            var userProject = new UserProject
            {
                WorkerId = dto.WorkerId,
                UserId = workerInfo.UserId, // null si el trabajador no tiene usuario
                ProjectId = dto.ProjectId,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId,
                Active = dto.Active,
                State = true
            };

            ctx.UserProject.Add(userProject);
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateProjectAsync(int userProjectId, int newProjectId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var userProject = await ctx.UserProject
                .FirstOrDefaultAsync(u => u.UserProjectId == userProjectId && u.State == true);
            if (userProject == null)
                throw new AbrilException("El recordatorio no existe.", 404);

            if (newProjectId <= 0)
                throw new AbrilException("Debe seleccionar un proyecto.", 400);

            if (userProject.ProjectId == newProjectId)
                throw new AbrilException("El recordatorio ya está asignado a ese proyecto.", 400);

            var projectExists = await ctx.Project
                .AnyAsync(p => p.ProjectId == newProjectId && p.State && p.Active);
            if (!projectExists)
                throw new AbrilException("El proyecto seleccionado no existe o está inactivo.", 404);

            // No permitir duplicar: el mismo trabajador ya tiene un recordatorio vivo
            // en el proyecto destino (otra fila).
            var duplicate = await ctx.UserProject.AnyAsync(u =>
                u.UserProjectId != userProjectId &&
                u.WorkerId == userProject.WorkerId &&
                u.ProjectId == newProjectId &&
                u.State == true);
            if (duplicate)
                throw new AbrilException("El trabajador ya tiene un recordatorio en el proyecto seleccionado.", 400);

            userProject.ProjectId = newProjectId;
            userProject.UpdatedDateTime = DateTime.UtcNow;
            userProject.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task<bool> DeleteSoftAsync(int userProjectId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var userProject = await ctx.UserProject
                .FirstOrDefaultAsync(u => u.UserProjectId == userProjectId && u.State == true);

            if (userProject == null)
                throw new AbrilException("El usuario no tiene asignado el proyecto especificado");

            userProject.State = false;
            userProject.Active = false;
            userProject.UpdatedDateTime = DateTime.UtcNow;
            userProject.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<ToggleLessonReminderResultDTO> ToggleActiveAsync(int userProjectId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var userProject = await ctx.UserProject
                .FirstOrDefaultAsync(u => u.UserProjectId == userProjectId && u.State == true);

            if (userProject == null)
                throw new AbrilException("El recordatorio no existe.", 404);

            // Invierte solo Active (State permanece true). El cron filtra por Active == true,
            // así que un recordatorio inactivo deja de enviarse sin perder el registro.
            userProject.Active = !userProject.Active;
            userProject.UpdatedDateTime = DateTime.UtcNow;
            userProject.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();

            return new ToggleLessonReminderResultDTO
            {
                UserProjectId = userProject.UserProjectId,
                Active = userProject.Active
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Filtro project_staff_reminder (toggle por proyecto con staff_email)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<ProjectStaffReminderConfigItemDTO>> GetAllProjectStaffAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Proyectos vivos que tienen staff_email registrado
            var projects = await ctx.Project
                .Where(p => p.State && p.Active && p.StaffEmail != null && p.StaffEmail != "")
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new
                {
                    p.ProjectId,
                    p.ProjectDescription,
                    p.StaffEmail
                })
                .ToListAsync();

            if (projects.Count == 0)
                return new List<ProjectStaffReminderConfigItemDTO>();

            var rows = await ctx.ProjectStaffReminder.ToListAsync();
            var byProjectId = rows.ToDictionary(r => r.ProjectId);

            return projects.Select(p =>
            {
                byProjectId.TryGetValue(p.ProjectId, out var row);
                return new ProjectStaffReminderConfigItemDTO
                {
                    ProjectStaffReminderId = row?.ProjectStaffReminderId,
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    StaffEmail = p.StaffEmail,
                    Active = row != null && row.Active
                };
            }).ToList();
        }

        public async Task<ToggleProjectStaffReminderResultDTO> ToggleProjectStaffAsync(int projectId)
        {
            using var ctx = _factory.CreateDbContext();

            // Projection para evitar SELECT * (la tabla project puede tener columnas
            // declaradas en el modelo pero aún no presentes en algunas BDs, ej. responsable_udp).
            var projectInfo = await ctx.Project
                .Where(p => p.ProjectId == projectId && p.State && p.Active)
                .Select(p => new { p.ProjectId, p.StaffEmail })
                .FirstOrDefaultAsync();

            if (projectInfo == null)
                throw new AbrilException("El proyecto no existe o está inactivo.", 404);
            if (string.IsNullOrWhiteSpace(projectInfo.StaffEmail))
                throw new AbrilException("El proyecto no tiene un correo de staff configurado.", 400);

            var row = await ctx.ProjectStaffReminder.FirstOrDefaultAsync(r => r.ProjectId == projectId);
            if (row == null)
            {
                row = new ProjectStaffReminder
                {
                    ProjectId = projectId,
                    Active = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                ctx.ProjectStaffReminder.Add(row);
            }
            else
            {
                row.Active = !row.Active;
            }

            await ctx.SaveChangesAsync();
            return new ToggleProjectStaffReminderResultDTO
            {
                ProjectStaffReminderId = row.ProjectStaffReminderId,
                Active = row.Active
            };
        }

        public async Task<List<ActiveProjectStaffEmailDTO>> GetActiveStaffEmailsAsync()
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from r in ctx.ProjectStaffReminder
                join p in ctx.Project on r.ProjectId equals p.ProjectId
                where r.Active
                      && p.State
                      && p.Active
                      && p.StaffEmail != null
                      && p.StaffEmail != ""
                orderby p.ProjectDescription
                select new ActiveProjectStaffEmailDTO
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription ?? string.Empty,
                    StaffEmail = p.StaffEmail!
                }
            ).ToListAsync();
        }

        public async Task<List<PendingStaffMemberDTO>> GetPendingMembersForProjectAsync(
            int projectId,
            string period,
            IReadOnlyList<string> emails)
        {
            if (emails == null || emails.Count == 0)
                return new List<PendingStaffMemberDTO>();

            // Normalizar: trim + lower, dedup
            var normalized = emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLower())
                .Distinct()
                .ToList();

            if (normalized.Count == 0)
                return new List<PendingStaffMemberDTO>();

            using var ctx = _factory.CreateDbContext();

            // Match case-insensitive contra user.email (también lowercased en el lado servidor)
            var matchedUsers = await (
                from u in ctx.User
                join p in ctx.Person on u.UserId equals p.UserId into pj
                from p in pj.DefaultIfEmpty()
                where u.Email != null && normalized.Contains(u.Email.ToLower())
                select new
                {
                    u.UserId,
                    Email = u.Email!,
                    FullName = p != null ? p.FullName : null
                }
            ).ToListAsync();

            var userByEmail = matchedUsers
                .GroupBy(x => x.Email.Trim().ToLower())
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // ¿Qué user_ids ya cumplieron (tienen lección del proyecto/período)?
            var matchedUserIds = matchedUsers.Select(u => u.UserId).Distinct().ToList();

            var compliedUserIds = matchedUserIds.Count == 0
                ? new HashSet<int>()
                : (await ctx.Lesson
                    .Where(l => matchedUserIds.Contains(l.CreatedUserId)
                                && l.ProjectId == projectId
                                && l.Period == period
                                && l.State == true
                                && l.Active == true)
                    .Select(l => l.CreatedUserId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();

            var pending = new List<PendingStaffMemberDTO>();
            foreach (var email in normalized)
            {
                if (userByEmail.TryGetValue(email, out var u))
                {
                    if (!compliedUserIds.Contains(u.UserId))
                    {
                        pending.Add(new PendingStaffMemberDTO
                        {
                            Email = u.Email,        // versión "original" guardada en BD
                            UserId = u.UserId,
                            FullName = u.FullName
                        });
                    }
                }
                else
                {
                    // No existe en user → no hay cómo verificar cumplimiento → pendiente.
                    pending.Add(new PendingStaffMemberDTO
                    {
                        Email = email,
                        UserId = null,
                        FullName = null
                    });
                }
            }

            return pending;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Usuarios pendientes (user_project) — consumido por ReminderService
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<UserWithoutLessonsDTO>> GetUsersWithoutLessonsThisMonth(string period)
        {
            using var ctx = _factory.CreateDbContext();

            // El período viene del llamador (formato "MM-yyyy") para que la fecha
            // simulada de ReminderService se propague correctamente al filtro.
            var currentPeriod = period;

            var query =
                from up in ctx.UserProject
                join w in ctx.Worker on up.WorkerId equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId
                join pj in ctx.Project on up.ProjectId equals pj.ProjectId
                where up.State == true
                      && up.Active == true
                      && w.EmailCorporativo != null && w.EmailCorporativo != ""
                      // Pendiente si NO existe lección suya (por su usuario, si lo tiene)
                      // en el período. Un trabajador sin usuario nunca tendrá lección → pendiente.
                      && !ctx.Lesson.Any(l =>
                             l.CreatedUserId == p.UserId &&
                             l.ProjectId == up.ProjectId &&
                             l.Period == currentPeriod &&
                             l.State == true &&
                             l.Active == true
                         )
                group new { up, pj } by new
                {
                    WorkerId = w.Id,
                    p.FullName,
                    Email = w.EmailCorporativo,
                    UserId = p.UserId
                }
                into g
                select new UserWithoutLessonsDTO
                {
                    WorkerId = g.Key.WorkerId,
                    UserId = g.Key.UserId,
                    UserFullName = g.Key.FullName,
                    Email = g.Key.Email,
                    Projects = g.Select(x => new ProjectSimpleDTO
                    {
                        ProjectId = x.pj.ProjectId,
                        ProjectDescription = x.pj.ProjectDescription ?? string.Empty
                    }).ToList()
                };

            return await query.ToListAsync();
        }

        public async Task<List<string>> GetAbrilWorkerEmailsWithUserAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // worker.email_corporativo guarda el correo corporativo @abril (la columna
            // email_corporativo está siempre en NULL). Solo trabajadores con usuario
            // registrado y activo: worker.person_id → person.person_id →
            // person.user_id → app_user. Comparación lower() para que el match de
            // "@abril" y la deduplicación sean case-insensitive.
            var emails = await (
                from w in ctx.Worker
                where w.EmailCorporativo != null
                      && w.EmailCorporativo.ToLower().Contains("@abril")
                join p in ctx.Person on w.PersonId equals p.PersonId
                join u in ctx.User on p.UserId equals u.UserId
                where u.State == true && u.Active == true
                select w.EmailCorporativo!
            ).ToListAsync();

            // Dedup case-insensitive conservando una sola variante por correo.
            return emails
                .Select(e => e.Trim())
                .Where(e => e.Length > 0)
                .GroupBy(e => e.ToLowerInvariant())
                .Select(g => g.First())
                .ToList();
        }

        public async Task<List<string>> GetLessonReminderUserProjectEmailsAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Misma base que el CANAL 1 del recordatorio de subida
            // (GetUsersWithoutLessonsThisMonth) pero SIN el filtro de cumplimiento:
            // aquí queremos a todos los asignados vía user_project vivos y activos,
            // hayan subido o no su lección.
            var emails = await (
                from up in ctx.UserProject
                join w in ctx.Worker on up.WorkerId equals w.Id
                where up.State == true
                      && up.Active == true
                      && w.EmailCorporativo != null
                      && w.EmailCorporativo != ""
                select w.EmailCorporativo!
            ).ToListAsync();

            return emails
                .Select(e => e.Trim())
                .Where(e => e.Length > 0)
                .GroupBy(e => e.ToLowerInvariant())
                .Select(g => g.First())
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Jefaturas (lesson_jefe_reminder) — recordatorio del 4.º día
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<JefeReminderConfigItemDTO>> GetAllJefesAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Todos los workers de una categoría de jefatura (Jefe/Coordinador/Residente).
            var jefes = await (
                from w in ctx.Worker
                join pu in ctx.Puesto on w.PuestoId equals pu.PuestoId
                join c in ctx.Categoria on pu.CategoriaId equals c.CategoriaId
                where CategoriasJefatura.Contains(c.CategoriaId)
                join p in ctx.Person on w.PersonId equals p.PersonId into pj
                from p in pj.DefaultIfEmpty()
                select new
                {
                    w.Id,
                    w.EmailCorporativo,
                    Categoria = c.Nombre,
                    FullName = p != null ? p.FullName : null
                }
            ).ToListAsync();

            // Solo la fila viva (state=true) por worker.
            var rows = await ctx.LessonJefeReminder.Where(r => r.State).ToListAsync();
            var byWorker = rows.ToDictionary(r => r.WorkerId);

            return jefes
                .Select(j =>
                {
                    byWorker.TryGetValue(j.Id, out var row);
                    return new JefeReminderConfigItemDTO
                    {
                        LessonJefeReminderId = row?.LessonJefeReminderId,
                        WorkerId = j.Id,
                        FullName = j.FullName,
                        Email = j.EmailCorporativo,
                        Categoria = j.Categoria,
                        Active = row != null && row.Active
                    };
                })
                .OrderBy(x => x.FullName)
                .ToList();
        }

        public async Task<ToggleJefeReminderResultDTO> ToggleJefeAsync(int workerId)
        {
            using var ctx = _factory.CreateDbContext();

            var isJefe = await (
                from w in ctx.Worker
                where w.Id == workerId
                      && w.PuestoCatalogo != null
                      && CategoriasJefatura.Contains(w.PuestoCatalogo.CategoriaId)
                select w.Id
            ).AnyAsync();
            if (!isJefe)
                throw new AbrilException("El trabajador no existe o no es una jefatura (Jefe/Coordinador/Residente).", 404);

            var row = await ctx.LessonJefeReminder.FirstOrDefaultAsync(r => r.WorkerId == workerId && r.State);
            if (row == null)
            {
                // Primera activación: se crea el registro vivo en active=true.
                row = new LessonJefeReminder
                {
                    WorkerId = workerId,
                    Active = true,
                    State = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                ctx.LessonJefeReminder.Add(row);
            }
            else
            {
                row.Active = !row.Active;
                row.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await ctx.SaveChangesAsync();
            return new ToggleJefeReminderResultDTO
            {
                LessonJefeReminderId = row.LessonJefeReminderId,
                Active = row.Active
            };
        }

        public async Task<List<JefeReviewStatusDTO>> GetActiveJefesReviewStatusAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Jefaturas activas con correo. No se exige usuario propio: el conteo de
            // pendientes se resuelve por worker_id, que siempre existe.
            var jefes = await (
                from r in ctx.LessonJefeReminder
                join w in ctx.Worker on r.WorkerId equals w.Id
                join pu in ctx.Puesto on w.PuestoId equals pu.PuestoId
                join c in ctx.Categoria on pu.CategoriaId equals c.CategoriaId
                join p in ctx.Person on w.PersonId equals p.PersonId
                where r.State
                      && r.Active
                      && CategoriasJefatura.Contains(c.CategoriaId)
                      && w.EmailCorporativo != null
                      && w.EmailCorporativo != ""
                select new
                {
                    WorkerId = w.Id,
                    Email = w.EmailCorporativo!,
                    p.FullName
                }
            ).ToListAsync();

            var result = new List<JefeReviewStatusDTO>();
            foreach (var j in jefes)
            {
                // Subordinados directos: workers cuyo worker_lesson_jefe_id apunta a este jefe.
                // Se usan sus user_ids para contar lecciones pendientes.
                var subordinateUserIds = await (
                    from w in ctx.Worker
                    where w.WorkerLessonJefeId == j.WorkerId
                    join p in ctx.Person on w.PersonId equals p.PersonId
                    where p.UserId != null
                    select p.UserId!.Value
                ).Distinct().ToListAsync();

                var pending = subordinateUserIds.Count == 0
                    ? 0
                    : await ctx.Lesson.CountAsync(l =>
                        subordinateUserIds.Contains(l.CreatedUserId)
                        && l.ApprovalStatus == "PENDIENTE"
                        && l.State
                        && l.Active);

                result.Add(new JefeReviewStatusDTO
                {
                    WorkerId = j.WorkerId,
                    FullName = j.FullName,
                    Email = j.Email,
                    PendingCount = pending
                });
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Revisor de Trabajadores (workers.worker_lesson_jefe_id)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<WorkerRevisorItemDTO>> GetWorkerRevisoresAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Trabajadores con correo corporativo @abril.pe (vive en email_corporativo)
            // + su jefe directo si lo tiene. Left join doble: persona del trabajador
            // y worker/persona del jefe pueden faltar.
            return await (
                from w in ctx.Worker
                where w.EmailCorporativo != null && w.EmailCorporativo.ToLower().Contains("@abril.pe")
                join p in ctx.Person on w.PersonId equals p.PersonId into pj
                from p in pj.DefaultIfEmpty()
                join pu in ctx.Puesto on w.PuestoId equals pu.PuestoId into puj
                from pu in puj.DefaultIfEmpty()
                join c in ctx.Categoria on pu.CategoriaId equals c.CategoriaId into cj
                from c in cj.DefaultIfEmpty()
                join j in ctx.Worker on w.WorkerLessonJefeId equals j.Id into jj
                from j in jj.DefaultIfEmpty()
                join jp in ctx.Person on j.PersonId equals jp.PersonId into jpj
                from jp in jpj.DefaultIfEmpty()
                join jpu in ctx.Puesto on j.PuestoId equals jpu.PuestoId into jpuj
                from jpu in jpuj.DefaultIfEmpty()
                join jc in ctx.Categoria on jpu.CategoriaId equals jc.CategoriaId into jcj
                from jc in jcj.DefaultIfEmpty()
                orderby p != null ? p.FullName : ""
                select new WorkerRevisorItemDTO
                {
                    WorkerId = w.Id,
                    FullName = p != null ? p.FullName : null,
                    Email = w.EmailCorporativo,
                    CategoryId = pu != null ? pu.CategoriaId : (int?)null,
                    Category = c != null ? c.Nombre : null,
                    JefeWorkerId = w.WorkerLessonJefeId,
                    JefeFullName = jp != null ? jp.FullName : null,
                    JefeEmail = j != null ? j.EmailCorporativo : null,
                    JefeCategoryId = jpu != null ? jpu.CategoriaId : (int?)null,
                    JefeCategory = jc != null ? jc.Nombre : null,
                    AutoApproveLesson = w.AutoApproveLesson
                }
            ).ToListAsync();
        }

        public async Task<List<WorkerRevisorOptionDTO>> GetWorkerRevisorOptionsAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Cualquier worker con persona asociada puede ser jefe.
            return await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.State == true
                orderby p.FullName
                select new WorkerRevisorOptionDTO
                {
                    WorkerId = w.Id,
                    FullName = p.FullName,
                    Email = w.EmailCorporativo
                }
            ).ToListAsync();
        }

        public async Task UpdateWorkerRevisorAsync(int workerId, int? jefeWorkerId)
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == workerId);
            if (worker == null)
                throw new AbrilException("El trabajador no existe.", 404);

            if (jefeWorkerId.HasValue)
            {
                if (jefeWorkerId.Value == workerId)
                    throw new AbrilException("Un trabajador no puede ser su propio jefe.", 400);

                var jefeExists = await ctx.Worker.AnyAsync(w => w.Id == jefeWorkerId.Value);
                if (!jefeExists)
                    throw new AbrilException("El jefe seleccionado no existe.", 404);
            }

            worker.WorkerLessonJefeId = jefeWorkerId;
            // Asignar un revisor desactiva la auto-aprobación (son mutuamente excluyentes).
            if (jefeWorkerId.HasValue)
                worker.AutoApproveLesson = false;
            worker.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<ToggleAutoApproveLessonResultDTO> ToggleAutoApproveLessonAsync(int workerId)
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == workerId);
            if (worker == null)
                throw new AbrilException("El trabajador no existe.", 404);

            // La auto-aprobación solo aplica a trabajadores SIN revisor asignado.
            if (worker.WorkerLessonJefeId != null && !worker.AutoApproveLesson)
                throw new AbrilException("No se puede activar la auto-aprobación: el trabajador tiene un revisor asignado.", 400);

            worker.AutoApproveLesson = !worker.AutoApproveLesson;
            worker.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            return new ToggleAutoApproveLessonResultDTO
            {
                WorkerId = workerId,
                AutoApproveLesson = worker.AutoApproveLesson
            };
        }

        public async Task<List<UserWithoutLessonsDTO>> GetUsersWithoutLessonsByPeriod(DateTime periodDate)
        {
            using var ctx = _factory.CreateDbContext();

            var targetPeriod = new DateTime(periodDate.Year, periodDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var query =
                from up in ctx.UserProject
                join w in ctx.Worker on up.WorkerId equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId
                join pj in ctx.Project on up.ProjectId equals pj.ProjectId
                where up.State == true
                      && up.Active == true
                      && w.EmailCorporativo != null && w.EmailCorporativo != ""
                      && !ctx.Lesson.Any(l =>
                             l.CreatedUserId == p.UserId &&
                             l.ProjectId == up.ProjectId &&
                             l.PeriodDate == targetPeriod &&
                             l.State == true &&
                             l.Active == true
                         )
                group new { up, pj } by new
                {
                    WorkerId = w.Id,
                    p.FullName,
                    Email = w.EmailCorporativo,
                    UserId = p.UserId
                }
                into g
                select new UserWithoutLessonsDTO
                {
                    WorkerId = g.Key.WorkerId,
                    UserId = g.Key.UserId,
                    UserFullName = g.Key.FullName,
                    Email = g.Key.Email,
                    Projects = g.Select(x => new ProjectSimpleDTO
                    {
                        ProjectId = x.pj.ProjectId,
                        ProjectDescription = x.pj.ProjectDescription ?? string.Empty
                    }).ToList()
                };

            return await query.ToListAsync();
        }
    }
}
