using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;
using Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Features.PedidosModule.Infrastructure.Models;
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
                // El DNI es opcional al crear (el borrador puede estar incompleto, y Compras crea guías sin él);
                // se exige completo recién al transmitir (ValidarParaTransmitir).
                if (!string.IsNullOrWhiteSpace(dto.ConductorDni) && !GuiaValidaciones.DniValido(dto.ConductorDni.Trim()))
                    throw new AbrilException("El DNI del conductor debe tener 8 dígitos.", 400);
            }

            // Conductores y vehículos adicionales (la guía de SUNAT admite principal + secundarios).
            if (dto.ConductoresSecundarios.Count > 5 || dto.VehiculosSecundarios.Count > 5)
                throw new AbrilException("Máximo 5 conductores adicionales y 5 vehículos adicionales.", 400);
            foreach (var c in dto.ConductoresSecundarios)
            {
                if (!GuiaValidaciones.DniValido(c.NumDoc?.Trim()) || string.IsNullOrWhiteSpace(c.Nombres) || string.IsNullOrWhiteSpace(c.Apellidos)
                    || !GuiaValidaciones.LicenciaValida(GuiaValidaciones.NormalizarLicencia(c.Licencia)))
                    throw new AbrilException("Cada conductor adicional requiere DNI (8 dígitos), apellidos, nombres y licencia válida.", 400);
            }
            foreach (var v in dto.VehiculosSecundarios)
            {
                if (!GuiaValidaciones.PlacaValida(GuiaValidaciones.NormalizarPlaca(v.Placa)))
                    throw new AbrilException("La placa de cada vehículo adicional debe tener 6 o 7 letras/números.", 400);
            }

            using var ctx = _factory.CreateDbContext();

            var almacenOrigen = await ctx.Almacen.FirstOrDefaultAsync(a => a.Id == dto.AlmacenOrigenId)
                ?? throw new AbrilException("Almacén de origen no encontrado.", 404);
            if (dto.AlmacenDestinoId.HasValue && !await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenDestinoId))
                throw new AbrilException("Almacén de destino no encontrado.", 404);

            // El correlativo SUNAT NO se asigna acá: un borrador no es un documento fiscal y puede
            // eliminarse. El número se toma al transmitir (ver Enviar), así no quedan huecos.
            var guia = new GuiaRemision
            {
                Serie = _settings.Serie,
                Numero = null,
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
                VehiculoPlaca = GuiaValidaciones.NormalizarPlaca(dto.VehiculoPlaca),
                VehiculoNroCirculacion = string.IsNullOrWhiteSpace(dto.VehiculoNroCirculacion) ? null : dto.VehiculoNroCirculacion.Trim(),
                ConductoresSecundariosJson = GuiaSecundarios.Escribir(dto.ConductoresSecundarios.Select(c => new ConductorSecundario
                {
                    NumDoc = c.NumDoc.Trim(),
                    Nombres = c.Nombres.Trim().ToUpperInvariant(),
                    Apellidos = c.Apellidos.Trim().ToUpperInvariant(),
                    Licencia = GuiaValidaciones.NormalizarLicencia(c.Licencia)!,
                }).ToList()),
                VehiculosSecundariosJson = GuiaSecundarios.Escribir(dto.VehiculosSecundarios.Select(v => new VehiculoSecundario
                {
                    Placa = GuiaValidaciones.NormalizarPlaca(v.Placa)!,
                    NroCirculacion = string.IsNullOrWhiteSpace(v.NroCirculacion) ? null : v.NroCirculacion.Trim(),
                }).ToList()),
                ConductorNombres = dto.ConductorNombres,
                ConductorLicencia = GuiaValidaciones.NormalizarLicencia(dto.ConductorLicencia),
                ConductorDni = dto.ConductorDni?.Trim(),
                ConductorApellidos = string.IsNullOrWhiteSpace(dto.ConductorApellidos) ? null : dto.ConductorApellidos.Trim(),
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
                if (item.ProductoId.HasValue)
                {
                    if (!await ctx.Producto.AnyAsync(p => p.Id == item.ProductoId.Value))
                        throw new AbrilException($"Producto {item.ProductoId} no encontrado.", 404);
                }
                else if (string.IsNullOrWhiteSpace(item.Descripcion))
                {
                    throw new AbrilException("Una línea sin producto del catálogo requiere una descripción del bien.", 400);
                }

                ctx.GuiaRemisionItem.Add(new GuiaRemisionItem
                {
                    GuiaRemisionId = guia.Id,
                    ProductoId = item.ProductoId,
                    Descripcion = item.ProductoId.HasValue ? null : item.Descripcion!.Trim(),
                    EsBienTercero = item.EsBienTercero,
                    Talla = item.ProductoId.HasValue ? item.Talla : "",
                    Color = item.ProductoId.HasValue ? item.Color : "",
                    Cantidad = item.Cantidad,
                    UnidadMedida = item.UnidadMedida,
                    PedidoItemId = item.ProductoId.HasValue ? item.PedidoItemId : null,
                });
            }
            await ctx.SaveChangesAsync();

            // Un borrador no mueve stock ni avanza pedidos: eso ocurre recién cuando SUNAT acepta la
            // guía (ver AplicarDespacho). Así crear/eliminar borradores no ensucia el kardex.
            return await BuildDetail(ctx, guia.Id);
        }

        /// <summary>
        /// Despacho físico: salida del almacén de origen, ingreso al de destino (si es traslado propio)
        /// y avance de lo despachado en el pedido. Se ejecuta una sola vez, cuando la guía es ACEPTADA.
        /// </summary>
        private async Task AplicarDespacho(AppDbContext ctx, GuiaRemision guia)
        {
            if (guia.DespachoAplicado) return;

            // Las líneas libres (sin producto del catálogo) no tienen stock: no mueven el kardex.
            foreach (var item in guia.Items.Where(i => i.ProductoId.HasValue))
            {
                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = guia.AlmacenOrigenId,
                    ProductoId = item.ProductoId!.Value,
                    Talla = item.Talla,
                    Color = item.Color,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.Cantidad,
                    ReferenciaTipo = "GUIA_REMISION",
                    ReferenciaId = guia.Id,
                }, guia.CreadoPorUsuarioSistemaId);

                // Mercadería en tránsito: el destino NO se acredita acá. El stock entra al almacén de destino recién cuando
                // alguien confirma lo que llegó (ver RecepcionGuiaService), así la mina no ve como disponible lo que sigue en camino.

                // Si este ítem despacha un pedido puntual, avanza su trazabilidad — recién con
                // esto el pedido sabe que ya salió de Lima, antes de que mina confirme.
                if (item.PedidoItemId.HasValue)
                {
                    var pedidoItem = await ctx.PedidoItem.FindAsync(item.PedidoItemId.Value);
                    if (pedidoItem != null) pedidoItem.CantidadDespachada += item.Cantidad;
                }
            }

            guia.DespachoAplicado = true;
            // Con destino propio la mercadería queda en tránsito hasta la recepción; sin destino no hay nada que recibir.
            guia.DestinoAcreditado = !guia.AlmacenDestinoId.HasValue;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Reglas de SUNAT que se exigen recién al transmitir (un borrador puede estar incompleto).
        /// Junta todos los problemas en un solo mensaje para corregirlos de una vez.
        /// </summary>
        private void ValidarParaTransmitir(GuiaRemision guia)
        {
            var problemas = new List<string>();
            var propio = guia.AlmacenDestinoId.HasValue;

            if (!guia.Items.Any()) problemas.Add("la guía no tiene productos");
            if (guia.PesoBrutoTotal <= 0) problemas.Add("falta el peso bruto total");

            void RevisarAlmacen(string rol, Abril_Backend.Features.PersonasModule.Infrastructure.Models.Almacen? a)
            {
                if (a == null) return;
                if (string.IsNullOrWhiteSpace(a.Direccion)) problemas.Add($"el almacén de {rol} «{a.Nombre}» no tiene dirección");
                if (a.Ubigeo == null || a.Ubigeo.Length != 6 || !a.Ubigeo.All(char.IsDigit)) problemas.Add($"el almacén de {rol} «{a.Nombre}» no tiene ubigeo de 6 dígitos");
            }
            RevisarAlmacen("origen", guia.AlmacenOrigen);
            RevisarAlmacen("llegada", guia.AlmacenDestino);

            if (guia.MotivoTraslado == "04")
            {
                if (!propio) problemas.Add("el traslado entre establecimientos de la misma empresa requiere un almacén de destino");
                else if (guia.AlmacenDestinoId == guia.AlmacenOrigenId) problemas.Add("el almacén de destino no puede ser el mismo que el de origen");
                else if (guia.AlmacenOrigen != null && guia.AlmacenDestino != null
                    && guia.AlmacenOrigen.CodigoEstablecimientoSunat == guia.AlmacenDestino.CodigoEstablecimientoSunat)
                    problemas.Add($"origen y destino tienen el mismo código de establecimiento SUNAT ({guia.AlmacenOrigen.CodigoEstablecimientoSunat}): revisa los códigos en Gestionar almacenes");
            }
            else if (!propio)
            {
                if (!GuiaValidaciones.RucValido(guia.DestinatarioRuc)) problemas.Add("el RUC del destinatario no es válido");
                else if (guia.DestinatarioRuc == _settings.Ruc) problemas.Add("para este motivo el destinatario no puede ser tu propia empresa");
                if (string.IsNullOrWhiteSpace(guia.DestinatarioRazonSocial)) problemas.Add("falta la razón social del destinatario");
            }

            if (guia.ModalidadTraslado == "01")
            {
                if (!GuiaValidaciones.RucValido(guia.TransportistaRuc)) problemas.Add("el RUC del transportista no es válido");
                else if (guia.TransportistaRuc == _settings.Ruc) problemas.Add("en transporte público el transportista es otra empresa; si llevas tú la carga usa transporte privado");
                if (string.IsNullOrWhiteSpace(guia.TransportistaRazonSocial)) problemas.Add("falta la razón social del transportista");
            }
            else
            {
                if (!GuiaValidaciones.PlacaValida(GuiaValidaciones.NormalizarPlaca(guia.VehiculoPlaca))) problemas.Add("la placa del vehículo no es válida (6 o 7 letras/números)");
                if (string.IsNullOrWhiteSpace(guia.ConductorNombres)) problemas.Add("falta el nombre del conductor");
                if (!GuiaValidaciones.DniValido(guia.ConductorDni)) problemas.Add("falta el DNI del conductor (8 dígitos)");
                if (!GuiaValidaciones.LicenciaValida(GuiaValidaciones.NormalizarLicencia(guia.ConductorLicencia))) problemas.Add("la licencia del conductor no es válida (8 a 12 letras/números)");
            }

            if (problemas.Count > 0)
                throw new AbrilException("No se puede transmitir todavía: " + string.Join("; ", problemas) + ".", 400);
        }

        /// <summary>Verifica antes de transmitir que el almacén de origen tiene el stock de toda la guía.</summary>
        private static async Task ValidarStockOrigen(AppDbContext ctx, GuiaRemision guia)
        {
            if (guia.DespachoAplicado) return;

            var requeridos = guia.Items.Where(i => i.ProductoId.HasValue)
                .GroupBy(i => new { ProductoId = i.ProductoId!.Value, i.Talla, i.Color });
            foreach (var g in requeridos)
            {
                var necesario = g.Sum(i => i.Cantidad);
                var disponible = await ctx.Stock.AsNoTracking()
                    .Where(s => s.AlmacenId == guia.AlmacenOrigenId && s.ProductoId == g.Key.ProductoId && s.Talla == g.Key.Talla && s.Color == g.Key.Color)
                    .Select(s => s.CantidadActual)
                    .FirstOrDefaultAsync();
                if (disponible < necesario)
                {
                    var nombre = g.First().Producto?.Nombre ?? $"producto {g.Key.ProductoId}";
                    throw new AbrilException(
                        $"Stock insuficiente en el almacén de origen para «{nombre}»: hay {disponible} y la guía lleva {necesario}.", 400);
                }
            }
        }

        public async Task<GuiaRemisionListResponseDto> List(string? search, string? estado, HashSet<int>? proyectosPermitidos, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.GuiaRemision
                .Include(g => g.AlmacenOrigen)
                .Include(g => g.AlmacenDestino)
                .Include(g => g.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(g =>
                    (g.DestinatarioRazonSocial != null && g.DestinatarioRazonSocial.ToLower().Contains(s)) ||
                    g.AlmacenOrigen!.Nombre.ToLower().Contains(s) ||
                    (g.AlmacenDestino != null && g.AlmacenDestino.Nombre.ToLower().Contains(s)));
            }
            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(g => g.Estado == estado);
            // La guía no tiene proyecto propio — se filtra por el proyecto de su almacén de origen O el de destino: quien
            // recibe en la mina debe ver las guías que le llegan, aunque salgan del almacén central (sin proyecto).
            if (proyectosPermitidos != null)
                query = query.Where(g =>
                    (g.AlmacenOrigen!.ProyectoId != null && proyectosPermitidos.Contains(g.AlmacenOrigen.ProyectoId.Value))
                    || (g.AlmacenDestino != null && g.AlmacenDestino.ProyectoId != null && proyectosPermitidos.Contains(g.AlmacenDestino.ProyectoId.Value)));

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(g => g.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new GuiaRemisionListItemDto
                {
                    Id = g.Id,
                    Codigo = g.Numero == null ? $"Borrador #{g.Id}" : $"{g.Serie}-{g.Numero:D8}",
                    Estado = g.Estado,
                    AlmacenOrigenNombre = g.AlmacenOrigen!.Nombre,
                    AlmacenDestinoNombre = g.AlmacenDestino != null ? g.AlmacenDestino.Nombre : null,
                    FechaTraslado = g.FechaTraslado,
                    CantidadItems = g.Items.Count,
                    CreadoEn = g.CreadoEn,
                    RecepcionEstado = g.RecepcionEstado,
                    TieneDestino = g.AlmacenDestinoId != null,
                    DiferenciasAbiertas = ctx.GuiaDiferencia.Count(d => d.GuiaRemisionId == g.Id && d.Estado == "ABIERTA"),
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

        public async Task<GuiaRemisionDetailDto> Enviar(long id, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var guia = await ctx.GuiaRemision
                .Include(g => g.AlmacenOrigen)
                .Include(g => g.AlmacenDestino)
                .Include(g => g.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

            // La guía no tiene proyecto propio — se valida contra el proyecto de su almacén de origen.
            if (guia.AlmacenOrigen!.ProyectoId.HasValue && !scope.Permite(guia.AlmacenOrigen.ProyectoId.Value))
                throw new AbrilException("No tienes permiso para transmitir guías de remisión de este proyecto.", 403);

            // RECHAZADA se puede reenviar: SUNAT rechaza por validación del XML sin registrar el
            // comprobante, así que se reintenta con el mismo número una vez corregido el dato.
            if (guia.Estado != "BORRADOR" && guia.Estado != "RECHAZADA")
                throw new AbrilException($"Esta guía ya fue transmitida (estado {guia.Estado}) — no se puede reenviar.", 400);

            // Antes de gastar un correlativo: que los datos estén completos para SUNAT y que haya stock.
            ValidarParaTransmitir(guia);

            // Una guía puede haber quedado en borrador desde un día anterior: SUNAT no admite un inicio de
            // traslado anterior a la emisión, así que la fecha pasa a hoy (queda guardada, el PDF coincide).
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            if (guia.FechaTraslado < hoy)
            {
                guia.FechaTraslado = hoy;
                await ctx.SaveChangesAsync();
            }

            await ValidarStockOrigen(ctx, guia);

            // Correlativo SUNAT: se toma recién ahora y queda fijo en la guía, de modo que un reintento
            // (error de red, rechazo por formato) reenvía con el MISMO número y no consume otro.
            if (guia.Numero == null)
            {
                guia.Numero = await ctx.Database
                    .SqlQuery<long>($"""SELECT nextval('lb_guia_remision_correlativo') AS "Value" """)
                    .SingleAsync();
                await ctx.SaveChangesAsync();
            }

            var xmlSinFirmar = _xmlBuilder.Build(guia);
            var (xmlFirmado, digestValue) = _xmlSigner.Firmar(xmlSinFirmar);
            var nombreArchivo = $"{_settings.Ruc}-09-{guia.Serie}-{guia.Numero}";

            var envio = await _sunatClient.EnviarAsync(xmlFirmado, nombreArchivo);
            if (!envio.Enviado)
                throw new AbrilException($"SUNAT no aceptó el envío: {envio.ErrorMensaje}", 502);

            guia.XmlNombreArchivo = nombreArchivo;
            guia.XmlHashFirma = digestValue;
            guia.XmlFirmado = xmlFirmado;
            guia.CdrXml = null;
            guia.QrUrl = null;
            guia.EnviadoEn = DateTimeOffset.UtcNow;
            guia.Ticket = envio.NumTicket;
            guia.Estado = "ENVIADA";
            guia.CdrCodigoRespuesta = null;
            guia.CdrDescripcion = null;
            guia.RespondidoEn = null;
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
                        guia.CdrXml = consulta.CdrXml;
                        guia.QrUrl = consulta.QrUrl;
                        guia.Estado = consulta.Aceptado ? "ACEPTADA" : "RECHAZADA";
                        guia.RespondidoEn = DateTimeOffset.UtcNow;
                        await ctx.SaveChangesAsync();
                        if (consulta.Aceptado) await AplicarDespacho(ctx, guia);
                        break;
                    }
                }
            }

            return await BuildDetail(ctx, id);
        }

        public async Task<GuiaRemisionDetailDto> ConsultarEstado(long id, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var guia = await ctx.GuiaRemision.Include(g => g.AlmacenOrigen).Include(g => g.Items).FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

            if (guia.AlmacenOrigen!.ProyectoId.HasValue && !scope.Permite(guia.AlmacenOrigen.ProyectoId.Value))
                throw new AbrilException("No tienes permiso para consultar guías de remisión de este proyecto.", 403);

            if (guia.Estado != "ENVIADA" || string.IsNullOrWhiteSpace(guia.Ticket))
                throw new AbrilException("Esta guía no tiene un ticket pendiente de SUNAT.", 400);

            var consulta = await _sunatClient.ConsultarEstadoAsync(guia.Ticket);
            if (consulta.CdrGenerado)
            {
                guia.CdrCodigoRespuesta = consulta.CodigoRespuesta;
                guia.CdrDescripcion = consulta.Descripcion;
                guia.CdrXml = consulta.CdrXml;
                guia.QrUrl = consulta.QrUrl;
                guia.Estado = consulta.Aceptado ? "ACEPTADA" : "RECHAZADA";
                guia.RespondidoEn = DateTimeOffset.UtcNow;
                await ctx.SaveChangesAsync();
                if (consulta.Aceptado) await AplicarDespacho(ctx, guia);
            }

            return await BuildDetail(ctx, id);
        }

        public async Task Eliminar(long id, long eliminadoPorId, LbScopeProyectos scope)
        {
            using var ctx = _factory.CreateDbContext();
            var guia = await ctx.GuiaRemision
                .Include(g => g.AlmacenOrigen)
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

            if (guia.AlmacenOrigen?.ProyectoId is int proyectoId && !scope.Permite(proyectoId))
                throw new AbrilException("No tienes permiso sobre el proyecto de origen de esta guía.", 403);

            // RECHAZADA: SUNAT no registró el comprobante, así que no hay documento legal que dar de baja.
            if (guia.Estado != "BORRADOR" && guia.Estado != "RECHAZADA")
                throw new AbrilException(
                    $"Solo se pueden eliminar guías en BORRADOR o RECHAZADA (esta está {guia.Estado}). Una guía ya transmitida a SUNAT es un documento legal: requiere comunicación de baja.", 400);
            if (guia.Items.Any(i => i.ConfirmadoEn != null))
                throw new AbrilException("Esta guía ya tiene una recepción confirmada — no se puede eliminar.", 400);

            await RevertirDespacho(ctx, guia, eliminadoPorId);

            ctx.GuiaRemisionItem.RemoveRange(guia.Items);
            ctx.GuiaRemision.Remove(guia);
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Anula una guía ya ACEPTADA por SUNAT: revierte stock y pedido y la deja en ANULADA (no se
        /// borra: es un documento legal y queda su historial). La baja ante SUNAT se hace aparte en SOL
        /// (Empresas > Guía de Remisión electrónica > Baja de GRE); acá solo se refleja en el sistema.
        /// </summary>
        public async Task Anular(long id, long anuladaPorId, LbScopeProyectos scope, AnularGuiaDto dto)
        {
            // SUNAT no ofrece baja por API: el sistema no puede comprobar que se hizo. Por eso se exige el
            // motivo y el número de la constancia de baja de SOL, y se guardan con quién y cuándo (auditable).
            var motivo = dto.Motivo?.Trim();
            var constancia = dto.ConstanciaBaja?.Trim();
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Length < 5)
                throw new AbrilException("Indica el motivo de la anulación (mínimo 5 caracteres).", 400);
            if (string.IsNullOrWhiteSpace(constancia) || constancia.Length < 3)
                throw new AbrilException("Indica el número de la constancia de baja que entregó SOL: primero se da la baja en SUNAT y recién después se anula acá.", 400);

            using var ctx = _factory.CreateDbContext();
            var guia = await ctx.GuiaRemision
                .Include(g => g.AlmacenOrigen)
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

            if (guia.AlmacenOrigen?.ProyectoId is int proyectoId && !scope.Permite(proyectoId))
                throw new AbrilException("No tienes permiso sobre el proyecto de origen de esta guía.", 403);

            if (guia.Estado != "ACEPTADA")
                throw new AbrilException($"Solo se puede anular una guía ACEPTADA por SUNAT (esta está {guia.Estado}). Las borradores y rechazadas se eliminan.", 400);
            if (guia.Items.Any(i => i.ConfirmadoEn != null))
                throw new AbrilException("Esta guía ya tiene una recepción confirmada — no se puede anular.", 400);

            await RevertirDespacho(ctx, guia, anuladaPorId);

            guia.Estado = "ANULADA";
            guia.DespachoAplicado = false;
            guia.AnulacionMotivo = motivo;
            guia.AnulacionConstancia = constancia;
            guia.AnuladaPorUsuarioSistemaId = anuladaPorId;
            guia.AnuladaEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        /// <summary>Compensa el stock y el pedido que movió la guía (compartido por Eliminar y Anular).</summary>
        private async Task RevertirDespacho(AppDbContext ctx, GuiaRemision guia, long eliminadoPorId)
        {
            // El kardex no se borra: lo que movió la guía se compensa con movimientos inversos.
            // Se parte de los movimientos reales y no de los ítems, porque si la creación falló a
            // mitad de camino puede haber guías con ítems pero sin (o con solo parte de) stock movido.
            var movimientos = await ctx.Movimiento.AsNoTracking()
                .Where(m => m.ReferenciaTipo == "GUIA_REMISION" && m.ReferenciaId == guia.Id)
                .ToListAsync();
            var yaRevertida = await ctx.Movimiento.AnyAsync(m => m.ReferenciaTipo == "GUIA_REMISION_ANULACION" && m.ReferenciaId == guia.Id);

            if (movimientos.Count > 0 && !yaRevertida)
            {
                // Revertir un INGRESO en destino es una SALIDA y puede fallar si la mina ya consumió
                // ese stock — se valida todo antes de mover nada para no dejar una reversa a medias.
                var ingresosDestino = movimientos
                    .Where(m => m.TipoMovimiento == "INGRESO")
                    .GroupBy(m => new { m.AlmacenId, m.ProductoId, m.Talla, m.Color });
                foreach (var g in ingresosDestino)
                {
                    var necesario = g.Sum(m => m.Cantidad);
                    var disponible = await ctx.Stock.AsNoTracking()
                        .Where(s => s.AlmacenId == g.Key.AlmacenId && s.ProductoId == g.Key.ProductoId && s.Talla == g.Key.Talla && s.Color == g.Key.Color)
                        .Select(s => s.CantidadActual)
                        .FirstOrDefaultAsync();
                    if (disponible < necesario)
                        throw new AbrilException(
                            $"No se puede eliminar: el almacén de destino ya no tiene el stock que esta guía le acreditó (quedan {disponible} de {necesario}).", 400);
                }

                foreach (var m in movimientos.Where(m => m.TipoMovimiento == "INGRESO"))
                {
                    await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                    {
                        AlmacenId = m.AlmacenId,
                        ProductoId = m.ProductoId,
                        Talla = m.Talla,
                        Color = m.Color,
                        TipoMovimiento = "SALIDA",
                        Cantidad = m.Cantidad,
                        ReferenciaTipo = "GUIA_REMISION_ANULACION",
                        ReferenciaId = guia.Id,
                    }, eliminadoPorId);
                }
                foreach (var m in movimientos.Where(m => m.TipoMovimiento == "SALIDA"))
                {
                    await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                    {
                        AlmacenId = m.AlmacenId,
                        ProductoId = m.ProductoId,
                        Talla = m.Talla,
                        Color = m.Color,
                        TipoMovimiento = "INGRESO",
                        Cantidad = m.Cantidad,
                        CostoUnitario = m.CostoUnitario,
                        ReferenciaTipo = "GUIA_REMISION_ANULACION",
                        ReferenciaId = guia.Id,
                    }, eliminadoPorId);
                }
            }

            // Solo si el despacho ya se había aplicado (guías anteriores o aceptadas) se sumó lo
            // despachado al pedido de origen — en ese caso se resta de vuelta.
            foreach (var item in guia.Items.Where(i => guia.DespachoAplicado && i.PedidoItemId.HasValue))
            {
                var pedidoItem = await ctx.PedidoItem.FindAsync(item.PedidoItemId!.Value);
                if (pedidoItem != null)
                    pedidoItem.CantidadDespachada = Math.Max(0, pedidoItem.CantidadDespachada - item.Cantidad);
            }
        }

        private async Task<GuiaRemisionDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var guia = await ctx.GuiaRemision
                .Include(g => g.AlmacenOrigen)
                .Include(g => g.AlmacenDestino)
                .Include(g => g.CreadoPor!).ThenInclude(u => u!.Persona)
                .Include(g => g.Items).ThenInclude(i => i.Producto)
                .Include(g => g.Items).ThenInclude(i => i.PedidoItem!).ThenInclude(pi => pi.Pedido)
                .Include(g => g.Items).ThenInclude(i => i.ConfirmadoPor!).ThenInclude(u => u!.Persona)
                .FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new AbrilException("Guía de remisión no encontrada.", 404);

            var diferencias = await RecepcionGuiaService.Armar(ctx, await ctx.GuiaDiferencia.AsNoTracking()
                .Where(d => d.GuiaRemisionId == guia.Id).OrderBy(d => d.Estado == "RESUELTA").ThenBy(d => d.Id).ToListAsync());

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
                ConductorDni = guia.ConductorDni,
                ConductorApellidos = guia.ConductorApellidos,
                VehiculoNroCirculacion = guia.VehiculoNroCirculacion,
                ConductoresSecundarios = GuiaSecundarios.Leer<ConductorSecundario>(guia.ConductoresSecundariosJson)
                    .Select(c => new ConductorSecundarioDto { NumDoc = c.NumDoc, Nombres = c.Nombres, Apellidos = c.Apellidos, Licencia = c.Licencia }).ToList(),
                VehiculosSecundarios = GuiaSecundarios.Leer<VehiculoSecundario>(guia.VehiculosSecundariosJson)
                    .Select(v => new VehiculoSecundarioDto { Placa = v.Placa, NroCirculacion = v.NroCirculacion }).ToList(),
                QrUrl = guia.QrUrl,
                AnulacionMotivo = guia.AnulacionMotivo,
                AnulacionConstancia = guia.AnulacionConstancia,
                AnuladaEn = guia.AnuladaEn,
                EmisorRuc = _settings.Ruc,
                EmisorRazonSocial = _settings.RazonSocial,
                AlmacenOrigenDireccion = guia.AlmacenOrigen.Direccion,
                AlmacenOrigenUbigeo = guia.AlmacenOrigen.Ubigeo,
                AlmacenDestinoDireccion = guia.AlmacenDestino?.Direccion,
                AlmacenDestinoUbigeo = guia.AlmacenDestino?.Ubigeo,
                Observacion = guia.Observacion,
                ReferenciaTipo = guia.ReferenciaTipo,
                ReferenciaId = guia.ReferenciaId,
                CreadoPorNombre = $"{guia.CreadoPor!.Persona!.Apellidos} {guia.CreadoPor.Persona.Nombres}",
                CreadoEn = guia.CreadoEn,
                Ticket = guia.Ticket,
                CdrCodigoRespuesta = guia.CdrCodigoRespuesta,
                CdrDescripcion = guia.CdrDescripcion,
                EnviadoEn = guia.EnviadoEn,
                RespondidoEn = guia.RespondidoEn,
                ConfirmacionPendiente = guia.AlmacenDestinoId != null
                    && guia.Estado == "ACEPTADA"
                    && guia.Items.All(i => i.ConfirmadoEn == null),
                RecepcionEstado = guia.RecepcionEstado,
                PuedeRecibir = guia.AlmacenDestinoId != null && guia.Estado == "ACEPTADA" && guia.RecepcionEstado is "PENDIENTE" or "PARCIAL",
                DiferenciasAbiertas = diferencias.Count(d => d.Estado == "ABIERTA"),
                Recepciones = await RecepcionGuiaService.Historial(ctx, guia.Id),
                Diferencias = diferencias,
                Items = guia.Items.Select(i => new GuiaRemisionItemDetailDto
                {
                    Id = i.Id,
                    ProductoId = i.ProductoId,
                    ProductoNombre = i.Producto?.Nombre ?? i.Descripcion ?? "",
                    ProductoCodigo = i.Producto?.Codigo,
                    EsLineaLibre = i.ProductoId == null,
                    EsBienTercero = i.EsBienTercero,
                    Talla = i.Talla,
                    Color = i.Color,
                    Cantidad = i.Cantidad,
                    UnidadMedida = i.UnidadMedida,
                    PedidoItemId = i.PedidoItemId,
                    PedidoCodigo = i.PedidoItem?.Pedido?.Codigo,
                    CantidadConfirmada = i.CantidadConfirmada,
                    CantidadDanada = i.CantidadDanada,
                    CantidadFaltante = i.CantidadFaltante,
                    CantidadPendiente = Math.Max(0, i.Cantidad - (i.CantidadConfirmada ?? 0) - i.CantidadDanada - i.CantidadFaltante),
                    ConfirmadoEn = i.ConfirmadoEn,
                    ConfirmadoPorNombre = i.ConfirmadoPor != null
                        ? $"{i.ConfirmadoPor.Persona!.Apellidos} {i.ConfirmadoPor.Persona.Nombres}"
                        : null,
                }).ToList(),
            };
        }
    }
}
