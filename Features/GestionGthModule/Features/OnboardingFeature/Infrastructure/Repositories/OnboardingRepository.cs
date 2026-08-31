using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Repositories
{
    public class OnboardingRepository : IOnboardingRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public OnboardingRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>Los timestamps se guardan en UTC y se sirven al frontend en hora de Perú.</summary>
        private static readonly TimeSpan PeruOffset = TimeSpan.FromHours(-5);

        /// <summary>
        /// Fila cruda de un onboarding con todo lo que se lee de reclutamiento. Es una clase con
        /// nombre (y no un tipo anónimo) porque la comparte la bandeja con el alta de un onboarding
        /// nuevo, que devuelve la misma fila para insertarla en la tabla sin recargar la pantalla.
        /// </summary>
        private sealed class OnboardingRawRow
        {
            public GthOnboarding Ob { get; set; } = null!;
            public string Codigo { get; set; } = string.Empty;
            public string CandidatoNombre { get; set; } = string.Empty;
            public string? PersonNombre { get; set; }
            public string? PersonEmail { get; set; }
            public string? Puesto { get; set; }
            public string? Area { get; set; }
            public string? Empresa { get; set; }
            public string? ProyectoObra { get; set; }
            public string? JefeDirecto { get; set; }
            public string FaseCodigo { get; set; } = string.Empty;
            public string FaseNombre { get; set; } = string.Empty;
            public int FaseOrden { get; set; }
            public string EstadoCodigo { get; set; } = string.Empty;
            public string EstadoNombre { get; set; } = string.Empty;
        }

        /// <summary>
        /// Onboardings vigentes con su vacante, su ficha maestra y sus catálogos, en una sola
        /// consulta. <paramref name="onboardingId"/> la acota a uno solo (alta de un ingreso nuevo).
        /// </summary>
        private static IQueryable<OnboardingRawRow> QueryOnboardings(AppDbContext ctx, int? onboardingId = null) =>
            from ob in ctx.GthOnboarding
            where ob.State && (onboardingId == null || ob.GthOnboardingId == onboardingId)
            join c in ctx.GthCandidato on ob.GthCandidatoId equals c.GthCandidatoId
            join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
            join s in ctx.GthSolicitud on r.GthSolicitudId equals s.GthSolicitudId
            join p in ctx.Puesto on r.PuestoId equals p.PuestoId
            join pr in ctx.Project on r.ProjectId equals pr.ProjectId
            join fa in ctx.GthOnboardingFase on ob.GthOnboardingFaseId equals fa.GthOnboardingFaseId
            join es in ctx.GthOnboardingEstado on ob.GthOnboardingEstadoId equals es.GthOnboardingEstadoId
                // Razón social, ficha maestra y jefe directo son todos opcionales.
            join co in ctx.Contributor on r.ContributorId equals (int?)co.ContributorId into coJoin
            from co in coJoin.DefaultIfEmpty()
            join pe in ctx.Person on ob.PersonId equals (int?)pe.PersonId into peJoin
            from pe in peJoin.DefaultIfEmpty()
            join w in ctx.Worker on s.SolicitanteWorkerId equals (int?)w.Id into wJoin
            from w in wJoin.DefaultIfEmpty()
            select new OnboardingRawRow
            {
                Ob              = ob,
                Codigo          = r.Codigo,
                CandidatoNombre = c.Nombre,
                PersonNombre    = pe == null ? null : pe.FullName,
                PersonEmail     = pe == null ? null : pe.Email,
                Puesto          = p.Nombre,
                Area            = s.AreaNombre,
                Empresa         = co == null ? null : co.ContributorName,
                ProyectoObra    = pr.ProjectDescription,
                JefeDirecto     = w == null ? null : (w.Person != null ? w.Person.FullName : w.ApellidoNombre),
                FaseCodigo      = fa.Codigo,
                FaseNombre      = fa.Nombre,
                FaseOrden       = fa.Orden,
                EstadoCodigo    = es.Codigo,
                EstadoNombre    = es.Nombre,
            };

        /// <summary>
        /// Una actividad del catálogo con lo justo para resolver si un onboarding ya la cumplió.
        /// </summary>
        private sealed record ActividadCatalogo(int FaseOrden, string Codigo, bool Automatica);

        /// <summary>
        /// Qué actividades del checklist tiene cumplidas un onboarding. Hoy NO se guarda actividad
        /// por actividad: se deduce de lo que sí está persistido en su fila, que es la única fuente
        /// de verdad. Vive acá —y no en la pantalla— para que el % de la tabla y los checks del
        /// detalle no puedan discrepar:
        ///
        ///   • Actividades de fases anteriores a la actual → hechas (para pasar de fase hubo que
        ///     cumplirlas).
        ///   • Actividades automáticas → hechas (los avisos preventivos a TI y al responsable de
        ///     obra salen al registrar la solicitud, antes de que el colaborador llegue a onboarding).
        ///   • Revisar y aprobar la carta firmada → hecha cuando la carta está aprobada.
        ///
        /// A medida que cada fase se implemente, su regla entra acá.
        /// </summary>
        private static List<string> ActividadesHechas(
            IReadOnlyList<ActividadCatalogo> checklist, GthOnboarding ob, int faseOrden) =>
            checklist
                .Where(a => a.FaseOrden < faseOrden
                         || a.Automatica
                         || (a.Codigo == ActividadOnboarding.RevisarAprobarCarta
                             && ob.CartaFirmadaAprobadaDateTime != null))
                .Select(a => a.Codigo)
                .ToList();

        /// <summary>Catálogo de actividades vigentes, ordenado por fase (para resolver el avance).</summary>
        private static Task<List<ActividadCatalogo>> LeerChecklist(AppDbContext ctx) =>
            (from a in ctx.GthOnboardingActividad
             where a.State && a.Active
             join f in ctx.GthOnboardingFase on a.GthOnboardingFaseId equals f.GthOnboardingFaseId
             where f.State && f.Active
             orderby f.Orden, a.Orden
             select new ActividadCatalogo(f.Orden, a.Codigo, a.Automatica))
            .ToListAsync();

        /// <summary>
        /// Proyecta la fila cruda al DTO de la tabla. <paramref name="checklist"/> es el catálogo de
        /// actividades vigentes: el % de avance son las actividades cumplidas sobre ese total.
        /// </summary>
        private static OnboardingListItemDto MapItem(OnboardingRawRow x, IReadOnlyList<ActividadCatalogo> checklist)
        {
            var hechas = ActividadesHechas(checklist, x.Ob, x.FaseOrden);
            return MapItem(x, hechas, checklist.Count);
        }

        private static OnboardingListItemDto MapItem(
            OnboardingRawRow x, List<string> actividadesHechas, int totalActividades) => new()
        {
            OnboardingId = x.Ob.GthOnboardingId,
            CandidatoId  = x.Ob.GthCandidatoId,
            PersonId     = x.Ob.PersonId,
            Codigo       = x.Codigo,
            // El nombre de la base maestra manda sobre el que registró GTH al cargar el CV: es el
            // que el propio postulante declaró y GTH validó al aprobar su formulario.
            Nombre       = string.IsNullOrWhiteSpace(x.PersonNombre) ? x.CandidatoNombre : x.PersonNombre!,
            Puesto       = x.Puesto,
            Area         = x.Area,
            Empresa      = x.Empresa,
            ProyectoObra = x.ProyectoObra,
            FechaIngreso = x.Ob.FechaIngreso,
            JefeDirecto  = x.JefeDirecto,
            // El correo vigente es el de la base maestra; el de la carta es el histórico del envío.
            Correo       = x.PersonEmail ?? x.Ob.CartaOfertaCorreo,
            FaseCodigo   = x.FaseCodigo,
            FaseNombre   = x.FaseNombre,
            FaseOrden    = x.FaseOrden,
            EstadoCodigo = x.EstadoCodigo,
            EstadoNombre = x.EstadoNombre,
            ActividadesHechas = actividadesHechas,
            AvancePorcentaje = totalActividades <= 0
                ? 0
                : (int)Math.Round(actividadesHechas.Count * 100d / totalActividades, MidpointRounding.AwayFromZero),
            CartaOfertaNombre    = x.Ob.CartaOfertaNombre,
            CartaOfertaUrl       = x.Ob.CartaOfertaUrl,
            CartaOfertaEnviadaEn = x.Ob.CartaOfertaEnviadaDateTime?.ToOffset(PeruOffset).DateTime,
            CartaFirmadaNombre       = x.Ob.CartaFirmadaNombre,
            CartaFirmadaUrl          = x.Ob.CartaFirmadaUrl,
            CartaFirmadaSubidaEn     = x.Ob.CartaFirmadaSubidaDateTime?.ToOffset(PeruOffset).DateTime,
            CartaFirmadaPostulanteEn = x.Ob.CartaFirmadaPostulanteDateTime?.ToOffset(PeruOffset).DateTime,
            CartaFirmadaAprobadaEn   = x.Ob.CartaFirmadaAprobadaDateTime?.ToOffset(PeruOffset).DateTime,
            FileDigitalCarpeta = x.Ob.FileDigitalRuta,
            Observacion        = x.Ob.Observacion,
            IniciadoEn         = x.Ob.CreatedDateTime.ToOffset(PeruOffset).DateTime,
        };

        public async Task<BandejaOnboardingDto> GetBandeja()
        {
            using var ctx = _factory.CreateDbContext();

            var fases = await ctx.GthOnboardingFase
                .Where(f => f.State && f.Active)
                .OrderBy(f => f.Orden)
                .Select(f => new FaseOnboardingDto
                {
                    FaseId      = f.GthOnboardingFaseId,
                    Codigo      = f.Codigo,
                    Nombre      = f.Nombre,
                    Descripcion = f.Descripcion,
                    Orden       = f.Orden,
                    // El checklist de cada fase va anidado acá para que el detalle no tenga que
                    // pedirlo por separado: es el mismo catálogo para todos los colaboradores.
                    Actividades = ctx.GthOnboardingActividad
                        .Where(a => a.State && a.Active && a.GthOnboardingFaseId == f.GthOnboardingFaseId)
                        .OrderBy(a => a.Orden)
                        .Select(a => new ActividadOnboardingDto
                        {
                            ActividadId = a.GthOnboardingActividadId,
                            Codigo      = a.Codigo,
                            Nombre      = a.Nombre,
                            Descripcion = a.Descripcion,
                            Orden       = a.Orden,
                            Automatica  = a.Automatica,
                        })
                        .ToList(),
                })
                .ToListAsync();

            var filas = await QueryOnboardings(ctx)
                .OrderByDescending(x => x.Ob.CreatedDateTime)
                .ThenByDescending(x => x.Ob.GthOnboardingId)
                .ToListAsync();

            // El catálogo de actividades se arma de las fases que ya se leyeron: es el mismo dato,
            // así que no vale un roundtrip más.
            var checklist = fases
                .SelectMany(f => f.Actividades.Select(a => new ActividadCatalogo(f.Orden, a.Codigo, a.Automatica)))
                .ToList();

            var colaboradores = filas.Select(x => MapItem(x, checklist)).ToList();

            // Conteo del embudo: cuántos colaboradores hay parados en cada fase.
            foreach (var f in fases)
                f.Total = colaboradores.Count(c => c.FaseCodigo == f.Codigo);

            var candidatos = await QueryCandidatosAptos(ctx);

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5).Date);
            var desde7Dias = DateTimeOffset.UtcNow.AddDays(-7);

            return new BandejaOnboardingDto
            {
                Fases           = fases,
                Colaboradores   = colaboradores,
                CandidatosAptos = candidatos,
                Resumen = new ResumenOnboardingDto
                {
                    IngresosDelMes = colaboradores.Count(c =>
                        c.FechaIngreso.HasValue
                        && c.FechaIngreso.Value.Year == hoy.Year
                        && c.FechaIngreso.Value.Month == hoy.Month),
                    EnProceso  = colaboradores.Count(c => c.EstadoCodigo != EstadoOnboarding.Completo),
                    Completos  = colaboradores.Count(c => c.EstadoCodigo == EstadoOnboarding.Completo),
                    ColaboradoresNuevos = filas.Count(x => x.Ob.CreatedDateTime >= desde7Dias),
                    CandidatosPorIngresar = candidatos.Count,
                },
            };
        }

        /// <summary>
        /// Candidatos que ya terminaron reclutamiento y pueden pasar a onboarding: resultado
        /// SELECCIONADO (la decisión final del área solicitante) sobre un requerimiento que quedó
        /// CERRADO, y que todavía no tienen un onboarding abierto.
        ///
        /// El correo destino de la carta oferta se resuelve acá, en la base de datos, y no se le pide
        /// a nadie: sale de <c>person.email</c> — el correo personal que GTH validó al aprobar el
        /// formulario del postulante — y, si esa ficha aún no existe, del correo que el postulante
        /// declaró (o, en última instancia, del que GTH usó para enviarle el formulario).
        /// </summary>
        private static async Task<List<CandidatoAptoDto>> QueryCandidatosAptos(AppDbContext ctx) =>
            await (
                from c in ctx.GthCandidato
                where c.State
                    // Un candidato solo puede tener un onboarding abierto.
                    && !ctx.GthOnboarding.Any(o => o.GthCandidatoId == c.GthCandidatoId && o.State)
                join ev in ctx.GthCandidatoEvaluacion.Where(x => x.State)
                    on c.GthCandidatoId equals ev.GthCandidatoId
                join res in ctx.GthCandidatoResultado on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                where res.Codigo == ResultadoCandidatoSeleccionado
                join r in ctx.GthRequerimiento.Where(x => x.State)
                    on c.GthRequerimientoId equals r.GthRequerimientoId
                join e in ctx.GthEstadoRequerimiento on r.GthEstadoRequerimientoId equals e.GthEstadoRequerimientoId
                where e.Codigo == EstadoRequerimientoCerrado
                join s in ctx.GthSolicitud on r.GthSolicitudId equals s.GthSolicitudId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                    // Razón social, formulario, ficha maestra y jefe directo son todos opcionales.
                join co in ctx.Contributor on r.ContributorId equals (int?)co.ContributorId into coJoin
                from co in coJoin.DefaultIfEmpty()
                join fo in ctx.GthPostulanteFormulario.Where(x => x.State)
                    on c.GthCandidatoId equals fo.GthCandidatoId into foJoin
                from fo in foJoin.DefaultIfEmpty()
                join w in ctx.Worker on s.SolicitanteWorkerId equals (int?)w.Id into wJoin
                from w in wJoin.DefaultIfEmpty()
                orderby r.Codigo descending
                select new CandidatoAptoDto
                {
                    CandidatoId     = c.GthCandidatoId,
                    RequerimientoId = r.GthRequerimientoId,
                    PersonId        = fo == null ? null : fo.PersonId,
                    // El nombre declarado por el propio postulante manda sobre el que registró GTH.
                    Nombre          = fo != null && fo.NombresCompletos != null ? fo.NombresCompletos : c.Nombre,
                    Codigo          = r.Codigo,
                    Puesto          = p.Nombre,
                    Area            = s.AreaNombre,
                    Empresa         = co == null ? null : co.ContributorName,
                    ProyectoObra    = pr.ProjectDescription,
                    // El correo de la base maestra manda; si esa ficha aún no existe (formulario sin
                    // aprobar), se cae al que declaró el postulante y, en última instancia, al que
                    // GTH usó para enviarle el formulario. Va como subconsulta y no como left join
                    // porque `person_id` cuelga de `fo`, que ya es un left join: encadenarlos deja la
                    // consulta a merced de cómo EF traduzca un DefaultIfEmpty sobre otro.
                    Correo = ctx.Person
                                .Where(x => x.PersonId == fo.PersonId)
                                .Select(x => x.Email)
                                .FirstOrDefault()
                             ?? fo.CorreoElectronico
                             ?? fo.CorreoEnvio,
                    // Mismo criterio y misma forma que el correo: manda el documento de la base
                    // maestra y, si esa ficha todavía no existe, el que declaró el postulante. Es
                    // lo que nombra su carpeta en el file de colaboradores.
                    Dni = ctx.Person
                             .Where(x => x.PersonId == fo.PersonId)
                             .Select(x => x.DocumentIdentityCode)
                             .FirstOrDefault()
                          ?? fo.NumeroDocumento,
                    JefeDirecto = w == null ? null : (w.Person != null ? w.Person.FullName : w.ApellidoNombre),
                    // La firma que el postulante dibuja en el enlace público se guarda en su ficha de
                    // la base maestra, así que sin ficha el envío no puede salir. Se busca igual que
                    // el correo y el documento: primero el enlace que dejó su formulario aprobado y,
                    // si no existe, por coincidencia de documento (person.document_identity_code es
                    // único), que cubre a quien ya estaba en la base maestra de antes. Va como un solo
                    // Any con el OR adentro —y no como dos Any unidos por ||— para que sea un único
                    // EXISTS en la consulta.
                    TieneFichaMaestra = ctx.Person.Any(x =>
                        x.PersonId == fo.PersonId
                        || (fo.NumeroDocumento != null && x.DocumentIdentityCode == fo.NumeroDocumento)),
                }).ToListAsync();

        public async Task<OnboardingContextoDto> PrepararInicio(int candidatoId, DateOnly? fechaIngreso, string? correo)
        {
            using var ctx = _factory.CreateDbContext();

            // Se reusa la misma consulta de aptos: así "quién puede entrar a onboarding" está escrito
            // una sola vez y el desplegable y esta validación no pueden discrepar.
            var apto = (await QueryCandidatosAptos(ctx)).FirstOrDefault(x => x.CandidatoId == candidatoId);
            if (apto == null)
                throw new AbrilException(
                    "Este candidato no puede pasar a onboarding: debe estar seleccionado en un proceso ya cerrado y no tener otro onboarding abierto.",
                    409);

            var destino = Trim(correo) ?? Trim(apto.Correo);
            if (string.IsNullOrWhiteSpace(destino))
                throw new AbrilException(
                    "El colaborador no tiene un correo personal registrado. Aprueba su formulario de postulante en Reclutamiento para que quede en la base maestra, o indica el correo a mano.",
                    409);

            // Sin documento no hay carpeta: el file del colaborador se llama «{DNI} - {NOMBRE}» y de
            // ese nombre dependen tanto encontrar su file como los permisos que se le dan encima. Se
            // corta acá, antes de enviarle la carta, y no se inventa un nombre alterno.
            var dni = Trim(apto.Dni);
            if (string.IsNullOrWhiteSpace(dni))
                throw new AbrilException(
                    "El colaborador no tiene documento de identidad registrado. Aprueba su formulario de postulante en Reclutamiento para que quede en la base maestra.",
                    409);

            // La ficha de la base maestra es obligatoria en este flujo: la firma que el postulante
            // dibuja en el enlace público se guarda en `person`, así que sin ficha no habría dónde
            // ponerla y el enlace llegaría a una página que no puede terminar. Se corta acá, antes de
            // enviar nada. Mismo orden de búsqueda que el correo y el documento: el enlace del
            // formulario aprobado y, si falta, la coincidencia por documento.
            var personId = apto.PersonId
                ?? await ctx.Person
                    .Where(x => x.DocumentIdentityCode == dni)
                    .Select(x => (int?)x.PersonId)
                    .FirstOrDefaultAsync();

            if (personId == null)
                throw new AbrilException(
                    "El colaborador no tiene ficha en la base maestra y su firma se guarda ahí. Aprueba su formulario de postulante en Reclutamiento para crearla y vuelve a intentarlo.",
                    409);

            return new OnboardingContextoDto
            {
                CandidatoId     = apto.CandidatoId,
                RequerimientoId = apto.RequerimientoId,
                PersonId        = personId.Value,
                Codigo          = apto.Codigo,
                Nombre          = apto.Nombre,
                Dni             = dni,
                Puesto          = apto.Puesto,
                Area            = apto.Area,
                Empresa         = apto.Empresa,
                ProyectoObra    = apto.ProyectoObra,
                Correo          = destino.ToLowerInvariant(),
                // La fecha la pacta GTH en el modal: el requerimiento ya no trae ninguna propuesta.
                FechaIngreso    = fechaIngreso,
                JefeDirecto     = apto.JefeDirecto,
            };
        }

        public async Task<OnboardingListItemDto> Crear(
            OnboardingContextoDto contexto,
            CartaOfertaPersistDto carta,
            FileDigitalCarpetaDto carpeta,
            string? observacion,
            int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var fase = await ctx.GthOnboardingFase
                .FirstOrDefaultAsync(f => f.Codigo == FaseOnboarding.CartaOfertaFirmada && f.State)
                ?? throw new AbrilException(
                    $"No está configurada la fase {FaseOnboarding.CartaOfertaFirmada} del onboarding.", 500);

            var estado = await ctx.GthOnboardingEstado
                .FirstOrDefaultAsync(e => e.Codigo == EstadoOnboarding.CartaEnviada && e.State)
                ?? throw new AbrilException(
                    $"No está configurado el estado {EstadoOnboarding.CartaEnviada} del onboarding.", 500);

            var now = DateTimeOffset.UtcNow;
            var ob = new GthOnboarding
            {
                GthCandidatoId        = contexto.CandidatoId,
                PersonId              = contexto.PersonId,
                GthOnboardingFaseId   = fase.GthOnboardingFaseId,
                GthOnboardingEstadoId = estado.GthOnboardingEstadoId,
                FechaIngreso          = contexto.FechaIngreso,
                CartaOfertaNombre     = carta.Nombre,
                CartaOfertaUrl        = carta.Url,
                CartaOfertaItemId     = carta.ItemId,
                CartaOfertaDriveId    = carta.DriveId,
                CartaOfertaCorreo     = contexto.Correo,
                CartaOfertaToken      = contexto.Token,
                CartaOfertaEnviadaDateTime = now,
                CartaOfertaEnviadaUserId   = userId,
                FileDigitalDriveId    = carpeta.DriveId,
                FileDigitalItemId     = carpeta.ItemId,
                FileDigitalRuta       = carpeta.Ruta,
                Observacion           = Trim(observacion),
                CreatedDateTime       = now,
                CreatedUserId         = userId,
            };

            ctx.GthOnboarding.Add(ob);
            await ctx.SaveChangesAsync();

            var checklist = await LeerChecklist(ctx);
            var fila = await QueryOnboardings(ctx, ob.GthOnboardingId).FirstOrDefaultAsync();

            // La fila se acaba de insertar en esta misma conexión, así que siempre está; el fallback
            // existe para no devolver null desde un método que promete una fila.
            return fila != null
                ? MapItem(fila, checklist)
                : new OnboardingListItemDto
                {
                    OnboardingId = ob.GthOnboardingId,
                    CandidatoId  = contexto.CandidatoId,
                    PersonId     = contexto.PersonId,
                    Codigo       = contexto.Codigo,
                    Nombre       = contexto.Nombre,
                    Puesto       = contexto.Puesto,
                    Area         = contexto.Area,
                    Empresa      = contexto.Empresa,
                    ProyectoObra = contexto.ProyectoObra,
                    FechaIngreso = contexto.FechaIngreso,
                    JefeDirecto  = contexto.JefeDirecto,
                    Correo       = contexto.Correo,
                    FaseCodigo   = fase.Codigo,
                    FaseNombre   = fase.Nombre,
                    FaseOrden    = fase.Orden,
                    EstadoCodigo = estado.Codigo,
                    EstadoNombre = estado.Nombre,
                    CartaOfertaNombre    = carta.Nombre,
                    CartaOfertaUrl       = carta.Url,
                    CartaOfertaEnviadaEn = now.ToOffset(PeruOffset).DateTime,
                    FileDigitalCarpeta   = carpeta.Ruta,
                    Observacion          = Trim(observacion),
                    IniciadoEn           = now.ToOffset(PeruOffset).DateTime,
                };
        }

        // ── Reenvío del enlace de firma ────────────────────────────────────────

        public async Task<OnboardingContextoDto> PrepararReenvio(
            int onboardingId, string? correo, string tokenSiFalta)
        {
            using var ctx = _factory.CreateDbContext();

            var fila = await QueryOnboardings(ctx, onboardingId).FirstOrDefaultAsync()
                ?? throw new AbrilException("El onboarding indicado no existe o fue dado de baja.", 404);

            var ob = fila.Ob;

            // Sin carta subida el enlace llega a una página sin nada que mostrar.
            if (string.IsNullOrWhiteSpace(ob.CartaOfertaUrl))
                throw new AbrilException(
                    "Este onboarding no tiene una carta oferta cargada, así que no hay nada que el colaborador pueda ver ni firmar.",
                    409);

            // Ya firmada: el enlace abriría una página de solo lectura y el correo le pediría firmar
            // algo que ya firmó. Si hay que rehacerla, GTH reemplaza la carta firmada desde el detalle.
            if (ob.CartaFirmadaAprobadaDateTime != null)
                throw new AbrilException(
                    "La carta oferta firmada ya fue aprobada: el proceso de firma está cerrado.", 409);
            if (!string.IsNullOrWhiteSpace(ob.CartaFirmadaUrl))
                throw new AbrilException(
                    "El colaborador ya devolvió su carta oferta firmada; no hace falta reenviarle el enlace.", 409);

            var destino = Trim(correo) ?? Trim(fila.PersonEmail) ?? Trim(ob.CartaOfertaCorreo);
            if (string.IsNullOrWhiteSpace(destino))
                throw new AbrilException(
                    "El colaborador no tiene un correo personal registrado. Complétalo en su ficha de la base maestra o indícalo a mano.",
                    409);

            // El documento y la ficha se resuelven igual que al abrir el onboarding: esto también es
            // la vía por la que un onboarding abierto ANTES de la firma en línea entra al flujo nuevo,
            // porque su fila no tiene ni token ni person_id.
            var dni = await ctx.Person
                          .Where(x => x.PersonId == ob.PersonId)
                          .Select(x => x.DocumentIdentityCode)
                          .FirstOrDefaultAsync()
                      ?? await ctx.GthPostulanteFormulario
                          .Where(f => f.State && f.GthCandidatoId == ob.GthCandidatoId)
                          .Select(f => f.NumeroDocumento)
                          .FirstOrDefaultAsync();

            var personId = ob.PersonId
                ?? (string.IsNullOrWhiteSpace(dni)
                    ? null
                    : await ctx.Person
                        .Where(x => x.DocumentIdentityCode == dni)
                        .Select(x => (int?)x.PersonId)
                        .FirstOrDefaultAsync());

            if (personId == null)
                throw new AbrilException(
                    "El colaborador no tiene ficha en la base maestra y su firma se guarda ahí. Aprueba su formulario de postulante en Reclutamiento para crearla y vuelve a intentarlo.",
                    409);

            return new OnboardingContextoDto
            {
                CandidatoId     = ob.GthCandidatoId,
                RequerimientoId = 0, // no se usa al reenviar: la vacante ya está atada al onboarding
                PersonId        = personId.Value,
                Codigo          = fila.Codigo,
                Nombre          = string.IsNullOrWhiteSpace(fila.PersonNombre)
                    ? fila.CandidatoNombre : fila.PersonNombre!,
                Token           = Trim(ob.CartaOfertaToken) ?? tokenSiFalta,
                Dni             = dni ?? string.Empty,
                Puesto          = fila.Puesto,
                Area            = fila.Area,
                Empresa         = fila.Empresa,
                ProyectoObra    = fila.ProyectoObra,
                Correo          = destino.ToLowerInvariant(),
                FechaIngreso    = ob.FechaIngreso,
                JefeDirecto     = fila.JefeDirecto,
            };
        }

        public async Task<OnboardingListItemDto> MarcarEnlaceEnviado(
            int onboardingId, OnboardingContextoDto contexto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var ob = await ctx.GthOnboarding.FirstOrDefaultAsync(o => o.GthOnboardingId == onboardingId && o.State)
                ?? throw new AbrilException("El onboarding indicado no existe o fue dado de baja.", 404);

            var now = DateTimeOffset.UtcNow;

            // Se escribe todo junto y recién después de que el correo salió: el token y la ficha
            // quedan guardados solo si el colaborador realmente recibió el enlace que los usa.
            ob.CartaOfertaToken   = contexto.Token;
            ob.PersonId         ??= contexto.PersonId;
            ob.CartaOfertaCorreo  = contexto.Correo;
            ob.CartaOfertaEnviadaDateTime = now;
            ob.CartaOfertaEnviadaUserId   = userId;
            ob.UpdatedDateTime = now;
            ob.UpdatedUserId   = userId;

            await ctx.SaveChangesAsync();

            return await LeerItem(ctx, onboardingId);
        }

        public async Task<CartaOfertaFolderDto?> GetCartaOfertaFolder()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.GthCartaOfertaFolder
                .Where(f => f.State && f.Active)
                .OrderBy(f => f.GthCartaOfertaFolderId)
                .Select(f => new CartaOfertaFolderDto { LinkUrl = f.LinkUrl, FolderName = f.FolderName })
                .FirstOrDefaultAsync();
        }

        // ── Carta oferta firmada ───────────────────────────────────────────────

        public async Task<OnboardingDocumentoContextoDto> PrepararDocumento(int onboardingId)
        {
            using var ctx = _factory.CreateDbContext();

            var fila = await (
                from ob in ctx.GthOnboarding
                where ob.State && ob.GthOnboardingId == onboardingId
                join c in ctx.GthCandidato on ob.GthCandidatoId equals c.GthCandidatoId
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join fa in ctx.GthOnboardingFase on ob.GthOnboardingFaseId equals fa.GthOnboardingFaseId
                select new
                {
                    Ob         = ob,
                    r.Codigo,
                    CandidatoNombre = c.Nombre,
                    // El nombre con el que se armó la carpeta al enviar la carta oferta: el que
                    // declaró el postulante en su formulario, y si no hay formulario el que registró
                    // GTH. NO es el de la base maestra: ese puede haber cambiado después del envío y
                    // llevaría la carta firmada a una carpeta distinta.
                    FormularioNombre = ctx.GthPostulanteFormulario
                        .Where(f => f.State && f.GthCandidatoId == c.GthCandidatoId)
                        .Select(f => f.NombresCompletos)
                        .FirstOrDefault(),
                    // Mismo orden que al abrir el onboarding (base maestra y, si no hay ficha, el
                    // formulario) para que la carpeta que se rearme sea la misma. Solo se usa en los
                    // onboardings que no la tienen persistida.
                    Dni = ctx.Person
                        .Where(x => x.PersonId == ob.PersonId)
                        .Select(x => x.DocumentIdentityCode)
                        .FirstOrDefault()
                        ?? ctx.GthPostulanteFormulario
                            .Where(f => f.State && f.GthCandidatoId == c.GthCandidatoId)
                            .Select(f => f.NumeroDocumento)
                            .FirstOrDefault(),
                    FaseCodigo = fa.Codigo,
                })
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("El onboarding indicado no existe o fue dado de baja.", 404);

            return new OnboardingDocumentoContextoDto
            {
                OnboardingId = fila.Ob.GthOnboardingId,
                Codigo       = fila.Codigo,
                Nombre       = string.IsNullOrWhiteSpace(fila.FormularioNombre)
                    ? fila.CandidatoNombre : fila.FormularioNombre!,
                Dni          = fila.Dni ?? string.Empty,
                FaseCodigo   = fila.FaseCodigo,
                Carpeta      = string.IsNullOrWhiteSpace(fila.Ob.FileDigitalDriveId)
                            || string.IsNullOrWhiteSpace(fila.Ob.FileDigitalItemId)
                    ? null
                    : new FileDigitalCarpetaDto
                    {
                        DriveId = fila.Ob.FileDigitalDriveId!,
                        ItemId  = fila.Ob.FileDigitalItemId!,
                        Ruta    = fila.Ob.FileDigitalRuta,
                    },
            };
        }

        /// <summary>
        /// Guarda la carta firmada recién subida. Reemplazarla borra la aprobación anterior: si
        /// entró un documento nuevo, lo que GTH aprobó ya no es lo que está adjunto.
        /// </summary>
        public async Task<OnboardingListItemDto> GuardarCartaFirmada(
            int onboardingId, CartaOfertaPersistDto carta, FileDigitalCarpetaDto? carpeta, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var ob = await ctx.GthOnboarding.FirstOrDefaultAsync(o => o.GthOnboardingId == onboardingId && o.State)
                ?? throw new AbrilException("El onboarding indicado no existe o fue dado de baja.", 404);

            var now = DateTimeOffset.UtcNow;
            ob.CartaFirmadaNombre           = carta.Nombre;
            ob.CartaFirmadaUrl              = carta.Url;
            ob.CartaFirmadaItemId           = carta.ItemId;
            ob.CartaFirmadaDriveId          = carta.DriveId;
            ob.CartaFirmadaSubidaDateTime   = now;
            ob.CartaFirmadaSubidaUserId     = userId;
            ob.CartaFirmadaAprobadaDateTime = null;
            ob.CartaFirmadaAprobadaUserId   = null;

            // Onboardings abiertos antes de que se persistiera la carpeta: se completa con la que
            // acaba de resolver el servicio, así el resto de documentos ya no la vuelve a derivar.
            if (carpeta != null && string.IsNullOrWhiteSpace(ob.FileDigitalItemId))
            {
                ob.FileDigitalDriveId = carpeta.DriveId;
                ob.FileDigitalItemId  = carpeta.ItemId;
                ob.FileDigitalRuta    = carpeta.Ruta;
            }

            ob.UpdatedDateTime = now;
            ob.UpdatedUserId   = userId;
            await ctx.SaveChangesAsync();

            return await LeerItem(ctx, onboardingId);
        }

        public async Task<OnboardingListItemDto> AprobarCartaFirmada(int onboardingId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var ob = await ctx.GthOnboarding.FirstOrDefaultAsync(o => o.GthOnboardingId == onboardingId && o.State)
                ?? throw new AbrilException("El onboarding indicado no existe o fue dado de baja.", 404);

            if (string.IsNullOrWhiteSpace(ob.CartaFirmadaUrl))
                throw new AbrilException("Primero adjunta la carta oferta firmada por el colaborador.", 409);

            if (ob.CartaFirmadaAprobadaDateTime == null)
            {
                var now = DateTimeOffset.UtcNow;
                ob.CartaFirmadaAprobadaDateTime = now;
                ob.CartaFirmadaAprobadaUserId   = userId;
                ob.UpdatedDateTime = now;
                ob.UpdatedUserId   = userId;
                await ctx.SaveChangesAsync();
            }

            return await LeerItem(ctx, onboardingId);
        }

        // ── Avance de fase ─────────────────────────────────────────────────────

        /// <summary>
        /// Mueve el onboarding a la fase siguiente del catálogo. Cada fase decide qué la habilita a
        /// avanzar; hoy solo está implementada la primera (carta firmada adjunta y aprobada), así
        /// que desde el resto se corta con un mensaje explícito en vez de avanzar en falso.
        /// </summary>
        public async Task<OnboardingListItemDto> Avanzar(int onboardingId, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var ob = await ctx.GthOnboarding.FirstOrDefaultAsync(o => o.GthOnboardingId == onboardingId && o.State)
                ?? throw new AbrilException("El onboarding indicado no existe o fue dado de baja.", 404);

            var fases = await ctx.GthOnboardingFase
                .Where(f => f.State && f.Active)
                .OrderBy(f => f.Orden)
                .ToListAsync();

            var actual = fases.FirstOrDefault(f => f.GthOnboardingFaseId == ob.GthOnboardingFaseId)
                ?? throw new AbrilException("La fase actual del onboarding ya no está vigente en el catálogo.", 409);

            if (actual.Codigo != FaseOnboarding.CartaOfertaFirmada)
                throw new AbrilException(
                    $"La fase «{actual.Nombre}» todavía no está habilitada para avanzar desde el sistema.", 409);

            if (string.IsNullOrWhiteSpace(ob.CartaFirmadaUrl))
                throw new AbrilException("Adjunta la carta oferta firmada antes de continuar.", 409);
            if (ob.CartaFirmadaAprobadaDateTime == null)
                throw new AbrilException("Aprueba la carta oferta firmada antes de continuar.", 409);

            var siguiente = fases.FirstOrDefault(f => f.Orden > actual.Orden)
                ?? throw new AbrilException("El onboarding ya está en la última fase del catálogo.", 409);

            var estado = await ctx.GthOnboardingEstado
                .FirstOrDefaultAsync(e => e.Codigo == EstadoOnboarding.EnProceso && e.State)
                ?? throw new AbrilException(
                    $"No está configurado el estado {EstadoOnboarding.EnProceso} del onboarding.", 500);

            var now = DateTimeOffset.UtcNow;
            ob.GthOnboardingFaseId   = siguiente.GthOnboardingFaseId;
            ob.GthOnboardingEstadoId = estado.GthOnboardingEstadoId;
            ob.UpdatedDateTime       = now;
            ob.UpdatedUserId         = userId;
            await ctx.SaveChangesAsync();

            return await LeerItem(ctx, onboardingId);
        }

        /// <summary>Relee la fila ya escrita para devolverla al frontend con sus catálogos resueltos.</summary>
        private static async Task<OnboardingListItemDto> LeerItem(AppDbContext ctx, int onboardingId)
        {
            var checklist = await LeerChecklist(ctx);
            var fila = await QueryOnboardings(ctx, onboardingId).FirstOrDefaultAsync()
                ?? throw new AbrilException("No se pudo releer el onboarding actualizado.", 500);
            return MapItem(fila, checklist);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        /// <summary>Espejo de <c>gth_candidato_resultado.codigo</c> para el seleccionado (dueño del puesto).</summary>
        private const string ResultadoCandidatoSeleccionado = "SELECCIONADO";

        /// <summary>Espejo de <c>gth_estado_requerimiento.codigo</c> para el proceso ya cerrado.</summary>
        private const string EstadoRequerimientoCerrado = "CERRADO";
    }

    /// <summary>Códigos estables de las fases del onboarding (espejo de <c>gth_onboarding_fase.codigo</c>).</summary>
    public static class FaseOnboarding
    {
        public const string CartaOfertaFirmada = "CARTA_OFERTA_FIRMADA";
        public const string FileDigital        = "FILE_DIGITAL";
        public const string CorreoBienvenida   = "CORREO_BIENVENIDA";
        public const string FormularioWeb      = "FORMULARIO_WEB";
        public const string Preinicio          = "PREINICIO";
        public const string CierreOnboarding   = "CIERRE_ONBOARDING";
        public const string BaseMaestra        = "BASE_MAESTRA";
    }

    /// <summary>
    /// Códigos estables de las actividades del checklist (espejo de
    /// <c>gth_onboarding_actividad.codigo</c>). Solo se declaran las que el código consulta; el
    /// resto del catálogo se sirve tal cual a la pantalla.
    /// </summary>
    public static class ActividadOnboarding
    {
        public const string RevisarAprobarCarta = "REVISAR_APROBAR_CARTA";
    }

    /// <summary>Códigos estables del estado del onboarding (espejo de <c>gth_onboarding_estado.codigo</c>).</summary>
    public static class EstadoOnboarding
    {
        public const string CartaEnviada = "CARTA_ENVIADA";
        public const string EnProceso    = "EN_PROCESO";
        public const string Completo     = "COMPLETO";
    }
}
