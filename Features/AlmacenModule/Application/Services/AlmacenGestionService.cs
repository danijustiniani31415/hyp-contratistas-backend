using System.Text.RegularExpressions;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.AlmacenModule.Application.Services
{
    public class AlmacenGestionService : IAlmacenGestionService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AlmacenGestionService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<AlmacenGestionDto>> Listar()
        {
            using var ctx = _factory.CreateDbContext();
            var almacenes = await ctx.Almacen.AsNoTracking().OrderBy(a => a.Nombre).ToListAsync();
            return almacenes.Select(ToDto).ToList();
        }

        public async Task<AlmacenGestionDto> Actualizar(int id, AlmacenActualizarDto dto)
        {
            var direccion = dto.Direccion?.Trim();
            var ubigeo = dto.Ubigeo?.Trim();
            var codigo = dto.CodigoEstablecimientoSunat?.Trim();

            if (string.IsNullOrEmpty(direccion))
                throw new AbrilException("La dirección es obligatoria: SUNAT la usa como punto de partida o llegada de la guía.", 400);
            if (ubigeo == null || !Regex.IsMatch(ubigeo, @"^\d{6}$"))
                throw new AbrilException("El ubigeo debe tener 6 dígitos (catálogo INEI).", 400);
            if (codigo == null || !Regex.IsMatch(codigo, @"^\d{4}$"))
                throw new AbrilException("El código de establecimiento SUNAT debe tener 4 dígitos (0000 es el domicilio fiscal).", 400);

            using var ctx = _factory.CreateDbContext();
            var almacen = await ctx.Almacen.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new AbrilException("Almacén no encontrado.", 404);

            almacen.Direccion = direccion;
            almacen.Ubigeo = ubigeo;
            almacen.CodigoEstablecimientoSunat = codigo;
            await ctx.SaveChangesAsync();

            return ToDto(almacen);
        }

        private static AlmacenGestionDto ToDto(Almacen a) => new()
        {
            Id = a.Id,
            Codigo = a.Codigo,
            Nombre = a.Nombre,
            Tipo = a.Tipo,
            Activo = a.Activo,
            Direccion = a.Direccion,
            Ubigeo = a.Ubigeo,
            CodigoEstablecimientoSunat = a.CodigoEstablecimientoSunat,
        };
    }
}
