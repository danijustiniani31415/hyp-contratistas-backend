using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.Shared.Dtos;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Shared.Application.Services;

public interface ISharedFiltersService
{
    Task<IEnumerable<GenericSelectOptionDto>> GetProyectosAsync();
    Task<IEnumerable<GenericSelectOptionDto>> GetRazonesSocialesAsync();
    IEnumerable<GenericSelectOptionDto> GetMeses();
    IEnumerable<GenericSelectOptionDto> GetAnios();
    IEnumerable<GenericSelectOptionDto> GetEstados();
}

public class SharedFiltersService : ISharedFiltersService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SharedFiltersService(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<IEnumerable<GenericSelectOptionDto>> GetProyectosAsync()
    {
        using var ctx = await _contextFactory.CreateDbContextAsync();

        var projects = await ctx.Project
            .AsNoTracking()
            .Where(p => p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.SharedFilters && !f.Active))
            .ToListAsync();

        return projects
            .Select(p => new GenericSelectOptionDto(p.ProjectId, p.ProjectDescription ?? "Sin descripción"))
            .OrderBy(p => p.Nombre);
    }

    public async Task<IEnumerable<GenericSelectOptionDto>> GetRazonesSocialesAsync()
    {
        using var ctx = await _contextFactory.CreateDbContextAsync();

        var contributors = await ctx.Contributor
            .AsNoTracking()
            .Where(c => c.State)
            .ToListAsync();

        return contributors
            .Select(c => new GenericSelectOptionDto(c.ContributorId, c.ContributorNombreComercial ?? c.ContributorName))
            .OrderBy(c => c.Nombre);
    }

    public IEnumerable<GenericSelectOptionDto> GetMeses() =>
    [
        new(1, "Enero"),
        new(2, "Febrero"),
        new(3, "Marzo"),
        new(4, "Abril"),
        new(5, "Mayo"),
        new(6, "Junio"),
        new(7, "Julio"),
        new(8, "Agosto"),
        new(9, "Septiembre"),
        new(10, "Octubre"),
        new(11, "Noviembre"),
        new(12, "Diciembre"),
    ];

    public IEnumerable<GenericSelectOptionDto> GetAnios() =>
        Enumerable.Range(DateTime.Now.Year - 5, 10)
            .OrderByDescending(y => y)
            .Select(y => new GenericSelectOptionDto(y, y.ToString()));

    public IEnumerable<GenericSelectOptionDto> GetEstados() =>
    [
        new(1, "Pendiente"),
        new(2, "Enviado"),
        new(3, "Aprobado"),
        new(4, "Rechazado"),
        new(5, "En revisión"),
    ];
}
