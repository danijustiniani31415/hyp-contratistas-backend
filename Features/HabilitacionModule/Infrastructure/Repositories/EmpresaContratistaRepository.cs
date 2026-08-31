using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class EmpresaContratistaRepository : IEmpresaContratistaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EmpresaContratistaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<EmpresaContratistaDetalleDto?> GetByIdAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var contributor = await ctx.Contributor
                .Where(c => c.ContributorId == id && !c.EsAbril)
                .FirstOrDefaultAsync();

            if (contributor is null) return null;

            var contractor = await ctx.Contractor
                .Where(c => c.ContributorId == id && c.Active)
                .FirstOrDefaultAsync();

            var emails = contractor is null
                ? new List<string>()
                : await ctx.ContractorEmail
                    .Where(ce => ce.ContractorId == contractor.ContractorId && ce.Active && ce.State)
                    .OrderBy(ce => ce.ContractorEmailId)
                    .Select(ce => ce.Email)
                    .ToListAsync();

            return MapToDetalle(contributor, contractor, emails);
        }

        public async Task<(List<EmpresaContratistaListDto> Items, int Total)> GetPagedAsync(
            string? search, bool? activo, bool? soloContratistas, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var filtrarContratistas = soloContratistas ?? true;
            var query = ctx.Contributor
                .Where(c => !filtrarContratistas || !c.EsAbril)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(c =>
                    c.ContributorName.ToLower().Contains(s) ||
                    (c.ContributorNombreComercial != null && c.ContributorNombreComercial.ToLower().Contains(s)) ||
                    c.ContributorRuc.Contains(s));
            }

            if (activo.HasValue)
                query = query.Where(c => c.Active == activo.Value);

            var total = await query.CountAsync();

            var contributors = await query
                .OrderBy(c => c.ContributorName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (contributors.Count == 0)
                return (new List<EmpresaContratistaListDto>(), total);

            var ids = contributors.Select(c => c.ContributorId).ToList();

            var contractors = await ctx.Contractor
                .Where(ct => ids.Contains(ct.ContributorId) && ct.Active)
                .ToListAsync();

            var contractorMap = contractors.ToDictionary(ct => ct.ContributorId);

            var contractorIds = contractors.Select(ct => ct.ContractorId).ToList();
            var emailRows = await ctx.ContractorEmail
                .Where(ce => contractorIds.Contains(ce.ContractorId) && ce.Active && ce.State)
                .OrderBy(ce => ce.ContractorEmailId)
                .Select(ce => new { ce.ContractorId, ce.Email })
                .ToListAsync();

            var emailsByContractor = emailRows
                .GroupBy(e => e.ContractorId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Email).ToList());

            var items = contributors.Select(c =>
            {
                contractorMap.TryGetValue(c.ContributorId, out var ct);
                var emails = ct is not null && emailsByContractor.TryGetValue(ct.ContractorId, out var el)
                    ? el
                    : new List<string>();

                return new EmpresaContratistaListDto
                {
                    Id = c.ContributorId,
                    RazonSocial = c.ContributorName,
                    NombreComercial = c.ContributorNombreComercial,
                    Ruc = c.ContributorRuc,
                    Tipo = "CONTRATISTA",
                    Activo = c.Active,
                    EmailAdmin = emails.ElementAtOrDefault(0),
                    EmailSsoma = emails.ElementAtOrDefault(1),
                    LogoUrl = ct?.LogoFileUrl,
                    Rubro = c.ContributorEconomicActivityDescription
                };
            }).ToList();

            return (items, total);
        }

        public async Task<bool> ExisteRucAsync(string ruc)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Contributor.AnyAsync(c => c.ContributorRuc == ruc && !c.EsAbril);
        }

        public async Task<int?> GetContributorIdByRucAsync(string ruc)
        {
            using var ctx = _factory.CreateDbContext();
            var id = await ctx.Contributor
                .Where(c => c.ContributorRuc == ruc)
                .Select(c => c.ContributorId)
                .FirstOrDefaultAsync();
            return id == 0 ? null : id;
        }

        public async Task<EmpresaContratistaListDto> CreateAsync(EmpresaContratistaCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var contributor = await ctx.Contributor
                .FirstOrDefaultAsync(c => c.ContributorRuc == dto.Ruc);

            if (contributor is null)
            {
                contributor = new Contributor
                {
                    ContributorRuc = dto.Ruc ?? string.Empty,
                    ContributorName = dto.RazonSocial,
                    ContributorNombreComercial = dto.NombreComercial,
                    ContributorAddress = dto.Direccion ?? string.Empty,
                    ContributorEconomicActivityDescription = dto.Rubro ?? string.Empty,
                    LegalEntityRegistryNumber = dto.PartidaRegistral,
                    EsAbril = false,
                    Active = dto.Activo,
                    State = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                };
                ctx.Contributor.Add(contributor);
                await ctx.SaveChangesAsync();
            }
            else
            {
                contributor.ContributorName = dto.RazonSocial;
                contributor.ContributorNombreComercial = dto.NombreComercial;
                contributor.Active = dto.Activo;
                contributor.UpdatedDateTime = DateTimeOffset.UtcNow;
            }

            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContributorId == contributor.ContributorId);

            if (contractor is null)
            {
                contractor = new Contractor
                {
                    ContributorId = contributor.ContributorId,
                    ContractorStateId = 2,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                };
                ctx.Contractor.Add(contractor);
                await ctx.SaveChangesAsync();
            }

            var newEmails = new[] { dto.EmailAdmin, dto.EmailSsoma, dto.EmailGerente, dto.EmailResidente }
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!.Trim().ToLower())
                .Distinct()
                .ToList();

            var existingEmails = await ctx.ContractorEmail
                .Where(ce => ce.ContractorId == contractor.ContractorId)
                .ToListAsync();

            var existingSet = existingEmails.Select(e => e.Email.Trim().ToLower()).ToHashSet();

            foreach (var email in newEmails.Where(e => !existingSet.Contains(e)))
            {
                ctx.ContractorEmail.Add(new ContractorEmail
                {
                    ContractorId = contractor.ContractorId,
                    Email = email,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                });
            }

            await ctx.SaveChangesAsync();

            return new EmpresaContratistaListDto
            {
                Id = contributor.ContributorId,
                RazonSocial = contributor.ContributorName,
                NombreComercial = contributor.ContributorNombreComercial,
                Ruc = contributor.ContributorRuc,
                Tipo = "CONTRATISTA",
                Activo = contributor.Active,
                EmailAdmin = newEmails.ElementAtOrDefault(0),
                EmailSsoma = newEmails.ElementAtOrDefault(1),
                LogoUrl = contractor.LogoFileUrl,
                Rubro = contributor.ContributorEconomicActivityDescription
            };
        }

        public async Task UpdateAsync(int id, EmpresaContratistaUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var contributor = await ctx.Contributor.FirstOrDefaultAsync(c => c.ContributorId == id)
                ?? throw new AbrilException("Empresa no encontrada.", 404);

            contributor.ContributorName = dto.RazonSocial;
            contributor.ContributorNombreComercial = dto.NombreComercial;
            contributor.ContributorAddress = dto.Direccion ?? string.Empty;
            contributor.ContributorEconomicActivityDescription = dto.Rubro ?? string.Empty;
            contributor.LegalEntityRegistryNumber = dto.PartidaRegistral;
            contributor.Active = dto.Activo;
            contributor.UpdatedDateTime = DateTimeOffset.UtcNow;

            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContributorId == id && c.Active);

            if (contractor is not null)
            {
                var newEmails = new[] { dto.EmailAdmin, dto.EmailSsoma, dto.EmailGerente, dto.EmailResidente }
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e!.Trim().ToLower())
                    .Distinct()
                    .ToList();

                var allEmails = await ctx.ContractorEmail
                    .Where(ce => ce.ContractorId == contractor.ContractorId)
                    .ToListAsync();

                var emailMap = allEmails.ToDictionary(e => e.Email.Trim().ToLower());
                var newSetLower = newEmails.ToHashSet();

                foreach (var e in allEmails.Where(e => e.Active && !newSetLower.Contains(e.Email.Trim().ToLower())))
                    e.Active = false;

                foreach (var email in newEmails)
                {
                    if (emailMap.TryGetValue(email, out var existing))
                    {
                        if (!existing.Active) existing.Active = true;
                    }
                    else
                    {
                        ctx.ContractorEmail.Add(new ContractorEmail
                        {
                            ContractorId = contractor.ContractorId,
                            Email = email,
                            Active = true,
                            State = true,
                            CreatedDateTime = DateTimeOffset.UtcNow
                        });
                    }
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task UpdatePasswordAsync(int id, string passwordHash)
        {
            using var ctx = _factory.CreateDbContext();

            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContributorId == id && c.Active)
                ?? throw new AbrilException("Empresa contratista no encontrada.", 404);

            var userId = await ctx.ContractorUser
                .Where(cu => cu.ContractorId == contractor.ContractorId && cu.Active && cu.State)
                .OrderBy(cu => cu.ContractorUserId)
                .Select(cu => (int?)cu.UserId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("No existe un usuario activado para esta empresa.", 404);

            var user = await ctx.User.FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            user.Password = passwordHash;
            user.UpdatedDateTime = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<List<SsEmpresaProyecto>> GetProyectosAsync(int empresaId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsEmpresaProyecto
                .Include(ep => ep.Proyecto)
                .Where(ep => ep.EmpresaId == empresaId)
                .ToListAsync();
        }

        public async Task AddProyectoAsync(SsEmpresaProyecto ep)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.SsEmpresaProyecto.Add(ep);
            await ctx.SaveChangesAsync();
        }

        public async Task RemoveProyectoAsync(int empresaId, int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            var ep = await ctx.SsEmpresaProyecto
                .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.ProyectoId == proyectoId);
            if (ep is null) return;
            ctx.SsEmpresaProyecto.Remove(ep);
            await ctx.SaveChangesAsync();
        }

        /// <summary>Proyecto de la vinculación laboral activa del usuario logueado (personal
        /// interno) — usado para preseleccionar el filtro de Proyecto en pantallas de admin,
        /// arrancando cada quien viendo su propio proyecto en vez de "todos".</summary>
        public async Task<int?> GetProyectoActualDeUsuarioAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.WorkerVinculacion
                .Where(v => v.FechaFin == null
                         && v.Worker != null && v.Worker.State
                         && v.Worker.Person != null && v.Worker.Person.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => v.ProyectoId)
                .FirstOrDefaultAsync();
        }

        private static EmpresaContratistaDetalleDto MapToDetalle(
            Contributor c, Contractor? ct, List<string> emails) => new()
        {
            Id = c.ContributorId,
            Ruc = c.ContributorRuc,
            RazonSocial = c.ContributorName,
            NombreComercial = c.ContributorNombreComercial,
            Rubro = c.ContributorEconomicActivityDescription,
            Direccion = c.ContributorAddress,
            EmailAdmin = emails.ElementAtOrDefault(0),
            EmailSsoma = emails.ElementAtOrDefault(1),
            EmailGerente = emails.ElementAtOrDefault(2),
            EmailResidente = emails.ElementAtOrDefault(3),
            LogoUrl = ct?.LogoFileUrl,
            PartidaRegistral = c.LegalEntityRegistryNumber,
            Tipo = "CONTRATISTA",
            Activo = c.Active,
            ActivoRetirado = null,
            ProyectoId = null,
            IdLegacy = null,
            CreatedAt = c.CreatedDateTime.DateTime,
            UpdatedAt = c.UpdatedDateTime?.DateTime
        };
    }
}
