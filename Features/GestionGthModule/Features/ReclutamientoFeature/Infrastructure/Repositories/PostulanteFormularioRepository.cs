using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared;
using Abril_Backend.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Repositories
{
    public class PostulanteFormularioRepository : IPostulanteFormularioRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public PostulanteFormularioRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        private static readonly TimeSpan PeruOffset = TimeSpan.FromHours(-5);

        // ── Público (postulante, por token) ───────────────────────────────────
        public async Task<PostulanteFormularioPublicoDto?> GetByToken(string token)
        {
            using var ctx = _factory.CreateDbContext();

            var f = await ctx.GthPostulanteFormulario.FirstOrDefaultAsync(x => x.Token == token && x.State);
            if (f == null) return null;

            var estado = await ctx.GthPostulanteFormularioEstado
                .FirstOrDefaultAsync(e => e.GthPostulanteFormularioEstadoId == f.GthPostulanteFormularioEstadoId);

            var head = await (
                from c in ctx.GthCandidato
                where c.GthCandidatoId == f.GthCandidatoId
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                select new { c.Nombre, Puesto = p.Nombre }).FirstOrDefaultAsync();

            var dto = new PostulanteFormularioPublicoDto
            {
                Puesto          = head?.Puesto ?? string.Empty,
                CandidatoNombre = head?.Nombre ?? string.Empty,
                EstadoCodigo    = estado?.Codigo ?? string.Empty,
                EstadoNombre    = estado?.Nombre ?? string.Empty,
                // Solo el APROBADO cierra el formulario. El RECHAZADO se reabre con las respuestas ya
                // cargadas para que el postulante corrija lo observado y lo vuelva a enviar.
                SoloLectura     = estado?.Codigo == EstadoFormularioPostulante.Aprobado,
                Observaciones   = estado?.Codigo == EstadoFormularioPostulante.Rechazado ? f.MotivoRechazo : null,
                Respuestas      = MapRespuestas(f),
                // Nombre con el que lo subió, no el de SharePoint: es lo único que le sirve al
                // postulante para reconocer su archivo. Sin url: el archivo no es público.
                CvNombre        = f.CvNombreOriginal ?? f.CvNombre,
            };

            dto.EstadosCiviles = await ctx.GthEstadoCivil.Where(x => x.State && x.Active).OrderBy(x => x.Orden)
                .Select(x => new OpcionDto { Id = x.GthEstadoCivilId, Nombre = x.Nombre }).ToListAsync();
            dto.TiposDocumento = await ctx.GthTipoDocumento.Where(x => x.State && x.Active).OrderBy(x => x.Orden)
                .Select(x => new TipoDocumentoOpcionDto { Id = x.GthTipoDocumentoId, Nombre = x.Nombre, Codigo = x.Codigo }).ToListAsync();
            dto.Universidades = await ctx.GthUniversidad.Where(x => x.State && x.Active).OrderBy(x => x.Orden)
                .Select(x => new OpcionDto { Id = x.GthUniversidadId, Nombre = x.Nombre }).ToListAsync();
            dto.GradosAcademicos = await ctx.GthGradoAcademico.Where(x => x.State && x.Active).OrderBy(x => x.Orden)
                .Select(x => new OpcionDto { Id = x.GthGradoAcademicoId, Nombre = x.Nombre }).ToListAsync();
            dto.Disponibilidades = await ctx.GthDisponibilidad.Where(x => x.State && x.Active).OrderBy(x => x.Orden)
                .Select(x => new OpcionDto { Id = x.GthDisponibilidadId, Nombre = x.Nombre }).ToListAsync();
            dto.MotivosCese = await ctx.GthMotivoCese.Where(x => x.State && x.Active).OrderBy(x => x.Orden)
                .Select(x => new OpcionDto { Id = x.GthMotivoCeseId, Nombre = x.Nombre }).ToListAsync();
            dto.Distritos = await ctx.GthDistrito.Where(x => x.State && x.Active).OrderBy(x => x.Orden)
                .Select(x => new DistritoOpcionDto { Id = x.GthDistritoId, Nombre = x.Nombre, Provincia = x.Provincia }).ToListAsync();

            return dto;
        }

        public async Task<PostulanteCvContextoDto?> GetCvContexto(string token)
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from f in ctx.GthPostulanteFormulario
                where f.Token == token && f.State
                join fe in ctx.GthPostulanteFormularioEstado
                    on f.GthPostulanteFormularioEstadoId equals fe.GthPostulanteFormularioEstadoId
                join c in ctx.GthCandidato on f.GthCandidatoId equals c.GthCandidatoId
                join req in ctx.GthRequerimiento on c.GthRequerimientoId equals req.GthRequerimientoId
                select new PostulanteCvContextoDto
                {
                    CandidatoId = f.GthCandidatoId,
                    Codigo      = req.Codigo,
                    TieneCv     = f.CvUrl != null,
                    SoloLectura = fe.Codigo == EstadoFormularioPostulante.Aprobado,
                }).FirstOrDefaultAsync();
        }

        public async Task<FormularioCompletadoContextoDto> GuardarRespuestasByToken(
            string token, PostulanteFormularioRespuestasDto r, PostulanteCvSubidaDto? cv)
        {
            using var ctx = _factory.CreateDbContext();

            var f = await ctx.GthPostulanteFormulario.FirstOrDefaultAsync(x => x.Token == token && x.State);
            if (f == null)
                throw new AbrilException("El enlace del formulario no es válido o ya no está disponible.", 404);

            var estados = await ctx.GthPostulanteFormularioEstado.Where(e => e.State).ToListAsync();
            var actual = estados.FirstOrDefault(e => e.GthPostulanteFormularioEstadoId == f.GthPostulanteFormularioEstadoId);
            // Un formulario RECHAZADO sí admite cambios: es justamente lo que se le pide al postulante
            // en el correo de rechazo. El APROBADO es el único que queda cerrado.
            if (actual?.Codigo == EstadoFormularioPostulante.Aprobado)
                throw new AbrilException("Este formulario ya fue aprobado por la empresa y no admite cambios.", 409);

            var completadoId = estados.FirstOrDefault(e => e.Codigo == EstadoFormularioPostulante.Completado)?.GthPostulanteFormularioEstadoId
                ?? throw new AbrilException("No está configurado el estado COMPLETADO del formulario del postulante.", 500);

            var esCorreccion = actual?.Codigo == EstadoFormularioPostulante.Rechazado;
            var now = DateTimeOffset.UtcNow;

            // Página 0
            f.ConsentimientoDatosPersonales = r.ConsentimientoDatosPersonales;
            // Página 1
            f.NombresCompletos        = Trim(r.NombresCompletos);
            f.FechaNacimiento         = r.FechaNacimiento;
            f.GthEstadoCivilId        = r.EstadoCivilId;
            f.GthTipoDocumentoId      = r.TipoDocumentoId;
            f.NumeroDocumento         = Trim(r.NumeroDocumento);
            f.GthDistritoId           = r.DistritoId;
            f.CorreoElectronico       = Trim(r.CorreoElectronico);
            f.NumeroCelular           = Trim(r.NumeroCelular);
            f.PretensionesSalariales  = Trim(r.PretensionesSalariales);
            f.GthDisponibilidadId     = r.DisponibilidadId;
            f.Linkedin                = Trim(r.Linkedin);
            f.PortafolioLink          = Trim(r.PortafolioLink);
            // Página 2
            f.Profesion               = Trim(r.Profesion);
            f.GthUniversidadId        = r.UniversidadId;
            f.GthGradoAcademicoId     = r.GradoAcademicoId;
            f.NumeroColegiatura       = Trim(r.NumeroColegiatura);
            // Página 3
            f.Empresa                          = Trim(r.Empresa);
            f.AreaTrabajo                       = Trim(r.AreaTrabajo);
            f.Cargo                             = Trim(r.Cargo);
            f.FechaInicio                       = r.FechaInicio;
            f.FechaTermino                      = r.FechaTermino;
            f.GthMotivoCeseId                   = r.MotivoCeseId;
            f.FuncionesPrincipales              = Trim(r.FuncionesPrincipales);
            f.Logros                            = Trim(r.Logros);
            f.IngresoBrutoMensual               = Trim(r.IngresoBrutoMensual);
            f.PersonasACargo                    = r.PersonasACargo;
            f.JefeInmediato                     = Trim(r.JefeInmediato);
            f.AutorizaVerificacionReferencias   = r.AutorizaVerificacionReferencias;
            // Página 4
            f.DeclaracionVeracidad   = r.DeclaracionVeracidad;
            f.ConfirmacionDocumentos = r.ConfirmacionDocumentos;

            // CV documentado: solo se pisa si el postulante adjuntó uno en ESTE envío. Si no
            // adjuntó nada se conserva el de un envío anterior — al corregir un formulario
            // observado no se le vuelve a pedir el archivo si ya estaba bien.
            if (cv != null)
            {
                f.CvNombre         = cv.Nombre;
                f.CvNombreOriginal = cv.NombreOriginal;
                f.CvUrl            = cv.Url;
                f.CvItemId         = cv.ItemId;
                f.CvDriveId        = cv.DriveId;
            }

            f.GthPostulanteFormularioEstadoId = completadoId;
            f.CompletadoDateTime = now;
            f.UpdatedDateTime    = now;

            await ctx.SaveChangesAsync();

            // Cabecera del proceso para el aviso a GTH: se lee acá y no en el servicio para no
            // abrir una segunda conexión por un envío que ya está resuelto.
            var head = await (
                from c in ctx.GthCandidato
                where c.GthCandidatoId == f.GthCandidatoId
                join req in ctx.GthRequerimiento on c.GthRequerimientoId equals req.GthRequerimientoId
                join p in ctx.Puesto on req.PuestoId equals p.PuestoId
                join pr in ctx.Project on req.ProjectId equals pr.ProjectId
                select new
                {
                    req.GthRequerimientoId,
                    req.Codigo,
                    Puesto       = p.Nombre,
                    Area         = req.Solicitud!.AreaNombre,
                    ProyectoObra = pr.ProjectDescription,
                    c.Nombre,
                }).FirstOrDefaultAsync();

            return new FormularioCompletadoContextoDto
            {
                Codigo           = head?.Codigo ?? string.Empty,
                Puesto           = head?.Puesto ?? string.Empty,
                Area             = head?.Area,
                ProyectoObra     = head?.ProyectoObra,
                RequerimientoId  = head?.GthRequerimientoId ?? 0,
                CandidatoId      = f.GthCandidatoId,
                CandidatoNombre  = f.NombresCompletos ?? head?.Nombre ?? string.Empty,
                CorreoPostulante = f.CorreoElectronico,
                NumeroCelular    = f.NumeroCelular,
                CompletadoEn     = now.ToOffset(PeruOffset).DateTime,
                EsCorreccion     = esCorreccion,
            };
        }

        // ── GTH (enviar / revisar / decidir) ──────────────────────────────────
        public async Task<EnviarFormularioContextoDto> PrepararEnvio(int candidatoId, string correo, string nuevoToken, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var cand = await (
                from c in ctx.GthCandidato
                where c.GthCandidatoId == candidatoId && c.State
                join est in ctx.GthCandidatoEstado on c.GthCandidatoEstadoId equals est.GthCandidatoEstadoId
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                select new { c.Nombre, EstadoCandidato = est.Codigo, Puesto = p.Nombre }).FirstOrDefaultAsync();
            if (cand == null)
                throw new AbrilException("Candidato no encontrado.", 404);
            if (cand.EstadoCandidato != EstadoCandidato.Aprobado)
                throw new AbrilException("El formulario solo se envía a candidatos aprobados por el solicitante.", 400);

            var estados = await ctx.GthPostulanteFormularioEstado.Where(e => e.State).ToListAsync();
            var enviado = estados.FirstOrDefault(e => e.Codigo == EstadoFormularioPostulante.Enviado)
                ?? throw new AbrilException("No está configurado el estado ENVIADO del formulario del postulante.", 500);

            var now = DateTimeOffset.UtcNow;

            var f = await ctx.GthPostulanteFormulario.FirstOrDefaultAsync(x => x.GthCandidatoId == candidatoId && x.State);

            var contexto = AplicarEnvio(ctx, f, candidatoId, correo, nuevoToken, cand.Nombre, cand.Puesto,
                                        estados, enviado, userId, now);

            await ctx.SaveChangesAsync();
            return contexto;
        }

        public async Task<List<EnvioMasivoPreparadoDto>> PrepararEnvioMasivo(
            IReadOnlyList<EnvioMasivoSolicitudDto> solicitudes, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            // El mismo candidato repetido en el lote sería un doble envío y, si aún no tenía formulario,
            // una segunda fila para el mismo candidato (que rompería el FirstOrDefault del resto de
            // consultas). Se queda el primero.
            var solicitudesUnicas = solicitudes
                .GroupBy(s => s.CandidatoId)
                .Select(g => g.First())
                .ToList();

            var ids = solicitudesUnicas.Select(s => s.CandidatoId).ToList();

            // Todo el lote se resuelve con 3 consultas + 1 SaveChanges, sin importar cuántos candidatos
            // vengan: candidatos con su estado y puesto, catálogo de estados del formulario y los
            // formularios ya existentes.
            var candidatos = await (
                from c in ctx.GthCandidato
                where ids.Contains(c.GthCandidatoId) && c.State
                join est in ctx.GthCandidatoEstado on c.GthCandidatoEstadoId equals est.GthCandidatoEstadoId
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                select new { c.GthCandidatoId, c.Nombre, EstadoCandidato = est.Codigo, Puesto = p.Nombre })
                .ToListAsync();

            var estados = await ctx.GthPostulanteFormularioEstado.Where(e => e.State).ToListAsync();
            var enviado = estados.FirstOrDefault(e => e.Codigo == EstadoFormularioPostulante.Enviado)
                ?? throw new AbrilException("No está configurado el estado ENVIADO del formulario del postulante.", 500);

            var formularios = await ctx.GthPostulanteFormulario
                .Where(x => ids.Contains(x.GthCandidatoId) && x.State)
                .ToListAsync();

            // TryAdd y no ToDictionary: una fila repetida por candidato no debería existir, pero si la
            // hubiera tumbaría el lote entero en vez de resolver el envío con la primera, que es lo que
            // hace el envío individual (FirstOrDefault).
            var candidatoPorId = new Dictionary<int, (string Nombre, string EstadoCandidato, string Puesto)>();
            foreach (var c in candidatos)
                candidatoPorId.TryAdd(c.GthCandidatoId, (c.Nombre, c.EstadoCandidato, c.Puesto));

            var formularioPorCandidato = new Dictionary<int, GthPostulanteFormulario>();
            foreach (var f in formularios)
                formularioPorCandidato.TryAdd(f.GthCandidatoId, f);

            var now = DateTimeOffset.UtcNow;
            var preparados = new List<EnvioMasivoPreparadoDto>(solicitudesUnicas.Count);

            foreach (var s in solicitudesUnicas)
            {
                // Un candidato inválido no tumba el lote: se reporta y los demás siguen su curso.
                if (!candidatoPorId.TryGetValue(s.CandidatoId, out var cand))
                {
                    preparados.Add(new EnvioMasivoPreparadoDto
                    {
                        CandidatoId = s.CandidatoId,
                        Error       = "Candidato no encontrado.",
                    });
                    continue;
                }

                if (cand.EstadoCandidato != EstadoCandidato.Aprobado)
                {
                    preparados.Add(new EnvioMasivoPreparadoDto
                    {
                        CandidatoId = s.CandidatoId,
                        Error       = "El formulario solo se envía a candidatos aprobados por el solicitante.",
                    });
                    continue;
                }

                formularioPorCandidato.TryGetValue(s.CandidatoId, out var existente);

                preparados.Add(new EnvioMasivoPreparadoDto
                {
                    CandidatoId = s.CandidatoId,
                    Contexto = AplicarEnvio(ctx, existente, s.CandidatoId, s.Correo, s.NuevoToken,
                                            cand.Nombre, cand.Puesto, estados, enviado, userId, now),
                });
            }

            // Un solo guardado para todo el lote.
            await ctx.SaveChangesAsync();
            return preparados;
        }

        /// <summary>
        /// Reglas del envío del formulario a un candidato: crea el formulario si no existía o actualiza
        /// el que ya estaba, y devuelve el contexto del correo. No guarda —el llamador decide cuándo
        /// hacer el SaveChanges— para que un lote se resuelva en un único guardado. Es el único lugar
        /// donde viven estas transiciones: lo comparten el envío individual y el masivo.
        /// </summary>
        private static EnviarFormularioContextoDto AplicarEnvio(
            AppDbContext ctx,
            GthPostulanteFormulario? f,
            int candidatoId,
            string correo,
            string nuevoToken,
            string candidatoNombre,
            string puesto,
            List<GthPostulanteFormularioEstado> estados,
            GthPostulanteFormularioEstado enviado,
            int? userId,
            DateTimeOffset now)
        {
            // Estado en el que queda el formulario tras el envío: ENVIADO salvo cuando se reenvía uno
            // rechazado, que se queda como está (ver abajo).
            var destino = enviado;
            var esRechazo = false;

            // Sin fila de formulario todavía = es la primera vez que este candidato recibe el
            // enlace, y solo esa vez lleva el correo de bienvenida al proceso. Se calcula acá y no
            // en el return porque la rama de abajo reasigna `f` con el formulario recién creado.
            var esPrimerEnvio = f == null;

            if (f != null)
            {
                var actual = estados.FirstOrDefault(e => e.GthPostulanteFormularioEstadoId == f.GthPostulanteFormularioEstadoId);

                // Un formulario ya APROBADO también se puede reenviar (el postulante se equivocó en
                // algún dato): cae en el camino de abajo, que lo devuelve a ENVIADO conservando lo
                // que declaró y limpiando la aprobación. Deja de contar como aprobado hasta que lo
                // complete de nuevo y GTH lo vuelva a revisar; la pantalla lo confirma antes.

                // Solo cuenta como "rechazo con observaciones" el de un formulario que el postulante
                // sí completó. El que se rechazó porque nunca lo llenó (sin CompletadoDateTime) no
                // tiene nada que corregir: reenviarlo es volver a invitarlo, camino de abajo.
                esRechazo = actual?.Codigo == EstadoFormularioPostulante.Rechazado
                            && f.CompletadoDateTime != null;

                if (esRechazo)
                {
                    // Reenvío de un formulario observado: se conserva el estado RECHAZADO junto con el
                    // motivo y la revisión. Si se pasara a ENVIADO se borrarían las observaciones, y el
                    // postulante recibiría el correo de invitación —como si nunca lo hubieran rechazado—
                    // y abriría el formulario sin saber qué corregir. Solo se actualiza el rastro del envío.
                    destino = actual!;
                    f.CorreoEnvio     = correo;
                    f.EnviadoDateTime = now;
                    f.EnviadoUserId   = userId;
                    f.UpdatedDateTime = now;
                    f.UpdatedUserId   = userId;
                }
                else
                {
                    // Reenvío normal: vuelve a ENVIADO (conserva las respuestas para que el postulante
                    // corrija) y limpia la revisión previa. Conserva el token original del enlace.
                    f.GthPostulanteFormularioEstadoId = enviado.GthPostulanteFormularioEstadoId;
                    f.CorreoEnvio       = correo;
                    f.EnviadoDateTime   = now;
                    f.EnviadoUserId     = userId;
                    f.RevisadoUserId    = null;
                    f.RevisadoNombre    = null;
                    f.RevisadoDateTime  = null;
                    f.MotivoRechazo     = null;
                    f.CompletadoDateTime = null;
                    f.UpdatedDateTime   = now;
                    f.UpdatedUserId     = userId;
                }
            }
            else
            {
                f = new GthPostulanteFormulario
                {
                    GthCandidatoId                  = candidatoId,
                    Token                           = nuevoToken,
                    GthPostulanteFormularioEstadoId = enviado.GthPostulanteFormularioEstadoId,
                    CorreoEnvio                     = correo,
                    EnviadoDateTime                 = now,
                    EnviadoUserId                   = userId,
                    CreatedDateTime                 = now,
                    CreatedUserId                   = userId,
                    Active                          = true,
                    State                           = true,
                };
                ctx.GthPostulanteFormulario.Add(f);
            }

            return new EnviarFormularioContextoDto
            {
                Token  = f.Token,
                Puesto = puesto,
                // El nombre que declaró el propio postulante manda sobre el que registró GTH.
                CandidatoNombre = Trim(f.NombresCompletos) ?? candidatoNombre,
                Correo          = correo,
                EsRechazo       = esRechazo,
                EsPrimerEnvio   = esPrimerEnvio,
                Motivo          = esRechazo ? f.MotivoRechazo : null,
                Resumen = new CandidatoFormularioResumenDto
                {
                    EstadoCodigo = destino.Codigo,
                    EstadoNombre = destino.Nombre,
                    CorreoEnvio  = correo,
                    EnviadoEn    = now.ToOffset(PeruOffset).DateTime,
                    // Al reenviar un rechazo el formulario sigue completado y revisado: la bandeja debe
                    // seguir mostrando el badge "Rechazado" y quién lo revisó.
                    CompletadoEn   = f.CompletadoDateTime?.ToOffset(PeruOffset).DateTime,
                    RevisadoNombre = f.RevisadoNombre,
                    RevisadoEn     = f.RevisadoDateTime?.ToOffset(PeruOffset).DateTime,
                },
            };
        }

        public async Task<FormularioRevisionDto> GetRevision(int candidatoId)
        {
            using var ctx = _factory.CreateDbContext();

            // Nombre del candidato + el CV que GTH cargó en su long list: los dos salen de la
            // misma fila, así que van en el mismo roundtrip.
            var cand = await ctx.GthCandidato
                .Where(c => c.GthCandidatoId == candidatoId && c.State)
                .Select(c => new { c.Nombre, c.CvNombre, c.CvUrl })
                .FirstOrDefaultAsync();
            if (cand == null)
                throw new AbrilException("Candidato no encontrado.", 404);

            var candNombre = cand.Nombre;
            // Sin url no hay nada que abrir, así que no se sirve una tarjeta de CV vacía.
            var cvGth = cand.CvUrl == null ? null : new FormularioCvDto
            {
                Nombre = cand.CvNombre ?? "CV cargado por GTH",
                Url    = cand.CvUrl,
            };

            var row = await (
                from f in ctx.GthPostulanteFormulario
                where f.GthCandidatoId == candidatoId && f.State
                join fe in ctx.GthPostulanteFormularioEstado on f.GthPostulanteFormularioEstadoId equals fe.GthPostulanteFormularioEstadoId
                join ecv in ctx.GthEstadoCivil on f.GthEstadoCivilId equals ecv.GthEstadoCivilId into ecvJ
                from ecv in ecvJ.DefaultIfEmpty()
                join tdoc in ctx.GthTipoDocumento on f.GthTipoDocumentoId equals tdoc.GthTipoDocumentoId into tdocJ
                from tdoc in tdocJ.DefaultIfEmpty()
                join dist in ctx.GthDistrito on f.GthDistritoId equals dist.GthDistritoId into distJ
                from dist in distJ.DefaultIfEmpty()
                join disp in ctx.GthDisponibilidad on f.GthDisponibilidadId equals disp.GthDisponibilidadId into dispJ
                from disp in dispJ.DefaultIfEmpty()
                join uni in ctx.GthUniversidad on f.GthUniversidadId equals uni.GthUniversidadId into uniJ
                from uni in uniJ.DefaultIfEmpty()
                join grad in ctx.GthGradoAcademico on f.GthGradoAcademicoId equals grad.GthGradoAcademicoId into gradJ
                from grad in gradJ.DefaultIfEmpty()
                join mce in ctx.GthMotivoCese on f.GthMotivoCeseId equals mce.GthMotivoCeseId into mceJ
                from mce in mceJ.DefaultIfEmpty()
                select new
                {
                    F            = f,
                    EstadoCodigo = fe.Codigo,
                    EstadoNombre = fe.Nombre,
                    EstadoCivil  = ecv != null ? ecv.Nombre : null,
                    TipoDocumento = tdoc != null ? tdoc.Nombre : null,
                    Distrito     = dist != null ? dist.Nombre : null,
                    Disponibilidad = disp != null ? disp.Nombre : null,
                    Universidad  = uni != null ? uni.Nombre : null,
                    GradoAcademico = grad != null ? grad.Nombre : null,
                    MotivoCese   = mce != null ? mce.Nombre : null,
                }).FirstOrDefaultAsync();

            // Sin formulario todavía no hay CV del postulante, pero el de GTH sí existe: se sirve
            // igual para que el modal pueda abrirlo antes de enviarle el enlace al candidato.
            if (row == null)
                return new FormularioRevisionDto { Existe = false, CandidatoNombre = candNombre, CvGth = cvGth };

            var f2 = row.F;
            var dto = new FormularioRevisionDto
            {
                Existe          = true,
                EstadoCodigo    = row.EstadoCodigo,
                EstadoNombre    = row.EstadoNombre,
                CandidatoNombre = candNombre,
                CorreoEnvio     = f2.CorreoEnvio,
                EnviadoEn       = f2.EnviadoDateTime?.ToOffset(PeruOffset).DateTime,
                CompletadoEn    = f2.CompletadoDateTime?.ToOffset(PeruOffset).DateTime,
                RevisadoNombre  = f2.RevisadoNombre,
                RevisadoEn      = f2.RevisadoDateTime?.ToOffset(PeruOffset).DateTime,
                MotivoRechazo   = f2.MotivoRechazo,
                CvGth           = cvGth,
                CvPostulante    = f2.CvUrl == null ? null : new FormularioCvDto
                {
                    Nombre = f2.CvNombreOriginal ?? f2.CvNombre ?? "CV del postulante",
                    Url    = f2.CvUrl,
                },
            };

            // Los datos se muestran una vez que el postulante completó el formulario (no en ENVIADO).
            if (row.EstadoCodigo != EstadoFormularioPostulante.Enviado)
            {
                dto.Datos = new FormularioDatosDto
                {
                    ConsentimientoDatosPersonales = f2.ConsentimientoDatosPersonales,
                    NombresCompletos       = f2.NombresCompletos,
                    FechaNacimiento        = f2.FechaNacimiento,
                    EstadoCivil            = row.EstadoCivil,
                    TipoDocumento          = row.TipoDocumento,
                    NumeroDocumento        = f2.NumeroDocumento,
                    Distrito               = row.Distrito,
                    CorreoElectronico      = f2.CorreoElectronico,
                    NumeroCelular          = f2.NumeroCelular,
                    PretensionesSalariales = f2.PretensionesSalariales,
                    Disponibilidad         = row.Disponibilidad,
                    Linkedin               = f2.Linkedin,
                    PortafolioLink         = f2.PortafolioLink,
                    Profesion              = f2.Profesion,
                    Universidad            = row.Universidad,
                    GradoAcademico         = row.GradoAcademico,
                    NumeroColegiatura      = f2.NumeroColegiatura,
                    Empresa                = f2.Empresa,
                    AreaTrabajo            = f2.AreaTrabajo,
                    Cargo                  = f2.Cargo,
                    FechaInicio            = f2.FechaInicio,
                    FechaTermino           = f2.FechaTermino,
                    MotivoCese             = row.MotivoCese,
                    FuncionesPrincipales   = f2.FuncionesPrincipales,
                    Logros                 = f2.Logros,
                    IngresoBrutoMensual    = f2.IngresoBrutoMensual,
                    PersonasACargo         = f2.PersonasACargo,
                    JefeInmediato          = f2.JefeInmediato,
                    AutorizaVerificacionReferencias = f2.AutorizaVerificacionReferencias,
                    DeclaracionVeracidad   = f2.DeclaracionVeracidad,
                    ConfirmacionDocumentos = f2.ConfirmacionDocumentos,
                };

                // Solo tiene sentido si el postulante ya declaró algo: en ENVIADO no hay documento
                // que cotejar, así que ese roundtrip no se paga. La coincidencia se sigue
                // resolviendo cuando el formulario ya está aprobado o rechazado, para que el aviso
                // no desaparezca del modal después de decidir.
                dto.Coincidencia = await CoincidenciaPersonaQuery.ResolverUnoAsync(ctx, candidatoId);
            }

            return dto;
        }

        public async Task<DecisionFormularioContextoDto> RegistrarDecision(int candidatoId, bool aprobado, string? motivo, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var f = await ctx.GthPostulanteFormulario.FirstOrDefaultAsync(x => x.GthCandidatoId == candidatoId && x.State);
            if (f == null)
                throw new AbrilException("No se encontró el formulario del candidato.", 404);

            var estados = await ctx.GthPostulanteFormularioEstado.Where(e => e.State).ToListAsync();
            var actual = estados.FirstOrDefault(e => e.GthPostulanteFormularioEstadoId == f.GthPostulanteFormularioEstadoId);
            var estabaCompletado = actual?.Codigo == EstadoFormularioPostulante.Completado;

            // Aprobar exige que el postulante lo haya completado: no se puede dar por buena
            // información que nadie declaró. Rechazar también vale sobre un formulario ENVIADO que
            // nunca llenó — es lo que destraba el paso a entrevistas cuando el postulante no
            // responde. El token no se toca: si lo completa después, vuelve a caer como COMPLETADO.
            if (aprobado && !estabaCompletado)
                throw new AbrilException("Solo puedes aprobar un formulario que el postulante ya completó.", 409);

            // El enlace del formulario sigue vigente después de rechazarlo, así que un postulante
            // al que GTH ya sacó del proceso puede volver a enviarlo y reaparecer como "Por
            // revisar". Aprobarlo ahí lo devolvería al proceso después de haberle mandado el
            // correo de fin de proceso: se corta acá.
            if (aprobado)
            {
                var fueraDelProceso = await (
                    from ev in ctx.GthCandidatoEvaluacion
                    where ev.GthCandidatoId == candidatoId && ev.State
                    join res in ctx.GthCandidatoResultado
                        on ev.GthCandidatoResultadoId equals res.GthCandidatoResultadoId
                    select res.Codigo).FirstOrDefaultAsync();

                if (fueraDelProceso == ResultadoCandidatoFormulario.NoPaso)
                    throw new AbrilException(
                        "Este postulante ya quedó fuera del proceso: no se puede aprobar su formulario.", 409);

                // El formulario es público y el postulante puede declarar el documento de
                // cualquiera, incluido un trabajador de Abril. Aprobar copia lo declarado a
                // `person`, así que si ese documento es de alguien que trabaja acá hoy la
                // aprobación estaría reescribiendo la ficha de un trabajador con lo que tecleó un
                // desconocido: se corta. La pantalla ya no ofrece el botón en ese caso; esto es la
                // garantía real (el endpoint existe con o sin pantalla, y la ficha puede haber
                // cambiado de estado entre que GTH abrió el modal y le dio a aprobar).
                //
                // Las coincidencias que NO están adentro (un retirado que vuelve a postular, un
                // finalista de otro proceso, alguien que solo existe en `person`) sí se aprueban:
                // ahí actualizar es lo correcto y GTH ya vio el aviso.
                var coincidencia = await CoincidenciaPersonaQuery.ResolverUnoAsync(ctx, candidatoId);
                if (coincidencia is { BloqueaAprobacion: true })
                    throw new AbrilException(
                        $"El documento declarado ({coincidencia.Documento}) pertenece a "
                        + $"{coincidencia.NombreEnBd ?? "un trabajador"}, que figura como "
                        + $"{coincidencia.WorkersEstadoNombre ?? "trabajador de Abril"} en la empresa. "
                        + "No se puede aprobar este formulario: aprobarlo actualizaría los datos de "
                        + "ese trabajador. Verifica el documento con el postulante.", 409);
            }
            if (!aprobado && !estabaCompletado && actual?.Codigo != EstadoFormularioPostulante.Enviado)
                throw new AbrilException("Solo puedes rechazar un formulario que ya se le envió al postulante.", 409);

            var destinoCodigo = aprobado ? EstadoFormularioPostulante.Aprobado : EstadoFormularioPostulante.Rechazado;
            var destino = estados.FirstOrDefault(e => e.Codigo == destinoCodigo)
                ?? throw new AbrilException($"No está configurado el estado {destinoCodigo} del formulario del postulante.", 500);

            // Nombre del revisor (snapshot para mostrar "Aprobado por …"). Best-effort.
            string? revisorNombre = userId.HasValue
                ? await ctx.Worker
                    .Where(w => w.Person != null && w.Person.UserId == userId.Value)
                    .Select(w => w.Person!.FullName ?? w.ApellidoNombre)
                    .FirstOrDefaultAsync()
                : null;

            var now = DateTimeOffset.UtcNow;
            f.GthPostulanteFormularioEstadoId = destino.GthPostulanteFormularioEstadoId;
            f.RevisadoUserId   = userId;
            f.RevisadoNombre   = revisorNombre;
            f.RevisadoDateTime = now;
            f.MotivoRechazo    = aprobado ? null : Trim(motivo);
            f.UpdatedDateTime  = now;
            f.UpdatedUserId    = userId;

            // Aprobar es el momento en que los datos declarados por el postulante pasan a ser datos
            // validados por GTH, así que es acá — y solo acá — donde se escriben en `person` (la data
            // maestra). Va en la misma transacción que el cambio de estado: o el formulario queda
            // aprobado CON su ficha en person, o no queda aprobado.
            string? personAviso = null;
            DecisionFormularioFftDto? fft = null;
            if (aprobado)
            {
                // Ingreso directo FFT: aprobar el formulario es el último paso del proceso además
                // del EMO. No hay entrevista que programar, ni multitest, ni finalista que enviarle
                // al solicitante —él pidió a esta persona por nombre—, así que la aprobación deja al
                // candidato SELECCIONADO, el requerimiento en EMO de ingreso y su ficha de
                // pre-ingreso abierta. Se lee acá, con la entidad del requerimiento, porque el salto
                // tiene que entrar en la misma transacción que la aprobación.
                var proceso = await (
                    from c in ctx.GthCandidato
                    where c.GthCandidatoId == candidatoId && c.State
                    join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                    join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                    join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                    select new
                    {
                        Req          = r,
                        r.Codigo,
                        r.EsFft,
                        Puesto       = p.Nombre,
                        Area         = r.Solicitud!.AreaNombre,
                        ProyectoObra = pr.ProjectDescription,
                        Candidato    = c.Nombre,
                    }).FirstOrDefaultAsync();

                // La transacción se abre dentro de la execution strategy porque el provider corre con
                // EnableRetryOnFailure y no admite transacciones iniciadas por fuera de ella.
                var strategy = ctx.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await ctx.Database.BeginTransactionAsync();
                    // La estrategia puede reintentar el bloque completo y acá es seguro: los cambios de
                    // `f` se hicieron antes de entrar (siguen trackeados como Modified porque el
                    // SaveChanges fallido no los acepta) y el upsert de person es idempotente por su
                    // ON CONFLICT, así que el reintento vuelve a escribir exactamente lo mismo. Por eso
                    // no hace falta limpiar el ChangeTracker como en otros bloques del módulo.
                    var sync = await SincronizarPersonAsync(ctx, f, candidatoId, userId);
                    f.PersonId  = sync.PersonId ?? f.PersonId;
                    personAviso = sync.Aviso;
                    await ctx.SaveChangesAsync();

                    if (proceso is { EsFft: true })
                    {
                        // Va después del SaveChanges de arriba a propósito: la ficha de pre-ingreso
                        // se busca por el `person_id` que ese guardado acaba de dejar. Sigue dentro
                        // de la misma transacción, así que o queda todo o no queda nada.
                        var (ficha, estadoNombre) = await FftFlujo.CerrarConSeleccionadoAsync(
                            ctx, candidatoId, proceso.Req, userId, now);
                        await ctx.SaveChangesAsync();

                        fft = new DecisionFormularioFftDto
                        {
                            RequerimientoId = proceso.Req.GthRequerimientoId,
                            Codigo          = proceso.Codigo,
                            Puesto          = proceso.Puesto,
                            Area            = proceso.Area,
                            ProyectoObra    = proceso.ProyectoObra,
                            CandidatoNombre = Trim(f.NombresCompletos) ?? proceso.Candidato,
                            EstadoNombre    = estadoNombre,
                            WorkerId        = ficha?.Id,
                        };
                    }

                    await tx.CommitAsync();
                });
            }
            else
            {
                await ctx.SaveChangesAsync();
            }

            var contexto = new DecisionFormularioContextoDto
            {
                PersonId    = f.PersonId,
                PersonAviso = personAviso,
                Fft         = fft,
                Resumen = new CandidatoFormularioResumenDto
                {
                    EstadoCodigo   = destino.Codigo,
                    EstadoNombre   = destino.Nombre,
                    CorreoEnvio    = f.CorreoEnvio,
                    EnviadoEn      = f.EnviadoDateTime?.ToOffset(PeruOffset).DateTime,
                    CompletadoEn   = f.CompletadoDateTime?.ToOffset(PeruOffset).DateTime,
                    RevisadoNombre = revisorNombre,
                    RevisadoEn     = now.ToOffset(PeruOffset).DateTime,
                },
            };

            // Los datos del correo solo hacen falta al rechazar un formulario que el postulante sí
            // completó, así que la consulta extra se paga únicamente en ese caso.
            contexto.AvisarAlPostulante = !aprobado && estabaCompletado;
            if (contexto.AvisarAlPostulante)
            {
                var head = await (
                    from c in ctx.GthCandidato
                    where c.GthCandidatoId == candidatoId
                    join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                    join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                    select new { c.Nombre, Puesto = p.Nombre }).FirstOrDefaultAsync();

                contexto.Token  = f.Token;
                contexto.Correo = f.CorreoEnvio;
                contexto.Puesto = head?.Puesto ?? string.Empty;
                // El nombre declarado por el propio postulante manda sobre el que registró GTH.
                contexto.CandidatoNombre = Trim(f.NombresCompletos) ?? head?.Nombre ?? string.Empty;
                contexto.Motivo = f.MotivoRechazo;
            }

            return contexto;
        }

        /// <summary>
        /// Crea o actualiza la ficha de <c>person</c> (la data maestra) con lo que el postulante
        /// declaró y GTH acaba de validar. Solo se copian los campos que YA tienen columna en
        /// <c>person</c>; no se agregó ninguna columna nueva para esto.
        ///
        /// Mapeo:
        /// <list type="bullet">
        ///   <item><description><c>nombres_completos</c> → <c>full_name</c> (en mayúsculas, como el resto de la tabla; si no lo declaró, el nombre que registró GTH en <c>gth_candidato</c>)</description></item>
        ///   <item><description><c>numero_documento</c> → <c>document_identity_code</c> — es además la llave de coincidencia</description></item>
        ///   <item><description><c>gth_tipo_documento</c> → <c>document_identity_type</c> (por <c>codigo</c> ↔ <c>abbreviation</c>: DNI / CE)</description></item>
        ///   <item><description><c>correo_electronico</c> → <c>email</c> (correo personal: es el que usa Onboarding para la carta oferta)</description></item>
        ///   <item><description><c>numero_celular</c> → <c>phone_number</c> (solo dígitos; la columna es integer)</description></item>
        ///   <item><description><c>fecha_nacimiento</c> → <c>fecha_nacimiento</c></description></item>
        ///   <item><description><c>gth_distrito</c> → <c>distrito</c> (texto: la columna de person no está normalizada, a propósito)</description></item>
        ///   <item><description><c>gth_grado_academico</c> / <c>gth_universidad</c> → <c>grado_academico_id</c> / <c>universidad_id</c> (por nombre; null si ese ítem no existe en el catálogo maestro)</description></item>
        ///   <item><description><c>profesion</c> (texto libre) → <c>profesion_id</c> (por nombre; null si no coincide con ninguna)</description></item>
        /// </list>
        ///
        /// Política de escritura sobre una ficha que YA existía (mismo documento): <b>actualización
        /// parcial, lo declarado manda</b>. Campo por campo, si el postulante declaró un valor ese
        /// gana; si no declaró nada, se conserva el que ya había. Un null entrante nunca borra un
        /// dato existente. Es lo que corresponde porque acá los datos no llegan crudos: llegan
        /// validados por GTH, y GTH está viendo el aviso de que esta persona ya existía cuando
        /// aprueba (ver <c>CoincidenciaPersonaQuery</c>).
        ///
        /// Lo que NO se toca nunca: <c>state</c> y <c>active</c> (si el documento pertenece a una
        /// ficha dada de baja, no se revive por la aprobación de un formulario — eso es una decisión
        /// aparte de GTH; el enlace <c>person_id</c> queda igual y Onboarding puede leer su correo),
        /// y las columnas que el formulario no pregunta (<c>sexo_id</c>, <c>direccion</c>,
        /// <c>numero_hijos</c>, <c>talla_id</c>…), que ni entran al INSERT.
        ///
        /// Una coincidencia con un trabajador que está adentro de la empresa no llega hasta acá:
        /// <c>RegistrarDecision</c> rechaza esa aprobación antes de escribir nada.
        ///
        /// Un solo roundtrip: el upsert resuelve los cuatro catálogos con subconsultas y devuelve el
        /// <c>person_id</c>. Va por Dapper porque <c>ON CONFLICT DO UPDATE</c> con
        /// <c>COALESCE(person.x, EXCLUDED.x)</c> no se expresa en EF.
        /// </summary>
        private static async Task<(int? PersonId, string? Aviso)> SincronizarPersonAsync(
            AppDbContext ctx, GthPostulanteFormulario f, int candidatoId, int? userId)
        {
            var doc = Trim(f.NumeroDocumento);
            // El documento es la llave de coincidencia con person (tiene UNIQUE) y sin él no hay forma
            // de saber si la persona ya existe: insertar a ciegas duplicaría fichas. El formulario
            // público lo exige para poder enviarse, así que esto solo salta en datos anteriores.
            if (string.IsNullOrWhiteSpace(doc))
                return (null, "No se registró en la base maestra: el postulante no declaró su número de documento.");

            // person.phone_number es integer: se guardan solo los dígitos y solo si caben.
            var celularDigitos = new string((f.NumeroCelular ?? string.Empty).Where(char.IsDigit).ToArray());
            int? celular = int.TryParse(celularDigitos, out var cel) ? cel : null;

            const string sql = """
                INSERT INTO person (
                    document_identity_type_id, document_identity_code, full_name, email, phone_number,
                    fecha_nacimiento, distrito, grado_academico_id, universidad_id, profesion_id,
                    created_date_time, created_user_id, active, state, mostrar_en_boletin)
                VALUES (
                    (SELECT dit.document_identity_type_id
                       FROM document_identity_type dit
                       JOIN gth_tipo_documento gtd
                         ON upper(btrim(gtd.codigo)) = upper(btrim(dit.document_identity_type_abbreviation))
                      WHERE gtd.gth_tipo_documento_id = @tipoDocumentoId AND dit.state = true
                      LIMIT 1),
                    @doc,
                    upper(btrim(coalesce(@nombresCompletos,
                                         (SELECT c.nombre FROM gth_candidato c
                                           WHERE c.gth_candidato_id = @candidatoId)))),
                    @email,
                    @celular,
                    @fechaNacimiento,
                    (SELECT gd.nombre FROM gth_distrito gd WHERE gd.gth_distrito_id = @distritoId),
                    (SELECT ga.grado_academico_id
                       FROM grado_academico ga
                       JOIN gth_grado_academico gga ON upper(btrim(gga.nombre)) = upper(btrim(ga.nombre))
                      WHERE gga.gth_grado_academico_id = @gradoAcademicoId AND ga.state = true
                      LIMIT 1),
                    (SELECT u.universidad_id
                       FROM universidad u
                       JOIN gth_universidad gu ON upper(btrim(gu.nombre)) = upper(btrim(u.nombre))
                      WHERE gu.gth_universidad_id = @universidadId AND u.state = true
                      LIMIT 1),
                    (SELECT p.profesion_id FROM profesion p
                      WHERE upper(btrim(p.nombre)) = upper(btrim(@profesion)) AND p.state = true
                      LIMIT 1),
                    now(), @userId, true, true, true)
                ON CONFLICT (document_identity_code) DO UPDATE SET
                    -- Actualización parcial: lo declarado manda campo por campo, y un null
                    -- entrante NO borra lo que ya había. Ver la nota de política arriba.
                    full_name                 = coalesce(nullif(btrim(EXCLUDED.full_name), ''), person.full_name),
                    email                     = coalesce(EXCLUDED.email,                     person.email),
                    phone_number              = coalesce(EXCLUDED.phone_number,              person.phone_number),
                    document_identity_type_id = coalesce(EXCLUDED.document_identity_type_id,  person.document_identity_type_id),
                    fecha_nacimiento          = coalesce(EXCLUDED.fecha_nacimiento,           person.fecha_nacimiento),
                    distrito                  = coalesce(EXCLUDED.distrito,                   person.distrito),
                    grado_academico_id        = coalesce(EXCLUDED.grado_academico_id,         person.grado_academico_id),
                    universidad_id            = coalesce(EXCLUDED.universidad_id,             person.universidad_id),
                    profesion_id              = coalesce(EXCLUDED.profesion_id,               person.profesion_id),
                    updated_date_time         = now(),
                    updated_user_id           = @userId
                RETURNING person_id;
                """;

            var personId = await ctx.Database.GetDbConnection().ExecuteScalarAsync<int?>(
                sql,
                new
                {
                    doc,
                    tipoDocumentoId  = f.GthTipoDocumentoId,
                    nombresCompletos = Trim(f.NombresCompletos),
                    candidatoId,
                    email            = Trim(f.CorreoElectronico)?.ToLowerInvariant(),
                    celular,
                    fechaNacimiento  = f.FechaNacimiento,
                    distritoId       = f.GthDistritoId,
                    gradoAcademicoId = f.GthGradoAcademicoId,
                    universidadId    = f.GthUniversidadId,
                    profesion        = Trim(f.Profesion),
                    userId,
                },
                transaction: ctx.Database.CurrentTransaction?.GetDbTransaction());

            var aviso = string.IsNullOrWhiteSpace(Trim(f.CorreoElectronico))
                ? "Se registró en la base maestra, pero sin correo personal: Onboarding no podrá enviarle la carta oferta hasta que se complete."
                : null;

            return (personId, aviso);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static PostulanteFormularioRespuestasDto MapRespuestas(GthPostulanteFormulario f) => new()
        {
            ConsentimientoDatosPersonales = f.ConsentimientoDatosPersonales,
            NombresCompletos       = f.NombresCompletos,
            FechaNacimiento        = f.FechaNacimiento,
            EstadoCivilId          = f.GthEstadoCivilId,
            TipoDocumentoId        = f.GthTipoDocumentoId,
            NumeroDocumento        = f.NumeroDocumento,
            DistritoId             = f.GthDistritoId,
            CorreoElectronico      = f.CorreoElectronico,
            NumeroCelular          = f.NumeroCelular,
            PretensionesSalariales = f.PretensionesSalariales,
            DisponibilidadId       = f.GthDisponibilidadId,
            Linkedin               = f.Linkedin,
            PortafolioLink         = f.PortafolioLink,
            Profesion              = f.Profesion,
            UniversidadId          = f.GthUniversidadId,
            GradoAcademicoId       = f.GthGradoAcademicoId,
            NumeroColegiatura      = f.NumeroColegiatura,
            Empresa                = f.Empresa,
            AreaTrabajo            = f.AreaTrabajo,
            Cargo                  = f.Cargo,
            FechaInicio            = f.FechaInicio,
            FechaTermino           = f.FechaTermino,
            MotivoCeseId           = f.GthMotivoCeseId,
            FuncionesPrincipales   = f.FuncionesPrincipales,
            Logros                 = f.Logros,
            IngresoBrutoMensual    = f.IngresoBrutoMensual,
            PersonasACargo         = f.PersonasACargo,
            JefeInmediato          = f.JefeInmediato,
            AutorizaVerificacionReferencias = f.AutorizaVerificacionReferencias,
            DeclaracionVeracidad   = f.DeclaracionVeracidad,
            ConfirmacionDocumentos = f.ConfirmacionDocumentos,
        };
    }

    /// <summary>Códigos estables del estado del formulario del postulante (espejo de gth_postulante_formulario_estado.codigo).</summary>
    /// <summary>
    /// Código del resultado con el que un candidato queda fuera del proceso (espejo de
    /// <c>gth_candidato_resultado.codigo</c>). Es el mismo catálogo que usa
    /// <c>ReclutamientoRepository</c>; acá solo hace falta este valor.
    /// </summary>
    internal static class ResultadoCandidatoFormulario
    {
        public const string NoPaso = "NO_PASO";
    }

    internal static class EstadoFormularioPostulante
    {
        public const string Enviado    = "ENVIADO";
        public const string Completado = "COMPLETADO";
        public const string Aprobado   = "APROBADO";
        public const string Rechazado  = "RECHAZADO";
    }
}
