using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class CatalogoMaterialesRepository : ICatalogoMaterialesRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public CatalogoMaterialesRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<SsMaterialTipo>> GetTiposAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsMaterialTipo.AsNoTracking().ToListAsync();
    }

    public async Task<List<SsMaterialFamilia>> GetFamiliasAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsMaterialFamilia.AsNoTracking().ToListAsync();
    }

    public async Task<List<FamiliaCatalogoDto>> ListarFamiliasDetalladoAsync(string? q, int? tipoId, bool? perteneceSsoma)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.SsMaterialFamilia.AsNoTracking().Include(f => f.Tipo).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(f => f.Nombre.ToLower().Contains(q.ToLower()));
        if (tipoId.HasValue)
            query = query.Where(f => f.TipoId == tipoId.Value);
        if (perteneceSsoma.HasValue)
            query = query.Where(f => f.PerteneceSsoma == perteneceSsoma.Value);

        return await query
            .OrderBy(f => f.Tipo.Nombre).ThenBy(f => f.Nombre)
            .Select(f => new FamiliaCatalogoDto
            {
                Id = f.Id,
                Nombre = f.Nombre,
                TipoId = f.TipoId,
                NombreTipo = f.Tipo.Nombre,
                VariableBase = f.VariableBase,
                UnidadMedida = f.UnidadMedida,
                PerteneceSsoma = f.PerteneceSsoma,
                Activo = f.Activo,
            })
            .ToListAsync();
    }

    public async Task ActualizarFamiliaAsync(int id, ActualizarFamiliaDto dto)
    {
        using var ctx = _factory.CreateDbContext();
        var familia = await ctx.SsMaterialFamilia.FindAsync(id);
        if (familia == null) throw new AbrilException("Familia no encontrada.", 404);

        familia.Nombre = dto.Nombre;
        familia.NombreNormalizado = Application.Services.TextoNormalizador.Normalizar(dto.Nombre);
        familia.TipoId = dto.TipoId;
        familia.VariableBase = dto.VariableBase;
        familia.UnidadMedida = dto.UnidadMedida;
        familia.PerteneceSsoma = dto.PerteneceSsoma;
        familia.Activo = dto.Activo;
        familia.ActualizadoEn = DateTimeOffset.UtcNow;

        await ctx.SaveChangesAsync();
    }

    public async Task<List<SsMaterialItem>> GetItemsAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsMaterialItem.AsNoTracking().ToListAsync();
    }

    public async Task<List<SsMaterialAlias>> GetAliasesAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsMaterialAlias.AsNoTracking().ToListAsync();
    }

    public async Task<SsMaterialTipo> GetOrCreateTipoAsync(string nombre)
    {
        using var ctx = _factory.CreateDbContext();
        var existente = await ctx.SsMaterialTipo.FirstOrDefaultAsync(t => t.Nombre == nombre);
        if (existente != null) return existente;

        var nuevo = new SsMaterialTipo { Nombre = nombre, Activo = true };
        ctx.SsMaterialTipo.Add(nuevo);
        await ctx.SaveChangesAsync();
        return nuevo;
    }

    public async Task<(SsMaterialFamilia Familia, bool Creada)> GetOrCreateFamiliaAsync(
        string nombre, string nombreNormalizado, int tipoId, string variableBase, bool perteneceSsoma)
    {
        using var ctx = _factory.CreateDbContext();
        var existente = await ctx.SsMaterialFamilia
            .FirstOrDefaultAsync(f => f.NombreNormalizado == nombreNormalizado);
        if (existente != null) return (existente, false);

        var nueva = new SsMaterialFamilia
        {
            Nombre = nombre,
            NombreNormalizado = nombreNormalizado,
            TipoId = tipoId,
            VariableBase = variableBase,
            PerteneceSsoma = perteneceSsoma,
            Activo = true,
            CreadoEn = DateTimeOffset.UtcNow
        };
        ctx.SsMaterialFamilia.Add(nueva);
        await ctx.SaveChangesAsync();
        return (nueva, true);
    }

    public async Task<(SsMaterialItem Item, bool Creado)> GetOrCreateItemAsync(
        string nombre, string nombreNormalizado, int familiaId, string? talla, string? dimensionNorm, bool noUsar)
    {
        using var ctx = _factory.CreateDbContext();
        var existente = await ctx.SsMaterialItem
            .FirstOrDefaultAsync(i => i.NombreNormalizado == nombreNormalizado && i.FamiliaId == familiaId);
        if (existente != null) return (existente, false);

        var nuevo = new SsMaterialItem
        {
            Nombre = nombre,
            NombreNormalizado = nombreNormalizado,
            FamiliaId = familiaId,
            Talla = talla,
            DimensionNorm = dimensionNorm,
            NoUsar = noUsar,
            Activo = !noUsar,
            CreadoEn = DateTimeOffset.UtcNow
        };
        ctx.SsMaterialItem.Add(nuevo);
        await ctx.SaveChangesAsync();
        return (nuevo, true);
    }

    public async Task<(SsMaterialItem Item, string NombreFamilia, string NombreTipo, bool PerteneceSsoma)> CrearItemManualAsync(
        string nombre, string nombreNormalizado, int familiaId)
    {
        using var ctx = _factory.CreateDbContext();
        var familia = await ctx.SsMaterialFamilia.Include(f => f.Tipo).FirstOrDefaultAsync(f => f.Id == familiaId)
            ?? throw new AbrilException("Familia no encontrada.", 404);

        var existente = await ctx.SsMaterialItem
            .FirstOrDefaultAsync(i => i.NombreNormalizado == nombreNormalizado && i.FamiliaId == familiaId);
        if (existente != null)
        {
            // El usuario pidió "crear" porque no lo encontraba: si ya existía pero estaba
            // marcado no_usar/inactivo (por eso no aparecía en ninguna búsqueda), reactivarlo
            // es lo correcto — devolverlo oculto tal cual repetiría el mismo "no lo encuentro".
            if (existente.NoUsar || !existente.Activo)
            {
                existente.NoUsar = false;
                existente.Activo = true;
                await ctx.SaveChangesAsync();
            }
            return (existente, familia.Nombre, familia.Tipo.Nombre, familia.PerteneceSsoma);
        }

        var nuevo = new SsMaterialItem
        {
            Nombre = nombre,
            NombreNormalizado = nombreNormalizado,
            FamiliaId = familiaId,
            Activo = true,
            CreadoEn = DateTimeOffset.UtcNow
        };
        ctx.SsMaterialItem.Add(nuevo);
        await ctx.SaveChangesAsync();
        return (nuevo, familia.Nombre, familia.Tipo.Nombre, familia.PerteneceSsoma);
    }

    public async Task<FamiliaCatalogoDto> CrearFamiliaManualAsync(
        string nombre, string nombreNormalizado, int tipoId, string variableBase, string? unidadMedida, bool perteneceSsoma)
    {
        using var ctx = _factory.CreateDbContext();
        var tipo = await ctx.SsMaterialTipo.FindAsync(tipoId)
            ?? throw new AbrilException("Tipo no encontrado.", 404);

        var existente = await ctx.SsMaterialFamilia.FirstOrDefaultAsync(f => f.NombreNormalizado == nombreNormalizado);
        if (existente != null)
        {
            return new FamiliaCatalogoDto
            {
                Id = existente.Id, Nombre = existente.Nombre, TipoId = existente.TipoId, NombreTipo = tipo.Nombre,
                VariableBase = existente.VariableBase, UnidadMedida = existente.UnidadMedida,
                PerteneceSsoma = existente.PerteneceSsoma, Activo = existente.Activo,
            };
        }

        var nueva = new SsMaterialFamilia
        {
            Nombre = nombre,
            NombreNormalizado = nombreNormalizado,
            TipoId = tipoId,
            VariableBase = variableBase,
            UnidadMedida = unidadMedida,
            PerteneceSsoma = perteneceSsoma,
            Activo = true,
            CreadoEn = DateTimeOffset.UtcNow
        };
        ctx.SsMaterialFamilia.Add(nueva);
        await ctx.SaveChangesAsync();

        return new FamiliaCatalogoDto
        {
            Id = nueva.Id, Nombre = nueva.Nombre, TipoId = nueva.TipoId, NombreTipo = tipo.Nombre,
            VariableBase = nueva.VariableBase, UnidadMedida = nueva.UnidadMedida,
            PerteneceSsoma = nueva.PerteneceSsoma, Activo = nueva.Activo,
        };
    }

    public async Task<bool> CreateAliasIfNotExistsAsync(
        string textoCrudo, string textoCrudoNorm, int itemId, string origen, decimal? confianza,
        decimal factorConversion = 1)
    {
        using var ctx = _factory.CreateDbContext();
        var existente = await ctx.SsMaterialAlias.FirstOrDefaultAsync(a => a.TextoCrudoNorm == textoCrudoNorm);
        if (existente != null)
        {
            if (existente.FactorConversion != factorConversion)
            {
                existente.FactorConversion = factorConversion;
                await ctx.SaveChangesAsync();
            }
            return false;
        }

        ctx.SsMaterialAlias.Add(new SsMaterialAlias
        {
            TextoCrudo = textoCrudo,
            TextoCrudoNorm = textoCrudoNorm,
            ItemId = itemId,
            Origen = origen,
            Confianza = confianza,
            FactorConversion = factorConversion,
            CreadoEn = DateTimeOffset.UtcNow
        });
        await ctx.SaveChangesAsync();
        return true;
    }
}
