using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;
using Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.GuiasRemisionModule.Application.Services
{
    public class GuiaRemisionService : IGuiaRemisionService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAlmacenKardexService _almacenService;
        private readonly IGuiaRemisionXmlBuilder _xmlBuilder;
        private readonly IXmlSigner _xmlSigner;
        private readonly ISunatGreClient _sunatClient;
        private readonly SunatGreSettings _settings;

        public GuiaRemisionService(
            IDbContextFactory<AppDbContext> factory,
            IAlmacenKardexService almacenService,
            IGuiaRemisionXmlBuilder xmlBuilder,
            IXmlSigner xmlSigner,
            ISunatGreClient sunatClient,
            IOptions<SunatGreSettings> settings)
        {
            _factory = factory;
            _almacenService = almacenService;
            _xmlBuilder = xmlBuilder;
            _xmlSigner = xmlSigner;
            _sunatClient = sunatClient;
            _settings = settings.Value;
        }

        public async Task<GuiaRemisionDetailDto> Crear(GuiaRemisionCreateDto dto, long creadoPorId)
        {
            if (dto.Items.Count == 0)
                throw new AbrilException("La guía debe tener al menos un producto.", 400);
            if (dto.Items.Any(i => i.Cantidad <= 0))
                throw new AbrilException("Las cantidades deben ser mayores a cero.", 400);
            if (dto.ModalidadTraslado == "01" && string.IsNullOrWhiteSpace(dto.TransportistaRuc))
                throw new AbrilException("Transporte público requiere RUC del transportista.", 400);
            if (dto.ModalidadTraslado == "02")
            {
                if (string.IsNullOrWhiteSpace(dto.VehiculoPlaca))
                    throw new AbrilException("Transporte privado requiere la placa del vehículo.", 400);
                if (string.IsNullOrWhiteSpace(dto.ConductorNombres) || string.IsNullOrWhiteSpace(dto.ConductorLicencia))
                    throw new AbrilException("Transporte privado requiere nombres y licencia del conductor — SUNAT lo exige para que la guía sea válida.", 400);
            }

            using var ctx = _factory.CreateDbContext();

            var almacenOrigen = await ctx.Almacen.FirstOrDefaultAsync(a => a.Id == dto.AlmacenOrigenId)
                ?? throw new AbrilException("Almacén de origen no encontrado.", 404);
            if (dto.AlmacenDestinoId.HasValue && !await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenDestinoId))
                throw new AbrilException("Almacén de destino no encontrado.", 404);

            var correlativo = await ctx.Database
                .SqlQuery<long>($"""SELECT nextval('lb_guia_remision_correlativo') AS "Value" """)
                .SingleAsync();

            var guia = new GuiaRemision
            {
                Serie = _settings.Serie,
                Numero = correlativo,
                Estado = "BORRADOR",
                MotivoTraslado = dto.MotivoTraslado,
                ModalidadTraslado = dto.ModalidadTraslado,
                FechaTraslado = dto.FechaTraslado,
                PesoBrutoTotal = dto.PesoBrutoTotal,
                PesoBrutoUnidad = dto.PesoBrutoUnidad,
                NumBultos = dto.NumBultos,
                AlmacenOrigenId = dto.AlmacenOrigenId,
                AlmacenDestinoId = dto.AlmacenDestinoId,
                DestinatarioRuc = dto.DestinatarioRuc,
                DestinatarioRazonSocial = dto.DestinatarioRazonSocial,
                TransportistaRuc = dto.TransportistaRuc,
                TransportistaRazonSocial = dto.TransportistaRazonSocial,
                VehiculoPlaca = dto.VehiculoPlaca,
                ConductorNombres = dto.ConductorNombres,
                ConductorLicencia = dto.ConductorLicencia,
                Observacion = dto.Observacion,
                ReferenciaTipo = dto.ReferenciaTipo,
                ReferenciaId = dto.ReferenciaId,
                CreadoPorUsuarioSistemaId = creadoPorId,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.GuiaRemision.Add(guia);
            await ctx.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                if (!await ctx.Producto.AnyAsync(p => p.Id == item.ProductoId))
                    throw new AbrilException($"Producto {item.ProductoId} no encontrado.", 404);

                ctx.GuiaRemisionItem.Add(new GuiaRemisionItem
                {
                    GuiaRemisionId = guia.Id,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    Cantidad = item.Cantidad,
                    UnidadMedida = item.UnidadMedida,
                });
            }
            await ctx.SaveChangesAsync();

            // El despacho físico ya ocurre acá (todo o nada) — la transmisión a SUNAT (Enviar) es
            // un paso legal aparte que puede reintentarse sin volver a tocar el stock.
            foreach (var item in dto.Items)
            {
                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = dto.AlmacenOrigenId,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.Cantidad,
                    ReferenciaTipo = "GUIA_REMISION",
                    ReferenciaId = guia.Id,
                }, creadoPorId);

                if (dto.AlmacenDestinoId.HasValue)
                {
                    await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                    {
                        AlmacenId = dto.AlmacenDestinoId.Value,
                        ProductoId = item.ProductoId,
                        Talla = item.Talla,
                        TipoMovimiento = "INGRESO",
                        Cantidad = item.Cantidad,
                        ReferenciaTipo = "GUIA_REMISION",
                        ReferenciaId = guia.Id,
                    }, creadoPorId);
                }
            }

            return await BuildDetail(ctx, guia.Id);
        }

        public async Task<GuiaRemisionListResponseDto> List(string? estado, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.GuiaRemision
                .Include(g => g.AlmacenOrigen)
                .Include(g => g.AlmacenDestino)
                .Include(g => g.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(g => g.Estado == estado);

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(g => g.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new GuiaRemisionListItemDto
                {
                    Id = g.Id,
                    Codigo = $"{g.Serie}-{g.Numero:D8}",
                    Estado = g.Estado,
                    AlmacenOrigenNombre = g.AlmacenOrigen!.Nombre,
                    AlmacenDestinoNombre = g.AlmacenDestino != null ? g.AlmacenDestino.Nombre : null,
                    FechaTraslado = g.FechaTraslado,
                    CantidadItems = g.Items.Count,
                    CreadoEn = g.CreadoEn,
                })
                .ToListAsync();

            return new GuiaRemisionListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<GuiaRemisionDetailDto> GetById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        public async Task<GuiaRemisionDetailDto> Enviar(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var guia = await ctx.GuiaRemision
                .Include(g => g.AlmacenOrigen)
                .Include(g => g.AlmacenDestino)
                .Include(g => g.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

            if (guia.Estado != "BORRADOR")
                throw new AbrilException($"Esta guía ya fue transmitida (estado {guia.Estado}) — no se puede reenviar.", 400);

            var xmlSinFirmar = _xmlBuilder.Build(guia);
            var (xmlFirmado, digestValue) = _xmlSigner.Firmar(xmlSinFirmar);
            var nombreArchivo = $"{_settings.Ruc}-09-{guia.Serie}-{guia.Numero}";

            var envio = await _sunatClient.EnviarAsync(xmlFirmado, nombreArchivo);
            if (!envio.Enviado)
                throw new AbrilException($"SUNAT no aceptó el envío: {envio.ErrorMensaje}", 502);

            guia.XmlNombreArchivo = nombreArchivo;
            guia.XmlHashFirma = digestValue;
            guia.EnviadoEn = DateTimeOffset.UtcNow;
            guia.Ticket = envio.NumTicket;
            guia.Estado = "ENVIADA";
            await ctx.SaveChangesAsync();

            // SUNAT resuelve el ticket de forma asíncrona — normalmente en segundos. Se intenta
            // resolver acá mismo con un par de reintentos cortos para no obligar al usuario a un
            // segundo clic en el caso común; si no alcanza, la guía queda ENVIADA con el ticket
            // guardado y ConsultarEstado (botón "Consultar estado" en el frontend) lo retoma.
            if (!string.IsNullOrWhiteSpace(envio.NumTicket))
            {
                for (var intento = 0; intento < 3; intento++)
                {
                    await Task.Delay(2000);
                    var consulta = await _sunatClient.ConsultarEstadoAsync(envio.NumTicket);
                    if (consulta.CdrGenerado)
                    {
                        guia.CdrCodigoRespuesta = consulta.CodigoRespuesta;
                        guia.CdrDescripcion = consulta.Descripcion;
                        guia.Estado = consulta.Aceptado ? "ACEPTADA" : "RECHAZADA";
                        guia.RespondidoEn = DateTimeOffset.UtcNow;
                        await ctx.SaveChangesAsync();
                        break;
                    }
                }
            }

            return await BuildDetail(ctx, id);
        }

        public async Task<GuiaRemisionDetailDto> ConsultarEstado(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var guia = await ctx.GuiaRemision.FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

            if (guia.Estado != "ENVIADA" || string.IsNullOrWhiteSpace(guia.Ticket))
                throw new AbrilException("Esta guía no tiene un ticket pendiente de SUNAT.", 400);

            var consulta = await _sunatClient.ConsultarEstadoAsync(guia.Ticket);
            if (consulta.CdrGenerado)
            {
                guia.CdrCodigoRespuesta = consulta.CodigoRespuesta;
                guia.CdrDescripcion = consulta.Descripcion;
                guia.Estado = consulta.Aceptado ? "ACEPTADA" : "RECHAZADA";
                guia.RespondidoEn = DateTimeOffset.UtcNow;
                await ctx.SaveChangesAsync();
            }

            return await BuildDetail(ctx, id);
        }

        private static async Task<GuiaRemisionDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var guia = await ctx.GuiaRemision
                .Include(g => g.AlmacenOrigen)
                .Include(g => g.AlmacenDestino)
                .Include(g => g.CreadoPor!).ThenInclude(u => u!.Persona)
                .Include(g => g.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

            return new GuiaRemisionDetailDto
            {
                Id = guia.Id,
                Serie = guia.Serie,
                Numero = guia.Numero,
                Estado = guia.Estado,
                MotivoTraslado = guia.MotivoTraslado,
                ModalidadTraslado = guia.ModalidadTraslado,
                FechaTraslado = guia.FechaTraslado,
                PesoBrutoTotal = guia.PesoBrutoTotal,
                PesoBrutoUnidad = guia.PesoBrutoUnidad,
                NumBultos = guia.NumBultos,
                AlmacenOrigenNombre = guia.AlmacenOrigen!.Nombre,
                AlmacenDestinoNombre = guia.AlmacenDestino?.Nombre,
                DestinatarioRuc = guia.DestinatarioRuc,
                DestinatarioRazonSocial = guia.DestinatarioRazonSocial,
                TransportistaRuc = guia.TransportistaRuc,
                TransportistaRazonSocial = guia.TransportistaRazonSocial,
                VehiculoPlaca = guia.VehiculoPlaca,
                ConductorNombres = guia.ConductorNombres,
                ConductorLicencia = guia.ConductorLicencia,
                Observacion = guia.Observacion,
                ReferenciaTipo = guia.ReferenciaTipo,
                ReferenciaId = guia.ReferenciaId,
                CreadoPorNombre = $"{guia.CreadoPor!.Persona!.Nombres} {guia.CreadoPor.Persona.Apellidos}",
                CreadoEn = guia.CreadoEn,
                Ticket = guia.Ticket,
                CdrCodigoRespuesta = guia.CdrCodigoRespuesta,
                CdrDescripcion = guia.CdrDescripcion,
                EnviadoEn = guia.EnviadoEn,
                RespondidoEn = guia.RespondidoEn,
                Items = guia.Items.Select(i => new GuiaRemisionItemDetailDto
                {
                    Id = i.Id,
                    ProductoNombre = i.Producto!.Nombre,
                    ProductoCodigo = i.Producto.Codigo,
                    Talla = i.Talla,
                    Cantidad = i.Cantidad,
                    UnidadMedida = i.UnidadMedida,
                }).ToList(),
            };
        }
    }
}
