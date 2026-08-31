using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvRecordatorioRepository : IEvRecordatorioRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IJefeRevisorResolver _jefeResolver;

        public EvRecordatorioRepository(
            IDbContextFactory<AppDbContext> factory,
            IJefeRevisorResolver jefeResolver)
        {
            _factory = factory;
            _jefeResolver = jefeResolver;
        }

        public async Task<EvPeriodo?> GetPeriodoActivoAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvPeriodos.FirstOrDefaultAsync(p => p.Activo);
        }

        public async Task<EvPeriodo?> GetPeriodoCerradoAyerAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var ayer = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            return await ctx.EvPeriodos
                .FirstOrDefaultAsync(p => !p.Activo && p.FechaCierre == ayer);
        }

        public async Task<List<EvaluadorDto>> GetEvaluadoresPendientesAsync(int periodoId, bool soloSinEvaluar)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            const string filtroBase = @"
                w.email_corporativo IS NOT NULL
                AND w.email_corporativo != ''
                AND " + WorkersPeriodoLaboralSql.NoRetiradoHoy;

            // El jefe al que se le hace CC ya no se deduce de un mapeo subárea → cargo contra
            // cat_jefatura: se resuelve por trabajador con IJefeRevisorResolver (revisor directo
            // → revisor del área → fallback GTH) después de estas consultas, en un solo lote.

            // REGLA 1: Jefes/Coordinadores OC Proyectos, subarea general
            var sqlR1 = $@"
                SELECT DISTINCT
                    au.user_id       AS UserId,
                    w.id             AS WorkerId,
                    p.full_name      AS NombreCompleto,
                    w.email_corporativo AS EmailCorporativo,
                    w.subarea        AS Subarea
                FROM workers w
                JOIN person p    ON p.person_id = w.person_id
                JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
                JOIN puesto pu   ON pu.puesto_id = w.puesto_id
                WHERE w.state AND w.obra_oficina_staff_id = {ObraOficinaStaffIds.OficinaCentral}
                  AND w.area          = 'Proyectos'
                  AND pu.categoria_id IN ({CategoriaIds.Jefe}, {CategoriaIds.Coordinador})
                  AND w.subarea      NOT IN ('Unidad de Proyectos', 'Planeamiento BIM')
                  AND {filtroBase}
                  {(soloSinEvaluar ? @"AND NOT EXISTS (
                      SELECT 1 FROM ev_evaluacion_residente er
                      WHERE er.evaluador_user_id = au.user_id
                        AND er.periodo_id        = @PeriodoId
                  )" : "")}
                ORDER BY p.full_name";

            // REGLA 2: Supervisores UDP/BIM con proyectos asignados en ev_asignacion_supervisor
            var sqlR2 = $@"
                SELECT DISTINCT
                    au.user_id       AS UserId,
                    w.id             AS WorkerId,
                    p.full_name      AS NombreCompleto,
                    w.email_corporativo AS EmailCorporativo,
                    w.subarea        AS Subarea
                FROM workers w
                JOIN person p         ON p.person_id = w.person_id
                LEFT JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
                LEFT JOIN puesto pu   ON pu.puesto_id = w.puesto_id
                WHERE w.state AND w.subarea IN ('Unidad de Proyectos', 'Planeamiento BIM')
                  AND NOT (pu.categoria_id = {CategoriaIds.Gerente} AND w.area = 'Proyectos')
                  AND {filtroBase}
                  AND EXISTS (
                      SELECT 1 FROM ev_asignacion_supervisor eas
                      WHERE eas.supervisor_worker_id = w.id AND eas.activo = true
                  )
                  {(soloSinEvaluar ? $@"AND EXISTS (
                      SELECT 1
                      FROM workers rw
                      JOIN person rp ON rp.person_id = rw.person_id
                      JOIN worker_vinculaciones wv_r ON wv_r.worker_id = rw.id AND wv_r.fecha_fin IS NULL
                      JOIN puesto rpu ON rpu.puesto_id = rw.puesto_id
                      JOIN ev_asignacion_supervisor eas
                                     ON eas.project_id           = wv_r.proyecto_id
                                    AND eas.supervisor_worker_id = w.id
                                    AND eas.activo              = true
                      WHERE rw.state AND rpu.categoria_id = {CategoriaIds.Residente} AND rw.contrata_casa = 'Casa'
                        AND rw.workers_estado_id IN ({WorkersEstadoIds.NoRetiradosSql})
                        AND NOT EXISTS (
                            SELECT 1 FROM ev_evaluacion_residente er
                            WHERE er.evaluado_user_id  = rp.user_id
                              AND er.evaluador_user_id = au.user_id
                              AND er.periodo_id        = @PeriodoId
                        )
                  )" : "")}
                ORDER BY p.full_name";

            // REGLA 3: Staff (obra_oficina_staff_id != Oficina Central) con residente en su proyecto
            var sqlR3 = $@"
                SELECT DISTINCT
                    au.user_id       AS UserId,
                    w.id             AS WorkerId,
                    p.full_name      AS NombreCompleto,
                    w.email_corporativo AS EmailCorporativo,
                    w.subarea        AS Subarea
                FROM workers w
                JOIN person p    ON p.person_id = w.person_id
                JOIN app_user au    ON LOWER(au.email) = LOWER(w.email_corporativo)
                LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
                WHERE w.state AND w.obra_oficina_staff_id <> {ObraOficinaStaffIds.OficinaCentral}
                  AND NOT (pu.categoria_id = {CategoriaIds.Gerente} AND w.area = 'Proyectos')
                  AND {filtroBase}
                  AND EXISTS (
                      SELECT 1
                      FROM workers rw
                      JOIN worker_vinculaciones wv_r ON wv_r.worker_id = rw.id AND wv_r.fecha_fin IS NULL
                      JOIN worker_vinculaciones wv_e ON wv_e.worker_id = w.id  AND wv_e.fecha_fin IS NULL
                      JOIN puesto rpu ON rpu.puesto_id = rw.puesto_id
                      WHERE rw.state AND rpu.categoria_id = {CategoriaIds.Residente} AND rw.contrata_casa = 'Casa'
                        AND rw.workers_estado_id IN ({WorkersEstadoIds.NoRetiradosSql})
                        AND rw.id           != w.id
                        AND wv_r.proyecto_id = wv_e.proyecto_id
                  )
                  {(soloSinEvaluar ? $@"AND EXISTS (
                      SELECT 1
                      FROM workers rw
                      JOIN person rp   ON rp.person_id = rw.person_id
                      JOIN worker_vinculaciones wv_r ON wv_r.worker_id = rw.id AND wv_r.fecha_fin IS NULL
                      JOIN worker_vinculaciones wv_e ON wv_e.worker_id = w.id  AND wv_e.fecha_fin IS NULL
                      JOIN puesto rpu ON rpu.puesto_id = rw.puesto_id
                      WHERE rw.state AND rpu.categoria_id = {CategoriaIds.Residente} AND rw.contrata_casa = 'Casa'
                        AND rw.workers_estado_id IN ({WorkersEstadoIds.NoRetiradosSql})
                        AND rw.id           != w.id
                        AND wv_r.proyecto_id = wv_e.proyecto_id
                        AND NOT EXISTS (
                            SELECT 1 FROM ev_evaluacion_residente er
                            WHERE er.evaluado_user_id  = rp.user_id
                              AND er.evaluador_user_id = au.user_id
                              AND er.periodo_id        = @PeriodoId
                        )
                  )" : "")}
                ORDER BY p.full_name";

            var qParams = new { PeriodoId = periodoId };
            var r1 = await conn.QueryAsync<EvaluadorDto>(sqlR1, qParams);
            var r2 = await conn.QueryAsync<EvaluadorDto>(sqlR2, qParams);
            var r3 = await conn.QueryAsync<EvaluadorDto>(sqlR3, qParams);

            var evaluadores = r1.Concat(r2).Concat(r3).ToList();

            // Jefe de cada evaluador (para el CC del recordatorio) desde la configuración
            // global de revisores, en un solo lote — sin importar cuántos evaluadores haya.
            var jefes = await _jefeResolver.ResolveManyAsync(
                evaluadores.Select(e => e.WorkerId).Distinct().ToList());

            foreach (var ev in evaluadores)
                if (jefes.TryGetValue(ev.WorkerId, out var jefe))
                {
                    ev.JefeEmail = jefe.Email;
                    ev.JefeNombre = jefe.Nombre;
                }

            return evaluadores;
        }

        public async Task RegistrarLogAsync(int periodoId, int? userId, string tipo, string emailDestino, bool ccJefatura, bool ccGerencia)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.EvRecordatorioLogs.Add(new EvRecordatorioLog
            {
                PeriodoId = periodoId,
                UserId = userId,
                Tipo = tipo,
                EmailDestino = emailDestino,
                CcJefatura = ccJefatura,
                CcGerencia = ccGerencia,
                EnviadoAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        public async Task<bool> YaEnvioRecordatorioHoyAsync(int periodoId, int? userId, string tipo)
        {
            using var ctx = _factory.CreateDbContext();
            var hoyUtc = DateTime.UtcNow.Date;
            return await ctx.EvRecordatorioLogs.AnyAsync(r =>
                r.PeriodoId == periodoId &&
                r.UserId == userId &&
                r.Tipo == tipo &&
                r.EnviadoAt >= hoyUtc &&
                r.EnviadoAt < hoyUtc.AddDays(1));
        }
    }
}
