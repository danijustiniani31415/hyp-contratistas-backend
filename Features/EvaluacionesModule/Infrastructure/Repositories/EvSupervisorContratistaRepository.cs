using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvSupervisorContratistaRepository : IEvSupervisorContratistaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        // Palabras que, si aparecen en el puesto (workers.puesto_id -> puesto.nombre),
        // cuentan como "supervisor de campo del contratista" para este flujo. Se compara
        // por CONTIENE, no por igualdad: puesto.nombre suele venir armado como
        // "<categoría> <puesto>" concatenados (p. ej. "SUPERVISOR SUPERVISOR DE CAMPO",
        // "CAPATAZ SUPERVISOR DE CAMPO"), así que una lista de textos exactos deja afuera
        // la mayoría de los casos reales. A pedido del usuario (2026-08-20): estas
        // palabras clave, sin distinguir más finamente por ahora.
        private static readonly string[] PalabrasClaveSupervisorDeCampo =
        [
            "SUPERVISOR", "CAPATAZ", "PREVENCIONISTA", "INGENIERO DE PRODUCCION",
        ];

        public EvSupervisorContratistaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
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

        public async Task<EvSupervisorContratistaInicioDto> GetInicioAsync(int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var periodo = await conn.QueryFirstOrDefaultAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo WHERE activo = TRUE LIMIT 1");

            var plantilla = await conn.QueryAsync<EvSupervisorContratistaCriterioDto>(
                @"SELECT id AS Id, criterio AS Criterio, orden AS Orden
                  FROM ev_supervisor_contratista_plantilla
                  WHERE activo = TRUE ORDER BY orden");

            if (periodo == null)
                return new EvSupervisorContratistaInicioDto { Plantilla = plantilla.ToList() };

            var yaMarcoNoAplica = await conn.QueryFirstOrDefaultAsync<bool>(
                @"SELECT EXISTS (
                    SELECT 1 FROM ev_evaluacion_supervisor_contratista
                    WHERE periodo_id = @PeriodoId AND evaluador_user_id = @UserId AND no_aplica = TRUE
                  )",
                new { PeriodoId = periodo.Id, UserId = evaluadorUserId });

            // El Jefe SSOMA (puesto único, PuestoIds.JefeSsoma) supervisa todos los proyectos,
            // no solo el suyo — a diferencia del Prevencionista/Coordinador, cuyo alcance es
            // su vinculación vigente.
            var esJefeSsoma = await conn.QueryFirstOrDefaultAsync<bool>(
                @"SELECT EXISTS (
                    SELECT 1
                    FROM app_user au
                    JOIN workers w ON LOWER(w.email_corporativo) = LOWER(au.email)
                    WHERE au.user_id = @UserId AND w.state AND w.contrata_casa = 'Casa' AND w.workers_estado_id = 1 /* WorkersEstadoIds.Activo */
                      AND w.puesto_id = @PuestoJefeSsoma
                  )",
                new { UserId = evaluadorUserId, PuestoJefeSsoma = PuestoIds.JefeSsoma });

            var proyectoIds = esJefeSsoma
                ? (await conn.QueryAsync<int>("SELECT project_id FROM project")).ToList()
                : (await conn.QueryAsync<int>(
                    @"SELECT DISTINCT wv.proyecto_id
                      FROM app_user au
                      JOIN workers w ON LOWER(w.email_corporativo) = LOWER(au.email)
                      JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
                      WHERE au.user_id = @UserId AND w.state AND w.contrata_casa = 'Casa' AND w.workers_estado_id = 1 /* WorkersEstadoIds.Activo */",
                    new { UserId = evaluadorUserId })).ToList();

            if (proyectoIds.Count == 0)
                return new EvSupervisorContratistaInicioDto
                {
                    Periodo = MapPeriodo(periodo),
                    Plantilla = plantilla.ToList(),
                    YaMarcoNoAplica = yaMarcoNoAplica
                };

            // Supervisores de campo: por PUESTO del trabajador, no por tener cuenta
            // logueada. La empresa se lee de worker_vinculaciones.empresa_id (la vinculación
            // vigente), no de workers.contributor_id — ese campo del maestro suele venir
            // vacío para personal contrata; la empresa real vive por vinculación.
            var supervisores = await conn.QueryAsync<SupervisorRaw>(
                @"SELECT DISTINCT
                    w.id                   AS SupervisorWorkerId,
                    COALESCE(per.full_name, w.apellido_nombre) AS SupervisorNombre,
                    COALESCE(wv.empresa_id, w.contributor_id, 0) AS ContributorId,
                    COALESCE(c.contributor_name, 'Sin empresa') AS ContributorNombre,
                    wv.proyecto_id         AS ProyectoId,
                    pr.project_description AS ProyectoNombre
                  FROM workers w
                  JOIN puesto pu ON pu.puesto_id = w.puesto_id
                  JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
                  JOIN project pr ON pr.project_id = wv.proyecto_id
                  LEFT JOIN person per ON per.person_id = w.person_id
                  LEFT JOIN contributor c ON c.contributor_id = COALESCE(wv.empresa_id, w.contributor_id)
                  WHERE w.state AND w.contrata_casa = 'Contratista'
                    AND w.workers_estado_id = 1
                    AND upper(pu.nombre) LIKE ANY(@Patrones)
                    AND wv.proyecto_id = ANY(@ProyectoIds)",
                new
                {
                    ProyectoIds = proyectoIds.ToArray(),
                    Patrones = PalabrasClaveSupervisorDeCampo.Select(p => $"%{p}%").ToArray(),
                });

            var yaEvaluadas = await conn.QueryAsync<YaEvaluadaRaw>(
                @"SELECT id AS EvaluacionId, supervisor_worker_id AS SupervisorId, nota AS Nota, comentario AS Comentario
                  FROM ev_evaluacion_supervisor_contratista
                  WHERE periodo_id = @PeriodoId AND evaluador_user_id = @UserId AND supervisor_worker_id IS NOT NULL",
                new { PeriodoId = periodo.Id, UserId = evaluadorUserId });
            var evaluadasMap = yaEvaluadas.ToDictionary(x => x.SupervisorId);

            var detallesPrevios = await conn.QueryAsync<DetallePrevioRaw>(
                @"SELECT d.evaluacion_supervisor_contratista_id AS EvaluacionId,
                    d.plantilla_id AS PlantillaId, d.criterio AS Criterio, d.puntaje AS Puntaje, d.es_na AS EsNa
                  FROM ev_evaluacion_supervisor_contratista_detalle d
                  JOIN ev_evaluacion_supervisor_contratista e ON e.id = d.evaluacion_supervisor_contratista_id
                  WHERE e.periodo_id = @PeriodoId AND e.evaluador_user_id = @UserId",
                new { PeriodoId = periodo.Id, UserId = evaluadorUserId });
            var detallesPreviosMap = detallesPrevios
                .GroupBy(d => d.EvaluacionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var aEvaluar = supervisores.Select(s =>
            {
                var yaEvalue = evaluadasMap.TryGetValue(s.SupervisorWorkerId, out var previa);
                detallesPreviosMap.TryGetValue(yaEvalue ? previa!.EvaluacionId : 0, out var detallesPrevia);
                return new EvSupervisorContratistaAEvaluarDto
                {
                    SupervisorSsContratistaUsuarioId = s.SupervisorWorkerId,
                    SupervisorNombre = s.SupervisorNombre,
                    ContributorId = s.ContributorId,
                    ContributorNombre = s.ContributorNombre,
                    ProyectoId = s.ProyectoId,
                    ProyectoNombre = s.ProyectoNombre,
                    YaEvalue = yaEvalue,
                    NotaPrevia = yaEvalue ? previa!.Nota : null,
                    EvaluacionId = yaEvalue ? previa!.EvaluacionId : null,
                    ComentarioPrevio = yaEvalue ? previa!.Comentario : null,
                    DetallesPrevios = (yaEvalue ? detallesPrevia : null)?.Select(d => new EvSupervisorContratistaDetallePrevioDto
                    {
                        PlantillaId = d.PlantillaId, Criterio = d.Criterio, Puntaje = d.Puntaje, EsNa = d.EsNa
                    }).ToList() ?? []
                };
            }).ToList();

            return new EvSupervisorContratistaInicioDto
            {
                Periodo = MapPeriodo(periodo),
                Plantilla = plantilla.ToList(),
                SupervisoresAEvaluar = aEvaluar,
                YaMarcoNoAplica = yaMarcoNoAplica
            };
        }

        public async Task<EvEvaluacionSupervisorContratista> CreateAsync(
            EvEvaluacionSupervisorContratista eval, List<EvEvaluacionSupervisorContratistaDetalle> detalles)
        {
            using var ctx = _factory.CreateDbContext();

            int maxPorCriterio = 4;
            var puntajesValidos = detalles.Where(d => !d.EsNa && d.Puntaje.HasValue).Select(d => d.Puntaje!.Value).ToList();
            int totalMax = puntajesValidos.Count * maxPorCriterio;
            decimal sumPuntajes = puntajesValidos.Sum();
            eval.Nota = totalMax > 0 ? Math.Round((sumPuntajes / totalMax) * 20m, 2) : 0;
            eval.Detalles = detalles;

            // El supervisor se identifica por worker (puesto de campo), no por login;
            // ContributorId/SupervisorNombre se resuelven acá porque el DTO de creación
            // no los trae (solo el id del worker seleccionado en la pantalla).
            if (eval.SupervisorWorkerId.HasValue)
            {
                await ctx.Database.OpenConnectionAsync();
                var conn = ctx.Database.GetDbConnection();
                var datos = await conn.QueryFirstOrDefaultAsync<WorkerDatosRaw>(
                    @"SELECT COALESCE(wv.empresa_id, w.contributor_id) AS ContributorId,
                             COALESCE(per.full_name, w.apellido_nombre) AS Nombre
                      FROM workers w
                      LEFT JOIN person per ON per.person_id = w.person_id
                      LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
                      WHERE w.state AND w.id = @Id",
                    new { Id = eval.SupervisorWorkerId.Value });
                eval.ContributorId = datos?.ContributorId ?? 0;
                eval.SupervisorNombre = datos?.Nombre ?? eval.SupervisorNombre;
            }

            ctx.EvEvaluacionesSupervisorContratista.Add(eval);
            await ctx.SaveChangesAsync();
            return eval;
        }

        public async Task<EvEvaluacionSupervisorContratista?> ObtenerPorIdAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesSupervisorContratista
                .Include(e => e.Detalles)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<EvEvaluacionSupervisorContratista> ActualizarAsync(
            int id, string? comentario, List<EvEvaluacionSupervisorContratistaDetalle> detalles)
        {
            using var ctx = _factory.CreateDbContext();
            var eval = await ctx.EvEvaluacionesSupervisorContratista
                .Include(e => e.Detalles)
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new AbrilException("Evaluación no encontrada.", 404);

            int maxPorCriterio = 4;
            var puntajesValidos = detalles.Where(d => !d.EsNa && d.Puntaje.HasValue).Select(d => d.Puntaje!.Value).ToList();
            int totalMax = puntajesValidos.Count * maxPorCriterio;
            decimal sumPuntajes = puntajesValidos.Sum();

            eval.Nota = totalMax > 0 ? Math.Round((sumPuntajes / totalMax) * 20m, 2) : 0;
            eval.Comentario = comentario;
            eval.UpdatedAt = DateTime.UtcNow;

            ctx.EvEvaluacionesSupervisorContratistaDetalle.RemoveRange(eval.Detalles);
            eval.Detalles = detalles;

            await ctx.SaveChangesAsync();
            return eval;
        }

        public async Task<bool> ExisteAsync(int periodoId, int supervisorWorkerId, int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesSupervisorContratista.AnyAsync(e =>
                e.PeriodoId == periodoId &&
                e.SupervisorWorkerId == supervisorWorkerId &&
                e.EvaluadorUserId == evaluadorUserId);
        }

        public async Task<bool> ExisteNoAplicaAsync(int periodoId, int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesSupervisorContratista.AnyAsync(e =>
                e.PeriodoId == periodoId && e.EvaluadorUserId == evaluadorUserId && e.NoAplica);
        }

        public async Task RegistrarNoAplicaAsync(
            int periodoId, int evaluadorUserId, string motivo,
            int? proyectoId = null, int? supervisorWorkerId = null)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.EvEvaluacionesSupervisorContratista.Add(new EvEvaluacionSupervisorContratista
            {
                PeriodoId = periodoId,
                EvaluadorUserId = evaluadorUserId,
                ProyectoId = proyectoId ?? 0,
                SupervisorWorkerId = supervisorWorkerId,
                NoAplica = true,
                NoAplicaMotivo = motivo
            });
            await ctx.SaveChangesAsync();
        }

        public async Task<EvSupervisorContratistaVerInicioDto> GetVerInicioAsync(int? periodoId, int? proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var periodos = await conn.QueryAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo ORDER BY anio DESC, mes DESC LIMIT 24");

            var proyectos = await conn.QueryAsync<EvSupervisorContratistaProyectoFiltroDto>(
                @"SELECT DISTINCT pr.project_id AS ProyectoId, pr.project_description AS ProyectoNombre
                  FROM ev_evaluacion_supervisor_contratista ec
                  JOIN project pr ON pr.project_id = ec.proyecto_id
                  WHERE ec.no_aplica = FALSE
                  ORDER BY pr.project_description");

            var evaluaciones = await ObtenerResumenesAsync(conn, periodoId, proyectoId);

            return new EvSupervisorContratistaVerInicioDto
            {
                Periodos = periodos.Select(MapPeriodo).ToList(),
                Proyectos = proyectos.ToList(),
                Evaluaciones = evaluaciones
            };
        }

        public async Task<EvSupervisorContratistaDashboardDto> GetDashboardAsync(int? periodoId, int? proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var evaluaciones = await ObtenerResumenesAsync(conn, periodoId, proyectoId);
            var conNota = evaluaciones.Where(e => e.Nota.HasValue).ToList();

            return new EvSupervisorContratistaDashboardDto
            {
                TotalEvaluaciones = evaluaciones.Count,
                PromedioGeneral = conNota.Count > 0 ? Math.Round(conNota.Average(e => e.Nota!.Value), 2) : null,
                Evaluaciones = evaluaciones
            };
        }

        public async Task<int?> ResolverPropioWorkerIdAsync(int userId, int contributorId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();
            return await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT worker_id FROM ss_contratista_usuario
                  WHERE user_id = @UserId AND contractor_id = @ContributorId AND activo = TRUE
                  LIMIT 1",
                new { UserId = userId, ContributorId = contributorId });
        }

        public async Task<EvSupervisorContratistaMiPerfilDto> GetMiPerfilAsync(int supervisorWorkerId, int? periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var rows = await conn.QueryAsync<NotaComentarioRaw>(
                @"SELECT nota AS Nota, comentario AS Comentario
                  FROM ev_evaluacion_supervisor_contratista
                  WHERE supervisor_worker_id = @SupervisorWorkerId
                    AND NOT no_aplica
                    AND (@PeriodoId IS NULL OR periodo_id = @PeriodoId)",
                new { SupervisorWorkerId = supervisorWorkerId, PeriodoId = periodoId });

            var lista = rows.ToList();
            var conNota = lista.Where(r => r.Nota.HasValue).ToList();

            return new EvSupervisorContratistaMiPerfilDto
            {
                TotalEvaluaciones = lista.Count,
                PromedioGeneral = conNota.Count > 0 ? Math.Round(conNota.Average(r => r.Nota!.Value), 2) : null,
                Comentarios = lista.Where(r => !string.IsNullOrWhiteSpace(r.Comentario)).Select(r => r.Comentario!).ToList()
            };
        }

        private record NotaComentarioRaw(decimal? Nota, string? Comentario);

        private static async Task<List<EvSupervisorContratistaResumenDto>> ObtenerResumenesAsync(
            System.Data.IDbConnection conn, int? periodoId, int? proyectoId)
        {
            var rows = await conn.QueryAsync<EvSupervisorContratistaResumenDto>(
                @"SELECT
                    ec.id                  AS EvaluacionId,
                    COALESCE(ec.supervisor_worker_id, ec.supervisor_ss_contratista_usuario_id, 0) AS SupervisorSsContratistaUsuarioId,
                    ec.supervisor_nombre   AS SupervisorNombre,
                    ec.contributor_id      AS ContributorId,
                    COALESCE(c.contributor_name, 'Sin empresa') AS ContributorNombre,
                    ec.proyecto_id         AS ProyectoId,
                    pr.project_description AS ProyectoNombre,
                    COALESCE(p.full_name, au.email) AS EvaluadorNombre,
                    ec.nota                AS Nota,
                    ec.comentario           AS Comentario,
                    ec.created_at           AS CreatedAt
                  FROM ev_evaluacion_supervisor_contratista ec
                  LEFT JOIN contributor c ON c.contributor_id = ec.contributor_id
                  JOIN project pr    ON pr.project_id    = ec.proyecto_id
                  JOIN app_user au   ON au.user_id       = ec.evaluador_user_id
                  LEFT JOIN workers w  ON w.id = (
                      SELECT w2.id FROM workers w2
                      JOIN person p2 ON p2.person_id = w2.person_id
                      WHERE w2.state AND p2.user_id = ec.evaluador_user_id LIMIT 1)
                  LEFT JOIN person p ON p.person_id = w.person_id
                  WHERE ec.no_aplica = FALSE
                    AND (@PeriodoId IS NULL OR ec.periodo_id = @PeriodoId)
                    AND (@ProyectoId IS NULL OR ec.proyecto_id = @ProyectoId)
                  ORDER BY ec.created_at DESC",
                new { PeriodoId = periodoId, ProyectoId = proyectoId });

            return rows.ToList();
        }

        public async Task<List<EvaluadorDto>> GetEvaluadoresCandidatosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var rows = await conn.QueryAsync<EvaluadorDto>(
                @"SELECT DISTINCT
                    au.user_id          AS UserId,
                    w.id                AS WorkerId,
                    p.full_name         AS NombreCompleto,
                    w.email_corporativo AS EmailCorporativo,
                    w.subarea           AS Subarea
                  FROM workers w
                  JOIN person p    ON p.person_id = w.person_id
                  JOIN puesto pu   ON pu.puesto_id = w.puesto_id AND pu.categoria_id IN (@CategoriaCoordinadorSsoma, @CategoriaPrevencionista)
                  JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
                  WHERE w.state AND w.email_corporativo IS NOT NULL AND w.email_corporativo != ''
                    AND w.contrata_casa = 'Casa'
                    AND w.workers_estado_id = @WorkersEstadoActivo
                    AND EXISTS (SELECT 1 FROM worker_vinculaciones wv WHERE wv.worker_id = w.id AND wv.fecha_fin IS NULL)",
                new { CategoriaCoordinadorSsoma = CategoriaIds.CoordinadorSsoma, CategoriaPrevencionista = CategoriaIds.Prevencionista, WorkersEstadoActivo = WorkersEstadoIds.Activo });

            return rows.ToList();
        }

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
        private record SupervisorRaw(int SupervisorWorkerId, string SupervisorNombre, int ContributorId, string ContributorNombre, int ProyectoId, string ProyectoNombre);
        private record YaEvaluadaRaw(int EvaluacionId, int SupervisorId, decimal? Nota, string? Comentario);
        private record DetallePrevioRaw(int EvaluacionId, int? PlantillaId, string Criterio, int? Puntaje, bool EsNa);
        private record WorkerDatosRaw(int? ContributorId, string? Nombre);
    }
}
