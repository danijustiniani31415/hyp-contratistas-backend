using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    /// <summary>Catálogo genérico de listas fijas (banco, tipo AFP/ONP, categoría laboral, ...) —
    /// ver CatalogoValor.cs. Todo editable desde el frontend, nada hardcodeado en el HTML.</summary>
    public class CatalogoValorService : ICatalogoValorService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CatalogoValorService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<CatalogoValorDto>> List(string tipo)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.CatalogoValor
                .Where(c => c.Tipo == tipo)
                .OrderBy(c => c.Orden).ThenBy(c => c.Valor)
                .Select(c => new CatalogoValorDto { Id = c.Id, Tipo = c.Tipo, Valor = c.Valor, Activo = c.Activo })
                .ToListAsync();
        }

        public async Task<CatalogoValorDto> Crear(CatalogoValorCreateDto dto)
        {
            var tipo = dto.Tipo.Trim().ToUpperInvariant();
            var valor = dto.Valor.Trim();
            if (string.IsNullOrWhiteSpace(tipo) || string.IsNullOrWhiteSpace(valor))
                throw new AbrilException("Tipo y valor son obligatorios.", 400);

            using var ctx = _factory.CreateDbContext();
            if (await ctx.CatalogoValor.AnyAsync(c => c.Tipo == tipo && c.Valor == valor))
                throw new AbrilException($"Ya existe el valor \"{valor}\" en {tipo}.", 409);

            var maxOrden = await ctx.CatalogoValor.Where(c => c.Tipo == tipo)
                .Select(c => (int?)c.Orden).MaxAsync() ?? 0;

            var entidad = new CatalogoValor { Tipo = tipo, Valor = valor, Orden = maxOrden + 1, Activo = true };
            ctx.CatalogoValor.Add(entidad);
            await ctx.SaveChangesAsync();
            return new CatalogoValorDto { Id = entidad.Id, Tipo = entidad.Tipo, Valor = entidad.Valor, Activo = entidad.Activo };
        }

        public async Task<CatalogoValorDto> Actualizar(int id, CatalogoValorUpdateDto dto)
        {
            var valor = dto.Valor.Trim();
            if (string.IsNullOrWhiteSpace(valor))
                throw new AbrilException("El valor es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            var entidad = await ctx.CatalogoValor.FindAsync(id)
                ?? throw new AbrilException("Valor de catálogo no encontrado.", 404);

            if (valor != entidad.Valor && await ctx.CatalogoValor.AnyAsync(c => c.Tipo == entidad.Tipo && c.Valor == valor && c.Id != id))
                throw new AbrilException($"Ya existe el valor \"{valor}\" en {entidad.Tipo}.", 409);

            entidad.Valor = valor;
            entidad.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new CatalogoValorDto { Id = entidad.Id, Tipo = entidad.Tipo, Valor = entidad.Valor, Activo = entidad.Activo };
        }
    }
}
