using System.Security.Cryptography;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class ConsumoService : IConsumoService
{
    private const string PartidaSsoma = "SEGURIDAD Y SALUD OCUPACIONAL";

    private readonly IConsumoRepository _repo;
    private readonly IEstandarizacionService _estandarizacion;

    public ConsumoService(IConsumoRepository repo, IEstandarizacionService estandarizacion)
    {
        _repo = repo;
        _estandarizacion = estandarizacion;
    }

    /// <summary>
    /// Importa el Kardex de materiales (Movimiento/Partida de Control) de un proyecto. Acepta el
    /// archivo ACUMULADO completo en cada subida (no solo el delta semanal): se queda solo con los
    /// egresos (Movimiento "E*") de la partida SSOMA, y compara línea por línea contra lo ya
    /// guardado usando la guía de origen como identidad — así una regularización del ERP (cambia
    /// cantidad/precio de una guía ya cargada) actualiza esa línea en vez de duplicarla, y una guía
    /// que deja de aparecer en el acumulado se da de baja (activo=false) en vez de quedar huérfana.
    /// </summary>
    public async Task<ImportConsumoResultDto> ImportarS10Async(IFormFile archivo, int projectId, int usuarioId)
    {
        byte[] contenidoBytes;
        using (var ms = new MemoryStream())
        {
            await archivo.CopyToAsync(ms);
            contenidoBytes = ms.ToArray();
        }
        var hash = Convert.ToHexString(SHA256.HashData(contenidoBytes));

        var lineasRaw = ParsearKardex(contenidoBytes, archivo.FileName);
        if (lineasRaw.Count == 0)
            throw new AbrilException(
                "No se encontraron egresos (Movimiento \"E*\") de la partida \"Seguridad y Salud Ocupacional\" en el archivo. " +
                "Verifica que sea el Kardex correcto del proyecto.", 400);

        var fechaMin = lineasRaw.Min(l => l.FechaGuia);
        var fechaMax = lineasRaw.Max(l => l.FechaGuia);

        // ─── Asignar ocurrencia (desambigua repeticiones exactas de guía+recurso+fecha+movimiento) ───
        var lineasConOcurrencia = lineasRaw
            .OrderBy(l => l.NroGuia).ThenBy(l => l.RecursoCrudo).ThenBy(l => l.FechaGuia)
                .ThenBy(l => l.Movimiento).ThenBy(l => l.Cantidad).ThenBy(l => l.PrecioUnitario)
            .GroupBy(l => (l.NroGuia, l.RecursoCrudo, l.FechaGuia, l.Movimiento))
            .SelectMany(g => g.Select((l, i) => (Linea: l, Ocurrencia: i + 1)))
            .ToList();

        // ─── Diff contra lo ya guardado (identidad = guía + recurso + fecha + movimiento + ocurrencia) ───
        var existentes = await _repo.ObtenerLineasActivasConGuiaPorProyectoAsync(projectId);
        var existentesPorClave = existentes.ToDictionary(ClaveDe);

        var carga = new SsConsumoCarga
        {
            ProjectId = projectId,
            NombreArchivo = archivo.FileName,
            HashArchivo = hash,
            FechaMin = fechaMin,
            FechaMax = fechaMax,
            Estado = "ACTIVA",
            SubidoPor = usuarioId,
            CreadoEn = DateTimeOffset.UtcNow
        };
        carga = await _repo.CrearCargaAsync(carga);

        var nuevas = new List<SsConsumoLinea>();
        var actualizaciones = new List<(long LineaId, decimal Cantidad, decimal PrecioUnitario, decimal PrecioTotal, DateOnly FechaGuia)>();
        var clavesVistas = new HashSet<string>();
        var sinCambio = 0;

        foreach (var (linea, ocurrencia) in lineasConOcurrencia)
        {
            var clave = ClaveDe(linea.NroGuia, linea.RecursoCrudo, linea.FechaGuia, linea.Movimiento, ocurrencia);
            clavesVistas.Add(clave);

            if (existentesPorClave.TryGetValue(clave, out var existente))
            {
                if (existente.Cantidad != linea.Cantidad || existente.PrecioUnitario != linea.PrecioUnitario)
                    actualizaciones.Add((existente.Id, linea.Cantidad, linea.PrecioUnitario, linea.PrecioTotal, linea.FechaGuia));
                else
                    sinCambio++;
            }
            else
            {
                nuevas.Add(new SsConsumoLinea
                {
                    CargaId = carga.Id,
                    ProjectId = projectId,
                    RecursoCrudo = linea.RecursoCrudo,
                    NroGuia = linea.NroGuia,
                    Movimiento = linea.Movimiento,
                    PartidaControl = linea.PartidaControl,
                    Ocurrencia = ocurrencia,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = linea.PrecioUnitario,
                    PrecioTotal = linea.PrecioTotal,
                    FechaGuia = linea.FechaGuia,
                    Estandarizado = false,
                    Activo = true,
                    CreadoEn = DateTimeOffset.UtcNow
                });
            }
        }

        var idsDarDeBaja = existentesPorClave
            .Where(kv => !clavesVistas.Contains(kv.Key))
            .Select(kv => kv.Value.Id)
            .ToList();
        var motivoBaja = $"No aparece en la carga acumulada del {DateOnly.FromDateTime(DateTime.UtcNow):dd/MM/yyyy} ({archivo.FileName}).";

        await _repo.AplicarDiffCargaAsync(nuevas, actualizaciones, idsDarDeBaja, motivoBaja);

        var totalLineas = nuevas.Count + actualizaciones.Count + sinCambio;
        await _repo.ActualizarResumenCargaAsync(carga.Id, totalLineas, nuevas.Count, actualizaciones.Count, idsDarDeBaja.Count);

        var resultadoEstand = nuevas.Count > 0
            ? await _estandarizacion.EstandarizarCargaAsync(carga.Id)
            : new EstandarizacionLoteResultDto();

        var advertencias = new List<string>();
        if (idsDarDeBaja.Count > 0)
            advertencias.Add($"{idsDarDeBaja.Count} línea(s) ya cargadas no aparecen en este archivo y se dieron de baja (posible regularización/anulación en el ERP). Revísalas en el historial de cargas.");
        if (resultadoEstand.ConError > 0)
            advertencias.Add($"{resultadoEstand.ConError} línea(s) no se pudieron procesar por un error puntual (ej. corte breve de conexión). Usa el botón \"Re-estandarizar\" de esta carga para reintentarlas.");

        return new ImportConsumoResultDto
        {
            CargaId = carga.Id,
            NombreArchivo = archivo.FileName,
            TotalLineas = totalLineas,
            LineasNuevas = nuevas.Count,
            LineasActualizadas = actualizaciones.Count,
            LineasEliminadas = idsDarDeBaja.Count,
            LineasSinCambio = sinCambio,
            LineasEstandarizadas = resultadoEstand.AutoResueltas,
            LineasAutoRechazadas = resultadoEstand.AutoRechazadas,
            LineasPendientes = resultadoEstand.EnRevision,
            LineasSinMatch = resultadoEstand.SinMatch,
            Estado = "ACTIVA",
            Advertencias = advertencias
        };
    }

    public async Task<List<ConsumoCargaResumenDto>> ObtenerCargasAsync(int projectId) =>
        await _repo.ObtenerCargasPorProyectoAsync(projectId);

    public async Task<int> AsignarHitosAsync(int projectId) =>
        await _repo.AsignarHitosPorFechaAsync(projectId);

    private static string ClaveDe(SsConsumoLinea l) => ClaveDe(l.NroGuia, l.RecursoCrudo, l.FechaGuia, l.Movimiento, l.Ocurrencia);

    private static string ClaveDe(string? nroGuia, string recurso, DateOnly fecha, string? movimiento, int ocurrencia) =>
        $"{nroGuia}|{recurso}|{fecha:O}|{movimiento}|{ocurrencia}";

    // ─── Parser Kardex (Movimiento / Nro. Guía / Partida de Control) ──────────

    private record LineaRaw(
        string RecursoCrudo, decimal Cantidad, decimal PrecioUnitario, decimal PrecioTotal, DateOnly FechaGuia,
        string NroGuia, string Movimiento, string? PartidaControl);

    private static List<LineaRaw> ParsearKardex(byte[] bytes, string nombreArchivo)
    {
        using var stream = new MemoryStream(bytes);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        int headerRow = EncontrarFilaEncabezado(ws);
        if (headerRow == 0)
            throw new AbrilException($"No se encontró la fila de encabezados en '{nombreArchivo}'. Columnas requeridas: Movimiento, Nro. Guía, Partida de Control, Fecha Guía, Recurso, Cantidad, Precio.", 400);

        var cols = MapearColumnas(ws, headerRow);
        ValidarColumnasRequeridas(cols, nombreArchivo);

        var lineas = new List<LineaRaw>();
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var recurso = ws.Cell(r, cols["recurso"]).GetString().Trim();
            if (string.IsNullOrWhiteSpace(recurso)) continue;
            if (recurso.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase)) continue;

            var movimiento = ws.Cell(r, cols["movimiento"]).GetString().Trim();
            if (!movimiento.StartsWith("E", StringComparison.OrdinalIgnoreCase)) continue; // solo egresos

            var partida = ws.Cell(r, cols["partida"]).GetString().Trim();
            if (!NormalizarTexto(partida).Contains(PartidaSsoma)) continue; // solo partida SSOMA

            var nroGuia = ws.Cell(r, cols["guia"]).GetString().Trim();

            if (!TryLeerDecimal(ws.Cell(r, cols["cantidad"]), out var cantidadRaw)) continue;
            if (!TryLeerDecimal(ws.Cell(r, cols["precio"]), out var precio)) continue;
            if (!TryLeerFecha(ws.Cell(r, cols["fecha"]), out var fecha)) continue;

            var cantidad = Math.Abs(cantidadRaw); // los egresos vienen en negativo en el Kardex
            decimal precioTotal;
            if (cols.TryGetValue("preciototal", out var ptCol))
            {
                if (!TryLeerDecimal(ws.Cell(r, ptCol), out precioTotal) || precioTotal == 0)
                    precioTotal = cantidad * precio;
                else
                    precioTotal = Math.Abs(precioTotal);
            }
            else
            {
                precioTotal = cantidad * precio;
            }

            lineas.Add(new LineaRaw(recurso, cantidad, precio, precioTotal, fecha, nroGuia, movimiento, partida));
        }

        return lineas;
    }

    private static int EncontrarFilaEncabezado(IXLWorksheet ws)
    {
        int lastRow = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 1, 30);
        for (int r = 1; r <= lastRow; r++)
        {
            for (int c = 1; c <= 20; c++)
            {
                var val = ws.Cell(r, c).GetString().Trim().ToUpperInvariant();
                if (val.Contains("RECURSO") || val.Contains("DESCRIPCION") || val.Contains("MATERIAL"))
                    return r;
            }
        }
        return 0;
    }

    private static Dictionary<string, int> MapearColumnas(IXLWorksheet ws, int headerRow)
    {
        var map = new Dictionary<string, int>();
        int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 20;

        for (int c = 1; c <= lastCol; c++)
        {
            var val = NormalizarTexto(ws.Cell(headerRow, c).GetString());

            if ((val.Contains("RECURSO") || val.Contains("DESCRIPCION") || val.Contains("MATERIAL")) && !map.ContainsKey("recurso"))
                map["recurso"] = c;
            // "Fecha Guía" debe mapear a fecha; "Nro. Guía" (sin "FECHA") debe mapear a guia. El
            // orden de estas dos condiciones importa: si se invierte, "Nro. Guía" (que también
            // contiene "GUIA") se cuela como columna de fecha y el parseo de fecha falla en
            // silencio para todas las filas.
            else if (val.Contains("FECHA") && !map.ContainsKey("fecha"))
                map["fecha"] = c;
            else if (val.Contains("GUIA") && !map.ContainsKey("guia"))
                map["guia"] = c;
            else if (val.Contains("MOVIMIENTO") && !map.ContainsKey("movimiento"))
                map["movimiento"] = c;
            else if (val.Contains("PARTIDA") && !map.ContainsKey("partida"))
                map["partida"] = c;
            else if ((val == "CANTIDAD" || val.Contains("CANT")) && !map.ContainsKey("cantidad"))
                map["cantidad"] = c;
            else if ((val.Contains("PRECIO") && !val.Contains("TOTAL")) && !map.ContainsKey("precio"))
                map["precio"] = c;
            else if ((val.Contains("TOTAL") || val == "IMPORTE") && !map.ContainsKey("preciototal"))
                map["preciototal"] = c;
        }
        return map;
    }

    private static void ValidarColumnasRequeridas(Dictionary<string, int> cols, string archivo)
    {
        var requeridas = new[] { "recurso", "cantidad", "precio", "fecha", "movimiento", "guia", "partida" };
        var faltantes = requeridas.Where(r => !cols.ContainsKey(r)).ToList();
        if (faltantes.Count > 0)
            throw new AbrilException($"Archivo '{archivo}': no se encontraron columnas {string.Join(", ", faltantes)}. Se requiere el Kardex con Movimiento, Nro. Guía, Partida de Control, Fecha Guía, Recurso, Cantidad y Precio.", 400);
    }

    private static string NormalizarTexto(string s) =>
        s.Trim().ToUpperInvariant()
            .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U");

    /// <summary>
    /// Lee un valor numérico de la celda sin pasar por texto cuando es posible: el Kardex real
    /// guarda Cantidad/Precio como celdas numéricas nativas con punto decimal (549.161), no como
    /// texto con coma peruana. Tratarlas como texto y aplicarles el reemplazo de "," por "."
    /// destruye el separador decimal real (549.161 → 549161, un precio 1000 veces inflado).
    /// El parseo de texto con coma solo aplica a exportaciones que guardan el número como texto.
    /// </summary>
    private static bool TryLeerDecimal(IXLCell cell, out decimal result)
    {
        if (cell.DataType == XLDataType.Number)
        {
            result = cell.GetValue<decimal>();
            return true;
        }
        return ParseDecimal(cell.GetString().Trim(), out result);
    }

    private static bool ParseDecimal(string s, out decimal result)
    {
        // Exportaciones en texto con coma como separador decimal en Perú: "237,29" → 237.29
        s = s.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Lee la fecha desde el valor nativo de la celda cuando es posible, en vez de confiar en el
    /// texto formateado (GetString): un Kardex con la columna Fecha Guía como celda de fecha real
    /// pero con un formato numérico regional que ClosedXML no renderiza como "dd/MM/yyyy" (ni como
    /// número puro) hacía fallar el parseo de TODAS las filas en silencio — quedaba "sin cambio" en
    /// realidad "sin ninguna fila válida", con 0 líneas y el error genérico de "no se encontraron
    /// egresos" aunque Movimiento y Partida estuvieran perfectos.
    /// </summary>
    private static bool TryLeerFecha(IXLCell cell, out DateOnly result)
    {
        if (cell.DataType == XLDataType.DateTime)
        {
            result = DateOnly.FromDateTime(cell.GetDateTime());
            return true;
        }
        if (cell.DataType == XLDataType.Number)
        {
            var serial = cell.GetValue<double>();
            if (serial > 1000)
            {
                try { result = DateOnly.FromDateTime(DateTime.FromOADate(serial)); return true; }
                catch { /* cae al parseo de texto */ }
            }
        }
        return ParseFecha(cell.GetString().Trim(), out result);
    }

    private static bool ParseFecha(string s, out DateOnly result)
    {
        if (DateOnly.TryParseExact(s, ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy"],
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result))
            return true;

        if (double.TryParse(s, out var serial) && serial > 1000)
        {
            try { result = DateOnly.FromDateTime(DateTime.FromOADate(serial)); return true; }
            catch { }
        }
        result = default;
        return false;
    }
}
