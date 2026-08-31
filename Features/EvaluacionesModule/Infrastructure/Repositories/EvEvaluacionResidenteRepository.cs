using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvEvaluacionResidenteRepository : IEvEvaluacionResidenteRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EvEvaluacionResidenteRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<EvEvaluacionResidente> CreateAsync(EvEvaluacionResidente eval, List<EvEvaluacionResidenteDetalle> detalles)
        {
            using var ctx = _factory.CreateDbContext();
            var puntajesValidos = detalles
                .Where(d => !d.EsNa && d.Puntaje.HasValue)
                .Select(d => d.Puntaje!.Value)
                .ToList();
            eval.Nota = puntajesValidos.Count != 0 ? Math.Round((decimal)puntajesValidos.Average() * 4, 2) : 0;
            eval.Detalles = detalles;
            ctx.EvEvaluacionesResidente.Add(eval);
            await ctx.SaveChangesAsync();
            return eval;
        }

        public async Task<EvEvaluacionResidente?> GetByIdAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesResidente
                .Include(e => e.Detalles)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        /// <summary>
        /// Corrige una evaluación ya enviada: reemplaza nota/comentario/detalles sin dejar
        /// rastro de que fue editada (regla de negocio: solo dentro de la ventana de 24h que
        /// valida el service, y la nota vieja no se conserva).
        /// </summary>
        public async Task<EvEvaluacionResidente> UpdateAsync(
            int id, decimal nota, string? comentario, bool noAplica, string? noAplicaMotivo,
            List<EvEvaluacionResidenteDetalle> detalles)
        {
            using var ctx = _factory.CreateDbContext();
            var eval = await ctx.EvEvaluacionesResidente
                .Include(e => e.Detalles)
                .FirstAsync(e => e.Id == id);

            eval.Nota = nota;
            eval.Comentario = comentario;
            eval.NoAplica = noAplica;
            eval.NoAplicaMotivo = noAplicaMotivo;
            eval.UpdatedAt = DateTime.UtcNow;

            ctx.EvEvaluacionesResidenteDetalle.RemoveRange(eval.Detalles);
            foreach (var d in detalles) d.EvaluacionId = id;
            ctx.EvEvaluacionesResidenteDetalle.AddRange(detalles);

            await ctx.SaveChangesAsync();
            return eval;
        }

        public async Task<bool> ExisteAsync(int periodoId, int evaluadorUserId, int evaluadoUserId, string areaNombre)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesResidente.AnyAsync(e =>
                e.PeriodoId == periodoId &&
                e.EvaluadorUserId == evaluadorUserId &&
                e.EvaluadoUserId == evaluadoUserId &&
                e.AreaNombre == areaNombre);
        }

        public async Task<List<EvEvaluacionResidenteResponseDto>> GetByPeriodoAsync(int periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            var evals = await ctx.EvEvaluacionesResidente
                .Where(e => e.PeriodoId == periodoId)
                .Include(e => e.Detalles)
                .Include(e => e.Periodo)
                .Include(e => e.Project)
                .ToListAsync();

            return await MapToResponseDtos(ctx, evals);
        }

        public async Task<List<EvEvaluacionResidenteResponseDto>> GetByEvaluadorAsync(int evaluadorUserId, int periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            var evals = await ctx.EvEvaluacionesResidente
                .Where(e => e.EvaluadorUserId == evaluadorUserId && e.PeriodoId == periodoId)
                .Include(e => e.Detalles)
                .Include(e => e.Periodo)
                .Include(e => e.Project)
                .ToListAsync();

            return await MapToResponseDtos(ctx, evals);
        }

        public async Task<List<EvEvaluacionResidenteResponseDto>> GetByEvaluadoAsync(int evaluadoUserId, int periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            var evals = await ctx.EvEvaluacionesResidente
                .Where(e => e.EvaluadoUserId == evaluadoUserId && e.PeriodoId == periodoId)
                .Include(e => e.Detalles)
                .Include(e => e.Periodo)
                .Include(e => e.Project)
                .ToListAsync();

            return await MapToResponseDtos(ctx, evals);
        }

        public async Task<EvEvaluacionResidenteResponseDto?> GetDetalleAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var eval = await ctx.EvEvaluacionesResidente
                .Where(e => e.Id == id)
                .Include(e => e.Detalles)
                .Include(e => e.Periodo)
                .Include(e => e.Project)
                .FirstOrDefaultAsync();

            if (eval == null) return null;

            var dtos = await MapToResponseDtos(ctx, [eval]);
            return dtos.FirstOrDefault();
        }

        public async Task<List<ResidenteEvaluableDto>> GetResidentesEvaluablesAsync(int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var evaluador = await conn.QueryFirstOrDefaultAsync<EvaluadorInfo>(
                @"SELECT oos.name AS ObraOficina, w.area AS Area, w.subarea AS Subarea,
                         pu.categoria_id AS CategoriaId, w.id AS WorkerId
                  FROM workers w
                  JOIN person p ON p.person_id = w.person_id
                  LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
                  LEFT JOIN workers_obra_oficina_staff oos
                         ON oos.workers_obra_oficina_staff_id = w.obra_oficina_staff_id
                  WHERE w.state AND p.user_id = @EvaluadorUserId
                  LIMIT 1",
                new { EvaluadorUserId = evaluadorUserId });

            if (evaluador == null) return [];

            // REGLA 4: Gerente de Proyectos → no evalúa
            if (evaluador.CategoriaId == CategoriaIds.Gerente && Eq(evaluador.Area, "Proyectos"))
                return [];

            // No es `const` porque interpola el id de la categoría RESIDENTE.
            string selectBase = $@"
                SELECT DISTINCT
                    p.full_name            AS NombreCompleto,
                    p.user_id              AS UserId,
                    pr.project_id          AS ProjectId,
                    pr.project_description AS ProjectNombre,
                    w.area                 AS Area,
                    w.subarea              AS Subarea
                FROM workers w
                JOIN person p   ON p.person_id = w.person_id
                JOIN app_user u ON u.user_id   = p.user_id
                JOIN puesto pu  ON pu.puesto_id = w.puesto_id
                JOIN project pr ON pr.project_id = (
                    SELECT wv.proyecto_id
                    FROM worker_vinculaciones wv
                    WHERE wv.worker_id = w.id AND wv.fecha_fin IS NULL
                    ORDER BY wv.fecha_inicio DESC
                    LIMIT 1
                )
                WHERE w.state
                  AND pu.categoria_id = {CategoriaIds.Residente}
                  AND w.contrata_casa = 'Casa'
                  AND w.workers_estado_id IN ({WorkersEstadoIds.NoRetiradosSql})
                  AND u.active    = true";

            bool esOficinaProyectos = Eq(evaluador.ObraOficina, "Oficina Central")
                                      && Eq(evaluador.Area, "Proyectos");
            bool esSubareaEspecial  = Eq(evaluador.Subarea, "Unidad de Proyectos")
                                      || Eq(evaluador.Subarea, "Planeamiento BIM");

            // REGLA 1: Oficina Central, Proyectos, subarea general, Jefe/Coordinador → ve todos
            if (esOficinaProyectos && !esSubareaEspecial
                && (evaluador.CategoriaId == CategoriaIds.Jefe
                    || evaluador.CategoriaId == CategoriaIds.Coordinador))
            {
                var todos = (await conn.QueryAsync<ResidenteEvaluableDto>(
                    selectBase + "\nORDER BY p.full_name",
                    new { EvaluadorUserId = evaluadorUserId })).ToList();
                todos.ForEach(r => r.PuedeVerTodos = true);
                return todos;
            }

            // REGLA 2: Oficina Central, Proyectos, Unidad de Proyectos/Planeamiento BIM → proyectos asignados
            if (esOficinaProyectos && esSubareaEspecial)
            {
                var projectIds = (await conn.QueryAsync<int>(
                    @"SELECT project_id FROM ev_asignacion_supervisor
                      WHERE supervisor_worker_id = @WorkerId AND activo = true",
                    new { evaluador.WorkerId })).ToList();

                if (projectIds.Count == 0) return [];

                return (await conn.QueryAsync<ResidenteEvaluableDto>(
                    selectBase + "\n                  AND pr.project_id = ANY(@ProjectIds)\n                ORDER BY p.full_name",
                    new { ProjectIds = projectIds.ToArray() })).ToList();
            }

            // REGLA 3: Staff → residentes del mismo proyecto que el evaluador
            return (await conn.QueryAsync<ResidenteEvaluableDto>(
                selectBase + @"
                  AND pr.project_id = (
                      SELECT wv2.proyecto_id
                      FROM workers w2
                      JOIN person p2 ON p2.person_id = w2.person_id
                      JOIN worker_vinculaciones wv2 ON wv2.worker_id = w2.id AND wv2.fecha_fin IS NULL
                      WHERE w2.state AND p2.user_id = @EvaluadorUserId
                      ORDER BY wv2.fecha_inicio DESC
                      LIMIT 1
                  )
                ORDER BY p.full_name",
                new { EvaluadorUserId = evaluadorUserId })).ToList();
        }

        private static bool Eq(string? a, string b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private sealed class EvaluadorInfo
        {
            public string? ObraOficina { get; set; }
            public string? Area { get; set; }
            public string? Subarea { get; set; }
            public int? CategoriaId { get; set; }
            public int WorkerId { get; set; }
        }

        public async Task<string?> GetMiSubareaAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();
            return await conn.QueryFirstOrDefaultAsync<string>(
                @"SELECT w.subarea
                  FROM workers w
                  JOIN person p ON p.person_id = w.person_id
                  WHERE w.state AND p.user_id = @UserId
                  LIMIT 1",
                new { UserId = userId });
        }

        private static async Task<List<EvEvaluacionResidenteResponseDto>> MapToResponseDtos(
            AppDbContext ctx, List<EvEvaluacionResidente> evals)
        {
            var userIds = evals
                .SelectMany(e => new int?[] { e.EvaluadorUserId, e.EvaluadoUserId })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var persons = await ctx.Person
                .Where(p => p.UserId.HasValue && userIds.Contains(p.UserId.Value))
                .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

            return evals.Select(e => new EvEvaluacionResidenteResponseDto
            {
                Id = e.Id,
                PeriodoId = e.PeriodoId,
                NombreMes = e.Periodo != null
                    ? new DateTime(e.Periodo.Anio, e.Periodo.Mes, 1)
                        .ToString("MMMM", new System.Globalization.CultureInfo("es-PE"))
                    : "",
                EvaluadorUserId = e.EvaluadorUserId ?? 0,
                EvaluadorNombre = e.EvaluadorUserId.HasValue ? persons.GetValueOrDefault(e.EvaluadorUserId.Value, "") : "",
                EvaluadoUserId = e.EvaluadoUserId,
                EvaluadoNombre = persons.GetValueOrDefault(e.EvaluadoUserId, ""),
                ProjectId = e.ProjectId,
                ProjectNombre = e.Project?.ProjectDescription,
                AreaNombre = e.AreaNombre,
                Nota = e.Nota,
                Comentario = e.Comentario,
                NoAplica = e.NoAplica,
                NoAplicaMotivo = e.NoAplicaMotivo,
                CreatedAt = e.CreatedAt,
                Detalles = e.Detalles.Select(d => new EvDetalleResponseDto
                {
                    Id = d.Id,
                    PlantillaId = d.PlantillaId,
                    Criterio = d.Criterio,
                    Puntaje = d.Puntaje,
                    EsNa = d.EsNa
                }).ToList()
            }).ToList();
        }
    }
}
