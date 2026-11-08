using System.Globalization;
using System.Text.Json;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.FacturacionElectronicaModule.Application.Dtos;
using Abril_Backend.Features.FacturacionElectronicaModule.Application.Interfaces;
using Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Models;
using Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Sunat;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.FacturacionElectronicaModule.Application.Services
{
    public class CpeService : ICpeService
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ICpeXmlBuilder _xml;
        private readonly IXmlSigner _signer;
        private readonly ISunatCpeClient _sunat;
        private readonly SunatCpeSettings _s;

        public CpeService(
            IDbContextFactory<AppDbContext> factory,
            ICpeXmlBuilder xml,
            IXmlSigner signer,
            ISunatCpeClient sunat,
            IOptions<SunatCpeSettings> settings)
        {
            _factory = factory;
            _xml = xml;
            _signer = signer;
            _sunat = sunat;
            _s = settings.Value;
        }

        private static DateOnly HoyPeru() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        private static TimeOnly AhoraPeru() => TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

        // ── Borradores ──────────────────────────────────────────────────────────

        public async Task<CpeDetailDto> Guardar(long? id, CpeCreateDto dto, long usuarioId)
        {
            using var ctx = _factory.CreateDbContext();
            Cpe cpe;
            if (id.HasValue)
            {
                cpe = await ctx.Cpe.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id.Value)
                    ?? throw new AbrilException("Comprobante no encontrado.", 404);
                if (cpe.Estado is not ("BORRADOR" or "RECHAZADO"))
                    throw new AbrilException($"Un comprobante {cpe.Estado} no se puede modificar: se corrige con una nota de crédito o de débito.", 400);
                ctx.CpeItem.RemoveRange(cpe.Items);
                cpe.Items = new List<CpeItem>();
                // El tipo y la serie quedan fijos si ya tiene número: ese número ya es de esa serie.
                if (cpe.Numero.HasValue && (dto.TipoDocumento != cpe.TipoDocumento || (!string.IsNullOrWhiteSpace(dto.Serie) && dto.Serie != cpe.Serie)))
                    throw new AbrilException("Este comprobante ya tiene número: no se puede cambiar su tipo ni su serie.", 400);
            }
            else
            {
                cpe = new Cpe { CreadoPorUsuarioSistemaId = usuarioId, CreadoEn = DateTimeOffset.UtcNow };
                ctx.Cpe.Add(cpe);
            }

            if (!cpe.Numero.HasValue)
            {
                cpe.TipoDocumento = dto.TipoDocumento;
                cpe.Serie = await ResolverSerie(ctx, dto.TipoDocumento, dto.Serie);
            }

            cpe.FechaEmision = dto.FechaEmision ?? HoyPeru();
            cpe.FechaVencimiento = dto.FechaVencimiento;
            cpe.Moneda = dto.Moneda;
            cpe.TipoCambio = dto.Moneda == "USD" ? dto.TipoCambio : null;
            // SUNAT exige tipo de operación 1001 cuando la factura lleva detracción (error 3128 en el Beta).
            cpe.TipoOperacion = !string.IsNullOrWhiteSpace(dto.DetraccionCodigo) && dto.TipoOperacion == "0101" ? "1001" : dto.TipoOperacion == "1001" && string.IsNullOrWhiteSpace(dto.DetraccionCodigo) ? "0101" : dto.TipoOperacion;
            cpe.ClienteTipoDoc = dto.ClienteTipoDoc;
            cpe.ClienteNumeroDoc = (dto.ClienteNumeroDoc ?? "").Trim();
            cpe.ClienteRazon = (dto.ClienteRazon ?? "").Trim();
            cpe.ClienteDireccion = string.IsNullOrWhiteSpace(dto.ClienteDireccion) ? null : dto.ClienteDireccion.Trim();
            cpe.ClientePais = string.IsNullOrWhiteSpace(dto.ClientePais) ? null : dto.ClientePais.Trim().ToUpperInvariant();
            // Boleta a cliente sin documento: SUNAT pide "-" como número y el sistema lo llama CLIENTES VARIOS.
            if (cpe.TipoDocumento == "03" && cpe.ClienteTipoDoc == "0")
            {
                cpe.ClienteNumeroDoc = "-";
                if (string.IsNullOrWhiteSpace(cpe.ClienteRazon)) cpe.ClienteRazon = "CLIENTES VARIOS";
            }
            cpe.PreciosIncluyenIgv = dto.PreciosIncluyenIgv;
            cpe.FormaPago = dto.FormaPago == "CREDITO" ? "CREDITO" : "CONTADO";
            cpe.CuotasJson = cpe.FormaPago == "CREDITO" && dto.Cuotas is { Count: > 0 } ? JsonSerializer.Serialize(dto.Cuotas, Json) : null;
            cpe.DetraccionCodigo = string.IsNullOrWhiteSpace(dto.DetraccionCodigo) ? null : dto.DetraccionCodigo.Trim();
            cpe.DetraccionPorcentaje = cpe.DetraccionCodigo == null ? null : dto.DetraccionPorcentaje;
            cpe.DetraccionCuenta = cpe.DetraccionCodigo == null
                ? null
                : (string.IsNullOrWhiteSpace(dto.DetraccionCuenta) ? _s.CuentaDetracciones : dto.DetraccionCuenta!.Trim());
            cpe.MotivoCodigo = dto.MotivoCodigo;
            cpe.MotivoDescripcion = dto.MotivoDescripcion?.Trim();
            cpe.Observacion = dto.Observacion?.Trim();
            cpe.OrdenCompraRef = string.IsNullOrWhiteSpace(dto.OrdenCompraRef) ? null : dto.OrdenCompraRef.Trim();
            cpe.GuiaRef = string.IsNullOrWhiteSpace(dto.GuiaRef) ? null : dto.GuiaRef.Trim();
            cpe.ContratoId = dto.ContratoId;
            cpe.ProyectoId = dto.ProyectoId;

            // Notas: el comprobante que modifican debe existir y estar aceptado por SUNAT.
            cpe.RefCpeId = null; cpe.RefTipoDocumento = null; cpe.RefSerie = null; cpe.RefNumero = null;
            if (cpe.TipoDocumento is "07" or "08" && dto.RefCpeId.HasValue)
            {
                var r = await ctx.Cpe.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.RefCpeId.Value)
                    ?? throw new AbrilException("El comprobante a modificar no existe.", 400);
                if (r.Estado is not ("ACEPTADO" or "ACEPTADO_OBS"))
                    throw new AbrilException("Solo se puede emitir una nota sobre un comprobante aceptado por SUNAT.", 400);
                if (r.TipoDocumento != "01")
                    throw new AbrilException("Por ahora las notas solo pueden modificar facturas.", 400);
                cpe.RefCpeId = r.Id; cpe.RefTipoDocumento = r.TipoDocumento; cpe.RefSerie = r.Serie; cpe.RefNumero = r.Numero;
                // La nota va al mismo cliente y en la misma moneda que la factura que modifica.
                cpe.ClienteTipoDoc = r.ClienteTipoDoc; cpe.ClienteNumeroDoc = r.ClienteNumeroDoc; cpe.ClienteRazon = r.ClienteRazon;
                cpe.Moneda = r.Moneda; cpe.TipoCambio = r.TipoCambio;
            }

            var orden = 1;
            foreach (var i in dto.Items)
                cpe.Items.Add(new CpeItem
                {
                    Orden = orden++,
                    Codigo = string.IsNullOrWhiteSpace(i.Codigo) ? null : i.Codigo.Trim(),
                    Descripcion = (i.Descripcion ?? "").Trim(),
                    UnidadMedida = (i.UnidadMedida ?? "NIU").Trim().ToUpperInvariant(),
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario,
                    Descuento = i.Descuento,
                    AfectacionIgv = i.AfectacionIgv,
                    ProductoId = i.ProductoId,
                    PartidaId = i.PartidaId,
                });

            CpeCalculo.Recalcular(cpe);
            // Un RECHAZADO corregido vuelve a ser borrador de hecho: se limpia el resultado anterior pero se conserva el número.
            if (cpe.Estado == "RECHAZADO") { cpe.SunatCodigo = null; cpe.SunatDescripcion = null; }
            await ctx.SaveChangesAsync();
            return await BuildDetail(ctx, cpe.Id);
        }

        private static async Task<string> ResolverSerie(AppDbContext ctx, string tipo, string? pedida)
        {
            var q = ctx.CpeSerie.AsNoTracking().Where(s => s.TipoDocumento == tipo && s.Activa);
            if (!string.IsNullOrWhiteSpace(pedida))
            {
                var s = await q.FirstOrDefaultAsync(x => x.Serie == pedida.Trim().ToUpperInvariant());
                return s?.Serie ?? throw new AbrilException($"La serie {pedida} no existe o está inactiva.", 400);
            }
            var primera = await q.OrderBy(s => s.Serie).FirstOrDefaultAsync();
            return primera?.Serie ?? throw new AbrilException("No hay una serie activa para este tipo de documento. Créala primero.", 400);
        }

        public async Task<CpeListResponseDto> List(string? search, string? estado, string? tipo, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.Cpe.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(c => c.Estado == estado);
            if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(c => c.TipoDocumento == tipo);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var t = search.Trim().ToLower();
                q = q.Where(c => c.ClienteRazon.ToLower().Contains(t) || c.ClienteNumeroDoc.Contains(t) || (c.Serie + "-" + c.Numero).ToLower().Contains(t));
            }
            var total = await q.CountAsync();
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
            var data = await q.OrderByDescending(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new CpeListItemDto
                {
                    Id = c.Id, TipoDocumento = c.TipoDocumento, Estado = c.Estado, FechaEmision = c.FechaEmision,
                    ClienteRazon = c.ClienteRazon, ClienteNumeroDoc = c.ClienteNumeroDoc, Moneda = c.Moneda, Total = c.Total,
                    Identificador = c.Serie + "-" + (c.Numero.HasValue ? c.Numero.Value.ToString() : "borrador"),
                }).ToListAsync();
            return new CpeListResponseDto { Data = data, Page = page, PageSize = pageSize, TotalRecords = total };
        }

        public async Task<CpeDetailDto> GetById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        public async Task Eliminar(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var cpe = await ctx.Cpe.FirstOrDefaultAsync(c => c.Id == id) ?? throw new AbrilException("Comprobante no encontrado.", 404);
            if (cpe.Estado != "BORRADOR" || cpe.Numero.HasValue)
                throw new AbrilException("Solo se pueden eliminar borradores que nunca recibieron número. Un comprobante emitido se anula con baja o nota de crédito.", 400);
            ctx.Cpe.Remove(cpe);
            await ctx.SaveChangesAsync();
        }

        // ── Emisión ─────────────────────────────────────────────────────────────

        public async Task<CpeDetailDto> Emitir(long id, long usuarioId)
        {
            ExigirAmbienteHabilitado();
            using var ctx = _factory.CreateDbContext();
            var cpe = await ctx.Cpe.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new AbrilException("Comprobante no encontrado.", 404);

            // RECHAZADO se puede reenviar con el MISMO número una vez corregido el dato: SUNAT no lo registró.
            if (cpe.Estado is not ("BORRADOR" or "RECHAZADO"))
                throw new AbrilException($"Este comprobante ya fue transmitido (estado {cpe.Estado}).", 400);

            CpeCalculo.Recalcular(cpe);
            var errores = CpeValidador.Validar(cpe, HoyPeru()).Where(a => a.Nivel == "ERROR").ToList();
            if (errores.Count > 0)
                throw new AbrilException("No se puede emitir: " + string.Join(" ", errores.Select(e => e.Mensaje)), 400);

            // Correlativo: se asigna una sola vez; un reintento reenvía con el mismo número.
            if (!cpe.Numero.HasValue) await AsignarNumero(ctx, cpe);

            // Las boletas no se mandan una por una: quedan emitidas (con número y XML firmado) hasta el resumen diario.
            if (cpe.TipoDocumento == "03") return await EmitirBoleta(ctx, cpe, usuarioId);

            // Protección contra otro canal usando la misma serie: si SUNAT ya tiene este número, se salta al siguiente libre.
            if (_s.VerificarExistenciaAntesDeEmitir && cpe.Estado == "BORRADOR")
            {
                for (var salto = 0; ; salto++)
                {
                    var existe = await ExisteEnSunat(cpe);
                    if (existe == null || !existe.Existe) break;
                    Log(ctx, cpe.Id, "NUMERO_OCUPADO", false, existe.Codigo, $"{cpe.Serie}-{cpe.Numero} ya existe en SUNAT ({existe.Mensaje}); se usa el siguiente.");
                    if (salto >= 9)
                        throw new AbrilException($"Los siguientes 10 números de la serie {cpe.Serie} ya existen en SUNAT: otro sistema o el portal está usando la misma serie. Crea una serie propia para el ERP en \"Series y correlativos\" (por ejemplo F002) o ajusta el último número.", 409);
                    cpe.Numero = null;
                    await AsignarNumero(ctx, cpe);
                }
            }

            cpe.HoraEmision = AhoraPeru();
            cpe.EmitidoPorUsuarioSistemaId = usuarioId;
            var nombre = $"{_s.RucEmisor}-{cpe.TipoDocumento}-{cpe.Serie}-{cpe.Numero}";
            var (firmado, hash) = _signer.Firmar(_xml.Build(cpe));
            cpe.XmlNombreArchivo = nombre;
            cpe.XmlHashFirma = hash;
            cpe.XmlFirmado = firmado;
            cpe.EnviadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            try
            {
                var r = await _sunat.EnviarComprobanteAsync(firmado, nombre);
                cpe.CdrXml = r.CdrXml;
                cpe.SunatCodigo = r.Codigo;
                cpe.SunatDescripcion = r.Descripcion;
                cpe.RespondidoEn = DateTimeOffset.UtcNow;
                cpe.Estado = r.Aceptado ? (r.ConObservaciones ? "ACEPTADO_OBS" : "ACEPTADO") : "RECHAZADO";
                Log(ctx, cpe.Id, "ENVIO", r.Aceptado, r.Codigo, r.Descripcion);
            }
            catch (SunatFaultException ex)
            {
                // SUNAT rechazó el documento (formato, reglas): no quedó registrado y se puede corregir y reenviar con el mismo número.
                cpe.Estado = "RECHAZADO";
                cpe.SunatCodigo = ex.Codigo;
                cpe.SunatDescripcion = ex.Message;
                cpe.RespondidoEn = DateTimeOffset.UtcNow;
                Log(ctx, cpe.Id, "ENVIO", false, ex.Codigo, ex.Message);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                // Falla de red o respuesta ilegible: NO se sabe si SUNAT lo recibió. Queda con número y sin respuesta;
                // reintentar reenvía el mismo documento. [PENDIENTE] consultar getStatusCdr antes de reenviar.
                Log(ctx, cpe.Id, "ENVIO", false, null, ex.Message);
                await ctx.SaveChangesAsync();
                throw new AbrilException("No se pudo completar el envío a SUNAT: " + ex.Message + " El comprobante conserva su número; reintenta en unos minutos.", 502);
            }
            await ctx.SaveChangesAsync();
            return await BuildDetail(ctx, id);
        }

        /// <summary>Toma el siguiente correlativo de la serie con bloqueo de fila, dentro de la estrategia de ejecución de EF (hay reintentos automáticos).</summary>
        private static async Task AsignarNumero(AppDbContext ctx, Cpe cpe)
        {
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await ctx.Database.BeginTransactionAsync();
                var filas = await ctx.CpeSerie
                    .FromSql($"SELECT * FROM lb_cpe_serie WHERE tipo_documento = {cpe.TipoDocumento} AND serie = {cpe.Serie} AND activa FOR UPDATE")
                    .ToListAsync();
                var serie = filas.FirstOrDefault() ?? throw new AbrilException($"La serie {cpe.Serie} no existe o está inactiva.", 400);
                serie.UltimoNumero += 1;
                cpe.Numero = serie.UltimoNumero;
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
            });
        }

        /// <summary>Pregunta a SUNAT si el número ya existe. Devuelve null si no hay respuesta útil (timeout, error, servicio ausente): nunca bloquea.</summary>
        private async Task<CpeExistenciaResultado?> ExisteEnSunat(Cpe cpe)
        {
            try
            {
                var tarea = _sunat.ConsultarExistenciaAsync(cpe.TipoDocumento, cpe.Serie, cpe.Numero!.Value);
                if (await Task.WhenAny(tarea, Task.Delay(TimeSpan.FromSeconds(8))) != tarea)
                {
                    _ = tarea.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    return null;
                }
                return await tarea;
            }
            catch
            {
                return null;
            }
        }

        public async Task<CpeDetailDto> PedirNumeroNuevo(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var cpe = await ctx.Cpe.FirstOrDefaultAsync(c => c.Id == id) ?? throw new AbrilException("Comprobante no encontrado.", 404);
            if (cpe.Estado != "RECHAZADO" || !cpe.Numero.HasValue)
                throw new AbrilException("Solo un comprobante rechazado puede pedir un número nuevo.", 400);
            Log(ctx, cpe.Id, "RENUMERAR", true, null, $"Se liberó el número {cpe.Serie}-{cpe.Numero} (rechazado: {cpe.SunatCodigo} {cpe.SunatDescripcion}).");
            // El número rechazado queda como hueco en la serie; SUNAT no lo registró.
            cpe.Numero = null;
            cpe.Estado = "BORRADOR";
            cpe.XmlFirmado = null; cpe.XmlHashFirma = null; cpe.XmlNombreArchivo = null; cpe.CdrXml = null;
            cpe.SunatCodigo = null; cpe.SunatDescripcion = null; cpe.EnviadoEn = null; cpe.RespondidoEn = null;
            await ctx.SaveChangesAsync();
            return await BuildDetail(ctx, id);
        }

        // ── Baja ────────────────────────────────────────────────────────────────

        public async Task<CpeDetailDto> DarDeBaja(long id, CpeBajaDto dto, long usuarioId)
        {
            ExigirAmbienteHabilitado();
            if (string.IsNullOrWhiteSpace(dto.Motivo)) throw new AbrilException("Indica el motivo de la baja.", 400);
            using var ctx = _factory.CreateDbContext();
            var cpe = await ctx.Cpe.FirstOrDefaultAsync(c => c.Id == id) ?? throw new AbrilException("Comprobante no encontrado.", 404);
            if (cpe.TipoDocumento == "03")
            {
                if (cpe.Estado is "POR_ENVIAR" or "EN_RESUMEN")
                    throw new AbrilException("Esta boleta todavía no fue aceptada por SUNAT: envía o espera su resumen diario y luego podrás anularla.", 400);
                if (cpe.Estado is not ("ACEPTADO" or "ACEPTADO_OBS"))
                    throw new AbrilException("Solo se puede anular una boleta aceptada por SUNAT.", 400);
                // La anulación de una boleta viaja en el resumen diario de su fecha, con estado 3.
                cpe.BajaMotivo = dto.Motivo.Trim();
                cpe.Estado = "BAJA_SOLICITADA";
                Log(ctx, cpe.Id, "BAJA", true, null, "Anulación solicitada: irá en el próximo resumen diario de su fecha.");
                await ctx.SaveChangesAsync();
                return await BuildDetail(ctx, id);
            }
            if (cpe.Estado is not ("ACEPTADO" or "ACEPTADO_OBS"))
                throw new AbrilException("Solo se puede dar de baja un comprobante aceptado por SUNAT.", 400);

            var hoy = HoyPeru();
            var prefijo = $"RA-{hoy:yyyyMMdd}-";
            var usados = await ctx.Cpe.CountAsync(c => c.BajaIdentificador != null && c.BajaIdentificador.StartsWith(prefijo));
            var identificador = prefijo + (usados + 1).ToString(CultureInfo.InvariantCulture);

            cpe.BajaMotivo = dto.Motivo.Trim();
            var (firmado, _) = _signer.Firmar(_xml.BuildBaja(cpe, identificador, hoy));
            try
            {
                var t = await _sunat.EnviarBajaAsync(firmado, $"{_s.RucEmisor}-{identificador}");
                cpe.BajaTicket = t.Ticket;
                cpe.BajaIdentificador = identificador;
                cpe.BajaEn = DateTimeOffset.UtcNow;
                cpe.Estado = "BAJA_PENDIENTE";
                Log(ctx, cpe.Id, "BAJA", true, null, $"Ticket {t.Ticket}");
            }
            catch (SunatFaultException ex)
            {
                cpe.BajaMotivo = null;
                Log(ctx, cpe.Id, "BAJA", false, ex.Codigo, ex.Message);
                await ctx.SaveChangesAsync();
                throw new AbrilException($"SUNAT rechazó la baja: {ex.Message} ({ex.Codigo}).", 400);
            }
            await ctx.SaveChangesAsync();
            return await ConsultarBajaInterno(ctx, cpe);
        }

        public async Task<CpeDetailDto> ConsultarBaja(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var cpe = await ctx.Cpe.FirstOrDefaultAsync(c => c.Id == id) ?? throw new AbrilException("Comprobante no encontrado.", 404);
            if (cpe.TipoDocumento == "03" && cpe.ResumenId.HasValue)
            {
                await ConsultarResumen(cpe.ResumenId.Value);
                return await BuildDetail(ctx, id);
            }
            if (cpe.Estado != "BAJA_PENDIENTE" || string.IsNullOrEmpty(cpe.BajaTicket))
                throw new AbrilException("Este comprobante no tiene una baja pendiente de consultar.", 400);
            return await ConsultarBajaInterno(ctx, cpe);
        }

        private async Task<CpeDetailDto> ConsultarBajaInterno(AppDbContext ctx, Cpe cpe)
        {
            try
            {
                var r = await _sunat.ConsultarTicketAsync(cpe.BajaTicket!);
                if (!r.EnProceso)
                {
                    cpe.BajaCdrXml = r.CdrXml;
                    if (r.Aceptado) cpe.Estado = "ANULADO";
                    else
                    {
                        // La baja no prosperó: el comprobante sigue vigente.
                        cpe.Estado = cpe.SunatCodigo is { Length: > 0 } c && c != "0" ? "ACEPTADO_OBS" : "ACEPTADO";
                        cpe.BajaTicket = null;
                    }
                    Log(ctx, cpe.Id, "CONSULTA_BAJA", r.Aceptado, r.Codigo, r.Descripcion);
                }
            }
            catch (SunatFaultException ex)
            {
                Log(ctx, cpe.Id, "CONSULTA_BAJA", false, ex.Codigo, ex.Message);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                // SUNAT (sobre todo el Beta) a veces tarda o corta la consulta del ticket: la baja sigue PENDIENTE y se reintenta con "Consultar baja".
                Log(ctx, cpe.Id, "CONSULTA_BAJA", false, null, "Sin respuesta de SUNAT al consultar el ticket: " + ex.Message);
            }
            await ctx.SaveChangesAsync();
            return await BuildDetail(ctx, cpe.Id);
        }

        // ── Boletas y resumen diario ───────────────────────────────────────────

        private async Task<CpeDetailDto> EmitirBoleta(AppDbContext ctx, Cpe cpe, long usuarioId)
        {
            cpe.HoraEmision = AhoraPeru();
            cpe.EmitidoPorUsuarioSistemaId = usuarioId;
            var nombre = $"{_s.RucEmisor}-03-{cpe.Serie}-{cpe.Numero}";
            // Se firma y guarda el XML de la boleta como respaldo; a SUNAT viaja solo el resumen.
            var (firmado, hash) = _signer.Firmar(_xml.Build(cpe));
            cpe.XmlNombreArchivo = nombre;
            cpe.XmlHashFirma = hash;
            cpe.XmlFirmado = firmado;
            cpe.Estado = "POR_ENVIAR";
            Log(ctx, cpe.Id, "EMITIDA_BOLETA", true, null, "Boleta emitida; se informará a SUNAT en el resumen diario.");
            await ctx.SaveChangesAsync();
            return await BuildDetail(ctx, cpe.Id);
        }

        public async Task<List<CpePendienteResumenDto>> PendientesResumen()
        {
            using var ctx = _factory.CreateDbContext();
            var filas = await ctx.Cpe.AsNoTracking()
                .Where(c => c.TipoDocumento == "03" && (c.Estado == "POR_ENVIAR" || c.Estado == "BAJA_SOLICITADA"))
                .Select(c => new { c.FechaEmision, c.Estado, c.Total })
                .ToListAsync();
            return filas.GroupBy(f => f.FechaEmision).OrderBy(g => g.Key)
                .Select(g => new CpePendienteResumenDto
                {
                    Fecha = g.Key,
                    Cantidad = g.Count(x => x.Estado == "POR_ENVIAR"),
                    Bajas = g.Count(x => x.Estado == "BAJA_SOLICITADA"),
                    Total = g.Where(x => x.Estado == "POR_ENVIAR").Sum(x => x.Total),
                }).ToList();
        }

        public async Task<CpeResumenDto> EnviarResumen(DateOnly fecha, long usuarioId)
        {
            ExigirAmbienteHabilitado();
            var hoy = HoyPeru();
            if (fecha > hoy) throw new AbrilException("No se puede enviar el resumen de una fecha futura.", 400);

            using var ctx = _factory.CreateDbContext();
            var boletas = await ctx.Cpe
                .Where(c => c.TipoDocumento == "03" && c.FechaEmision == fecha && (c.Estado == "POR_ENVIAR" || c.Estado == "BAJA_SOLICITADA"))
                .OrderBy(c => c.Serie).ThenBy(c => c.Numero)
                .Take(500) // máximo de líneas por resumen
                .ToListAsync();
            if (boletas.Count == 0) throw new AbrilException("No hay boletas pendientes de informar para esa fecha.", 400);

            var prefijo = $"RC-{hoy:yyyyMMdd}-";
            var usados = await ctx.CpeResumen.CountAsync(r => r.Identificador.StartsWith(prefijo));
            var identificador = prefijo + (usados + 1).ToString(CultureInfo.InvariantCulture);

            var (firmado, _) = _signer.Firmar(_xml.BuildResumen(boletas, identificador, fecha, hoy));
            var res = new CpeResumen
            {
                Identificador = identificador, FechaReferencia = fecha, FechaGeneracion = hoy, Estado = "ENVIADO",
                XmlFirmado = firmado, Cantidad = boletas.Count, CreadoPorUsuarioSistemaId = usuarioId, CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.CpeResumen.Add(res);
            await ctx.SaveChangesAsync();

            try
            {
                var t = await _sunat.EnviarBajaAsync(firmado, $"{_s.RucEmisor}-{identificador}");
                res.Ticket = t.Ticket;
            }
            catch (SunatFaultException ex)
            {
                res.Estado = "RECHAZADO";
                res.SunatCodigo = ex.Codigo;
                res.SunatDescripcion = ex.Message;
                res.RespondidoEn = DateTimeOffset.UtcNow;
                await ctx.SaveChangesAsync();
                return ADto(res);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                res.Estado = "RECHAZADO";
                res.SunatDescripcion = "No se pudo enviar a SUNAT: " + ex.Message;
                res.RespondidoEn = DateTimeOffset.UtcNow;
                await ctx.SaveChangesAsync();
                throw new AbrilException("No se pudo enviar el resumen a SUNAT: " + ex.Message + " Las boletas siguen pendientes; reintenta en unos minutos.", 502);
            }

            foreach (var b in boletas)
            {
                b.ResumenId = res.Id;
                b.Estado = b.Estado == "BAJA_SOLICITADA" ? "BAJA_PENDIENTE" : "EN_RESUMEN";
                Log(ctx, b.Id, "RESUMEN", true, null, $"Incluida en {identificador} (ticket {res.Ticket}).");
            }
            await ctx.SaveChangesAsync();
            return await ConsultarResumenInterno(ctx, res);
        }

        public async Task<CpeResumenDto> ConsultarResumen(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var res = await ctx.CpeResumen.FirstOrDefaultAsync(r => r.Id == id) ?? throw new AbrilException("Resumen no encontrado.", 404);
            return await ConsultarResumenInterno(ctx, res);
        }

        public async Task<List<CpeResumenDto>> ListResumenes()
        {
            using var ctx = _factory.CreateDbContext();
            var filas = await ctx.CpeResumen.AsNoTracking().OrderByDescending(r => r.Id).Take(100).ToListAsync();
            return filas.Select(ADto).ToList();
        }

        private async Task<CpeResumenDto> ConsultarResumenInterno(AppDbContext ctx, CpeResumen res)
        {
            if (res.Estado != "ENVIADO" || string.IsNullOrEmpty(res.Ticket)) return ADto(res);
            try
            {
                var r = await _sunat.ConsultarTicketAsync(res.Ticket);
                if (r.EnProceso) return ADto(res);

                res.CdrXml = r.CdrXml;
                res.SunatCodigo = r.Codigo;
                res.SunatDescripcion = r.Descripcion;
                res.RespondidoEn = DateTimeOffset.UtcNow;
                var boletas = await ctx.Cpe.Where(c => c.ResumenId == res.Id).ToListAsync();
                if (r.Aceptado)
                {
                    res.Estado = "ACEPTADO";
                    foreach (var b in boletas)
                        b.Estado = b.Estado == "BAJA_PENDIENTE" ? "ANULADO" : "ACEPTADO";
                }
                else
                {
                    // SUNAT no aceptó el resumen: las boletas vuelven a quedar pendientes para corregir y reenviar.
                    res.Estado = "RECHAZADO";
                    foreach (var b in boletas)
                    {
                        b.Estado = b.Estado == "BAJA_PENDIENTE" ? "ACEPTADO" : "POR_ENVIAR";
                        b.ResumenId = null;
                    }
                }
                foreach (var b in boletas) Log(ctx, b.Id, "CONSULTA_RESUMEN", r.Aceptado, r.Codigo, r.Descripcion);
            }
            catch (SunatFaultException ex)
            {
                res.SunatCodigo = ex.Codigo;
                res.SunatDescripcion = ex.Message;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                // Sin respuesta (el Beta suele cortar esta consulta): el resumen sigue ENVIADO y se reintenta.
                res.SunatDescripcion = "Sin respuesta de SUNAT al consultar el ticket: " + ex.Message;
            }
            await ctx.SaveChangesAsync();
            return ADto(res);
        }

        private static CpeResumenDto ADto(CpeResumen r) => new()
        {
            Id = r.Id, Identificador = r.Identificador, FechaReferencia = r.FechaReferencia, Estado = r.Estado, Ticket = r.Ticket,
            Cantidad = r.Cantidad, SunatCodigo = r.SunatCodigo, SunatDescripcion = r.SunatDescripcion, CreadoEn = r.CreadoEn,
        };

        // ── Archivos ────────────────────────────────────────────────────────────

        public async Task<string> ObtenerXml(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return (await ctx.Cpe.AsNoTracking().Where(c => c.Id == id).Select(c => c.XmlFirmado).FirstOrDefaultAsync())
                ?? throw new AbrilException("Este comprobante todavía no tiene XML firmado.", 404);
        }

        public async Task<string> ObtenerCdr(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return (await ctx.Cpe.AsNoTracking().Where(c => c.Id == id).Select(c => c.CdrXml).FirstOrDefaultAsync())
                ?? throw new AbrilException("Este comprobante todavía no tiene constancia (CDR) de SUNAT.", 404);
        }

        // ── Series ──────────────────────────────────────────────────────────────

        public async Task<List<CpeSerieDto>> ListSeries()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.CpeSerie.AsNoTracking().OrderBy(s => s.TipoDocumento).ThenBy(s => s.Serie)
                .Select(s => new CpeSerieDto { Id = s.Id, TipoDocumento = s.TipoDocumento, Serie = s.Serie, UltimoNumero = s.UltimoNumero, Activa = s.Activa })
                .ToListAsync();
        }

        public async Task<CpeSerieDto> GuardarSerie(CpeSerieUpsertDto dto)
        {
            var serie = (dto.Serie ?? "").Trim().ToUpperInvariant();
            var prefijoOk = dto.TipoDocumento switch { "01" => serie.StartsWith('F'), "07" or "08" => serie.StartsWith('F'), "03" => serie.StartsWith('B'), _ => false };
            if (serie.Length != 4 || !prefijoOk)
                throw new AbrilException("La serie debe tener 4 caracteres: empieza con F (facturas y sus notas, por ejemplo F001 o FC01) o con B (boletas, por ejemplo B001).", 400);
            if (dto.UltimoNumero < 0) throw new AbrilException("El último número no puede ser negativo.", 400);

            using var ctx = _factory.CreateDbContext();
            var s = await ctx.CpeSerie.FirstOrDefaultAsync(x => x.TipoDocumento == dto.TipoDocumento && x.Serie == serie);
            if (s == null)
            {
                s = new CpeSerie { TipoDocumento = dto.TipoDocumento, Serie = serie, CreadoEn = DateTimeOffset.UtcNow };
                ctx.CpeSerie.Add(s);
            }
            else
            {
                // El correlativo nunca retrocede por debajo de lo ya emitido: sería repetir números ante SUNAT.
                var maxEmitido = await ctx.Cpe.Where(c => c.TipoDocumento == s.TipoDocumento && c.Serie == s.Serie && c.Numero != null)
                    .Select(c => (long?)c.Numero).MaxAsync() ?? 0;
                if (dto.UltimoNumero < maxEmitido)
                    throw new AbrilException($"Ya se emitió hasta el {maxEmitido} en esta serie: el último número no puede ser menor.", 400);
            }
            s.UltimoNumero = dto.UltimoNumero;
            s.Activa = dto.Activa;
            await ctx.SaveChangesAsync();
            return new CpeSerieDto { Id = s.Id, TipoDocumento = s.TipoDocumento, Serie = s.Serie, UltimoNumero = s.UltimoNumero, Activa = s.Activa };
        }

        // ── Internos ────────────────────────────────────────────────────────────

        private void ExigirAmbienteHabilitado()
        {
            if (_s.EsProduccion && !_s.PermitirProduccion)
                throw new AbrilException("Este servidor está configurado en Producción pero bloqueado. Para emitir de verdad activa SunatCpe:PermitirProduccion; para probar usa Ambiente Beta.", 403);
            if (_s.EsProduccion && (string.IsNullOrWhiteSpace(_s.Ruc) || string.IsNullOrWhiteSpace(_s.SolUsuario) || string.IsNullOrWhiteSpace(_s.SolClave)))
                throw new AbrilException("Faltan el RUC o las credenciales SOL de SunatCpe en la configuración.", 500);
        }

        private static void Log(AppDbContext ctx, long cpeId, string operacion, bool ok, string? codigo, string? mensaje) =>
            ctx.CpeEnvio.Add(new CpeEnvio { CpeId = cpeId, Operacion = operacion, Ambiente = "-", Ok = ok, Codigo = codigo, Mensaje = mensaje, CreadoEn = DateTimeOffset.UtcNow });

        private async Task<CpeDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var c = await ctx.Cpe.AsNoTracking().Include(x => x.Items).Include(x => x.Contrato).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AbrilException("Comprobante no encontrado.", 404);
            var avisos = c.Estado is "BORRADOR" or "RECHAZADO" ? CpeValidador.Validar(c, HoyPeru()) : new List<CpeAvisoDto>();
            return new CpeDetailDto
            {
                Id = c.Id, TipoDocumento = c.TipoDocumento, Serie = c.Serie, Numero = c.Numero, Identificador = c.Identificador, Estado = c.Estado,
                FechaEmision = c.FechaEmision, FechaVencimiento = c.FechaVencimiento, Moneda = c.Moneda, TipoCambio = c.TipoCambio, TipoOperacion = c.TipoOperacion,
                ClienteTipoDoc = c.ClienteTipoDoc, ClienteNumeroDoc = c.ClienteNumeroDoc, ClienteRazon = c.ClienteRazon, ClienteDireccion = c.ClienteDireccion, ClientePais = c.ClientePais,
                TotalGravado = c.TotalGravado, TotalExonerado = c.TotalExonerado, TotalInafecto = c.TotalInafecto, TotalExportacion = c.TotalExportacion,
                TotalDescuentos = c.TotalDescuentos, Igv = c.Igv, Total = c.Total, PreciosIncluyenIgv = c.PreciosIncluyenIgv,
                FormaPago = c.FormaPago, Cuotas = CpeValidador.LeerCuotas(c.CuotasJson),
                DetraccionCodigo = c.DetraccionCodigo, DetraccionPorcentaje = c.DetraccionPorcentaje, DetraccionMonto = c.DetraccionMonto, DetraccionCuenta = c.DetraccionCuenta,
                RefCpeId = c.RefCpeId, RefIdentificador = c.RefSerie == null ? null : $"{c.RefSerie}-{c.RefNumero}",
                MotivoCodigo = c.MotivoCodigo, MotivoDescripcion = c.MotivoDescripcion, Observacion = c.Observacion,
                OrdenCompraRef = c.OrdenCompraRef, GuiaRef = c.GuiaRef, ContratoId = c.ContratoId, ProyectoId = c.ProyectoId,
                SunatCodigo = c.SunatCodigo, SunatDescripcion = c.SunatDescripcion, EnviadoEn = c.EnviadoEn, RespondidoEn = c.RespondidoEn,
                BajaMotivo = c.BajaMotivo, BajaTicket = c.BajaTicket,
                TieneXml = c.XmlFirmado != null, TieneCdr = c.CdrXml != null, Ambiente = _s.Ambiente,
                EmisorRuc = _s.RucEmisor, EmisorRazonSocial = _s.RazonSocialEmisor,
                EmisorDireccion = string.IsNullOrWhiteSpace(_s.Direccion) ? null : string.Join(" - ", new[] { _s.Direccion, _s.Distrito, _s.Provincia, _s.Departamento }.Where(x => !string.IsNullOrWhiteSpace(x))),
                XmlHashFirma = c.XmlHashFirma, TotalEnLetras = NumeroALetras.Monto(c.Total, c.Moneda),
                ContratoCodigo = c.Contrato != null ? c.Contrato.Codigo : null,
                Items = c.Items.OrderBy(i => i.Orden).Select(i => new CpeItemDetalleDto
                {
                    Codigo = i.Codigo, Descripcion = i.Descripcion, UnidadMedida = i.UnidadMedida, Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario, Descuento = i.Descuento, AfectacionIgv = i.AfectacionIgv,
                    ProductoId = i.ProductoId, PartidaId = i.PartidaId, ValorUnitario = i.ValorUnitario, ValorVenta = i.ValorVenta, Igv = i.Igv,
                }).ToList(),
                Avisos = avisos,
            };
        }
    }
}
