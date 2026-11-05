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

            if (!string.IsNullOrWhiteSpace(dto.Nombre))
            {
                var nombre = dto.Nombre.Trim();
                if (await ctx.Almacen.AnyAsync(a => a.Id != id && a.Nombre.ToLower() == nombre.ToLower()))
                    throw new AbrilException("Ya existe un almacén con ese nombre.", 409);
                almacen.Nombre = nombre;
            }
            if (dto.Activo.HasValue) almacen.Activo = dto.Activo.Value;

            almacen.Direccion = direccion;
            almacen.Ubigeo = ubigeo;
            almacen.CodigoEstablecimientoSunat = codigo;
            await ctx.SaveChangesAsync();

            return ToDto(almacen);
        }

        public async Task<AlmacenGestionDto> Crear(AlmacenCrearDto dto)
        {
            var nombre = dto.Nombre?.Trim();
            var direccion = dto.Direccion?.Trim();
            var ubigeo = dto.Ubigeo?.Trim();
            var codigoSunat = dto.CodigoEstablecimientoSunat?.Trim();
            var tipo = (dto.Tipo ?? "").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(nombre))
                throw new AbrilException("El nombre del almacén es obligatorio.", 400);
            if (tipo != "CENTRAL" && tipo != "PROYECTO")
                throw new AbrilException("El tipo debe ser CENTRAL o PROYECTO.", 400);
            if (string.IsNullOrEmpty(direccion))
                throw new AbrilException("La dirección es obligatoria: SUNAT la usa como punto de partida o llegada de la guía.", 400);
            if (ubigeo == null || !Regex.IsMatch(ubigeo, @"^\d{6}$"))
                throw new AbrilException("El ubigeo debe tener 6 dígitos (catálogo INEI).", 400);
            if (codigoSunat == null || !Regex.IsMatch(codigoSunat, @"^\d{4}$"))
                throw new AbrilException("El código de establecimiento SUNAT debe tener 4 dígitos (0000 es el domicilio fiscal).", 400);

            using var ctx = _factory.CreateDbContext();
            if (await ctx.Almacen.AnyAsync(a => a.Nombre.ToLower() == nombre.ToLower()))
                throw new AbrilException("Ya existe un almacén con ese nombre.", 409);

            // Código interno automático a partir del nombre (ej. "Unidad Esperanza" → ALM-UNIDAD-ESPERANZA);
            // si ya existe se le agrega un número. El usuario no lo escribe ni lo ve en las guías.
            var baseCodigo = "ALM-" + Regex.Replace(SinTildes(nombre).ToUpperInvariant(), "[^A-Z0-9]+", "-").Trim('-');
            if (baseCodigo.Length > 40) baseCodigo = baseCodigo[..40].TrimEnd('-');
            var codigo = baseCodigo;
            for (var n = 2; await ctx.Almacen.AnyAsync(a => a.Codigo == codigo); n++)
                codigo = $"{baseCodigo}-{n}";

            var almacen = new Almacen
            {
                Codigo = codigo,
                Nombre = nombre,
                Tipo = tipo,
                Activo = true,
                Direccion = direccion,
                Ubigeo = ubigeo,
                CodigoEstablecimientoSunat = codigoSunat,
            };
            ctx.Almacen.Add(almacen);
            await ctx.SaveChangesAsync();
            return ToDto(almacen);
        }

        private static string SinTildes(string s)
        {
            var d = s.Normalize(System.Text.NormalizationForm.FormD);
            return new string(d.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray())
                .Normalize(System.Text.NormalizationForm.FormC);
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
