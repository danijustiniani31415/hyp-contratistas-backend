using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Repositories
{
    /// <summary>
    /// Aprobación de la solicitud de personal en sus dos niveles (gerente del área y Gerencia
    /// General). La pantalla «Aprobaciones» se sirve en dos roundtrips (cabeceras + vacantes de
    /// todas ellas) y el detalle de una aprobación en uno solo (cabecera + sus vacantes): el
    /// gerente entra desde el correo a decidir, no a navegar.
    ///
    /// Todo lo que sale de acá está filtrado por el <see cref="AprobacionScope"/> del usuario: el
    /// Gerente General ve todas las solicitudes, un gerente de área solo las de su
    /// <c>area_scope</c> hacia abajo, y cualquier otra categoría no ve ninguna.
    /// </summary>
    public class AprobacionGgRepository : IAprobacionGgRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AprobacionGgRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>Los timestamps se guardan en UTC y se sirven al frontend en hora de Perú.</summary>
        private static readonly TimeSpan PeruOffset = TimeSpan.FromHours(-5);

        public async Task<AprobacionGgEnvioContextoDto> PrepararEnvio(int solicitudId, string nuevoToken, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var solicitud = await ctx.GthSolicitud
                .FirstOrDefaultAsync(s => s.GthSolicitudId == solicitudId && s.State);
            if (solicitud == null)
                throw new AbrilException("Solicitud de personal no encontrada.", 404);

            // Idempotente: si ya hay una aprobación vigente se reutiliza (mismo token, para que el
            // enlace que ya viajó por correo siga funcionando).
            var aprobacion = await ctx.GthAprobacionGg
                .FirstOrDefaultAsync(a => a.GthSolicitudId == solicitudId && a.State);

            var now = DateTimeOffset.UtcNow;
            var esNueva = aprobacion == null;

            if (aprobacion == null)
            {
                // Las tres casillas nacen pendientes, decida quien decida después: cuál de ellas
                // cuenta lo deciden los tipos de las vacantes, y eso se evalúa al leer. Una
                // solicitud sin reemplazos deja las de área y GTH en pendiente para siempre, y eso
                // no significa nada — la pantalla ni las muestra.
                var pendienteId = await ResolveEstadoId(ctx, AprobacionGgEstadoCodigo.Pendiente);
                aprobacion = new GthAprobacionGg
                {
                    GthSolicitudId         = solicitudId,
                    Token                  = nuevoToken,
                    EstadoGerenteGeneralId = pendienteId,
                    EstadoGerenteAreaId    = pendienteId,
                    EstadoGthId            = pendienteId,
                    CreatedDateTime        = now,
                    CreatedUserId          = userId,
                    Active                 = true,
                    State                  = true,
                };
                ctx.GthAprobacionGg.Add(aprobacion);
                await ctx.SaveChangesAsync();
            }

            // Detalle: una fila por vacante vigente que ALGUIEN tenga que firmar (las que falten se
            // agregan). Los ingresos directos quedan fuera: no los aprueba nadie, ya nacieron en
            // manos de GTH, y darles fila los volvería a meter en la pantalla «Aprobaciones» de una
            // solicitud mixta. Que un FFT tenga fila es justamente lo que distingue a los que
            // quedaron del flujo viejo (ver RutaAprobacion.De).
            var requerimientoIds = await ctx.GthRequerimiento
                .Where(r => r.GthSolicitudId == solicitudId && r.State && !r.EsFft)
                .Select(r => r.GthRequerimientoId)
                .ToListAsync();

            // En una aprobación recién creada no hay detalle que consultar (el caso normal al
            // registrar la solicitud); solo se revisa cuando se reutiliza una ya existente.
            var yaEnDetalle = esNueva
                ? new List<int>()
                : await ctx.GthAprobacionGgDetalle
                    .Where(d => d.GthAprobacionGgId == aprobacion.GthAprobacionGgId && d.State)
                    .Select(d => d.GthRequerimientoId)
                    .ToListAsync();

            var faltantes = requerimientoIds.Except(yaEnDetalle).ToList();
            if (faltantes.Count > 0)
            {
                foreach (var reqId in faltantes)
                {
                    ctx.GthAprobacionGgDetalle.Add(new GthAprobacionGgDetalle
                    {
                        GthAprobacionGgId      = aprobacion.GthAprobacionGgId,
                        GthRequerimientoId     = reqId,
                        AprobadoGerenteGeneral = null,
                        AprobadoGerenteArea    = null,
                        CreatedDateTime        = now,
                        CreatedUserId          = userId,
                        Active                 = true,
                        State                  = true,
                    });
                }
                await ctx.SaveChangesAsync();
            }

            return await BuildEnvioContexto(ctx, aprobacion, solicitud);
        }

        public async Task<AprobacionGgEnvioContextoDto?> GetEnvioContextoByRequerimiento(
            int requerimientoId, SolicitudPersonalScope scope)
        {
            using var ctx = _factory.CreateDbContext();

            // Scope: el mismo de «Solicitud de Personal» —el área del solicitante, más lo que el
            // usuario registró él mismo—, porque el requerimiento es del área y su jefatura tiene
            // que poder recordarle la firma a quien la debe aunque quien lo pidió ya no esté. Se
            // traen las dos entidades (aprobación y solicitud) en un roundtrip; EF las rastrea
            // aunque vengan dentro de un anónimo.
            var areaIds = scope.AreaScopeIds.ToList();
            var par = await (
                from r in ctx.GthRequerimiento
                where r.GthRequerimientoId == requerimientoId
                      && r.State && r.Solicitud!.State
                      && (scope.VeTodo
                          || r.Solicitud.SolicitanteUserId == scope.UserId
                          || (r.Solicitud.AreaScopeId != null
                              && areaIds.Contains(r.Solicitud.AreaScopeId.Value)))
                join a in ctx.GthAprobacionGg on r.GthSolicitudId equals a.GthSolicitudId
                where a.State
                select new { Aprobacion = a, Solicitud = r.Solicitud! }).FirstOrDefaultAsync();

            if (par == null) return null;

            return await BuildEnvioContexto(ctx, par.Aprobacion, par.Solicitud);
        }

        /// <summary>Cabecera de la solicitud + vacantes con su decisión, para el correo a los gerentes.</summary>
        private static async Task<AprobacionGgEnvioContextoDto> BuildEnvioContexto(
            AppDbContext ctx, GthAprobacionGg aprobacion, GthSolicitud solicitud)
        {
            var solicitanteNombre = solicitud.SolicitanteUserId.HasValue
                ? await ctx.Worker
                    .Where(w => w.Person != null && w.Person.UserId == solicitud.SolicitanteUserId.Value)
                    .Select(w => w.Person!.FullName ?? w.ApellidoNombre)
                    .FirstOrDefaultAsync()
                : null;

            return new AprobacionGgEnvioContextoDto
            {
                SolicitudId       = aprobacion.GthSolicitudId,
                AprobacionId      = aprobacion.GthAprobacionGgId,
                Area              = solicitud.AreaNombre,
                AreaScopeId       = solicitud.AreaScopeId,
                SolicitanteNombre = solicitanteNombre,
                Justificacion     = solicitud.Justificacion,
                SustentoNombre    = solicitud.SustentoNombre,
                SustentoUrl       = solicitud.SustentoUrl,
                // La fecha de decisión y el estado se escriben juntos, así que basta con la fecha
                // para saber si esa casilla ya cerró (evita tres joins solo para leer códigos de
                // estado). Las tres viajan porque cada ruta se cierra por su lado: una solicitud
                // mixta puede tener los nuevos ya aprobados y los reemplazos todavía esperando.
                DecididaGg          = aprobacion.GerenteGeneralDecididoDateTime.HasValue,
                DecididaGerenteArea = aprobacion.GerenteAreaDecididoDateTime.HasValue,
                DecididaGth         = aprobacion.GthDecididoDateTime.HasValue,
                Vacantes          = await QueryVacantes(ctx, aprobacion.GthAprobacionGgId, aprobacion.GthSolicitudId),
            };
        }

        /// <summary>
        /// Vacantes vigentes de la solicitud con la decisión de cada nivel registrada en el detalle
        /// (left join: una vacante sin fila de detalle queda como "sin decidir" en ambos).
        /// </summary>
        private static async Task<List<AprobacionGgVacanteDto>> QueryVacantes(
            AppDbContext ctx, int aprobacionId, int solicitudId)
        {
            return await (
                from r in ctx.GthRequerimiento
                where r.GthSolicitudId == solicitudId && r.State
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join t in ctx.GthTipoRequerimiento on r.GthTipoRequerimientoId equals t.GthTipoRequerimientoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                join d in ctx.GthAprobacionGgDetalle.Where(x => x.GthAprobacionGgId == aprobacionId && x.State)
                    on r.GthRequerimientoId equals d.GthRequerimientoId into detalleJoin
                from d in detalleJoin.DefaultIfEmpty()
                // Trabajador reemplazado: left join porque solo lo tienen las vacantes de tipo
                // Reemplazo registradas desde que se pide ese dato.
                join wr in ctx.Worker on r.ReemplazaWorkerId equals (int?)wr.Id into reemplazaJoin
                from wr in reemplazaJoin.DefaultIfEmpty()
                // Tipo del documento del candidato FFT: left join porque solo lo tienen las vacantes
                // FFT registradas desde que la casilla ofrece el desplegable.
                join td in ctx.GthTipoDocumento on r.GthTipoDocumentoId equals (int?)td.GthTipoDocumentoId into tipoDocJoin
                from td in tipoDocJoin.DefaultIfEmpty()
                orderby r.GthRequerimientoId
                select new AprobacionGgVacanteDto
                {
                    RequerimientoId        = r.GthRequerimientoId,
                    Codigo                 = r.Codigo,
                    Puesto                 = p.Nombre,
                    TipoRequerimiento      = t.Nombre,
                    // El código además del nombre: es lo que decide la ruta de aprobación de la
                    // vacante (ver AprobacionGgVacanteDto.Ruta).
                    TipoRequerimientoCodigo = t.Codigo,
                    TrabajadorReemplazado  = wr == null ? null
                        : (wr.Person != null ? wr.Person.FullName : wr.ApellidoNombre),
                    ProyectoObra           = pr.ProjectDescription,
                    SalarioBrutoMensual    = r.SalarioBrutoMensual,
                    EsFft                  = r.EsFft,
                    FftCandidatoNombre     = r.FftCandidatoNombre,
                    FftCandidatoDocumento  = r.FftCandidatoDocumento,
                    FftTipoDocumento       = td != null ? td.Nombre : null,
                    FftCandidatoCorreo     = r.FftCandidatoCorreo,
                    // Tener fila de detalle es lo único que mete a un FFT en la aprobación, y solo
                    // los de antes del salto la tienen (ver RutaAprobacion.De).
                    FftEnAprobacionLegada  = d != null,
                    AprobadoGerenteArea    = d != null ? d.AprobadoGerenteArea : null,
                    AprobadoGerenteGeneral = d != null ? d.AprobadoGerenteGeneral : null,
                    AprobadoGth            = d != null ? d.AprobadoGth : null,
                }).ToListAsync();
        }

        /// <summary>
        /// Cabecera de la solicitud + sus vacantes SIN pasar por una aprobación: es lo que necesita
        /// el correo del ingreso directo FFT, que no la crea (a un FFT no lo aprueba nadie).
        /// <c>AprobacionId</c> queda en 0 y las decisiones en null, que es exactamente lo que
        /// corresponde: nadie decidió nada porque no había nada que decidir. Quien llama se queda
        /// con <c>VacantesFft</c>: en una solicitud mixta las demás sí están esperando una firma.
        /// </summary>
        public async Task<AprobacionGgEnvioContextoDto?> GetContextoSinAprobacion(int solicitudId)
        {
            using var ctx = _factory.CreateDbContext();

            var solicitud = await ctx.GthSolicitud.AsNoTracking()
                .FirstOrDefaultAsync(s => s.GthSolicitudId == solicitudId && s.State);
            if (solicitud == null) return null;

            var solicitanteNombre = solicitud.SolicitanteUserId.HasValue
                ? await ctx.Worker.AsNoTracking()
                    .Where(w => w.Person != null && w.Person.UserId == solicitud.SolicitanteUserId.Value)
                    .Select(w => w.Person!.FullName ?? w.ApellidoNombre)
                    .FirstOrDefaultAsync()
                : null;

            return new AprobacionGgEnvioContextoDto
            {
                SolicitudId       = solicitudId,
                AprobacionId      = 0,
                Area              = solicitud.AreaNombre,
                AreaScopeId       = solicitud.AreaScopeId,
                SolicitanteNombre = solicitanteNombre,
                Justificacion     = solicitud.Justificacion,
                SustentoNombre    = solicitud.SustentoNombre,
                SustentoUrl       = solicitud.SustentoUrl,
                Vacantes          = await QueryVacantes(ctx, aprobacionId: 0, solicitudId),
            };
        }

        public async Task RegistrarEnvio(int aprobacionId, List<string> principales, List<string> copias, bool esReenvio, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var aprobacion = await ctx.GthAprobacionGg
                .FirstOrDefaultAsync(a => a.GthAprobacionGgId == aprobacionId && a.State);
            if (aprobacion == null) return; // la aprobación se dio de baja mientras se enviaba el correo

            var now = DateTimeOffset.UtcNow;
            aprobacion.CorreoEnvio = principales.Count > 0 ? string.Join("; ", principales) : null;
            aprobacion.CorreoCopia = copias.Count > 0 ? string.Join("; ", copias) : null;

            if (esReenvio) aprobacion.ReenviadoDateTime = now;
            // El primer envío exitoso marca enviado_date_time; los reenvíos no lo mueven.
            aprobacion.EnviadoDateTime ??= now;

            aprobacion.UpdatedDateTime = now;
            aprobacion.UpdatedUserId   = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task<AprobacionGgBandejaDto> GetBandeja(AprobacionScope scope)
        {
            var bandeja = new AprobacionGgBandejaDto
            {
                Nivel       = scope.Nivel,
                AreaAlcance = scope.AreaNombre,
            };

            // Sin nivel no hay nada que mostrar: la pantalla se abre por rol, pero las solicitudes
            // se ven por categoría de trabajador. Ni siquiera se consulta la BD.
            if (!scope.PuedeDecidir) return bandeja;

            using var ctx = _factory.CreateDbContext();

            // EF traduce Contains sobre una lista; el HashSet del scope se materializa una vez.
            var areaIds = scope.AreaScopeIds.ToList();

            // 1) Cabeceras: una fila por solicitud dentro del alcance del usuario. Los nombres del
            //    solicitante y de quienes decidieron salen por left join a person (user_id es 1:1
            //    con app_user), no por consulta suelta: evita el N+1 de la lista completa.
            var cabeceras = await (
                from a in ctx.GthAprobacionGg.AsNoTracking()
                where a.State
                join s in ctx.GthSolicitud.AsNoTracking() on a.GthSolicitudId equals s.GthSolicitudId
                where s.State
                      // Alcance: el GG no filtra; el gerente de área solo ve su subárbol. Una
                      // solicitud sin area_scope (no se pudo resolver al registrarla) queda fuera
                      // del alcance de cualquier gerente de área: no hay a qué gerencia atribuirla.
                      && (scope.VeTodo || (s.AreaScopeId != null && areaIds.Contains(s.AreaScopeId.Value)))
                join egg in ctx.GthAprobacionGgEstado.AsNoTracking() on a.EstadoGerenteGeneralId equals egg.GthAprobacionGgEstadoId
                join ega in ctx.GthAprobacionGgEstado.AsNoTracking() on a.EstadoGerenteAreaId equals ega.GthAprobacionGgEstadoId
                join egt in ctx.GthAprobacionGgEstado.AsNoTracking() on a.EstadoGthId equals egt.GthAprobacionGgEstadoId
                join ps in ctx.Person.AsNoTracking() on s.SolicitanteUserId equals ps.UserId into solicitanteJoin
                from ps in solicitanteJoin.DefaultIfEmpty()
                join pgg in ctx.Person.AsNoTracking() on a.GerenteGeneralDecididoUserId equals pgg.UserId into ggJoin
                from pgg in ggJoin.DefaultIfEmpty()
                join pga in ctx.Person.AsNoTracking() on a.GerenteAreaDecididoUserId equals pga.UserId into gaJoin
                from pga in gaJoin.DefaultIfEmpty()
                join pgt in ctx.Person.AsNoTracking() on a.GthDecididoUserId equals pgt.UserId into gthJoin
                from pgt in gthJoin.DefaultIfEmpty()
                orderby a.GthAprobacionGgId descending
                select new
                {
                    a.GthAprobacionGgId,
                    s.AreaNombre,
                    s.Justificacion,
                    s.CreatedDateTime,
                    SolicitanteNombre = ps != null ? ps.FullName : null,

                    GgEstadoCodigo = egg.Codigo,
                    GgEstadoNombre = egg.Nombre,
                    GgDecididoEn   = a.GerenteGeneralDecididoDateTime,
                    GgDecididoPor  = pgg != null ? pgg.FullName : null,
                    GgComentario   = a.GerenteGeneralComentario,

                    GaEstadoCodigo = ega.Codigo,
                    GaEstadoNombre = ega.Nombre,
                    GaDecididoEn   = a.GerenteAreaDecididoDateTime,
                    GaDecididoPor  = pga != null ? pga.FullName : null,
                    GaComentario   = a.GerenteAreaComentario,

                    GtEstadoCodigo = egt.Codigo,
                    GtEstadoNombre = egt.Nombre,
                    GtDecididoEn   = a.GthDecididoDateTime,
                    GtDecididoPor  = pgt != null ? pgt.FullName : null,
                    GtComentario   = a.GthComentario,
                }).ToListAsync();

            if (cabeceras.Count == 0) return bandeja;

            // 2) Vacantes de todas esas solicitudes de una sola vez (código + las dos decisiones).
            //    Se parte de gth_requerimiento y el detalle entra por left join —igual que
            //    QueryVacantes, para que la lista y el modal no puedan contar distinto si a una
            //    vacante le falta su fila de detalle. La llave del join es el par (aprobación,
            //    requerimiento): filtrar solo por requerimiento traería el detalle de una
            //    aprobación dada de baja.
            var ids = cabeceras.Select(c => c.GthAprobacionGgId).ToList();
            var vacantes = await (
                from a in ctx.GthAprobacionGg.AsNoTracking()
                where ids.Contains(a.GthAprobacionGgId)
                join r in ctx.GthRequerimiento.AsNoTracking() on a.GthSolicitudId equals r.GthSolicitudId
                where r.State
                join t in ctx.GthTipoRequerimiento.AsNoTracking() on r.GthTipoRequerimientoId equals t.GthTipoRequerimientoId
                join d in ctx.GthAprobacionGgDetalle.AsNoTracking().Where(x => x.State)
                    on new { A = a.GthAprobacionGgId, R = r.GthRequerimientoId }
                    equals new { A = d.GthAprobacionGgId, R = d.GthRequerimientoId } into detalleJoin
                from d in detalleJoin.DefaultIfEmpty()
                orderby r.GthRequerimientoId
                select new
                {
                    a.GthAprobacionGgId,
                    r.Codigo,
                    r.EsFft,
                    TipoCodigo  = t.Codigo,
                    // Ver RutaAprobacion.De: un FFT solo entra a la aprobación si tiene detalle.
                    FftLegado   = d != null,
                    AprobadoGg  = d != null ? d.AprobadoGerenteGeneral : null,
                    AprobadoGa  = d != null ? d.AprobadoGerenteArea : null,
                    AprobadoGt  = d != null ? d.AprobadoGth : null,
                }).ToListAsync();

            var porAprobacion = vacantes.ToLookup(v => v.GthAprobacionGgId);

            bandeja.Aprobaciones = cabeceras.Select(c =>
            {
                // Solo las vacantes de MI ruta y de MI turno. Es un corte de VISIBILIDAD y no de
                // presentación: en una solicitud mixta el Gerente General no ve los reemplazos —ni
                // sus códigos, ni sus conteos, ni las casillas del gerente del área y de GTH— y el
                // gerente del área y GTH no ven las vacantes nuevas. Además, en un reemplazo las
                // dos firmas van en orden: GTH no ve las que el gerente del área todavía no
                // aprobó (ver RutaAprobacion.LeTocaAhora).
                var vs = porAprobacion[c.GthAprobacionGgId]
                    .Select(v => new { v.Codigo, v.AprobadoGg, v.AprobadoGa, v.AprobadoGt,
                                       Ruta = RutaAprobacion.De(v.EsFft, v.TipoCodigo, v.FftLegado) })
                    .Where(v => RutaAprobacion.LeTocaAhora(v.Ruta, scope.Nivel, v.AprobadoGa, v.AprobadoGt))
                    .ToList();

                // Cada casilla cuenta solo sus vacantes. Con el corte de arriba una de las dos
                // listas queda siempre vacía, y es justo la que apaga las casillas ajenas por
                // Requiere*: al Gerente General no se le pintan las del gerente del área ni GTH.
                var deGg   = vs.Where(v => v.Ruta == RutaAprobacion.GerenciaGeneral).ToList();
                var deArea = vs.Where(v => v.Ruta == RutaAprobacion.AreaYGth).ToList();

                var gg = new AprobacionNivelResumenDto
                {
                    EstadoCodigo       = c.GgEstadoCodigo,
                    EstadoNombre       = c.GgEstadoNombre,
                    Decidida           = c.GgEstadoCodigo != AprobacionGgEstadoCodigo.Pendiente,
                    DecididoEn         = c.GgDecididoEn?.ToOffset(PeruOffset).DateTime,
                    DecididoPor        = c.GgDecididoPor,
                    Comentario         = c.GgComentario,
                    VacantesAprobadas  = deGg.Count(v => v.AprobadoGg == true),
                    VacantesRechazadas = deGg.Count(v => v.AprobadoGg == false),
                };

                var ga = new AprobacionNivelResumenDto
                {
                    EstadoCodigo       = c.GaEstadoCodigo,
                    EstadoNombre       = c.GaEstadoNombre,
                    Decidida           = c.GaEstadoCodigo != AprobacionGgEstadoCodigo.Pendiente,
                    DecididoEn         = c.GaDecididoEn?.ToOffset(PeruOffset).DateTime,
                    DecididoPor        = c.GaDecididoPor,
                    Comentario         = c.GaComentario,
                    VacantesAprobadas  = deArea.Count(v => v.AprobadoGa == true),
                    VacantesRechazadas = deArea.Count(v => v.AprobadoGa == false),
                };

                var gth = new AprobacionNivelResumenDto
                {
                    EstadoCodigo       = c.GtEstadoCodigo,
                    EstadoNombre       = c.GtEstadoNombre,
                    Decidida           = c.GtEstadoCodigo != AprobacionGgEstadoCodigo.Pendiente,
                    DecididoEn         = c.GtDecididoEn?.ToOffset(PeruOffset).DateTime,
                    DecididoPor        = c.GtDecididoPor,
                    Comentario         = c.GtComentario,
                    VacantesAprobadas  = deArea.Count(v => v.AprobadoGt == true),
                    VacantesRechazadas = deArea.Count(v => v.AprobadoGt == false),
                };

                // Todas las que quedaron son mías: el corte de arriba ya sacó las ajenas.
                var mias = vs.Count;
                var miCasilla = MiCasilla(scope.Nivel, gg, ga, gth);

                return new AprobacionGgBandejaItemDto
                {
                    AprobacionId           = c.GthAprobacionGgId,
                    Codigos                = string.Join(", ", vs.Select(v => v.Codigo)),
                    Area                   = c.AreaNombre,
                    SolicitanteNombre      = c.SolicitanteNombre,
                    Justificacion          = c.Justificacion,
                    Enviado                = c.CreatedDateTime.ToOffset(PeruOffset).DateTime,
                    TotalVacantes          = mias,
                    GerenteGeneral         = gg,
                    GerenteArea            = ga,
                    Gth                    = gth,
                    RequiereGerenteGeneral = deGg.Count > 0,
                    RequiereGerenteArea    = deArea.Count > 0,
                    RequiereGth            = deArea.Count > 0,
                    // Espera mi firma solo si además hay algo mío que firmar: al gerente de un área
                    // que solo pidió vacantes nuevas esta solicitud no le toca, y ponérsela como
                    // pendiente le dejaría en la bandeja un trabajo que no puede hacer.
                    EsperaMiDecision       = mias > 0 && miCasilla is { Decidida: false },
                };
            })
            // Solo lo que este usuario puede aprobar. Desde el corte por tipo de requerimiento una
            // solicitud puede no tener ninguna vacante suya —un gerente de área frente a una de
            // puras vacantes nuevas, GTH frente a lo mismo—, y esta pantalla es la de aprobaciones:
            // una fila sin un solo botón que pulsar solo ensucia la bandeja y descuadra los
            // contadores. El solicitante sigue viendo su solicitud completa en «Solicitud de
            // Personal», que es donde le corresponde seguirla.
            .Where(i => i.TotalVacantes > 0)
            .ToList();

            // 3) Tarjetas: siempre contra la casilla del usuario que consulta.
            var propias = bandeja.Aprobaciones
                .Select(i => new { Item = i, Mi = MiCasilla(scope.Nivel, i.GerenteGeneral, i.GerenteArea, i.Gth)! })
                .ToList();

            bandeja.Resumen = new AprobacionGgBandejaResumenDto
            {
                Pendientes         = propias.Count(x => !x.Mi.Decidida),
                VacantesPendientes = propias.Where(x => !x.Mi.Decidida).Sum(x => x.Item.TotalVacantes),
                // "Aprobadas" incluye las parciales: en ambas hay vacantes que sí tuvieron el visto bueno.
                Aprobadas          = propias.Count(x => x.Mi.Decidida && x.Mi.VacantesAprobadas > 0),
                Rechazadas         = propias.Count(x => x.Mi.Decidida && x.Mi.VacantesAprobadas == 0),
            };

            return bandeja;
        }

        /// <summary>
        /// La casilla que le toca a un nivel. Null cuando el nivel no decide nada (NINGUNO): es la
        /// diferencia entre "mi casilla sigue pendiente" y "no tengo casilla".
        /// </summary>
        private static AprobacionNivelResumenDto? MiCasilla(
            string nivel,
            AprobacionNivelResumenDto gg,
            AprobacionNivelResumenDto ga,
            AprobacionNivelResumenDto gth) => nivel switch
        {
            AprobacionNivel.GerenteGeneral => gg,
            AprobacionNivel.GerenteArea    => ga,
            AprobacionNivel.Gth            => gth,
            _                              => null,
        };

        /// <summary>Id del estado de la casilla que le toca a un nivel (para el chequeo de "ya decidió").</summary>
        private static int EstadoDeMiCasilla(GthAprobacionGg a, string nivel) => nivel switch
        {
            AprobacionNivel.GerenteGeneral => a.EstadoGerenteGeneralId,
            AprobacionNivel.GerenteArea    => a.EstadoGerenteAreaId,
            AprobacionNivel.Gth            => a.EstadoGthId,
            _                              => a.EstadoGerenteGeneralId,
        };

        /// <summary>Mensaje de "esta casilla ya está cerrada", con el nombre de quien la cerró.</summary>
        private static string YaDecidio(string nivel) => nivel switch
        {
            AprobacionNivel.GerenteGeneral => "Gerencia General ya decidió sobre esta solicitud.",
            AprobacionNivel.GerenteArea    => "El gerente del área ya decidió sobre esta solicitud.",
            AprobacionNivel.Gth            => "Gestión del Talento Humano ya decidió sobre esta solicitud.",
            _                              => "Esta solicitud ya fue decidida.",
        };

        public async Task<AprobacionGgDetalleDto?> GetDetalle(int aprobacionId, AprobacionScope scope)
        {
            using var ctx = _factory.CreateDbContext();

            var head = await (
                from a in ctx.GthAprobacionGg.AsNoTracking()
                where a.GthAprobacionGgId == aprobacionId && a.State
                join s in ctx.GthSolicitud.AsNoTracking() on a.GthSolicitudId equals s.GthSolicitudId
                where s.State
                join egg in ctx.GthAprobacionGgEstado.AsNoTracking() on a.EstadoGerenteGeneralId equals egg.GthAprobacionGgEstadoId
                join ega in ctx.GthAprobacionGgEstado.AsNoTracking() on a.EstadoGerenteAreaId equals ega.GthAprobacionGgEstadoId
                join egt in ctx.GthAprobacionGgEstado.AsNoTracking() on a.EstadoGthId equals egt.GthAprobacionGgEstadoId
                join ps in ctx.Person.AsNoTracking() on s.SolicitanteUserId equals ps.UserId into solicitanteJoin
                from ps in solicitanteJoin.DefaultIfEmpty()
                join pgg in ctx.Person.AsNoTracking() on a.GerenteGeneralDecididoUserId equals pgg.UserId into ggJoin
                from pgg in ggJoin.DefaultIfEmpty()
                join pga in ctx.Person.AsNoTracking() on a.GerenteAreaDecididoUserId equals pga.UserId into gaJoin
                from pga in gaJoin.DefaultIfEmpty()
                join pgt in ctx.Person.AsNoTracking() on a.GthDecididoUserId equals pgt.UserId into gthJoin
                from pgt in gthJoin.DefaultIfEmpty()
                select new
                {
                    a.GthAprobacionGgId,
                    a.GthSolicitudId,
                    s.AreaScopeId,
                    s.AreaNombre,
                    s.Justificacion,
                    s.SustentoNombre,
                    s.SustentoUrl,
                    s.CreatedDateTime,
                    SolicitanteNombre = ps != null ? ps.FullName : null,

                    GgEstadoCodigo = egg.Codigo,
                    GgEstadoNombre = egg.Nombre,
                    GgDecididoEn   = a.GerenteGeneralDecididoDateTime,
                    GgDecididoPor  = pgg != null ? pgg.FullName : null,
                    GgComentario   = a.GerenteGeneralComentario,

                    GaEstadoCodigo = ega.Codigo,
                    GaEstadoNombre = ega.Nombre,
                    GaDecididoEn   = a.GerenteAreaDecididoDateTime,
                    GaDecididoPor  = pga != null ? pga.FullName : null,
                    GaComentario   = a.GerenteAreaComentario,

                    GtEstadoCodigo = egt.Codigo,
                    GtEstadoNombre = egt.Nombre,
                    GtDecididoEn   = a.GthDecididoDateTime,
                    GtDecididoPor  = pgt != null ? pgt.FullName : null,
                    GtComentario   = a.GthComentario,
                }).FirstOrDefaultAsync();

            if (head == null) return null;

            // El enlace del correo lleva un id de aprobación: un gerente de área que reciba (o
            // reenvíen) uno de otra gerencia no puede abrirlo. Se distingue de "no existe" a
            // propósito: es un caso real y el mensaje tiene que explicarlo.
            EnsureAlcance(scope, head.AreaScopeId);

            // Mismo corte de visibilidad que la bandeja: solo las vacantes de MI ruta y de MI
            // turno. Acá importa el doble, porque el modal es lo que trae los datos de cada
            // vacante: así el Gerente General no recibe siquiera el puesto, el salario ni a quién
            // reemplaza de un reemplazo ajeno, el gerente del área y GTH no reciben los de las
            // vacantes nuevas, y GTH no recibe los de un reemplazo que el área todavía no aprobó.
            var vacantes = (await QueryVacantes(ctx, head.GthAprobacionGgId, head.GthSolicitudId))
                .Where(v => RutaAprobacion.LeTocaAhora(
                    v.Ruta, scope.Nivel, v.AprobadoGerenteArea, v.AprobadoGth))
                .ToList();

            // Sin ninguna vacante mía esta aprobación no me toca. Pasa al abrir el enlace del
            // correo de una solicitud de puras vacantes de la otra ruta —la bandeja ya no la
            // lista—, y se corta con un mensaje en vez de abrir un modal vacío.
            if (vacantes.Count == 0)
                throw new AbrilException(
                    "Esta solicitud de personal no tiene vacantes que te toque decidir.", 403);

            // Cada casilla cuenta solo las vacantes de su ruta (ver GetBandeja).
            var deGg   = vacantes.Where(v => v.Ruta == RutaAprobacion.GerenciaGeneral).ToList();
            var deArea = vacantes.Where(v => v.Ruta == RutaAprobacion.AreaYGth).ToList();

            var gg = new AprobacionNivelResumenDto
            {
                EstadoCodigo       = head.GgEstadoCodigo,
                EstadoNombre       = head.GgEstadoNombre,
                Decidida           = head.GgEstadoCodigo != AprobacionGgEstadoCodigo.Pendiente,
                DecididoEn         = head.GgDecididoEn?.ToOffset(PeruOffset).DateTime,
                DecididoPor        = head.GgDecididoPor,
                Comentario         = head.GgComentario,
                VacantesAprobadas  = deGg.Count(v => v.AprobadoGerenteGeneral == true),
                VacantesRechazadas = deGg.Count(v => v.AprobadoGerenteGeneral == false),
            };

            var ga = new AprobacionNivelResumenDto
            {
                EstadoCodigo       = head.GaEstadoCodigo,
                EstadoNombre       = head.GaEstadoNombre,
                Decidida           = head.GaEstadoCodigo != AprobacionGgEstadoCodigo.Pendiente,
                DecididoEn         = head.GaDecididoEn?.ToOffset(PeruOffset).DateTime,
                DecididoPor        = head.GaDecididoPor,
                Comentario         = head.GaComentario,
                VacantesAprobadas  = deArea.Count(v => v.AprobadoGerenteArea == true),
                VacantesRechazadas = deArea.Count(v => v.AprobadoGerenteArea == false),
            };

            var gth = new AprobacionNivelResumenDto
            {
                EstadoCodigo       = head.GtEstadoCodigo,
                EstadoNombre       = head.GtEstadoNombre,
                Decidida           = head.GtEstadoCodigo != AprobacionGgEstadoCodigo.Pendiente,
                DecididoEn         = head.GtDecididoEn?.ToOffset(PeruOffset).DateTime,
                DecididoPor        = head.GtDecididoPor,
                Comentario         = head.GtComentario,
                VacantesAprobadas  = deArea.Count(v => v.AprobadoGth == true),
                VacantesRechazadas = deArea.Count(v => v.AprobadoGth == false),
            };

            var miCasilla = MiCasilla(scope.Nivel, gg, ga, gth);

            return new AprobacionGgDetalleDto
            {
                AprobacionId           = head.GthAprobacionGgId,
                Area                   = head.AreaNombre,
                SolicitanteNombre      = head.SolicitanteNombre,
                Justificacion          = head.Justificacion,
                SustentoNombre         = head.SustentoNombre,
                SustentoUrl            = head.SustentoUrl,
                Enviado                = head.CreatedDateTime.ToOffset(PeruOffset).DateTime,
                GerenteGeneral         = gg,
                GerenteArea            = ga,
                Gth                    = gth,
                RequiereGerenteGeneral = deGg.Count > 0,
                RequiereGerenteArea    = deArea.Count > 0,
                RequiereGth            = deArea.Count > 0,
                Nivel                  = scope.Nivel,
                // Basta con que mi casilla siga abierta: el corte de arriba ya garantizó que hay al
                // menos una vacante mía que marcar (si no, se cortó con 403). MiCasilla es null solo
                // para NINGUNO, que nunca llega acá porque EnsureAlcance lo frena antes.
                PuedeDecidir           = miCasilla is { Decidida: false },
                Vacantes               = vacantes,
            };
        }

        public async Task<AprobacionGgDecisionContextoDto> RegistrarDecision(
            int aprobacionId, AprobacionGgDecisionDto dto, int userId, AprobacionScope scope)
        {
            if (!scope.PuedeDecidir)
                throw new AbrilException(
                    "Tu ficha de trabajador no es de Gerencia General, ni de gerente de área, ni del " +
                    "área de Gestión del Talento Humano, así que no puedes aprobar ni rechazar " +
                    "solicitudes de personal.", 403);

            using var ctx = _factory.CreateDbContext();

            var aprobacion = await ctx.GthAprobacionGg
                .FirstOrDefaultAsync(a => a.GthAprobacionGgId == aprobacionId && a.State);
            if (aprobacion == null)
                throw new AbrilException("La solicitud por aprobar ya no está disponible.", 404);

            var solicitud = await ctx.GthSolicitud
                .FirstOrDefaultAsync(s => s.GthSolicitudId == aprobacion.GthSolicitudId && s.State)
                ?? throw new AbrilException("La solicitud de personal ya no está disponible.", 404);

            EnsureAlcance(scope, solicitud.AreaScopeId);

            var esGg = scope.Nivel == AprobacionNivel.GerenteGeneral;

            // Cada nivel decide UNA sola vez. Se lee el estado de SU casilla: que otro nivel ya
            // haya decidido no bloquea (los tres pueden tener el modal abierto a la vez).
            var estadoActualId = EstadoDeMiCasilla(aprobacion, scope.Nivel);
            var estadoActual = await ctx.GthAprobacionGgEstado
                .Where(e => e.GthAprobacionGgEstadoId == estadoActualId)
                .Select(e => e.Codigo)
                .FirstOrDefaultAsync();
            if (estadoActual != AprobacionGgEstadoCodigo.Pendiente)
                throw new AbrilException(YaDecidio(scope.Nivel), 409);

            // Vacantes vigentes de la solicitud, con el código de su tipo: es lo que decide por qué
            // ruta va cada una y, con eso, cuáles le tocan a este nivel.
            var todas = await (
                from r in ctx.GthRequerimiento
                where r.GthSolicitudId == aprobacion.GthSolicitudId && r.State
                join t in ctx.GthTipoRequerimiento on r.GthTipoRequerimientoId equals t.GthTipoRequerimientoId
                orderby r.GthRequerimientoId
                select new { Req = r, TipoCodigo = t.Codigo }).ToListAsync();
            if (todas.Count == 0)
                throw new AbrilException("La solicitud no tiene vacantes por aprobar.", 400);

            // El detalle se lee ACÁ y no más abajo porque es lo que decide la ruta de una vacante
            // FFT: solo las que quedaron enganchadas a la aprobación de antes del salto tienen fila
            // y siguen esperando la firma de Gerencia General (ver RutaAprobacion.De).
            var detalles = await ctx.GthAprobacionGgDetalle
                .Where(d => d.GthAprobacionGgId == aprobacion.GthAprobacionGgId && d.State)
                .ToListAsync();
            var conDetalle = detalles.Select(d => d.GthRequerimientoId).ToHashSet();

            // Solo las de SU ruta y SU turno: un gerente de área no firma vacantes nuevas, el
            // Gerente General no firma reemplazos, un ingreso directo no lo firma nadie y GTH no
            // firma un reemplazo que el gerente del área todavía no aprobó. Una solicitud mixta se
            // decide en dos actos, uno por cada lado.
            var porRequerimiento = detalles.ToDictionary(d => d.GthRequerimientoId);
            var mias = todas
                .Where(x =>
                {
                    porRequerimiento.TryGetValue(x.Req.GthRequerimientoId, out var d);
                    return RutaAprobacion.LeTocaAhora(
                        RutaAprobacion.De(x.Req.EsFft, x.TipoCodigo,
                                          conDetalle.Contains(x.Req.GthRequerimientoId)),
                        scope.Nivel, d?.AprobadoGerenteArea, d?.AprobadoGth);
                })
                .ToList();
            if (mias.Count == 0)
                throw new AbrilException(
                    "Esta solicitud no tiene vacantes que te toque aprobar a ti.", 403);

            var requerimientos = mias.Select(x => x.Req).ToList();

            // La decisión debe cubrir exactamente a las vacantes de su ruta. Lo que venga de las
            // otras se ignora: el modal no las deja marcar, y aceptarlas sería dejar que un nivel
            // firme por otro.
            var decisionPorId = new Dictionary<int, bool>();
            foreach (var d in dto.Decisiones) decisionPorId[d.RequerimientoId] = d.Aprobado;
            if (requerimientos.Any(r => !decisionPorId.ContainsKey(r.GthRequerimientoId)))
                throw new AbrilException("Debes aprobar o rechazar todas las vacantes que te corresponden.", 400);

            // Estados destino del requerimiento. Ya no los mueve solo Gerencia General: cada vacante
            // avanza cuando junta TODAS las firmas de su ruta. Las de ruta GG con la del Gerente
            // General; las de reemplazo, recién cuando el gerente del área Y GTH la aprobaron — la
            // primera de las dos decisiones la deja donde está.
            //
            // Un rechazo, en cambio, corta al toque: con que uno de los dos diga que no, la vacante
            // se cae y ya no tiene sentido esperar al otro.
            //
            // Una vacante FFT aprobada no va a VALIDACION_GTH sino directo al EMO de ingreso: no
            // hay nada que publicar, ni long list que armar, ni formulario que mandarle — el
            // candidato ya viene con nombre y con sus datos. Eso solo pasa por la ruta GG (un FFT
            // nunca va por la del área).
            FftFlujo.Catalogo? catalogoFft = null;
            GthEstadoRequerimiento estadoValidacionGth, estadoRechazado;
            var hayFft = requerimientos.Any(r => r.EsFft);
            {
                var estadosReq = await ctx.GthEstadoRequerimiento
                    .Where(e => e.State && (e.Codigo == EstadoReclutamiento.ValidacionGth
                                            || e.Codigo == EstadoReclutamiento.RechazadoGg))
                    .ToListAsync();
                var validacionGth = estadosReq.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.ValidacionGth)
                    ?? throw new AbrilException("No está configurado el estado VALIDACION_GTH de reclutamiento.", 500);
                var rechazadoGg = estadosReq.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.RechazadoGg)
                    ?? throw new AbrilException("No está configurado el estado RECHAZADO_GG de reclutamiento.", 500);
                estadoValidacionGth = validacionGth;
                estadoRechazado     = rechazadoGg;

                if (esGg && hayFft) catalogoFft = await FftFlujo.CargarCatalogoAsync(ctx);
            }

            // Nombre del puesto de las vacantes FFT (snapshot de la ficha del candidato) y qué
            // requerimientos ya tienen candidato (guardia de idempotencia): una sola consulta cada
            // uno para todas las vacantes, aunque la solicitud traiga varias.
            var puestoIdsFft = requerimientos.Where(r => r.EsFft).Select(r => r.PuestoId).Distinct().ToList();
            var nombrePorPuesto = esGg && puestoIdsFft.Count > 0
                ? await ctx.Puesto
                    .Where(p => puestoIdsFft.Contains(p.PuestoId))
                    .ToDictionaryAsync(p => p.PuestoId, p => p.Nombre)
                : new Dictionary<int, string>();
            var yaConCandidato = esGg && puestoIdsFft.Count > 0
                ? await FftFlujo.RequerimientosConCandidatoAsync(
                    ctx, requerimientos.Where(r => r.EsFft).Select(r => r.GthRequerimientoId).ToList())
                : new HashSet<int>();

            var now = DateTimeOffset.UtcNow;
            int aprobados = 0, rechazados = 0;

            // Las vacantes que quedaron COMPLETAMENTE aprobadas con esta decisión: son las únicas
            // que se le mandan a GTH. En la ruta GG son las que el Gerente General aprobó; en la de
            // reemplazo, solo las que con esta firma juntaron las dos.
            var completadas = new HashSet<int>();

            // Las que esta decisión aprobó pero todavía esperan la firma que sigue. Hoy solo pasa
            // en un caso —el gerente del área aprobando un reemplazo— y son exactamente las que
            // hay que ponerle a GTH en la bandeja, así que son las que lleva su correo.
            var esperandoSiguienteFirma = new HashSet<int>();

            foreach (var r in requerimientos)
            {
                var aprobado = decisionPorId[r.GthRequerimientoId];
                if (aprobado) aprobados++; else rechazados++;

                var detalle = detalles.FirstOrDefault(d => d.GthRequerimientoId == r.GthRequerimientoId);
                if (detalle == null)
                {
                    detalle = new GthAprobacionGgDetalle
                    {
                        GthAprobacionGgId  = aprobacion.GthAprobacionGgId,
                        GthRequerimientoId = r.GthRequerimientoId,
                        CreatedDateTime    = now,
                        CreatedUserId      = userId,
                        Active             = true,
                        State              = true,
                    };
                    ctx.GthAprobacionGgDetalle.Add(detalle);
                }
                else
                {
                    detalle.UpdatedDateTime = now;
                    detalle.UpdatedUserId   = userId;
                }

                // Solo se escribe la columna del nivel que está decidiendo: las de los otros quedan
                // como estén (pueden tener ya una decisión distinta, y esa discrepancia es
                // información). Se escribe ANTES de mover el requerimiento porque la regla de los
                // reemplazos se evalúa sobre el detalle ya actualizado.
                switch (scope.Nivel)
                {
                    case AprobacionNivel.GerenteGeneral:
                        detalle.AprobadoGerenteGeneral         = aprobado;
                        detalle.GerenteGeneralDecididoDateTime = now;
                        break;
                    case AprobacionNivel.GerenteArea:
                        detalle.AprobadoGerenteArea         = aprobado;
                        detalle.GerenteAreaDecididoDateTime = now;
                        break;
                    case AprobacionNivel.Gth:
                        detalle.AprobadoGth         = aprobado;
                        detalle.GthDecididoDateTime = now;
                        break;
                }

                // ¿Esta decisión mueve la vacante? Un rechazo la corta siempre; una aprobación solo
                // cuando ya no falta ninguna firma de su ruta.
                var completa = !aprobado || (esGg
                    ? true
                    : detalle.AprobadoGerenteArea == true && detalle.AprobadoGth == true);
                if (!completa)
                {
                    // El reemplazo espera la otra firma: nada que mover, pero sí a quién avisarle.
                    esperandoSiguienteFirma.Add(r.GthRequerimientoId);
                    continue;
                }

                if (aprobado) completadas.Add(r.GthRequerimientoId);

                if (aprobado && r.EsFft)
                {
                    // Aprobar un FFT es dárselo a GTH con el proceso de selección ya cerrado: se
                    // salta publicación, revisión de CV, long list, formulario del postulante,
                    // entrevistas y finalistas, y lo único que queda es programarle su EMO.
                    await FftFlujo.AbrirIngresoDirectoAsync(
                        ctx, r, catalogoFft!,
                        nombrePorPuesto.GetValueOrDefault(r.PuestoId), yaConCandidato, userId, now);
                }
                else
                {
                    r.GthEstadoRequerimientoId = aprobado
                        ? estadoValidacionGth.GthEstadoRequerimientoId
                        : estadoRechazado.GthEstadoRequerimientoId;
                    r.UpdatedDateTime = now;
                    r.UpdatedUserId   = userId;
                }
            }

            var codigoEstado = aprobados == 0 ? AprobacionGgEstadoCodigo.Rechazada
                             : rechazados == 0 ? AprobacionGgEstadoCodigo.Aprobada
                             : AprobacionGgEstadoCodigo.AprobadaParcial;
            var estadoDestino = await ctx.GthAprobacionGgEstado
                .FirstOrDefaultAsync(e => e.Codigo == codigoEstado && e.State)
                ?? throw new AbrilException($"No está configurado el estado {codigoEstado} de la aprobación.", 500);

            var comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? null : dto.Comentario.Trim();

            // Traza de quién decidió, aparte de updated_user_id para que un update posterior (la
            // firma tardía de otro nivel) no la pise.
            switch (scope.Nivel)
            {
                case AprobacionNivel.GerenteGeneral:
                    aprobacion.EstadoGerenteGeneralId         = estadoDestino.GthAprobacionGgEstadoId;
                    aprobacion.GerenteGeneralDecididoDateTime = now;
                    aprobacion.GerenteGeneralDecididoUserId   = userId;
                    aprobacion.GerenteGeneralComentario       = comentario;
                    break;
                case AprobacionNivel.GerenteArea:
                    aprobacion.EstadoGerenteAreaId         = estadoDestino.GthAprobacionGgEstadoId;
                    aprobacion.GerenteAreaDecididoDateTime = now;
                    aprobacion.GerenteAreaDecididoUserId   = userId;
                    aprobacion.GerenteAreaComentario       = comentario;
                    break;
                case AprobacionNivel.Gth:
                    aprobacion.EstadoGthId          = estadoDestino.GthAprobacionGgEstadoId;
                    aprobacion.GthDecididoDateTime  = now;
                    aprobacion.GthDecididoUserId    = userId;
                    aprobacion.GthComentario        = comentario;
                    break;
            }

            aprobacion.UpdatedDateTime = now;
            aprobacion.UpdatedUserId   = userId;

            var solicitanteNombre = solicitud.SolicitanteUserId.HasValue
                ? await ctx.Worker
                    .Where(w => w.Person != null && w.Person.UserId == solicitud.SolicitanteUserId.Value)
                    .Select(w => w.Person!.FullName ?? w.ApellidoNombre)
                    .FirstOrDefaultAsync()
                : null;

            // Qué dejó dicho el gerente del área: va como contexto en el correo a GTH, tanto en el
            // de Gerencia General como en el del reemplazo (donde además es una de las dos firmas
            // que lo movieron). Se lee antes del SaveChanges: la decisión que se está registrando ya
            // está aplicada sobre la entidad en memoria, así que si el que decide es el propio
            // gerente del área, el resumen sale con su decisión de ahora.
            var gerenteAreaResumen = await BuildGerenteAreaResumen(ctx, aprobacion);

            await ctx.SaveChangesAsync();

            // Vacantes con sus datos legibles para el correo a GTH. Solo las que esta decisión dejó
            // completamente aprobadas: en un reemplazo, la primera de las dos firmas todavía no
            // manda nada — la vacante sigue esperando a la otra.
            var vacantes = await QueryVacantes(ctx, aprobacion.GthAprobacionGgId, aprobacion.GthSolicitudId);

            return new AprobacionGgDecisionContextoDto
            {
                Resultado = new AprobacionGgDecisionResultDto
                {
                    Nivel        = scope.Nivel,
                    EstadoCodigo = estadoDestino.Codigo,
                    EstadoNombre = estadoDestino.Nombre,
                    Aprobados    = aprobados,
                    Rechazados   = rechazados,
                },
                SolicitudId        = aprobacion.GthSolicitudId,
                AprobacionId       = aprobacion.GthAprobacionGgId,
                Area               = solicitud.AreaNombre,
                SolicitanteNombre  = solicitanteNombre,
                Justificacion      = solicitud.Justificacion,
                SustentoNombre     = solicitud.SustentoNombre,
                SustentoUrl        = solicitud.SustentoUrl,
                Comentario         = comentario,
                GerenteAreaResumen = gerenteAreaResumen,
                Aprobadas          = vacantes.Where(v => completadas.Contains(v.RequerimientoId)).ToList(),
                EsperandoSiguienteFirma = vacantes
                    .Where(v => esperandoSiguienteFirma.Contains(v.RequerimientoId)).ToList(),
            };
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Misma regla que <see cref="RegistrarDecision"/> —solo se escribe la casilla del nivel del
        /// usuario y solo el Gerente General mueve el pipeline— pero resuelta en lote: las
        /// cabeceras, las vacantes, los detalles y los catálogos se leen de una vez para todas las
        /// solicitudes seleccionadas y todo se guarda en un solo <c>SaveChanges</c>, en vez de
        /// repetir N veces la decisión de una.
        ///
        /// El lote no es todo-o-nada: cada solicitud que ya no admite la decisión de este usuario se
        /// aparta con su motivo y las demás se registran igual. Lo contrario obligaría al gerente a
        /// adivinar cuál de las diez que seleccionó se cerró mientras revisaba.
        /// </remarks>
        public async Task<AprobacionGgDecisionMasivaContextoDto> RegistrarDecisionMasiva(
            List<int> aprobacionIds, bool aprobado, string? comentario, int userId, AprobacionScope scope)
        {
            if (!scope.PuedeDecidir)
                throw new AbrilException(
                    "Tu ficha de trabajador no es de Gerencia General, ni de gerente de área, ni del " +
                    "área de Gestión del Talento Humano, así que no puedes aprobar ni rechazar " +
                    "solicitudes de personal.", 403);

            var resultado = new AprobacionGgDecisionMasivaContextoDto { Nivel = scope.Nivel };
            if (aprobacionIds.Count == 0) return resultado;

            using var ctx = _factory.CreateDbContext();
            var esGg = scope.Nivel == AprobacionNivel.GerenteGeneral;

            // 1) Cabeceras: la aprobación y su solicitud. Entidades (no proyección) porque las dos
            //    se escriben más abajo.
            var cabeceras = await (
                from a in ctx.GthAprobacionGg
                where aprobacionIds.Contains(a.GthAprobacionGgId) && a.State
                join s in ctx.GthSolicitud on a.GthSolicitudId equals s.GthSolicitudId
                where s.State
                select new { Aprobacion = a, Solicitud = s }
            ).ToListAsync();

            // Las que ni siquiera existen (o se dieron de baja) se apartan acá: sin esto
            // desaparecerían del conteo sin explicación.
            foreach (var id in aprobacionIds.Where(id => cabeceras.All(c => c.Aprobacion.GthAprobacionGgId != id)))
                resultado.Omitidas.Add(new AprobacionGgDecisionOmitidaDto
                {
                    AprobacionId = id,
                    Motivo       = "La solicitud ya no está disponible.",
                });

            if (cabeceras.Count == 0) return resultado;

            // 2) Catálogos y filas hijas de TODAS las cabeceras, de una sola vez.
            //    Los estados de la aprobación son un catálogo de 4 filas: se trae completo (sin
            //    filtrar State) porque hace falta tanto para leer el estado actual de cada casilla
            //    como para resolver el destino.
            var estados = await ctx.GthAprobacionGgEstado.AsNoTracking().ToListAsync();

            var solicitudIds = cabeceras.Select(c => c.Solicitud.GthSolicitudId).ToList();
            // Con el código del tipo: es lo que decide la ruta de cada vacante y, con ella, cuáles
            // de las de cada solicitud le tocan a este nivel.
            var reqConTipo = await (
                from r in ctx.GthRequerimiento
                where solicitudIds.Contains(r.GthSolicitudId) && r.State
                join t in ctx.GthTipoRequerimiento on r.GthTipoRequerimientoId equals t.GthTipoRequerimientoId
                orderby r.GthRequerimientoId
                select new { Req = r, TipoCodigo = t.Codigo }).ToListAsync();

            // El detalle se lee ANTES de repartir por ruta: es lo que decide la de una vacante FFT
            // —solo las que quedaron enganchadas a la aprobación de antes del salto tienen fila y
            // siguen esperando la firma de Gerencia General (ver RutaAprobacion.De).
            var idsVivos = cabeceras.Select(c => c.Aprobacion.GthAprobacionGgId).ToList();
            var detalles = await ctx.GthAprobacionGgDetalle
                .Where(d => idsVivos.Contains(d.GthAprobacionGgId) && d.State)
                .ToListAsync();
            var conDetalle = detalles.Select(d => d.GthRequerimientoId).ToHashSet();

            // Solo las de la ruta y el turno de este nivel: aprobar "toda la fila" desde la lista
            // significa aprobar todo lo que a ESTE usuario le toca de ella AHORA, no las vacantes
            // de los demás ni las que todavía esperan la firma anterior (ver LeTocaAhora). La
            // llave del detalle es el par (aprobación, requerimiento) como en GetBandeja: un
            // requerimiento puede tener detalle de otra aprobación de la misma solicitud.
            var detallePorPar = detalles
                .ToDictionary(d => (d.GthAprobacionGgId, d.GthRequerimientoId));
            var aprobacionPorSolicitud = cabeceras
                .ToDictionary(c => c.Solicitud.GthSolicitudId, c => c.Aprobacion.GthAprobacionGgId);

            var reqPorSolicitud = reqConTipo
                .Where(x =>
                {
                    var aprId = aprobacionPorSolicitud.GetValueOrDefault(x.Req.GthSolicitudId);
                    detallePorPar.TryGetValue((aprId, x.Req.GthRequerimientoId), out var d);
                    return RutaAprobacion.LeTocaAhora(
                        RutaAprobacion.De(x.Req.EsFft, x.TipoCodigo,
                                          conDetalle.Contains(x.Req.GthRequerimientoId)),
                        scope.Nivel, d?.AprobadoGerenteArea, d?.AprobadoGth);
                })
                .GroupBy(x => x.Req.GthSolicitudId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Req).ToList());

            // Las que este nivel va a decidir de verdad, en una sola lista: es sobre estas que se
            // resuelve el salto FFT, no sobre todas las de las solicitudes. Una vacante de ingreso
            // directo nueva no se decide acá (ya está en manos de GTH), así que preguntar por su
            // puesto sería un roundtrip para nada.
            var requerimientos = reqPorSolicitud.Values.SelectMany(v => v).ToList();

            var detallePorAprobacion = detalles
                .GroupBy(d => d.GthAprobacionGgId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Estados destino del requerimiento. Los mueve el nivel que completa la ruta de la
            // vacante (ver RegistrarDecision), así que hacen falta siempre y no solo con el GG.
            // Las vacantes FFT aprobadas van directo al EMO de ingreso, no a VALIDACION_GTH.
            GthEstadoRequerimiento validacionGth, rechazadoGg;
            FftFlujo.Catalogo? catalogoFft = null;
            var puestoIdsFft = requerimientos.Where(r => r.EsFft).Select(r => r.PuestoId).Distinct().ToList();
            var nombrePorPuesto = new Dictionary<int, string>();
            var yaConCandidato = new HashSet<int>();
            {
                var estadosReq = await ctx.GthEstadoRequerimiento
                    .Where(e => e.State && (e.Codigo == EstadoReclutamiento.ValidacionGth
                                            || e.Codigo == EstadoReclutamiento.RechazadoGg))
                    .ToListAsync();
                validacionGth = estadosReq.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.ValidacionGth)
                    ?? throw new AbrilException("No está configurado el estado VALIDACION_GTH de reclutamiento.", 500);
                rechazadoGg = estadosReq.FirstOrDefault(e => e.Codigo == EstadoReclutamiento.RechazadoGg)
                    ?? throw new AbrilException("No está configurado el estado RECHAZADO_GG de reclutamiento.", 500);

                if (esGg && puestoIdsFft.Count > 0)
                {
                    catalogoFft = await FftFlujo.CargarCatalogoAsync(ctx);

                    // Snapshot del puesto para las fichas de candidato FFT y guardia de idempotencia:
                    // una sola consulta cada uno para todo el lote, aunque toque varias solicitudes.
                    nombrePorPuesto = await ctx.Puesto
                        .Where(p => puestoIdsFft.Contains(p.PuestoId))
                        .ToDictionaryAsync(p => p.PuestoId, p => p.Nombre);
                    yaConCandidato = await FftFlujo.RequerimientosConCandidatoAsync(
                        ctx, requerimientos.Where(r => r.EsFft).Select(r => r.GthRequerimientoId).ToList());
                }
            }

            // Estado destino de la casilla: en bloque la decisión es la misma para todas las
            // vacantes de la solicitud, así que nunca queda una aprobación parcial.
            var codigoDestino = aprobado ? AprobacionGgEstadoCodigo.Aprobada : AprobacionGgEstadoCodigo.Rechazada;
            var estadoDestino = estados.FirstOrDefault(e => e.Codigo == codigoDestino && e.State)
                ?? throw new AbrilException($"No está configurado el estado {codigoDestino} de la aprobación.", 500);

            var comentarioLimpio = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
            var now = DateTimeOffset.UtcNow;

            // 3) La decisión, solicitud por solicitud pero sin volver a la BD.
            var decididas = new List<(GthAprobacionGg Aprobacion, GthSolicitud Solicitud, int Vacantes)>();

            // Qué vacantes quedaron COMPLETAMENTE aprobadas en cada solicitud: son las únicas que
            // salen en el correo a GTH. En un reemplazo, la primera de las dos firmas no completa
            // nada todavía.
            var completadasPorAprobacion = new Dictionary<int, HashSet<int>>();

            // Y cuáles quedaron aprobadas esperando la firma que sigue: son las que le abren el
            // turno a GTH y las que lleva su correo (ver RegistrarDecision).
            var esperandoPorAprobacion = new Dictionary<int, HashSet<int>>();

            foreach (var c in cabeceras)
            {
                var aprobacion = c.Aprobacion;
                var solicitud  = c.Solicitud;

                // Alcance: acá no corta con 403 —el lote sigue— pero tampoco deja pasar una
                // solicitud de otra gerencia. Es la misma regla de EnsureAlcance, como omisión.
                if (!scope.Alcanza(solicitud.AreaScopeId))
                {
                    resultado.Omitidas.Add(new AprobacionGgDecisionOmitidaDto
                    {
                        AprobacionId = aprobacion.GthAprobacionGgId,
                        Motivo       = "Pertenece a otra área y no está dentro de tu alcance.",
                    });
                    continue;
                }

                // Cada nivel decide UNA sola vez, y solo se mira SU casilla: que otro nivel ya haya
                // decidido no impide registrar la propia.
                var estadoActualId = EstadoDeMiCasilla(aprobacion, scope.Nivel);
                var codigoActual = estados.FirstOrDefault(e => e.GthAprobacionGgEstadoId == estadoActualId)?.Codigo;
                if (codigoActual != AprobacionGgEstadoCodigo.Pendiente)
                {
                    resultado.Omitidas.Add(new AprobacionGgDecisionOmitidaDto
                    {
                        AprobacionId = aprobacion.GthAprobacionGgId,
                        Motivo       = YaDecidio(scope.Nivel),
                    });
                    continue;
                }

                // `reqPorSolicitud` ya viene filtrado por ruta: si acá no hay nada es que esta
                // solicitud no tiene vacantes de este nivel (p. ej. el GG seleccionó una de puros
                // reemplazos), y hay que decirlo en vez de dejarla desaparecer del conteo.
                if (!reqPorSolicitud.TryGetValue(solicitud.GthSolicitudId, out var vacantes) || vacantes.Count == 0)
                {
                    resultado.Omitidas.Add(new AprobacionGgDecisionOmitidaDto
                    {
                        AprobacionId = aprobacion.GthAprobacionGgId,
                        Motivo       = "No tiene vacantes que te toque aprobar a ti.",
                    });
                    continue;
                }

                var completadas = new HashSet<int>();
                completadasPorAprobacion[aprobacion.GthAprobacionGgId] = completadas;

                var esperando = new HashSet<int>();
                esperandoPorAprobacion[aprobacion.GthAprobacionGgId] = esperando;

                detallePorAprobacion.TryGetValue(aprobacion.GthAprobacionGgId, out var detallesDeEsta);

                // Todas las vacantes de la fila reciben la misma decisión: es justamente lo que
                // significa aprobar o rechazar la fila completa desde la lista.
                foreach (var r in vacantes)
                {
                    var detalle = detallesDeEsta?.FirstOrDefault(d => d.GthRequerimientoId == r.GthRequerimientoId);
                    if (detalle == null)
                    {
                        detalle = new GthAprobacionGgDetalle
                        {
                            GthAprobacionGgId  = aprobacion.GthAprobacionGgId,
                            GthRequerimientoId = r.GthRequerimientoId,
                            CreatedDateTime    = now,
                            CreatedUserId      = userId,
                            Active             = true,
                            State              = true,
                        };
                        ctx.GthAprobacionGgDetalle.Add(detalle);
                    }
                    else
                    {
                        detalle.UpdatedDateTime = now;
                        detalle.UpdatedUserId   = userId;
                    }

                    // Solo la columna del nivel que decide: las de los otros quedan como estén. Va
                    // antes de mover el requerimiento, igual que en la decisión de una.
                    switch (scope.Nivel)
                    {
                        case AprobacionNivel.GerenteGeneral:
                            detalle.AprobadoGerenteGeneral         = aprobado;
                            detalle.GerenteGeneralDecididoDateTime = now;
                            break;
                        case AprobacionNivel.GerenteArea:
                            detalle.AprobadoGerenteArea         = aprobado;
                            detalle.GerenteAreaDecididoDateTime = now;
                            break;
                        case AprobacionNivel.Gth:
                            detalle.AprobadoGth         = aprobado;
                            detalle.GthDecididoDateTime = now;
                            break;
                    }

                    // Un rechazo corta la vacante al toque; una aprobación la mueve solo cuando ya
                    // no falta ninguna firma de su ruta (ver RegistrarDecision).
                    var completa = !aprobado || esGg
                        || (detalle.AprobadoGerenteArea == true && detalle.AprobadoGth == true);
                    if (!completa)
                    {
                        esperando.Add(r.GthRequerimientoId);
                        continue;
                    }

                    if (aprobado) completadas.Add(r.GthRequerimientoId);

                    if (aprobado && r.EsFft)
                    {
                        // Igual que en la decisión de una: el FFT aprobado va derecho al EMO de
                        // ingreso, con su candidato ya seleccionado y su ficha abierta (ver FftFlujo).
                        await FftFlujo.AbrirIngresoDirectoAsync(
                            ctx, r, catalogoFft!,
                            nombrePorPuesto.GetValueOrDefault(r.PuestoId), yaConCandidato, userId, now);
                    }
                    else
                    {
                        r.GthEstadoRequerimientoId = aprobado
                            ? validacionGth.GthEstadoRequerimientoId
                            : rechazadoGg.GthEstadoRequerimientoId;
                        r.UpdatedDateTime = now;
                        r.UpdatedUserId   = userId;
                    }
                }

                switch (scope.Nivel)
                {
                    case AprobacionNivel.GerenteGeneral:
                        aprobacion.EstadoGerenteGeneralId         = estadoDestino.GthAprobacionGgEstadoId;
                        aprobacion.GerenteGeneralDecididoDateTime = now;
                        aprobacion.GerenteGeneralDecididoUserId   = userId;
                        aprobacion.GerenteGeneralComentario       = comentarioLimpio;
                        break;
                    case AprobacionNivel.GerenteArea:
                        aprobacion.EstadoGerenteAreaId         = estadoDestino.GthAprobacionGgEstadoId;
                        aprobacion.GerenteAreaDecididoDateTime = now;
                        aprobacion.GerenteAreaDecididoUserId   = userId;
                        aprobacion.GerenteAreaComentario       = comentarioLimpio;
                        break;
                    case AprobacionNivel.Gth:
                        aprobacion.EstadoGthId         = estadoDestino.GthAprobacionGgEstadoId;
                        aprobacion.GthDecididoDateTime = now;
                        aprobacion.GthDecididoUserId   = userId;
                        aprobacion.GthComentario       = comentarioLimpio;
                        break;
                }

                aprobacion.UpdatedDateTime = now;
                aprobacion.UpdatedUserId   = userId;

                decididas.Add((aprobacion, solicitud, vacantes.Count));
            }

            if (decididas.Count == 0) return resultado;

            await ctx.SaveChangesAsync();

            // 4) Contexto de cada decisión registrada. Los datos legibles (vacantes, solicitante,
            //    visto bueno del área) solo los consume el correo del Gerente General, así que en el
            //    visto bueno del gerente del área no se consulta nada más: se devuelven las
            //    cabeceras con sus conteos y listo.
            var aprobacionIdsDecididas = decididas.Select(d => d.Aprobacion.GthAprobacionGgId).ToList();

            // Los datos legibles hacen falta siempre que haya algo que mandar, y eso ya no es
            // exclusivo del Gerente General: la firma de GTH completa un reemplazo (correo a GTH
            // con lo aprobado) y la del gerente del área le abre el turno (correo a GTH pidiendo
            // la suya), así que las dos necesitan las vacantes con sus datos.
            var hayCompletadas = completadasPorAprobacion.Values.Any(v => v.Count > 0);
            var hayEsperando   = esperandoPorAprobacion.Values.Any(v => v.Count > 0);
            var vacantesPorAprobacion = hayCompletadas || hayEsperando
                ? await QueryVacantesDeVarias(ctx, aprobacionIdsDecididas)
                : new Dictionary<int, List<AprobacionGgVacanteDto>>();

            var nombresPorUser = new Dictionary<int, string?>();
            var resumenAreaPorAprobacion = new Dictionary<int, string?>();
            if (hayCompletadas || hayEsperando)
            {
                // Nombres del solicitante de cada solicitud y del gerente de área que ya dio su
                // visto bueno, en una sola consulta a person para todos los usuarios del lote.
                var userIds = decididas
                    .Select(d => d.Solicitud.SolicitanteUserId)
                    .Concat(decididas.Select(d => d.Aprobacion.GerenteAreaDecididoUserId))
                    .Where(id => id.HasValue)
                    .Distinct()
                    .ToList();

                if (userIds.Count > 0)
                {
                    var filas = await ctx.Person.AsNoTracking()
                        .Where(p => userIds.Contains(p.UserId))
                        .Select(p => new { p.UserId, p.FullName })
                        .ToListAsync();
                    foreach (var f in filas)
                        if (f.UserId.HasValue) nombresPorUser[f.UserId.Value] = f.FullName;
                }

                // "Aprobada parcialmente — Juan Pérez": lo mismo que BuildGerenteAreaResumen, pero
                // con el catálogo y los nombres ya en memoria. Null si el área nunca opinó.
                foreach (var d in decididas)
                {
                    if (!d.Aprobacion.GerenteAreaDecididoDateTime.HasValue) continue;

                    var estadoNombre = estados
                        .FirstOrDefault(e => e.GthAprobacionGgEstadoId == d.Aprobacion.EstadoGerenteAreaId)?.Nombre;
                    if (string.IsNullOrWhiteSpace(estadoNombre)) continue;

                    var quien = d.Aprobacion.GerenteAreaDecididoUserId.HasValue
                        ? nombresPorUser.GetValueOrDefault(d.Aprobacion.GerenteAreaDecididoUserId.Value)
                        : null;

                    resumenAreaPorAprobacion[d.Aprobacion.GthAprobacionGgId] =
                        string.IsNullOrWhiteSpace(quien) ? estadoNombre : $"{estadoNombre} — {quien}";
                }
            }

            foreach (var d in decididas)
            {
                var vacantes = vacantesPorAprobacion.GetValueOrDefault(d.Aprobacion.GthAprobacionGgId)
                               ?? new List<AprobacionGgVacanteDto>();
                var completas = completadasPorAprobacion.GetValueOrDefault(d.Aprobacion.GthAprobacionGgId)
                                ?? new HashSet<int>();
                var esperan = esperandoPorAprobacion.GetValueOrDefault(d.Aprobacion.GthAprobacionGgId)
                              ?? new HashSet<int>();

                resultado.Registradas.Add(new AprobacionGgDecisionContextoDto
                {
                    Resultado = new AprobacionGgDecisionResultDto
                    {
                        Nivel        = scope.Nivel,
                        EstadoCodigo = estadoDestino.Codigo,
                        EstadoNombre = estadoDestino.Nombre,
                        Aprobados    = aprobado ? d.Vacantes : 0,
                        Rechazados   = aprobado ? 0 : d.Vacantes,
                    },
                    SolicitudId        = d.Solicitud.GthSolicitudId,
                    AprobacionId       = d.Aprobacion.GthAprobacionGgId,
                    Area               = d.Solicitud.AreaNombre,
                    SolicitanteNombre  = d.Solicitud.SolicitanteUserId.HasValue
                        ? nombresPorUser.GetValueOrDefault(d.Solicitud.SolicitanteUserId.Value)
                        : null,
                    Justificacion      = d.Solicitud.Justificacion,
                    SustentoNombre     = d.Solicitud.SustentoNombre,
                    SustentoUrl        = d.Solicitud.SustentoUrl,
                    Comentario         = comentarioLimpio,
                    GerenteAreaResumen = resumenAreaPorAprobacion.GetValueOrDefault(d.Aprobacion.GthAprobacionGgId),
                    Aprobadas          = vacantes.Where(v => completas.Contains(v.RequerimientoId)).ToList(),
                    EsperandoSiguienteFirma = vacantes
                        .Where(v => esperan.Contains(v.RequerimientoId)).ToList(),
                });
            }

            return resultado;
        }

        /// <summary>
        /// Igual que <see cref="QueryVacantes"/> pero para varias aprobaciones en un solo roundtrip,
        /// agrupadas por aprobación. La llave del join al detalle es el par (aprobación,
        /// requerimiento), como en <see cref="GetBandeja"/>: filtrar solo por requerimiento traería
        /// el detalle de otra aprobación de la misma solicitud.
        /// </summary>
        private static async Task<Dictionary<int, List<AprobacionGgVacanteDto>>> QueryVacantesDeVarias(
            AppDbContext ctx, List<int> aprobacionIds)
        {
            var filas = await (
                from a in ctx.GthAprobacionGg.AsNoTracking()
                where aprobacionIds.Contains(a.GthAprobacionGgId)
                join r in ctx.GthRequerimiento.AsNoTracking() on a.GthSolicitudId equals r.GthSolicitudId
                where r.State
                join p in ctx.Puesto.AsNoTracking() on r.PuestoId equals p.PuestoId
                join t in ctx.GthTipoRequerimiento.AsNoTracking() on r.GthTipoRequerimientoId equals t.GthTipoRequerimientoId
                join pr in ctx.Project.AsNoTracking() on r.ProjectId equals pr.ProjectId
                join d in ctx.GthAprobacionGgDetalle.AsNoTracking().Where(x => x.State)
                    on new { A = a.GthAprobacionGgId, R = r.GthRequerimientoId }
                    equals new { A = d.GthAprobacionGgId, R = d.GthRequerimientoId } into detalleJoin
                from d in detalleJoin.DefaultIfEmpty()
                join wr in ctx.Worker.AsNoTracking() on r.ReemplazaWorkerId equals (int?)wr.Id into reemplazaJoin
                from wr in reemplazaJoin.DefaultIfEmpty()
                join td in ctx.GthTipoDocumento.AsNoTracking() on r.GthTipoDocumentoId equals (int?)td.GthTipoDocumentoId into tipoDocJoin
                from td in tipoDocJoin.DefaultIfEmpty()
                orderby r.GthRequerimientoId
                select new
                {
                    a.GthAprobacionGgId,
                    Vacante = new AprobacionGgVacanteDto
                    {
                        RequerimientoId        = r.GthRequerimientoId,
                        Codigo                 = r.Codigo,
                        Puesto                 = p.Nombre,
                        TipoRequerimiento      = t.Nombre,
                        TipoRequerimientoCodigo = t.Codigo,
                        TrabajadorReemplazado  = wr == null ? null
                            : (wr.Person != null ? wr.Person.FullName : wr.ApellidoNombre),
                        ProyectoObra           = pr.ProjectDescription,
                        SalarioBrutoMensual    = r.SalarioBrutoMensual,
                        // Los datos del ingreso directo también acá: sin ellos, una decisión en
                        // bloque veía todas sus vacantes como normales y el correo FFT no salía.
                        EsFft                  = r.EsFft,
                        FftCandidatoNombre     = r.FftCandidatoNombre,
                        FftCandidatoDocumento  = r.FftCandidatoDocumento,
                        FftTipoDocumento       = td != null ? td.Nombre : null,
                        FftCandidatoCorreo     = r.FftCandidatoCorreo,
                        FftEnAprobacionLegada  = d != null,
                        AprobadoGerenteArea    = d != null ? d.AprobadoGerenteArea : null,
                        AprobadoGerenteGeneral = d != null ? d.AprobadoGerenteGeneral : null,
                    },
                }).ToListAsync();

            return filas
                .GroupBy(f => f.GthAprobacionGgId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Vacante).ToList());
        }

        /// <summary>
        /// Frase legible con lo que el gerente del área dejó registrado ("Aprobada parcialmente —
        /// Juan Pérez"). Null si nunca opinó: en ese caso el correo a GTH no dice nada del área en
        /// vez de afirmar que está pendiente, que no aporta.
        /// </summary>
        private static async Task<string?> BuildGerenteAreaResumen(AppDbContext ctx, GthAprobacionGg aprobacion)
        {
            if (!aprobacion.GerenteAreaDecididoDateTime.HasValue) return null;

            var estado = await ctx.GthAprobacionGgEstado.AsNoTracking()
                .Where(e => e.GthAprobacionGgEstadoId == aprobacion.EstadoGerenteAreaId)
                .Select(e => e.Nombre)
                .FirstOrDefaultAsync();

            var quien = aprobacion.GerenteAreaDecididoUserId.HasValue
                ? await ctx.Person.AsNoTracking()
                    .Where(p => p.UserId == aprobacion.GerenteAreaDecididoUserId.Value)
                    .Select(p => p.FullName)
                    .FirstOrDefaultAsync()
                : null;

            if (string.IsNullOrWhiteSpace(estado)) return null;
            return string.IsNullOrWhiteSpace(quien) ? estado : $"{estado} — {quien}";
        }

        public async Task<AprobacionGgResumenDto?> GetResumenByRequerimiento(int requerimientoId)
        {
            using var ctx = _factory.CreateDbContext();

            // Left join al detalle: la aprobación es de la solicitud completa, pero la tarjeta
            // muestra además la decisión de ESTA vacante en cada nivel.
            var raw = await (
                from r in ctx.GthRequerimiento
                where r.GthRequerimientoId == requerimientoId && r.State
                join a in ctx.GthAprobacionGg on r.GthSolicitudId equals a.GthSolicitudId
                where a.State
                join egg in ctx.GthAprobacionGgEstado on a.EstadoGerenteGeneralId equals egg.GthAprobacionGgEstadoId
                join ega in ctx.GthAprobacionGgEstado on a.EstadoGerenteAreaId equals ega.GthAprobacionGgEstadoId
                join egt in ctx.GthAprobacionGgEstado on a.EstadoGthId equals egt.GthAprobacionGgEstadoId
                join t in ctx.GthTipoRequerimiento on r.GthTipoRequerimientoId equals t.GthTipoRequerimientoId
                join d in ctx.GthAprobacionGgDetalle.Where(x => x.State)
                    on new { A = a.GthAprobacionGgId, R = r.GthRequerimientoId }
                    equals new { A = d.GthAprobacionGgId, R = d.GthRequerimientoId } into detalleJoin
                from d in detalleJoin.DefaultIfEmpty()
                select new
                {
                    GgEstadoCodigo = egg.Codigo,
                    GgEstadoNombre = egg.Nombre,
                    GaEstadoCodigo = ega.Codigo,
                    GaEstadoNombre = ega.Nombre,
                    GtEstadoCodigo = egt.Codigo,
                    GtEstadoNombre = egt.Nombre,
                    AprobadoGg     = d != null ? d.AprobadoGerenteGeneral : null,
                    AprobadoGa     = d != null ? d.AprobadoGerenteArea : null,
                    AprobadoGt     = d != null ? d.AprobadoGth : null,
                    a.GerenteGeneralComentario,
                    a.GerenteAreaComentario,
                    a.GthComentario,
                    a.EnviadoDateTime,
                    a.GerenteGeneralDecididoDateTime,
                    a.GerenteAreaDecididoDateTime,
                    a.GthDecididoDateTime,
                    r.EsFft,
                    TipoCodigo = t.Codigo,
                    TieneDetalle = d != null,
                }).FirstOrDefaultAsync();

            if (raw == null) return null;

            // Un ingreso directo no pasa por ninguna firma: la tarjeta de aprobaciones del
            // seguimiento no aplica y devolver la de la solicitud lo dejaría "pendiente" para
            // siempre. Pasa en las solicitudes mixtas, donde la aprobación existe pero es de las
            // OTRAS vacantes. Los FFT anteriores al salto sí tienen su fila de detalle y siguen
            // mostrando la firma que les tocó (ver RutaAprobacion.De).
            var ruta = RutaAprobacion.De(raw.EsFft, raw.TipoCodigo, raw.TieneDetalle);
            if (ruta == RutaAprobacion.Ninguna) return null;

            // Conversión a hora Perú en memoria (ToOffset no se traduce a SQL).
            return new AprobacionGgResumenDto
            {
                EstadoCodigo            = raw.GgEstadoCodigo,
                EstadoNombre            = raw.GgEstadoNombre,
                Aprobado                = raw.AprobadoGg,
                GerenteAreaEstadoCodigo = raw.GaEstadoCodigo,
                GerenteAreaEstadoNombre = raw.GaEstadoNombre,
                AprobadoGerenteArea     = raw.AprobadoGa,
                GthEstadoCodigo         = raw.GtEstadoCodigo,
                GthEstadoNombre         = raw.GtEstadoNombre,
                AprobadoGth             = raw.AprobadoGt,
                Comentario              = raw.GerenteGeneralComentario,
                GerenteAreaComentario   = raw.GerenteAreaComentario,
                GthComentario           = raw.GthComentario,
                EnviadoEn               = raw.EnviadoDateTime?.ToOffset(PeruOffset).DateTime,
                DecididoEn              = raw.GerenteGeneralDecididoDateTime?.ToOffset(PeruOffset).DateTime,
                GerenteAreaDecididoEn   = raw.GerenteAreaDecididoDateTime?.ToOffset(PeruOffset).DateTime,
                GthDecididoEn           = raw.GthDecididoDateTime?.ToOffset(PeruOffset).DateTime,
                // Con esto el seguimiento pinta solo las firmas que esta vacante necesita.
                Ruta                    = ruta,
            };
        }

        /// <summary>
        /// Corta con 403 si la solicitud no cae dentro del alcance del usuario. Mensaje explícito a
        /// propósito: el gerente tiene que entender que no es un error, es que no es su área.
        /// </summary>
        private static void EnsureAlcance(AprobacionScope scope, int? areaScopeId)
        {
            if (scope.Alcanza(areaScopeId)) return;

            throw new AbrilException(
                scope.PuedeDecidir
                    ? "Esta solicitud de personal pertenece a otra área y no está dentro de tu alcance."
                    : "Tu ficha de trabajador no te da acceso a las solicitudes de personal por aprobar.",
                403);
        }

        /// <summary>Resuelve el id de un estado de la aprobación por su código estable; 500 si no está sembrado.</summary>
        private static async Task<int> ResolveEstadoId(AppDbContext ctx, string codigo)
        {
            var id = await ctx.GthAprobacionGgEstado
                .Where(e => e.Codigo == codigo && e.State)
                .Select(e => (int?)e.GthAprobacionGgEstadoId)
                .FirstOrDefaultAsync();
            if (id == null)
                throw new AbrilException($"No está configurado el estado {codigo} de la aprobación.", 500);
            return id.Value;
        }
    }

    /// <summary>Códigos estables del estado de la aprobación (espejo de gth_aprobacion_gg_estado.codigo).</summary>
    internal static class AprobacionGgEstadoCodigo
    {
        public const string Pendiente       = "PENDIENTE";
        public const string Aprobada        = "APROBADA";
        public const string AprobadaParcial = "APROBADA_PARCIAL";
        public const string Rechazada       = "RECHAZADA";
    }
}
