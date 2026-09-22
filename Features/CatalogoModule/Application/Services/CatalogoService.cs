using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CatalogoModule.Application.Dtos;
using Abril_Backend.Features.CatalogoModule.Application.Interfaces;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.CatalogoModule.Application.Services
{
    public class CatalogoService : ICatalogoService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CatalogoService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<CategoriaDto>> ListCategorias()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.CategoriaProducto
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .Select(c => new CategoriaDto { Id = c.Id, Nombre = c.Nombre, Tipo = c.Tipo })
                .ToListAsync();
        }

        public async Task<CategoriaDto> CrearCategoria(CategoriaCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var categoria = new CategoriaProducto { Nombre = dto.Nombre, Tipo = dto.Tipo };
            ctx.CategoriaProducto.Add(categoria);
            await ctx.SaveChangesAsync();
            return new CategoriaDto { Id = categoria.Id, Nombre = categoria.Nombre, Tipo = categoria.Tipo };
        }

        public async Task<List<TallaDto>> ListTallas(string tipoTalla)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Talla
                .Where(t => t.TipoTalla == tipoTalla)
                .OrderBy(t => t.Orden)
                .Select(t => new TallaDto { Valor = t.Valor })
                .ToListAsync();
        }

        public async Task<ProductoListResponseDto> ListProductos(string? search, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Producto.Where(p => p.Activo).Include(p => p.Categoria).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Nombre.ToLower().Contains(s) ||
                    (p.Codigo != null && p.Codigo.ToLower().Contains(s)));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(p => p.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductoListItemDto
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    CategoriaNombre = p.Categoria!.Nombre,
                    CategoriaTipo = p.Categoria.Tipo,
                    UnidadMedida = p.UnidadMedida,
                    RequiereTalla = p.RequiereTalla,
                    TipoTalla = p.TipoTalla,
                    EsRetornable = p.EsRetornable,
                    Activo = p.Activo,
                })
                .ToListAsync();

            return new ProductoListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<ProductoDetailDto> GetProductoById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var producto = await ctx.Producto.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Producto no encontrado.", 404);
            return ToDetailDto(producto);
        }

        private static readonly string[] TiposTallaValidos = { "ROPA", "CALZADO", "GUANTES" };

        private static string? ValidarTipoTalla(bool requiereTalla, string? tipoTalla)
        {
            if (!requiereTalla) return null;
            if (string.IsNullOrWhiteSpace(tipoTalla) || !TiposTallaValidos.Contains(tipoTalla))
                throw new AbrilException("Si el producto requiere talla, debes indicar el tipo: ROPA, CALZADO o GUANTES.", 400);
            return tipoTalla;
        }

        public async Task<ProductoDetailDto> CrearProducto(ProductoCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var categoria = await ctx.CategoriaProducto.FindAsync(dto.CategoriaId)
                ?? throw new AbrilException("Categoría no encontrada.", 404);

            if (!string.IsNullOrWhiteSpace(dto.Codigo))
            {
                var codigoEnUso = await ctx.Producto.AnyAsync(p => p.Codigo == dto.Codigo);
                if (codigoEnUso)
                    throw new AbrilException($"Ya existe un producto con el código {dto.Codigo}.", 409);
            }

            var producto = new Producto
            {
                Codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CategoriaId = dto.CategoriaId,
                UnidadMedida = dto.UnidadMedida,
                RequiereTalla = dto.RequiereTalla,
                TipoTalla = ValidarTipoTalla(dto.RequiereTalla, dto.TipoTalla),
                EsRetornable = dto.EsRetornable,
                Activo = true,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.Producto.Add(producto);
            await ctx.SaveChangesAsync();

            producto.Categoria = categoria;
            return ToDetailDto(producto);
        }

        public async Task<ProductoDetailDto> ActualizarProducto(long id, ProductoUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var producto = await ctx.Producto.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Producto no encontrado.", 404);

            if (!await ctx.CategoriaProducto.AnyAsync(c => c.Id == dto.CategoriaId))
                throw new AbrilException("Categoría no encontrada.", 404);

            if (!string.IsNullOrWhiteSpace(dto.Codigo) && dto.Codigo != producto.Codigo)
            {
                var codigoEnUso = await ctx.Producto.AnyAsync(p => p.Codigo == dto.Codigo && p.Id != id);
                if (codigoEnUso)
                    throw new AbrilException($"Ya existe un producto con el código {dto.Codigo}.", 409);
            }

            producto.Codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo;
            producto.Nombre = dto.Nombre;
            producto.Descripcion = dto.Descripcion;
            producto.CategoriaId = dto.CategoriaId;
            producto.UnidadMedida = dto.UnidadMedida;
            producto.RequiereTalla = dto.RequiereTalla;
            producto.TipoTalla = ValidarTipoTalla(dto.RequiereTalla, dto.TipoTalla);
            producto.EsRetornable = dto.EsRetornable;
            producto.Activo = dto.Activo;
            await ctx.SaveChangesAsync();

            if (producto.Categoria == null || producto.Categoria.Id != dto.CategoriaId)
                producto.Categoria = await ctx.CategoriaProducto.FindAsync(dto.CategoriaId);

            return ToDetailDto(producto);
        }

        public async Task<List<SugerenciaProductoDto>> SugerirProductos(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return new List<SugerenciaProductoDto>();

            using var ctx = _factory.CreateDbContext();

            // [CONTEXT_LOGISTICA.md sección 6] similarity() de pg_trgm — sugiere productos
            // parecidos por nombre antes de crear uno nuevo, para no terminar con "Casco blanco"
            // y "Casco Blanco" como dos filas distintas.
            return await ctx.Database
                .SqlQuery<SugerenciaProductoDto>($"""
                    SELECT id AS "Id", nombre AS "Nombre", similarity(nombre, {nombre}) AS "Score"
                    FROM lb_producto
                    WHERE activo AND similarity(nombre, {nombre}) > 0.2
                    ORDER BY similarity(nombre, {nombre}) DESC
                    LIMIT 5
                    """)
                .ToListAsync();
        }

        private static ProductoDetailDto ToDetailDto(Producto p) => new()
        {
            Id = p.Id,
            Codigo = p.Codigo,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            CategoriaId = p.CategoriaId,
            CategoriaNombre = p.Categoria?.Nombre ?? "",
            UnidadMedida = p.UnidadMedida,
            RequiereTalla = p.RequiereTalla,
            TipoTalla = p.TipoTalla,
            EsRetornable = p.EsRetornable,
            Activo = p.Activo,
        };
    }
}
