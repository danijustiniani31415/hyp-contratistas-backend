using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.CatalogoModule.Application.Dtos;
using Abril_Backend.Features.CatalogoModule.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.AlmacenModule.Application.Services
{
    public class AlmacenCargaMasivaService : IAlmacenCargaMasivaService
    {
        private const int MaxFilas = 500;

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAlmacenKardexService _kardex;
        private readonly ICatalogoService _catalogo;

        public AlmacenCargaMasivaService(IDbContextFactory<AppDbContext> factory, IAlmacenKardexService kardex, ICatalogoService catalogo)
        {
            _factory = factory;
            _kardex = kardex;
            _catalogo = catalogo;
        }

        public async Task<CargaMasivaResultadoDto> Cargar(CargaMasivaDto dto, long? usuarioSistemaId)
        {
            if (dto.Filas.Count == 0)
                throw new AbrilException("No hay filas para cargar.", 400);
            if (dto.Filas.Count > MaxFilas)
                throw new AbrilException($"Máximo {MaxFilas} filas por carga: divide el Excel en partes.", 400);

            using (var ctx = _factory.CreateDbContext())
            {
                if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                    throw new AbrilException("Almacén no encontrado.", 404);
            }

            var resultado = new CargaMasivaResultadoDto();
            // Dos filas del Excel con el mismo producto nuevo no deben crear dos productos iguales.
            var creadosEnEstaCarga = new Dictionary<string, long>();

            for (var i = 0; i < dto.Filas.Count; i++)
            {
                var fila = dto.Filas[i];
                var r = new CargaMasivaFilaResultadoDto { Fila = i + 1 };
                try
                {
                    if (fila.Cantidad <= 0)
                        throw new AbrilException("La cantidad debe ser mayor a cero.", 400);

                    long productoId;
                    if (fila.ProductoId.HasValue)
                    {
                        productoId = fila.ProductoId.Value;
                    }
                    else
                    {
                        var nuevo = fila.NuevoProducto ?? throw new AbrilException("La fila no indica un producto existente ni los datos de uno nuevo.", 400);
                        var nombre = nuevo.Nombre?.Trim();
                        if (string.IsNullOrWhiteSpace(nombre))
                            throw new AbrilException("El producto nuevo necesita un nombre.", 400);

                        var clave = nombre.ToLowerInvariant();
                        if (creadosEnEstaCarga.TryGetValue(clave, out var yaCreado))
                        {
                            productoId = yaCreado;
                        }
                        else
                        {
                            using var ctx = _factory.CreateDbContext();
                            var existente = await ctx.Producto.AsNoTracking()
                                .Where(p => p.Nombre.ToLower() == clave)
                                .Select(p => (long?)p.Id)
                                .FirstOrDefaultAsync();
                            if (existente.HasValue)
                            {
                                productoId = existente.Value;
                            }
                            else
                            {
                                var creado = await _catalogo.CrearProducto(new ProductoCreateDto
                                {
                                    Nombre = nombre,
                                    CategoriaId = nuevo.CategoriaId,
                                    UnidadMedida = string.IsNullOrWhiteSpace(nuevo.UnidadMedida) ? "UND" : nuevo.UnidadMedida.Trim().ToUpperInvariant(),
                                    RequiereTalla = nuevo.RequiereTalla,
                                    TipoTalla = nuevo.RequiereTalla ? nuevo.TipoTalla : null,
                                });
                                productoId = creado.Id;
                                r.ProductoCreado = true;
                                resultado.ProductosCreados++;
                            }
                            creadosEnEstaCarga[clave] = productoId;
                        }
                    }
                    r.ProductoId = productoId;

                    // La talla solo aplica a productos que la exigen; en los demás se ignora lo que traiga el Excel.
                    string talla;
                    using (var ctx = _factory.CreateDbContext())
                    {
                        var p = await ctx.Producto.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productoId)
                            ?? throw new AbrilException("Producto no encontrado.", 404);
                        talla = p.RequiereTalla ? (fila.Talla ?? "").Trim().ToUpperInvariant() : "";
                        if (p.RequiereTalla && talla.Length == 0)
                            throw new AbrilException($"«{p.Nombre}» requiere talla y la fila no la trae.", 400);
                    }

                    await _kardex.RegistrarMovimiento(new RegistrarMovimientoDto
                    {
                        AlmacenId = dto.AlmacenId,
                        ProductoId = productoId,
                        Talla = talla,
                        Color = (fila.Color ?? "").Trim(),
                        TipoMovimiento = "INGRESO",
                        Cantidad = fila.Cantidad,
                        CostoUnitario = fila.CostoUnitario,
                        ReferenciaTipo = "CARGA_EXCEL",
                    }, usuarioSistemaId);

                    r.Ok = true;
                    resultado.Registradas++;
                }
                catch (AbrilException ex)
                {
                    r.Mensaje = ex.Message;
                    resultado.Fallidas++;
                }
                catch (Exception)
                {
                    r.Mensaje = "Error inesperado al procesar esta fila.";
                    resultado.Fallidas++;
                }
                resultado.Filas.Add(r);
            }

            return resultado;
        }
    }
}
