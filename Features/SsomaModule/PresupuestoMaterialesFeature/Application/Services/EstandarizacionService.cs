using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class EstandarizacionService : IEstandarizacionService
{
    // Umbral para auto-estandarizar sin revisión humana
    private const decimal UMBRAL_AUTO = 0.80m;
    // Umbral mínimo para enviar a revisión (debajo = sin match)
    private const decimal UMBRAL_REVISION = 0.55m;

    private readonly IConsumoRepository _consumoRepo;
    private readonly IEstandarizacionRepository _estandarizacionRepo;

    public EstandarizacionService(IConsumoRepository consumoRepo, IEstandarizacionRepository estandarizacionRepo)
    {
        _consumoRepo = consumoRepo;
        _estandarizacionRepo = estandarizacionRepo;
    }

    public async Task<EstandarizacionLoteResultDto> EstandarizarCargaAsync(int cargaId)
    {
        var lineas = await _consumoRepo.ObtenerLineasSinEstandarizarAsync(cargaId);
        var resultado = new EstandarizacionLoteResultDto { TotalProcesadas = lineas.Count };
        var detalles = new List<EstandarizacionLineaDto>();
        var cambios = new List<ResultadoLineaParaGuardar>(lineas.Count);

        // Precarga única del lote entero: antes las Etapas 0-2 hacían 1 consulta a la base POR
        // LÍNEA (con miles de líneas eso es decenas de miles de idas y vueltas, secuenciales, y
        // cualquier corte breve de conexión a mitad de camino dejaba el resto sin procesar). El
        // catálogo/alias es chico comparado al volumen de líneas, así que se trae una sola vez y
        // de ahí en más esas 3 etapas son lookups en memoria. Solo la Etapa 4 (fuzzy) sigue yendo
        // a la base por línea porque depende de pg_trgm.
        var rechazosConocidos = await _estandarizacionRepo.ObtenerRechazosConocidosAsync();
        var aliasPorTexto = await _estandarizacionRepo.ObtenerAliasesActivosAsync();
        var itemPorNombre = await _estandarizacionRepo.ObtenerNombresItemActivosAsync();

        int autoResueltas = 0, autoRechazadas = 0, enRevision = 0, sinMatch = 0, errores = 0;

        EstandarizacionProgreso.Iniciar(cargaId, lineas.Count);
        try
        {
            foreach (var linea in lineas)
            {
                try
                {
                    var textoNorm = TextoNormalizador.Normalizar(linea.RecursoCrudo);
                    var (detalle, cambio) = await ProcesarLineaAsync(linea.Id, linea.RecursoCrudo, textoNorm, rechazosConocidos, aliasPorTexto, itemPorNombre);
                    detalles.Add(detalle);
                    cambios.Add(cambio);

                    switch (detalle.Resultado)
                    {
                        case "AUTO_ALIAS":
                        case "AUTO_EXACTO":
                        case "AUTO_FUZZY":
                            autoResueltas++;
                            break;
                        case "AUTO_RECHAZADO":
                            autoRechazadas++;
                            break;
                        case "REVISION":
                            enRevision++;
                            break;
                        default:
                            sinMatch++;
                            break;
                    }
                }
                catch (Exception)
                {
                    // Una línea con problema (ej. corte breve de conexión a la base) no debe tumbar el
                    // lote completo dejando miles de líneas restantes sin procesar y sin aviso — esta
                    // línea queda tal cual (sin estandarizar, sin estado) y el botón "Re-estandarizar"
                    // la vuelve a intentar después, en vez de perder todo el progreso ya hecho.
                    errores++;
                }
                finally
                {
                    EstandarizacionProgreso.Avanzar(cargaId);
                }
            }

            // Persistir TODO el lote en una sola operación — antes cada línea hacía su propio
            // SaveChanges (una conexión/round-trip por línea), que con miles de líneas era el
            // verdadero cuello de botella (mucho más que las consultas de búsqueda ya cacheadas).
            await _consumoRepo.AplicarResultadosEstandarizacionAsync(cambios);
        }
        finally
        {
            EstandarizacionProgreso.Finalizar(cargaId);
        }

        resultado.AutoResueltas = autoResueltas;
        resultado.AutoRechazadas = autoRechazadas;
        resultado.EnRevision = enRevision;
        resultado.SinMatch = sinMatch;
        resultado.ConError = errores;
        resultado.Detalles = detalles;

        // "Pendientes" incluye sin match: ambas terminan con estado PENDIENTE y aparecen juntas en
        // Revisión de Materiales — si solo contara enRevision, el historial de cargas mostraría
        // menos pendientes de los que en verdad hay que resolver.
        await _consumoRepo.ActualizarContadoresCargaAsync(cargaId, autoResueltas, enRevision + sinMatch);
        return resultado;
    }

    public (int Procesadas, int Total)? ObtenerProgreso(int cargaId) => EstandarizacionProgreso.Obtener(cargaId);

    private async Task<(EstandarizacionLineaDto Detalle, ResultadoLineaParaGuardar Cambio)> ProcesarLineaAsync(
        long lineaId, string recursoCrudo, string textoNorm,
        HashSet<string> rechazosConocidos, Dictionary<string, MatchResult> aliasPorTexto, Dictionary<string, MatchResult> itemPorNombre)
    {
        // Etapa 0: ¿Ya se rechazó este mismo texto antes en Revisión? No volver a preguntar —
        // se aprende igual que un alias de ítem, pero para "esto no es SSOMA" (ver CrearAliasRechazoAsync).
        if (rechazosConocidos.Contains(textoNorm))
        {
            var detalleRechazo = new EstandarizacionLineaDto { LineaId = lineaId, RecursoCrudo = recursoCrudo, Resultado = "AUTO_RECHAZADO" };
            var cambioRechazo = new ResultadoLineaParaGuardar(lineaId, false, null, false, "ALIAS_RECHAZO", null, "RECHAZADO", 1);
            return (detalleRechazo, cambioRechazo);
        }

        // Etapa 1: Alias exacto (lookup en memoria — diccionario de aprendizaje)
        if (aliasPorTexto.TryGetValue(textoNorm, out var match))
        {
            var cambio = new ResultadoLineaParaGuardar(lineaId, true, match.ItemId, match.PerteneceSsoma, "ALIAS", 1.0m, null, match.FactorConversion);
            return (ToDetalle(lineaId, recursoCrudo, "AUTO_ALIAS", match), cambio);
        }

        // Etapa 2: Nombre normalizado exacto en catálogo
        if (itemPorNombre.TryGetValue(textoNorm, out match))
        {
            // Aprender: guardar alias para próxima vez, y cachearlo ya mismo por si el resto del
            // lote trae más líneas con este mismo texto crudo exacto (muy común en un Kardex).
            await _estandarizacionRepo.CrearAliasAsync(recursoCrudo, textoNorm, match.ItemId, "FUZZY_CONFIRMADO", 1.0m);
            aliasPorTexto[textoNorm] = match;
            var cambio = new ResultadoLineaParaGuardar(lineaId, true, match.ItemId, match.PerteneceSsoma, "EXACTO", 1.0m, null, 1);
            return (ToDetalle(lineaId, recursoCrudo, "AUTO_EXACTO", match), cambio);
        }

        // Etapa 3: Intentar sin talla ni dimensión (expansión de búsqueda)
        var (sinTalla, _) = TextoNormalizador.ExtraerTalla(textoNorm);
        var (sinDim, _) = TextoNormalizador.ExtraerDimension(sinTalla);
        if (sinDim != textoNorm && itemPorNombre.TryGetValue(sinDim, out match))
        {
            await _estandarizacionRepo.CrearAliasAsync(recursoCrudo, textoNorm, match.ItemId, "FUZZY_CONFIRMADO", 0.95m);
            aliasPorTexto[textoNorm] = match;
            var cambio = new ResultadoLineaParaGuardar(lineaId, true, match.ItemId, match.PerteneceSsoma, "EXACTO_SIN_TALLA", 0.95m, null, 1);
            return (ToDetalle(lineaId, recursoCrudo, "AUTO_EXACTO", match, 0.95m), cambio);
        }

        // Etapa 4: Trigram con umbral alto → auto-estandariza (única etapa que sigue yendo a la
        // base por línea: depende de pg_trgm, no se puede precargar en memoria).
        var candidatos = await _estandarizacionRepo.BuscarPorTrigramAsync(textoNorm, UMBRAL_REVISION);
        if (candidatos.Count > 0)
        {
            var mejor = candidatos[0];
            if (mejor.Score >= UMBRAL_AUTO)
            {
                // Score alto: auto-estandarizar y aprender
                await _estandarizacionRepo.CrearAliasAsync(recursoCrudo, textoNorm, mejor.ItemId, "FUZZY_CONFIRMADO", mejor.Score);
                aliasPorTexto[textoNorm] = mejor;
                var cambio = new ResultadoLineaParaGuardar(lineaId, true, mejor.ItemId, mejor.PerteneceSsoma, "FUZZY", mejor.Score, null, 1);
                return (ToDetalle(lineaId, recursoCrudo, "AUTO_FUZZY", mejor), cambio);
            }
            else
            {
                // Score medio: enviar a revisión humana (se guarda el mejor match como sugerencia)
                var cambio = new ResultadoLineaParaGuardar(lineaId, false, mejor.ItemId, mejor.PerteneceSsoma, "FUZZY", mejor.Score, "PENDIENTE", 1);
                return (ToDetalle(lineaId, recursoCrudo, "REVISION", mejor), cambio);
            }
        }

        // Etapa 5: Sin match — igual va a Revisión (sin sugerencia), para asignarle un ítem a mano.
        var detalleSinMatch = new EstandarizacionLineaDto { LineaId = lineaId, RecursoCrudo = recursoCrudo, Resultado = "SIN_MATCH" };
        var cambioSinMatch = new ResultadoLineaParaGuardar(lineaId, false, null, true, "SIN_MATCH", null, "PENDIENTE", 1);
        return (detalleSinMatch, cambioSinMatch);
    }

    private static EstandarizacionLineaDto ToDetalle(long lineaId, string recursoCrudo, string resultado, MatchResult match, decimal? scoreOverride = null) =>
        new()
        {
            LineaId = lineaId,
            RecursoCrudo = recursoCrudo,
            Resultado = resultado,
            ItemId = match.ItemId,
            NombreItem = match.NombreItem,
            NombreFamilia = match.NombreFamilia,
            Score = scoreOverride ?? match.Score
        };
}
