using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Repositories;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared
{
    /// <summary>
    /// El salto del flujo <b>FFT</b> (ingreso directo), en un solo lugar porque lo disparan tres
    /// repositorios distintos y tiene que hacer exactamente lo mismo en los tres.
    ///
    /// <see cref="AbrirIngresoDirectoAsync"/> — el proceso se salta TODO el pipeline de selección:
    /// aprobación, publicación, revisión de CV, long list, formulario del postulante, entrevistas,
    /// finalistas y decisión del solicitante. No hay nada que decidir — quien pidió la vacante ya
    /// nombró a la persona — así que la vacante nace con su candidato SELECCIONADO, su ficha de
    /// pre-ingreso abierta y el requerimiento en EMO de ingreso, que es lo único que queda por
    /// hacer. Pasa al registrarse el pedido, lo pida quien lo pida (ver
    /// <see cref="Application.RutaAprobacion.Ninguna"/>); los FFT que quedaron esperando la firma
    /// de Gerencia General desde antes de ese cambio entran acá al aprobárselos.
    ///
    /// El candidato no llena formulario: sus datos (nombre, tipo y número de documento y correo
    /// personal) los declaró el solicitante y ya entraron a <c>person</c> al registrarse el pedido
    /// (<c>gth_requerimiento.fft_person_id</c>), que es de donde sale la ficha.
    ///
    /// No guarda: deja los cambios en el <see cref="AppDbContext"/> que se le pasa para que entren
    /// en el mismo <c>SaveChanges</c> que la operación que lo disparó. Un requerimiento FFT a medio
    /// abrir sería un proceso trabado sin forma de destrabarlo desde la pantalla.
    ///
    /// <see cref="CerrarConSeleccionadoAsync"/> sigue existiendo para los FFT <b>anteriores</b> a
    /// este salto, que quedaron parados en el formulario del postulante: al aprobárselo, cierran
    /// como cerraban antes.
    /// </summary>
    internal static class FftFlujo
    {
        /// <summary>
        /// Fase en la que espera un requerimiento FFT que ya está en manos de GTH: el EMO de
        /// ingreso del candidato, que es lo único que le queda al proceso. Antes esperaba en el
        /// formulario del postulante; desde que el ingreso directo no lo pide, esa parada
        /// desapareció.
        /// </summary>
        public const string FaseDestino = EstadoReclutamiento.EmoIngreso;

        /// <summary>
        /// Fase en la que quedaron parados los FFT <b>anteriores</b> a que el flujo se saltara el
        /// formulario del postulante: la misma en la que el flujo normal deja al proceso cuando el
        /// solicitante aprobó la long list. Ya no se manda a nadie ahí — solo se reconoce, para que
        /// los que quedaron puedan terminar por el camino viejo.
        /// </summary>
        public const string FaseFormularioLegado = EstadoReclutamiento.LongListAprobada;

        /// <summary>
        /// Texto del "siguiente paso" de un requerimiento FFT de los que quedaron parados en
        /// <see cref="FaseFormularioLegado"/>. La descripción del catálogo habla de la long list,
        /// que en este flujo no existe.
        /// </summary>
        public const string SiguientePasoFormulario =
            "GTH le enviará el formulario de información al candidato del ingreso directo (FFT).";

        /// <summary>
        /// Fases que un requerimiento FFT nunca recorre. Se usan para recortar la línea de tiempo
        /// del seguimiento: mostrar «Publicación», «Long list» o «Entrevistas» como pasos pendientes
        /// de un proceso que no los tiene deja al solicitante esperando algo que no va a pasar.
        ///
        /// <see cref="FaseFormularioLegado"/> entró a la lista cuando el ingreso directo dejó de
        /// pedir el formulario. Los FFT viejos que quedaron parados ahí son la excepción: quien
        /// recorta la línea no puede quitar la fase en la que el requerimiento está de verdad.
        ///
        /// APROBACION_GG no está acá porque los FFT anteriores a que el ingreso directo dejara de
        /// aprobarse sí la recorrieron, y eso se resuelve al leer (por su fila de detalle).
        /// </summary>
        public static readonly HashSet<string> FasesOmitidas = new()
        {
            EstadoReclutamiento.Publicacion,
            EstadoReclutamiento.LongList,
            EstadoReclutamiento.LongListEnviada,
            EstadoReclutamiento.LongListAprobada,
            EstadoReclutamiento.Entrevistas,
            EstadoReclutamiento.SeleccionJefatura,
        };

        /// <summary>
        /// Los tres ids de catálogo que necesita <see cref="AbrirIngresoDirecto"/>. Se resuelven una
        /// sola vez por operación con <see cref="CargarCatalogoAsync"/>, aunque el lote traiga varias
        /// vacantes FFT.
        /// </summary>
        public sealed record Catalogo(
            int EstadoEmoIngresoId,
            string EstadoEmoIngresoNombre,
            int CandidatoAprobadoId,
            int ResultadoSeleccionadoId);

        /// <summary>
        /// Ids de catálogo del salto FFT. Solo se llama cuando la operación trae alguna vacante FFT:
        /// en una solicitud normal no cuesta ningún roundtrip.
        /// </summary>
        public static async Task<Catalogo> CargarCatalogoAsync(AppDbContext ctx)
        {
            var emo = await ctx.GthEstadoRequerimiento
                .Where(e => e.Codigo == FaseDestino && e.State)
                .Select(e => new { e.GthEstadoRequerimientoId, e.Nombre })
                .FirstOrDefaultAsync()
                ?? throw new AbrilException(
                    $"No está configurado el estado {FaseDestino} de reclutamiento.", 500);

            var aprobadoId = await ctx.GthCandidatoEstado
                .Where(e => e.Codigo == EstadoCandidato.Aprobado && e.State)
                .Select(e => e.GthCandidatoEstadoId)
                .FirstOrDefaultAsync();
            if (aprobadoId == 0)
                throw new AbrilException(
                    "No está configurado el estado APROBADO de candidatos de reclutamiento.", 500);

            var seleccionadoId = await ctx.GthCandidatoResultado
                .Where(r => r.Codigo == ResultadoCandidato.Seleccionado && r.State)
                .Select(r => r.GthCandidatoResultadoId)
                .FirstOrDefaultAsync();
            if (seleccionadoId == 0)
                throw new AbrilException(
                    "No está configurado el resultado SELECCIONADO de candidatos.", 500);

            return new Catalogo(
                emo.GthEstadoRequerimientoId, emo.Nombre, aprobadoId, seleccionadoId);
        }

        /// <summary>
        /// Abre el ingreso directo entero: le crea al candidato su ficha (ya APROBADA y ya
        /// SELECCIONADA), le abre su ficha de pre-ingreso en <c>workers</c> y deja el requerimiento
        /// en <see cref="FaseDestino"/> — el EMO de ingreso, lo único que le queda al proceso. Lo
        /// llaman el registro de la solicitud (todo FFT nuevo pasa por ahí) y la decisión de
        /// Gerencia General sobre los FFT que quedaron esperando su firma.
        ///
        /// No hay formulario del postulante de por medio: el ingreso directo no lo pide. Los datos
        /// del candidato los declaró el solicitante y ya están en <c>person</c> desde que se
        /// registró el pedido, así que no hay nada que preguntarle antes de programarle el examen.
        ///
        /// Idempotente: si el candidato ya existe (reintento de la execution strategy, o una
        /// aprobación que se registró dos veces) no crea otro ni vuelve a mover nada.
        /// </summary>
        /// <param name="catalogo">Ids de catálogo resueltos una vez por operación (ver <see cref="CargarCatalogoAsync"/>).</param>
        /// <param name="puestoNombre">
        /// Nombre del puesto del requerimiento, para el snapshot del candidato (igual que en la long
        /// list normal, donde lo copia el sistema y no lo escribe GTH). Lo pasa el llamador, que ya
        /// lo tiene o lo puede traer de una vez para todas las vacantes del lote.
        /// </param>
        /// <param name="yaConCandidato">
        /// Requerimientos que ya tienen candidato, resuelto de una vez para todo el lote con
        /// <see cref="RequerimientosConCandidatoAsync"/> (una decisión en bloque puede traer varias
        /// vacantes FFT y preguntarlo por cada una sería un roundtrip por vacante). El método lo
        /// actualiza al crear, así que también protege de una doble llamada dentro del mismo lote.
        /// </param>
        /// <returns>
        /// La ficha de pre-ingreso del candidato (su <c>Id</c> queda en 0 hasta el
        /// <c>SaveChanges</c> si es nueva), para poder enlazar el correo directo a su programación
        /// de EMO. Null cuando el requerimiento no es FFT, cuando el candidato ya estaba abierto o
        /// cuando la vacante no dejó <c>fft_person_id</c> — los FFT anteriores a que la casilla
        /// pidiera el documento no lo tienen y no hay a quién programarle nada.
        /// </returns>
        /// <remarks>
        /// El requerimiento tiene que estar YA persistido: <c>gth_candidato</c> no tiene navegación
        /// hacia él, así que la FK se copia a mano y un id en 0 dejaría al candidato colgado. Todos
        /// los llamadores lo cumplen (la creación de la solicitud guarda antes de llamar acá).
        /// La evaluación sí va por navegación, que es lo que permite que todo esto entre en un solo
        /// <c>SaveChanges</c> (ver <c>GthCandidatoEvaluacion.Candidato</c>).
        /// </remarks>
        public static async Task<Worker?> AbrirIngresoDirectoAsync(
            AppDbContext ctx,
            GthRequerimiento req,
            Catalogo catalogo,
            string? puestoNombre,
            ISet<int> yaConCandidato,
            int? userId,
            DateTimeOffset now)
        {
            if (!req.EsFft) return null;

            req.GthEstadoRequerimientoId = catalogo.EstadoEmoIngresoId;
            req.UpdatedDateTime          = now;
            req.UpdatedUserId            = userId;

            if (req.GthRequerimientoId == 0)
                throw new AbrilException(
                    "No se pudo abrir la ficha del candidato FFT: el requerimiento aún no está registrado.", 500);

            // Idempotente: un reintento de la execution strategy o una aprobación que se registró
            // dos veces no puede dejar dos candidatos en el mismo proceso.
            if (!yaConCandidato.Add(req.GthRequerimientoId)) return null;

            var candidato = new GthCandidato
            {
                GthRequerimientoId   = req.GthRequerimientoId,
                Nombre               = req.FftCandidatoNombre?.Trim() ?? "Candidato FFT",
                Puesto               = puestoNombre,
                // Sin CV: en FFT no hay revisión curricular ni formulario en el que el postulante
                // pueda adjuntar el suyo.
                GthCandidatoEstadoId = catalogo.CandidatoAprobadoId,
                Orden                = 0,
                NumeroLongList       = 1,
                CreatedDateTime      = now,
                CreatedUserId        = userId,
                Active               = true,
                State                = true,
            };
            ctx.GthCandidato.Add(candidato);

            // El candidato queda SELECCIONADO en el mismo acto: no hay entrevista que evaluar ni
            // finalista que decidir — quien pidió la vacante ya eligió. La decisión se registra a
            // nombre de quien aprobó (o registró) el ingreso, que es cuando realmente ocurre.
            ctx.GthCandidatoEvaluacion.Add(new GthCandidatoEvaluacion
            {
                Candidato               = candidato,
                GthCandidatoResultadoId = catalogo.ResultadoSeleccionadoId,
                DecisionDateTime        = now,
                DecisionUserId          = userId,
                CreatedDateTime         = now,
                CreatedUserId           = userId,
                Active                  = true,
                State                   = true,
            });

            // La ficha de pre-ingreso sale de la persona que el pedido dejó registrada en `person`.
            // Sin ella no hay a quién programarle el EMO: el requerimiento igual avanza y la
            // pantalla lo dice, como en el flujo normal cuando el formulario no dejó person_id.
            return req.FftPersonId.HasValue
                ? await AbrirFichaPreIngresoAsync(ctx, req.FftPersonId.Value, req, now)
                : null;
        }

        /// <summary>
        /// De los requerimientos indicados, cuáles ya tienen candidato. Una sola consulta para todo
        /// el lote: es el guardia de idempotencia de <see cref="AbrirIngresoDirectoAsync"/>. Vacío (sin tocar
        /// la BD) cuando no hay ninguna vacante FFT que abrir.
        /// </summary>
        public static async Task<HashSet<int>> RequerimientosConCandidatoAsync(
            AppDbContext ctx, IReadOnlyCollection<int> requerimientoIds)
        {
            if (requerimientoIds.Count == 0) return new HashSet<int>();

            var ids = requerimientoIds.ToList();
            return (await ctx.GthCandidato
                    .Where(c => c.State && ids.Contains(c.GthRequerimientoId))
                    .Select(c => c.GthRequerimientoId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();
        }

        /// <summary>
        /// Cierre de los FFT <b>anteriores</b> a que el ingreso directo se saltara el formulario:
        /// los que quedaron parados en <see cref="FaseFormularioLegado"/> siguen terminando por acá
        /// cuando GTH les aprueba el formulario. Marca al candidato como SELECCIONADO, mueve el
        /// requerimiento a EMO de ingreso y le abre su ficha de pre-ingreso en <c>workers</c>.
        ///
        /// Los FFT nuevos ya no pasan por este método: nacen así desde
        /// <see cref="AbrirIngresoDirectoAsync"/>.
        /// </summary>
        /// <returns>
        /// La ficha de pre-ingreso (su <c>Id</c> queda en 0 hasta el <c>SaveChanges</c> si es nueva) y
        /// el nombre del estado en el que quedó el requerimiento. La ficha es null cuando el
        /// formulario no dejó <c>person_id</c> y por lo tanto no hay a quién programarle el EMO.
        /// </returns>
        public static async Task<(Worker? Ficha, string EstadoNombre)> CerrarConSeleccionadoAsync(
            AppDbContext ctx, int candidatoId, GthRequerimiento req, int? userId, DateTimeOffset now)
        {
            var seleccionadoId = await ctx.GthCandidatoResultado
                .Where(r => r.Codigo == ResultadoCandidato.Seleccionado && r.State)
                .Select(r => (int?)r.GthCandidatoResultadoId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("No está configurado el resultado SELECCIONADO de candidatos.", 500);

            // Lo que ya está en el contexto sin guardar cuenta igual que lo que está en la base: si
            // la execution strategy reintenta el bloque, buscar solo en la BD no encontraría la fila
            // que el intento anterior dejó como Added y se insertaría dos veces.
            var evaluacion = ctx.GthCandidatoEvaluacion.Local
                    .FirstOrDefault(e => e.GthCandidatoId == candidatoId && e.State)
                ?? await ctx.GthCandidatoEvaluacion
                    .FirstOrDefaultAsync(e => e.GthCandidatoId == candidatoId && e.State);

            if (evaluacion == null)
            {
                evaluacion = new GthCandidatoEvaluacion
                {
                    GthCandidatoId  = candidatoId,
                    CreatedDateTime = now,
                    CreatedUserId   = userId,
                    Active          = true,
                    State           = true,
                };
                ctx.GthCandidatoEvaluacion.Add(evaluacion);
            }
            else
            {
                evaluacion.UpdatedDateTime = now;
                evaluacion.UpdatedUserId   = userId;
            }

            evaluacion.GthCandidatoResultadoId = seleccionadoId;
            // Quién y cuándo: en FFT la decisión es la aprobación del formulario que acaba de hacer
            // GTH, así que ese es el momento y ese es el usuario. No hay decisión del solicitante
            // que registrar — su decisión fue pedir a esta persona por nombre.
            evaluacion.DecisionDateTime = now;
            evaluacion.DecisionUserId   = userId;

            var emoIngreso = await ctx.GthEstadoRequerimiento
                .FirstOrDefaultAsync(e => e.Codigo == EstadoReclutamiento.EmoIngreso && e.State)
                ?? throw new AbrilException("No está configurado el estado EMO_INGRESO de reclutamiento.", 500);

            req.GthEstadoRequerimientoId = emoIngreso.GthEstadoRequerimientoId;
            req.UpdatedDateTime          = now;
            req.UpdatedUserId            = userId;

            // En el camino legado la persona sale del formulario que el candidato sí llenó.
            var personId = await ctx.GthPostulanteFormulario
                .Where(f => f.GthCandidatoId == candidatoId && f.State && f.PersonId != null)
                .Select(f => f.PersonId)
                .FirstOrDefaultAsync();

            var ficha = personId.HasValue
                ? await AbrirFichaPreIngresoAsync(ctx, personId.Value, req, now)
                : null;

            return (ficha, emoIngreso.Nombre);
        }

        /// <summary>
        /// Ficha de pre-ingreso del candidato FFT: la misma regla que la del finalista aprobado del
        /// flujo normal (reusar la ficha viva si ya existe, abrirla si no) porque es la misma cosa —
        /// alguien que todavía no ingresó pero al que hay que poder programarle su EMO.
        ///
        /// El área sale del DESTINO del puesto que se pidió (<c>puesto.area_destino_scope_id</c>),
        /// que es lo que permite resolver su jefatura subiendo por el árbol. Si el puesto no tiene
        /// destino —los de obra no lo tienen— entra al área del solicitante, que es quien pidió a
        /// esta persona.
        /// </summary>
        /// <param name="personId">
        /// De quién es la ficha. Lo resuelve el llamador porque cada camino lo saca de otro lado: el
        /// ingreso directo, del <c>fft_person_id</c> que dejó el pedido; el cierre legado, del
        /// formulario que el candidato llenó.
        /// </param>
        private static async Task<Worker?> AbrirFichaPreIngresoAsync(
            AppDbContext ctx, int personId, GthRequerimiento req, DateTimeOffset now)
        {
            var areaSolicitante = await ctx.GthSolicitud
                .Where(s => s.GthSolicitudId == req.GthSolicitudId)
                .Select(s => s.AreaScopeId)
                .FirstOrDefaultAsync();

            // El área a la que ENTRA el candidato la decide el puesto, no quien lo pidió: la
            // Gerencia Inmobiliaria pide un INGENIERO RESIDENTE y el residente entra a Residencia.
            // Sin destino (los puestos de obra) se cae al área del solicitante.
            var areaPuesto = await ctx.Puesto
                .Where(p => p.PuestoId == req.PuestoId)
                .Select(p => p.AreaDestinoScopeId)
                .FirstOrDefaultAsync();

            var areaDestino = areaPuesto ?? areaSolicitante;

            // Una persona puede tener varias fichas (reingresos): si ya tiene una viva se reusa en
            // vez de abrir otra. Se prioriza la de pre-ingreso y, si no la hay, la más reciente.
            // La ficha que dejó Added un intento anterior de la execution strategy cuenta igual:
            // buscar solo en la BD abriría una segunda al reintentar.
            var existente = ctx.Worker.Local
                    .FirstOrDefault(w => w.PersonId == personId
                                      && w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado)
                ?? await ctx.Worker
                    .Where(w => w.PersonId == personId
                             && (w.WorkersEstadoId == WorkersEstadoIds.Activo
                              || w.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma
                              || w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado))
                    .OrderByDescending(w => w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado ? 1 : 0)
                    .ThenByDescending(w => w.Id)
                    .FirstOrDefaultAsync();

            if (existente != null)
            {
                // El área de una ficha de pre-ingreso sale siempre del requerimiento que la puso
                // ahí. La de un trabajador real es su área de verdad: solo se llena si estaba vacía,
                // para no moverlo de sitio en el árbol antes de que exista el contrato.
                var puedeReasignarArea = existente.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado
                                      || existente.AreaScopeId == null;
                if (areaDestino != null && puedeReasignarArea && existente.AreaScopeId != areaDestino)
                {
                    existente.AreaScopeId = areaDestino;
                    existente.UpdatedAt   = now;
                }
                return existente;
            }

            var ficha = new Worker
            {
                PersonId        = personId,
                WorkersEstadoId = WorkersEstadoIds.FinalistaAprobado,
                // Solo el puesto: la categoría de la ficha sale de puesto.categoria_id.
                PuestoId        = req.PuestoId,
                ContributorId   = req.ContributorId,
                AreaScopeId     = areaDestino,
                // Clasificación desde ya, no en el onboarding: es lo que hace que los correos de
                // EMO encuentren su columna en la matriz de Configuración de EMOs cuando GTH le
                // programa el examen de ingreso. Ver ClasificacionPreIngreso.
                ContrataCasa       = ClasificacionPreIngreso.ContrataCasaPropia,
                ObraOficinaStaffId = await ClasificacionPreIngreso
                    .ResolverObraOficinaStaffIdAsync(ctx, req.ProjectId),
                // Sin periodo laboral: todavía no ingresó. Se le abre uno cuando firme
                // (ver WorkersPeriodoLaboral).
                CreatedAt       = now,
                UpdatedAt       = now,
            };
            ctx.Worker.Add(ficha);
            return ficha;
        }
    }
}
