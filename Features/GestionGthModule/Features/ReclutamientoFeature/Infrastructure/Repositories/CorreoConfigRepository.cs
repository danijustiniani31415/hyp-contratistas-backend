using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Repositories
{
    /// <inheritdoc cref="ICorreoConfigRepository"/>
    public class CorreoConfigRepository : ICorreoConfigRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CorreoConfigRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ───────────────────────────── Lectura ─────────────────────────────
        public async Task<CorreoConfigDto> GetConfigAsync(IReadOnlyList<string> tipoCodigos)
        {
            using var ctx = _factory.CreateDbContext();

            var codigos = tipoCodigos.Select(c => c.ToUpperInvariant()).ToList();

            var tiposRaw = await ctx.GthCorreoTipo.AsNoTracking()
                .Where(t => t.State && codigos.Contains(t.Codigo.ToUpper()))
                .OrderBy(t => t.Orden).ThenBy(t => t.GthCorreoTipoId)
                .Select(t => new
                {
                    Evento = new CorreoConfigEventoDto
                    {
                        Id                  = t.GthCorreoTipoId,
                        Codigo              = t.Codigo,
                        Nombre              = t.Nombre,
                        Descripcion         = t.Descripcion,
                        Active              = t.Active,
                        PrincipalAutomatico = t.PrincipalAutomatico,
                        Orden               = t.Orden,
                    },
                    t.PrincipalAutomaticoActive,
                    t.PrincipalAutomaticoNombre,
                })
                .ToListAsync();
            if (tiposRaw.Count == 0) return new CorreoConfigDto();

            var tipos = tiposRaw.Select(t => t.Evento).ToList();

            var tipoIds = tipos.Select(t => t.Id).ToList();

            var filas = await ctx.GthCorreoDestinatario.AsNoTracking()
                .Where(d => d.State && tipoIds.Contains(d.GthCorreoTipoId))
                .OrderBy(d => d.Orden).ThenBy(d => d.GthCorreoDestinatarioId)
                .Select(d => new
                {
                    d.GthCorreoTipoId,
                    Fila = new CorreoDestinatarioFilaDto
                    {
                        DestinatarioId = d.GthCorreoDestinatarioId,
                        Codigo         = d.Codigo,
                        Nombre         = d.Nombre,
                        Descripcion    = d.Descripcion,
                        Email          = d.Email,
                        EsCopia        = d.EsCopia,
                        Active         = d.Active,
                        Orden          = d.Orden,
                    },
                })
                .ToListAsync();

            // Los dinámicos que no dependen de la solicitud se resuelven acá para que la
            // pantalla muestre el correo real y avise si hoy no resuelve a nadie. Solo se
            // consulta lo que alguna fila necesita.
            var codigosPresentes = new HashSet<string>(
                filas.Where(f => !string.IsNullOrWhiteSpace(f.Fila.Codigo)).Select(f => f.Fila.Codigo!),
                StringComparer.OrdinalIgnoreCase);

            var gerenteGeneral = codigosPresentes.Contains(CorreoDestinatarioCodigo.GerenteGeneral)
                ? await GetGerenteGeneralAsync(ctx)
                : null;

            // Las áreas van juntas en una sola consulta: la pantalla puede tener varias secciones
            // con destinatarios de área distintos y no hay motivo para pagar un roundtrip por cada
            // una.
            var areasPedidas = new List<int>();
            if (codigosPresentes.Contains(CorreoDestinatarioCodigo.GthArea))
                areasPedidas.Add(AreaScopeIds.GestionDelTalentoHumano);
            if (codigosPresentes.Contains(CorreoDestinatarioCodigo.TiArea))
                areasPedidas.Add(AreaScopeIds.TecnologiaDeLaInformacion);
            var emailsArea = await GetEmailsAreasAsync(ctx, areasPedidas);

            foreach (var f in filas)
            {
                var fila = f.Fila;
                var esAdicional = string.IsNullOrWhiteSpace(fila.Codigo);

                fila.Editable   = esAdicional;
                fila.Eliminable = esAdicional;

                if (esAdicional)
                {
                    fila.EmailResuelto = fila.Email;
                    continue;
                }

                switch (fila.Codigo!.ToUpperInvariant())
                {
                    case CorreoDestinatarioCodigo.GerenteGeneral:
                        fila.EmailResuelto  = gerenteGeneral?.Email;
                        fila.NombreResuelto = gerenteGeneral?.Nombre;
                        break;

                    case CorreoDestinatarioCodigo.GthArea:
                        fila.EmailResuelto = emailsArea.GetValueOrDefault(AreaScopeIds.GestionDelTalentoHumano);
                        break;

                    case CorreoDestinatarioCodigo.TiArea:
                        fila.EmailResuelto = emailsArea.GetValueOrDefault(AreaScopeIds.TecnologiaDeLaInformacion);
                        break;

                    case CorreoDestinatarioCodigo.GerenteArea:
                        // Cambia según quién registre la solicitud: no hay un correo que mostrar.
                        fila.DependeDeLaSolicitud = true;
                        break;
                }

                fila.SinCorreo = !fila.DependeDeLaSolicitud && string.IsNullOrWhiteSpace(fila.EmailResuelto);
            }

            var porTipo = filas.ToLookup(f => f.GthCorreoTipoId, f => f.Fila);
            foreach (var t in tiposRaw)
            {
                var destinatarios = porTipo[t.Evento.Id].ToList();

                // El destinatario que pone el sistema va como una fila más y de primera: en la
                // pantalla es un destinatario como cualquier otro (se prende y se apaga), aunque no
                // salga de gth_correo_destinatario sino del propio tipo de correo.
                if (t.Evento.PrincipalAutomatico)
                    destinatarios.Insert(0, new CorreoDestinatarioFilaDto
                    {
                        DestinatarioId     = 0,
                        Nombre             = string.IsNullOrWhiteSpace(t.PrincipalAutomaticoNombre)
                                                ? "Destinatario que asigna el sistema"
                                                : t.PrincipalAutomaticoNombre,
                        EsCopia            = false,
                        Active             = t.PrincipalAutomaticoActive,
                        Editable           = false,
                        Eliminable         = false,
                        EsPrincipalSistema = true,
                        Orden              = 0,
                    });

                t.Evento.Destinatarios = destinatarios;
            }

            return new CorreoConfigDto { Eventos = tipos };
        }

        public async Task<CorreoEnvioConfigDto> GetEnvioConfigAsync(string tipoCodigo)
        {
            using var ctx = _factory.CreateDbContext();

            // Left join en vez de dos consultas: el interruptor del principal automático vive en el
            // tipo y hay que leerlo aunque el correo esté apagado o no tenga ningún destinatario
            // configurado, que es justo cuando la consulta de filas no devolvería nada.
            // Columnas planas y nullable en vez de un DTO armado dentro de la proyección: con el
            // left join, las filas del tipo sin destinatarios traen todo en NULL y así se
            // materializan sin ambigüedad. El orden se aplica en memoria (son un puñado de filas)
            // para no ordenar por columnas del lado nulo del join.
            var raw = await (
                from t in ctx.GthCorreoTipo
                where t.State && t.Codigo.ToUpper() == tipoCodigo.ToUpper()
                join d in ctx.GthCorreoDestinatario.Where(x => x.State && x.Active)
                    on t.GthCorreoTipoId equals d.GthCorreoTipoId into ds
                from d in ds.DefaultIfEmpty()
                select new
                {
                    TipoActive = t.Active,
                    t.PrincipalAutomaticoActive,
                    DestinatarioId = (int?)d.GthCorreoDestinatarioId,
                    d.Codigo,
                    d.Email,
                    d.Nombre,
                    EsCopia = (bool?)d.EsCopia,
                    Orden   = (int?)d.Orden,
                })
                .AsNoTracking()
                .ToListAsync();

            if (raw.Count == 0) return new CorreoEnvioConfigDto();

            return new CorreoEnvioConfigDto
            {
                // El maestro manda: apagado, el correo no se envía a NADIE, ni siquiera a su
                // principal automático. Antes ese principal lo seguía recibiendo (era la única
                // forma de no dejar sin aviso al postulante), pero ahora tiene su propio
                // interruptor, así que "Correo desactivado" significa lo que dice.
                PrincipalAutomaticoActivo = raw[0].TipoActive && raw[0].PrincipalAutomaticoActive,
                // Correo apagado con el interruptor maestro → ninguno de los destinatarios
                // configurados recibe nada (el principal automático se rige por su propio flag).
                Filas = raw[0].TipoActive
                    ? raw.Where(x => x.DestinatarioId.HasValue)
                         .OrderBy(x => x.Orden).ThenBy(x => x.DestinatarioId)
                         .Select(x => new CorreoDestinatarioEnvioDto
                         {
                             Codigo  = x.Codigo,
                             Email   = x.Email,
                             Nombre  = x.Nombre,
                             EsCopia = x.EsCopia ?? false,
                             Orden   = x.Orden ?? 0,
                         })
                         .ToList()
                    : new List<CorreoDestinatarioEnvioDto>(),
            };
        }

        public async Task<CorreoDestinatarioResueltoDto?> GetGerenteGeneralAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await GetGerenteGeneralAsync(ctx);
        }

        public async Task<string?> GetEmailAreaGthAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await GetEmailAreaAsync(ctx, AreaScopeIds.GestionDelTalentoHumano);
        }

        public async Task<string?> GetEmailAreaTiAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await GetEmailAreaAsync(ctx, AreaScopeIds.TecnologiaDeLaInformacion);
        }

        /// <summary>
        /// Gerente General vigente. Una misma persona puede tener varias fichas en
        /// <c>workers</c> (reingreso), así que se filtra por ACTIVO y se desempata por la ficha
        /// más reciente en vez de dejar que Postgres devuelva una cualquiera.
        /// </summary>
        private static async Task<CorreoDestinatarioResueltoDto?> GetGerenteGeneralAsync(AppDbContext ctx)
        {
            return await (
                from w in ctx.Worker.AsNoTracking()
                where w.PuestoCatalogo != null
                      && w.PuestoCatalogo.CategoriaId == CategoriaIds.GerenteGeneral
                      && w.WorkersEstadoId == WorkersEstadoIds.Activo
                      && w.EmailCorporativo != null && w.EmailCorporativo.Contains("@")
                orderby w.Id descending
                select new CorreoDestinatarioResueltoDto
                {
                    Email  = w.EmailCorporativo!,
                    Nombre = w.Person != null ? w.Person.FullName : w.ApellidoNombre,
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>Correo cargado en un nodo vigente de <c>area_scope</c>; null si no tiene.</summary>
        private static async Task<string?> GetEmailAreaAsync(AppDbContext ctx, int areaScopeId)
        {
            return await ctx.AreaScope.AsNoTracking()
                .Where(s => s.AreaScopeId == areaScopeId && s.State)
                .Select(s => s.Email)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Lo mismo que <see cref="GetEmailAreaAsync"/> pero para varias áreas a la vez, en un solo
        /// roundtrip. Las áreas sin correo cargado no aparecen en el diccionario.
        /// </summary>
        private static async Task<Dictionary<int, string?>> GetEmailsAreasAsync(
            AppDbContext ctx, IReadOnlyList<int> areaScopeIds)
        {
            if (areaScopeIds.Count == 0) return new Dictionary<int, string?>();

            return await ctx.AreaScope.AsNoTracking()
                .Where(s => areaScopeIds.Contains(s.AreaScopeId) && s.State)
                .ToDictionaryAsync(s => s.AreaScopeId, s => s.Email);
        }

        // ───────────────────────────── Escritura ─────────────────────────────
        public async Task<int> CreateAdicionalAsync(
            string tipoCodigo, string email, string? nombre, bool esCopia, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var tipo = await ctx.GthCorreoTipo
                .FirstOrDefaultAsync(t => t.State && t.Codigo.ToUpper() == tipoCodigo.ToUpper())
                ?? throw new AbrilException("El correo indicado no existe.", 400);

            var emailNorm = email.Trim().ToLowerInvariant();

            var duplicado = await ctx.GthCorreoDestinatario.AnyAsync(d =>
                d.State && d.Codigo == null && d.GthCorreoTipoId == tipo.GthCorreoTipoId &&
                d.Email != null && d.Email.ToLower() == emailNorm);
            if (duplicado)
                throw new AbrilException("Ese correo ya está agregado como destinatario de este correo.", 409);

            // Los adicionales van siempre después de los dinámicos del catálogo (orden 1..n).
            var ultimoOrden = await ctx.GthCorreoDestinatario
                .Where(d => d.State && d.GthCorreoTipoId == tipo.GthCorreoTipoId)
                .MaxAsync(d => (int?)d.Orden) ?? 0;

            var now = DateTimeOffset.UtcNow;
            var dest = new GthCorreoDestinatario
            {
                GthCorreoTipoId = tipo.GthCorreoTipoId,
                Codigo          = null,
                Email           = emailNorm,
                Nombre          = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim(),
                EsCopia         = esCopia,
                Orden           = Math.Max(ultimoOrden + 1, 100),
                Active          = true,
                State           = true,
                CreatedDateTime = now,
                CreatedUserId   = userId,
            };
            ctx.GthCorreoDestinatario.Add(dest);
            await ctx.SaveChangesAsync();

            return dest.GthCorreoDestinatarioId;
        }

        public async Task UpdateAdicionalAsync(
            int destinatarioId, string email, string? nombre, bool esCopia,
            IReadOnlyList<string> tiposPermitidos, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var dest = await BuscarDestinatarioAsync(ctx, destinatarioId, tiposPermitidos);

            if (dest.Codigo != null)
                throw new AbrilException(
                    "Este destinatario se resuelve al enviar: su correo no se edita acá.", 409);

            var emailNorm = email.Trim().ToLowerInvariant();

            var duplicado = await ctx.GthCorreoDestinatario.AnyAsync(d =>
                d.State && d.Codigo == null && d.GthCorreoDestinatarioId != destinatarioId &&
                d.GthCorreoTipoId == dest.GthCorreoTipoId &&
                d.Email != null && d.Email.ToLower() == emailNorm);
            if (duplicado)
                throw new AbrilException("Ese correo ya está agregado como destinatario de este correo.", 409);

            dest.Email             = emailNorm;
            dest.Nombre            = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            dest.EsCopia           = esCopia;
            dest.UpdatedDateTime   = DateTimeOffset.UtcNow;
            dest.UpdatedUserId     = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task SetDestinatarioActiveAsync(
            int destinatarioId, bool active, IReadOnlyList<string> tiposPermitidos, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var dest = await BuscarDestinatarioAsync(ctx, destinatarioId, tiposPermitidos);

            dest.Active          = active;
            dest.UpdatedDateTime = DateTimeOffset.UtcNow;
            dest.UpdatedUserId   = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task SetPrincipalAutomaticoActiveAsync(string tipoCodigo, bool active, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var tipo = await ctx.GthCorreoTipo
                .FirstOrDefaultAsync(t => t.State && t.Codigo.ToUpper() == tipoCodigo.ToUpper())
                ?? throw new AbrilException("El correo indicado no existe.", 404);

            if (!tipo.PrincipalAutomatico)
                throw new AbrilException("Este correo no tiene un destinatario que asigne el sistema.", 400);

            tipo.PrincipalAutomaticoActive = active;
            tipo.UpdatedDateTime           = DateTimeOffset.UtcNow;
            tipo.UpdatedUserId             = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task SetTipoActiveAsync(string tipoCodigo, bool active, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var tipo = await ctx.GthCorreoTipo
                .FirstOrDefaultAsync(t => t.State && t.Codigo.ToUpper() == tipoCodigo.ToUpper())
                ?? throw new AbrilException("El correo indicado no existe.", 404);

            tipo.Active            = active;
            tipo.UpdatedDateTime   = DateTimeOffset.UtcNow;
            tipo.UpdatedUserId     = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task DeleteAdicionalAsync(
            int destinatarioId, IReadOnlyList<string> tiposPermitidos, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var dest = await BuscarDestinatarioAsync(ctx, destinatarioId, tiposPermitidos);

            if (dest.Codigo != null)
                throw new AbrilException(
                    "Este destinatario es parte del catálogo y no se puede eliminar. Apágalo con su interruptor.", 409);

            // Soft delete: nada se borra de la BD (auditoría).
            dest.State           = false;
            dest.UpdatedDateTime = DateTimeOffset.UtcNow;
            dest.UpdatedUserId   = userId;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Destinatario vigente que pertenezca a alguno de los correos de la pantalla que lo pide.
        /// Un id de otra pantalla devuelve 404 igual que uno inexistente: desde Reclutamiento no se
        /// tocan los destinatarios de Solicitud de Personal ni al revés.
        /// </summary>
        private static async Task<GthCorreoDestinatario> BuscarDestinatarioAsync(
            AppDbContext ctx, int destinatarioId, IReadOnlyList<string> tiposPermitidos)
        {
            var codigos = tiposPermitidos.Select(c => c.ToUpperInvariant()).ToList();

            return await (
                from d in ctx.GthCorreoDestinatario
                join t in ctx.GthCorreoTipo on d.GthCorreoTipoId equals t.GthCorreoTipoId
                where d.GthCorreoDestinatarioId == destinatarioId && d.State
                      && t.State && codigos.Contains(t.Codigo.ToUpper())
                select d).FirstOrDefaultAsync()
                ?? throw new AbrilException("Destinatario no encontrado.", 404);
        }
    }
}
