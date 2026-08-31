using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Repositories;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Services.ReclutamientoEmoIngreso.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services.ReclutamientoEmoIngreso.Services
{
    /// <inheritdoc cref="IReclutamientoEmoIngresoService"/>
    /// <remarks>
    /// Los códigos de estado y de resultado se toman de las constantes del propio repositorio de
    /// Reclutamiento (<c>EstadoReclutamiento</c>, <c>ResultadoCandidato</c>, <c>EstadoCandidato</c>)
    /// y no se copian acá: son el espejo de los catálogos de la base y tener dos listas de los
    /// mismos strings es la forma más fácil de que una se desactualice sin que nada avise.
    /// </remarks>
    public class ReclutamientoEmoIngresoService : IReclutamientoEmoIngresoService
    {
        private readonly ILogger<ReclutamientoEmoIngresoService> _logger;

        public ReclutamientoEmoIngresoService(ILogger<ReclutamientoEmoIngresoService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tipo de EMO que cierra un proceso de selección. Los demás (periódico, retiro, cambio de
        /// puesto) son de gente que ya está adentro y no tienen nada que ver con un requerimiento.
        /// </summary>
        private const string TipoEmoIngreso = "Ingreso";

        /// <summary>Aptitud "Apto" a secas.</summary>
        private const string AptitudApto = "Apto";

        /// <summary>
        /// Aptitud "Apto con Restricciones": la persona puede entrar igual. El resto del sistema ya
        /// la trata como apta (le da vigencia al EMO y aprueba el Certificado de Aptitud), así que
        /// habilita el cierre lo mismo que un "Apto" a secas — solo se distingue para que el badge
        /// del requerimiento diga cuál de las dos fue.
        /// </summary>
        private const string AptitudAptoRestricciones = "Apto con Restricciones";

        /// <summary>La única aptitud que devuelve el requerimiento a manos de GTH.</summary>
        private const string AptitudNoApto = "No Apto";

        /// <summary>
        /// Aptitud sin veredicto: la clínica derivó al candidato a interconsulta y su aptitud real
        /// se define después. El proceso queda parado en <c>EMO_OBSERVADO</c> —ni se cierra ni se
        /// puede continuar con otro— hasta que se cargue el EMO con la aptitud definitiva.
        /// </summary>
        private const string AptitudObservado = "Observado";

        /// <summary>
        /// Fases desde las que este servicio todavía puede mover el requerimiento. Fuera de ellas no
        /// se toca nada: si GTH ya retomó a otro candidato (el proceso está en long list,
        /// entrevistas o selección), una corrección tardía del EMO del que quedó fuera no puede
        /// pisar el trabajo nuevo.
        ///
        /// <c>CERRADO</c> está incluido a propósito, y es seguro porque el servicio solo actúa
        /// mientras la ficha siga siendo de pre-ingreso: un requerimiento viejo cuyo trabajador ya
        /// firmó nunca se reabre, pero una clínica que se equivocó de aptitud y la corrige antes de
        /// que la persona entre sí destraba el proceso.
        /// </summary>
        private static readonly HashSet<string> FasesQueSeMueven = new()
        {
            EstadoReclutamiento.EmoIngreso,
            EstadoReclutamiento.EmoApto,
            EstadoReclutamiento.EmoAptoRestricciones,
            EstadoReclutamiento.EmoObservado,
            EstadoReclutamiento.EmoNoApto,
            EstadoReclutamiento.Cerrado,
            EstadoReclutamiento.CerradoSinCubrir,
        };

        public async Task<bool> AplicarAptitudAsync(
            AppDbContext ctx, Worker worker, string? tipoEmoNombre, string? aptitud, int? userId)
        {
            // Los tres frenos que acotan el servicio al único caso que le toca: el examen de ingreso
            // de alguien que todavía no entró y que viene de un proceso de selección.
            if (worker.WorkersEstadoId != WorkersEstadoIds.FinalistaAprobado) return false;
            if (worker.PersonId == null) return false;
            if (!string.Equals(tipoEmoNombre?.Trim(), TipoEmoIngreso, StringComparison.OrdinalIgnoreCase))
                return false;

            var aptitudNormalizada = aptitud?.Trim() ?? string.Empty;
            bool Es(string esperada) =>
                string.Equals(aptitudNormalizada, esperada, StringComparison.OrdinalIgnoreCase);

            var noApto = Es(AptitudNoApto);
            var apto   = Es(AptitudApto) || Es(AptitudAptoRestricciones);
            // Una aptitud que no es ninguna de las cuatro (o el EMO todavía sin calificar) no mueve
            // nada: no hay veredicto que aplicarle al requerimiento.
            if (!apto && !noApto && !Es(AptitudObservado)) return false;

            var proceso = await BuscarProcesoAsync(ctx, worker.PersonId.Value);
            if (proceso == null) return false;

            // Un proceso que GTH ya cerró no vuelve atrás por un apto: reguardar el mismo examen (o
            // corregir "Apto" por "Apto con Restricciones") lo devolvería a la fase de resultado y
            // GTH tendría que cerrarlo otra vez, además de sacar al candidato de Onboarding. Un No
            // Apto o un Observado sí lo reabren: ahí la corrección cambia el veredicto y el proceso
            // efectivamente no puede seguir cerrado.
            if (proceso.FaseCodigo == EstadoReclutamiento.Cerrado && apto) return false;

            var now = DateTimeOffset.UtcNow;

            // El EMO ya NO cierra el proceso: lo deja en la fase que dice qué salió, y el cierre lo
            // confirma GTH desde el detalle del requerimiento. Sin ese paso, un candidato aparecía
            // en Onboarding en cuanto la clínica guardaba el examen, sin que GTH llegara a ver el
            // resultado ni a decidir nada.
            var destinoCodigo = noApto
                ? await FaseTrasNoAptoAsync(
                    ctx, proceso.RequerimientoId, proceso.CandidatoId, proceso.Requerimiento.EsFft)
                : Es(AptitudApto)                ? EstadoReclutamiento.EmoApto
                : Es(AptitudAptoRestricciones)   ? EstadoReclutamiento.EmoAptoRestricciones
                                                 : EstadoReclutamiento.EmoObservado;

            // El resultado del candidato solo cambia con el No Apto. Con apto u observado sigue
            // SELECCIONADO: es el que eligió el área solicitante y nadie lo descartó — un observado
            // todavía puede terminar entrando.
            var resultadoCodigo = noApto
                ? ResultadoCandidato.NoAptoEmo
                : ResultadoCandidato.Seleccionado;

            // Idempotencia: reguardar un EMO que ya se aplicó no tiene que reescribir nada (y sobre
            // todo no tiene que volver a marcar como "actualizada" una evaluación que no cambió).
            if (proceso.FaseCodigo == destinoCodigo && proceso.ResultadoCodigo == resultadoCodigo)
                return false;

            var destino = await ctx.GthEstadoRequerimiento
                .FirstOrDefaultAsync(e => e.Codigo == destinoCodigo && e.State);
            var resultadoId = await ctx.GthCandidatoResultado
                .Where(r => r.Codigo == resultadoCodigo && r.State)
                .Select(r => (int?)r.GthCandidatoResultadoId)
                .FirstOrDefaultAsync();

            if (destino == null || resultadoId == null)
            {
                // Catálogo sin sembrar: se deja el EMO como está y se avisa. Hacer fallar el
                // registro de un examen médico por esto sería peor que dejar el requerimiento donde
                // estaba, que es algo que GTH puede ver y reportar.
                _logger.LogWarning(
                    "EMO de ingreso sin aplicar al requerimiento {RequerimientoId}: falta el estado "
                    + "{Estado} o el resultado {Resultado} en el catálogo.",
                    proceso.RequerimientoId, destinoCodigo, resultadoCodigo);
                return false;
            }

            // El resultado del candidato NO toca DecisionDateTime: esa es la fecha en que el área
            // solicitante lo eligió y se conserva para que una corrección de la clínica lo devuelva
            // a SELECCIONADO tal como estaba. La fecha del rechazo por EMO la lee el historial de
            // UpdatedDateTime (ver QueryCandidatosRechazados).
            proceso.Evaluacion.GthCandidatoResultadoId = resultadoId.Value;
            proceso.Evaluacion.UpdatedDateTime         = now;
            proceso.Evaluacion.UpdatedUserId           = userId;

            proceso.Requerimiento.GthEstadoRequerimientoId = destino.GthEstadoRequerimientoId;
            proceso.Requerimiento.UpdatedDateTime          = now;
            proceso.Requerimiento.UpdatedUserId            = userId;

            _logger.LogInformation(
                "EMO de ingreso {Aptitud}: requerimiento {RequerimientoId} pasa de {Origen} a {Destino}.",
                aptitud, proceso.RequerimientoId, proceso.FaseCodigo, destinoCodigo);

            return true;
        }

        public async Task<bool> SincronizarRazonSocialAsync(
            AppDbContext ctx, Worker worker, int contributorId, int? userId)
        {
            // Solo aplica mientras la persona siga siendo de pre-ingreso: a quien ya firmó no se le
            // toca el requerimiento del proceso por el que entró, que es historia.
            if (worker.WorkersEstadoId != WorkersEstadoIds.FinalistaAprobado) return false;
            if (worker.PersonId == null) return false;

            // Solo el ingreso directo llega al EMO sin razón social (el flujo normal la exige antes
            // de publicar la vacante), y su enlace con la persona es `fft_person_id`: ese pedido no
            // llena formulario del postulante, así que no hay otro por dónde llegar.
            var req = await ctx.GthRequerimiento
                .Where(r => r.State && r.EsFft
                         && r.FftPersonId == worker.PersonId.Value
                         && r.ContributorId == null)
                .OrderByDescending(r => r.GthRequerimientoId)
                .FirstOrDefaultAsync();
            if (req == null) return false;

            req.ContributorId   = contributorId;
            req.UpdatedDateTime = DateTimeOffset.UtcNow;
            req.UpdatedUserId   = userId;

            _logger.LogInformation(
                "Razón social {ContributorId} asignada al requerimiento {RequerimientoId} desde la "
                + "programación del EMO de ingreso.", contributorId, req.GthRequerimientoId);

            return true;
        }

        /// <summary>
        /// El requerimiento del que esta persona es el resultado del proceso, junto con su
        /// evaluación y la fase en la que está. Se llega por <c>person_id</c>, y hay dos caminos
        /// porque hay dos formas de que un candidato quede enganchado a una ficha:
        ///
        /// <list type="bullet">
        ///   <item><description>el <b>formulario del postulante</b>, que escribe
        ///   <c>person_id</c> al aprobarse — es el flujo normal;</description></item>
        ///   <item><description><c>gth_requerimiento.fft_person_id</c> en el <b>ingreso
        ///   directo</b>, que no pide formulario: sus datos los declaró quien pidió la vacante y la
        ///   persona entra a <c>person</c> al registrarse el pedido.</description></item>
        /// </list>
        ///
        /// Sin el segundo camino ningún FFT nuevo cerraría su requerimiento al salir Apto: el
        /// proceso se quedaría en EMO de ingreso para siempre.
        ///
        /// Vale tanto el candidato SELECCIONADO como el que ya quedó marcado NO_APTO_EMO, para que
        /// una corrección de la aptitud pueda deshacer lo que hizo la anterior.
        /// </summary>
        private static async Task<ProcesoDelFinalista?> BuscarProcesoAsync(AppDbContext ctx, int personId)
        {
            var fases = FasesQueSeMueven.ToList();

            return await (
                from c in ctx.GthCandidato
                where c.State
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                where r.State
                      && (ctx.GthPostulanteFormulario.Any(f => f.State
                                                            && f.GthCandidatoId == c.GthCandidatoId
                                                            && f.PersonId == personId)
                          || (r.EsFft && r.FftPersonId == personId))
                join ev in ctx.GthCandidatoEvaluacion on c.GthCandidatoId equals ev.GthCandidatoId
                where ev.State
                join res in ctx.GthCandidatoResultado
                    on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                where res.Codigo == ResultadoCandidato.Seleccionado
                      || res.Codigo == ResultadoCandidato.NoAptoEmo
                join e in ctx.GthEstadoRequerimiento
                    on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                where fases.Contains(e.Codigo)
                // Una persona puede haber pasado por varios procesos: manda el más reciente.
                orderby r.GthRequerimientoId descending
                select new ProcesoDelFinalista
                {
                    RequerimientoId = r.GthRequerimientoId,
                    CandidatoId     = c.GthCandidatoId,
                    Requerimiento   = r,
                    Evaluacion      = ev,
                    FaseCodigo      = e.Codigo,
                    ResultadoCodigo = res.Codigo,
                }).FirstOrDefaultAsync();
        }

        /// <summary>
        /// A dónde va el requerimiento cuando el EMO sale No Apto: a <c>EMO_NO_APTO</c> para que GTH
        /// elija a quién de los rechazados retoma, o directo a <c>LONG_LIST</c> si no queda ninguno
        /// —sin candidatos a los que volver, la pantalla de decisión no tendría nada que ofrecer y
        /// preparar otra long list es lo único que se puede hacer—.
        ///
        /// Un ingreso directo FFT sin rechazados termina en <c>CERRADO_SIN_CUBRIR</c>: ese flujo no
        /// tiene long list (nace con su candidato puesto por el solicitante) ni candidatos previos,
        /// así que no queda nada que GTH pueda hacer con él. Mandarlo a LONG_LIST lo dejaría en una
        /// fase que su propia línea de tiempo oculta, y dejarlo en EMO_NO_APTO lo colgaría en una
        /// pantalla de decisión sin ninguna opción. Para volver a pedir esa vacante hay que
        /// registrar una solicitud nueva.
        ///
        /// Se pregunta en dos consultas simples en vez de un left join encadenado porque son las dos
        /// formas en que queda registrado un rechazo, y solo se pagan cuando un EMO sale No Apto.
        /// </summary>
        private static async Task<string> FaseTrasNoAptoAsync(
            AppDbContext ctx, int requerimientoId, int candidatoNoAptoId, bool esFft)
        {
            // Descartados por el resultado de su evaluación: formulario, entrevistas o decisión
            // final. NO_APTO_EMO queda fuera a propósito — un examen médico no se revierte
            // volviendo a elegir a la misma persona.
            var porEvaluacion = await (
                from ev in ctx.GthCandidatoEvaluacion
                where ev.State
                join c in ctx.GthCandidato on ev.GthCandidatoId equals c.GthCandidatoId
                where c.State && c.GthRequerimientoId == requerimientoId
                      && c.GthCandidatoId != candidatoNoAptoId
                join res in ctx.GthCandidatoResultado
                    on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                where res.Codigo == ResultadoCandidato.NoPaso || res.Codigo == ResultadoCandidato.Rechazado
                select c.GthCandidatoId).AnyAsync();

            if (porEvaluacion) return EstadoReclutamiento.EmoNoApto;

            // Rechazados por el solicitante al revisar la long list: esos no tienen evaluación.
            var enLongList = await (
                from c in ctx.GthCandidato
                where c.State && c.GthRequerimientoId == requerimientoId
                      && c.GthCandidatoId != candidatoNoAptoId
                join est in ctx.GthCandidatoEstado on c.GthCandidatoEstadoId equals est.GthCandidatoEstadoId
                where est.Codigo == EstadoCandidato.Rechazado
                select c.GthCandidatoId).AnyAsync();

            if (enLongList) return EstadoReclutamiento.EmoNoApto;

            return esFft ? EstadoReclutamiento.CerradoSinCubrir : EstadoReclutamiento.LongList;
        }

        /// <summary>Lo que hace falta del proceso para moverlo, en una sola consulta.</summary>
        private sealed class ProcesoDelFinalista
        {
            public int RequerimientoId { get; init; }
            public int CandidatoId { get; init; }
            public GthRequerimiento Requerimiento { get; init; } = null!;
            public GthCandidatoEvaluacion Evaluacion { get; init; } = null!;
            public string FaseCodigo { get; init; } = string.Empty;
            public string ResultadoCodigo { get; init; } = string.Empty;
        }
    }
}
