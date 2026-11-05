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
                    Marca = p.Marca,
                    CategoriaNombre = p.Categoria!.Nombre,
                    CategoriaTipo = p.Categoria.Tipo,
                    UnidadMedida = p.UnidadMedida,
                    RequiereTalla = p.RequiereTalla,
                    TipoTalla = p.TipoTalla,
                    RequiereColor = p.RequiereColor,
                    EsRetornable = p.EsRetornable,
                    Presentacion = p.Presentacion,
                    UnidadesPorPresentacion = p.UnidadesPorPresentacion,
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
            var detalle = ToDetailDto(producto);
            detalle.Alias = await ctx.ProductoAlias.AsNoTracking()
                .Where(a => a.ProductoId == id).OrderBy(a => a.Alias).Select(a => a.Alias).ToListAsync();
            return detalle;
        }

        private static readonly string[] TiposTallaValidos = { "ROPA", "CALZADO", "GUANTES" };

        // ── Alias y limpieza de duplicados ──────────────────────────────────

        /// <summary>Escritura sin tildes, espacios ni signos: «Arco-Sierra» y «arco sierra» dan la misma clave.</summary>
        public static string ClaveAlias(string texto)
        {
            var d = (texto ?? "").Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in d)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>Todo lo que referencia a un producto: si aparece acá, desactivarlo afectaría historial.</summary>
        private const string ReferenciasSql =
            "SELECT producto_id FROM lb_stock UNION ALL SELECT producto_id FROM lb_movimiento " +
            "UNION ALL SELECT producto_id FROM lb_pedido_item UNION ALL SELECT producto_id FROM lb_orden_compra_item " +
            "UNION ALL SELECT producto_id FROM lb_guia_remision_item UNION ALL SELECT producto_id FROM lb_entrega_epp_item " +
            "UNION ALL SELECT producto_id FROM lb_prestamo_item";

        public async Task<List<ProductoAliasDto>> ListAlias()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.ProductoAlias.AsNoTracking().OrderBy(a => a.ProductoId)
                .Select(a => new ProductoAliasDto { ProductoId = a.ProductoId, Alias = a.Alias }).ToListAsync();
        }

        public async Task<List<long>> ListProductosConUso()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Database
                .SqlQueryRaw<long>("SELECT DISTINCT producto_id AS \"Value\" FROM (" + ReferenciasSql + ") x WHERE producto_id IS NOT NULL")
                .ToListAsync();
        }

        public async Task AgregarAlias(long productoId, string alias)
        {
            var texto = (alias ?? "").Trim();
            var clave = ClaveAlias(texto);
            if (clave.Length < 3) return;

            using var ctx = _factory.CreateDbContext();
            var producto = await ctx.Producto.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productoId)
                ?? throw new AbrilException("Producto no encontrado.", 404);
            if (ClaveAlias(producto.Nombre) == clave) return;
            if (await ctx.ProductoAlias.AnyAsync(a => a.AliasNorm == clave)) return;

            ctx.ProductoAlias.Add(new ProductoAlias { ProductoId = productoId, Alias = texto, AliasNorm = clave, CreadoEn = DateTimeOffset.UtcNow });
            await ctx.SaveChangesAsync();
        }

        public async Task<ResolverDuplicadosResultDto> ResolverDuplicados(ResolverDuplicadosDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var principal = await ctx.Producto.FirstOrDefaultAsync(p => p.Id == dto.PrincipalId && p.Activo)
                ?? throw new AbrilException("El producto principal no existe o está inactivo.", 404);

            var resultado = new ResolverDuplicadosResultDto();
            foreach (var id in dto.DuplicadoIds.Distinct().Where(i => i != dto.PrincipalId))
            {
                var dup = await ctx.Producto.FirstOrDefaultAsync(p => p.Id == id);
                if (dup == null) { resultado.Omitidos.Add($"El producto {id} no existe."); continue; }

                // Se vuelve a comprobar acá: no se confía en lo que diga la pantalla.
                var usos = await ctx.Database
                    .SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM (" + ReferenciasSql + ") x WHERE producto_id = {0}", id)
                    .SingleAsync();
                if (usos > 0)
                {
                    resultado.Omitidos.Add($"«{dup.Nombre}» tiene stock, movimientos o documentos: no se desactiva (hay que resolverlo a mano).");
                    continue;
                }

                // Su nombre y sus alias pasan al principal, así «ARCO SIERRA» se reconoce solo en próximas cargas.
                var clave = ClaveAlias(dup.Nombre);
                foreach (var a in await ctx.ProductoAlias.Where(a => a.ProductoId == id).ToListAsync()) a.ProductoId = principal.Id;
                if (clave != ClaveAlias(principal.Nombre) && !await ctx.ProductoAlias.AnyAsync(a => a.AliasNorm == clave))
                    ctx.ProductoAlias.Add(new ProductoAlias { ProductoId = principal.Id, Alias = dup.Nombre, AliasNorm = clave, CreadoEn = DateTimeOffset.UtcNow });

                dup.Activo = false;
                await ctx.SaveChangesAsync();
                resultado.Desactivados++;
            }
            return resultado;
        }

        private static string? LimpiarMarca(string? marca) => string.IsNullOrWhiteSpace(marca) ? null : marca.Trim();

        /// <summary>
        /// Nombre completo del producto: si tiene marca y el nombre todavía no la menciona (como palabra completa),
        /// se agrega al final («Flexómetro 5m» + «Truper» → «Flexómetro 5m Truper»). Así todos los selectores del
        /// sistema distinguen una marca de otra sin tocar cada pantalla.
        /// </summary>
        /// <summary>Presentación y factor van juntos: sin factor mayor que 1 no hay conversión que guardar.</summary>
        private static string? LimpiarPresentacion(string? presentacion, decimal? factor, out decimal? factorLimpio)
        {
            var p = presentacion?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(p) || factor is null || factor <= 1) { factorLimpio = null; return null; }
            factorLimpio = factor;
            return p;
        }

        public static string ComponerNombre(string nombre, string? marca)
        {
            nombre = (nombre ?? "").Trim();
            marca = LimpiarMarca(marca);
            if (marca == null) return nombre;
            var yaLaTiene = System.Text.RegularExpressions.Regex.IsMatch(
                nombre, $@"(?<![\p{{L}}\p{{N}}]){System.Text.RegularExpressions.Regex.Escape(marca)}(?![\p{{L}}\p{{N}}])",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return yaLaTiene ? nombre : $"{nombre} {marca}";
        }

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
                Nombre = ComponerNombre(dto.Nombre, dto.Marca),
                Marca = LimpiarMarca(dto.Marca),
                Descripcion = dto.Descripcion,
                CategoriaId = dto.CategoriaId,
                UnidadMedida = dto.UnidadMedida,
                RequiereTalla = dto.RequiereTalla,
                TipoTalla = ValidarTipoTalla(dto.RequiereTalla, dto.TipoTalla),
                RequiereColor = dto.RequiereColor,
                EsRetornable = dto.EsRetornable,
                Presentacion = LimpiarPresentacion(dto.Presentacion, dto.UnidadesPorPresentacion, out var factor),
                UnidadesPorPresentacion = factor,
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
            producto.Nombre = ComponerNombre(dto.Nombre, dto.Marca);
            producto.Marca = LimpiarMarca(dto.Marca);
            producto.Descripcion = dto.Descripcion;
            producto.CategoriaId = dto.CategoriaId;
            producto.UnidadMedida = dto.UnidadMedida;
            producto.RequiereTalla = dto.RequiereTalla;
            producto.TipoTalla = ValidarTipoTalla(dto.RequiereTalla, dto.TipoTalla);
            producto.RequiereColor = dto.RequiereColor;
            producto.EsRetornable = dto.EsRetornable;
            producto.Presentacion = LimpiarPresentacion(dto.Presentacion, dto.UnidadesPorPresentacion, out var factorPres);
            producto.UnidadesPorPresentacion = factorPres;
            producto.Activo = dto.Activo;

            if (dto.Alias != null)
            {
                var nuevos = dto.Alias.Select(a => (a ?? "").Trim()).Where(a => a.Length > 0)
                    .GroupBy(ClaveAlias).Select(g => g.First())
                    .Where(a => ClaveAlias(a).Length >= 3 && ClaveAlias(a) != ClaveAlias(producto.Nombre)).ToList();
                var existentes = await ctx.ProductoAlias.Where(a => a.ProductoId == id).ToListAsync();
                var clavesNuevas = nuevos.Select(ClaveAlias).ToHashSet();
                ctx.ProductoAlias.RemoveRange(existentes.Where(a => !clavesNuevas.Contains(a.AliasNorm)));
                foreach (var texto in nuevos)
                {
                    var clave = ClaveAlias(texto);
                    if (existentes.Any(a => a.AliasNorm == clave)) continue;
                    if (await ctx.ProductoAlias.AnyAsync(a => a.AliasNorm == clave && a.ProductoId != id))
                        throw new AbrilException($"El alias «{texto}» ya pertenece a otro producto.", 409);
                    ctx.ProductoAlias.Add(new ProductoAlias { ProductoId = id, Alias = texto, AliasNorm = clave, CreadoEn = DateTimeOffset.UtcNow });
                }
            }
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
            Marca = p.Marca,
            Descripcion = p.Descripcion,
            CategoriaId = p.CategoriaId,
            CategoriaNombre = p.Categoria?.Nombre ?? "",
            UnidadMedida = p.UnidadMedida,
            RequiereTalla = p.RequiereTalla,
            TipoTalla = p.TipoTalla,
            RequiereColor = p.RequiereColor,
            EsRetornable = p.EsRetornable,
            Presentacion = p.Presentacion,
            UnidadesPorPresentacion = p.UnidadesPorPresentacion,
            Activo = p.Activo,
        };
    }
}
