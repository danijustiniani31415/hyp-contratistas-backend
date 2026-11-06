using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.ComprasModule.Application.Services
{
    public class ComprobanteCompraService : IComprobanteCompraService
    {
        private const long TamanoMaximo = 15 * 1024 * 1024;
        private static readonly Dictionary<string, string> Extensiones = new()
        {
            ["application/pdf"] = ".pdf",
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/heic"] = ".heic",
        };
        private static readonly HashSet<string> Tipos = new()
        {
            "FACTURA", "BOLETA", "NOTA_CREDITO", "NOTA_DEBITO", "RECIBO_HONORARIOS", "TICKET",
            "GUIA_REMISION", "INVOICE_EXTRANJERO", "OTRO",
        };
        private static readonly HashSet<string> Destinos = new() { "STOCK", "GASTO", "ACTIVO", "RENDICION" };
        /// <summary>Documentos que no son una compra de bienes: se registran pero nunca mueven el kardex.</summary>
        private static readonly HashSet<string> SinStock = new() { "NOTA_CREDITO", "NOTA_DEBITO", "GUIA_REMISION" };

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IComprobanteLectorService _lector;
        private readonly IAlmacenKardexService _almacen;

        public ComprobanteCompraService(
            IDbContextFactory<AppDbContext> factory,
            IComprobanteLectorService lector,
            IAlmacenKardexService almacen)
        {
            _factory = factory;
            _lector = lector;
            _almacen = almacen;
        }

        private const string RutaArchivos = "/api/v1/compras/comprobantes/archivos/";

        public async Task<(byte[] Contenido, string Mime, string Nombre)> GetArchivo(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var a = await ctx.ComprobanteCompraArchivo.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AbrilException("Archivo no encontrado.", 404);
            return (a.Contenido, a.Mime, a.Nombre);
        }

        public async Task<ComprobanteLecturaResultDto> Leer(Stream archivo, string nombreArchivo, string mimeType, long usuarioId)
        {
            mimeType = (mimeType ?? "").ToLowerInvariant();
            if (!Extensiones.TryGetValue(mimeType, out var extension))
                throw new AbrilException("Formato no soportado. Sube un PDF o una foto (JPG, PNG, WEBP).", 400);

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            if (ms.Length == 0) throw new AbrilException("El archivo está vacío.", 400);
            if (ms.Length > TamanoMaximo) throw new AbrilException("El archivo pesa más de 15 MB.", 400);
            var bytes = ms.ToArray();

            // Fotos: directo al modelo preciso (más errores de lectura). PDFs: primero el barato, y se escala si algo no cuadra.
            var (rapido, preciso) = _lector.Modelos;
            var esFoto = mimeType.StartsWith("image/");
            var modelo = esFoto ? preciso : rapido;
            var lectura = await _lector.Leer(bytes, mimeType, modelo);
            var avisos = ComprobanteValidador.Validar(lectura);

            if (!esFoto && preciso != rapido && ComprobanteValidador.NecesitaSegundaLectura(lectura, avisos))
            {
                try
                {
                    var segunda = await _lector.Leer(bytes, mimeType, preciso);
                    lectura = segunda;
                    avisos = ComprobanteValidador.Validar(lectura);
                    modelo = preciso;
                }
                catch (AbrilException)
                {
                    // Si el modelo preciso falla se queda con la primera lectura, que ya trae sus avisos.
                }
            }

            using var ctx = _factory.CreateDbContext();

            // La copia del archivo se guarda en la base; si algo falla el comprobante se puede registrar igual.
            string? url = null;
            try
            {
                // Lo leído que nunca se confirmó (sin comprobante) se limpia al día siguiente.
                var limite = DateTimeOffset.UtcNow.AddDays(-1);
                await ctx.ComprobanteCompraArchivo.Where(a => a.ComprobanteCompraId == null && a.CreadoEn < limite).ExecuteDeleteAsync();

                var archivoGuardado = new ComprobanteCompraArchivo
                {
                    Nombre = Path.GetFileName(nombreArchivo ?? "comprobante" + extension) is { Length: > 0 } n
                        ? (n.Length > 200 ? n[^200..] : n)
                        : "comprobante" + extension,
                    Mime = mimeType,
                    Contenido = bytes,
                    CreadoEn = DateTimeOffset.UtcNow,
                };
                ctx.ComprobanteCompraArchivo.Add(archivoGuardado);
                await ctx.SaveChangesAsync();
                url = RutaArchivos + archivoGuardado.Id;
            }
            catch (Exception)
            {
                avisos.Add(new ComprobanteAdvertenciaDto { Nivel = "AVISO", Mensaje = "No se pudo guardar la copia del archivo; se puede registrar igual." });
            }

            int? proveedorId = null;
            long? duplicado = null;
            if (!string.IsNullOrEmpty(lectura.EmisorRuc))
            {
                proveedorId = await ctx.Proveedor.Where(p => p.Ruc == lectura.EmisorRuc).Select(p => (int?)p.Id).FirstOrDefaultAsync();
                if (proveedorId != null && !string.IsNullOrEmpty(lectura.Serie) && !string.IsNullOrEmpty(lectura.Numero))
                {
                    duplicado = await ctx.ComprobanteCompra
                        .Where(c => c.ProveedorId == proveedorId && c.Tipo == lectura.Tipo && c.Serie == lectura.Serie && c.Numero == lectura.Numero)
                        .Select(c => (long?)c.Id).FirstOrDefaultAsync();
                    if (duplicado != null)
                        avisos.Insert(0, new ComprobanteAdvertenciaDto { Nivel = "ERROR", Mensaje = $"Este comprobante ya fue cargado ({lectura.Serie}-{lectura.Numero})." });
                }
            }
            else if (lectura.Tipo is "FACTURA" or "BOLETA")
            {
                avisos.Add(new ComprobanteAdvertenciaDto { Nivel = "AVISO", Mensaje = "No se leyó el RUC del proveedor." });
            }

            if (proveedorId == null && !string.IsNullOrEmpty(lectura.EmisorRuc))
                avisos.Add(new ComprobanteAdvertenciaDto { Nivel = "INFO", Mensaje = "El proveedor es nuevo: se registrará al confirmar." });

            return new ComprobanteLecturaResultDto
            {
                Lectura = lectura,
                Advertencias = avisos,
                ModeloUsado = modelo,
                ArchivoUrl = url,
                ProveedorId = proveedorId,
                DuplicadoDeId = duplicado,
            };
        }

        public async Task<ComprobanteGuardadoDto> Guardar(ComprobanteGuardarDto dto, long usuarioId, LbScopeProyectos scope)
        {
            dto.Tipo = (dto.Tipo ?? "").Trim().ToUpperInvariant();
            dto.Serie = (dto.Serie ?? "").Trim().ToUpperInvariant();
            dto.Numero = (dto.Numero ?? "").Trim().TrimStart('0');
            dto.Moneda = dto.Moneda?.ToUpperInvariant() == "USD" ? "USD" : "PEN";

            if (!Tipos.Contains(dto.Tipo)) throw new AbrilException("Tipo de comprobante no válido.", 400);
            if (dto.Serie.Length == 0 || dto.Numero.Length == 0) throw new AbrilException("Falta la serie o el número del comprobante.", 400);
            if (dto.Items.Count == 0) throw new AbrilException("El comprobante no tiene líneas.", 400);
            if (dto.Moneda == "USD" && (dto.TipoCambio ?? 0) <= 0)
                throw new AbrilException("El comprobante está en dólares: indica el tipo de cambio.", 400);

            var avisos = new List<string>();
            foreach (var it in dto.Items)
            {
                it.Destino = (it.Destino ?? "GASTO").Trim().ToUpperInvariant();
                if (!Destinos.Contains(it.Destino)) throw new AbrilException($"Destino no válido en «{it.Descripcion}».", 400);
                if (it.Cantidad <= 0) throw new AbrilException($"Cantidad inválida en «{it.Descripcion}».", 400);
                if (it.Destino == "STOCK" && SinStock.Contains(dto.Tipo))
                {
                    it.Destino = "GASTO";
                    if (!avisos.Contains("Las notas y guías no mueven stock: sus líneas se registraron sin ingreso al almacén."))
                        avisos.Add("Las notas y guías no mueven stock: sus líneas se registraron sin ingreso al almacén.");
                }
                if (it.Destino == "STOCK" && it.ProductoId == null)
                    throw new AbrilException($"Elige el producto del catálogo para «{it.Descripcion}» (o cámbialo a gasto).", 400);
            }
            var lineasStock = dto.Items.Where(i => i.Destino == "STOCK").ToList();
            if (lineasStock.Count > 0 && dto.AlmacenId == null)
                throw new AbrilException("Elige el almacén al que ingresa la mercadería.", 400);

            using var ctx = _factory.CreateDbContext();

            if (dto.AlmacenId != null)
            {
                var alm = await ctx.Almacen.FirstOrDefaultAsync(a => a.Id == dto.AlmacenId)
                    ?? throw new AbrilException("Almacén no encontrado.", 404);
                if (lineasStock.Count > 0 && alm.ProyectoId.HasValue && !scope.Permite(alm.ProyectoId.Value))
                    throw new AbrilException("No tienes permiso para ingresar mercadería a este almacén.", 403);
            }

            // Proveedor: el elegido, el que ya existe con ese RUC, o uno nuevo.
            var proveedorId = dto.ProveedorId;
            if (proveedorId == null)
            {
                var ruc = (dto.ProveedorRuc ?? "").Trim();
                if (ruc.Length > 0)
                    proveedorId = await ctx.Proveedor.Where(p => p.Ruc == ruc).Select(p => (int?)p.Id).FirstOrDefaultAsync();
                if (proveedorId == null)
                {
                    if (string.IsNullOrWhiteSpace(dto.ProveedorRazonSocial))
                        throw new AbrilException("Falta la razón social del proveedor.", 400);
                    if (ruc.Length > 0 && !ComprobanteValidador.RucValido(ruc))
                        throw new AbrilException("El RUC del proveedor no es válido.", 400);
                    var nuevo = new Proveedor { RazonSocial = dto.ProveedorRazonSocial.Trim(), Ruc = ruc.Length > 0 ? ruc : null, Activo = true };
                    ctx.Proveedor.Add(nuevo);
                    await ctx.SaveChangesAsync();
                    proveedorId = nuevo.Id;
                }
            }

            // Mismo comprobante: si quedó a medias (ingresos sin aplicar) se completa; si ya está listo, es un duplicado.
            var existente = await ctx.ComprobanteCompra.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.ProveedorId == proveedorId && c.Tipo == dto.Tipo && c.Serie == dto.Serie && c.Numero == dto.Numero);
            if (existente != null && existente.Estado != "PENDIENTE_STOCK")
                throw new AbrilException($"El comprobante {dto.Serie}-{dto.Numero} ya está registrado.", 409);

            var comprobante = existente;
            if (comprobante == null)
            {
                comprobante = new ComprobanteCompra
                {
                    ProveedorId = proveedorId.Value,
                    AlmacenId = dto.AlmacenId,
                    Tipo = dto.Tipo,
                    Serie = dto.Serie,
                    Numero = dto.Numero,
                    FechaEmision = dto.FechaEmision,
                    Moneda = dto.Moneda,
                    TipoCambio = dto.Moneda == "USD" ? dto.TipoCambio : null,
                    Subtotal = dto.Subtotal,
                    Igv = dto.Igv,
                    Total = dto.Total,
                    FormaPago = dto.FormaPago,
                    DetraccionMonto = dto.DetraccionMonto,
                    OrdenCompraRef = dto.OrdenCompraRef,
                    GuiaRef = dto.GuiaRef,
                    ArchivoUrl = dto.ArchivoUrl,
                    Observacion = dto.Observacion,
                    ModeloIa = dto.ModeloIa,
                    Estado = lineasStock.Count > 0 ? "PENDIENTE_STOCK" : "REGISTRADO",
                    CreadoPorUsuarioSistemaId = usuarioId,
                    CreadoEn = DateTimeOffset.UtcNow,
                };
                var n = 1;
                foreach (var it in dto.Items)
                    comprobante.Items.Add(new ComprobanteCompraItem
                    {
                        Linea = n++,
                        Descripcion = it.Descripcion.Trim(),
                        Cantidad = it.Cantidad,
                        Unidad = it.Unidad,
                        PrecioUnitario = it.PrecioUnitario,
                        Total = it.Total,
                        Destino = it.Destino,
                        ProductoId = it.Destino == "STOCK" ? it.ProductoId : null,
                        Talla = it.Talla ?? "",
                        Color = it.Color ?? "",
                        CategoriaGasto = it.CategoriaGasto,
                    });
                ctx.ComprobanteCompra.Add(comprobante);
                await ctx.SaveChangesAsync();

                // La copia leída pasa a pertenecer a este comprobante (ya no se limpia como huérfana).
                var m = System.Text.RegularExpressions.Regex.Match(dto.ArchivoUrl ?? "", @"/archivos/(\d+)$");
                if (m.Success && long.TryParse(m.Groups[1].Value, out var archivoId))
                    await ctx.ComprobanteCompraArchivo
                        .Where(a => a.Id == archivoId && a.ComprobanteCompraId == null)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.ComprobanteCompraId, comprobante.Id));
            }

            // Ingresos al kardex, uno por línea. Cada línea queda marcada al aplicarse, así un reintento no duplica.
            var tc = comprobante.Moneda == "USD" ? comprobante.TipoCambio ?? 1m : 1m;
            foreach (var item in comprobante.Items.Where(i => i.Destino == "STOCK" && !i.MovimientoRegistrado))
            {
                // La boleta no recupera IGV: su costo real es el precio con IGV incluido.
                var costoBase = comprobante.Tipo == "BOLETA" && item.Cantidad > 0 ? item.Total / item.Cantidad : item.PrecioUnitario;
                try
                {
                    await _almacen.RegistrarMovimiento(new RegistrarMovimientoDto
                    {
                        AlmacenId = comprobante.AlmacenId ?? dto.AlmacenId!.Value,
                        ProductoId = item.ProductoId!.Value,
                        Talla = item.Talla,
                        Color = item.Color,
                        TipoMovimiento = "INGRESO",
                        Cantidad = item.Cantidad,
                        CostoUnitario = Math.Round(costoBase * tc, 4),
                        ReferenciaTipo = "COMPROBANTE",
                        ReferenciaId = comprobante.Id,
                    }, usuarioId);
                }
                catch (AbrilException ex)
                {
                    throw new AbrilException(
                        $"El comprobante se guardó, pero falló el ingreso de «{item.Descripcion}»: {ex.Message} Corrígelo y vuelve a confirmar: no se duplica nada.",
                        ex.StatusCode);
                }
                item.MovimientoRegistrado = true;
                await ctx.SaveChangesAsync();
            }

            comprobante.Estado = "REGISTRADO";
            await ctx.SaveChangesAsync();

            if (comprobante.Tipo == "BOLETA")
                avisos.Add("Es boleta: no da crédito fiscal.");
            if (comprobante.DetraccionMonto is > 0)
                avisos.Add($"Detracción pendiente: S/ {comprobante.DetraccionMonto:0.00} al Banco de la Nación.");

            return new ComprobanteGuardadoDto
            {
                Id = comprobante.Id,
                Estado = comprobante.Estado,
                LineasAStock = comprobante.Items.Count(i => i.Destino == "STOCK"),
                LineasGasto = comprobante.Items.Count(i => i.Destino != "STOCK"),
                Avisos = avisos,
            };
        }
    }
}
