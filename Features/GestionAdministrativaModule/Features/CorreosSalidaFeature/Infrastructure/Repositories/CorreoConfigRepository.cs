using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Shared.Models;
using Abril_Backend.Features.GestionAdministrativa.Shared.Services;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Infrastructure.Repositories
{
    public class CorreoConfigRepository : ICorreoConfigRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private const string EmailDomainCorp = "@abril.pe";

        public CorreoConfigRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        public async Task<CorreoConfigInicialDto> GetInicialAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var eventos = await ctx.GaCorreoEvento
                .Where(e => e.State)
                .OrderBy(e => e.Orden)
                .Select(e => new CorreoEventoDto
                {
                    Id = e.Id,
                    Codigo = e.Codigo,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Orden = e.Orden,
                    Active = e.Active,
                    DestinatarioPrincipalNombre = e.DestinatarioPrincipalNombre,
                    DestinatarioPrincipalActivo = e.DestinatarioPrincipalActivo,
                    PermiteDesactivarEnvio = e.PermiteDesactivarEnvio,
                    PermiteDesactivarPrincipal = e.PermiteDesactivarPrincipal,
                })
                .ToListAsync();

            var tipos = await ctx.GaCorreoTipoDestinatario
                .Where(t => t.State)
                .OrderBy(t => t.Orden)
                .Select(t => new CorreoTipoDto { Id = t.Id, Codigo = t.Codigo, Nombre = t.Nombre })
                .ToListAsync();

            var reglas = await (
                from r in ctx.GaCorreoRegla
                join t in ctx.GaCorreoTipoDestinatario on r.TipoId equals t.Id
                where r.State
                orderby r.Orden
                select new
                {
                    r.Id,
                    r.EventoId,
                    r.EsExclusion,
                    TipoCodigo = t.Codigo,
                    r.WorkerId,
                    r.AreaScopeId,
                    r.Correo,
                    r.IncluirDescendientes,
                    r.Active,
                }
            ).ToListAsync();

            foreach (var ev in eventos)
            {
                var delEvento = reglas.Where(r => r.EventoId == ev.Id).ToList();
                ev.Incluir = delEvento.Where(r => !r.EsExclusion).Select(r => new CorreoReglaDto
                {
                    Id = r.Id,
                    TipoCodigo = r.TipoCodigo,
                    WorkerId = r.WorkerId,
                    AreaScopeId = r.AreaScopeId,
                    Correo = r.Correo,
                    IncluirDescendientes = r.IncluirDescendientes,
                    Active = r.Active,
                }).ToList();
                ev.Excluir = delEvento.Where(r => r.EsExclusion).Select(r => new CorreoReglaDto
                {
                    Id = r.Id,
                    TipoCodigo = r.TipoCodigo,
                    WorkerId = r.WorkerId,
                    AreaScopeId = r.AreaScopeId,
                    Correo = r.Correo,
                    IncluirDescendientes = r.IncluirDescendientes,
                    Active = r.Active,
                }).ToList();
            }

            var trabajadores = await (
                from w in ctx.Worker
                where w.EmailCorporativo != null && w.EmailCorporativo.ToLower().Contains(EmailDomainCorp)
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.State == true
                orderby p.FullName
                select new CorreoWorkerOptionDto
                {
                    WorkerId = w.Id,
                    FullName = p.FullName,
                    Email = w.EmailCorporativo,
                }
            ).ToListAsync();

            var areas = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                where s.State && ai.State
                orderby s.DisplayOrder
                select new CorreoAreaOptionDto
                {
                    AreaScopeId = s.AreaScopeId,
                    Nombre = ai.AreaItemName,
                    ParentId = s.AreaScopeParentId,
                    Email = s.Email,
                }
            ).ToListAsync();

            return new CorreoConfigInicialDto
            {
                Eventos = eventos,
                Tipos = tipos,
                Trabajadores = trabajadores,
                Areas = areas,
            };
        }

        public async Task UpdateReglasAsync(string eventoCodigo, CorreoReglasUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var evento = await ctx.GaCorreoEvento
                .FirstOrDefaultAsync(e => e.Codigo == eventoCodigo && e.State);
            if (evento == null)
                throw new AbrilException("El correo indicado no existe.", 404);

            var tipoIdByCodigo = await ctx.GaCorreoTipoDestinatario
                .Where(t => t.State)
                .ToDictionaryAsync(t => t.Codigo.ToUpper(), t => t.Id);

            var now = DateTimeOffset.UtcNow;
            var nuevas = new List<GaCorreoRegla>();

            void Agregar(CorreoReglaInputDto input, bool esExclusion, int orden)
            {
                var tipoCodigo = (input.TipoCodigo ?? string.Empty).Trim().ToUpperInvariant();
                if (!tipoIdByCodigo.TryGetValue(tipoCodigo, out var tipoId))
                    throw new AbrilException($"Tipo de destinatario inválido: '{input.TipoCodigo}'.", 400);

                int? workerId = null;
                int? areaScopeId = null;
                string? correo = null;

                switch (tipoCodigo)
                {
                    case CorreoTipoCodigos.Trabajador:
                        if (input.WorkerId is null or <= 0)
                            throw new AbrilException("Falta seleccionar el trabajador en una fila.", 400);
                        workerId = input.WorkerId;
                        break;
                    case CorreoTipoCodigos.Area:
                        if (input.AreaScopeId is null or <= 0)
                            throw new AbrilException("Falta seleccionar el área en una fila.", 400);
                        areaScopeId = input.AreaScopeId;
                        break;
                    case CorreoTipoCodigos.Correo:
                        var c = (input.Correo ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(c))
                            throw new AbrilException("Falta escribir el correo en una fila.", 400);
                        if (!c.Contains('@') || c.Contains(' '))
                            throw new AbrilException($"Correo inválido: '{input.Correo}'.", 400);
                        correo = c;
                        break;
                }

                nuevas.Add(new GaCorreoRegla
                {
                    EventoId = evento.Id,
                    EsExclusion = esExclusion,
                    TipoId = tipoId,
                    WorkerId = workerId,
                    AreaScopeId = areaScopeId,
                    Correo = correo,
                    IncluirDescendientes = input.IncluirDescendientes,
                    Orden = orden,
                    Active = input.Active,
                    State = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            var incluir = dto.Incluir ?? new List<CorreoReglaInputDto>();
            var excluir = dto.Excluir ?? new List<CorreoReglaInputDto>();
            for (int i = 0; i < incluir.Count; i++) Agregar(incluir[i], false, i + 1);
            for (int i = 0; i < excluir.Count; i++) Agregar(excluir[i], true, i + 1);

            // Validar existencia de los trabajadores/áreas referenciados.
            var workerIds = nuevas.Where(n => n.WorkerId.HasValue).Select(n => n.WorkerId!.Value).Distinct().ToList();
            if (workerIds.Count > 0)
            {
                var existen = await ctx.Worker.Where(w => workerIds.Contains(w.Id)).Select(w => w.Id).ToListAsync();
                if (workerIds.Except(existen).Any())
                    throw new AbrilException("Uno o más trabajadores seleccionados no existen.", 400);
            }
            var areaIds = nuevas.Where(n => n.AreaScopeId.HasValue).Select(n => n.AreaScopeId!.Value).Distinct().ToList();
            if (areaIds.Count > 0)
            {
                var existen = await ctx.AreaScope
                    .Where(a => areaIds.Contains(a.AreaScopeId) && a.State)
                    .Select(a => a.AreaScopeId)
                    .ToListAsync();
                if (areaIds.Except(existen).Any())
                    throw new AbrilException("Una o más áreas seleccionadas no existen.", 400);
            }

            // Interruptores del correo. Solo se aceptan si el correo los permite, y solo se
            // rechaza cuando el valor realmente cambia: así la pantalla puede mandar siempre el
            // estado actual de los correos que no son apagables sin recibir un error.
            if (dto.Active.HasValue && dto.Active.Value != evento.Active)
            {
                if (!evento.PermiteDesactivarEnvio)
                    throw new AbrilException("Este correo no se puede desactivar.", 400);
                evento.Active = dto.Active.Value;
                evento.UpdatedAt = now;
            }
            if (dto.DestinatarioPrincipalActivo.HasValue
                && dto.DestinatarioPrincipalActivo.Value != evento.DestinatarioPrincipalActivo)
            {
                if (!evento.PermiteDesactivarPrincipal)
                    throw new AbrilException("El destinatario principal de este correo no se puede desactivar.", 400);
                evento.DestinatarioPrincipalActivo = dto.DestinatarioPrincipalActivo.Value;
                evento.UpdatedAt = now;
            }

            // Reemplazo completo: soft-delete de las reglas vivas del correo + insertar las nuevas.
            var vivas = await ctx.GaCorreoRegla.Where(r => r.EventoId == evento.Id && r.State).ToListAsync();
            foreach (var v in vivas)
            {
                v.State = false;
                v.UpdatedAt = now;
            }
            if (nuevas.Count > 0) ctx.GaCorreoRegla.AddRange(nuevas);

            await ctx.SaveChangesAsync();
        }
    }
}
