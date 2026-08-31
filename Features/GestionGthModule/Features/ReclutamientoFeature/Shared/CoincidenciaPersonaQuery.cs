using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared
{
    /// <summary>
    /// Resuelve si el documento que un postulante declaró en su formulario ya existe en la base:
    /// primero en <c>person</c> (el registro maestro de personas) y, si existe, si además tiene
    /// ficha en <c>workers</c> y si esa ficha está adentro de la empresa hoy.
    ///
    /// Por qué hace falta: el formulario es público y el postulante puede escribir lo que sea,
    /// incluidos los datos de un trabajador de Abril. Aprobarlo es lo que copia esos datos a
    /// <c>person</c> (<c>PostulanteFormularioRepository.SincronizarPersonAsync</c>), así que la
    /// aprobación puede estar actualizando la ficha de alguien que ya existía. GTH tiene que
    /// saberlo antes de decidir; al postulante no se le dice nada nunca — no es una regla de
    /// negocio suya y confirmarle que un documento existe en la base sería filtrar información.
    /// Por eso esto solo lo consumen las consultas de GTH, jamás los endpoints públicos por token.
    ///
    /// Vive acá porque lo usan los dos repositorios de la feature —el modal «Ver formulario»
    /// (<c>PostulanteFormularioRepository</c>) y las fichas de candidato del detalle del
    /// requerimiento (<c>ReclutamientoRepository</c>)— y el criterio tiene que ser idéntico: si el
    /// modal bloqueara la aprobación con una regla y la ficha con otra, GTH tendría dos respuestas
    /// distintas para el mismo postulante según por dónde entró.
    /// </summary>
    public static class CoincidenciaPersonaQuery
    {
        /// <summary>
        /// Coincidencias de los candidatos indicados, indexadas por <c>gth_candidato_id</c>. Un
        /// candidato sin formulario, sin documento declarado o cuyo documento no existe en
        /// <c>person</c> simplemente no aparece en el diccionario (no hay nada que avisar).
        ///
        /// Un solo roundtrip para los N candidatos: el detalle del requerimiento lista varios y
        /// resolverlos uno por uno sería un N+1.
        /// </summary>
        public static async Task<Dictionary<int, FormularioCoincidenciaDto>> ResolverAsync(
            AppDbContext ctx, IReadOnlyCollection<int> candidatoIds)
        {
            if (candidatoIds.Count == 0) return new Dictionary<int, FormularioCoincidenciaDto>();

            var filas = await ctx.Database.GetDbConnection().QueryAsync<CoincidenciaRow>(
                Sql,
                new { candidatoIds = candidatoIds.ToArray() },
                transaction: ctx.Database.CurrentTransaction?.GetDbTransaction());

            // Normalmente hay 0 o 1 fila por candidato (el UNIQUE del documento en `person`), pero
            // pueden salir dos si el mismo documento está registrado con distintas mayúsculas: son
            // dos filas distintas para el UNIQUE y ambas coinciden con la comparación insensible de
            // acá. Se agrupa en vez de asumir una sola —un ToDictionary reventaría— y se muestra la
            // que bloquea, que es la que le cambia la decisión a GTH.
            return filas
                .GroupBy(x => x.CandidatoId)
                .ToDictionary(
                    g => g.Key,
                    g => Mapear(g.OrderByDescending(x => x.EstaAdentro).ThenBy(x => x.PersonId).First()));
        }

        /// <summary>
        /// Coincidencia de un solo candidato. null si no hay ninguna. Atajo sobre
        /// <see cref="ResolverAsync(AppDbContext, IReadOnlyCollection{int})"/>.
        /// </summary>
        public static async Task<FormularioCoincidenciaDto?> ResolverUnoAsync(
            AppDbContext ctx, int candidatoId)
        {
            var todas = await ResolverAsync(ctx, new[] { candidatoId });
            return todas.GetValueOrDefault(candidatoId);
        }

        /// <summary>
        /// La llave de coincidencia es <c>person.document_identity_code</c>, que es la que tiene el
        /// UNIQUE y por tanto la misma que dispara el <c>ON CONFLICT</c> del upsert de la
        /// aprobación: se compara exactamente contra lo que va a pasar al aprobar, no contra una
        /// heurística aparte. Se compara con <c>upper(btrim(...))</c> para que un carné de
        /// extranjería escrito con otras mayúsculas también salga avisado (el UNIQUE distingue
        /// mayúsculas, así que en ese caso la aprobación crearía una ficha nueva en vez de
        /// actualizar la existente — y eso también es algo que GTH necesita ver).
        ///
        /// No se filtra por <c>person.state</c>: el UNIQUE tampoco lo hace, así que una ficha dada
        /// de baja igual atrapa el <c>ON CONFLICT</c> y la aprobación termina actualizándola. Es
        /// una coincidencia real, no un fantasma del histórico.
        ///
        /// <c>workers</c> no tiene <c>state</c> —el histórico se modela con
        /// <c>workers_estado_id</c>— así que no hay soft-delete que filtrar de ese lado.
        ///
        /// La ficha que el propio formulario ya escribió (<c>gth_postulante_formulario.person_id</c>)
        /// no cuenta como coincidencia: si no, al reabrir un formulario ya aprobado el aviso diría
        /// «esta persona ya existe» señalando a la ficha que esa misma aprobación acababa de crear.
        /// Lo mismo vale para la ficha que registró el propio pedido en un ingreso directo
        /// (<c>gth_requerimiento.fft_person_id</c>): desde que la casilla FFT pide el DNI, el
        /// candidato entra a <c>person</c> al crearse la solicitud, así que TODO candidato FFT
        /// coincidiría consigo mismo y GTH vería el aviso en cada uno.
        ///
        /// La excepción es la misma en los dos casos: cuando esa ficha está adentro de la empresa el
        /// aviso se mantiene igual, porque el bloqueo de la aprobación no puede depender de si
        /// alguien ya aprobó ese formulario antes ni de por dónde entró el candidato.
        /// </summary>
        private const string Sql = """
            WITH declarado AS (
                SELECT f.gth_candidato_id,
                       upper(btrim(f.numero_documento)) AS documento,
                       f.person_id                      AS person_id_formulario,
                       r.fft_person_id                  AS person_id_fft,
                       td.nombre                        AS tipo_documento
                  FROM gth_postulante_formulario f
                  LEFT JOIN gth_candidato c
                         ON c.gth_candidato_id = f.gth_candidato_id
                  LEFT JOIN gth_requerimiento r
                         ON r.gth_requerimiento_id = c.gth_requerimiento_id
                  LEFT JOIN gth_tipo_documento td
                         ON td.gth_tipo_documento_id = f.gth_tipo_documento_id
                 WHERE f.state
                   AND f.gth_candidato_id = ANY(@candidatoIds)
                   AND coalesce(btrim(f.numero_documento), '') <> ''
            ),
            coincide AS (
                SELECT d.gth_candidato_id,
                       d.documento,
                       d.tipo_documento,
                       p.person_id,
                       p.full_name,
                       (p.person_id = d.person_id_formulario
                        OR p.person_id = d.person_id_fft) AS es_ficha_propia
                  FROM declarado d
                  JOIN person p
                    ON upper(btrim(p.document_identity_code)) = d.documento
            ),
            -- Una persona puede tener varias filas en workers (reingresos), así que se agrega:
            -- basta UNA ficha adentro para que la coincidencia bloquee, y el estado que se muestra
            -- es el de esa ficha (o el de la más reciente si ninguna está adentro).
            ficha AS (
                SELECT c.gth_candidato_id,
                       c.person_id,
                       bool_or(we.esta_adentro) AS esta_adentro,
                       (array_agg(w.id       ORDER BY we.esta_adentro DESC, w.id DESC))[1] AS worker_id,
                       (array_agg(we.codigo  ORDER BY we.esta_adentro DESC, w.id DESC))[1] AS estado_codigo,
                       (array_agg(we.nombre  ORDER BY we.esta_adentro DESC, w.id DESC))[1] AS estado_nombre
                  FROM coincide c
                  JOIN workers w         ON w.person_id = c.person_id AND w.state
                  JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
                 GROUP BY c.gth_candidato_id, c.person_id
            )
            SELECT c.gth_candidato_id                    AS CandidatoId,
                   c.documento                           AS Documento,
                   c.tipo_documento                      AS TipoDocumento,
                   c.person_id                           AS PersonId,
                   c.full_name                           AS NombreEnBd,
                   f.worker_id                           AS WorkerId,
                   f.estado_codigo                       AS WorkersEstadoCodigo,
                   f.estado_nombre                       AS WorkersEstadoNombre,
                   coalesce(f.esta_adentro, false)       AS EstaAdentro
              FROM coincide c
              LEFT JOIN ficha f
                     ON f.gth_candidato_id = c.gth_candidato_id
                    AND f.person_id        = c.person_id
             WHERE NOT coalesce(c.es_ficha_propia, false)
                OR coalesce(f.esta_adentro, false);
            """;

        private static FormularioCoincidenciaDto Mapear(CoincidenciaRow r)
        {
            var tieneFicha = r.WorkerId != null;
            return new FormularioCoincidenciaDto
            {
                Documento           = r.Documento,
                TipoDocumento       = r.TipoDocumento,
                PersonId            = r.PersonId,
                NombreEnBd          = r.NombreEnBd,
                WorkerId            = r.WorkerId,
                WorkersEstadoCodigo = r.WorkersEstadoCodigo,
                WorkersEstadoNombre = r.WorkersEstadoNombre,
                EstaAdentro         = r.EstaAdentro,
                Nivel = r.EstaAdentro
                    ? NivelCoincidenciaPersona.TrabajadorActual
                    : tieneFicha
                        ? NivelCoincidenciaPersona.FichaPrevia
                        : NivelCoincidenciaPersona.SoloPerson,
            };
        }

        /// <summary>Fila cruda del SQL (Dapper la mapea por nombre de columna).</summary>
        private sealed class CoincidenciaRow
        {
            public int CandidatoId { get; set; }
            public string Documento { get; set; } = string.Empty;
            public string? TipoDocumento { get; set; }
            public int PersonId { get; set; }
            public string? NombreEnBd { get; set; }
            public int? WorkerId { get; set; }
            public string? WorkersEstadoCodigo { get; set; }
            public string? WorkersEstadoNombre { get; set; }
            public bool EstaAdentro { get; set; }
        }
    }
}
