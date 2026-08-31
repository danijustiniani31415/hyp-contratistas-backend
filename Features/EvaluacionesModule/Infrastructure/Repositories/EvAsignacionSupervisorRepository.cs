using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvAsignacionSupervisorRepository : IEvAsignacionSupervisorRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EvAsignacionSupervisorRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<SupervisorConAsignacionesDto>> GetSupervisoresConAsignacionesAsync()
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var supervisores = (await conn.QueryAsync<SupervisorConAsignacionesDto>(
                $@"SELECT
                    w.id           AS WorkerId,
                    p.full_name    AS NombreCompleto,
                    w.subarea      AS Subarea
                  FROM workers w
                  JOIN person p        ON p.person_id = w.person_id
                  LEFT JOIN app_user u ON u.user_id   = p.user_id
                  WHERE w.state AND w.workers_estado_id IN ({WorkersEstadoIds.NoRetiradosSql})
                    AND (u.active = true OR u.user_id IS NULL)
                    AND (
                      (w.subarea = 'Unidad de Proyectos' AND w.obra_oficina_staff_id = {ObraOficinaStaffIds.OficinaCentral} AND w.area = 'Proyectos')
                      OR
                      (w.subarea = 'Planeamiento BIM')
                    )
                  ORDER BY w.subarea, p.full_name")).ToList();

            if (supervisores.Count == 0) return supervisores;

            var workerIds = supervisores.Select(s => s.WorkerId).ToArray();

            var asignaciones = (await conn.QueryAsync<AsignacionRow>(
                @"SELECT
                    eas.supervisor_worker_id AS SupervisorWorkerId,
                    eas.project_id           AS ProjectId,
                    pr.project_description   AS ProjectDescription
                  FROM ev_asignacion_supervisor eas
                  JOIN project pr ON pr.project_id = eas.project_id
                  WHERE eas.supervisor_worker_id = ANY(@WorkerIds)
                    AND eas.activo = true
                  ORDER BY pr.project_description",
                new { WorkerIds = workerIds })).ToList();

            var porSupervisor = asignaciones
                .GroupBy(a => a.SupervisorWorkerId)
                .ToDictionary(g => g.Key, g => g.Select(a => new ProyectoAsignadoDto
                {
                    ProjectId = a.ProjectId,
                    ProjectDescription = a.ProjectDescription
                }).ToList());

            foreach (var sup in supervisores)
                sup.Proyectos = porSupervisor.GetValueOrDefault(sup.WorkerId, []);

            return supervisores;
        }

        public async Task ActualizarAsignacionesAsync(int supervisorWorkerId, List<int> projectIds, int updatedByUserId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            // Desactivar todas las asignaciones activas actuales
            await conn.ExecuteAsync(
                @"UPDATE ev_asignacion_supervisor
                  SET activo = false, updated_at = NOW(), updated_by_user_id = @UserId
                  WHERE supervisor_worker_id = @WorkerId AND activo = true",
                new { WorkerId = supervisorWorkerId, UserId = updatedByUserId });

            if (projectIds.Count == 0) return;

            // Reactivar registros existentes (activos o no) que están en la nueva lista
            var existentes = (await conn.QueryAsync<int>(
                @"SELECT project_id FROM ev_asignacion_supervisor
                  WHERE supervisor_worker_id = @WorkerId AND project_id = ANY(@Ids)",
                new { WorkerId = supervisorWorkerId, Ids = projectIds.ToArray() })).ToHashSet();

            if (existentes.Count > 0)
                await conn.ExecuteAsync(
                    @"UPDATE ev_asignacion_supervisor
                      SET activo = true, updated_at = NOW(), updated_by_user_id = @UserId
                      WHERE supervisor_worker_id = @WorkerId AND project_id = ANY(@Ids)",
                    new { WorkerId = supervisorWorkerId, Ids = existentes.ToArray(), UserId = updatedByUserId });

            // Insertar los que no tienen registro previo
            var nuevos = projectIds.Where(id => !existentes.Contains(id)).ToList();
            foreach (var pid in nuevos)
                await conn.ExecuteAsync(
                    @"INSERT INTO ev_asignacion_supervisor
                        (supervisor_worker_id, project_id, activo, created_at, updated_at, updated_by_user_id)
                      VALUES (@WorkerId, @ProjectId, true, NOW(), NOW(), @UserId)",
                    new { WorkerId = supervisorWorkerId, ProjectId = pid, UserId = updatedByUserId });
        }

        public async Task<List<ProyectoAsignadoDto>> GetProyectosActivosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            return (await conn.QueryAsync<ProyectoAsignadoDto>(
                @"SELECT
                    project_id          AS ProjectId,
                    project_description AS ProjectDescription
                  FROM project
                  WHERE project_description NOT IN
                        ('General', 'Arquitectura Comercial', 'Post Venta', 'Oficina Central')
                  ORDER BY project_description")).ToList();
        }

        private sealed class AsignacionRow
        {
            public int SupervisorWorkerId { get; set; }
            public int ProjectId { get; set; }
            public string? ProjectDescription { get; set; }
        }
    }
}
