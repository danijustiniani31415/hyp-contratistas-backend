using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvJefeSsomaRepository : IEvJefeSsomaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EvJefeSsomaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<EvJefeSsomaInicioDto> GetInicioAsync(int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var periodo = await conn.QueryFirstOrDefaultAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo WHERE activo = TRUE LIMIT 1");

            var plantilla = await conn.QueryAsync<EvSupervisorContratistaCriterioDto>(
                @"SELECT id AS Id, criterio AS Criterio, orden AS Orden
                  FROM ev_jefe_ssoma_plantilla WHERE activo = TRUE ORDER BY orden");

            if (periodo == null)
                return new EvJefeSsomaInicioDto { Plantilla = plantilla.ToList() };

            var yaEvalue = await YaEvaluoAsync(periodo.Id, evaluadorUserId);

            return new EvJefeSsomaInicioDto
            {
                Periodo = MapPeriodo(periodo),
                Plantilla = plantilla.ToList(),
                YaEvalue = yaEvalue
            };
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

        public async Task<bool> EsJefeSsomaPuestoAsync(int userId)
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

        public async Task<bool> YaEvaluoAsync(int periodoId, int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesJefeSsomaCumplimiento.AnyAsync(c =>
                c.PeriodoId == periodoId && c.EvaluadorUserId == evaluadorUserId);
        }

        public async Task RegistrarAsync(
            int periodoId, int evaluadorUserId, string? comentario,
            List<(int? plantillaId, string criterio, int puntaje)> detalles, decimal nota)
        {
            using var ctx = _factory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();
            try
            {
                var eval = new EvEvaluacionJefeSsoma
                {
                    PeriodoId = periodoId,
                    Nota = nota,
                    Comentario = comentario,
                    Detalles = detalles.Select(d => new EvEvaluacionJefeSsomaDetalle
                    {
                        PlantillaId = d.plantillaId,
                        Criterio = d.criterio,
                        Puntaje = d.puntaje
                    }).ToList()
                };
                ctx.EvEvaluacionesJefeSsoma.Add(eval);

                ctx.EvEvaluacionesJefeSsomaCumplimiento.Add(new EvEvaluacionJefeSsomaCumplimiento
                {
                    PeriodoId = periodoId,
                    EvaluadorUserId = evaluadorUserId
                });

                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<EvJefeSsomaCumplimientoDto> GetCumplimientoAsync(int periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            // Mismo criterio que EvContratistaRepository.GetEvaluadoresCandidatosAsync:
            // trabajadores activos con vinculación vigente, aquí filtrados a los PUESTOS
            // que integran el equipo SSOMA (categoria_id: Coordinador SSOMA=41, Prevencionista=35
            // — workers.puesto_id -> puesto.categoria_id, ver CategoriaIds). Antes esto exigía
            // un user_role (70/72) aparte que nadie asignaba en la práctica; el puesto real
            // que ya mantiene Habilitación es la fuente confiable de quién integra el equipo.
            var pool = await conn.QueryAsync<PoolRaw>(
                @"SELECT DISTINCT au.user_id AS UserId, p.full_name AS NombreCompleto, w.email_corporativo AS EmailCorporativo
                  FROM workers w
                  JOIN person p ON p.person_id = w.person_id
                  JOIN puesto pu ON pu.puesto_id = w.puesto_id AND pu.categoria_id IN (@CategoriaCoordinadorSsoma, @CategoriaPrevencionista)
                  JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
                  WHERE w.state AND w.email_corporativo IS NOT NULL AND w.email_corporativo != ''
                    AND w.contrata_casa = 'Casa'
                    AND w.workers_estado_id = @WorkersEstadoActivo
                    AND EXISTS (SELECT 1 FROM worker_vinculaciones wv WHERE wv.worker_id = w.id AND wv.fecha_fin IS NULL)",
                new { CategoriaCoordinadorSsoma = CategoriaIds.CoordinadorSsoma, CategoriaPrevencionista = CategoriaIds.Prevencionista, WorkersEstadoActivo = WorkersEstadoIds.Activo });

            var completaron = (await conn.QueryAsync<int>(
                "SELECT evaluador_user_id FROM ev_evaluacion_jefe_ssoma_cumplimiento WHERE periodo_id = @PeriodoId",
                new { PeriodoId = periodoId })).ToHashSet();

            var pendientes = pool.Where(x => !completaron.Contains(x.UserId))
                .Select(x => new EvJefeSsomaPendienteDto
                {
                    UserId = x.UserId,
                    NombreCompleto = x.NombreCompleto,
                    EmailCorporativo = x.EmailCorporativo
                }).ToList();

            return new EvJefeSsomaCumplimientoDto
            {
                TotalEvaluadores = pool.Count(),
                TotalCompletaron = completaron.Count,
                Pendientes = pendientes
            };
        }

        public async Task<EvJefeSsomaResultadosDto> GetResultadosAsync(int? periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            int targetPeriodo;
            if (periodoId.HasValue)
            {
                targetPeriodo = periodoId.Value;
            }
            else
            {
                var ultimo = await ResolverUltimoPeriodoIdAsync(conn);
                if (!ultimo.HasValue) return new EvJefeSsomaResultadosDto();
                targetPeriodo = ultimo.Value;
            }

            var periodo = await conn.QueryFirstOrDefaultAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo WHERE id = @Id",
                new { Id = targetPeriodo });

            var evaluaciones = await conn.QueryAsync<EvalRaw>(
                "SELECT id AS Id, nota AS Nota, comentario AS Comentario FROM ev_evaluacion_jefe_ssoma WHERE periodo_id = @PeriodoId",
                new { PeriodoId = targetPeriodo });

            var promediosCriterio = await conn.QueryAsync<EvJefeSsomaCriterioPromedioDto>(
                @"SELECT d.criterio AS Criterio, ROUND(AVG(d.puntaje)::NUMERIC, 2) AS Promedio
                  FROM ev_evaluacion_jefe_ssoma_detalle d
                  JOIN ev_evaluacion_jefe_ssoma e ON e.id = d.evaluacion_jefe_ssoma_id
                  WHERE e.periodo_id = @PeriodoId
                  GROUP BY d.criterio, d.plantilla_id
                  ORDER BY MIN(d.id)",
                new { PeriodoId = targetPeriodo });

            var tendencia = await conn.QueryAsync<TendenciaRaw>(
                @"SELECT ep.mes AS Mes, ep.anio AS Anio, ROUND(AVG(e.nota)::NUMERIC, 2) AS Promedio
                  FROM ev_evaluacion_jefe_ssoma e
                  JOIN ev_periodo ep ON ep.id = e.periodo_id
                  WHERE e.periodo_id IN (SELECT id FROM ev_periodo ORDER BY anio DESC, mes DESC LIMIT 6)
                  GROUP BY ep.mes, ep.anio
                  ORDER BY ep.anio, ep.mes");

            var conNota = evaluaciones.Where(e => e.Nota.HasValue).ToList();

            return new EvJefeSsomaResultadosDto
            {
                Periodo = periodo != null ? MapPeriodo(periodo) : null,
                TotalRespuestas = evaluaciones.Count(),
                PromedioGeneral = conNota.Count > 0 ? Math.Round(conNota.Average(e => e.Nota!.Value), 2) : null,
                PromediosPorCriterio = promediosCriterio.ToList(),
                Comentarios = evaluaciones.Where(e => !string.IsNullOrWhiteSpace(e.Comentario)).Select(e => e.Comentario!).ToList(),
                Tendencia = tendencia.Select(t => new EvJefeSsomaTendenciaDto
                {
                    Mes = t.Mes,
                    Anio = t.Anio,
                    NombreMes = new DateTime(t.Anio, t.Mes, 1).ToString("MMM", new System.Globalization.CultureInfo("es-PE")),
                    Promedio = t.Promedio
                }).ToList()
            };
        }

        private static async Task<int?> ResolverUltimoPeriodoIdAsync(System.Data.IDbConnection conn)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var mesAnterior = hoy.AddMonths(-1);

            var delMesAnterior = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT id FROM ev_periodo WHERE mes = @Mes AND anio = @Anio LIMIT 1",
                new { Mes = mesAnterior.Month, Anio = mesAnterior.Year });
            if (delMesAnterior.HasValue) return delMesAnterior;

            return await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT id FROM ev_periodo
                  WHERE anio < @HoyAnio OR (anio = @HoyAnio AND mes <= @HoyMes)
                  ORDER BY anio DESC, mes DESC LIMIT 1",
                new { HoyAnio = hoy.Year, HoyMes = hoy.Month });
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
        private record PoolRaw(int UserId, string NombreCompleto, string EmailCorporativo);
        private record EvalRaw(int Id, decimal? Nota, string? Comentario);
        private record TendenciaRaw(int Mes, int Anio, decimal? Promedio);
    }
}
