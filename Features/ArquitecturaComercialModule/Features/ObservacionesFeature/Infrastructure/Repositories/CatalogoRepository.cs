using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Repositories;

public class CatalogoRepository : ICatalogoRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public CatalogoRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<AcCatalogoItem>> GetByTipo(string tipo, bool soloActivos)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.AcCatalogoItems.Where(c => c.Tipo == tipo);
        if (soloActivos) query = query.Where(c => c.Activo);
        return await query.OrderBy(c => c.Orden).ThenBy(c => c.Nombre).ToListAsync();
    }

    public async Task<AcCatalogoItem> Create(string tipo, string nombre)
    {
        using var ctx = _factory.CreateDbContext();
        var maxOrden = await ctx.AcCatalogoItems.Where(c => c.Tipo == tipo).Select(c => (int?)c.Orden).MaxAsync() ?? 0;
        var item = new AcCatalogoItem { Tipo = tipo, Nombre = nombre, Orden = maxOrden + 1, Activo = true };
        ctx.AcCatalogoItems.Add(item);
        await ctx.SaveChangesAsync();
        return item;
    }

    public async Task<AcCatalogoItem?> Update(int id, string nombre, bool activo)
    {
        using var ctx = _factory.CreateDbContext();
        var item = await ctx.AcCatalogoItems.FindAsync(id);
        if (item == null) return null;
        item.Nombre = nombre;
        item.Activo = activo;
        await ctx.SaveChangesAsync();
        return item;
    }

    public async Task<bool> Delete(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var item = await ctx.AcCatalogoItems.FindAsync(id);
        if (item == null) return false;
        ctx.AcCatalogoItems.Remove(item);
        await ctx.SaveChangesAsync();
        return true;
    }
}
