using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Features.SsomaModule.Shared;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.MiSaludFeature.Infrastructure.Repositories
{
    public class MiSaludRepository : IMiSaludRepository
    {
        private const int PageSize = 10;
        /// <summary>Nombre exacto del área en area_item cuyo area_scope.email es el correo de GTH.</summary>
        private const string AreaGthNombre = "Gestión del Talento Humano";
        private readonly IDbContextFactory<AppDbContext> _factory;

        public MiSaludRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<int> ResolverWorkerIdAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var person = await ctx.Person.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new AbrilException("No tienes un perfil de persona asociado a tu usuario.", 403);

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.PersonId == person.PersonId)
                ?? throw new AbrilException("No tienes un perfil de trabajador registrado en el sistema.", 403);

            return worker.Id;
        }

        public async Task<MiSaludResumenDto> GetResumen(int workerId)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var worker = await ctx.Worker
                .Include(w => w.Person)
                .FirstOrDefaultAsync(w => w.Id == workerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            // EMO activo
            var emo = await (
                from e in ctx.WorkerEmo
                join t in ctx.SsEmoTipo on e.TipoEmoId equals t.Id into tj
                from t in tj.DefaultIfEmpty()
                where e.WorkerId == workerId && e.Activo
                orderby e.FechaEmo descending
                select new { e, tipoNombre = t != null ? t.Nombre : null }
            ).FirstOrDefaultAsync();

            // Restricciones vigentes del EMO activo
            var restricciones = new List<string>();
            if (emo != null)
            {
                restricciones = await (
                    from r in ctx.SsEmoRestriccion
                    join rt in ctx.SsRestriccionTipo on r.RestriccionTipoId equals rt.Id into rtj
                    from rt in rtj.DefaultIfEmpty()
                    where r.EmoId == emo.e.Id && r.Vigente
                    select rt != null ? rt.Descripcion : r.DescripcionLibre
                ).Where(d => d != null).Select(d => d!).ToListAsync();
            }

            // Último descanso
            var ultimoDescanso = await ctx.SsDescansoMedico
                .Where(d => d.WorkerId == workerId && d.State)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new { d.Estado, d.FechaFin })
                .FirstOrDefaultAsync();

            // Catálogo de tipos para el formulario de registro: solo los que el trabajador
            // puede elegir. Nombre = lo que se guarda; NombreCorto = lo que se le muestra.
            var tipos = await ctx.SsDescansoTipo
                .Where(t => t.State && t.Active && t.DisponibleMiSalud)
                .OrderBy(t => t.Orden).ThenBy(t => t.Nombre)
                .Select(t => new DescansoTipoDto
                {
                    Id          = t.Id,
                    Nombre      = t.Nombre,
                    NombreCorto = t.NombreCorto ?? t.Nombre,
                })
                .ToListAsync();

            DateOnly? fechaVenc = emo?.e.FechaVencimientoCalculada ?? emo?.e.FechaVencimiento;

            return new MiSaludResumenDto
            {
                WorkerId        = workerId,
                WorkerNombre    = worker.Person?.FullName,
                TieneEmo        = emo != null,
                EmoId           = emo?.e.Id,
                TipoEmo         = emo?.tipoNombre,
                Aptitud         = emo?.e.Aptitud,
                FechaEmo        = emo?.e.FechaEmo,
                FechaVencimiento = fechaVenc,
                DiasParaVencer  = fechaVenc.HasValue ? fechaVenc.Value.DayNumber - hoy.DayNumber : null,
                RestriccionesVigentes = restricciones,
                UltimoDescansoEstado  = ultimoDescanso?.Estado,
                UltimoDescansoFechaFin = ultimoDescanso?.FechaFin,
                TiposDescanso = tipos,
            };
        }

        public async Task<PagedResult<MiDescansoDto>> GetDescansos(int workerId, int page)
        {
            using var ctx = _factory.CreateDbContext();

            var q = ctx.SsDescansoMedico
                .Where(d => d.WorkerId == workerId && d.State)
                .OrderByDescending(d => d.FechaInicio);

            var total = await q.CountAsync();
            var pg    = page < 1 ? 1 : page;

            var items = await (
                from d in q
                join t in ctx.SsDescansoTipo on d.TipoId equals t.Id
                select new MiDescansoDto
                {
                    Id               = d.Id,
                    Tipo             = t.Nombre,
                    FechaInicio      = d.FechaInicio,
                    FechaFin         = d.FechaFin,
                    Dias             = d.Dias,
                    Diagnostico      = d.Diagnostico,
                    Estado           = d.Estado,
                    MotivoRechazo    = d.MotivoRechazo,
                    UrlCertificado   = d.UrlCertificado,
                    UrlDocumento     = d.UrlDocumento,
                    CreatedAt        = d.CreatedAt,
                })
                .Skip((pg - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Adjuntos de los descansos de la página (una sola consulta, sin N+1)
            var ids = items.Select(i => i.Id).ToList();
            if (ids.Count > 0)
            {
                var adjuntos = await ctx.SsDescansoMedicoAdjunto
                    .Where(a => a.State && ids.Contains(a.DescansoId))
                    .OrderBy(a => a.Id)
                    .Select(a => new { a.Id, a.DescansoId, a.Url, a.NombreArchivo })
                    .ToListAsync();

                var porDescanso = adjuntos
                    .GroupBy(a => a.DescansoId)
                    .ToDictionary(g => g.Key, g => g
                        .Select(a => new MiDescansoAdjuntoDto { Id = a.Id, Url = a.Url, Nombre = a.NombreArchivo })
                        .ToList());

                foreach (var item in items)
                    if (porDescanso.TryGetValue(item.Id, out var lista))
                        item.Adjuntos = lista;
            }

            return new PagedResult<MiDescansoDto>
            {
                Page        = pg,
                PageSize    = PageSize,
                TotalRecords = total,
                TotalPages  = (int)Math.Ceiling(total / (double)PageSize),
                Data        = items,
            };
        }

        public async Task<int> CreateDescanso(int workerId, CrearMiDescansoDto dto, int? userId, List<DescansoCertificadoSubidoDto> adjuntos)
        {
            using var ctx = _factory.CreateDbContext();

            // El trabajador solo puede clasificar su descanso con los tipos habilitados para
            // Mi Salud (los "común"); los ocupacionales los asigna SSOMA.
            var tipo = await ctx.SsDescansoTipo
                .FirstOrDefaultAsync(t => t.Id == dto.TipoId && t.State && t.Active && t.DisponibleMiSalud)
                ?? throw new AbrilException("El tipo de descanso seleccionado no es válido.", 400);

            // Todo descanso pertenece a un caso clínico (ss_descanso_medico.caso_id es NOT NULL).
            // El trabajador que autorreporta desde Mi Salud no declara si su descanso extiende un
            // caso ya abierto — no tiene por qué saberlo — así que siempre se le abre un caso nuevo;
            // si resulta ser la continuación de otro, SSOMA lo mueve al caso correcto desde
            // Revisión de Descansos (ver DescansoMedicoRepository.VincularCaso).
            var caso = new SsDescansoCaso
            {
                WorkerId      = workerId,
                FechaApertura = dto.FechaInicio,
                Estado        = "Abierto",
                CreatedAt     = DateTimeOffset.UtcNow,
                UpdatedAt     = DateTimeOffset.UtcNow,
                State         = true,
            };
            ctx.SsDescansoCaso.Add(caso);

            var entity = new SsDescansoMedico
            {
                WorkerId               = workerId,
                // CasoId se resuelve vía esta navegación al guardar (el caso aún no tiene id).
                Caso                   = caso,
                TipoId                 = tipo.Id,
                FechaInicio            = dto.FechaInicio,
                FechaFin               = dto.FechaFin,
                Dias                   = dto.Dias ?? (dto.FechaFin.DayNumber - dto.FechaInicio.DayNumber + 1),
                Diagnostico            = dto.Diagnostico,
                UrlCertificado         = adjuntos.Count > 0 ? adjuntos[0].Url : null,
                Estado                 = "Pendiente",
                ReportadoPorTrabajador = true,
                RegistradoPorId        = userId ?? workerId,
                CreatedAt              = DateTimeOffset.UtcNow,
                UpdatedAt              = DateTimeOffset.UtcNow,
                State                  = true,
            };

            ctx.SsDescansoMedico.Add(entity);

            foreach (var adjunto in adjuntos)
            {
                ctx.SsDescansoMedicoAdjunto.Add(new SsDescansoMedicoAdjunto
                {
                    Descanso      = entity,
                    Url           = adjunto.Url,
                    NombreArchivo = adjunto.Nombre,
                    DriveId       = adjunto.DriveId,
                    ItemId        = adjunto.ItemId,
                    State         = true,
                    CreatedAt     = DateTimeOffset.UtcNow,
                    UpdatedAt     = DateTimeOffset.UtcNow,
                });
            }

            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<MiDescansoAdjuntoArchivoDto?> GetAdjuntoDelWorkerAsync(int adjuntoId, int workerId)
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from a in ctx.SsDescansoMedicoAdjunto
                join d in ctx.SsDescansoMedico on a.DescansoId equals d.Id
                where a.Id == adjuntoId && a.State && d.State && d.WorkerId == workerId
                select new MiDescansoAdjuntoArchivoDto
                {
                    Url           = a.Url,
                    NombreArchivo = a.NombreArchivo,
                    DriveId       = a.DriveId,
                    ItemId        = a.ItemId,
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<DescansoNotificacionDatosDto> GetDatosNotificacionDescansoAsync(int workerId, int userId, int tipoId)
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker
                .Where(w => w.Id == workerId)
                .Select(w => new
                {
                    Nombre = w.Person != null ? w.Person.FullName : null,
                    w.EmailCorporativo,
                })
                .FirstOrDefaultAsync();

            var userEmail = await ctx.User
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            // Correo del área GTH: configurable en area_scope.email (mismo criterio que
            // el fallback de solicitud-salidas) para no hardcodear el correo del área.
            var gthEmail = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                where s.State && ai.State
                      && ai.AreaItemName == AreaGthNombre
                      && s.Email != null && s.Email != ""
                orderby s.AreaScopeId
                select s.Email
            ).FirstOrDefaultAsync();

            // En el correo va el nombre largo del tipo, no el corto que ve el trabajador.
            var tipoNombre = await ctx.SsDescansoTipo
                .Where(t => t.Id == tipoId)
                .Select(t => t.Nombre)
                .FirstOrDefaultAsync();

            return new DescansoNotificacionDatosDto
            {
                WorkerNombre = worker?.Nombre,
                WorkerEmail  = !string.IsNullOrWhiteSpace(userEmail) ? userEmail.Trim() : worker?.EmailCorporativo?.Trim(),
                GthEmail     = gthEmail?.Trim(),
                TipoNombre   = tipoNombre,
            };
        }

        public async Task<List<MiDescansoCorreoConfigDto>> GetCorreoConfigsAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsDescansoCorreoConfig
                .Where(c => c.State)
                .OrderBy(c => c.Orden)
                .ThenBy(c => c.Id)
                .Select(c => new MiDescansoCorreoConfigDto
                {
                    Id          = c.Id,
                    Codigo      = c.Codigo,
                    Nombre      = c.Nombre,
                    Descripcion = c.Descripcion,
                    Active      = c.Active,
                    Orden       = c.Orden,
                })
                .ToListAsync();
        }

        public async Task<Dictionary<string, bool>> GetCorreoConfigMapAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var rows = await ctx.SsDescansoCorreoConfig
                .Where(c => c.State)
                .Select(c => new { c.Codigo, c.Active })
                .ToListAsync();

            // Case-insensitive por codigo; si hubiera duplicados vivos, gana el último.
            var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
                map[r.Codigo] = r.Active;
            return map;
        }

        public async Task<bool> SetCorreoConfigActiveAsync(int id, bool active)
        {
            using var ctx = _factory.CreateDbContext();
            var row = await ctx.SsDescansoCorreoConfig.FirstOrDefaultAsync(c => c.Id == id && c.State);
            if (row == null) return false;

            row.Active    = active;
            row.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
            return true;
        }
    }
}
