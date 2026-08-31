using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    /// <summary>
    /// Matriz de destinatarios de los correos de EMO: correo × perfil del trabajador ×
    /// destinatario (<c>ss_emo_correo_regla</c>). Sirve tanto a la pantalla de
    /// configuración (lectura completa + CRUD de correos adicionales) como al envío
    /// real, que consume las celdas activas ya aplanadas.
    /// </summary>
    public class EmoCorreoConfigRepository : IEmoCorreoConfigRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EmoCorreoConfigRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<EmoCorreosConfigDto> GetConfigAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var perfiles = await ctx.SsEmoCorreoPerfil.AsNoTracking()
                .Where(p => p.State && p.Active)
                .OrderBy(p => p.Orden).ThenBy(p => p.Id)
                .Select(p => new EmoCorreoPerfilDto
                {
                    Id          = p.Id,
                    Codigo      = p.Codigo,
                    Nombre      = p.Nombre,
                    Descripcion = p.Descripcion,
                })
                .ToListAsync();

            var eventos = await ctx.SsEmoCorreoEvento.AsNoTracking()
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.Orden).ThenBy(e => e.Id)
                .Select(e => new EmoCorreoEventoDto
                {
                    Id          = e.Id,
                    Codigo      = e.Codigo,
                    Nombre      = e.Nombre,
                    Descripcion = e.Descripcion,
                    Orden       = e.Orden,
                })
                .ToListAsync();

            // Toda la matriz de una sola vez; el armado por evento se hace en memoria
            // para no repetir la consulta una vez por sección de la pantalla.
            var celdas = await (
                from r in ctx.SsEmoCorreoRegla
                join d in ctx.SsEmoCorreoDestinatario on r.DestinatarioId equals d.Id
                join t in ctx.SsEmoCorreoTipo on d.TipoId equals t.Id
                join p in ctx.SsEmoCorreoPerfil on r.PerfilId equals p.Id
                where r.State && d.State && t.State && p.State && p.Active
                select new
                {
                    r.Id,
                    r.EventoId,
                    r.PerfilId,
                    PerfilCodigo = p.Codigo,
                    r.Active,
                    DestinatarioId = d.Id,
                    d.Codigo,
                    d.Email,
                    d.Nombre,
                    d.Descripcion,
                    d.Editable,
                    d.Orden,
                    TipoCodigo = t.Codigo,
                })
                .AsNoTracking()
                .ToListAsync();

            var porEvento = celdas.ToLookup(c => c.EventoId);

            foreach (var evento in eventos)
            {
                evento.Destinatarios = porEvento[evento.Id]
                    .GroupBy(c => c.DestinatarioId)
                    .Select(g =>
                    {
                        var d = g.First();
                        var esAdicional = string.IsNullOrWhiteSpace(d.Codigo);
                        return new EmoCorreoFilaDto
                        {
                            DestinatarioId = d.DestinatarioId,
                            Codigo         = d.Codigo,
                            Nombre         = d.Nombre,
                            Descripcion    = d.Descripcion,
                            Email          = d.Email,
                            Tipo           = d.TipoCodigo,
                            Editable       = d.Editable,
                            Eliminable     = esAdicional,
                            RequiereArqCom = EmoCorreoDestinatarioCodigo.RequiereArquitecturaComercial(d.Codigo),
                            // Aviso para la pantalla: un buzón de área activo pero sin correo
                            // cargado no le llega a nadie, y sin este dato no se nota.
                            SinCorreo      = d.Editable && string.IsNullOrWhiteSpace(d.Email),
                            Orden          = d.Orden,
                            Celdas = g
                                .Select(c => new EmoCorreoCeldaDto
                                {
                                    ReglaId      = c.Id,
                                    PerfilId     = c.PerfilId,
                                    PerfilCodigo = c.PerfilCodigo,
                                    Active       = c.Active,
                                })
                                .OrderBy(c => perfiles.FindIndex(p => p.Id == c.PerfilId))
                                .ToList(),
                        };
                    })
                    .OrderBy(f => f.Orden).ThenBy(f => f.DestinatarioId)
                    .ToList();
            }

            return new EmoCorreosConfigDto { Perfiles = perfiles, Eventos = eventos };
        }

        public async Task<int> CreateAdicionalAsync(
            string eventoCodigo, string tipoCodigo, string email, string? nombre)
        {
            using var ctx = _factory.CreateDbContext();

            var evento = await ctx.SsEmoCorreoEvento
                .FirstOrDefaultAsync(e => e.State && e.Codigo.ToUpper() == eventoCodigo.ToUpper())
                ?? throw new AbrilException("El correo indicado no existe.", 400);

            var tipo = await ctx.SsEmoCorreoTipo
                .FirstOrDefaultAsync(t => t.State && t.Codigo.ToUpper() == tipoCodigo.ToUpper())
                ?? throw new AbrilException("El tipo de destinatario no existe.", 400);

            var emailNorm = email.Trim();

            // Solo se controla el duplicado entre correos adicionales: dos buzones de área
            // pueden compartir dirección (la misma persona puede cubrir dos roles).
            var duplicado = await ctx.SsEmoCorreoDestinatario.AnyAsync(d =>
                d.State && d.Codigo == null && d.TipoId == tipo.Id && d.Email != null &&
                d.Email.ToLower() == emailNorm.ToLower());
            if (duplicado)
                throw new AbrilException("Ese correo ya está agregado como destinatario adicional.", 409);

            // Los adicionales van después del catálogo, que ocupa el orden 1..13.
            var ultimoOrden = await ctx.SsEmoCorreoDestinatario
                .Where(d => d.State)
                .MaxAsync(d => (int?)d.Orden) ?? 0;

            var dest = new SsEmoCorreoDestinatario
            {
                TipoId    = tipo.Id,
                Codigo    = null,
                Email     = emailNorm,
                Nombre    = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim(),
                Editable  = true,
                Orden     = Math.Max(ultimoOrden + 1, 100),
                Active    = true,
                State     = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            ctx.SsEmoCorreoDestinatario.Add(dest);
            await ctx.SaveChangesAsync();

            // Celda en los 4 correos × 4 perfiles: activo solo en el correo desde cuya
            // sección se agregó, para que el alta haga lo que el usuario espera sin
            // prenderle de golpe los otros tres.
            var eventos  = await ctx.SsEmoCorreoEvento.AsNoTracking()
                .Where(e => e.State).Select(e => new { e.Id, e.Codigo }).ToListAsync();
            var perfiles = await ctx.SsEmoCorreoPerfil.AsNoTracking()
                .Where(p => p.State).Select(p => p.Id).ToListAsync();

            foreach (var e in eventos)
                foreach (var perfilId in perfiles)
                    ctx.SsEmoCorreoRegla.Add(new SsEmoCorreoRegla
                    {
                        EventoId        = e.Id,
                        PerfilId        = perfilId,
                        DestinatarioId  = dest.Id,
                        Active          = e.Id == evento.Id,
                        State           = true,
                        CreatedAt       = DateTimeOffset.UtcNow,
                        UpdatedAt       = DateTimeOffset.UtcNow,
                    });

            await ctx.SaveChangesAsync();
            return dest.Id;
        }

        public async Task UpdateDestinatarioAsync(int id, string email, string? nombre, string? tipoCodigo)
        {
            using var ctx = _factory.CreateDbContext();

            var ent = await ctx.SsEmoCorreoDestinatario.FirstOrDefaultAsync(d => d.Id == id && d.State)
                ?? throw new AbrilException("Destinatario no encontrado.", 404);

            if (!ent.Editable)
                throw new AbrilException(
                    "Este destinatario se resuelve al enviar: su correo no se edita acá.", 409);

            var emailNorm = email.Trim();
            var tipoId = ent.TipoId;

            // El tipo (Para/CC) solo tiene sentido en los correos adicionales; los buzones
            // de área siempre van en "Para".
            if (!string.IsNullOrWhiteSpace(tipoCodigo) && ent.Codigo == null)
            {
                var tipo = await ctx.SsEmoCorreoTipo
                    .FirstOrDefaultAsync(t => t.State && t.Codigo.ToUpper() == tipoCodigo.ToUpper())
                    ?? throw new AbrilException("El tipo de destinatario no existe.", 400);
                tipoId = tipo.Id;
            }

            // Igual que en el alta: la unicidad solo aplica entre correos adicionales.
            if (ent.Codigo == null)
            {
                var duplicado = await ctx.SsEmoCorreoDestinatario.AnyAsync(d =>
                    d.State && d.Codigo == null && d.Id != id && d.TipoId == tipoId &&
                    d.Email != null && d.Email.ToLower() == emailNorm.ToLower());
                if (duplicado)
                    throw new AbrilException("Ese correo ya está agregado como destinatario adicional.", 409);
            }

            ent.TipoId    = tipoId;
            ent.Email     = emailNorm;
            ent.Nombre    = string.IsNullOrWhiteSpace(nombre) ? ent.Nombre : nombre.Trim();
            ent.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task SetReglaActiveAsync(int reglaId, bool active)
        {
            using var ctx = _factory.CreateDbContext();

            var regla = await ctx.SsEmoCorreoRegla.FirstOrDefaultAsync(r => r.Id == reglaId && r.State)
                ?? throw new AbrilException("Configuración de correo no encontrada.", 404);

            regla.Active    = active;
            regla.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task DeleteAdicionalAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var ent = await ctx.SsEmoCorreoDestinatario.FirstOrDefaultAsync(d => d.Id == id && d.State)
                ?? throw new AbrilException("Destinatario no encontrado.", 404);

            if (ent.Codigo != null)
                throw new AbrilException(
                    "Este destinatario es parte del catálogo y no se puede eliminar. Desactívalo en cada correo.", 409);

            // Soft delete: nada se borra de la BD (auditoría).
            var reglas = await ctx.SsEmoCorreoRegla.Where(r => r.DestinatarioId == id && r.State).ToListAsync();
            foreach (var r in reglas)
            {
                r.State     = false;
                r.UpdatedAt = DateTimeOffset.UtcNow;
            }

            ent.State     = false;
            ent.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<List<EmoCorreoReglaEnvioDto>> GetReglasEnvioAsync(string eventoCodigo)
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from r in ctx.SsEmoCorreoRegla
                join e in ctx.SsEmoCorreoEvento on r.EventoId equals e.Id
                join p in ctx.SsEmoCorreoPerfil on r.PerfilId equals p.Id
                join d in ctx.SsEmoCorreoDestinatario on r.DestinatarioId equals d.Id
                join t in ctx.SsEmoCorreoTipo on d.TipoId equals t.Id
                where r.State && r.Active
                      && e.State && e.Active && e.Codigo.ToUpper() == eventoCodigo.ToUpper()
                      && p.State && p.Active
                      && d.State && t.State
                select new EmoCorreoReglaEnvioDto
                {
                    PerfilCodigo       = p.Codigo,
                    DestinatarioCodigo = d.Codigo,
                    Email              = d.Email,
                    Nombre             = d.Nombre,
                    EsCopia            = t.Codigo.ToUpper() == EmoCorreoTipoCodigo.Copia,
                })
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
