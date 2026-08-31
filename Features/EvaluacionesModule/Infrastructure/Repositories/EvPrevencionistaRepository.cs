using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvPrevencionistaRepository : IEvPrevencionistaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EvPrevencionistaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<EvPrevencionistaInicioDto> GetInicioAsync(int evaluadorUserId, int evaluadorContributorId, List<int> proyectoIds)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var periodo = await conn.QueryFirstOrDefaultAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo WHERE activo = TRUE LIMIT 1");

            var plantillaCoordinador = await conn.QueryAsync<EvSupervisorContratistaCriterioDto>(
                @"SELECT id AS Id, criterio AS Criterio, orden AS Orden
                  FROM ev_prevencionista_plantilla WHERE activo = TRUE AND rol_evaluado = 'COORDINADOR' ORDER BY orden");
            var plantillaPrevencionista = await conn.QueryAsync<EvSupervisorContratistaCriterioDto>(
                @"SELECT id AS Id, criterio AS Criterio, orden AS Orden
                  FROM ev_prevencionista_plantilla WHERE activo = TRUE AND rol_evaluado = 'PREVENCIONISTA' ORDER BY orden");

            if (periodo == null || proyectoIds.Count == 0)
                return new EvPrevencionistaInicioDto
                {
                    PlantillaCoordinador = plantillaCoordinador.ToList(),
                    PlantillaPrevencionista = plantillaPrevencionista.ToList(),
                };

            var evaluadorSsUsuarioId = await ResolverEvaluadorSsUsuarioIdAsync(conn, evaluadorUserId, evaluadorContributorId);

            // Puesto real del trabajador (workers.puesto_id -> puesto.categoria_id), no un
            // user_role aparte — ver el mismo criterio en EvGestionSsomaRepository/EvJefeSsomaRepository.
            var candidatos = await conn.QueryAsync<CandidatoRaw>(
                @"SELECT DISTINCT
                    au.user_id AS EvaluadoUserId,
                    p.full_name AS EvaluadoNombre,
                    CASE WHEN pu.categoria_id = @CategoriaCoordinadorSsoma THEN 'Coordinador SSOMA' ELSE 'Prevencionista' END AS EvaluadoPuesto,
                    wv.proyecto_id AS ProyectoId,
                    pr.project_description AS ProyectoNombre
                  FROM workers w
                  JOIN person p ON p.person_id = w.person_id
                  JOIN puesto pu ON pu.puesto_id = w.puesto_id AND pu.categoria_id IN (@CategoriaCoordinadorSsoma, @CategoriaPrevencionista)
                  JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
                  JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
                  JOIN project pr ON pr.project_id = wv.proyecto_id
                  WHERE w.state AND w.contrata_casa = 'Casa' AND w.workers_estado_id = 1 /* WorkersEstadoIds.Activo */ AND wv.proyecto_id = ANY(@ProyectoIds)",
                new {
                    ProyectoIds = proyectoIds.ToArray(),
                    CategoriaCoordinadorSsoma = CategoriaIds.CoordinadorSsoma,
                    CategoriaPrevencionista = CategoriaIds.Prevencionista,
                });

            List<EvPrevencionistaAEvaluarDto> aEvaluar;
            if (evaluadorSsUsuarioId == null)
            {
                aEvaluar = candidatos.Select(c => new EvPrevencionistaAEvaluarDto
                {
                    EvaluadoUserId = c.EvaluadoUserId,
                    EvaluadoNombre = c.EvaluadoNombre,
                    EvaluadoPuesto = c.EvaluadoPuesto,
                    ProyectoId = c.ProyectoId,
                    ProyectoNombre = c.ProyectoNombre,
                    YaEvalue = false
                }).ToList();
            }
            else
            {
                var yaEvaluados = (await conn.QueryAsync<YaEvaluadoRaw>(
                    @"SELECT evaluado_user_id AS EvaluadoUserId, proyecto_id AS ProyectoId
                      FROM ev_evaluacion_prevencionista
                      WHERE periodo_id = @PeriodoId AND evaluador_ss_contratista_usuario_id = @EvaluadorId",
                    new { PeriodoId = periodo.Id, EvaluadorId = evaluadorSsUsuarioId }))
                    .Select(x => (x.EvaluadoUserId, x.ProyectoId))
                    .ToHashSet();

                aEvaluar = candidatos.Select(c => new EvPrevencionistaAEvaluarDto
                {
                    EvaluadoUserId = c.EvaluadoUserId,
                    EvaluadoNombre = c.EvaluadoNombre,
                    EvaluadoPuesto = c.EvaluadoPuesto,
                    ProyectoId = c.ProyectoId,
                    ProyectoNombre = c.ProyectoNombre,
                    YaEvalue = yaEvaluados.Contains((c.EvaluadoUserId, c.ProyectoId))
                }).ToList();
            }

            return new EvPrevencionistaInicioDto
            {
                Periodo = MapPeriodo(periodo),
                PlantillaCoordinador = plantillaCoordinador.ToList(),
                PlantillaPrevencionista = plantillaPrevencionista.ToList(),
                AEvaluar = aEvaluar
            };
        }

        public async Task<List<int>> ResolverProyectoIdsActualesAsync(int userId, int contractorId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var workerId = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT worker_id FROM ss_contratista_usuario
                  WHERE user_id = @UserId AND contractor_id = @ContractorId AND activo = TRUE
                  LIMIT 1",
                new { UserId = userId, ContractorId = contractorId });

            if (workerId != null)
            {
                var proyectosActuales = (await conn.QueryAsync<int>(
                    @"SELECT DISTINCT proyecto_id FROM worker_vinculaciones
                      WHERE worker_id = @WorkerId AND fecha_fin IS NULL",
                    new { WorkerId = workerId })).ToList();
                if (proyectosActuales.Count > 0) return proyectosActuales;
            }

            // Fallback: cuentas sin worker_id detrás (admin de contratista sin persona física
            // asociada) siguen usando la asignación estática de ss_contratista_usuario_proyecto.
            return (await conn.QueryAsync<int>(
                @"SELECT scup.proyecto_id
                  FROM ss_contratista_usuario scu
                  JOIN ss_contratista_usuario_proyecto scup ON scup.contratista_usuario_id = scu.id
                  WHERE scu.user_id = @UserId AND scu.contractor_id = @ContractorId AND scu.activo = TRUE",
                new { UserId = userId, ContractorId = contractorId })).ToList();
        }

        public async Task<int?> ObtenerCategoriaPuestoAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();
            return await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT pu.categoria_id
                  FROM app_user au
                  JOIN workers w ON LOWER(w.email_corporativo) = LOWER(au.email)
                  JOIN puesto pu ON pu.puesto_id = w.puesto_id
                  WHERE au.user_id = @UserId AND w.state AND w.contrata_casa = 'Casa' AND w.workers_estado_id = 1 /* WorkersEstadoIds.Activo */
                  LIMIT 1",
                new { UserId = userId });
        }

        public async Task<bool> EsJefeSsomaAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();
            return await conn.QueryFirstOrDefaultAsync<bool>(
                @"SELECT EXISTS (
                    SELECT 1
                    FROM app_user au
                    JOIN workers w ON LOWER(w.email_corporativo) = LOWER(au.email)
                    WHERE au.user_id = @UserId AND w.state AND w.contrata_casa = 'Casa' AND w.workers_estado_id = 1 /* WorkersEstadoIds.Activo */
                      AND w.puesto_id = @PuestoJefeSsoma
                  )",
                new { UserId = userId, PuestoJefeSsoma = PuestoIds.JefeSsoma });
        }

        public async Task<int?> ResolverEvaluadorSsUsuarioIdAsync(int userId, int contributorId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();
            return await ResolverEvaluadorSsUsuarioIdAsync(conn, userId, contributorId);
        }

        public async Task<EvEvaluacionPrevencionista> CreateAsync(
            EvEvaluacionPrevencionista eval, List<EvEvaluacionPrevencionistaDetalle> detalles)
        {
            using var ctx = _factory.CreateDbContext();

            var puntajesValidos = detalles.Select(d => d.Puntaje).ToList();
            eval.Nota = puntajesValidos.Count > 0 ? Math.Round((decimal)puntajesValidos.Average() * 4, 2) : 0;
            eval.Detalles = detalles;

            ctx.EvEvaluacionesPrevencionista.Add(eval);
            await ctx.SaveChangesAsync();
            return eval;
        }

        public async Task<bool> ExisteAsync(int periodoId, int evaluadoUserId, int proyectoId, int evaluadorSsContratistaUsuarioId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesPrevencionista.AnyAsync(e =>
                e.PeriodoId == periodoId &&
                e.EvaluadoUserId == evaluadoUserId &&
                e.ProyectoId == proyectoId &&
                e.EvaluadorSsContratistaUsuarioId == evaluadorSsContratistaUsuarioId);
        }

        public async Task<EvPrevencionistaMiPerfilDto> GetMiPerfilAsync(int evaluadoUserId, int? periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var rows = await conn.QueryAsync<NotaComentarioRaw>(
                @"SELECT nota AS Nota, comentario AS Comentario
                  FROM ev_evaluacion_prevencionista
                  WHERE evaluado_user_id = @EvaluadoUserId
                    AND (@PeriodoId IS NULL OR periodo_id = @PeriodoId)",
                new { EvaluadoUserId = evaluadoUserId, PeriodoId = periodoId });

            var lista = rows.ToList();
            var conNota = lista.Where(r => r.Nota.HasValue).ToList();

            return new EvPrevencionistaMiPerfilDto
            {
                TotalEvaluaciones = lista.Count,
                PromedioGeneral = conNota.Count > 0 ? Math.Round(conNota.Average(r => r.Nota!.Value), 2) : null,
                Comentarios = lista.Where(r => !string.IsNullOrWhiteSpace(r.Comentario)).Select(r => r.Comentario!).ToList()
            };
        }

        public async Task<EvPrevencionistaDashboardDto> GetDashboardAsync(int? periodoId, int? proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var rows = await conn.QueryAsync<EvPrevencionistaResumenDto>(
                @"SELECT
                    e.id                    AS EvaluacionId,
                    e.evaluado_user_id      AS EvaluadoUserId,
                    COALESCE(p.full_name, au.email) AS EvaluadoNombre,
                    e.proyecto_id           AS ProyectoId,
                    pr.project_description  AS ProyectoNombre,
                    c.contributor_name      AS EvaluadorContributorNombre,
                    e.nota                  AS Nota,
                    e.comentario            AS Comentario,
                    e.created_at            AS CreatedAt
                  FROM ev_evaluacion_prevencionista e
                  JOIN project pr ON pr.project_id = e.proyecto_id
                  JOIN contributor c ON c.contributor_id = e.evaluador_contributor_id
                  JOIN app_user au ON au.user_id = e.evaluado_user_id
                  LEFT JOIN workers w ON w.id = (
                      SELECT w2.id FROM workers w2
                      JOIN person p2 ON p2.person_id = w2.person_id
                      WHERE w2.state AND p2.user_id = e.evaluado_user_id LIMIT 1)
                  LEFT JOIN person p ON p.person_id = w.person_id
                  WHERE (@PeriodoId IS NULL OR e.periodo_id = @PeriodoId)
                    AND (@ProyectoId IS NULL OR e.proyecto_id = @ProyectoId)
                  ORDER BY e.created_at DESC",
                new { PeriodoId = periodoId, ProyectoId = proyectoId });

            var lista = rows.ToList();
            var conNota = lista.Where(e => e.Nota.HasValue).ToList();

            return new EvPrevencionistaDashboardDto
            {
                TotalEvaluaciones = lista.Count,
                PromedioGeneral = conNota.Count > 0 ? Math.Round(conNota.Average(e => e.Nota!.Value), 2) : null,
                Evaluaciones = lista
            };
        }

        public async Task<List<EvPrevencionistaCandidatoDto>> GetEvaluadoresCandidatosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            // Mismo alcance que ResolverProyectoIdsActualesAsync: el proyecto real de un
            // supervisor de campo sale de la vinculación vigente de su worker (solo el
            // trabajador/persona sabe en qué obra está hoy), no de la asignación estática
            // ss_contratista_usuario_proyecto — esa tabla solo aplica a cuentas sin worker_id
            // (admin de contratista sin persona física detrás). El cron no tiene JWT, así que
            // reconstruye esto directo de la base.
            var rows = await conn.QueryAsync<CandidatoRawFlat>(
                @"SELECT
                    scu.user_id       AS UserId,
                    scu.contractor_id AS ContributorId,
                    au.email          AS Email,
                    COALESCE(p.full_name, au.email) AS Nombre,
                    COALESCE(wv.proyecto_id, scup.proyecto_id) AS ProyectoId
                  FROM ss_contratista_usuario scu
                  JOIN app_user au ON au.user_id = scu.user_id
                  LEFT JOIN worker_vinculaciones wv ON wv.worker_id = scu.worker_id AND wv.fecha_fin IS NULL
                  LEFT JOIN ss_contratista_usuario_proyecto scup
                    ON scup.contratista_usuario_id = scu.id AND scu.worker_id IS NULL
                  LEFT JOIN workers w ON w.id = scu.worker_id
                  LEFT JOIN person p ON p.person_id = w.person_id
                  WHERE scu.activo = TRUE
                    AND COALESCE(wv.proyecto_id, scup.proyecto_id) IS NOT NULL");

            return rows
                .GroupBy(r => (r.UserId, r.ContributorId))
                .Select(g => new EvPrevencionistaCandidatoDto
                {
                    UserId = g.Key.UserId,
                    ContributorId = g.Key.ContributorId,
                    Email = g.First().Email,
                    Nombre = g.First().Nombre,
                    ProyectoIds = g.Select(x => x.ProyectoId).Distinct().ToList()
                })
                .ToList();
        }

        /// <summary>
        /// Resuelve el registro ss_contratista_usuario de la persona logueada (empresa +
        /// usuario), que es lo que identifica de forma estable al evaluador aunque cambie
        /// de proyecto o se le reasignen módulos.
        /// </summary>
        private static Task<int?> ResolverEvaluadorSsUsuarioIdAsync(System.Data.IDbConnection conn, int userId, int contributorId)
            => conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT id FROM ss_contratista_usuario
                  WHERE user_id = @UserId AND contractor_id = @ContributorId AND activo = TRUE
                  LIMIT 1",
                new { UserId = userId, ContributorId = contributorId });

        private static EvPeriodoDto MapPeriodo(EvPeriodoRaw r) => new()
        {
            Id = r.Id,
            Mes = r.Mes,
            Anio = r.Anio,
            FechaApertura = r.FechaApertura,
            FechaCierre = r.FechaCierre,
            Activo = r.Activo,
        };

        private record EvPeriodoRaw(int Id, int Mes, int Anio, DateOnly FechaApertura, DateOnly FechaCierre, bool Activo);
        private record CandidatoRaw(int EvaluadoUserId, string EvaluadoNombre, string EvaluadoPuesto, int ProyectoId, string ProyectoNombre);
        private record CandidatoRawFlat(int UserId, int ContributorId, string Email, string Nombre, int ProyectoId);
        private record YaEvaluadoRaw(int EvaluadoUserId, int ProyectoId);
        private record NotaComentarioRaw(decimal? Nota, string? Comentario);
    }
}
