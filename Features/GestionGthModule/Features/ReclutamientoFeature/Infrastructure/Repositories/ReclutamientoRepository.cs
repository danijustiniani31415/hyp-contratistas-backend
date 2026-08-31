using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Helpers;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Repositories
{
    public class ReclutamientoRepository : IReclutamientoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ReclutamientoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>Los timestamps se guardan en UTC y se sirven al frontend en hora de Perú.</summary>
        private static readonly TimeSpan PeruOffset = TimeSpan.FromHours(-5);

        /// <summary>
        /// Nombre del <c>area_type</c> de las gerencias, el único tipo que NO sirve como "Área del
        /// solicitante" (ver <see cref="ResolveAreaNombreInternal"/>). Se compara por nombre y no
        /// por id porque los ids de <c>area_type</c> no están verificados como iguales en dev y
        /// prod; es el mismo criterio que usan Revisores de Áreas y Lecciones Aprendidas.
        /// </summary>
        private const string AreaTypeGerencia = "Área de Gerencia";

        /// <summary>
        /// Primera clave del <c>pg_advisory_xact_lock(int, int)</c> que serializa la generación del
        /// correlativo de <c>gth_requerimiento</c>; la segunda es el año. Los advisory locks de
        /// Postgres viven en un espacio global compartido por toda la base, así que el número solo
        /// tiene que ser distinto del que use cualquier otro candado del sistema — hoy este es el
        /// único, y por eso no hay una tabla de constantes donde ponerlo.
        /// </summary>
        private const int CorrelativoLockNamespace = 8471;

        /// <summary>
        /// Fila cruda del formulario del postulante en el detalle de GTH. Es una clase con nombre
        /// (y no un tipo anónimo) porque la consulta se saltea cuando no hay candidatos y ambas
        /// ramas del condicional necesitan el mismo tipo de lista.
        /// </summary>
        private sealed class FormularioRawRow
        {
            public int GthCandidatoId { get; set; }
            public string EstadoCodigo { get; set; } = string.Empty;
            public string EstadoNombre { get; set; } = string.Empty;
            public string CorreoEnvio { get; set; } = string.Empty;
            public string? CorreoElectronico { get; set; }
            public DateTimeOffset? EnviadoDateTime { get; set; }
            public DateTimeOffset? CompletadoDateTime { get; set; }
            public string? RevisadoNombre { get; set; }
            public DateTimeOffset? RevisadoDateTime { get; set; }
            /// <summary>CV documentado que adjuntó el postulante (nombre visible + link).</summary>
            public string? CvNombre { get; set; }
            public string? CvUrl { get; set; }
        }

        /// <summary>
        /// Fila cruda de la evaluación de la entrevista de un candidato (mismo motivo que
        /// <see cref="FormularioRawRow"/>: la consulta se saltea cuando no hay candidatos y ambas
        /// ramas necesitan el mismo tipo de lista).
        /// </summary>
        private sealed class EvaluacionRawRow
        {
            public int GthCandidatoId { get; set; }
            /// <summary>Id de la evaluación: con él se le cuelgan sus archivos del informe.</summary>
            public int GthCandidatoEvaluacionId { get; set; }
            /// <summary>Archivos del informe (0..2). Se llenan aparte, en una sola consulta.</summary>
            public List<EvaluacionArchivoDto> Archivos { get; set; } = new();
            public string? ComentarioEntrevista { get; set; }
            public string? ComentarioPsicotecnico { get; set; }
            public string? ComentarioRecomendacion { get; set; }
            public string ResultadoCodigo { get; set; } = string.Empty;
            public string ResultadoNombre { get; set; } = string.Empty;
            public string? AgradecimientoCorreo { get; set; }
            public DateTimeOffset? AgradecimientoDateTime { get; set; }
            public DateTimeOffset? DecisionDateTime { get; set; }
            /// <summary>
            /// Ultima modificacion de la evaluacion. Es la fecha del rechazo por EMO: ese resultado
            /// no toca DecisionDateTime a proposito, para no perder cuando el area lo eligio (una
            /// correccion de la clinica puede devolverlo a SELECCIONADO).
            /// </summary>
            public DateTimeOffset? UpdatedDateTime { get; set; }
        }

        /// <summary>
        /// Candidatos de la long list VIGENTE de su requerimiento: los vivos
        /// (<c>state = true</c>) de la última vuelta.
        ///
        /// Un requerimiento puede tener varias long lists: cuando el solicitante rechaza a todos,
        /// vuelve a LONG_LIST y GTH sube otra con <c>numero_long_list + 1</c>. Las anteriores se
        /// conservan vivas —un candidato rechazado no está eliminado, su rechazo vive en
        /// <c>gth_candidato_estado</c>— así que "la long list actual" ya no se puede resolver solo
        /// con <c>state</c>.
        ///
        /// Toda consulta que signifique "los candidatos de este proceso ahora mismo" debe partir
        /// de acá y no de <c>ctx.GthCandidato</c>: es la única definición de vigencia, para que
        /// agregar una vuelta no obligue a acordarse de N filtros repartidos por el repositorio.
        /// El historial de rechazados es la excepción a propósito: lee todas las vueltas.
        /// </summary>
        private static IQueryable<GthCandidato> CandidatosVigentes(AppDbContext ctx) =>
            ctx.GthCandidato.Where(c => c.State && c.NumeroLongList == ctx.GthCandidato
                .Where(x => x.GthRequerimientoId == c.GthRequerimientoId && x.State)
                .Max(x => x.NumeroLongList));

        /// <summary>
        /// Portafolio/anexos vivos de los candidatos indicados, agrupados por candidato y en orden
        /// de carga. Una sola consulta para toda la long list (nunca una por candidato). Vacío si
        /// no hay candidatos.
        /// </summary>
        private static async Task<Dictionary<int, List<CandidatoAnexoDto>>> QueryAnexos(
            AppDbContext ctx, List<int> candidatoIds)
        {
            if (candidatoIds.Count == 0) return new Dictionary<int, List<CandidatoAnexoDto>>();

            var filas = await ctx.GthCandidatoAnexo
                .Where(a => a.State && candidatoIds.Contains(a.GthCandidatoId))
                .OrderBy(a => a.Orden).ThenBy(a => a.GthCandidatoAnexoId)
                .Select(a => new
                {
                    a.GthCandidatoId,
                    Anexo = new CandidatoAnexoDto
                    {
                        AnexoId = a.GthCandidatoAnexoId,
                        // El nombre original es el que GTH reconoce; el de SharePoint lleva el
                        // código del requerimiento y un timestamp.
                        Nombre  = a.NombreOriginal ?? a.Nombre,
                        Url     = a.Url,
                    },
                })
                .ToListAsync();

            return filas
                .GroupBy(f => f.GthCandidatoId)
                .ToDictionary(g => g.Key, g => g.Select(f => f.Anexo).ToList());
        }

        /// <summary>
        /// Evaluaciones vigentes de los candidatos indicados, con los archivos de su informe
        /// (vacío si no hay candidatos). Los archivos van en una segunda consulta acotada y no en
        /// un left join encadenado: son 0..2 por evaluación y el join multiplicaría las filas de
        /// los comentarios, que son textos largos.
        /// </summary>
        private static async Task<List<EvaluacionRawRow>> QueryEvaluaciones(
            AppDbContext ctx, List<int> candidatoIds, bool incluirArchivos = true)
        {
            if (candidatoIds.Count == 0) return new List<EvaluacionRawRow>();

            var evaluaciones = await (
                from ev in ctx.GthCandidatoEvaluacion
                where ev.State && candidatoIds.Contains(ev.GthCandidatoId)
                join res in ctx.GthCandidatoResultado on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                select new EvaluacionRawRow
                {
                    GthCandidatoId           = ev.GthCandidatoId,
                    GthCandidatoEvaluacionId = ev.GthCandidatoEvaluacionId,
                    ComentarioEntrevista     = ev.ComentarioEntrevista,
                    ComentarioPsicotecnico   = ev.ComentarioPsicotecnico,
                    ComentarioRecomendacion  = ev.ComentarioRecomendacion,
                    ResultadoCodigo          = res.Codigo,
                    ResultadoNombre          = res.Nombre,
                    AgradecimientoCorreo     = ev.AgradecimientoCorreo,
                    AgradecimientoDateTime   = ev.AgradecimientoDateTime,
                    DecisionDateTime         = ev.DecisionDateTime,
                    UpdatedDateTime          = ev.UpdatedDateTime,
                }).ToListAsync();

            // El historial de rechazados no muestra los archivos: ahí se pide sin ellos para no
            // pagar un roundtrip que nadie va a leer.
            if (!incluirArchivos) return evaluaciones;

            var archivos = await QueryEvaluacionArchivos(
                ctx, evaluaciones.Select(e => e.GthCandidatoEvaluacionId).ToList());

            foreach (var ev in evaluaciones)
                ev.Archivos = archivos.GetValueOrDefault(ev.GthCandidatoEvaluacionId) ?? new List<EvaluacionArchivoDto>();

            return evaluaciones;
        }

        /// <summary>
        /// Archivos vivos del informe de las evaluaciones indicadas, agrupados por evaluación y en
        /// el orden del catálogo (informe final primero). Una sola consulta para todas.
        /// </summary>
        private static async Task<Dictionary<int, List<EvaluacionArchivoDto>>> QueryEvaluacionArchivos(
            AppDbContext ctx, List<int> evaluacionIds)
        {
            if (evaluacionIds.Count == 0) return new Dictionary<int, List<EvaluacionArchivoDto>>();

            var filas = await (
                from a in ctx.GthCandidatoEvaluacionArchivo
                where a.State && evaluacionIds.Contains(a.GthCandidatoEvaluacionId)
                join t in ctx.GthEvaluacionArchivoTipo
                    on a.GthEvaluacionArchivoTipoId equals t.GthEvaluacionArchivoTipoId
                orderby t.Orden, a.GthCandidatoEvaluacionArchivoId
                select new
                {
                    a.GthCandidatoEvaluacionId,
                    Archivo = new EvaluacionArchivoDto
                    {
                        ArchivoId  = a.GthCandidatoEvaluacionArchivoId,
                        TipoCodigo = t.Codigo,
                        TipoNombre = t.Nombre,
                        // El nombre original es el que GTH reconoce; el de SharePoint lleva el
                        // código del requerimiento y un timestamp.
                        Nombre     = a.NombreOriginal ?? a.Nombre,
                        Url        = a.Url,
                    },
                }).ToListAsync();

            return filas
                .GroupBy(f => f.GthCandidatoEvaluacionId)
                .ToDictionary(g => g.Key, g => g.Select(f => f.Archivo).ToList());
        }

        /// <summary>
        /// Candidatos del requerimiento que quedaron rechazados, con la etapa del rechazo, para el
        /// «Historial de candidatos rechazados» de GTH y del solicitante.
        ///
        /// Lee TODAS las vueltas del requerimiento (no solo la long list vigente), pero siempre
        /// con <c>state = true</c>: un candidato rechazado no está eliminado del sistema, sigue
        /// vivo con su rechazo registrado en el estado. Es la única consulta que a propósito no
        /// parte de <see cref="CandidatosVigentes"/>, porque el historial es justamente lo que
        /// quedó fuera de la vuelta actual. La etapa sale de cruzar el estado del candidato con el
        /// resultado de su evaluación:
        ///
        ///   • resultado NO_APTO_EMO → el seleccionado no pasó el EMO de ingreso.
        ///   • resultado RECHAZADO  → el solicitante rechazó al finalista (decisión final).
        ///   • resultado NO_PASO    → GTH lo descartó, y la etapa la decide si llegó a tener cita:
        ///                            sin entrevista fue en el formulario, con entrevista fue tras ella.
        ///   • estado RECHAZADO     → el solicitante lo rechazó al revisar la long list.
        ///
        /// Los candidatos que se dieron de baja sin decisión (una long list reemplazada antes de
        /// que el solicitante la revisara) no son rechazos y quedan fuera.
        /// </summary>
        private static async Task<List<CandidatoRechazadoDto>> QueryCandidatosRechazados(
            AppDbContext ctx, int requerimientoId)
        {
            var candidatos = await (
                from c in ctx.GthCandidato
                where c.GthRequerimientoId == requerimientoId && c.State
                join est in ctx.GthCandidatoEstado on c.GthCandidatoEstadoId equals est.GthCandidatoEstadoId
                select new
                {
                    c.GthCandidatoId,
                    c.Nombre,
                    c.Puesto,
                    c.Comentario,
                    c.CvNombre,
                    c.CvUrl,
                    c.NumeroLongList,
                    EstadoCodigo = est.Codigo,
                    c.DecisionDateTime,
                    c.UpdatedDateTime,
                    c.CreatedDateTime,
                }).ToListAsync();

            if (candidatos.Count == 0) return new List<CandidatoRechazadoDto>();

            // Segundo roundtrip acotado en vez de un left join encadenado candidato→evaluación→
            // resultado: ese patrón EF Core no lo materializa bien (ver GetDetalleGth). Se une en
            // memoria por GthCandidatoId, que es 0..1 evaluación vigente por candidato.
            var ids = candidatos.Select(c => c.GthCandidatoId).ToList();
            var evaluaciones = (await QueryEvaluaciones(ctx, ids, incluirArchivos: false))
                .ToDictionary(x => x.GthCandidatoId);

            // Quiénes llegaron a tener cita. Es lo que separa las dos salidas que decide GTH: al
            // que se le rechazó el formulario nunca se le programó entrevista, así que etiquetarlo
            // como rechazado "en entrevistas" sería mentira.
            var conEntrevista = (await ctx.GthEntrevista
                    .Where(e => e.State && ids.Contains(e.GthCandidatoId))
                    .Select(e => e.GthCandidatoId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            var historial = new List<CandidatoRechazadoDto>();
            foreach (var x in candidatos)
            {
                var ev = evaluaciones.GetValueOrDefault(x.GthCandidatoId);

                // El resultado de la evaluación manda sobre el estado del candidato: un rechazado
                // en entrevistas o en la decisión final sigue siendo APROBADO en la long list (ahí
                // sí pasó), así que mirar solo el estado lo etiquetaría mal. La regla vive en
                // DerivarEtapaRechazo porque retomar un candidato tiene que leer exactamente lo
                // mismo que este historial (ver RetomarCandidatoRechazado).
                var etapaCodigo = DerivarEtapaRechazo(
                    x.EstadoCodigo, ev?.ResultadoCodigo, conEntrevista.Contains(x.GthCandidatoId));
                if (etapaCodigo == null)
                    continue; // sigue en carrera, o se dio de baja sin decisión: no es un rechazo

                // Quién lo rechazó y cuándo: lo que la etapa por sí sola no dice. La fecha sale de
                // donde quedó registrada la decisión de esa etapa, que no es la misma columna en
                // todas (el descarte de GTH se marca al enviar el correo de fin de proceso).
                var (porCodigo, porNombre, fecha) = etapaCodigo switch
                {
                    EtapaRechazo.Emo           => (RechazadoPor.SaludOcupacional, "Salud Ocupacional",
                                                   ev!.UpdatedDateTime),
                    EtapaRechazo.DecisionFinal => (RechazadoPor.Solicitante, "Área solicitante",
                                                   ev!.DecisionDateTime),
                    EtapaRechazo.Entrevistas   => (RechazadoPor.Gth, "GTH", ev!.AgradecimientoDateTime),
                    EtapaRechazo.Formulario    => (RechazadoPor.Gth, "GTH", ev!.AgradecimientoDateTime),
                    _                          => (RechazadoPor.Solicitante, "Área solicitante",
                                                   x.DecisionDateTime),
                };

                historial.Add(new CandidatoRechazadoDto
                {
                    CandidatoId        = x.GthCandidatoId,
                    Nombre             = x.Nombre,
                    Puesto             = x.Puesto,
                    NumeroLongList     = x.NumeroLongList,
                    Comentario         = x.Comentario,
                    CvNombre           = x.CvNombre,
                    CvUrl              = x.CvUrl,
                    EtapaCodigo        = etapaCodigo,
                    EtapaNombre        = NombreEtapaRechazo(etapaCodigo),
                    RechazadoPorCodigo = porCodigo,
                    RechazadoPorNombre = porNombre,
                    // Los candidatos decididos antes de que existiera decision_date_time no tienen
                    // la fecha exacta: se cae a la de actualización y luego a la de creación para
                    // que ninguna fila del historial quede sin fecha.
                    RechazadoEn        = (fecha ?? x.UpdatedDateTime ?? x.CreatedDateTime)
                                            .ToOffset(PeruOffset).DateTime,
                    PuedeRetomar       = EtapaRechazo.Retomables.Contains(etapaCodigo),
                });
            }

            // Lo más reciente arriba: el historial se lee de la última vuelta hacia atrás.
            return historial
                .OrderByDescending(h => h.RechazadoEn)
                .ThenByDescending(h => h.CandidatoId)
                .ToList();
        }

        /// <summary>
        /// Quién obtuvo el puesto del requerimiento: el candidato cuya evaluación quedó en
        /// SELECCIONADO (la decisión final del área solicitante, la que lo manda al EMO de
        /// ingreso), con quién y cuándo lo decidió y qué responsable de GTH llevó la vacante.
        ///
        /// Null mientras nadie haya sido seleccionado, así que sirve de bandera para las dos
        /// pantallas: no hay "puesto cubierto" que mostrar hasta que el área decide.
        /// </summary>
        private static async Task<SeleccionadoDto?> QuerySeleccionado(AppDbContext ctx, int requerimientoId)
        {
            // Solo puede haber un SELECCIONADO por requerimiento (aprobar a un finalista saca al
            // requerimiento de la fase de decisión y la segunda ya no se acepta), así que el First
            // es determinista.
            var raw = await (
                from c in ctx.GthCandidato
                where c.GthRequerimientoId == requerimientoId && c.State
                join ev in ctx.GthCandidatoEvaluacion on c.GthCandidatoId equals ev.GthCandidatoId
                where ev.State
                join res in ctx.GthCandidatoResultado on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                where res.Codigo == ResultadoCandidato.Seleccionado
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                // La fase del requerimiento es la que distingue "el EMO de ingreso sigue
                // pendiente" de "el proceso ya cerró": sin ella no se puede saber si una ficha
                // que ya está adentro es lo esperado (firmó) o una anomalía.
                join er in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals er.GthEstadoRequerimientoId
                // El responsable del proceso es opcional (puede no haberse asignado nunca).
                join rp in ctx.GthResponsableProceso
                    on r.GthResponsableProcesoId equals (int?)rp.GthResponsableProcesoId into rpJoin
                from rp in rpJoin.DefaultIfEmpty()
                // El formulario del postulante es opcional (de ahí sale su CV documentado), así que
                // va en left join: no se lee de la consulta de la ficha porque esa exige person_id
                // y un seleccionado sin formulario aprobado no tiene ficha, pero sí puede tener CV.
                join fm in ctx.GthPostulanteFormulario.Where(f => f.State)
                    on c.GthCandidatoId equals fm.GthCandidatoId into fmJoin
                from fm in fmJoin.DefaultIfEmpty()
                select new
                {
                    c.GthCandidatoId,
                    c.Nombre,
                    c.Puesto,
                    c.CvNombre,
                    c.CvUrl,
                    CvPostulanteNombre = fm != null ? (fm.CvNombreOriginal ?? fm.CvNombre) : null,
                    CvPostulanteUrl    = fm != null ? fm.CvUrl : null,
                    ev.DecisionDateTime,
                    ev.DecisionUserId,
                    // Persona del ingreso directo: es por donde se llega a su ficha, porque un FFT
                    // no tiene formulario del que leerla. Null en el flujo normal.
                    r.FftPersonId,
                    EstadoRequerimiento = er.Codigo,
                    ResponsableGth = rp == null ? null
                        : (rp.Worker!.Person != null ? rp.Worker.Person.FullName : rp.Worker.ApellidoNombre),
                }).FirstOrDefaultAsync();

            if (raw == null) return null;

            // Nombre de quien tomó la decisión final: segundo roundtrip mínimo y solo en los
            // procesos ya cerrados (los abiertos salen por el return de arriba sin tocarlo).
            string? seleccionadoPor = null;
            if (raw.DecisionUserId.HasValue)
                seleccionadoPor = await ctx.Worker
                    .Where(w => w.Person != null && w.Person.UserId == raw.DecisionUserId.Value)
                    // Una persona puede tener varias fichas por reingreso: la vigente manda.
                    // "Vigente" = tiene un periodo laboral abierto (ver WorkersPeriodoLaboral);
                    // el desempate es por la fecha de ingreso más reciente.
                    .OrderBy(w => !w.PeriodosLaborales.Any(p => p.State && p.FechaRetiro == null))
                    .ThenByDescending(w => w.PeriodosLaborales
                        .Where(p => p.State)
                        .OrderByDescending(p => p.FechaIngreso)
                        .ThenByDescending(p => p.WorkersPeriodoLaboralId)
                        .Select(p => (DateOnly?)p.FechaIngreso)
                        .FirstOrDefault())
                    .Select(w => w.Person!.FullName ?? w.ApellidoNombre)
                    .FirstOrDefaultAsync();

            // Ficha de pre-ingreso del seleccionado. Se llega por person_id, que es el unico
            // enlace entre el candidato y su ficha, pero ese person_id sale de dos lados distintos:
            // en el flujo normal lo dejo el formulario del postulante al aprobarse, y en un ingreso
            // directo —que no tiene formulario— lo dejo el propio pedido (fft_person_id). Cada
            // camino resuelve la ficha en una sola consulta.
            var ficha = raw.FftPersonId.HasValue
                ? await (
                    from w in ctx.Worker
                    where w.PersonId == raw.FftPersonId
                    join we in ctx.WorkersEstado on w.WorkersEstadoId equals we.WorkersEstadoId
                    orderby w.Id descending
                    select new { w.Id, w.WorkersEstadoId, we.EstaAdentro, EstadoNombre = we.Nombre })
                    .FirstOrDefaultAsync()
                : await (
                    from f in ctx.GthPostulanteFormulario
                    where f.GthCandidatoId == raw.GthCandidatoId && f.State && f.PersonId != null
                    join w in ctx.Worker on f.PersonId equals w.PersonId
                    join we in ctx.WorkersEstado on w.WorkersEstadoId equals we.WorkersEstadoId
                    orderby w.Id descending
                    select new { w.Id, w.WorkersEstadoId, we.EstaAdentro, EstadoNombre = we.Nombre })
                    .FirstOrDefaultAsync();

            var esPreIngreso = ficha?.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado;

            // Anomalia: el requerimiento sigue esperando el EMO de Ingreso pero la ficha que le
            // toco al seleccionado es de alguien que ya trabaja en Abril. No deberia pasar —
            // aprobar el formulario de un postulante cuyo documento es de un trabajador que esta
            // adentro esta bloqueado (CoincidenciaPersonaQuery)—, y si pasa el proceso no puede
            // avanzar por aca: el freno de ProgramacionEmoRepository.Create rechaza la cita. Se
            // sirve para que el detalle lo diga en vez de quedarse sin boton ni explicacion.
            //
            // La fase importa: una ficha ya adentro con el requerimiento CERRADO es lo normal
            // (firmo el contrato y paso a ACTIVO), no una anomalia.
            var fichaAdentro = raw.EstadoRequerimiento == EstadoReclutamiento.EmoIngreso
                && ficha?.EstaAdentro == true;

            // La cita del EMO de Ingreso: su estado y la aptitud del resultado que le colgo la
            // clinica. Ya no alcanza con saber si existe — desde que programar la cita dejo de
            // cerrar el proceso, entre programarla y recibir el resultado el requerimiento se queda
            // en EMO_INGRESO, y sin esto el detalle mostraba esa fase sin boton y sin ninguna
            // explicacion. La aptitud sale de la propia cita (emo_resultado_id) y no del ultimo EMO
            // del trabajador: es la de ESTE examen.
            //
            // El roundtrip solo se paga si hay algo que contar con el: si la ficha no es de
            // pre-ingreso ni es la anomalia, no hay boton ni aviso que mostrar.
            var cita = (esPreIngreso || fichaAdentro)
                ? await ctx.SsProgramacionEmo
                    .Where(pe => pe.WorkerId == ficha!.Id && pe.State)
                    .OrderByDescending(pe => pe.FechaProgramada).ThenByDescending(pe => pe.Id)
                    .Select(pe => new
                    {
                        pe.Estado,
                        pe.FechaProgramada,
                        Aptitud = pe.EmoResultado != null ? pe.EmoResultado.Aptitud : null,
                    })
                    .FirstOrDefaultAsync()
                : null;

            // Una cita que se cayó (la clínica la rechazó, se canceló, o el candidato no fue) no
            // resuelve nada: hay que volver a agendar. Antes daba igual —el requerimiento ya estaba
            // CERRADO desde que se creaba la cita— pero ahora el proceso sigue abierto esperando el
            // resultado, y sin esto se quedaría en la fase del EMO sin botón con el que destrabarlo.
            var necesitaCita = (esPreIngreso || fichaAdentro)
                && (cita == null || CitasQueNoResuelven.Contains(cita.Estado));

            return new SeleccionadoDto
            {
                CandidatoId         = raw.GthCandidatoId,
                Nombre              = raw.Nombre,
                Puesto              = raw.Puesto,
                CvNombre            = raw.CvNombre,
                CvUrl               = raw.CvUrl,
                CvPostulanteNombre  = raw.CvPostulanteUrl == null ? null : raw.CvPostulanteNombre,
                CvPostulanteUrl     = raw.CvPostulanteUrl,
                SeleccionadoEn      = raw.DecisionDateTime?.ToOffset(PeruOffset).DateTime,
                SeleccionadoPor     = seleccionadoPor,
                ResponsableGth      = raw.ResponsableGth,
                WorkerId            = ficha?.Id,
                // Pendiente solo mientras la ficha siga siendo de pre-ingreso Y no tenga la cita
                // creada: si ya firmo y paso a ACTIVO, o si la programacion ya existe, no hay nada
                // que hacer desde el detalle del requerimiento.
                EmoIngresoPendiente = esPreIngreso && necesitaCita,
                EmoIngresoBloqueado = fichaAdentro && necesitaCita,
                FichaEstadoNombre   = fichaAdentro ? ficha!.EstadoNombre : null,
                EmoProgramacionEstado = cita?.Estado,
                EmoFechaProgramada    = cita?.FechaProgramada.ToDateTime(TimeOnly.MinValue),
                EmoAptitud            = cita?.Aptitud,
            };
        }

        /// <summary>
        /// Estados en los que una cita de EMO ya no va a dar un resultado: hay que agendar otra. Son
        /// los mismos que <c>ProgramacionEmoRepository</c> deja fuera de su chequeo de "ya tiene una
        /// programación activa", que es lo que permite crear la siguiente.
        /// </summary>
        private static readonly HashSet<string> CitasQueNoResuelven = new()
        {
            "Cancelado",
            "Rechazado por Clínica",
            "No se presentó",
        };

        /// <summary>Proyecta la fila cruda de la evaluación al DTO (fechas ya en hora de Perú).</summary>
        private static EvaluacionResumenDto MapEvaluacion(EvaluacionRawRow x) => new()
        {
            ComentarioEntrevista    = x.ComentarioEntrevista,
            ComentarioPsicotecnico  = x.ComentarioPsicotecnico,
            ComentarioRecomendacion = x.ComentarioRecomendacion,
            ResultadoCodigo         = x.ResultadoCodigo,
            ResultadoNombre         = x.ResultadoNombre,
            AgradecimientoCorreo    = x.AgradecimientoCorreo,
            AgradecimientoEnviadoEn = x.AgradecimientoDateTime?.ToOffset(PeruOffset).DateTime,
            DecididoEn              = x.DecisionDateTime?.ToOffset(PeruOffset).DateTime,
            Archivos                = x.Archivos,
        };

        public async Task<ReclutamientoFormDataDto> GetFormData(int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var dto = new ReclutamientoFormDataDto { MaxVacantes = 10 };

            if (userId.HasValue)
            {
                var ficha = await ResolveSolicitanteInternal(ctx, userId.Value);
                dto.AreaNombre = ficha.AreaNombre;
                dto.AreaScopeId = ficha.AreaScopeId;
                dto.PuestoNombre = ficha.PuestoNombre;
                dto.CategoriaNombre = ficha.CategoriaNombre;
            }

            dto.Puestos = await QueryPuestosDelArea(ctx, dto.AreaScopeId);

            dto.TiposRequerimiento = await ctx.GthTipoRequerimiento
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.Orden)
                .Select(t => new TipoRequerimientoOpcionDto
                {
                    Id     = t.GthTipoRequerimientoId,
                    Nombre = t.Nombre,
                    Codigo = t.Codigo,
                })
                .ToListAsync();

            dto.Proyectos = await ctx.Project
                .Where(p => p.State && p.Active)
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new OpcionDto { Id = p.ProjectId, Nombre = p.ProjectDescription })
                .ToListAsync();

            // Tipos de documento del candidato de un ingreso directo FFT. Mismo catálogo que el
            // formulario del postulante: es la misma persona y termina en la misma columna de
            // `person`, así que ofrecer dos listas distintas dejaría documentos incomparables.
            dto.TiposDocumento = await QueryTiposDocumento(ctx);

            // Candidatos a "trabajador reemplazado" del tipo Reemplazo. Sin área del solicitante no
            // hay subárbol que recorrer, así que la lista queda vacía y el campo deja de exigirse.
            dto.TrabajadoresArea = await QueryTrabajadoresDelArea(ctx, dto.AreaScopeId);

            return dto;
        }

        /// <inheritdoc/>
        public async Task<List<TipoDocumentoOpcionDto>> GetTiposDocumento()
        {
            using var ctx = _factory.CreateDbContext();
            return await QueryTiposDocumento(ctx);
        }

        /// <summary>
        /// Catálogo <c>gth_tipo_documento</c> (DNI / CE) con su código estable: es por el código que
        /// se decide cuántos dígitos admite el número, tanto en el formulario como al validar.
        /// </summary>
        private static Task<List<TipoDocumentoOpcionDto>> QueryTiposDocumento(AppDbContext ctx) =>
            ctx.GthTipoDocumento
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.Orden)
                .Select(t => new TipoDocumentoOpcionDto
                {
                    Id     = t.GthTipoDocumentoId,
                    Nombre = t.Nombre,
                    Codigo = t.Codigo,
                })
                .ToListAsync();

        /// <summary>
        /// Puestos que se le ofrecen al solicitante: aquellos cuya ÁREA SOLICITANTE
        /// (<c>puesto.area_solicitante_scope_id</c>) es la suya o una de sus áreas hijas (mismo
        /// subárbol que el resto de filtros por área del sistema). Un gerente de Proyectos ve
        /// además los de Unidad de Proyectos, SSOMA, Calidad, etc.
        ///
        /// El área a la que ENTRARÁ el trabajador es otra columna y no interviene acá: la
        /// Gerencia Inmobiliaria puede pedir un INGENIERO RESIDENTE, que entra a Residencia.
        ///
        /// Es también la lista contra la que se valida lo que llega del cliente, igual que
        /// <see cref="QueryTrabajadoresDelArea"/>: lo que no se ofrece tampoco se acepta.
        ///
        /// Los puestos sin área solicitante quedan fuera a propósito: el padrón de GTH solo
        /// cubrió personal de oficina, y esta pantalla es justamente la de oficina.
        ///
        /// Sin área del solicitante se cae al catálogo COMPLETO en vez de a una lista vacía: un
        /// usuario recién creado al que todavía no le asignaron área podría necesitar pedir
        /// personal, y dejarlo sin ningún puesto lo bloquearía del todo.
        /// </summary>
        private static async Task<List<PuestoOpcionDto>> QueryPuestosDelArea(AppDbContext ctx, int? areaScopeId)
        {
            var puestos = ctx.Puesto.Where(p => p.State && p.Active);

            if (areaScopeId.HasValue)
            {
                var idsArea = await ctx.ResolveDescendantsAsync(areaScopeId.Value);
                puestos = puestos.Where(p => p.AreaSolicitanteScopeId != null
                                          && idsArea.Contains(p.AreaSolicitanteScopeId.Value));
            }

            return await puestos
                .OrderBy(p => p.Orden).ThenBy(p => p.Nombre)
                .Select(p => new PuestoOpcionDto
                {
                    Id     = p.PuestoId,
                    Nombre = p.Nombre,
                    // El área a la que entrará quien ocupe el puesto: el formulario la muestra al
                    // elegirlo, en vez de preguntarla.
                    AreaDestino = p.AreaDestinoScope != null && p.AreaDestinoScope.State
                                  && p.AreaDestinoScope.AreaItem != null
                                  && p.AreaDestinoScope.AreaItem.State
                                      ? p.AreaDestinoScope.AreaItem.AreaItemName
                                      : null,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Área a la que ENTRA el trabajador que ocupe un puesto
        /// (<c>puesto.area_destino_scope_id</c>). El solicitante ya no la elige: se le muestra
        /// para que sepa a dónde va el candidato que aprueba, y es la misma que el backend
        /// escribe en la ficha de pre-ingreso.
        ///
        /// El nombre que se devuelve es el del nodo y no su rama completa. No es una simplificación
        /// visual: un puesto asociado a un área estándar tiene en ese nodo el primer área estándar
        /// de su rama, y uno asociado a un nodo de gerencia —los puestos de categoría gerente o
        /// gerente general, que cuelgan del nodo de su gerencia— tiene ahí su área real. En los dos
        /// casos el nodo propio ES el área, así que no hay ancestros que agregar.
        ///
        /// Null cuando el puesto no tiene área de destino: el padrón de GTH solo cubrió personal
        /// de oficina, así que los puestos de obra no la tienen y el finalista se cae al área del
        /// solicitante.
        /// </summary>
        private static async Task<OpcionDto?> QueryAreaDestinoDelPuesto(AppDbContext ctx, int? puestoId)
        {
            if (puestoId is not > 0) return null;

            return await (
                from p in ctx.Puesto
                where p.PuestoId == puestoId.Value && p.AreaDestinoScopeId != null
                join s in ctx.AreaScope on p.AreaDestinoScopeId equals s.AreaScopeId
                where s.State
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                where ai.State
                select new OpcionDto { Id = s.AreaScopeId, Nombre = ai.AreaItemName })
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Trabajadores del <c>area_scope</c> indicado y de todas sus áreas hijas, ordenados por
        /// nombre. Alimenta el desplegable "Trabajador al que reemplaza" y es también la lista
        /// contra la que se valida lo que llega del cliente, así que ambos usan este mismo método:
        /// lo que no se ofrece tampoco se acepta.
        ///
        /// Solo los que trabajan en Abril HOY: <c>workers_estado.esta_adentro = true</c> (ACTIVO e
        /// INHABILITADO_SSOMA). Antes no se filtraba por estado y se colaban dos cosas:
        /// - las fichas de PRE-INGRESO (FINALISTA_APROBADO / NO_INGRESO), que no son trabajadores.
        ///   Además ensuciaban el desplegable con repetidos: cada finalista aprobado abre una
        ///   <c>person</c> nueva, así que la deduplicación por <c>person_id</c> de más abajo no las
        ///   junta con la ficha real de esa misma persona y el mismo nombre salía varias veces;
        /// - los RETIRADOS, que ya no están en el área.
        ///
        /// Incluye al solicitante: pedir el reemplazo de uno mismo por renuncia o promoción es un
        /// caso real. Una persona con varias fichas por reingreso aparece una sola vez, con la
        /// vigente — ver <c>workers</c> duplicadas por reingreso.
        /// </summary>
        private static async Task<List<OpcionDto>> QueryTrabajadoresDelArea(AppDbContext ctx, int? areaScopeId)
        {
            if (!areaScopeId.HasValue) return new List<OpcionDto>();

            var idsArea = await ctx.ResolveDescendantsAsync(areaScopeId.Value);

            var raw = await ctx.Worker
                // El estado se filtra por id y no con un join a workers_estado: la consulta no
                // necesita ninguna otra columna del catálogo (ver WorkersEstadoIds.EstanAdentro).
                .Where(w => WorkersEstadoIds.EstanAdentro.Contains(w.WorkersEstadoId)
                         && w.AreaScopeId != null && idsArea.Contains(w.AreaScopeId.Value))
                .Select(w => new
                {
                    w.Id,
                    w.PersonId,
                    Nombre = w.Person != null ? w.Person.FullName : w.ApellidoNombre,
                    // Las dos fechas del último periodo laboral (ver WorkersPeriodoLaboral):
                    // antes eran columnas de `workers`. Solo se usan para elegir la ficha
                    // vigente entre las varias que puede tener una misma persona.
                    Vigente = w.PeriodosLaborales.Any(p => p.State && p.FechaRetiro == null),
                    FechaIngreso = w.PeriodosLaborales
                        .Where(p => p.State)
                        .OrderByDescending(p => p.FechaIngreso)
                        .ThenByDescending(p => p.WorkersPeriodoLaboralId)
                        .Select(p => (DateOnly?)p.FechaIngreso)
                        .FirstOrDefault(),
                })
                .AsNoTracking()
                .ToListAsync();

            return raw
                .Where(w => !string.IsNullOrWhiteSpace(w.Nombre))
                // Una ficha por persona: la vigente primero y, entre varias, la de ingreso más
                // reciente. Las fichas sin person_id no se agrupan (cada una es su propia clave).
                .GroupBy(w => w.PersonId.HasValue ? $"p{w.PersonId}" : $"w{w.Id}")
                .Select(g => g
                    .OrderByDescending(w => w.Vigente)
                    .ThenByDescending(w => w.FechaIngreso)
                    .ThenByDescending(w => w.Id)
                    .First())
                .Select(w => new OpcionDto { Id = w.Id, Nombre = w.Nombre! })
                .OrderBy(o => o.Nombre, StringComparer.CurrentCulture)
                .ToList();
        }

        public async Task<(string? AreaNombre, int? AreaScopeId, int? WorkerId)> ResolveSolicitante(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var ficha = await ResolveSolicitanteInternal(ctx, userId);
            return (ficha.AreaNombre, ficha.AreaScopeId, ficha.WorkerId);
        }

        public async Task<string?> GetSustentoFolderUrl()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.GthSustentoFolder
                .Where(f => f.State && f.Active)
                .OrderBy(f => f.GthSustentoFolderId)
                .Select(f => f.LinkUrl)
                .FirstOrDefaultAsync();
        }

        public async Task<SolicitantePanelDto> GetSolicitantePanel(SolicitudPersonalScope scope)
        {
            using var ctx = _factory.CreateDbContext();

            // El alcance es del ÁREA, no del usuario: ver EnScope. Los ids se materializan una vez
            // para que EF los traduzca a un `= ANY(...)` en cada consulta.
            var areaIds = scope.AreaScopeIds.ToList();
            var delArea = EnScope(
                ctx.GthRequerimiento.Where(x => x.State && x.Solicitud!.State), scope, areaIds);

            // Tabla "Solicitudes de vacante" del área (mismo proyectado de siempre).
            var misSolicitudes = await ProjectRequerimientos(ctx, delArea);

            // Tarjetas "Gestión de candidatos": requerimientos del área cuya long list ya fue
            // enviada por GTH (estado LONG_LIST_ENVIADA), con el conteo de candidatos vigentes.
            var cards = await (
                from r in delArea
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                where e.Codigo == EstadoReclutamiento.LongListEnviada
                orderby r.UpdatedDateTime descending, r.GthRequerimientoId descending
                select new GestionCandidatoCardDto
                {
                    RequerimientoId = r.GthRequerimientoId,
                    Codigo          = r.Codigo,
                    Puesto          = p.Nombre,
                    Area            = r.Solicitud!.AreaNombre,
                    ProyectoObra    = pr.ProjectDescription,
                    TotalCandidatos = CandidatosVigentes(ctx).Count(c => c.GthRequerimientoId == r.GthRequerimientoId),
                    EstadoCodigo    = e.Codigo,
                    EstadoNombre    = e.Nombre,
                    Tipo            = TipoGestionCandidato.LongList,
                }).ToListAsync();

            // Tarjetas "Finalistas enviados por GTH": requerimientos del área en la fase de
            // selección de jefatura (ahí los deja GTH al enviar a los finalistas) que todavía tienen
            // al menos un candidato evaluado en carrera. El filtro por SELECCION_JEFATURA es el que
            // hace desaparecer la tarjeta cuando el proceso se cierra (aprobó a un finalista) o
            // vuelve a LONG_LIST (rechazó a todos).
            //
            // Los ids de los resultados que sacan a un candidato de carrera se resuelven aparte
            // (consulta mínima al catálogo) para que el conteo por requerimiento sea una subconsulta
            // simple sobre ids en vez de un join anidado. NO_APTO_EMO entra igual que NO_PASO: un
            // retomado puede devolver el requerimiento a SELECCION_JEFATURA con el candidato que no
            // pasó el EMO todavía colgando del proceso, y contarlo inflaría la tarjeta.
            var fueraDeCarreraIds = await ctx.GthCandidatoResultado
                .Where(x => x.State && (x.Codigo == ResultadoCandidato.NoPaso
                                        || x.Codigo == ResultadoCandidato.NoAptoEmo))
                .Select(x => x.GthCandidatoResultadoId)
                .ToListAsync(); // vacío = catálogo sin sembrar: no descarta a nadie

            var finalistas = await (
                from r in delArea
                where CandidatosVigentes(ctx).Any(c => c.GthRequerimientoId == r.GthRequerimientoId
                            && ctx.GthCandidatoEvaluacion.Any(ev => ev.GthCandidatoId == c.GthCandidatoId
                                  && ev.State && !fueraDeCarreraIds.Contains(ev.GthCandidatoResultadoId)))
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                where e.Codigo == EstadoReclutamiento.SeleccionJefatura
                orderby r.UpdatedDateTime descending, r.GthRequerimientoId descending
                select new GestionCandidatoCardDto
                {
                    RequerimientoId = r.GthRequerimientoId,
                    Codigo          = r.Codigo,
                    Puesto          = p.Nombre,
                    Area            = r.Solicitud!.AreaNombre,
                    ProyectoObra    = pr.ProjectDescription,
                    TotalCandidatos = CandidatosVigentes(ctx).Count(c => c.GthRequerimientoId == r.GthRequerimientoId
                            && ctx.GthCandidatoEvaluacion.Any(ev => ev.GthCandidatoId == c.GthCandidatoId
                                  && ev.State && !fueraDeCarreraIds.Contains(ev.GthCandidatoResultadoId))),
                    EstadoCodigo    = e.Codigo,
                    EstadoNombre    = e.Nombre,
                    Tipo            = TipoGestionCandidato.Finalistas,
                }).ToListAsync();

            cards.AddRange(finalistas);

            // Tarjetas resumen. Se calculan sobre lo ya traído (sin roundtrips extra).
            //
            // "En revisión · GTH evaluando" = procesos cuyo siguiente paso le toca a GTH, sin contar
            // el primero (NUEVO, que es justamente "Pendientes · Sin respuesta"). Además se descartan
            // los que están esperando una acción del solicitante, que son exactamente los que ya
            // tienen tarjeta en "Gestión de candidatos": sería contradictorio mostrar el mismo
            // proceso como "GTH evaluando" y a la vez pedirle una decisión al solicitante. Las fases
            // de esas tarjetas (LONG_LIST_ENVIADA / SELECCION_JEFATURA) ya están fuera de FasesGth,
            // así que este descarte cubre solo a los requerimientos que se quedaron en una fase
            // anterior al cambio a SELECCION_JEFATURA con sus finalistas ya enviados.
            var esperandoAlSolicitante = cards.Select(c => c.RequerimientoId).ToHashSet();

            var anioActual = DateTimeOffset.UtcNow.ToOffset(PeruOffset).Year;

            var resumen = new ResumenSolicitantePanelDto
            {
                TotalRegistradas = misSolicitudes.Count,
                // "Pendientes · Sin respuesta" = nadie movió todavía el requerimiento. Con el nuevo
                // flujo eso es APROBACION_GG (esperando a Gerencia General); se sigue contando NUEVO
                // por los requerimientos anteriores a ese cambio.
                Pendientes       = misSolicitudes.Count(s => s.EstadoCodigo == EstadoReclutamiento.Nuevo
                                                             || s.EstadoCodigo == EstadoReclutamiento.AprobacionGg),
                EnRevisionGth    = misSolicitudes.Count(s =>
                                       EstadoReclutamiento.FasesGth.Contains(s.EstadoCodigo)
                                       && !esperandoAlSolicitante.Contains(s.RequerimientoId)),
                // "Este período" = año en curso (hora Perú), el mismo que rotula la cabecera de la
                // página. Se toma la fecha de envío porque es la que define a qué año pertenece el
                // requerimiento en la tabla.
                Aprobadas        = misSolicitudes.Count(s =>
                                       s.EstadoCodigo == EstadoReclutamiento.Cerrado
                                       && s.Enviado.Year == anioActual),
            };

            return new SolicitantePanelDto
            {
                Resumen           = resumen,
                GestionCandidatos = cards,
                MisSolicitudes    = misSolicitudes,
                PuedeGestionar    = scope.PuedeGestionar,
            };
        }

        /// <summary>
        /// Recorta una consulta de requerimientos al alcance del usuario en «Solicitud de Personal»:
        /// los de su área y los de las áreas que cuelgan de ella, más —siempre— los que él mismo
        /// registró. El requerimiento es del ÁREA y no de quien lo pidió: si el solicitante se va de
        /// la empresa, su jefatura y sus compañeros tienen que poder continuar el proceso.
        ///
        /// Una solicitud sin <c>area_scope_id</c> (no se pudo resolver al registrarla) solo la ve
        /// quien la registró y quien ve todo: no hay a qué área atribuirla.
        /// </summary>
        /// <param name="areaIds">
        /// <c>scope.AreaScopeIds</c> ya materializado. Va por parámetro y no se saca del scope acá
        /// adentro para no rearmar la lista en cada consulta del mismo panel.
        /// </param>
        private static IQueryable<GthRequerimiento> EnScope(
            IQueryable<GthRequerimiento> reqs, SolicitudPersonalScope scope, List<int> areaIds) =>
            scope.VeTodo
                ? reqs
                : reqs.Where(r => r.Solicitud!.SolicitanteUserId == scope.UserId
                                  || (r.Solicitud.AreaScopeId != null
                                      && areaIds.Contains(r.Solicitud.AreaScopeId.Value)));

        /// <summary>
        /// <see cref="EnScope(IQueryable{GthRequerimiento}, SolicitudPersonalScope, List{int})"/>
        /// para las consultas de UN requerimiento, que no repiten el filtro y no ganan nada
        /// materializando los ids aparte.
        /// </summary>
        private static IQueryable<GthRequerimiento> EnScope(
            IQueryable<GthRequerimiento> reqs, SolicitudPersonalScope scope) =>
            EnScope(reqs, scope, scope.AreaScopeIds.ToList());

        public async Task<RevisionLongListDto?> GetRevisionLongList(
            int requerimientoId, SolicitudPersonalScope scope)
        {
            using var ctx = _factory.CreateDbContext();

            // Cabecera del requerimiento (scope: el área del usuario, ver EnScope) que ya tenga la
            // long list enviada (LONG_LIST_ENVIADA o posterior).
            var head = await (
                from r in EnScope(ctx.GthRequerimiento, scope)
                where r.GthRequerimientoId == requerimientoId
                      && r.State && r.Solicitud!.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                select new
                {
                    r.GthRequerimientoId,
                    r.Codigo,
                    Puesto       = p.Nombre,
                    Area         = r.Solicitud!.AreaNombre,
                    ProyectoObra = pr.ProjectDescription,
                    EstadoCodigo = e.Codigo,
                    EstadoNombre = e.Nombre,
                    EstadoOrden  = e.Orden,
                }).FirstOrDefaultAsync();

            if (head == null) return null;

            // La revisión solo está disponible una vez que GTH envió la long list.
            var longListEnviadaOrden = await ctx.GthEstadoRequerimiento
                .Where(e => e.Codigo == EstadoReclutamiento.LongListEnviada && e.State)
                .Select(e => (int?)e.Orden)
                .FirstOrDefaultAsync();
            if (longListEnviadaOrden == null || head.EstadoOrden < longListEnviadaOrden)
                return null;

            var candidatos = await (
                from c in CandidatosVigentes(ctx)
                where c.GthRequerimientoId == requerimientoId
                join est in ctx.GthCandidatoEstado on c.GthCandidatoEstadoId equals est.GthCandidatoEstadoId
                orderby c.Orden, c.GthCandidatoId
                select new CandidatoRevisionDto
                {
                    CandidatoId   = c.GthCandidatoId,
                    Nombre        = c.Nombre,
                    Puesto        = c.Puesto,
                    Comentario    = c.Comentario,
                    CvNombre      = c.CvNombre,
                    CvUrl         = c.CvUrl,
                    EstadoCodigo  = est.Codigo,
                    EstadoNombre  = est.Nombre,
                }).ToListAsync();

            // Portafolio/anexos de todos los candidatos en una sola consulta (no una por
            // candidato), igual que las evaluaciones de los finalistas.
            var anexos = await QueryAnexos(ctx, candidatos.Select(c => c.CandidatoId).ToList());
            foreach (var c in candidatos)
                if (anexos.TryGetValue(c.CandidatoId, out var suyos)) c.Anexos = suyos;

            return new RevisionLongListDto
            {
                RequerimientoId = head.GthRequerimientoId,
                Codigo          = head.Codigo,
                Puesto          = head.Puesto,
                Area            = head.Area,
                ProyectoObra    = head.ProyectoObra,
                EstadoCodigo    = head.EstadoCodigo,
                EstadoNombre    = head.EstadoNombre,
                Candidatos      = candidatos,
            };
        }

        public async Task<LongListDecisionContextoDto> RegistrarDecisionLongList(
            int requerimientoId, List<CandidatoDecisionDto> decisiones, SolicitudPersonalScope scope)
        {
            using var ctx = _factory.CreateDbContext();

            // Quien decide ya no es forzosamente quien registró la solicitud: puede ser cualquier
            // jefatura del área (ver EnScope). Lo que se graba en la trazabilidad y lo que firma el
            // correo a GTH es quien tomó ESTA decisión.
            var userId = scope.UserId;

            // Cabecera + estado actual, con scope al área del usuario. Se trae la entidad del
            // requerimiento (r) para mutarla; EF la rastrea aunque venga en un anónimo.
            var head = await (
                from r in EnScope(ctx.GthRequerimiento, scope)
                where r.GthRequerimientoId == requerimientoId
                      && r.State && r.Solicitud!.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                select new
                {
                    Req          = r,
                    r.Codigo,
                    Puesto       = p.Nombre,
                    Area         = r.Solicitud!.AreaNombre,
                    ProyectoObra = pr.ProjectDescription,
                    EstadoCodigo = e.Codigo,
                }).FirstOrDefaultAsync();

            if (head == null)
                throw new AbrilException("No se encontró la long list del requerimiento.", 404);

            // La decisión solo se registra una vez, cuando GTH ya envió la long list (LONG_LIST_ENVIADA).
            if (head.EstadoCodigo != EstadoReclutamiento.LongListEnviada)
                throw new AbrilException("Esta long list ya fue revisada o aún no está disponible para revisión.", 409);

            var candidatos = await CandidatosVigentes(ctx)
                .Where(c => c.GthRequerimientoId == requerimientoId)
                .OrderBy(c => c.Orden).ThenBy(c => c.GthCandidatoId)
                .ToListAsync();
            if (candidatos.Count == 0)
                throw new AbrilException("La long list no tiene candidatos para revisar.", 400);

            // Decisión recibida por candidato; deben cubrir exactamente a los candidatos vigentes.
            var decisionPorId = new Dictionary<int, bool>();
            foreach (var d in decisiones) decisionPorId[d.CandidatoId] = d.Aprobado;
            if (candidatos.Any(c => !decisionPorId.ContainsKey(c.GthCandidatoId)))
                throw new AbrilException("Debes aprobar o rechazar a todos los candidatos antes de enviar la decisión.", 400);

            var estadosCand = await ctx.GthCandidatoEstado
                .Where(e => e.State && (e.Codigo == EstadoCandidato.Aprobado || e.Codigo == EstadoCandidato.Rechazado))
                .ToListAsync();
            var aprobadoId = estadosCand.FirstOrDefault(e => e.Codigo == EstadoCandidato.Aprobado)?.GthCandidatoEstadoId
                ?? throw new AbrilException("No está configurado el estado APROBADO de candidatos.", 500);
            var rechazadoId = estadosCand.FirstOrDefault(e => e.Codigo == EstadoCandidato.Rechazado)?.GthCandidatoEstadoId
                ?? throw new AbrilException("No está configurado el estado RECHAZADO de candidatos.", 500);

            var now = DateTimeOffset.UtcNow;
            int aprobados = 0, rechazados = 0;
            foreach (var c in candidatos)
            {
                var aprobado = decisionPorId[c.GthCandidatoId];
                c.GthCandidatoEstadoId = aprobado ? aprobadoId : rechazadoId;
                // La fecha de la decisión va en su propia columna: `updated_date_time` la pisa el
                // envío de la siguiente long list (da de baja la anterior), y el historial de
                // rechazados necesita justo la fecha en que el solicitante los rechazó.
                c.DecisionDateTime     = now;
                c.DecisionUserId       = userId;
                c.UpdatedDateTime      = now;
                c.UpdatedUserId        = userId;
                if (aprobado) aprobados++; else rechazados++;
            }

            // ≥1 aprobado → LONG_LIST_APROBADA. 0 aprobados → de vuelta a LONG_LIST para que GTH
            // vuelva a enviar una long list (los rechazados quedan grabados como data histórica).
            var todosRechazados = aprobados == 0;
            var codigoDestino   = todosRechazados ? EstadoReclutamiento.LongList : EstadoReclutamiento.LongListAprobada;
            var estadoDestino   = await ctx.GthEstadoRequerimiento
                .FirstOrDefaultAsync(e => e.Codigo == codigoDestino && e.State)
                ?? throw new AbrilException($"No está configurado el estado {codigoDestino} de reclutamiento.", 500);

            head.Req.GthEstadoRequerimientoId = estadoDestino.GthEstadoRequerimientoId;
            head.Req.UpdatedDateTime          = now;
            head.Req.UpdatedUserId            = userId;

            // Nombre del solicitante para el cuerpo del correo (best-effort; no bloquea).
            var solicitanteNombre = await ctx.Worker
                .Where(w => w.Person != null && w.Person.UserId == userId)
                .Select(w => w.Person!.FullName ?? w.ApellidoNombre)
                .FirstOrDefaultAsync();

            await ctx.SaveChangesAsync();

            return new LongListDecisionContextoDto
            {
                Resultado = new LongListDecisionResultDto
                {
                    EstadoCodigo    = estadoDestino.Codigo,
                    EstadoNombre    = estadoDestino.Nombre,
                    Aprobados       = aprobados,
                    Rechazados      = rechazados,
                    TodosRechazados = todosRechazados,
                },
                Codigo            = head.Codigo,
                Puesto            = head.Puesto,
                Area              = head.Area,
                ProyectoObra      = head.ProyectoObra,
                SolicitanteNombre = solicitanteNombre,
                Candidatos        = candidatos.Select(c => new CandidatoDecididoDto
                {
                    Nombre   = c.Nombre,
                    Puesto   = c.Puesto,
                    Aprobado = decisionPorId[c.GthCandidatoId],
                }).ToList(),
            };
        }

        public async Task<BandejaReclutamientoDto> GetBandeja()
        {
            using var ctx = _factory.CreateDbContext();

            // Todos los requerimientos vigentes (de cualquier área), más recientes primero, EXCEPTO
            // los que todavía no le pertenecen a GTH: los que esperan la aprobación de Gerencia
            // General y los que el GG rechazó (EstadoReclutamiento.FueraDeGth). GTH solo ve lo
            // aprobado. Como el embudo del pipeline se calcula sobre esta misma lista, la suma de
            // las etapas sigue siendo el total de la tabla.
            // Left join a gth_prioridad porque la prioridad es opcional (nullable).
            var raw = await (
                from r in ctx.GthRequerimiento
                where r.State && r.Solicitud!.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                where !EstadoReclutamiento.FueraDeGth.Contains(e.Codigo)
                join prio in ctx.GthPrioridad on r.GthPrioridadId equals prio.GthPrioridadId into prioJoin
                from prio in prioJoin.DefaultIfEmpty()
                orderby r.CreatedDateTime descending, r.GthRequerimientoId descending
                select new
                {
                    r.GthRequerimientoId,
                    r.Codigo,
                    Area            = r.Solicitud!.AreaNombre,
                    Puesto          = p.Nombre,
                    ProyectoObra    = pr.ProjectDescription,
                    r.CreatedDateTime,
                    PrioridadId     = prio != null ? (int?)prio.GthPrioridadId : null,
                    PrioridadNombre = prio != null ? prio.Nombre : null,
                    EstadoCodigo    = e.Codigo,
                    EstadoNombre    = e.Nombre,
                }).ToListAsync();

            // Conversión a hora Perú en memoria (evita traducir ToOffset en el join).
            var solicitudes = raw.Select(x => new RequerimientoGthListItemDto
            {
                RequerimientoId       = x.GthRequerimientoId,
                Codigo                = x.Codigo,
                Area                  = x.Area,
                Puesto                = x.Puesto,
                ProyectoObra          = x.ProyectoObra,
                FechaLlegada          = x.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                PrioridadId           = x.PrioridadId,
                PrioridadNombre       = x.PrioridadNombre,
                EstadoCodigo          = x.EstadoCodigo,
                EstadoNombre          = x.EstadoNombre,
            }).ToList();

            // Catálogo de prioridades para el desplegable de la columna (orden semántico Alta→Media→Baja).
            var prioridades = await ctx.GthPrioridad
                .Where(p => p.State && p.Active)
                .OrderBy(p => p.Orden)
                .Select(p => new OpcionDto { Id = p.GthPrioridadId, Nombre = p.Nombre })
                .ToListAsync();

            // "Evaluaciones · Programadas": entrevistas ya agendadas cuyo resultado GTH todavía no
            // cierra (sin evaluación registrada, o registrada pero aún en PENDIENTE). Una vez que el
            // candidato queda en PASO / NO_PASO / SELECCIONADO / RECHAZADO deja de estar programada.
            var evaluacionesProgramadas = await (
                from ent in ctx.GthEntrevista
                join c in CandidatosVigentes(ctx) on ent.GthCandidatoId equals c.GthCandidatoId
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                where ent.State && r.State
                      && !(from ev in ctx.GthCandidatoEvaluacion
                           join res in ctx.GthCandidatoResultado
                               on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                           where ev.GthCandidatoId == c.GthCandidatoId && ev.State
                                 && res.Codigo != ResultadoCandidato.Pendiente
                           select ev.GthCandidatoEvaluacionId).Any()
                select ent.GthEntrevistaId).CountAsync();

            // ── Embudo del pipeline y tarjetas de resumen ─────────────────────────────────
            // Se calculan sobre `solicitudes`, que ya está materializado (sin roundtrips extra).
            var totalPorFase = solicitudes
                .GroupBy(s => s.EstadoCodigo)
                .ToDictionary(g => g.Key, g => g.Count());

            var pipeline = EtapaPipeline.Todas
                .Select(etapa => new PipelineEtapaDto
                {
                    Codigo = etapa.Codigo,
                    Nombre = etapa.Nombre,
                    Total  = etapa.Fases.Sum(f => totalPorFase.GetValueOrDefault(f)),
                })
                .ToList();

            var cerrados = pipeline.First(e => e.Codigo == EtapaPipeline.CodigoCierre).Total;

            // "Vacantes abiertas" = ya publicadas y todavía en curso: las etapas desde Publicado
            // hasta la anterior a Cierre. Se recorta por posición para no repetir la lista de fases.
            var desdePublicado = Array.FindIndex(EtapaPipeline.Todas, e => e.Codigo == EtapaPipeline.CodigoPublicado);
            var hastaCierre    = Array.FindIndex(EtapaPipeline.Todas, e => e.Codigo == EtapaPipeline.CodigoCierre);
            var vacantesAbiertas = pipeline
                .Take(hastaCierre)
                .Skip(desdePublicado)
                .Sum(e => e.Total);

            // "Este período" = año en curso (hora Perú), el mismo que rotula la cabecera de la página.
            var anioActual = DateTimeOffset.UtcNow.ToOffset(PeruOffset).Year;

            var resumen = new ResumenReclutamientoDto
            {
                // Lo cerrado ya no está en curso, así que sale de "En proceso".
                EnProceso               = solicitudes.Count - cerrados,
                VacantesAbiertas        = vacantesAbiertas,
                EvaluacionesProgramadas = evaluacionesProgramadas,
                ProcesosCerrados        = solicitudes.Count(s => s.EstadoCodigo == EstadoReclutamiento.Cerrado
                                                                 && s.FechaLlegada.Year == anioActual),
                // "Solicitudes nuevas" = las que acaban de llegar a GTH. Con el paso de Gerencia
                // General eso es VALIDACION_GTH; se sigue contando NUEVO por los requerimientos
                // anteriores a ese cambio, que se quedaron en esa fase.
                SolicitudesNuevas       = totalPorFase.GetValueOrDefault(EstadoReclutamiento.Nuevo)
                                          + totalPorFase.GetValueOrDefault(EstadoReclutamiento.ValidacionGth),
            };

            return new BandejaReclutamientoDto
            {
                Resumen     = resumen,
                Pipeline    = pipeline,
                Solicitudes = solicitudes,
                Prioridades = prioridades,
            };
        }

        public async Task UpdatePrioridad(int requerimientoId, int prioridadId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var prioridadOk = await ctx.GthPrioridad
                .AnyAsync(p => p.GthPrioridadId == prioridadId && p.State && p.Active);
            if (!prioridadOk)
                throw new AbrilException("La prioridad seleccionada no es válida.", 400);

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State);
            if (req == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);

            req.GthPrioridadId  = prioridadId;
            req.UpdatedDateTime = DateTimeOffset.UtcNow;
            req.UpdatedUserId   = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task<DetalleRequerimientoGthDto?> GetDetalleGth(int requerimientoId)
        {
            using var ctx = _factory.CreateDbContext();

            // Cabecera + asignación interna actual (sin scope por usuario: es la vista de GTH).
            var head = await (
                from r in ctx.GthRequerimiento
                where r.GthRequerimientoId == requerimientoId && r.State && r.Solicitud!.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join t in ctx.GthTipoRequerimiento on r.GthTipoRequerimientoId equals t.GthTipoRequerimientoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                // Trabajador reemplazado: left join porque solo lo tienen las vacantes de tipo
                // Reemplazo registradas desde que se pide ese dato.
                join wr in ctx.Worker on r.ReemplazaWorkerId equals (int?)wr.Id into reemplazaJoin
                from wr in reemplazaJoin.DefaultIfEmpty()
                // Tipo del documento del candidato FFT: left join por lo mismo que el de arriba.
                join td in ctx.GthTipoDocumento on r.GthTipoDocumentoId equals (int?)td.GthTipoDocumentoId into tipoDocJoin
                from td in tipoDocJoin.DefaultIfEmpty()
                select new
                {
                    r.GthRequerimientoId,
                    r.Codigo,
                    Puesto       = p.Nombre,
                    Tipo         = t.Nombre,
                    Area         = r.Solicitud!.AreaNombre,
                    // Área a la que entrará el contratado: la de destino del puesto. Sale del
                    // mismo join que ya trae el puesto, así que no cuesta un viaje extra.
                    AreaDestino  = p.AreaDestinoScope != null && p.AreaDestinoScope.State
                                   && p.AreaDestinoScope.AreaItem != null
                                       ? p.AreaDestinoScope.AreaItem.AreaItemName
                                       : null,
                    ProyectoObra = pr.ProjectDescription,
                    TrabajadorReemplazado = wr == null ? null
                        : (wr.Person != null ? wr.Person.FullName : wr.ApellidoNombre),
                    r.SalarioBrutoMensual,
                    r.EsFft,
                    r.FftCandidatoNombre,
                    r.FftCandidatoDocumento,
                    FftTipoDocumento = td != null ? td.Nombre : null,
                    r.FftCandidatoCorreo,
                    EstadoCodigo = e.Codigo,
                    EstadoNombre = e.Nombre,
                    r.GthResponsableProcesoId,
                    r.GthTipoProcesoId,
                    r.GthPrioridadId,
                    r.ContributorId,
                }).FirstOrDefaultAsync();

            if (head == null) return null;

            // Responsables del proceso: los reclutadores activos, que administra la pantalla
            // Configuración → Reclutadores (tabla filtro gth_responsable_proceso, aparte de
            // workers). Se excluye a los retirados acá y no en la pantalla: allá siguen
            // listados justamente para poder apagarlos.
            var responsables = await ctx.GthResponsableProceso
                .Where(rp => rp.State && rp.Active
                          && WorkersEstadoIds.EstanAdentro.Contains(rp.Worker!.WorkersEstadoId))
                .Select(rp => new OpcionDto
                {
                    Id     = rp.GthResponsableProcesoId,
                    Nombre = rp.Worker!.Person!.FullName ?? rp.Worker.ApellidoNombre ?? "",
                })
                .OrderBy(o => o.Nombre)
                .ToListAsync();

            var tiposProceso = await ctx.GthTipoProceso
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.Orden)
                .Select(t => new TipoProcesoOpcionDto
                {
                    Id          = t.GthTipoProcesoId,
                    Nombre      = t.Nombre,
                    SlaDias     = t.SlaDias,
                })
                .ToListAsync();

            var prioridades = await ctx.GthPrioridad
                .Where(p => p.State && p.Active)
                .OrderBy(p => p.Orden)
                .Select(p => new OpcionDto { Id = p.GthPrioridadId, Nombre = p.Nombre })
                .ToListAsync();

            // Razones sociales activas del grupo con sus cupos. La cuenta vive en Shared porque
            // la comparte con «Programar EMO con clínica», que se la pide a un finalista de ingreso
            // directo que llegó sin razón social (ver RazonSocialCuposHelper).
            var razonesSociales = await RazonSocialCuposHelper.ListarAsync(ctx);

            // Canales de publicación + publicaciones ya registradas de este requerimiento.
            var canales = await ctx.GthCanalPublicacion
                .Where(c => c.State && c.Active)
                .OrderBy(c => c.Orden)
                .Select(c => new CanalPublicacionDto
                {
                    Id     = c.GthCanalPublicacionId,
                    Nombre = c.Nombre,
                })
                .ToListAsync();

            var publicados = await ctx.GthRequerimientoCanal
                .Where(rc => rc.GthRequerimientoId == requerimientoId && rc.State && rc.Active)
                .Select(rc => rc.GthCanalPublicacionId)
                .ToListAsync();
            foreach (var c in canales)
                c.Publicado = publicados.Contains(c.Id);

            // Candidatos aprobados por el solicitante (para la fase "Long list aprobada", imágenes 4/5).
            // Vacío en fases anteriores; los rechazados no se incluyen.
            var candidatosAprobadosRaw = await (
                from c in CandidatosVigentes(ctx)
                where c.GthRequerimientoId == requerimientoId
                join est in ctx.GthCandidatoEstado on c.GthCandidatoEstadoId equals est.GthCandidatoEstadoId
                where est.Codigo == EstadoCandidato.Aprobado
                orderby c.Orden, c.GthCandidatoId
                select new
                {
                    c.GthCandidatoId,
                    c.Nombre,
                    c.Puesto,
                    c.CvNombre,
                    c.CvUrl,
                    c.MultitestRealizado,
                }).ToListAsync();

            // Formulario del postulante de cada candidato aprobado (0..1 por candidato) para saber, por
            // candidato, si GTH ya lo envió y en qué fase está. Se trae en un segundo roundtrip pequeño
            // (acotado a los candidatos aprobados) en vez de un left join a subconsulta: EF Core no
            // materializa bien ese patrón —proyección de la subconsulta con value types nullable +
            // "fr != null ? fr.Campo : null"— y lanza "Nullable object must have a value" al iterar
            // cuando el candidato aún no tiene formulario. Se une en memoria por GthCandidatoId.
            var candidatoIds = candidatosAprobadosRaw.Select(x => x.GthCandidatoId).ToList();
            var formulariosRaw = candidatoIds.Count == 0
                ? new List<FormularioRawRow>()
                : await (
                    from f in ctx.GthPostulanteFormulario
                    where f.State && candidatoIds.Contains(f.GthCandidatoId)
                    join fe in ctx.GthPostulanteFormularioEstado on f.GthPostulanteFormularioEstadoId equals fe.GthPostulanteFormularioEstadoId
                    select new FormularioRawRow
                    {
                        GthCandidatoId     = f.GthCandidatoId,
                        EstadoCodigo       = fe.Codigo,
                        EstadoNombre       = fe.Nombre,
                        CorreoEnvio        = f.CorreoEnvio,
                        CorreoElectronico  = f.CorreoElectronico,
                        EnviadoDateTime    = f.EnviadoDateTime,
                        CompletadoDateTime = f.CompletadoDateTime,
                        RevisadoNombre     = f.RevisadoNombre,
                        RevisadoDateTime   = f.RevisadoDateTime,
                        CvNombre           = f.CvNombreOriginal ?? f.CvNombre,
                        CvUrl              = f.CvUrl,
                    }).ToListAsync();

            var formulariosPorCandidato = formulariosRaw.ToDictionary(x => x.GthCandidatoId, x => new CandidatoFormularioResumenDto
            {
                EstadoCodigo   = x.EstadoCodigo,
                EstadoNombre   = x.EstadoNombre,
                CorreoEnvio    = x.CorreoEnvio,
                EnviadoEn      = x.EnviadoDateTime?.ToOffset(PeruOffset).DateTime,
                CompletadoEn   = x.CompletadoDateTime?.ToOffset(PeruOffset).DateTime,
                RevisadoNombre = x.RevisadoNombre,
                RevisadoEn     = x.RevisadoDateTime?.ToOffset(PeruOffset).DateTime,
            });

            // Correo de contacto para la invitación a la entrevista: el que el postulante declaró
            // en el formulario y, si no lo declaró, aquel al que GTH le envió el enlace.
            var correosPorCandidato = formulariosRaw.ToDictionary(
                x => x.GthCandidatoId,
                x => string.IsNullOrWhiteSpace(x.CorreoElectronico) ? x.CorreoEnvio : x.CorreoElectronico!);

            // Entrevista programada de cada candidato (0..1 vigente por candidato).
            var entrevistasPorCandidato = candidatoIds.Count == 0
                ? new Dictionary<int, EntrevistaResumenDto>()
                : (await (
                        from en in ctx.GthEntrevista
                        where en.State && candidatoIds.Contains(en.GthCandidatoId)
                        join l in ctx.GthLugarEntrevista on en.GthLugarEntrevistaId equals l.GthLugarEntrevistaId
                        // La respuesta del candidato es opcional (null mientras no conteste), así
                        // que va en left join: con un join normal desaparecerían del modal las
                        // entrevistas que todavía están esperando respuesta.
                        join rp in ctx.GthEntrevistaRespuesta
                            on en.GthEntrevistaRespuestaId equals rp.GthEntrevistaRespuestaId into resp
                        from rp in resp.DefaultIfEmpty()
                        select new
                        {
                            en.GthCandidatoId,
                            en.Fecha,
                            en.Hora,
                            en.GthLugarEntrevistaId,
                            LugarNombre = l.Nombre,
                            en.CorreoEnvio,
                            en.EnviadoDateTime,
                            RespuestaCodigo = rp != null ? rp.Codigo : null,
                            RespuestaNombre = rp != null ? rp.Nombre : null,
                            en.RespuestaDateTime,
                        }).ToListAsync())
                    .ToDictionary(x => x.GthCandidatoId, x => new EntrevistaResumenDto
                    {
                        Fecha           = x.Fecha,
                        Hora            = x.Hora.ToString("HH\\:mm"),
                        LugarId         = x.GthLugarEntrevistaId,
                        LugarNombre     = x.LugarNombre,
                        CorreoEnvio     = x.CorreoEnvio,
                        EnviadoEn       = x.EnviadoDateTime?.ToOffset(PeruOffset).DateTime,
                        RespuestaCodigo = x.RespuestaCodigo,
                        RespuestaNombre = x.RespuestaNombre,
                        RespondidoEn    = x.RespuestaDateTime?.ToOffset(PeruOffset).DateTime,
                    });

            // Evaluación de la entrevista de cada candidato (0..1 vigente por candidato): puntajes,
            // comentarios del informe y resultado (incluido el correo de agradecimiento si no continúa).
            var evaluacionesPorCandidato = (await QueryEvaluaciones(ctx, candidatoIds))
                .ToDictionary(x => x.GthCandidatoId, MapEvaluacion);

            // Documentos declarados que ya existen en la base. Va acá y no solo en el modal «Ver
            // formulario» porque los botones Aprobar/Rechazar también viven en la ficha de cada
            // candidato: sin esto GTH podría aprobar sin haber visto nunca el aviso. Un solo
            // roundtrip para todos los candidatos de la lista.
            var coincidenciasPorCandidato = await CoincidenciaPersonaQuery.ResolverAsync(ctx, candidatoIds);

            // CV documentado del postulante, indexado por candidato: sale de la misma consulta de
            // formularios que ya se hizo arriba, así que no cuesta un roundtrip nuevo.
            var cvPostulantePorCandidato = formulariosRaw
                .Where(x => x.CvUrl != null)
                .ToDictionary(x => x.GthCandidatoId, x => x);

            var candidatosAprobados = candidatosAprobadosRaw.Select(x => new CandidatoAprobadoDto
            {
                CandidatoId        = x.GthCandidatoId,
                Nombre             = x.Nombre,
                Puesto             = x.Puesto,
                CvNombre           = x.CvNombre,
                CvUrl              = x.CvUrl,
                CvPostulanteNombre = cvPostulantePorCandidato.GetValueOrDefault(x.GthCandidatoId)?.CvNombre,
                CvPostulanteUrl    = cvPostulantePorCandidato.GetValueOrDefault(x.GthCandidatoId)?.CvUrl,
                Formulario         = formulariosPorCandidato.GetValueOrDefault(x.GthCandidatoId),
                Coincidencia       = coincidenciasPorCandidato.GetValueOrDefault(x.GthCandidatoId),
                MultitestRealizado = x.MultitestRealizado,
                // En FFT el correo del candidato lo declaró el solicitante, así que el campo del
                // envío del formulario ya sale lleno: es todo lo que GTH tiene que hacer y no hay
                // por qué obligarlo a tipearlo. Cuando el formulario ya salió manda el del envío.
                CorreoContacto     = correosPorCandidato.GetValueOrDefault(x.GthCandidatoId)
                                     ?? (head.EsFft ? head.FftCandidatoCorreo : null),
                Entrevista         = entrevistasPorCandidato.GetValueOrDefault(x.GthCandidatoId),
                Evaluacion         = evaluacionesPorCandidato.GetValueOrDefault(x.GthCandidatoId),
            }).ToList();

            // Catálogo de lugares para el desplegable de programación de entrevistas.
            var lugaresEntrevista = await ctx.GthLugarEntrevista
                .Where(l => l.State && l.Active)
                .OrderBy(l => l.Orden).ThenBy(l => l.Nombre)
                .Select(l => new OpcionDto { Id = l.GthLugarEntrevistaId, Nombre = l.Nombre })
                .ToListAsync();

            // Historial de rechazados (incluye los de long lists anteriores). Va en esta misma
            // petición: es una sección más del modal, no una pantalla aparte.
            var candidatosRechazados = await QueryCandidatosRechazados(ctx, requerimientoId);

            // Quién obtuvo el puesto (null mientras el proceso no cierre con un seleccionado).
            var seleccionado = await QuerySeleccionado(ctx, requerimientoId);

            return new DetalleRequerimientoGthDto
            {
                RequerimientoId       = head.GthRequerimientoId,
                Codigo                = head.Codigo,
                Puesto                = head.Puesto,
                Area                  = head.Area,
                AreaDestino           = head.AreaDestino,
                ProyectoObra          = head.ProyectoObra,
                TipoRequerimiento     = head.Tipo,
                TrabajadorReemplazado = head.TrabajadorReemplazado,
                SalarioBrutoMensual   = head.SalarioBrutoMensual,
                EsFft                 = head.EsFft,
                FftCandidatoNombre    = head.FftCandidatoNombre,
                // Con su tipo: «DNI 12345678». Los FFT anteriores al desplegable eran todos DNI.
                FftCandidatoDocumento = FftDocumento.Texto(
                    head.FftTipoDocumento ?? FftDocumento.Dni, head.FftCandidatoDocumento),
                Vacantes              = 1, // cada vacante de una solicitud genera su propio requerimiento
                EstadoCodigo          = head.EstadoCodigo,
                EstadoNombre          = head.EstadoNombre,
                Asignacion = new AsignacionGthDto
                {
                    ResponsableId = head.GthResponsableProcesoId,
                    TipoProcesoId = head.GthTipoProcesoId,
                    PrioridadId   = head.GthPrioridadId,
                    ContributorId = head.ContributorId,
                },
                Responsables    = responsables,
                TiposProceso    = tiposProceso,
                Prioridades     = prioridades,
                RazonesSociales     = razonesSociales,
                Canales             = canales,
                LugaresEntrevista   = lugaresEntrevista,
                CandidatosAprobados = candidatosAprobados,
                CandidatosRechazados = candidatosRechazados,
                Seleccionado         = seleccionado,
            };
        }

        public async Task SetMultitest(int candidatoId, bool realizado, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var cand = await ctx.GthCandidato
                .FirstOrDefaultAsync(c => c.GthCandidatoId == candidatoId && c.State);
            if (cand == null)
                throw new AbrilException("Candidato no encontrado.", 404);

            var now = DateTimeOffset.UtcNow;
            cand.MultitestRealizado = realizado;
            cand.MultitestDateTime  = now;
            cand.MultitestUserId    = userId;
            cand.UpdatedDateTime    = now;
            cand.UpdatedUserId      = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task<EstadoRequerimientoResultDto> ContinuarAEntrevistas(int requerimientoId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State);
            if (req == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);

            var estados = await ctx.GthEstadoRequerimiento
                .Where(e => e.State && (e.Codigo == EstadoReclutamiento.LongListAprobada
                                        || e.Codigo == EstadoReclutamiento.Entrevistas
                                        || e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId))
                .ToListAsync();
            var longListAprobada = estados.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.LongListAprobada)
                ?? throw new AbrilException("No está configurado el estado LONG_LIST_APROBADA de reclutamiento.", 500);
            var entrevistas = estados.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.Entrevistas)
                ?? throw new AbrilException("No está configurado el estado ENTREVISTAS de reclutamiento.", 500);
            var actual = estados.FirstOrDefault(e => e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId);

            // Idempotente: si ya está en Entrevistas (o más adelante) no se retrocede ni se revalida.
            if (actual != null && actual.Orden >= entrevistas.Orden)
                return new EstadoRequerimientoResultDto { EstadoCodigo = actual.Codigo, EstadoNombre = actual.Nombre };

            if (actual == null || actual.Orden < longListAprobada.Orden)
                throw new AbrilException("El solicitante aún no aprobó la long list de este requerimiento.", 400);

            // Requisitos para pasar a entrevistas (mismos que habilitan el botón en la vista de GTH):
            // todos los formularios ya revisados (aprobados o rechazados), al menos uno aprobado y
            // el Multitest marcado en los candidatos que siguen en carrera.
            var candidatos = await (
                from c in CandidatosVigentes(ctx)
                where c.GthRequerimientoId == requerimientoId
                join est in ctx.GthCandidatoEstado on c.GthCandidatoEstadoId equals est.GthCandidatoEstadoId
                where est.Codigo == EstadoCandidato.Aprobado
                select new { c.GthCandidatoId, c.MultitestRealizado }).ToListAsync();

            if (candidatos.Count == 0)
                throw new AbrilException("No hay candidatos aprobados por el solicitante en este requerimiento.", 400);

            // Estado del formulario de cada candidato (segundo roundtrip acotado: el left join
            // encadenado formulario→estado no lo materializa bien EF Core, ver GetDetalleGth).
            var idsCandidatos = candidatos.Select(c => c.GthCandidatoId).ToList();
            var estadoFormPorCandidato = (await (
                    from f in ctx.GthPostulanteFormulario
                    where f.State && idsCandidatos.Contains(f.GthCandidatoId)
                    join fe in ctx.GthPostulanteFormularioEstado on f.GthPostulanteFormularioEstadoId equals fe.GthPostulanteFormularioEstadoId
                    select new { f.GthCandidatoId, fe.Codigo }).ToListAsync())
                .ToDictionary(x => x.GthCandidatoId, x => x.Codigo);

            var estadosForm = idsCandidatos.Select(id => estadoFormPorCandidato.GetValueOrDefault(id)).ToList();
            if (estadosForm.Any(e => e != EstadoFormularioPostulante.Aprobado
                                     && e != EstadoFormularioPostulante.Rechazado))
                throw new AbrilException("Todos los formularios del postulante deben estar aprobados o rechazados antes de continuar.", 400);
            if (!estadosForm.Any(e => e == EstadoFormularioPostulante.Aprobado))
                throw new AbrilException("Se necesita al menos un formulario del postulante aprobado para programar entrevistas.", 400);

            // El Multitest solo se exige a quien sigue en carrera: al que se le rechazó el
            // formulario ya no se le va a entrevistar, así que pedir su check dejaba trabado el
            // paso a entrevistas por una prueba que ese postulante nunca va a rendir.
            if (candidatos.Any(c => !c.MultitestRealizado
                                    && estadoFormPorCandidato.GetValueOrDefault(c.GthCandidatoId)
                                       != EstadoFormularioPostulante.Rechazado))
                throw new AbrilException(
                    "Marca el Multitest de todos los candidatos que siguen en el proceso antes de continuar.", 400);

            req.GthEstadoRequerimientoId = entrevistas.GthEstadoRequerimientoId;
            req.UpdatedDateTime          = DateTimeOffset.UtcNow;
            req.UpdatedUserId            = userId;
            await ctx.SaveChangesAsync();

            return new EstadoRequerimientoResultDto
            {
                EstadoCodigo = entrevistas.Codigo,
                EstadoNombre = entrevistas.Nombre,
            };
        }

        public async Task<EntrevistaEnvioContextoDto> GuardarEntrevista(
            int candidatoId, DateOnly fecha, TimeOnly hora, int lugarId, int? userId,
            string nuevoToken)
        {
            using var ctx = _factory.CreateDbContext();

            // Candidato + puesto/código del requerimiento.
            var cand = await (
                from c in ctx.GthCandidato
                where c.GthCandidatoId == candidatoId && c.State
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                select new { c.Nombre, Puesto = p.Nombre, r.Codigo }).FirstOrDefaultAsync();
            if (cand == null)
                throw new AbrilException("Candidato no encontrado.", 404);

            // Formulario del postulante (debe estar aprobado) y correo al que se cita.
            var form = await (
                from f in ctx.GthPostulanteFormulario
                where f.GthCandidatoId == candidatoId && f.State
                join fe in ctx.GthPostulanteFormularioEstado on f.GthPostulanteFormularioEstadoId equals fe.GthPostulanteFormularioEstadoId
                select new { EstadoCodigo = fe.Codigo, f.CorreoElectronico, f.CorreoEnvio }).FirstOrDefaultAsync();

            if (form?.EstadoCodigo != EstadoFormularioPostulante.Aprobado)
                throw new AbrilException("Solo se programa entrevista a los candidatos con el formulario del postulante aprobado.", 400);

            var correo = string.IsNullOrWhiteSpace(form.CorreoElectronico) ? form.CorreoEnvio : form.CorreoElectronico;
            if (string.IsNullOrWhiteSpace(correo))
                throw new AbrilException("El candidato no tiene un correo registrado al cual enviar la invitación.", 400);

            var lugar = await ctx.GthLugarEntrevista
                .FirstOrDefaultAsync(l => l.GthLugarEntrevistaId == lugarId && l.State && l.Active);
            if (lugar == null)
                throw new AbrilException("El lugar de entrevista seleccionado no es válido.", 400);

            var now = DateTimeOffset.UtcNow;

            // Una sola entrevista vigente por candidato: reprogramar actualiza esta misma fila.
            var entrevista = await ctx.GthEntrevista
                .FirstOrDefaultAsync(e => e.GthCandidatoId == candidatoId && e.State);
            if (entrevista == null)
            {
                entrevista = new GthEntrevista
                {
                    GthCandidatoId       = candidatoId,
                    Fecha                = fecha,
                    Hora                 = hora,
                    GthLugarEntrevistaId = lugarId,
                    CorreoEnvio          = correo,
                    Token                = nuevoToken,
                    EnviadoDateTime      = now,
                    EnviadoUserId        = userId,
                    CreatedDateTime      = now,
                    CreatedUserId        = userId,
                    Active               = true,
                    State                = true,
                };
                ctx.GthEntrevista.Add(entrevista);
            }
            else
            {
                entrevista.Fecha                = fecha;
                entrevista.Hora                 = hora;
                entrevista.GthLugarEntrevistaId = lugarId;
                entrevista.CorreoEnvio          = correo;
                entrevista.EnviadoDateTime      = now;
                entrevista.EnviadoUserId        = userId;
                entrevista.UpdatedDateTime      = now;
                entrevista.UpdatedUserId        = userId;

                // Cada envío lleva su propio token y borra la respuesta anterior: lo que el
                // candidato confirmó fue la cita vieja, así que una reprogramación le vuelve a
                // preguntar y los botones del correo anterior dejan de responder.
                entrevista.Token                    = nuevoToken;
                entrevista.GthEntrevistaRespuestaId = null;
                entrevista.RespuestaDateTime        = null;
            }

            await ctx.SaveChangesAsync();

            return new EntrevistaEnvioContextoDto
            {
                CandidatoNombre = cand.Nombre,
                Puesto          = cand.Puesto,
                Codigo          = cand.Codigo,
                LugarMapsUrl    = lugar.MapsUrl,
                Token           = nuevoToken,
                Resumen = new EntrevistaResumenDto
                {
                    Fecha       = fecha,
                    Hora        = hora.ToString("HH\\:mm"),
                    LugarId     = lugarId,
                    LugarNombre = lugar.Nombre,
                    CorreoEnvio = correo,
                    EnviadoEn   = now.ToOffset(PeruOffset).DateTime,
                },
            };
        }

        // ── Respuesta del candidato a su citación (pública, por token) ────────
        public async Task<EntrevistaRespuestaContextoDto> RegistrarRespuestaEntrevista(
            string token, string respuestaCodigo)
        {
            using var ctx = _factory.CreateDbContext();

            var entrevista = await ctx.GthEntrevista.FirstOrDefaultAsync(e => e.Token == token && e.State);
            if (entrevista == null)
                throw new AbrilException(
                    "El enlace de la entrevista no es válido o ya no está disponible. Si tu entrevista fue "
                    + "reprogramada, responde desde el último correo que recibiste.", 404);

            var respuesta = await ctx.GthEntrevistaRespuesta
                .FirstOrDefaultAsync(r => r.Codigo == respuestaCodigo && r.State)
                ?? throw new AbrilException(
                    $"No está configurada la respuesta {respuestaCodigo} de la entrevista.", 500);

            // Contexto del proceso: es lo que lleva el aviso a GTH (código, puesto, área) y lo que
            // la página pública le muestra al candidato para que sepa sobre qué cita respondió.
            // El lugar entra por SelectMany contra su id (y no por join contra `entrevista`, que es
            // una entidad ya materializada y no parte de la consulta) con DefaultIfEmpty: si el
            // lugar quedó de baja, la respuesta del candidato se registra igual y solo se queda
            // sin el nombre del sitio.
            var lugarId = entrevista.GthLugarEntrevistaId;
            var contexto = await (
                from c in ctx.GthCandidato
                where c.GthCandidatoId == entrevista.GthCandidatoId && c.State
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                from l in ctx.GthLugarEntrevista
                    .Where(x => x.GthLugarEntrevistaId == lugarId).DefaultIfEmpty()
                // Solicitante: es el destinatario del aviso de "entrevista confirmada" — el que
                // tiene que ir a la cita. Left join por lo mismo que en el resto del módulo: un
                // requerimiento viejo puede haber quedado sin usuario solicitante.
                join u in ctx.User on r.Solicitud!.SolicitanteUserId equals (int?)u.UserId into uJoin
                from u in uJoin.DefaultIfEmpty()
                join ps in ctx.Person on r.Solicitud!.SolicitanteUserId equals ps.UserId into psJoin
                from ps in psJoin.DefaultIfEmpty()
                select new
                {
                    c.Nombre,
                    Puesto = p.Nombre,
                    r.Codigo,
                    r.GthRequerimientoId,
                    Area = r.Solicitud!.AreaNombre,
                    LugarNombre = l != null ? l.Nombre : null,
                    LugarMapsUrl = l != null ? l.MapsUrl : null,
                    SolicitanteEmail  = u != null ? u.Email : null,
                    SolicitanteNombre = ps != null ? ps.FullName : null,
                }).FirstOrDefaultAsync();
            if (contexto == null)
                throw new AbrilException("No se encontró el proceso al que corresponde esta entrevista.", 404);

            // Abrir dos veces el mismo enlace no es una respuesta nueva: se conserva la fecha
            // original y el llamador se ahorra mandarle a GTH un aviso que no cuenta nada nuevo.
            var repetida = entrevista.GthEntrevistaRespuestaId == respuesta.GthEntrevistaRespuestaId;
            if (!repetida)
            {
                var now = DateTimeOffset.UtcNow;
                entrevista.GthEntrevistaRespuestaId = respuesta.GthEntrevistaRespuestaId;
                entrevista.RespuestaDateTime        = now;
                entrevista.UpdatedDateTime          = now;
                await ctx.SaveChangesAsync();
            }

            return new EntrevistaRespuestaContextoDto
            {
                CandidatoNombre = contexto.Nombre,
                Puesto          = contexto.Puesto,
                Codigo          = contexto.Codigo,
                Area            = contexto.Area,
                CorreoCandidato = entrevista.CorreoEnvio,
                RequerimientoId = contexto.GthRequerimientoId,
                SolicitanteEmail  = contexto.SolicitanteEmail,
                SolicitanteNombre = contexto.SolicitanteNombre,
                LugarMapsUrl      = contexto.LugarMapsUrl,
                YaHabiaRespondidoLoMismo = repetida,
                Resumen = new EntrevistaResumenDto
                {
                    Fecha           = entrevista.Fecha,
                    Hora            = entrevista.Hora.ToString("HH\\:mm"),
                    LugarId         = entrevista.GthLugarEntrevistaId,
                    LugarNombre     = contexto.LugarNombre ?? string.Empty,
                    CorreoEnvio     = entrevista.CorreoEnvio,
                    EnviadoEn       = entrevista.EnviadoDateTime?.ToOffset(PeruOffset).DateTime,
                    RespuestaCodigo = respuesta.Codigo,
                    RespuestaNombre = respuesta.Nombre,
                    RespondidoEn    = entrevista.RespuestaDateTime?.ToOffset(PeruOffset).DateTime,
                },
            };
        }

        // ── Evaluación de la entrevista (puntajes, informe y no continuidad) ──
        /// <summary>Texto opcional normalizado: sin espacios sobrantes y null si queda vacío.</summary>
        private static string? Limpiar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        /// <summary>
        /// Cabecera del candidato citado a entrevista. Trae también el requerimiento al que
        /// pertenece (entidad rastreada por EF) y su fase actual, porque enviar al finalista mueve
        /// esa fase: así el avance del pipeline no cuesta un roundtrip extra.
        /// </summary>
        private sealed record CandidatoEntrevistado(
            string Nombre,
            string Puesto,
            string Codigo,
            string Correo,
            GthRequerimiento Requerimiento,
            string EstadoCodigo,
            string? Area,
            string? ProyectoObra,
            string? SolicitanteEmail);

        /// <summary>
        /// Cabecera del candidato para evaluarlo: nombre, puesto/código y fase del requerimiento, y
        /// correo de la entrevista programada. Lanza 404 si no existe y 400 si aún no se le envió la
        /// invitación (solo se evalúa a quien ya fue citado).
        /// </summary>
        private static async Task<CandidatoEntrevistado> GetCandidatoEntrevistado(
            AppDbContext ctx, int candidatoId)
        {
            var cand = await (
                from c in ctx.GthCandidato
                where c.GthCandidatoId == candidatoId && c.State
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId into prJoin
                from pr in prJoin.DefaultIfEmpty()
                join u in ctx.User on r.Solicitud!.SolicitanteUserId equals (int?)u.UserId into uJoin
                from u in uJoin.DefaultIfEmpty()
                select new
                {
                    c.Nombre,
                    Puesto           = p.Nombre,
                    r.Codigo,
                    Req              = r,
                    EstadoCodigo     = e.Codigo,
                    Area             = r.Solicitud!.AreaNombre,
                    ProyectoObra     = pr != null ? pr.ProjectDescription : null,
                    SolicitanteEmail = u != null ? u.Email : null,
                })
                .FirstOrDefaultAsync();
            if (cand == null)
                throw new AbrilException("Candidato no encontrado.", 404);

            var correo = await ctx.GthEntrevista
                .Where(e => e.GthCandidatoId == candidatoId && e.State)
                .Select(e => e.CorreoEnvio)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(correo))
                throw new AbrilException("Primero envía la invitación a la entrevista de este candidato.", 400);

            return new CandidatoEntrevistado(
                cand.Nombre, cand.Puesto, cand.Codigo, correo, cand.Req, cand.EstadoCodigo,
                cand.Area, cand.ProyectoObra, cand.SolicitanteEmail);
        }

        /// <summary>
        /// Evaluación vigente del candidato, creándola en PENDIENTE si aún no existe. No guarda:
        /// el llamador ajusta los campos y hace el <c>SaveChangesAsync</c>.
        /// </summary>
        private static async Task<GthCandidatoEvaluacion> GetOrCreateEvaluacion(
            AppDbContext ctx, int candidatoId, int? userId, DateTimeOffset now)
        {
            var evaluacion = await ctx.GthCandidatoEvaluacion
                .FirstOrDefaultAsync(e => e.GthCandidatoId == candidatoId && e.State);
            if (evaluacion != null)
            {
                evaluacion.UpdatedDateTime = now;
                evaluacion.UpdatedUserId   = userId;
                return evaluacion;
            }

            var pendienteId = await ctx.GthCandidatoResultado
                .Where(r => r.Codigo == ResultadoCandidato.Pendiente && r.State)
                .Select(r => (int?)r.GthCandidatoResultadoId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("No está configurado el resultado PENDIENTE de la entrevista.", 500);

            evaluacion = new GthCandidatoEvaluacion
            {
                GthCandidatoId          = candidatoId,
                GthCandidatoResultadoId = pendienteId,
                CreatedDateTime         = now,
                CreatedUserId           = userId,
                Active                  = true,
                State                   = true,
            };
            ctx.GthCandidatoEvaluacion.Add(evaluacion);
            return evaluacion;
        }

        /// <summary>Resumen de la evaluación ya persistida (relee el catálogo para el nombre del resultado).</summary>
        private static async Task<EvaluacionResumenDto> BuildEvaluacionResumen(AppDbContext ctx, GthCandidatoEvaluacion e)
        {
            var resultado = await ctx.GthCandidatoResultado
                .Where(r => r.GthCandidatoResultadoId == e.GthCandidatoResultadoId)
                .Select(r => new { r.Codigo, r.Nombre })
                .FirstAsync();

            return new EvaluacionResumenDto
            {
                ComentarioEntrevista    = e.ComentarioEntrevista,
                ComentarioPsicotecnico  = e.ComentarioPsicotecnico,
                ComentarioRecomendacion = e.ComentarioRecomendacion,
                ResultadoCodigo         = resultado.Codigo,
                ResultadoNombre         = resultado.Nombre,
                AgradecimientoCorreo    = e.AgradecimientoCorreo,
                AgradecimientoEnviadoEn = e.AgradecimientoDateTime?.ToOffset(PeruOffset).DateTime,
                Archivos                = (await QueryEvaluacionArchivos(
                                              ctx, new List<int> { e.GthCandidatoEvaluacionId }))
                                          .GetValueOrDefault(e.GthCandidatoEvaluacionId)
                                          ?? new List<EvaluacionArchivoDto>(),
            };
        }

        public async Task<EvaluacionGuardadaDto> GuardarEvaluacion(int candidatoId, EvaluacionGuardarDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var cand = await GetCandidatoEntrevistado(ctx, candidatoId);

            var now = DateTimeOffset.UtcNow;
            var evaluacion = await GetOrCreateEvaluacion(ctx, candidatoId, userId, now);

            // Un candidato descartado por GTH o ya decidido por el solicitante tiene su resultado
            // cerrado: el informe no se sigue editando.
            var resultadoActual = await ctx.GthCandidatoResultado
                .Where(r => r.GthCandidatoResultadoId == evaluacion.GthCandidatoResultadoId)
                .Select(r => r.Codigo)
                .FirstOrDefaultAsync();
            if (resultadoActual == ResultadoCandidato.NoPaso)
                throw new AbrilException("El candidato ya no continúa en el proceso: su evaluación quedó cerrada.", 400);
            if (resultadoActual == ResultadoCandidato.NoAptoEmo)
                throw new AbrilException("Este candidato no pasó el EMO de ingreso: su informe quedó cerrado.", 400);
            if (resultadoActual is ResultadoCandidato.Seleccionado or ResultadoCandidato.Rechazado)
                throw new AbrilException("El área solicitante ya tomó una decisión sobre este finalista: su informe quedó cerrado.", 400);

            // Guardar el informe es, a la vez, enviarlo como finalista: el candidato pasa a PASO y
            // desde ahí lo ve el área solicitante.
            if (resultadoActual != ResultadoCandidato.Paso)
            {
                evaluacion.GthCandidatoResultadoId = await ctx.GthCandidatoResultado
                    .Where(r => r.Codigo == ResultadoCandidato.Paso && r.State)
                    .Select(r => (int?)r.GthCandidatoResultadoId)
                    .FirstOrDefaultAsync()
                    ?? throw new AbrilException("No está configurado el resultado PASO de la entrevista.", 500);
            }

            evaluacion.ComentarioEntrevista    = Limpiar(dto.ComentarioEntrevista);
            evaluacion.ComentarioPsicotecnico  = Limpiar(dto.ComentarioPsicotecnico);
            evaluacion.ComentarioRecomendacion = Limpiar(dto.ComentarioRecomendacion);

            // Archivos que GTH quitó de la pantalla: baja lógica (nada se borra). Los que no vengan
            // acá se quedan como estaban, así que reguardar el informe no pierde lo ya subido.
            var quitados = (dto.ArchivosQuitados ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToUpperInvariant())
                .ToList();
            if (quitados.Count > 0 && evaluacion.GthCandidatoEvaluacionId != 0)
            {
                var aQuitar = await (
                    from a in ctx.GthCandidatoEvaluacionArchivo
                    where a.State && a.GthCandidatoEvaluacionId == evaluacion.GthCandidatoEvaluacionId
                    join t in ctx.GthEvaluacionArchivoTipo
                        on a.GthEvaluacionArchivoTipoId equals t.GthEvaluacionArchivoTipoId
                    where quitados.Contains(t.Codigo.ToUpper())
                    select a).ToListAsync();

                foreach (var archivo in aQuitar)
                {
                    archivo.State           = false;
                    archivo.UpdatedDateTime = now;
                    archivo.UpdatedUserId   = userId;
                }
            }

            // Enviar al finalista también mueve el requerimiento a SELECCION_JEFATURA: GTH terminó
            // su parte y la decisión pasa al área solicitante. Solo avanza desde ENTREVISTAS —
            // editar el informe de otro finalista cuando el proceso ya cerró (alguien más obtuvo el
            // puesto) o volvió a LONG_LIST no debe retroceder la fase.
            GthEstadoRequerimiento? avanzoA = null;
            if (cand.EstadoCodigo == EstadoReclutamiento.Entrevistas)
            {
                avanzoA = await ctx.GthEstadoRequerimiento
                    .FirstOrDefaultAsync(e => e.Codigo == EstadoReclutamiento.SeleccionJefatura && e.State)
                    ?? throw new AbrilException("No está configurado el estado SELECCION_JEFATURA de reclutamiento.", 500);

                cand.Requerimiento.GthEstadoRequerimientoId = avanzoA.GthEstadoRequerimientoId;
                cand.Requerimiento.UpdatedDateTime          = now;
                cand.Requerimiento.UpdatedUserId            = userId;
            }

            await ctx.SaveChangesAsync();

            var resumen = await BuildEvaluacionResumen(ctx, evaluacion);

            return new EvaluacionGuardadaDto
            {
                Evaluacion   = resumen,
                EvaluacionId = evaluacion.GthCandidatoEvaluacionId,
                Codigo       = cand.Codigo,
                EstadoCodigo = avanzoA?.Codigo,
                EstadoNombre = avanzoA?.Nombre,
                Envio = new FinalistaEnvioContextoDto
                {
                    RequerimientoId   = cand.Requerimiento.GthRequerimientoId,
                    Codigo            = cand.Codigo,
                    Puesto            = cand.Puesto,
                    Area              = cand.Area,
                    ProyectoObra      = cand.ProyectoObra,
                    SolicitanteEmail  = cand.SolicitanteEmail,
                    CandidatoNombre   = cand.Nombre,
                    Evaluacion        = resumen,
                },
            };
        }

        public async Task<List<EvaluacionArchivoDto>> GuardarEvaluacionArchivos(
            int evaluacionId, List<EvaluacionArchivoPersistDto> archivos, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            if (archivos.Count == 0)
                return (await QueryEvaluacionArchivos(ctx, new List<int> { evaluacionId }))
                       .GetValueOrDefault(evaluacionId) ?? new List<EvaluacionArchivoDto>();

            var codigos = archivos.Select(a => a.TipoCodigo.ToUpperInvariant()).ToList();

            var tipos = await ctx.GthEvaluacionArchivoTipo
                .Where(t => t.State && codigos.Contains(t.Codigo.ToUpper()))
                .ToDictionaryAsync(t => t.Codigo.ToUpperInvariant(), t => t.GthEvaluacionArchivoTipoId);

            var tipoIds = tipos.Values.ToList();

            // Un archivo vivo por evaluación y tipo: el anterior se da de baja (nunca se borra) y
            // el nuevo entra en su lugar.
            var previos = await ctx.GthCandidatoEvaluacionArchivo
                .Where(a => a.State && a.GthCandidatoEvaluacionId == evaluacionId
                            && tipoIds.Contains(a.GthEvaluacionArchivoTipoId))
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;
            foreach (var previo in previos)
            {
                previo.State           = false;
                previo.UpdatedDateTime = now;
                previo.UpdatedUserId   = userId;
            }

            foreach (var archivo in archivos)
            {
                if (!tipos.TryGetValue(archivo.TipoCodigo.ToUpperInvariant(), out var tipoId))
                    throw new AbrilException($"El tipo de archivo «{archivo.TipoCodigo}» no está configurado.", 500);

                ctx.GthCandidatoEvaluacionArchivo.Add(new GthCandidatoEvaluacionArchivo
                {
                    GthCandidatoEvaluacionId   = evaluacionId,
                    GthEvaluacionArchivoTipoId = tipoId,
                    Nombre                     = archivo.Nombre,
                    NombreOriginal             = archivo.NombreOriginal,
                    Url                        = archivo.Url,
                    ItemId                     = archivo.ItemId,
                    DriveId                    = archivo.DriveId,
                    CreatedDateTime            = now,
                    CreatedUserId              = userId,
                    Active                     = true,
                    State                      = true,
                });
            }

            await ctx.SaveChangesAsync();

            return (await QueryEvaluacionArchivos(ctx, new List<int> { evaluacionId }))
                   .GetValueOrDefault(evaluacionId) ?? new List<EvaluacionArchivoDto>();
        }

        public async Task<AgradecimientoEnvioContextoDto> RegistrarAgradecimiento(int candidatoId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var cand = await GetCandidatoEntrevistado(ctx, candidatoId);

            var noPasoId = await ctx.GthCandidatoResultado
                .Where(r => r.Codigo == ResultadoCandidato.NoPaso && r.State)
                .Select(r => (int?)r.GthCandidatoResultadoId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("No está configurado el resultado NO_PASO de la entrevista.", 500);

            var now = DateTimeOffset.UtcNow;
            var evaluacion = await GetOrCreateEvaluacion(ctx, candidatoId, userId, now);

            evaluacion.GthCandidatoResultadoId = noPasoId;
            evaluacion.AgradecimientoCorreo    = cand.Correo;
            evaluacion.AgradecimientoDateTime  = now;
            evaluacion.AgradecimientoUserId    = userId;

            await ctx.SaveChangesAsync();

            return new AgradecimientoEnvioContextoDto
            {
                CandidatoNombre = cand.Nombre,
                Puesto          = cand.Puesto,
                Codigo          = cand.Codigo,
                Correo          = cand.Correo,
                Resumen         = await BuildEvaluacionResumen(ctx, evaluacion),
            };
        }

        public async Task<AgradecimientoEnvioContextoDto> RegistrarRechazoPostulante(int candidatoId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            // A diferencia de RegistrarAgradecimiento no se pasa por GetCandidatoEntrevistado: este
            // candidato nunca llegó a la entrevista, así que no tiene fila en gth_entrevista y el
            // correo con el que se le escribió es el del formulario del postulante.
            var cand = await (
                from c in ctx.GthCandidato
                where c.GthCandidatoId == candidatoId && c.State
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                select new { c.Nombre, Puesto = p.Nombre, r.Codigo })
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("Candidato no encontrado.", 404);

            var formulario = await (
                from f in ctx.GthPostulanteFormulario
                where f.GthCandidatoId == candidatoId && f.State
                join fe in ctx.GthPostulanteFormularioEstado
                    on f.GthPostulanteFormularioEstadoId equals fe.GthPostulanteFormularioEstadoId
                select new { f.CorreoEnvio, EstadoCodigo = fe.Codigo })
                .FirstOrDefaultAsync();

            // Solo se saca del proceso a quien ya tiene el formulario rechazado: mientras esté
            // pendiente de revisión o corrección el postulante sigue en carrera, y el correo de
            // fin de proceso contradiría al de correcciones que se le acaba de mandar.
            if (formulario == null || formulario.EstadoCodigo != EstadoFormularioPostulante.Rechazado)
                throw new AbrilException(
                    "Solo se puede rechazar al postulante cuando su formulario quedó rechazado.", 400);

            var noPasoId = await ctx.GthCandidatoResultado
                .Where(r => r.Codigo == ResultadoCandidato.NoPaso && r.State)
                .Select(r => (int?)r.GthCandidatoResultadoId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("No está configurado el resultado NO_PASO de la entrevista.", 500);

            var now = DateTimeOffset.UtcNow;
            var evaluacion = await GetOrCreateEvaluacion(ctx, candidatoId, userId, now);

            evaluacion.GthCandidatoResultadoId = noPasoId;
            evaluacion.AgradecimientoCorreo    = formulario.CorreoEnvio;
            evaluacion.AgradecimientoDateTime  = now;
            evaluacion.AgradecimientoUserId    = userId;

            await ctx.SaveChangesAsync();

            return new AgradecimientoEnvioContextoDto
            {
                CandidatoNombre = cand.Nombre,
                Puesto          = cand.Puesto,
                Codigo          = cand.Codigo,
                Correo          = formulario.CorreoEnvio,
                Resumen         = await BuildEvaluacionResumen(ctx, evaluacion),
            };
        }

        public async Task<RevisionFinalistasDto?> GetRevisionFinalistas(
            int requerimientoId, SolicitudPersonalScope scope)
        {
            using var ctx = _factory.CreateDbContext();

            // Cabecera del requerimiento (scope: el área del usuario, ver EnScope).
            var head = await (
                from r in EnScope(ctx.GthRequerimiento, scope)
                where r.GthRequerimientoId == requerimientoId
                      && r.State && r.Solicitud!.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                select new
                {
                    r.GthRequerimientoId,
                    r.Codigo,
                    r.PuestoId,
                    Puesto       = p.Nombre,
                    Area         = r.Solicitud!.AreaNombre,
                    ProyectoObra = pr.ProjectDescription,
                    EstadoCodigo = e.Codigo,
                    EstadoNombre = e.Nombre,
                }).FirstOrDefaultAsync();

            if (head == null) return null;

            // Área de destino del puesto: informativa, para que el solicitante vea a qué área
            // entra el candidato que apruebe. No se elige — la decide el puesto.
            var areaDestino = await QueryAreaDestinoDelPuesto(ctx, head.PuestoId);

            // Finalistas: candidatos aprobados en la long list con evaluación registrada por GTH.
            // Los que no continúan (resultado NO_PASO, con su correo de agradecimiento ya enviado)
            // no se muestran al solicitante. NO_APTO_EMO tampoco: si el proceso volvió a esta
            // pantalla es porque GTH retomó a otro candidato, y ofrecer de nuevo al que no pasó el
            // examen médico sería ofrecer algo que el backend va a rechazar.
            var candidatos = await (
                from c in CandidatosVigentes(ctx)
                where c.GthRequerimientoId == requerimientoId
                join est in ctx.GthCandidatoEstado on c.GthCandidatoEstadoId equals est.GthCandidatoEstadoId
                where est.Codigo == EstadoCandidato.Aprobado
                join ev in ctx.GthCandidatoEvaluacion on c.GthCandidatoId equals ev.GthCandidatoId
                where ev.State
                join res in ctx.GthCandidatoResultado on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                where res.Codigo != ResultadoCandidato.NoPaso && res.Codigo != ResultadoCandidato.NoAptoEmo
                // El formulario del postulante es opcional (puede no habérselo enviado nunca), así
                // que va en left join: con un join normal desaparecerían finalistas de la lista.
                join fm in ctx.GthPostulanteFormulario.Where(f => f.State)
                    on c.GthCandidatoId equals fm.GthCandidatoId into fmJoin
                from fm in fmJoin.DefaultIfEmpty()
                select new
                {
                    c.GthCandidatoId,
                    c.Nombre,
                    c.Puesto,
                    c.CvNombre,
                    c.CvUrl,
                    CvPostulanteNombre = fm != null ? (fm.CvNombreOriginal ?? fm.CvNombre) : null,
                    CvPostulanteUrl    = fm != null ? fm.CvUrl : null,
                    Evaluacion = new EvaluacionRawRow
                    {
                        GthCandidatoId           = c.GthCandidatoId,
                        GthCandidatoEvaluacionId = ev.GthCandidatoEvaluacionId,
                        ComentarioEntrevista     = ev.ComentarioEntrevista,
                        ComentarioPsicotecnico   = ev.ComentarioPsicotecnico,
                        ComentarioRecomendacion  = ev.ComentarioRecomendacion,
                        ResultadoCodigo          = res.Codigo,
                        ResultadoNombre          = res.Nombre,
                        AgradecimientoCorreo     = ev.AgradecimientoCorreo,
                        AgradecimientoDateTime   = ev.AgradecimientoDateTime,
                    },
                }).ToListAsync();

            // Archivos del informe (informe final y evaluación de conocimientos) de todos los
            // finalistas de una sola vez: es lo que el solicitante abre para decidir.
            var archivos = await QueryEvaluacionArchivos(
                ctx, candidatos.Select(x => x.Evaluacion.GthCandidatoEvaluacionId).ToList());

            foreach (var x in candidatos)
                x.Evaluacion.Archivos =
                    archivos.GetValueOrDefault(x.Evaluacion.GthCandidatoEvaluacionId)
                    ?? new List<EvaluacionArchivoDto>();

            return new RevisionFinalistasDto
            {
                RequerimientoId = head.GthRequerimientoId,
                Codigo          = head.Codigo,
                Puesto          = head.Puesto,
                Area            = head.Area,
                ProyectoObra    = head.ProyectoObra,
                EstadoCodigo    = head.EstadoCodigo,
                EstadoNombre    = head.EstadoNombre,
                AreaDestino     = areaDestino,
                // Sin puntajes que los ordenen, los finalistas van alfabéticamente.
                Finalistas      = candidatos
                    .OrderBy(x => x.Nombre)
                    .Select(x => new FinalistaDto
                    {
                        CandidatoId        = x.GthCandidatoId,
                        Nombre             = x.Nombre,
                        Puesto             = x.Puesto,
                        CvNombre           = x.CvNombre,
                        CvUrl              = x.CvUrl,
                        CvPostulanteNombre = x.CvPostulanteUrl == null ? null : x.CvPostulanteNombre,
                        CvPostulanteUrl    = x.CvPostulanteUrl,
                        Evaluacion         = MapEvaluacion(x.Evaluacion),
                    }).ToList(),
            };
        }

        /// <summary>
        /// Ficha de <c>workers</c> del finalista aprobado. Es lo que permite reusar todo el modulo
        /// de EMOs tal cual: <c>worker_emos</c> y <c>ss_programacion_emos</c> cuelgan de
        /// <c>workers.id</c>, asi que sin ficha no hay a quien programarle el examen de ingreso.
        ///
        /// La ficha nace SIN fila en <c>worker_vinculaciones</c>, y eso es deliberado: la
        /// vinculacion es el contrato. Mientras no exista, el finalista queda fuera de la lista de
        /// trabajadores de Habilitacion, de la auto-programacion de EMOs, de los dashboards y de
        /// todo lo que exige vinculacion vigente con empresa Abril — sin tocar ninguna de esas
        /// consultas. Lo unico que lo hace visible es el opt-in explicito de la pantalla de EMOs.
        ///
        /// La persona ya existe en <c>person</c> antes de llegar aca: aprobar el formulario del
        /// postulante escribe la data maestra (ver PostulanteFormularioRepository.RegistrarDecision)
        /// y eso ocurre antes de entrevistas, o sea antes de esta decision. Si no la encuentra
        /// devuelve null en vez de reventar: la decision del solicitante se registra igual y GTH
        /// vera el aviso de que falta el formulario.
        ///
        /// <paramref name="areaScopeId"/> es el area a la que entra el seleccionado: la de DESTINO
        /// del PUESTO que se pidio (<c>puesto.area_destino_scope_id</c>), no la del solicitante ni
        /// la que puede pedir el puesto. La Gerencia Inmobiliaria pide un INGENIERO RESIDENTE y el
        /// residente entra a Residencia. Cuando el puesto no tiene destino se cae al area del
        /// solicitante.
        /// Se graba en la ficha desde ya (y no recien en el onboarding) porque es lo que permite
        /// resolver la jefatura del seleccionado — subiendo por el arbol de area_scope — al
        /// programarle su EMO de ingreso, que ocurre antes de que exista contrato.
        ///
        /// La entidad se agrega al ChangeTracker pero NO se guarda: la persiste el SaveChanges de
        /// quien llama, junto con el cambio de estado del requerimiento.
        /// </summary>
        private static async Task<Worker?> ResolverFichaFinalistaAsync(
            AppDbContext ctx, int candidatoId, GthRequerimiento req, int? areaScopeId)
        {
            var personId = await ctx.GthPostulanteFormulario
                .Where(f => f.GthCandidatoId == candidatoId && f.State && f.PersonId != null)
                .Select(f => f.PersonId)
                .FirstOrDefaultAsync();
            if (personId == null) return null;

            // Una persona puede tener varias fichas (reingresos). Si ya tiene una viva se reusa en
            // vez de abrir otra: un trabajador de Abril que postula internamente sigue siendo el
            // mismo worker, y un finalista que ya paso por aca no necesita ficha nueva.
            // Se prioriza la de pre-ingreso, y si no la hay, la mas reciente.
            var existente = await ctx.Worker
                .Where(w => w.PersonId == personId
                         && (w.WorkersEstadoId == WorkersEstadoIds.Activo
                          || w.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma
                          || w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado))
                .OrderByDescending(w => w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado ? 1 : 0)
                .ThenByDescending(w => w.Id)
                .FirstOrDefaultAsync();
            if (existente != null)
            {
                // El área de una ficha de pre-ingreso sale siempre del requerimiento que la aprobó
                // (es lo unico que la puso ahi, y esta decision es la ultima). La de un trabajador
                // real, en cambio, es su area de verdad: solo se llena si estaba vacia, para no
                // moverlo de sitio en el arbol antes de que el contrato exista.
                var puedeReasignarArea = existente.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado
                                      || existente.AreaScopeId == null;
                if (areaScopeId != null && puedeReasignarArea && existente.AreaScopeId != areaScopeId)
                {
                    existente.AreaScopeId = areaScopeId;
                    existente.UpdatedAt   = DateTimeOffset.UtcNow;
                }
                return existente;
            }

            var ahora = DateTimeOffset.UtcNow;
            var ficha = new Worker
            {
                PersonId        = personId,
                WorkersEstadoId = WorkersEstadoIds.FinalistaAprobado,
                // Lo que ya se sabe del puesto, del area y de la clasificacion sale del
                // requerimiento; el resto (correo corporativo, fecha de ingreso) lo completa
                // Onboarding cuando firme.
                // Solo el puesto: la categoría de la ficha sale de puesto.categoria_id.
                PuestoId        = req.PuestoId,
                ContributorId   = req.ContributorId,
                // Clasificacion desde ya, no en el onboarding: es lo que hace que los correos de
                // EMO encuentren su columna en la matriz de Configuracion de EMOs cuando GTH le
                // programa el examen de ingreso. Ver ClasificacionPreIngreso.
                ContrataCasa       = ClasificacionPreIngreso.ContrataCasaPropia,
                ObraOficinaStaffId = await ClasificacionPreIngreso
                    .ResolverObraOficinaStaffIdAsync(ctx, req.ProjectId),
                // Area del puesto que se pidio: es a donde entra el seleccionado, y es lo que deja
                // resolver su jefatura (subiendo por el arbol de area_scope) sin tener que esperar
                // al onboarding — la programacion de su EMO de ingreso la necesita ya.
                AreaScopeId     = areaScopeId,
                // Sin periodo laboral: todavia no ingreso. Se le abre uno cuando firme
                // (ver WorkersPeriodoLaboral).
                CreatedAt       = ahora,
                UpdatedAt       = ahora,
            };
            ctx.Worker.Add(ficha);
            return ficha;
        }

        public async Task<FinalistaDecisionContextoDto> RegistrarDecisionFinalista(
            int requerimientoId, int candidatoId, bool aprobado, SolicitudPersonalScope scope)
        {
            using var ctx = _factory.CreateDbContext();

            // Quien decide es cualquier jefatura del área, no forzosamente quien registró la
            // solicitud (ver EnScope); en la trazabilidad queda quien tomó ESTA decisión.
            var userId = scope.UserId;

            // Cabecera + estado actual, con scope al área del usuario. Se trae la entidad del
            // requerimiento (r) para mutarla; EF la rastrea aunque venga en un anónimo.
            var head = await (
                from r in EnScope(ctx.GthRequerimiento, scope)
                where r.GthRequerimientoId == requerimientoId
                      && r.State && r.Solicitud!.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                select new
                {
                    Req          = r,
                    r.Codigo,
                    r.PuestoId,
                    Puesto       = p.Nombre,
                    Area         = r.Solicitud!.AreaNombre,
                    // Área del solicitante: solo se usa como respaldo cuando el puesto no tiene
                    // ninguna área mapeada (ver ResolverAreaDestinoAsync).
                    AreaScopeId  = r.Solicitud!.AreaScopeId,
                    ProyectoObra = pr.ProjectDescription,
                    EstadoCodigo = e.Codigo,
                }).FirstOrDefaultAsync();

            if (head == null)
                throw new AbrilException("No se encontró el informe de finalistas del requerimiento.", 404);

            // La decisión final solo se toma mientras el requerimiento está en SELECCION_JEFATURA,
            // la fase en la que lo deja GTH al enviar a los finalistas (después ya quedó cerrado o
            // volvió a long list).
            if (head.EstadoCodigo != EstadoReclutamiento.SeleccionJefatura)
                throw new AbrilException("Este proceso ya no está en la fase de decisión de finalistas.", 409);

            // Finalistas del requerimiento: candidatos con evaluación vigente que GTH no descartó.
            var finalistas = await (
                from c in CandidatosVigentes(ctx)
                where c.GthRequerimientoId == requerimientoId
                join ev in ctx.GthCandidatoEvaluacion on c.GthCandidatoId equals ev.GthCandidatoId
                where ev.State
                join res in ctx.GthCandidatoResultado on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                where res.Codigo != ResultadoCandidato.NoPaso && res.Codigo != ResultadoCandidato.NoAptoEmo
                select new { c.GthCandidatoId, c.Nombre, Evaluacion = ev, ResultadoCodigo = res.Codigo })
                .ToListAsync();

            var elegido = finalistas.FirstOrDefault(f => f.GthCandidatoId == candidatoId)
                ?? throw new AbrilException("El candidato no forma parte de los finalistas de este requerimiento.", 404);
            if (ResultadoCandidato.Cerrados.Contains(elegido.ResultadoCodigo))
                throw new AbrilException("Ya registraste una decisión sobre este finalista.", 409);

            // Área a la que entra el seleccionado: la de destino de su puesto, con el área del
            // solicitante como respaldo. Se resuelve antes de tocar nada.
            var areaDestino = aprobado
                ? await ResolverAreaDestinoAsync(ctx, head.PuestoId, head.AreaScopeId)
                : null;

            var codigoResultado = aprobado ? ResultadoCandidato.Seleccionado : ResultadoCandidato.Rechazado;
            var resultadoDestino = await ctx.GthCandidatoResultado
                .Where(r => r.Codigo == codigoResultado && r.State)
                .Select(r => (int?)r.GthCandidatoResultadoId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException($"No está configurado el resultado {codigoResultado} de candidatos.", 500);

            var now = DateTimeOffset.UtcNow;
            elegido.Evaluacion.GthCandidatoResultadoId = resultadoDestino;
            elegido.Evaluacion.DecisionDateTime        = now;
            elegido.Evaluacion.DecisionUserId          = userId;
            elegido.Evaluacion.UpdatedDateTime         = now;
            elegido.Evaluacion.UpdatedUserId           = userId;

            // Correos de los finalistas involucrados en la decisión: el rechazado (para su correo
            // de fin de proceso) y, al aprobar, los que quedaron sin elegir. Se resuelven en una
            // sola consulta a gth_entrevista, que es de donde sale el correo con el que se les
            // escribió durante todo el proceso.
            //
            // Al aprobar a un finalista el puesto queda cubierto: los demás que seguían en carrera
            // ya no tienen a qué esperar, así que se cierran acá mismo como RECHAZADO por el
            // solicitante y reciben el mismo correo de fin de proceso que el rechazado
            // explícitamente. Antes se quedaban en PASO para siempre, sin decisión y sin aviso.
            // El Where arranca por `aprobado` (y no un if/else con dos ramas) para que el tipo
            // anónimo de `finalistas` se siga infiriendo sin tener que nombrarlo.
            var noElegidos = finalistas
                .Where(f => aprobado
                            && f.GthCandidatoId != candidatoId
                            && !ResultadoCandidato.Cerrados.Contains(f.ResultadoCodigo))
                .ToList();

            var idsConCorreo = noElegidos.Select(f => f.GthCandidatoId).Append(candidatoId).ToList();
            var correoPorCandidato = (await ctx.GthEntrevista
                    .Where(e => e.State && idsConCorreo.Contains(e.GthCandidatoId))
                    .Select(e => new { e.GthCandidatoId, e.CorreoEnvio })
                    .ToListAsync())
                .GroupBy(e => e.GthCandidatoId)
                .ToDictionary(g => g.Key, g => g.First().CorreoEnvio);

            // Deja registrado el envío del correo de fin de proceso en la evaluación y devuelve
            // el correo al que va (vacío si el candidato no tiene uno cargado).
            string MarcarFinDeProceso(GthCandidatoEvaluacion evaluacion, int candId)
            {
                var correo = correoPorCandidato.GetValueOrDefault(candId) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(correo)) return string.Empty;

                evaluacion.AgradecimientoCorreo   = correo;
                evaluacion.AgradecimientoDateTime = now;
                evaluacion.AgradecimientoUserId   = userId;
                return correo;
            }

            // Al rechazar también se le manda el correo de fin de proceso (el mismo que envía GTH),
            // así que se registra su envío igual que en RegistrarAgradecimiento.
            var correoCandidato = aprobado ? string.Empty : MarcarFinDeProceso(elegido.Evaluacion, candidatoId);

            var noElegidosCtx = new List<FinalistaNoElegidoDto>(noElegidos.Count);
            if (noElegidos.Count > 0)
            {
                var rechazadoId = await ctx.GthCandidatoResultado
                    .Where(r => r.Codigo == ResultadoCandidato.Rechazado && r.State)
                    .Select(r => (int?)r.GthCandidatoResultadoId)
                    .FirstOrDefaultAsync()
                    ?? throw new AbrilException("No está configurado el resultado RECHAZADO de candidatos.", 500);

                foreach (var f in noElegidos)
                {
                    f.Evaluacion.GthCandidatoResultadoId = rechazadoId;
                    f.Evaluacion.DecisionDateTime        = now;
                    f.Evaluacion.DecisionUserId          = userId;
                    f.Evaluacion.UpdatedDateTime         = now;
                    f.Evaluacion.UpdatedUserId           = userId;

                    noElegidosCtx.Add(new FinalistaNoElegidoDto
                    {
                        CandidatoId = f.GthCandidatoId,
                        Nombre      = f.Nombre,
                        Correo      = MarcarFinDeProceso(f.Evaluacion, f.GthCandidatoId),
                    });
                }
            }

            // Aprobar ya NO cierra el proceso: lo deja en EMO_INGRESO, la fase en la que GTH le
            // programa el examen medico de ingreso al seleccionado. Cierra recien cuando el EMO sale
            // APTO, y si sale NO APTO vuelve para atras (ver ReclutamientoEmoIngresoService).
            // Rechazar al último finalista en carrera devuelve el requerimiento a LONG_LIST para que
            // GTH prepare y envíe una nueva long list; los rechazados quedan grabados como historial.
            // Si aún quedan finalistas por decidir, se queda en SELECCION_JEFATURA.
            var quedanEnCarrera = finalistas.Any(f =>
                f.GthCandidatoId != candidatoId
                && !ResultadoCandidato.Cerrados.Contains(f.ResultadoCodigo));
            var todosRechazados = !aprobado && !quedanEnCarrera;

            var codigoEstado = aprobado ? EstadoReclutamiento.EmoIngreso
                             : todosRechazados ? EstadoReclutamiento.LongList
                             : EstadoReclutamiento.SeleccionJefatura;
            var estadoDestino = await ctx.GthEstadoRequerimiento
                .FirstOrDefaultAsync(e => e.Codigo == codigoEstado && e.State)
                ?? throw new AbrilException($"No está configurado el estado {codigoEstado} de reclutamiento.", 500);

            head.Req.GthEstadoRequerimientoId = estadoDestino.GthEstadoRequerimientoId;
            head.Req.UpdatedDateTime          = now;
            head.Req.UpdatedUserId            = userId;

            // Nombre del solicitante para el cuerpo del correo a GTH (best-effort; no bloquea).
            var solicitanteNombre = await ctx.Worker
                .Where(w => w.Person != null && w.Person.UserId == userId)
                .Select(w => w.Person!.FullName ?? w.ApellidoNombre)
                .FirstOrDefaultAsync();

            // Ficha de pre-ingreso del seleccionado: se agrega al mismo SaveChanges que la
            // decision, para que no exista una sin la otra. Su area sale del puesto pedido, no del
            // solicitante (ver ResolverAreaDestinoAsync).
            var fichaFinalista = aprobado
                ? await ResolverFichaFinalistaAsync(ctx, candidatoId, head.Req, areaDestino)
                : null;

            await ctx.SaveChangesAsync();

            return new FinalistaDecisionContextoDto
            {
                Resultado = new FinalistaDecisionResultDto
                {
                    EstadoCodigo    = estadoDestino.Codigo,
                    EstadoNombre    = estadoDestino.Nombre,
                    Aprobado        = aprobado,
                    TodosRechazados = todosRechazados,
                    CandidatoNombre = elegido.Nombre,
                    WorkerId        = fichaFinalista?.Id,
                },
                Codigo            = head.Codigo,
                Puesto            = head.Puesto,
                Area              = head.Area,
                ProyectoObra      = head.ProyectoObra,
                SolicitanteNombre = solicitanteNombre,
                CandidatoCorreo   = correoCandidato,
                NoElegidos        = noElegidosCtx,
            };
        }

        /// <summary>
        /// Área a la que entra el seleccionado, y que queda en <c>workers.area_scope_id</c> de su
        /// ficha de pre-ingreso. Sale del PUESTO que se pidió
        /// (<c>puesto.area_destino_scope_id</c>) y no del solicitante ni de una elección en
        /// pantalla: la Gerencia Inmobiliaria pide un INGENIERO RESIDENTE, y el residente entra a
        /// Residencia.
        ///
        ///   • El puesto tiene destino → ese, sin preguntar.
        ///   • El puesto no tiene ninguno → el área del solicitante, que es lo que se usaba antes
        ///     de esta regla. Pasa con los puestos de obra, que el padrón de GTH nunca mapeó.
        /// </summary>
        private static async Task<int?> ResolverAreaDestinoAsync(
            AppDbContext ctx, int? puestoId, int? areaSolicitante)
        {
            var destino = await QueryAreaDestinoDelPuesto(ctx, puestoId);
            return destino?.Id ?? areaSolicitante;
        }

        // ── Reapertura del proceso tras un EMO de ingreso No Apto ────────────────────────
        //
        // Las dos salidas de la fase EMO_NO_APTO. La eligen desde el detalle de GTH y son
        // excluyentes: o se continúa con alguien que ya se había descartado, o se descarta a todos
        // y se arma una long list nueva.

        /// <summary>
        /// Etapa en la que quedó rechazado un candidato. Es la MISMA derivación que usa el
        /// historial (<see cref="QueryCandidatosRechazados"/>): tiene que serlo, porque el botón
        /// «Retomar» se enciende con lo que dice el historial y acá se decide qué significa
        /// retomarlo. Si las dos reglas se separaran, la pantalla ofrecería una etapa y el backend
        /// desharía otra.
        ///
        /// Null cuando el candidato no está rechazado (sigue en carrera o se dio de baja sin
        /// decisión): no hay nada que retomar con él.
        /// </summary>
        private static string? DerivarEtapaRechazo(
            string? estadoCandidatoCodigo, string? resultadoEvaluacionCodigo, bool tuvoEntrevista) =>
            resultadoEvaluacionCodigo switch
            {
                ResultadoCandidato.NoAptoEmo    => EtapaRechazo.Emo,
                ResultadoCandidato.Rechazado    => EtapaRechazo.DecisionFinal,
                ResultadoCandidato.NoPaso       => tuvoEntrevista ? EtapaRechazo.Entrevistas
                                                                  : EtapaRechazo.Formulario,
                _ => estadoCandidatoCodigo == EstadoCandidato.Rechazado ? EtapaRechazo.LongList : null,
            };

        /// <summary>Nombre visible de la etapa del rechazo (el mismo que muestra el historial).</summary>
        private static string NombreEtapaRechazo(string etapaCodigo) => etapaCodigo switch
        {
            EtapaRechazo.LongList      => "Long list",
            EtapaRechazo.Formulario    => "Formulario",
            EtapaRechazo.Entrevistas   => "Entrevistas",
            EtapaRechazo.DecisionFinal => "Decisión final",
            EtapaRechazo.Emo           => "EMO",
            _                          => etapaCodigo,
        };

        public async Task<RetomarCandidatoContextoDto> RetomarCandidatoRechazado(
            int requerimientoId, int candidatoId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State)
                ?? throw new AbrilException("Requerimiento no encontrado.", 404);

            // Los tres catálogos que hacen falta, de una vez: son tablas de una decena de filas y
            // resolverlas por código una por una serían cinco o seis roundtrips por retomada.
            var estadosReq = await ctx.GthEstadoRequerimiento
                .Where(e => e.State)
                .Select(e => new { e.GthEstadoRequerimientoId, e.Codigo, e.Nombre })
                .ToListAsync();
            var estadosCandidato = await ctx.GthCandidatoEstado
                .Where(e => e.State)
                .Select(e => new { e.GthCandidatoEstadoId, e.Codigo })
                .ToListAsync();
            var resultados = await ctx.GthCandidatoResultado
                .Where(r => r.State)
                .Select(r => new { r.GthCandidatoResultadoId, r.Codigo })
                .ToListAsync();

            // Retomar es una salida de EMO_NO_APTO y de ninguna otra fase: en cualquier otra el
            // proceso está en curso y devolverlo a una fase anterior sería pisar el trabajo que se
            // esté haciendo (una long list a medio revisar, un finalista esperando decisión).
            var estadoActual = estadosReq
                .FirstOrDefault(e => e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId)?.Codigo;
            if (estadoActual != EstadoReclutamiento.EmoNoApto)
                throw new AbrilException(
                    "Solo se puede retomar a un candidato rechazado cuando el EMO de ingreso del "
                    + "seleccionado salió No Apto.", 409);

            var cand = await ctx.GthCandidato
                .FirstOrDefaultAsync(c => c.GthCandidatoId == candidatoId
                                       && c.GthRequerimientoId == requerimientoId && c.State)
                ?? throw new AbrilException("El candidato no pertenece a este requerimiento.", 404);

            var evaluacion = await ctx.GthCandidatoEvaluacion
                .FirstOrDefaultAsync(e => e.GthCandidatoId == candidatoId && e.State);

            // Con entrevista o sin ella se separan las dos salidas que decide GTH (ver EtapaRechazo).
            var tuvoEntrevista = await ctx.GthEntrevista
                .AnyAsync(e => e.State && e.GthCandidatoId == candidatoId);

            var estadoCandidatoCodigo = estadosCandidato
                .FirstOrDefault(e => e.GthCandidatoEstadoId == cand.GthCandidatoEstadoId)?.Codigo;
            var resultadoCodigo = evaluacion == null ? null : resultados
                .FirstOrDefault(r => r.GthCandidatoResultadoId == evaluacion.GthCandidatoResultadoId)?.Codigo;

            var etapa = DerivarEtapaRechazo(estadoCandidatoCodigo, resultadoCodigo, tuvoEntrevista)
                ?? throw new AbrilException(
                    "Este candidato no está rechazado: no hay proceso que retomar con él.", 400);

            if (!EtapaRechazo.Retomables.Contains(etapa))
                throw new AbrilException(
                    "Este candidato no pasó el EMO de ingreso: no se puede retomar el proceso con él.", 400);

            int ResultadoId(string codigo) => resultados
                .FirstOrDefault(r => r.Codigo == codigo)?.GthCandidatoResultadoId
                ?? throw new AbrilException($"No está configurado el resultado {codigo} de candidatos.", 500);

            var aprobadoId = estadosCandidato
                .FirstOrDefault(e => e.Codigo == EstadoCandidato.Aprobado)?.GthCandidatoEstadoId
                ?? throw new AbrilException("No está configurado el estado APROBADO de candidatos.", 500);

            var now = DateTimeOffset.UtcNow;

            // El candidato vuelve a estar APROBADO en la long list en todas las etapas, no solo
            // cuando lo rechazaron ahí: es el estado que lo hace visible en las pantallas de GTH, y
            // en las otras tres etapas ya lo estaba (su rechazo vive en el resultado de la
            // evaluación), así que asignarlo no cambia nada y cubre el caso que sí lo necesita.
            cand.GthCandidatoEstadoId = aprobadoId;
            cand.DecisionDateTime     = now;
            cand.DecisionUserId       = userId;
            cand.UpdatedDateTime      = now;
            cand.UpdatedUserId        = userId;

            // Un rechazado de una vuelta anterior sigue con su número de long list, y la long list
            // vigente es la del número mayor: sin subirlo, retomarlo lo dejaría fuera de todas las
            // pantallas (ver CandidatosVigentes) y el proceso quedaría trabado sin candidato.
            var vueltaVigente = await ctx.GthCandidato
                .Where(c => c.GthRequerimientoId == requerimientoId && c.State)
                .MaxAsync(c => (int?)c.NumeroLongList) ?? cand.NumeroLongList;
            if (cand.NumeroLongList < vueltaVigente)
                cand.NumeroLongList = vueltaVigente;

            // Fase a la que vuelve el requerimiento: la que deja el trabajo pendiente justo donde
            // estaba cuando se lo descartó. No se retrocede más de eso — lo ya hecho con este
            // candidato (su formulario, su entrevista, su informe) sigue guardado y sirve igual.
            string faseDestino;
            string siguientePaso;
            switch (etapa)
            {
                case EtapaRechazo.LongList:
                    // Lo rechazó el solicitante al revisar los CVs y nunca tuvo evaluación: basta
                    // con devolverlo a APROBADO (ya está hecho arriba) para que GTH le mande el
                    // formulario del postulante, que es el paso siguiente de esa fase.
                    faseDestino   = EstadoReclutamiento.LongListAprobada;
                    siguientePaso = "Envíale el formulario de información del postulante.";
                    break;

                case EtapaRechazo.Formulario:
                    // GTH le rechazó el formulario. La fila del formulario se deja como está: el
                    // reenvío ya sabe reabrir un formulario RECHAZADO (lo vuelve a ENVIADO y limpia
                    // la fecha de completado) para que el postulante lo corrija.
                    ReabrirEvaluacion(evaluacion!, ResultadoId(ResultadoCandidato.Pendiente), userId, now);
                    faseDestino   = EstadoReclutamiento.LongListAprobada;
                    siguientePaso = "Reenvíale el formulario del postulante para que lo corrija.";
                    break;

                case EtapaRechazo.Entrevistas:
                    // GTH lo descartó tras la entrevista: su cita y su informe siguen guardados, así
                    // que vuelve a PENDIENTE para que GTH lo reevalúe o lo cite de nuevo.
                    ReabrirEvaluacion(evaluacion!, ResultadoId(ResultadoCandidato.Pendiente), userId, now);
                    faseDestino   = EstadoReclutamiento.Entrevistas;
                    siguientePaso = "Revisa su evaluación y envíalo como finalista al área solicitante.";
                    break;

                default: // EtapaRechazo.DecisionFinal
                    // Lo rechazó el solicitante en la decisión final: vuelve a PASO, que es el
                    // resultado con el que se lo había enviado, y la pelota vuelve a su lado.
                    ReabrirEvaluacion(evaluacion!, ResultadoId(ResultadoCandidato.Paso), userId, now);
                    faseDestino   = EstadoReclutamiento.SeleccionJefatura;
                    siguientePaso = "El área solicitante tiene que volver a decidir sobre este finalista.";
                    break;
            }

            var destino = estadosReq.FirstOrDefault(e => e.Codigo == faseDestino)
                ?? throw new AbrilException($"No está configurado el estado {faseDestino} de reclutamiento.", 500);

            req.GthEstadoRequerimientoId = destino.GthEstadoRequerimientoId;
            req.UpdatedDateTime          = now;
            req.UpdatedUserId            = userId;

            await ctx.SaveChangesAsync();

            // Contexto del aviso al solicitante, en un solo roundtrip. Los left join son por lo
            // mismo que en el resto del módulo: el proyecto es opcional y el solicitante puede
            // haberse quedado sin usuario.
            var cabecera = await (
                from r in ctx.GthRequerimiento.AsNoTracking()
                where r.GthRequerimientoId == requerimientoId
                join p in ctx.Puesto.AsNoTracking() on r.PuestoId equals p.PuestoId
                join pr in ctx.Project.AsNoTracking() on r.ProjectId equals pr.ProjectId into prJoin
                from pr in prJoin.DefaultIfEmpty()
                join u in ctx.User.AsNoTracking()
                    on r.Solicitud!.SolicitanteUserId equals (int?)u.UserId into uJoin
                from u in uJoin.DefaultIfEmpty()
                join ps in ctx.Person.AsNoTracking()
                    on r.Solicitud!.SolicitanteUserId equals ps.UserId into psJoin
                from ps in psJoin.DefaultIfEmpty()
                select new
                {
                    r.Codigo,
                    Puesto            = p.Nombre,
                    Area              = r.Solicitud!.AreaNombre,
                    ProyectoObra      = pr != null ? pr.ProjectDescription : null,
                    SolicitanteEmail  = u != null ? u.Email : null,
                    SolicitanteNombre = ps != null ? ps.FullName : null,
                }).FirstOrDefaultAsync();

            return new RetomarCandidatoContextoDto
            {
                Resultado = new RetomarCandidatoResultDto
                {
                    EstadoCodigo    = destino.Codigo,
                    EstadoNombre    = destino.Nombre,
                    CandidatoNombre = cand.Nombre,
                    EtapaCodigo     = etapa,
                    EtapaNombre     = NombreEtapaRechazo(etapa),
                    SiguientePaso   = siguientePaso,
                },
                RequerimientoId   = requerimientoId,
                Codigo            = cabecera?.Codigo ?? req.Codigo,
                Puesto            = cabecera?.Puesto ?? string.Empty,
                Area              = cabecera?.Area,
                ProyectoObra      = cabecera?.ProyectoObra,
                SolicitanteEmail  = cabecera?.SolicitanteEmail,
                SolicitanteNombre = cabecera?.SolicitanteNombre,
            };
        }

        /// <summary>
        /// Devuelve la evaluación de un candidato retomado al resultado con el que estaba antes del
        /// rechazo y borra el rastro del cierre: la fecha de la decisión y el correo de fin de
        /// proceso. Ese correo se le envió de verdad, pero dejarlo puesto haría que la pantalla lo
        /// siga mostrando como "ya se le agradeció" mientras vuelve a estar en carrera, y bloquearía
        /// el envío del correo de cierre que sí le corresponda al final.
        /// </summary>
        private static void ReabrirEvaluacion(
            GthCandidatoEvaluacion evaluacion, int resultadoId, int? userId, DateTimeOffset now)
        {
            evaluacion.GthCandidatoResultadoId = resultadoId;
            evaluacion.AgradecimientoCorreo    = null;
            evaluacion.AgradecimientoDateTime  = null;
            evaluacion.AgradecimientoUserId    = null;
            evaluacion.DecisionDateTime        = null;
            evaluacion.DecisionUserId          = null;
            evaluacion.UpdatedDateTime         = now;
            evaluacion.UpdatedUserId           = userId;
        }

        public async Task<EstadoRequerimientoResultDto> VolverALongListDesdeEmoNoApto(
            int requerimientoId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State)
                ?? throw new AbrilException("Requerimiento no encontrado.", 404);

            var estados = await ctx.GthEstadoRequerimiento
                .Where(e => e.State && (e.Codigo == EstadoReclutamiento.EmoNoApto
                                        || e.Codigo == EstadoReclutamiento.LongList
                                        || e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId))
                .Select(e => new { e.GthEstadoRequerimientoId, e.Codigo, e.Nombre })
                .ToListAsync();

            var actual = estados
                .FirstOrDefault(e => e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId)?.Codigo;
            if (actual != EstadoReclutamiento.EmoNoApto)
                throw new AbrilException(
                    "Solo se puede preparar una nueva long list cuando el EMO de ingreso del "
                    + "seleccionado salió No Apto.", 409);

            var longList = estados.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.LongList)
                ?? throw new AbrilException("No está configurado el estado LONG_LIST de reclutamiento.", 500);

            // Nada más que cambiar la fase: los rechazados se quedan como están y son el historial
            // que GTH mira para no volver a presentar a los mismos. La próxima carga de CVs entra
            // como una vuelta nueva (la anterior ya está decidida) y son esos los que pasan a ser la
            // long list vigente.
            req.GthEstadoRequerimientoId = longList.GthEstadoRequerimientoId;
            req.UpdatedDateTime          = DateTimeOffset.UtcNow;
            req.UpdatedUserId            = userId;

            await ctx.SaveChangesAsync();

            return new EstadoRequerimientoResultDto
            {
                EstadoCodigo = longList.Codigo,
                EstadoNombre = longList.Nombre,
            };
        }

        public async Task<EstadoRequerimientoResultDto> CerrarProcesoDesdeEmoApto(
            int requerimientoId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State)
                ?? throw new AbrilException("Requerimiento no encontrado.", 404);

            var estados = await ctx.GthEstadoRequerimiento
                .Where(e => e.State && (e.Codigo == EstadoReclutamiento.Cerrado
                                        || e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId))
                .Select(e => new { e.GthEstadoRequerimientoId, e.Codigo, e.Nombre })
                .ToListAsync();

            var actual = estados
                .FirstOrDefault(e => e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId)?.Codigo;

            // El cierre es la salida de las dos fases de "el examen salió bien" y de ninguna otra.
            // Desde EMO_OBSERVADO no se cierra (todavía no hay aptitud), desde EMO_NO_APTO tampoco
            // (el candidato quedó fuera) y desde una fase anterior sería saltarse el examen.
            if (!EstadoReclutamiento.EmoAptas.Contains(actual ?? string.Empty))
                throw new AbrilException(
                    "Solo se puede cerrar el proceso cuando el EMO de ingreso del seleccionado salió "
                    + "Apto o Apto con Restricciones.", 409);

            var cerrado = estados.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.Cerrado)
                ?? throw new AbrilException("No está configurado el estado CERRADO de reclutamiento.", 500);

            // Nada más que la fase: el candidato ya está SELECCIONADO desde la decisión del área
            // solicitante y su ficha de pre-ingreso ya existe. Cerrar es exactamente lo que hace que
            // aparezca en Onboarding como candidato por ingresar (ver OnboardingRepository, que
            // lista los SELECCIONADOS de requerimientos CERRADOS).
            req.GthEstadoRequerimientoId = cerrado.GthEstadoRequerimientoId;
            req.UpdatedDateTime          = DateTimeOffset.UtcNow;
            req.UpdatedUserId            = userId;

            await ctx.SaveChangesAsync();

            return new EstadoRequerimientoResultDto
            {
                EstadoCodigo = cerrado.Codigo,
                EstadoNombre = cerrado.Nombre,
            };
        }

        public async Task UpdateAsignacionGth(int requerimientoId, AsignacionGthUpdateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State);
            if (req == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);

            // Validar cada id no nulo contra su catálogo vigente.
            if (dto.ResponsableId.HasValue)
            {
                var ok = await ctx.GthResponsableProceso
                    .AnyAsync(r => r.GthResponsableProcesoId == dto.ResponsableId.Value && r.State && r.Active);
                if (!ok) throw new AbrilException("El responsable seleccionado no es válido.", 400);
            }
            if (dto.TipoProcesoId.HasValue)
            {
                var ok = await ctx.GthTipoProceso
                    .AnyAsync(t => t.GthTipoProcesoId == dto.TipoProcesoId.Value && t.State && t.Active);
                if (!ok) throw new AbrilException("El tipo de proceso seleccionado no es válido.", 400);
            }
            if (dto.PrioridadId.HasValue)
            {
                var ok = await ctx.GthPrioridad
                    .AnyAsync(p => p.GthPrioridadId == dto.PrioridadId.Value && p.State && p.Active);
                if (!ok) throw new AbrilException("La prioridad seleccionada no es válida.", 400);
            }
            if (dto.ContributorId.HasValue)
            {
                var ok = await ctx.Contributor
                    .AnyAsync(c => c.ContributorId == dto.ContributorId.Value && c.State && c.Active && c.Operativo);
                if (!ok) throw new AbrilException("La razón social seleccionada no es válida.", 400);
            }

            var razonSocialCambio = req.ContributorId != dto.ContributorId;

            req.GthResponsableProcesoId = dto.ResponsableId;
            req.GthTipoProcesoId        = dto.TipoProcesoId;
            req.GthPrioridadId          = dto.PrioridadId;
            req.ContributorId           = dto.ContributorId;
            req.UpdatedDateTime         = DateTimeOffset.UtcNow;
            req.UpdatedUserId           = userId;

            // En un ingreso directo la ficha de pre-ingreso ya existe desde que se aprobó la
            // vacante (no espera ningún formulario), así que la razón social que se asigna acá
            // llega tarde para el momento en que se creó: hay que bajársela. En el flujo normal no
            // aplica — ahí la ficha nace después, con la razón social ya puesta.
            if (req.EsFft && razonSocialCambio && req.FftPersonId.HasValue)
            {
                var ficha = await ctx.Worker
                    .Where(w => w.PersonId == req.FftPersonId.Value
                             && w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado)
                    .OrderByDescending(w => w.Id)
                    .FirstOrDefaultAsync();
                if (ficha != null)
                {
                    ficha.ContributorId = dto.ContributorId;
                    ficha.UpdatedAt     = DateTimeOffset.UtcNow;
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<EstadoRequerimientoResultDto> ReplacePublicaciones(int requerimientoId, List<int> canalIds, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State);
            if (req == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);

            // Publicar cierra la asignación interna: de acá en adelante el requerimiento se
            // trabaja con responsable, SLA, prioridad y razón social ya definidos, así que los
            // cuatro son obligatorios antes de avanzar la fase.
            var faltantes = new List<string>();
            if (req.GthResponsableProcesoId == null) faltantes.Add("el responsable del proceso");
            if (req.GthTipoProcesoId == null)        faltantes.Add("el tipo de proceso");
            if (req.GthPrioridadId == null)          faltantes.Add("la prioridad interna");
            if (req.ContributorId == null)           faltantes.Add("la razón social activa");
            if (faltantes.Count > 0)
                throw new AbrilException($"Antes de publicar debes seleccionar {string.Join(", ", faltantes)}.", 400);

            var deseados = canalIds.Distinct().ToList();
            if (deseados.Count > 0)
            {
                var validos = await ctx.GthCanalPublicacion
                    .CountAsync(c => deseados.Contains(c.GthCanalPublicacionId) && c.State && c.Active);
                if (validos != deseados.Count)
                    throw new AbrilException("Uno o más canales seleccionados no son válidos.", 400);
            }

            var now = DateTimeOffset.UtcNow;

            // Reconciliación contra las publicaciones vigentes (mismo patrón que los
            // destinatarios de correo: alta de nuevos, baja de los quitados).
            var vigentes = await ctx.GthRequerimientoCanal
                .Where(rc => rc.GthRequerimientoId == requerimientoId && rc.State)
                .ToListAsync();

            foreach (var canalId in deseados)
            {
                var v = vigentes.FirstOrDefault(x => x.GthCanalPublicacionId == canalId);
                if (v != null)
                {
                    if (!v.Active)
                    {
                        v.Active          = true;
                        v.UpdatedDateTime = now;
                        v.UpdatedUserId   = userId;
                    }
                }
                else
                {
                    ctx.GthRequerimientoCanal.Add(new GthRequerimientoCanal
                    {
                        GthRequerimientoId   = requerimientoId,
                        GthCanalPublicacionId = canalId,
                        CreatedDateTime      = now,
                        CreatedUserId        = userId,
                        Active               = true,
                        State                = true,
                    });
                }
            }

            foreach (var v in vigentes)
            {
                if (!deseados.Contains(v.GthCanalPublicacionId))
                {
                    v.State           = false;
                    v.UpdatedDateTime = now;
                    v.UpdatedUserId   = userId;
                }
            }

            // Avance del pipeline: registrar la publicación deja el requerimiento en la fase
            // PUBLICACION (no hay integración real con los portales; el registro es manual).
            // Si ya está en esa fase o más adelante, no se retrocede.
            var estados = await ctx.GthEstadoRequerimiento
                .Where(e => e.State && (e.Codigo == EstadoReclutamiento.Publicacion
                                        || e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId))
                .ToListAsync();
            var publicacion = estados.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.Publicacion)
                ?? throw new AbrilException("No está configurado el estado PUBLICACION de reclutamiento.", 500);
            var actual = estados.FirstOrDefault(e => e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId);

            if (actual == null || actual.Orden < publicacion.Orden)
            {
                req.GthEstadoRequerimientoId = publicacion.GthEstadoRequerimientoId;
                req.UpdatedDateTime          = now;
                req.UpdatedUserId            = userId;
                actual = publicacion;
            }

            await ctx.SaveChangesAsync();

            return new EstadoRequerimientoResultDto
            {
                EstadoCodigo = actual.Codigo,
                EstadoNombre = actual.Nombre,
            };
        }

        public async Task<EstadoRequerimientoResultDto> IniciarRevisionCv(int requerimientoId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State);
            if (req == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);

            var estados = await ctx.GthEstadoRequerimiento
                .Where(e => e.State && (e.Codigo == EstadoReclutamiento.LongList
                                        || e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId))
                .ToListAsync();
            var longList = estados.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.LongList)
                ?? throw new AbrilException("No está configurado el estado LONG_LIST de reclutamiento.", 500);
            var actual = estados.FirstOrDefault(e => e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId);

            // Idempotente: si ya está en Long list (o más adelante) no se retrocede ni se duplica.
            if (actual != null && actual.Orden >= longList.Orden)
                return new EstadoRequerimientoResultDto { EstadoCodigo = actual.Codigo, EstadoNombre = actual.Nombre };

            // Solo se inicia la revisión de CV desde la fase de publicación.
            if (actual == null || actual.Codigo != EstadoReclutamiento.Publicacion)
                throw new AbrilException("La vacante aún no está publicada en los canales de publicación.", 400);

            req.GthEstadoRequerimientoId = longList.GthEstadoRequerimientoId;
            req.UpdatedDateTime          = DateTimeOffset.UtcNow;
            req.UpdatedUserId            = userId;
            await ctx.SaveChangesAsync();

            return new EstadoRequerimientoResultDto
            {
                EstadoCodigo = longList.Codigo,
                EstadoNombre = longList.Nombre,
            };
        }

        public async Task<LongListEnvioContextoDto> GetLongListEnvioContexto(int requerimientoId)
        {
            using var ctx = _factory.CreateDbContext();

            // Cabecera + estado actual + SLA del tipo de proceso, en 1 roundtrip.
            var info = await (
                from r in ctx.GthRequerimiento
                where r.GthRequerimientoId == requerimientoId && r.State && r.Solicitud!.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                join tp in ctx.GthTipoProceso on r.GthTipoProcesoId equals tp.GthTipoProcesoId into tpJoin
                from tp in tpJoin.DefaultIfEmpty()
                join u in ctx.User on r.Solicitud!.SolicitanteUserId equals (int?)u.UserId into uJoin
                from u in uJoin.DefaultIfEmpty()
                select new
                {
                    r.Codigo,
                    Puesto           = p.Nombre,
                    Area             = r.Solicitud!.AreaNombre,
                    ProyectoObra     = pr.ProjectDescription,
                    EstadoCodigo     = e.Codigo,
                    EstadoNombre     = e.Nombre,
                    EstadoOrden      = e.Orden,
                    SlaDias          = tp != null ? (int?)tp.SlaDias : null,
                    SolicitanteEmail = u != null ? u.Email : null,
                }).FirstOrDefaultAsync();

            if (info == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);

            // Solo se puede enviar la long list si la revisión de CV ya inició (fase LONG_LIST o
            // posterior). Reenviar cuando ya está en LONG_LIST_ENVIADA está permitido.
            var longListOrden = await ctx.GthEstadoRequerimiento
                .Where(e => e.Codigo == EstadoReclutamiento.LongList && e.State)
                .Select(e => (int?)e.Orden)
                .FirstOrDefaultAsync();
            if (longListOrden == null || info.EstadoOrden < longListOrden)
                throw new AbrilException("La revisión de CV aún no ha iniciado; no hay long list para enviar.", 400);

            return new LongListEnvioContextoDto
            {
                EstadoCodigo          = info.EstadoCodigo,
                EstadoNombre          = info.EstadoNombre,
                Codigo                = info.Codigo,
                Puesto                = info.Puesto,
                Area                  = info.Area,
                ProyectoObra          = info.ProyectoObra,
                SlaDias               = info.SlaDias,
                SolicitanteEmail      = info.SolicitanteEmail,
            };
        }

        public async Task<EstadoRequerimientoResultDto> GuardarLongListCandidatos(
            int requerimientoId, List<LongListCandidatoPersistDto> candidatos, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var req = await ctx.GthRequerimiento
                .FirstOrDefaultAsync(r => r.GthRequerimientoId == requerimientoId && r.State);
            if (req == null)
                throw new AbrilException("Requerimiento no encontrado.", 404);

            var estados = await ctx.GthEstadoRequerimiento
                .Where(e => e.State && (e.Codigo == EstadoReclutamiento.LongList
                                        || e.Codigo == EstadoReclutamiento.LongListEnviada
                                        || e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId))
                .ToListAsync();
            var longList        = estados.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.LongList);
            var longListEnviada = estados.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.LongListEnviada)
                ?? throw new AbrilException("No está configurado el estado LONG_LIST_ENVIADA de reclutamiento.", 500);
            var actual = estados.FirstOrDefault(e => e.GthEstadoRequerimientoId == req.GthEstadoRequerimientoId);

            if (longList == null || actual == null || actual.Orden < longList.Orden)
                throw new AbrilException("La revisión de CV aún no ha iniciado; no hay long list para enviar.", 400);

            // Estado inicial de cada candidato: PENDIENTE (aún sin decisión del solicitante).
            var estadoPendienteId = await ctx.GthCandidatoEstado
                .Where(e => e.Codigo == EstadoCandidato.Pendiente && e.State)
                .Select(e => (int?)e.GthCandidatoEstadoId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("No está configurado el estado PENDIENTE de candidatos.", 500);

            // El puesto del candidato es siempre el del requerimiento (el que registró el
            // solicitante): se guarda como snapshot para que la revisión no dependa del catálogo.
            var puestoRequerimiento = await ctx.Puesto
                .Where(p => p.PuestoId == req.PuestoId)
                .Select(p => p.Nombre)
                .FirstOrDefaultAsync();

            var now = DateTimeOffset.UtcNow;

            // Qué hacer con la long list anterior depende de si el solicitante llegó a decidirla:
            //
            //   • Ya decidida  → esto es una VUELTA NUEVA (rechazó a todos y el requerimiento
            //     volvió a LONG_LIST). La anterior se conserva viva: sus rechazados son el
            //     historial que el sistema muestra, y `state = false` significa "eliminado del
            //     sistema, no mostrar nunca", que no es el caso de un rechazado.
            //   • Sin decidir  → esto es una CORRECCIÓN del mismo envío. Ahí las filas anteriores
            //     sí se eliminan (state = false): se reemplazan, no son historial de nada.
            //
            // No pueden convivir ambas cosas en una misma vuelta: la decisión de la long list se
            // registra para todos los candidatos de una sola vez.
            // Con sus anexos (mismo viaje): si esta carga reemplaza a la anterior, los anexos de
            // los candidatos que se dan de baja se dan de baja con ellos.
            var vigentes = await ctx.GthCandidato
                .Include(c => c.Anexos)
                .Where(c => c.GthRequerimientoId == requerimientoId && c.State)
                .ToListAsync();

            var vueltaAnterior = vigentes.Count == 0 ? 0 : vigentes.Max(c => c.NumeroLongList);
            var ultimaVuelta   = vigentes.Where(c => c.NumeroLongList == vueltaAnterior).ToList();
            var yaDecidida     = ultimaVuelta.Any(c => c.GthCandidatoEstadoId != estadoPendienteId);

            int numeroLongList;
            if (yaDecidida)
            {
                numeroLongList = vueltaAnterior + 1;
            }
            else
            {
                numeroLongList = Math.Max(vueltaAnterior, 1);
                foreach (var v in ultimaVuelta)
                {
                    v.State           = false;
                    v.UpdatedDateTime = now;
                    v.UpdatedUserId   = userId;

                    foreach (var a in v.Anexos.Where(a => a.State))
                    {
                        a.State           = false;
                        a.UpdatedDateTime = now;
                        a.UpdatedUserId   = userId;
                    }
                }
            }

            var orden = 1;
            foreach (var c in candidatos)
            {
                // Los anexos entran por la navegación: EF inserta candidato + anexos en el mismo
                // SaveChanges, así que no puede quedar un candidato guardado sin su portafolio.
                var ordenAnexo = 1;
                var anexos = c.Anexos.Select(a => new GthCandidatoAnexo
                {
                    Nombre          = string.IsNullOrWhiteSpace(a.Nombre) ? (a.NombreOriginal ?? "anexo") : a.Nombre!,
                    NombreOriginal  = a.NombreOriginal,
                    Url             = a.Url,
                    ItemId          = a.ItemId,
                    DriveId         = a.DriveId,
                    Orden           = ordenAnexo++,
                    CreatedDateTime = now,
                    CreatedUserId   = userId,
                    Active          = true,
                    State           = true,
                }).ToList();

                ctx.GthCandidato.Add(new GthCandidato
                {
                    GthRequerimientoId   = requerimientoId,
                    Nombre               = string.IsNullOrWhiteSpace(c.Nombre) ? $"Candidato {orden}" : c.Nombre.Trim(),
                    Puesto               = puestoRequerimiento,
                    Comentario           = string.IsNullOrWhiteSpace(c.Comentario) ? null : c.Comentario.Trim(),
                    CvNombre             = c.CvNombre,
                    CvUrl                = c.CvUrl,
                    CvItemId             = c.CvItemId,
                    CvDriveId            = c.CvDriveId,
                    GthCandidatoEstadoId = estadoPendienteId,
                    Orden                = orden,
                    NumeroLongList       = numeroLongList,
                    Anexos               = anexos,
                    CreatedDateTime      = now,
                    CreatedUserId        = userId,
                    Active               = true,
                    State                = true,
                });
                orden++;
            }

            // Avance del pipeline a LONG_LIST_ENVIADA (idempotente: no se retrocede).
            if (actual.Orden < longListEnviada.Orden)
            {
                req.GthEstadoRequerimientoId = longListEnviada.GthEstadoRequerimientoId;
                req.UpdatedDateTime          = now;
                req.UpdatedUserId            = userId;
                actual = longListEnviada;
            }
            else
            {
                // Reenvío estando ya en LONG_LIST_ENVIADA: refresca la fecha para ordenar las tarjetas.
                req.UpdatedDateTime = now;
                req.UpdatedUserId   = userId;
            }

            await ctx.SaveChangesAsync();

            return new EstadoRequerimientoResultDto
            {
                EstadoCodigo = actual.Codigo,
                EstadoNombre = actual.Nombre,
            };
        }

        public async Task<SeguimientoDto?> GetSeguimiento(
            int requerimientoId, SolicitudPersonalScope scope)
        {
            using var ctx = _factory.CreateDbContext();

            // Cabecera del requerimiento (scope: el área del usuario, ver EnScope).
            var head = await (
                from r in EnScope(ctx.GthRequerimiento, scope)
                where r.GthRequerimientoId == requerimientoId
                      && r.State && r.Solicitud!.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join t in ctx.GthTipoRequerimiento on r.GthTipoRequerimientoId equals t.GthTipoRequerimientoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                select new
                {
                    r.GthRequerimientoId,
                    r.Codigo,
                    Puesto            = p.Nombre,
                    Tipo              = t.Nombre,
                    Area              = r.Solicitud!.AreaNombre,
                    ProyectoObra      = pr.ProjectDescription,
                    r.GthSolicitudId,
                    r.Solicitud.Justificacion,
                    r.SalarioBrutoMensual,
                    r.EsFft,
                    r.FftCandidatoNombre,
                    r.CreatedDateTime,
                    EstadoCodigo      = e.Codigo,
                    EstadoNombre      = e.Nombre,
                    EstadoOrden       = e.Orden,
                    r.Solicitud.SustentoNombre,
                    r.Solicitud.SustentoUrl,
                }).FirstOrDefaultAsync();

            if (head == null) return null;

            // Catálogo de fases del pipeline (las vigentes y activas, en orden). RECHAZADO_GG está
            // sembrado con active = false justamente para que no aparezca acá: es un estado del
            // requerimiento, no un paso del pipeline.
            var fases = await ctx.GthEstadoRequerimiento
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.Orden)
                .Select(e => new FaseSeguimientoDto
                {
                    Codigo      = e.Codigo,
                    Nombre      = e.Nombre,
                    Descripcion = e.Descripcion,
                    Orden       = e.Orden,
                })
                .ToListAsync();

            // El ingreso directo FFT no recorre el pipeline completo: dejar «Publicación», «Long
            // list», «Formulario» o «Entrevistas» como pasos pendientes dejaría al solicitante
            // esperando algo que no va a pasar. La aprobación tampoco la recorre — a un FFT no lo
            // aprueba nadie— y se recorta salvo en los que quedaron enganchados a una de antes de
            // ese cambio, que se reconocen por su fila de detalle. Es por VACANTE y no por
            // solicitud: una solicitud mixta tiene aprobación, pero es de las otras vacantes.
            //
            // La fase en la que el requerimiento ESTÁ nunca se recorta, aunque esté en la lista:
            // los FFT anteriores a que el flujo se saltara el formulario quedaron parados ahí, y
            // quitarles su fase actual daría el proceso por más avanzado de lo que está.
            if (head.EsFft)
            {
                var tuvoAprobacionGg = await (
                    from d in ctx.GthAprobacionGgDetalle
                    where d.GthRequerimientoId == requerimientoId && d.State
                    join a in ctx.GthAprobacionGg on d.GthAprobacionGgId equals a.GthAprobacionGgId
                    where a.State
                    select d.GthAprobacionGgDetalleId).AnyAsync();

                fases.RemoveAll(f => f.Codigo != head.EstadoCodigo
                                  && (FftFlujo.FasesOmitidas.Contains(f.Codigo)
                                   || (!tuvoAprobacionGg && f.Codigo == EstadoReclutamiento.AprobacionGg)));
            }

            // Un requerimiento rechazado por Gerencia General se quedó en esa fase: su orden (13,
            // fuera del pipeline) marcaría todas las fases como cumplidas, que es justo lo contrario.
            var rechazadoGg = head.EstadoCodigo == EstadoReclutamiento.RechazadoGg;
            var ordenEfectivo = rechazadoGg
                ? fases.FirstOrDefault(f => f.Codigo == EstadoReclutamiento.AprobacionGg)?.Orden ?? head.EstadoOrden
                : head.EstadoOrden;

            // Estado visual de cada fase respecto a la fase actual del requerimiento.
            foreach (var f in fases)
                f.Estado = f.Orden < ordenEfectivo ? "done"
                         : f.Orden == ordenEfectivo ? "current"
                         : "pending";

            // Historial de rechazados del requerimiento: el solicitante lo ve acá, que es su
            // pantalla de historial (incluye lo que rechazó él y lo que descartó GTH).
            var candidatosRechazados = await QueryCandidatosRechazados(ctx, requerimientoId);

            // Quién obtuvo el puesto (null mientras el proceso no cierre con un seleccionado).
            var seleccionado = await QuerySeleccionado(ctx, requerimientoId);

            return new SeguimientoDto
            {
                RequerimientoId       = head.GthRequerimientoId,
                Codigo                = head.Codigo,
                Puesto                = head.Puesto,
                TipoRequerimiento     = head.Tipo,
                Area                  = head.Area,
                ProyectoObra          = head.ProyectoObra,
                Justificacion         = head.Justificacion,
                SalarioBrutoMensual   = head.SalarioBrutoMensual,
                EsFft                 = head.EsFft,
                FftCandidatoNombre    = head.FftCandidatoNombre,
                Enviado               = head.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                EstadoCodigo          = head.EstadoCodigo,
                EstadoNombre          = head.EstadoNombre,
                EstadoOrden           = head.EstadoOrden,
                SustentoNombre        = head.SustentoNombre,
                SustentoUrl           = head.SustentoUrl,
                Fases                 = fases,
                CandidatosRechazados  = candidatosRechazados,
                Seleccionado          = seleccionado,
                // Rechazado por el GG no tiene "siguiente paso": el proceso terminó ahí. En un FFT
                // parado en la fase del formulario, la descripción del catálogo habla de la long
                // list —que este flujo no tiene—, así que se dice el paso real.
                // Las cuatro fases de resultado del EMO comparten el orden de EMO_INGRESO, así que
                // sin estos textos el solicitante leería "GTH le programará el examen médico" justo
                // cuando el examen ya se hizo y ya se sabe cómo salió.
                SiguientePaso         = rechazadoGg
                    ? "Gerencia General no aprobó esta vacante. Para volver a pedirla hay que registrar una nueva solicitud."
                    : SiguientePasoTrasEmo(head.EstadoCodigo)
                      ?? (head.EsFft && head.EstadoCodigo == FftFlujo.FaseFormularioLegado
                            ? FftFlujo.SiguientePasoFormulario
                            : fases.FirstOrDefault(f => f.Estado == "current")?.Descripcion),
            };
        }

        /// <summary>
        /// Qué le falta al requerimiento cuando ya está en una de las fases de resultado del EMO de
        /// ingreso. Null si la fase no es una de esas, para que el llamador siga con la descripción
        /// del catálogo.
        ///
        /// Estas cuatro fases comparten el <c>orden</c> de <c>EMO_INGRESO</c> (es el paso vigente de
        /// la línea de tiempo), así que la descripción del catálogo diría que falta programar el
        /// examen cuando en realidad ya se hizo.
        /// </summary>
        private static string? SiguientePasoTrasEmo(string estadoCodigo) => estadoCodigo switch
        {
            EstadoReclutamiento.EmoApto =>
                "El examen médico de ingreso salió Apto. GTH está cerrando el proceso para pasar al "
                + "candidato a onboarding.",
            EstadoReclutamiento.EmoAptoRestricciones =>
                "El examen médico de ingreso salió Apto con Restricciones. GTH está cerrando el "
                + "proceso para pasar al candidato a onboarding.",
            EstadoReclutamiento.EmoObservado =>
                "El examen médico de ingreso quedó Observado: el candidato fue derivado a "
                + "interconsulta y su aptitud se define con ese resultado.",
            EstadoReclutamiento.EmoNoApto =>
                "El examen médico de ingreso del candidato seleccionado salió No Apto. GTH está "
                + "retomando el proceso con otro candidato.",
            EstadoReclutamiento.CerradoSinCubrir =>
                "El examen médico de ingreso salió No Apto y este pedido era un ingreso directo, sin "
                + "otros candidatos ni long list a la que volver: el proceso terminó sin cubrir la "
                + "vacante. Para volver a pedirla hay que registrar una nueva solicitud.",
            _ => null,
        };

        /// <summary>Proyecta requerimientos (+ puesto, proyecto y estado) a filas de la tabla, en 1 roundtrip.</summary>
        private static async Task<List<SolicitudVacanteListItemDto>> ProjectRequerimientos(
            AppDbContext ctx, IQueryable<GthRequerimiento> reqs)
        {
            var raw = await (
                from r in reqs
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                // Quién lo pidió. Desde que la tabla muestra los requerimientos del área y no solo
                // los propios, la fila tiene que decirlo: si no, no hay forma de distinguir el
                // pedido de uno del pedido del vecino. LEFT porque el usuario puede no tener ficha.
                join ps in ctx.Person on r.Solicitud!.SolicitanteUserId equals ps.UserId into solicitanteJoin
                from ps in solicitanteJoin.DefaultIfEmpty()
                orderby r.CreatedDateTime descending, r.GthRequerimientoId descending
                select new
                {
                    r.GthRequerimientoId,
                    r.Codigo,
                    Puesto = p.Nombre,
                    r.Solicitud!.Justificacion,
                    Area = r.Solicitud.AreaNombre,
                    ProyectoObra = pr.ProjectDescription,
                    r.CreatedDateTime,
                    EstadoCodigo = e.Codigo,
                    EstadoNombre = e.Nombre,
                    Solicitante = ps != null ? ps.FullName : null,
                }).ToListAsync();

            // Conversión a hora Perú en memoria (evita traducir ToOffset en el join).
            return raw.Select(x => new SolicitudVacanteListItemDto
            {
                RequerimientoId = x.GthRequerimientoId,
                Codigo          = x.Codigo,
                Puesto          = x.Puesto,
                Justificacion   = x.Justificacion,
                Area            = x.Area,
                ProyectoObra    = x.ProyectoObra,
                Enviado         = x.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                EstadoCodigo    = x.EstadoCodigo,
                EstadoNombre    = x.EstadoNombre,
                Solicitante     = x.Solicitante,
            }).ToList();
        }

        // ── Configuración de destinatarios del correo (por tipo: SOLICITUD / LONG_LIST) ─
        public async Task<CorreoDestinatariosDto> GetCorreoDestinatarios(string tipoCodigo)
        {
            using var ctx = _factory.CreateDbContext();
            var tipoId = await ResolveCorreoTipoId(ctx, tipoCodigo);

            // Solo los correos escritos a mano: los destinatarios dinámicos (codigo no nulo) no
            // tienen correo guardado y se resuelven al enviar, así que no le corresponden a este
            // modal — se administran desde la pantalla de Configuración.
            var rows = await ctx.GthCorreoDestinatario
                .Where(d => d.State && d.Active && d.GthCorreoTipoId == tipoId
                            && d.Codigo == null && d.Email != null)
                .OrderBy(d => d.Email)
                .Select(d => new { Email = d.Email!, d.EsCopia })
                .ToListAsync();

            return new CorreoDestinatariosDto
            {
                Principales = rows.Where(r => !r.EsCopia).Select(r => r.Email).ToList(),
                Copias      = rows.Where(r =>  r.EsCopia).Select(r => r.Email).ToList(),
            };
        }

        public async Task ReplaceCorreoDestinatarios(string tipoCodigo, List<string> principales, List<string> copias, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var tipoId = await ResolveCorreoTipoId(ctx, tipoCodigo);
            var now = DateTimeOffset.UtcNow;

            // Estado deseado: email -> esCopia (principal gana si estuviera en ambas, ya resuelto en el servicio).
            var deseado = new Dictionary<string, bool>();
            foreach (var e in principales) deseado[e] = false;
            foreach (var e in copias)      deseado.TryAdd(e, true);

            // Vigentes (state = true) del tipo — email único entre ellos por el índice parcial
            // (tipo, email). Los dinámicos quedan fuera: no tienen correo y este reemplazo masivo
            // los daría de baja como si el usuario los hubiera quitado.
            var vigentes = await ctx.GthCorreoDestinatario
                .Where(d => d.State && d.GthCorreoTipoId == tipoId
                            && d.Codigo == null && d.Email != null)
                .ToListAsync();
            var vigentesByEmail = vigentes.ToDictionary(v => v.Email!);

            // Alta o actualización en sitio (si cambia el tipo se actualiza es_copia, no se borra+inserta:
            // así nunca hay dos filas vigentes con el mismo correo y no choca con el índice único).
            foreach (var (email, esCopia) in deseado)
            {
                if (vigentesByEmail.TryGetValue(email, out var v))
                {
                    if (v.EsCopia != esCopia || !v.Active)
                    {
                        v.EsCopia = esCopia;
                        v.Active = true;
                        v.UpdatedDateTime = now;
                        v.UpdatedUserId = userId;
                    }
                }
                else
                {
                    ctx.GthCorreoDestinatario.Add(new GthCorreoDestinatario
                    {
                        GthCorreoTipoId = tipoId,
                        Email           = email,
                        EsCopia         = esCopia,
                        Active          = true,
                        State           = true,
                        CreatedDateTime = now,
                        CreatedUserId   = userId,
                    });
                }
            }

            // Baja (soft delete) de los vigentes que ya no están en el conjunto deseado.
            foreach (var v in vigentes)
            {
                if (!deseado.ContainsKey(v.Email!))
                {
                    v.State = false;
                    v.UpdatedDateTime = now;
                    v.UpdatedUserId = userId;
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<GerenteAreaDto?> GetGerenteDeArea(int? areaScopeId)
        {
            if (areaScopeId is not > 0) return null;

            using var ctx = _factory.CreateDbContext();

            // 1) Cadena área del solicitante → raíz. Se sube por el árbol porque el gerente casi
            //    nunca cuelga del área estándar del solicitante ("Tecnología de la Información")
            //    sino del nodo "Área de Gerencia" del que esa depende ("Gerencia de
            //    Administración"). El árbol es una tabla chica: se arma en memoria, igual que en
            //    JefeRevisorResolver.
            var scopes = await ctx.AreaScope.AsNoTracking()
                .Where(s => s.State)
                .Select(s => new { s.AreaScopeId, s.AreaScopeParentId })
                .ToListAsync();
            var parentById = scopes.ToDictionary(s => s.AreaScopeId, s => s.AreaScopeParentId);

            var cadena    = new List<int>();
            var visitados = new HashSet<int>();
            int? actual   = areaScopeId;
            while (actual != null && visitados.Add(actual.Value)) // el HashSet corta ciclos por si el árbol quedó mal
            {
                cadena.Add(actual.Value);
                parentById.TryGetValue(actual.Value, out actual);
            }
            if (cadena.Count == 0) return null;

            // 2) Gerentes vigentes de cualquier nodo de la cadena, con el nombre del área para la
            //    etiqueta del aviso. La categoría se resuelve como subconsulta para no gastar un
            //    roundtrip extra solo en leer su id.
            var candidatos = await (
                from w in ctx.Worker.AsNoTracking()
                where w.AreaScopeId != null && cadena.Contains(w.AreaScopeId.Value)
                      && w.WorkersEstadoId == WorkersEstadoIds.Activo
                      && w.EmailCorporativo != null && w.EmailCorporativo.Contains("@")
                      && w.PuestoCatalogo != null
                      && w.PuestoCatalogo.CategoriaId == CategoriaIds.Gerente
                join s in ctx.AreaScope.AsNoTracking() on w.AreaScopeId equals s.AreaScopeId
                join ai in ctx.AreaItem.AsNoTracking() on s.AreaItemId equals ai.AreaItemId
                select new
                {
                    w.Id,
                    AreaScopeId = w.AreaScopeId!.Value,
                    Email       = w.EmailCorporativo!,
                    Nombre      = w.Person != null ? w.Person.FullName : w.ApellidoNombre,
                    AreaNombre  = ai.AreaItemName,
                }).ToListAsync();

            // Gana el nodo más cercano al solicitante y, dentro de él, el primer trabajador por id.
            var elegido = candidatos
                .OrderBy(c => cadena.IndexOf(c.AreaScopeId))
                .ThenBy(c => c.Id)
                .FirstOrDefault();

            return elegido == null ? null : new GerenteAreaDto
            {
                WorkerId   = elegido.Id,
                Email      = elegido.Email.Trim(),
                Nombre     = elegido.Nombre,
                AreaNombre = elegido.AreaNombre,
            };
        }

        /// <summary>Resuelve el id del tipo de correo por su código estable (SOLICITUD/LONG_LIST); 500 si no está sembrado.</summary>
        private static async Task<int> ResolveCorreoTipoId(AppDbContext ctx, string tipoCodigo)
        {
            var tipoId = await ctx.GthCorreoTipo
                .Where(t => t.Codigo == tipoCodigo && t.State)
                .Select(t => (int?)t.GthCorreoTipoId)
                .FirstOrDefaultAsync();
            if (tipoId == null)
                throw new AbrilException("No está configurado el tipo de correo de reclutamiento solicitado.", 500);
            return tipoId.Value;
        }

        /// <summary>
        /// La ficha del solicitante tal como la muestra el encabezado de "Nueva solicitud de
        /// personal": área, puesto y categoría. Los tres son de solo lectura — se derivan del
        /// usuario y no se preguntan.
        ///
        /// El puesto y la categoría salen del catálogo por <c>workers.puesto_id</c> y no de las
        /// columnas congeladas <c>workers.puesto</c> / <c>workers.categoria</c>, que son texto
        /// plano del padrón viejo que dejó de mantenerse. La categoría además ya no vive en la
        /// ficha: el único camino es <c>puesto.categoria_id</c>.
        ///
        /// Ambos van en el mismo <c>SELECT</c> que el área: no cuestan ningún roundtrip extra.
        /// </summary>
        private static async Task<SolicitanteFicha> ResolveSolicitanteInternal(AppDbContext ctx, int userId)
        {
            var w = await ctx.Worker
                .Where(x => x.Person != null && x.Person.UserId == userId)
                .Select(x => new
                {
                    x.Id,
                    x.AreaScopeId,
                    // Sin puesto no hay categoría: una ficha con puesto_id null deja los dos
                    // campos vacíos, y el formulario lo dice en vez de mostrar un hueco.
                    Puesto = x.PuestoCatalogo != null && x.PuestoCatalogo.State
                        ? x.PuestoCatalogo.Nombre
                        : null,
                    Categoria = x.PuestoCatalogo != null && x.PuestoCatalogo.State
                                && x.PuestoCatalogo.Categoria != null && x.PuestoCatalogo.Categoria.State
                        ? x.PuestoCatalogo.Categoria.Nombre
                        : null,
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();
            if (w == null) return new SolicitanteFicha();

            return new SolicitanteFicha
            {
                AreaNombre      = await ResolveAreaNombreInternal(ctx, w.AreaScopeId),
                AreaScopeId     = w.AreaScopeId,
                WorkerId        = w.Id,
                PuestoNombre    = w.Puesto,
                CategoriaNombre = w.Categoria,
            };
        }

        /// <summary>Ficha del solicitante resuelta desde su usuario. Todo null = no tiene ficha de trabajador.</summary>
        private sealed class SolicitanteFicha
        {
            public string? AreaNombre { get; init; }
            public int? AreaScopeId { get; init; }
            public int? WorkerId { get; init; }
            public string? PuestoNombre { get; init; }
            public string? CategoriaNombre { get; init; }
        }

        /// <summary>
        /// Nombre del área que se muestra como "Área del solicitante" y que queda guardado en
        /// <c>gth_solicitud.area_nombre</c>. Sale del árbol (<c>area_scope</c> → <c>area_item</c>)
        /// y ya NO de <c>workers.area</c>: esa columna es texto plano del padrón viejo que dejó de
        /// mantenerse, así que a un trabajador de "Tecnología de la Información" le seguía
        /// diciendo "Proyectos".
        ///
        /// Se sube por el árbol desde el nodo del trabajador hasta el primero que no sea de tipo
        /// "Área de Gerencia", es decir la primera "Área Estándar" de su rama. El walk-up hace
        /// falta porque los gerentes no cuelgan de un área estándar sino directamente del nodo de
        /// su gerencia (mismo motivo que en <see cref="GetGerenteDeArea"/>); cuando toda la rama
        /// es de gerencia — el caso del gerente mismo — se devuelve el nombre de esa gerencia, que
        /// es su área real: devolver null dejaría el campo en "(No se pudo identificar tu área)"
        /// justo para quien la tiene bien registrada.
        /// </summary>
        private static async Task<string?> ResolveAreaNombreInternal(AppDbContext ctx, int? areaScopeId)
        {
            if (areaScopeId is not > 0) return null;

            // El árbol es una tabla chica: se arma en memoria, igual que en GetGerenteDeArea.
            // No se filtra por at.State a propósito: un tipo de área dado de baja no debe cortar
            // el recorrido, solo deja de contar como "Área de Gerencia".
            var nodos = await (
                from s in ctx.AreaScope.AsNoTracking()
                join ai in ctx.AreaItem.AsNoTracking() on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType.AsNoTracking() on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State
                select new
                {
                    s.AreaScopeId,
                    s.AreaScopeParentId,
                    ai.AreaItemName,
                    at.AreaTypeName,
                }).ToListAsync();

            var nodoById = nodos.ToDictionary(n => n.AreaScopeId);

            string? nodoPropio = null;                 // respaldo para la rama toda de gerencia
            var visitados = new HashSet<int>();        // el HashSet corta ciclos por si el árbol quedó mal
            int? actual = areaScopeId;
            while (actual != null && visitados.Add(actual.Value) &&
                   nodoById.TryGetValue(actual.Value, out var nodo))
            {
                nodoPropio ??= nodo.AreaItemName;
                if (!string.Equals(nodo.AreaTypeName, AreaTypeGerencia, StringComparison.OrdinalIgnoreCase))
                    return nodo.AreaItemName;
                actual = nodo.AreaScopeParentId;
            }

            return nodoPropio;
        }

        /// <summary>
        /// Mete a los candidatos FFT de la solicitud en <c>person</c> (la base maestra de personas)
        /// y devuelve el <c>person_id</c> de cada uno indexado por su documento. Diccionario vacío
        /// cuando la solicitud no tiene ninguna vacante FFT: no cuesta ningún roundtrip.
        ///
        /// <b>Por qué acá y no al llenar el formulario.</b> El candidato de un ingreso directo ya
        /// tiene nombre, DNI y correo desde el momento en que se pide: hacerlo esperar hasta que
        /// llene su formulario y GTH lo apruebe lo dejaba fuera de la base maestra durante todo el
        /// trámite. Con esto existe desde el pedido.
        ///
        /// <b>Política de escritura: lo que ya está en la base gana.</b> Es la inversa de la del
        /// formulario del postulante (<c>SincronizarPersonAsync</c>), y a propósito: allá los datos
        /// llegan revisados por GTH, que además ve el aviso de que la persona ya existía; acá los
        /// escribió de memoria quien pide la vacante. Si el documento ya tiene ficha, solo se
        /// rellenan sus huecos — nunca se pisa un nombre ni un correo que ya estaban. Un dedazo en
        /// el DNI no puede renombrar a un trabajador de la empresa. Cuando el candidato llene su
        /// formulario de verdad, ese sí corrige todo con datos validados.
        ///
        /// El <c>ON CONFLICT</c> es sobre <c>document_identity_code</c>, que tiene UNIQUE sin
        /// filtrar por <c>state</c>: una ficha dada de baja con ese mismo documento también lo
        /// atrapa y se reusa, que es justo lo que hay que hacer (insertar reventaría). Ni
        /// <c>state</c> ni <c>active</c> se tocan: revivir una ficha de baja es una decisión de GTH,
        /// no el efecto colateral de pedir una vacante.
        ///
        /// Un solo roundtrip para todos los candidatos del lote. Los documentos repetidos dentro de
        /// la misma solicitud se colapsan antes de mandar la sentencia: Postgres no admite que un
        /// <c>ON CONFLICT DO UPDATE</c> toque la misma fila dos veces en un mismo comando.
        /// </summary>
        private static async Task<Dictionary<string, int>> RegistrarPersonasFftAsync(
            AppDbContext ctx, List<VacanteCreateDto> vacantes, int? userId)
        {
            var candidatos = vacantes
                .Where(v => v.EsFft && !string.IsNullOrWhiteSpace(v.FftCandidatoDocumento))
                .GroupBy(v => v.FftCandidatoDocumento!)
                .Select(g => g.First())
                .ToList();

            if (candidatos.Count == 0) return new Dictionary<string, int>();

            // El tipo de documento sale del que eligió el solicitante (gth_tipo_documento) y se
            // traduce al de la base maestra por el código: DNI ↔ DNI, CE ↔ CE. Es el mismo puente
            // que usa el formulario del postulante — la misma persona no puede entrar con un tipo
            // por un camino y con otro por el otro. Los FFT anteriores al desplegable no traen tipo
            // y caen en DNI, que era lo único que se podía declarar entonces.
            const string sql = """
                INSERT INTO person (
                    document_identity_type_id, document_identity_code, full_name, email,
                    created_date_time, created_user_id, active, state, mostrar_en_boletin)
                SELECT (SELECT dit.document_identity_type_id
                          FROM document_identity_type dit
                          LEFT JOIN gth_tipo_documento gtd
                                 ON gtd.gth_tipo_documento_id = v.tipo_id
                         WHERE upper(btrim(dit.document_identity_type_abbreviation))
                               = upper(btrim(coalesce(gtd.codigo, 'DNI')))
                           AND dit.state = true
                         LIMIT 1),
                       v.doc, upper(btrim(v.nombre)), v.email,
                       now(), @userId, true, true, true
                  FROM unnest(@docs::text[], @nombres::text[], @correos::text[], @tipos::int[])
                    AS v(doc, nombre, email, tipo_id)
                ON CONFLICT (document_identity_code) DO UPDATE SET
                    -- Solo se rellenan huecos: lo que la base maestra ya tenía manda.
                    full_name                 = coalesce(nullif(btrim(person.full_name), ''), EXCLUDED.full_name),
                    email                     = coalesce(nullif(btrim(person.email), ''),     EXCLUDED.email),
                    document_identity_type_id = coalesce(person.document_identity_type_id,    EXCLUDED.document_identity_type_id),
                    -- La marca de auditoría solo se mueve si de verdad se llenó algo: una ficha ya
                    -- completa sale de acá exactamente como entró.
                    updated_date_time = CASE WHEN nullif(btrim(person.full_name), '') IS NULL
                                               OR nullif(btrim(person.email), '')     IS NULL
                                               OR person.document_identity_type_id    IS NULL
                                             THEN now() ELSE person.updated_date_time END,
                    updated_user_id   = CASE WHEN nullif(btrim(person.full_name), '') IS NULL
                                               OR nullif(btrim(person.email), '')     IS NULL
                                               OR person.document_identity_type_id    IS NULL
                                             THEN @userId ELSE person.updated_user_id END
                RETURNING person_id, document_identity_code;
                """;

            var filas = await ctx.Database.GetDbConnection().QueryAsync<PersonFftRow>(
                sql,
                new
                {
                    docs    = candidatos.Select(v => v.FftCandidatoDocumento!).ToArray(),
                    nombres = candidatos.Select(v => v.FftCandidatoNombre ?? string.Empty).ToArray(),
                    correos = candidatos.Select(v => v.FftCandidatoCorreo).ToArray(),
                    tipos   = candidatos.Select(v => v.FftTipoDocumentoId).ToArray(),
                    userId,
                },
                transaction: ctx.Database.CurrentTransaction?.GetDbTransaction());

            return filas.ToDictionary(f => f.DocumentIdentityCode, f => f.PersonId);
        }

        /// <summary>Fila que devuelve el upsert de candidatos FFT en <c>person</c>.</summary>
        private sealed class PersonFftRow
        {
            public int PersonId { get; set; }
            public string DocumentIdentityCode { get; set; } = string.Empty;
        }

        public async Task<SolicitudPersonalCreateResultDto> Create(
            GthSolicitud solicitud, List<VacanteCreateDto> vacantes, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var hayFft        = vacantes.Any(v => v.EsFft);
            var hayAprobables = vacantes.Any(v => !v.EsFft);

            // Estado inicial del pipeline, VACANTE POR VACANTE y no de la solicitud completa: una
            // solicitud puede pedir un puesto normal y un ingreso directo a la vez, y cada uno
            // arranca en otro lado. La vacante normal nace esperando su aprobación (la fase NUEVO
            // —"solicitud registrada"— queda como paso ya cumplido); la FFT no espera a nadie —no
            // hay nada que aprobar, quien pide ya nombró a la persona— y arranca directamente en el
            // EMO de ingreso, que es lo único que le queda (ver FftFlujo).
            var codigosEstadoInicial = new List<string>(2);
            if (hayAprobables) codigosEstadoInicial.Add(EstadoReclutamiento.AprobacionGg);
            if (hayFft)        codigosEstadoInicial.Add(FftFlujo.FaseDestino);

            var estadosIniciales = await ctx.GthEstadoRequerimiento
                .Where(e => codigosEstadoInicial.Contains(e.Codigo) && e.State)
                .ToDictionaryAsync(e => e.Codigo, e => e.GthEstadoRequerimientoId);
            foreach (var codigo in codigosEstadoInicial)
                if (!estadosIniciales.ContainsKey(codigo))
                    throw new AbrilException(
                        $"No está configurado el estado inicial de reclutamiento ({codigo}).", 500);

            // Prioridad por defecto al crear: Media (GTH la ajusta luego desde la bandeja). Puede ser
            // null si el catálogo aún no está sembrado; no es bloqueante.
            var prioridadMediaId = await ctx.GthPrioridad
                .Where(p => p.Codigo == PrioridadReclutamiento.Media && p.State && p.Active)
                .Select(p => (int?)p.GthPrioridadId)
                .FirstOrDefaultAsync();

            // Validar que los ids referenciados existan y estén vigentes. Todo puesto sale del
            // catálogo: el formulario ya no puede dar de alta puestos nuevos.
            var puestoIds  = vacantes.Select(v => v.PuestoId!.Value).Distinct().ToList();
            var tipoIds    = vacantes.Select(v => v.TipoRequerimientoId).Distinct().ToList();
            var projectIds = vacantes.Select(v => v.ProjectId).Distinct().ToList();

            // Se revalida contra la MISMA lista que ofrece el formulario (los puestos del área del
            // solicitante y de sus áreas hijas): lo que no se ofrece tampoco se acepta.
            var puestosDelArea = await QueryPuestosDelArea(ctx, solicitud.AreaScopeId);
            var puestosOk = puestosDelArea.Select(p => p.Id).ToHashSet();
            if (puestoIds.Any(id => !puestosOk.Contains(id)))
                throw new AbrilException(
                    "Uno o más puestos seleccionados no son válidos para tu área.", 400);

            // Nombre del puesto por id: es el snapshot que lleva la ficha del candidato FFT. Sale de
            // la lista que ya se trajo para validar, así que no cuesta un roundtrip nuevo.
            var nombrePorPuesto = puestosDelArea.ToDictionary(p => p.Id, p => p.Nombre);

            // Los tipos se traen (y no solo se cuentan) porque su código decide si la vacante es un
            // reemplazo y, con eso, si hay que exigir el trabajador reemplazado.
            var tipos = await ctx.GthTipoRequerimiento
                .Where(t => tipoIds.Contains(t.GthTipoRequerimientoId) && t.State && t.Active)
                .Select(t => new { t.GthTipoRequerimientoId, t.Codigo })
                .ToListAsync();
            if (tipos.Count != tipoIds.Count)
                throw new AbrilException("Uno o más tipos de requerimiento no son válidos.", 400);

            var tiposReemplazo = tipos
                .Where(t => t.Codigo == TipoRequerimientoReclutamiento.Reemplazo)
                .Select(t => t.GthTipoRequerimientoId)
                .ToHashSet();

            var projectsOk = await ctx.Project.CountAsync(p => projectIds.Contains(p.ProjectId) && p.State && p.Active);
            if (projectsOk != projectIds.Count)
                throw new AbrilException("Uno o más proyectos/obras seleccionados no son válidos.", 400);

            // Trabajador reemplazado: solo tiene sentido en las vacantes de tipo Reemplazo, así que
            // en el resto se descarta lo que haya mandado el cliente.
            foreach (var v in vacantes)
                if (!tiposReemplazo.Contains(v.TipoRequerimientoId)) v.ReemplazaWorkerId = null;

            // Salario bruto mensual: obligatorio en las vacantes NUEVAS —es parte de lo que aprueba
            // Gerencia General— y descartado en los REEMPLAZOS, donde el formulario ya no lo pide.
            // La regla vive acá y no en el servicio porque depende del código del tipo, que es lo
            // que se acaba de traer: preguntarlo dos veces sería un roundtrip de más.
            for (int i = 0; i < vacantes.Count; i++)
            {
                var v = vacantes[i];
                if (tiposReemplazo.Contains(v.TipoRequerimientoId))
                    v.SalarioBrutoMensual = null;
                else if (v.SalarioBrutoMensual is null)
                    throw new AbrilException($"Vacante {i + 1}: debe indicar el salario bruto mensual.", 400);
            }

            if (vacantes.Any(v => tiposReemplazo.Contains(v.TipoRequerimientoId)))
            {
                // Se revalida contra la MISMA lista que ofrece el formulario (área del solicitante
                // y áreas hijas, y solo quienes están adentro hoy): lo que no se ofrece tampoco se
                // acepta. Si el solicitante no tiene area_scope la lista queda vacía y el campo no
                // se exige — exigir algo que el formulario no puede ofrecer dejaría bloqueado el
                // registro de la solicitud.
                var workerIdsArea = (await QueryTrabajadoresDelArea(ctx, solicitud.AreaScopeId))
                    .Select(t => t.Id).ToHashSet();

                for (int i = 0; i < vacantes.Count; i++)
                {
                    var v = vacantes[i];
                    if (!tiposReemplazo.Contains(v.TipoRequerimientoId)) continue;

                    if (v.ReemplazaWorkerId is null or <= 0)
                    {
                        if (workerIdsArea.Count == 0) { v.ReemplazaWorkerId = null; continue; }
                        throw new AbrilException($"Vacante {i + 1}: debe seleccionar el trabajador al que reemplaza.", 400);
                    }

                    // Un id fuera de la lista puede ser de otra área o de alguien que ya no está
                    // adentro (retirado, o una ficha de pre-ingreso): el mensaje cubre los dos.
                    if (!workerIdsArea.Contains(v.ReemplazaWorkerId.Value))
                        throw new AbrilException(
                            $"Vacante {i + 1}: el trabajador al que reemplaza debe ser alguien de tu área (o de un área hija) que trabaje actualmente en Abril.", 400);
                }
            }

            // La solicitud y sus requerimientos entran en una sola transacción, y el correlativo
            // anual se lee y se consume adentro con un candado por año (ver más abajo), así que dos
            // solicitudes simultáneas no pueden quedarse con el mismo código. La transacción se abre
            // dentro de la execution strategy porque el provider corre con EnableRetryOnFailure y no
            // admite transacciones iniciadas por fuera.
            var now = DateTimeOffset.UtcNow;
            var codigos = new List<string>(vacantes.Count);

            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                // La estrategia puede reintentar el bloque completo, así que se descarta lo que dejó
                // el intento anterior (entidades trackeadas, requerimientos ya armados y el id que le
                // hubiera asignado un SaveChanges parcial) para no duplicar nada en el reintento.
                ctx.ChangeTracker.Clear();
                solicitud.GthSolicitudId = 0;
                solicitud.Requerimientos.Clear();
                codigos.Clear();

                await using var tx = await ctx.Database.BeginTransactionAsync();

                // Correlativo anual del código REQ-AAAA-NNNN (año en hora Perú, UTC-5). Se busca
                // SOLO dentro del año, así que se reinicia solo al cambiar de año: el 31/12 se
                // cierra en REQ-2026-9874 y el 01/01 arranca en REQ-2027-0001 sin que haya que
                // correr nada. El año se toma en hora Perú y no en UTC para que el corte sea la
                // medianoche de acá y no las 19:00 del 31/12.
                var anio = now.ToOffset(PeruOffset).Year;

                // Candado por año antes de leer los números usados. La transacción sola NO alcanza:
                // en READ COMMITTED dos solicitudes simultáneas leen lo mismo, arman el mismo código
                // y la segunda muere con violación del índice único de `codigo` — que no es un error
                // transitorio, así que la execution strategy tampoco lo reintenta. El candado lo
                // suelta Postgres al cerrar la transacción, y al ser por año no serializa los
                // registros de años distintos.
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({CorrelativoLockNamespace}, {anio})");

                // El número es el MENOR libre del año, no el máximo + 1. La diferencia solo importa
                // cuando el año tiene huecos, y hoy tiene uno a propósito: la limpieza de la data de
                // prueba del 2026-08-26 borró todo menos REQ-2026-0015 y REQ-2026-0016 —esos ya
                // habían salido por correo y no se podían renumerar sin romper el enlace—, así que
                // el año arranca en 0001 y salta esos dos al llegar.
                //
                // No se filtra por State: el índice único de `codigo` tampoco lo hace, así que una
                // vacante dada de baja sigue ocupando su número. Es una tabla de decenas de filas
                // por año; traerlas cuesta lo mismo que el MAX.
                var numerosUsados = (await ctx.GthRequerimiento
                        .Where(r => r.Anio == anio)
                        .Select(r => r.Numero)
                        .ToListAsync())
                    .ToHashSet();

                var ultimoNumero = 0;

                // Devuelve el siguiente número libre y lo marca como tomado, así dos vacantes del
                // mismo lote tampoco chocan entre ellas.
                int SiguienteNumeroLibre()
                {
                    while (!numerosUsados.Add(++ultimoNumero)) { }
                    return ultimoNumero;
                }

                solicitud.CreatedDateTime = now;
                solicitud.CreatedUserId   = userId;
                solicitud.Active          = true;
                solicitud.State           = true;

                // Los candidatos FFT entran a la base maestra acá mismo, con el pedido: el
                // solicitante ya declaró su DNI, así que no hay por qué esperar a que llenen su
                // formulario para que existan. Va adentro de la transacción para que un pedido que
                // no llega a registrarse tampoco deje personas sueltas.
                var personIdPorDocumento = await RegistrarPersonasFftAsync(ctx, vacantes, userId);

                foreach (var v in vacantes)
                {
                    var numero = SiguienteNumeroLibre();
                    var codigo = $"REQ-{anio}-{numero:D4}";
                    codigos.Add(codigo);
                    solicitud.Requerimientos.Add(new GthRequerimiento
                    {
                        Codigo                   = codigo,
                        Anio                     = anio,
                        Numero                   = numero,
                        PuestoId                 = v.PuestoId!.Value,
                        GthTipoRequerimientoId   = v.TipoRequerimientoId,
                        // Ya normalizado arriba: null en todo lo que no sea un reemplazo.
                        ReemplazaWorkerId        = v.ReemplazaWorkerId,
                        ProjectId                = v.ProjectId,
                        // Ya validado y redondeado a 2 decimales en el servicio.
                        SalarioBrutoMensual      = v.SalarioBrutoMensual,
                        // Ya validado en el servicio: el nombre, el DNI y el correo del candidato
                        // solo viajan en las vacantes FFT y son obligatorios en ellas.
                        EsFft                    = v.EsFft,
                        FftCandidatoNombre       = v.EsFft ? v.FftCandidatoNombre : null,
                        FftCandidatoCorreo       = v.EsFft ? v.FftCandidatoCorreo : null,
                        FftCandidatoDocumento    = v.EsFft ? v.FftCandidatoDocumento : null,
                        GthTipoDocumentoId       = v.EsFft ? v.FftTipoDocumentoId : null,
                        // A qué fila de `person` quedó enganchado el candidato. Siempre hay una en
                        // una vacante FFT (el upsert de arriba la creó o la encontró).
                        FftPersonId              = v.EsFft
                            ? personIdPorDocumento.GetValueOrDefault(v.FftCandidatoDocumento ?? string.Empty)
                            : null,
                        GthEstadoRequerimientoId = estadosIniciales[
                            v.EsFft ? FftFlujo.FaseDestino : EstadoReclutamiento.AprobacionGg],
                        GthPrioridadId           = prioridadMediaId,
                        CreatedDateTime          = now,
                        CreatedUserId            = userId,
                        Active                   = true,
                        State                    = true,
                    });
                }

                ctx.GthSolicitud.Add(solicitud);
                await ctx.SaveChangesAsync();

                // Ingreso directo: no espera la aprobación de nadie, así que acá mismo se abre
                // entero cada vacante FFT — candidato seleccionado y ficha de pre-ingreso lista para
                // su EMO. Va en un segundo SaveChanges dentro de la MISMA transacción porque
                // gth_candidato copia la FK del requerimiento a mano (no tiene navegación) y hasta
                // el primer guardado no hay id que copiar: o queda la solicitud con sus ingresos
                // abiertos, o no queda nada.
                if (hayFft)
                {
                    var catalogoFft = await FftFlujo.CargarCatalogoAsync(ctx);
                    var reqsFft     = solicitud.Requerimientos.Where(r => r.EsFft).ToList();

                    // En una solicitud recién creada ningún requerimiento tiene candidato todavía,
                    // así que el guardia de idempotencia arranca vacío: lo único que tiene que
                    // atajar es que dos vacantes del mismo lote no abran dos candidatos.
                    var yaConCandidato = await FftFlujo.RequerimientosConCandidatoAsync(
                        ctx, reqsFft.Select(r => r.GthRequerimientoId).ToList());

                    foreach (var req in reqsFft)
                        await FftFlujo.AbrirIngresoDirectoAsync(
                            ctx, req, catalogoFft,
                            nombrePorPuesto.GetValueOrDefault(req.PuestoId), yaConCandidato, userId, now);

                    await ctx.SaveChangesAsync();
                }

                await tx.CommitAsync();
            });

            return new SolicitudPersonalCreateResultDto
            {
                SolicitudId = solicitud.GthSolicitudId,
                Codigos     = codigos,
            };
        }
    }

    /// <summary>
    /// Códigos estables del tipo de requerimiento (espejo de <c>gth_tipo_requerimiento.codigo</c>).
    /// El nombre del catálogo es presentación y puede cambiar; la lógica decide por estos códigos.
    /// </summary>
    internal static class TipoRequerimientoReclutamiento
    {
        public const string Nuevo     = "NUEVO";

        /// <summary>La vacante cubre a un trabajador que sale: exige decir a quién reemplaza.</summary>
        public const string Reemplazo = "REEMPLAZO";
    }

    /// <summary>Códigos estables de estados de reclutamiento (espejo de gth_estado_requerimiento.codigo).</summary>
    internal static class EstadoReclutamiento
    {
        public const string Nuevo             = "NUEVO";
        public const string AprobacionGg      = "APROBACION_GG";
        public const string ValidacionGth     = "VALIDACION_GTH";
        public const string Publicacion       = "PUBLICACION";
        public const string LongList          = "LONG_LIST";
        public const string LongListEnviada   = "LONG_LIST_ENVIADA";
        public const string LongListAprobada  = "LONG_LIST_APROBADA";
        public const string Entrevistas       = "ENTREVISTAS";

        /// <summary>
        /// GTH ya envió al menos un finalista con su informe: la decisión pasa al área solicitante,
        /// que elige a quién le da el puesto. Última fase antes del cierre.
        /// </summary>
        public const string SeleccionJefatura = "SELECCION_JEFATURA";

        /// <summary>
        /// El solicitante aprobó al finalista y falta el EMO de Ingreso. La ficha de pre-ingreso
        /// del finalista ya existe en <c>workers</c> y desde el botón de esta fase se salta a
        /// SSOMA · Salud Ocupacional · EMOs a programarle la cita.
        ///
        /// El requerimiento se queda acá todo lo que dure el examen —programación, confirmación de
        /// la clínica, atención y carga de resultados—: lo que lo mueve es la aptitud del EMO, no la
        /// cita. Con APTO pasa a <see cref="EmoApto"/>, con APTO CON RESTRICCIONES a
        /// <see cref="EmoAptoRestricciones"/>, con OBSERVADO a <see cref="EmoObservado"/> y con NO
        /// APTO a <see cref="EmoNoApto"/> o a <see cref="LongList"/>.
        /// </summary>
        public const string EmoIngreso        = "EMO_INGRESO";

        /// <summary>
        /// El EMO de ingreso salió <b>Apto</b>. El proceso NO cierra solo: queda esperando a que GTH
        /// lo cierre y lo pase a onboarding desde el detalle del requerimiento
        /// (<c>cerrar-proceso</c>). Esa confirmación existe porque el cierre es lo que habilita al
        /// candidato en Onboarding, y GTH tiene que poder ver el resultado del examen antes de dar
        /// ese paso en vez de encontrarse el proceso cerrado por su cuenta.
        ///
        /// Sembrado con <c>active = false</c> y con el <c>orden</c> de <see cref="EmoIngreso"/>, por
        /// el mismo motivo que <see cref="EmoNoApto"/>: es un estado del requerimiento, no un paso
        /// propio de la línea de tiempo, y el paso vigente sigue siendo el del EMO.
        /// </summary>
        public const string EmoApto           = "EMO_APTO";

        /// <summary>
        /// El EMO de ingreso salió <b>Apto con Restricciones</b>. Vale exactamente lo mismo que
        /// <see cref="EmoApto"/> —la persona puede entrar y GTH cierra el proceso— y es un estado
        /// aparte solo para que el badge diga qué dijo la clínica sin tener que abrir la ficha.
        /// </summary>
        public const string EmoAptoRestricciones = "EMO_APTO_RESTRICCIONES";

        /// <summary>
        /// El EMO de ingreso salió <b>Observado</b>: la clínica derivó al candidato a interconsulta
        /// y su aptitud real todavía no está definida. El proceso no avanza ni retrocede — no se
        /// puede cerrar (no hay apto que confirmar) ni continuar con otro candidato (este todavía
        /// puede resultar apto) —, solo espera el resultado de la interconsulta, que al cargarse
        /// como EMO vuelve a mover el requerimiento.
        ///
        /// Sembrado igual que <see cref="EmoApto"/>: <c>active = false</c> y el orden del EMO.
        /// </summary>
        public const string EmoObservado      = "EMO_OBSERVADO";

        /// <summary>
        /// El EMO de ingreso del finalista salió <b>No Apto</b> y el proceso volvió a manos de GTH:
        /// tiene que retomar a alguien del historial de rechazados o preparar una nueva long list.
        /// Es una fase de decisión y dura lo que GTH tarde en elegir.
        ///
        /// Está sembrado con <c>active = false</c> a propósito, igual que <see cref="RechazadoGg"/>:
        /// es un estado del requerimiento pero no un paso por el que pasen todos, y la línea de
        /// tiempo del seguimiento solo lista las fases activas. Su <c>orden</c> es el mismo de
        /// <see cref="EmoIngreso"/>, que es lo que hace que esa línea marque el paso del EMO como
        /// el actual en vez de darlo todo por cumplido.
        ///
        /// Solo se llega acá con rechazados que retomar. Sin ninguno el requerimiento se manda
        /// directo a <see cref="LongList"/> —el único trabajo que le queda a GTH— salvo que sea un
        /// ingreso directo FFT, que no tiene long list y termina en
        /// <see cref="CerradoSinCubrir"/>.
        /// </summary>
        public const string EmoNoApto         = "EMO_NO_APTO";

        /// <summary>
        /// Estado final: GTH cerró el proceso desde <see cref="EmoApto"/> /
        /// <see cref="EmoAptoRestricciones"/>. El reclutamiento termina y el seleccionado aparece en
        /// Onboarding como candidato por ingresar (ver <c>OnboardingRepository</c>, que lista
        /// exactamente los SELECCIONADOS de requerimientos CERRADOS).
        ///
        /// Ni programar la cita ni el resultado del examen cierran el proceso por sí solos: lo
        /// cierra GTH a mano, con el resultado del EMO ya a la vista.
        /// </summary>
        public const string Cerrado           = "CERRADO";

        /// <summary>
        /// Estado final del proceso que terminó SIN cubrir la vacante: el ingreso directo (FFT) cuyo
        /// candidato salió No Apto en el EMO. Ese flujo nace con su candidato puesto por el
        /// solicitante, así que no tiene rechazados que retomar ni long list a la que volver — no
        /// hay nada que GTH pueda hacer y dejarlo en <see cref="EmoNoApto"/> lo colgaba en una
        /// pantalla de decisión sin opciones.
        ///
        /// Es un estado aparte de <see cref="Cerrado"/> a propósito: comparte su <c>orden</c> (el
        /// proceso terminó) y su etapa del embudo, pero <see cref="Cerrado"/> significa "la vacante
        /// se cubrió y el seleccionado pasa a onboarding" y es lo que cuenta la tarjeta "Procesos
        /// cerrados". Meter acá una vacante que nunca se llenó la contaría como un cierre exitoso.
        /// </summary>
        public const string CerradoSinCubrir  = "CERRADO_SIN_CUBRIR";

        /// <summary>
        /// Las dos fases desde las que GTH puede cerrar el proceso y pasar el candidato a
        /// onboarding: las que dicen que el examen médico salió bien.
        /// </summary>
        public static readonly HashSet<string> EmoAptas = new()
        {
            EmoApto,
            EmoAptoRestricciones,
        };

        /// <summary>
        /// Estado final: Gerencia General no aprobó la vacante, así que nunca llega a GTH. Está
        /// sembrado con <c>active = false</c> a propósito: es un estado del requerimiento pero no
        /// una fase del pipeline, y la línea de tiempo del seguimiento solo lista las fases activas.
        /// </summary>
        public const string RechazadoGg       = "RECHAZADO_GG";

        /// <summary>
        /// Estados en los que el requerimiento todavía NO le pertenece a GTH: sigue en el paso de
        /// Gerencia General, o el GG lo rechazó. La bandeja de GTH los excluye (solo ve lo aprobado).
        /// </summary>
        public static readonly HashSet<string> FueraDeGth = new()
        {
            AprobacionGg,
            RechazadoGg,
        };

        /// <summary>
        /// Fases en las que el siguiente paso le toca a GTH (tarjeta "En revisión · GTH evaluando"
        /// del panel del solicitante). Deja fuera a propósito:
        /// <list type="bullet">
        ///   <item><description><see cref="Nuevo"/>: es el primer paso de GTH y ya se cuenta en
        ///   "Pendientes · Sin respuesta"; contarlo en ambas tarjetas duplicaría el mismo proceso.</description></item>
        ///   <item><description><see cref="AprobacionGg"/>: el paso es de Gerencia General, no de GTH.</description></item>
        ///   <item><description><see cref="LongListEnviada"/> y <see cref="SeleccionJefatura"/>:
        ///   la pelota está del lado del solicitante (revisar la long list / decidir al finalista).</description></item>
        ///   <item><description><see cref="Cerrado"/>: el proceso ya terminó.</description></item>
        /// </list>
        ///
        /// <see cref="EmoIngreso"/> y las cuatro fases de resultado del examen sí entran: desde que
        /// el cierre lo decide la aptitud del EMO, un requerimiento se queda en el examen todo lo
        /// que este dure y sin ellas desaparecía de las tarjetas del solicitante sin explicación.
        /// Antes no hacía falta porque la fase del EMO duraba lo que GTH tardaba en agendar la cita.
        /// </summary>
        public static readonly HashSet<string> FasesGth = new()
        {
            ValidacionGth,
            Publicacion,
            LongList,
            LongListAprobada,
            Entrevistas,
            EmoIngreso,
            EmoApto,
            EmoAptoRestricciones,
            EmoObservado,
            EmoNoApto,
        };
    }

    /// <summary>
    /// Etapas del embudo "Pipeline de reclutamiento" (vista de GTH), en el orden en que se muestran.
    /// El embudo agrupa las fases del catálogo en etapas legibles; cada fase pertenece a exactamente
    /// una etapa, de modo que la suma de las etapas es siempre el total de requerimientos vigentes y
    /// ninguno se pierde del embudo.
    ///
    /// El orden sigue el <c>orden</c> del catálogo, que es el mismo que ve el solicitante en el
    /// seguimiento vertical del requerimiento.
    /// </summary>
    internal sealed record EtapaPipeline(string Codigo, string Nombre, string[] Fases)
    {
        public static readonly EtapaPipeline[] Todas =
        {
            new("SOLICITUD",   "Solicitud",   new[] { EstadoReclutamiento.Nuevo,
                                                      EstadoReclutamiento.AprobacionGg,
                                                      EstadoReclutamiento.ValidacionGth }),
            new("PUBLICADO",   "Publicado",   new[] { EstadoReclutamiento.Publicacion }),
            new("REVISION",    "Revisión",    new[] { EstadoReclutamiento.LongList,
                                                      EstadoReclutamiento.LongListEnviada,
                                                      EstadoReclutamiento.LongListAprobada }),
            new("ENTREVISTAS", "Entrevistas", new[] { EstadoReclutamiento.Entrevistas }),
            new("SELECCION",   "Selección",   new[] { EstadoReclutamiento.SeleccionJefatura }),
            // El EMO es etapa propia y no un apéndice del cierre: desde que programar la cita dejó
            // de cerrar el proceso, un requerimiento puede pasar semanas acá. Sin esta etapa sus
            // cinco fases no pertenecen a ninguna y sus requerimientos se caen del embudo — y de
            // "Vacantes abiertas", que se calcula recortando estas mismas etapas.
            new("EMO",         "EMO",         new[] { EstadoReclutamiento.EmoIngreso,
                                                      EstadoReclutamiento.EmoApto,
                                                      EstadoReclutamiento.EmoAptoRestricciones,
                                                      EstadoReclutamiento.EmoObservado,
                                                      EstadoReclutamiento.EmoNoApto }),
            // CERRADO_SIN_CUBRIR entra en la etapa de cierre porque el proceso terminó igual: si no,
            // "En proceso" (que es el total menos esta etapa) seguiría contando un requerimiento
            // que ya no tiene nada pendiente.
            new("CIERRE",      "Cierre",      new[] { EstadoReclutamiento.Cerrado,
                                                      EstadoReclutamiento.CerradoSinCubrir }),
        };

        /// <summary>Código de la etapa terminal: lo ya cerrado no cuenta como proceso activo.</summary>
        public const string CodigoCierre = "CIERRE";

        /// <summary>Código de la etapa en la que la vacante recién se publica (inicio de "vacantes abiertas").</summary>
        public const string CodigoPublicado = "PUBLICADO";
    }

    /// <summary>Códigos estables de prioridad de reclutamiento (espejo de gth_prioridad.codigo).</summary>
    internal static class PrioridadReclutamiento
    {
        public const string Media = "MEDIA";
    }

    /// <summary>Códigos estables del estado de revisión de un candidato (espejo de gth_candidato_estado.codigo).</summary>
    internal static class EstadoCandidato
    {
        public const string Pendiente = "PENDIENTE";
        public const string Aprobado  = "APROBADO";
        public const string Rechazado = "RECHAZADO";
    }

    /// <summary>
    /// Códigos estables del resultado del candidato en el proceso (espejo de
    /// gth_candidato_resultado.codigo). Es una sola línea de tiempo: PENDIENTE → PASO (finalista
    /// enviado al solicitante) → SELECCIONADO / RECHAZADO por el solicitante; NO_PASO es la salida
    /// que decide GTH tras la entrevista, y NO_APTO_EMO la que decide el examen médico.
    /// </summary>
    internal static class ResultadoCandidato
    {
        public const string Pendiente    = "PENDIENTE";
        public const string Paso         = "PASO";
        public const string NoPaso       = "NO_PASO";
        public const string Seleccionado = "SELECCIONADO";
        public const string Rechazado    = "RECHAZADO";

        /// <summary>
        /// El seleccionado no pasó el EMO de ingreso. Es un resultado propio y no un
        /// <see cref="Rechazado"/> porque el historial muestra quién rechazó a cada candidato:
        /// dejarlo en RECHAZADO diría que lo descartó el área solicitante en la decisión final, que
        /// es lo contrario de lo que pasó — el área lo eligió y lo frenó el examen médico.
        /// </summary>
        public const string NoAptoEmo    = "NO_APTO_EMO";

        /// <summary>
        /// Resultados que cierran la participación de un candidato: ya no está "en carrera" y no se
        /// le puede volver a decidir sin retomarlo desde el historial de rechazados.
        /// </summary>
        public static readonly HashSet<string> Cerrados = new()
        {
            NoPaso,
            Seleccionado,
            Rechazado,
            NoAptoEmo,
        };
    }

    /// <summary>
    /// Etapas en las que un candidato puede quedar rechazado, para el historial de rechazados.
    /// No son un catálogo de la base: se derivan de <c>gth_candidato_estado</c> y
    /// <c>gth_candidato_resultado</c> al leer, así que no pueden desincronizarse de la decisión
    /// real. El frontend colorea la etiqueta por estos códigos.
    /// </summary>
    internal static class EtapaRechazo
    {
        /// <summary>El solicitante lo rechazó al revisar la long list de CVs.</summary>
        public const string LongList      = "LONG_LIST";

        /// <summary>
        /// GTH lo sacó del proceso tras rechazarle el formulario del postulante, antes de
        /// programarle entrevista (resultado NO_PASO + correo de fin de proceso, sin cita).
        /// </summary>
        public const string Formulario    = "FORMULARIO";

        /// <summary>GTH lo descartó tras la entrevista (resultado NO_PASO + agradecimiento).</summary>
        public const string Entrevistas   = "ENTREVISTAS";

        /// <summary>El solicitante rechazó al finalista en la decisión final del proceso.</summary>
        public const string DecisionFinal = "DECISION_FINAL";

        /// <summary>
        /// El seleccionado salió No Apto en su EMO de ingreso. Es la única etapa de la que NO se
        /// puede retomar a nadie: un examen médico no se revierte volviendo a elegir a la persona.
        /// </summary>
        public const string Emo           = "EMO";

        /// <summary>
        /// Etapas desde las que sí se puede retomar a un candidato (todas menos
        /// <see cref="Emo"/>). Es la regla que enciende el botón «Retomar» del historial.
        /// </summary>
        public static readonly HashSet<string> Retomables = new()
        {
            LongList,
            Formulario,
            Entrevistas,
            DecisionFinal,
        };
    }

    /// <summary>Quién tomó el rechazo en el historial: el área usuaria, GTH o el examen médico.</summary>
    internal static class RechazadoPor
    {
        public const string Solicitante      = "SOLICITANTE";
        public const string Gth              = "GTH";

        /// <summary>No lo decidió una persona del proceso sino el resultado del EMO de ingreso.</summary>
        public const string SaludOcupacional = "SALUD_OCUPACIONAL";
    }
}
