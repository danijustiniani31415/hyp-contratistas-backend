using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvContratistaRepository : IEvContratistaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EvContratistaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<EvEvaluacionContratista> CreateAsync(
            EvEvaluacionContratista eval,
            List<EvEvaluacionContratistaDetalle> detalles)
        {
            using var ctx = _factory.CreateDbContext();

            // Calcular nota normalizada a 20, excluyendo criterios marcados como No Aplica
            int maxPorCriterio = 4;
            var puntajesValidos = detalles.Where(d => !d.EsNa && d.Puntaje.HasValue).Select(d => d.Puntaje!.Value).ToList();
            int totalMax = puntajesValidos.Count * maxPorCriterio;
            decimal sumPuntajes = puntajesValidos.Sum();
            eval.Nota = totalMax > 0 ? Math.Round((sumPuntajes / totalMax) * 20m, 2) : 0;
            eval.Detalles = detalles;

            ctx.EvEvaluacionesContratista.Add(eval);
            await ctx.SaveChangesAsync();
            return eval;
        }

        public async Task<bool> ExisteAsync(
            int periodoId, int proyectoId, int contributorId, string areaNombre, int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesContratista.AnyAsync(e =>
                e.PeriodoId == periodoId &&
                e.ProyectoId == proyectoId &&
                e.ContributorId == contributorId &&
                e.AreaNombre == areaNombre &&
                e.EvaluadorUserId == evaluadorUserId);
        }

        public async Task<List<EvaluadorDto>> GetEvaluadoresCandidatosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var workers = await conn.QueryAsync<CandidatoRaw>(
                @"SELECT DISTINCT
                    au.user_id       AS UserId,
                    p.full_name      AS NombreCompleto,
                    w.email_corporativo AS EmailCorporativo,
                    w.subarea        AS Subarea
                  FROM workers w
                  JOIN person p    ON p.person_id = w.person_id
                  JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
                  WHERE w.state AND w.email_corporativo IS NOT NULL
                    AND w.email_corporativo != ''
                    AND " + WorkersPeriodoLaboralSql.NoRetiradoHoy + @"
                    AND EXISTS (
                        SELECT 1 FROM worker_vinculaciones wv
                        WHERE wv.worker_id = w.id AND wv.fecha_fin IS NULL
                    )");

            // Reutiliza ResolverArea (misma regla que usa GetInicioAsync) para no duplicar
            // el mapeo subárea -> área evaluadora en dos lugares distintos.
            return workers
                .Where(w => ResolverArea(w.Subarea ?? "").AreaNombre != null)
                .Select(w => new EvaluadorDto
                {
                    UserId = w.UserId,
                    NombreCompleto = w.NombreCompleto,
                    EmailCorporativo = w.EmailCorporativo,
                    Subarea = w.Subarea ?? string.Empty
                })
                .ToList();
        }

        public async Task<EvContratistaInicioDto> GetInicioAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            // Período activo
            var periodo = await conn.QueryFirstOrDefaultAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo WHERE activo = TRUE LIMIT 1");

            if (periodo == null)
                return new EvContratistaInicioDto();

            // Subarea del evaluador (para saber qué área evalúa)
            var evaluador = await conn.QueryFirstOrDefaultAsync<EvaluadorInfo>(
                @"SELECT w.id AS WorkerId, w.subarea AS Subarea, w.area AS Area,
                         w.obra_oficina_staff_id AS ObraOficinaStaffId, pu.categoria_id AS CategoriaId
                  FROM workers w
                  JOIN person p ON p.person_id = w.person_id
                  LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
                  WHERE w.state AND p.user_id = @UserId
                  LIMIT 1",
                new { UserId = userId });

            // Mismo criterio que REGLA 2 de EvEvaluacionResidenteRepository: Unidad de
            // Proyectos/Planeamiento BIM no evalúa por su propia vinculación (normalmente
            // "Oficina Central", sin contratistas) sino por los proyectos que le asignaron
            // a mano en ev_asignacion_supervisor (pantalla Evaluaciones > Asignaciones).
            bool esCandidatoPlaneamiento = evaluador != null &&
                ((evaluador.Subarea == "Unidad de Proyectos"
                    && evaluador.ObraOficinaStaffId == ObraOficinaStaffIds.OficinaCentral
                    && evaluador.Area == "Proyectos")
                 || evaluador.Subarea == "Planeamiento BIM");

            // Jefes de área ven contratistas de TODOS los proyectos (no solo el suyo propio).
            bool puedeVerTodos = evaluador?.CategoriaId == CategoriaIds.Jefe;

            // Determinar área según subarea del worker
            string? areaMatch = null;
            string? puestoMatch = null;
            if (evaluador != null)
            {
                (areaMatch, puestoMatch) = ResolverArea(evaluador.Subarea ?? evaluador.Area ?? "");
            }

            if (areaMatch == null)
                return new EvContratistaInicioDto
                {
                    Periodo = MapPeriodo(periodo)
                };

            var yaMarcoNoAplica = await conn.QueryFirstOrDefaultAsync<bool>(
                @"SELECT EXISTS (
                    SELECT 1 FROM ev_evaluacion_contratista
                    WHERE periodo_id = @PeriodoId AND evaluador_user_id = @UserId AND no_aplica = TRUE
                  )",
                new { PeriodoId = periodo.Id, UserId = userId });

            // Plantilla de criterios para esta área
            var plantilla = await conn.QueryAsync<EvContratistaCriterioDto>(
                @"SELECT id AS Id, criterio AS Criterio, orden AS Orden
                  FROM ev_contratista_plantilla
                  WHERE puesto_evaluador ILIKE @Puesto AND activo = TRUE
                  ORDER BY orden",
                new { Puesto = $"%{puestoMatch}%" });

            // Proyectos del evaluador: para UDP/Planeamiento BIM, los asignados a mano en
            // ev_asignacion_supervisor (ver esCandidatoPlaneamiento arriba); para el resto,
            // el/los proyecto(s) donde está actualmente destacado según su vinculación vigente
            // (worker_vinculaciones.fecha_fin IS NULL) — no el contributor_id de onboarding
            // (queda desactualizado si cambia de obra) ni user_project.
            // Los Jefes de área (puedeVerTodos) no se filtran por su propio proyecto: ven todos.
            List<int> proyectoIds = [];
            if (!puedeVerTodos)
            {
                var proyectosEvaluador = esCandidatoPlaneamiento
                    ? await conn.QueryAsync<int>(
                        @"SELECT project_id FROM ev_asignacion_supervisor
                          WHERE supervisor_worker_id = @WorkerId AND activo = true",
                        new { evaluador!.WorkerId })
                    : await conn.QueryAsync<int>(
                        @"SELECT DISTINCT wv.proyecto_id
                          FROM workers w
                          JOIN person p ON p.person_id = w.person_id
                          JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
                          WHERE w.state AND p.user_id = @UserId",
                        new { UserId = userId });

                proyectoIds = proyectosEvaluador.ToList();
                if (!proyectoIds.Any())
                    return new EvContratistaInicioDto
                    {
                        Periodo = MapPeriodo(periodo),
                        MiAreaNombre = areaMatch,
                        MiPuestoEvaluador = puestoMatch,
                        Plantilla = plantilla.ToList(),
                        YaMarcoNoAplica = yaMarcoNoAplica
                    };
            }

            // Contratistas que tuvieron tareo en el mes/año del período activo,
            // filtrados por los proyectos del evaluador (o todos, si es Jefe de área).
            var contratistas = await conn.QueryAsync<ContratistaRaw>(
                @"SELECT
                    c.contributor_id    AS ContributorId,
                    c.contributor_name  AS ContributorNombre,
                    c.contributor_ruc   AS ContributorRuc,
                    t.proyecto_id       AS ProyectoId,
                    pr.project_description AS ProyectoNombre,
                    COUNT(DISTINCT t.fecha)::int AS DiasLaborados
                  FROM ss_tareo_detalle_contratista tdc
                  JOIN ss_tareo t ON t.id = tdc.tareo_id
                  JOIN contributor c ON c.contributor_id = tdc.empresa_id
                  JOIN project pr ON pr.project_id = t.proyecto_id
                  WHERE EXTRACT(MONTH FROM t.fecha) = @Mes
                    AND EXTRACT(YEAR  FROM t.fecha) = @Anio
                    AND (@VerTodos OR t.proyecto_id = ANY(@ProyectoIds))
                  GROUP BY c.contributor_id, c.contributor_name, c.contributor_ruc,
                           t.proyecto_id, pr.project_description
                  ORDER BY c.contributor_name",
                new { Mes = periodo.Mes, Anio = periodo.Anio, VerTodos = puedeVerTodos, ProyectoIds = proyectoIds.ToArray() });

            // Verificar cuáles ya fueron evaluadas (o marcadas No Aplica) por este usuario en esta área
            var yaEvaluadas = await conn.QueryAsync<YaEvaluadaRaw>(
                @"SELECT contributor_id AS ContributorId, proyecto_id AS ProyectoId, nota AS Nota,
                         no_aplica AS NoAplica, no_aplica_motivo AS NoAplicaMotivo
                  FROM ev_evaluacion_contratista
                  WHERE periodo_id = @PeriodoId
                    AND evaluador_user_id = @UserId
                    AND area_nombre = @Area
                    AND contributor_id IS NOT NULL
                    AND proyecto_id IS NOT NULL",
                new { PeriodoId = periodo.Id, UserId = userId, Area = areaMatch });

            var evaluadasMap = yaEvaluadas.ToDictionary(x => (x.ContributorId, x.ProyectoId));

            var aEvaluar = contratistas.Select(c =>
            {
                var key = (c.ContributorId, c.ProyectoId);
                var yaEvalue = evaluadasMap.TryGetValue(key, out var previa);
                return new EvContratistaAEvaluarDto
                {
                    ContributorId = c.ContributorId,
                    ContributorNombre = c.ContributorNombre,
                    ContributorRuc = c.ContributorRuc,
                    ProyectoId = c.ProyectoId,
                    ProyectoNombre = c.ProyectoNombre,
                    DiasLaborados = c.DiasLaborados,
                    YaEvalue = yaEvalue,
                    NotaPrevia = yaEvalue ? previa.Nota : null,
                    NoAplica = yaEvalue && previa.NoAplica,
                    NoAplicaMotivo = yaEvalue ? previa.NoAplicaMotivo : null
                };
            }).ToList();

            return new EvContratistaInicioDto
            {
                Periodo = MapPeriodo(periodo),
                MiAreaNombre = areaMatch,
                MiPuestoEvaluador = puestoMatch,
                Plantilla = plantilla.ToList(),
                ContratistasAEvaluar = aEvaluar,
                PuedeVerTodos = puedeVerTodos,
                YaMarcoNoAplica = yaMarcoNoAplica
            };
        }

        public async Task<bool> ExisteNoAplicaAsync(int periodoId, int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesContratista.AnyAsync(e =>
                e.PeriodoId == periodoId &&
                e.EvaluadorUserId == evaluadorUserId &&
                e.NoAplica);
        }

        public async Task RegistrarNoAplicaAsync(
            int periodoId, int evaluadorUserId, string areaNombre, string motivo,
            int? proyectoId = null, int? contributorId = null)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.EvEvaluacionesContratista.Add(new EvEvaluacionContratista
            {
                PeriodoId = periodoId,
                EvaluadorUserId = evaluadorUserId,
                AreaNombre = areaNombre,
                ProyectoId = proyectoId,
                ContributorId = contributorId,
                NoAplica = true,
                NoAplicaMotivo = motivo
            });
            await ctx.SaveChangesAsync();
        }

        public async Task<EvContratistaVerInicioDto> GetVerInicioAsync(int? periodoId, int? proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            // Periodos disponibles
            var periodos = await conn.QueryAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo ORDER BY anio DESC, mes DESC LIMIT 24");

            // Proyectos con evaluaciones
            var proyectos = await conn.QueryAsync<EvContratistaProyectoFiltroDto>(
                @"SELECT DISTINCT pr.project_id AS ProyectoId, pr.project_description AS ProyectoNombre
                  FROM ev_evaluacion_contratista ec
                  JOIN project pr ON pr.project_id = ec.proyecto_id
                  ORDER BY pr.project_description");

            // Período a usar: el solicitado o, a falta de uno, el último registrado (no el
            // "activo" — este listado es de solo lectura y debe verse aunque hoy no esté
            // abierta la ventana de evaluación).
            int targetPeriodo;
            if (periodoId.HasValue)
            {
                targetPeriodo = periodoId.Value;
            }
            else
            {
                var ultimo = await ResolverUltimoPeriodoIdAsync(conn);
                if (!ultimo.HasValue)
                    return new EvContratistaVerInicioDto
                    {
                        Periodos = periodos.Select(MapPeriodo).ToList(),
                        Proyectos = proyectos.ToList()
                    };
                targetPeriodo = ultimo.Value;
            }

            var evaluaciones = await ObtenerResumenesAsync(conn, targetPeriodo, proyectoId);

            return new EvContratistaVerInicioDto
            {
                Periodos = periodos.Select(MapPeriodo).ToList(),
                Proyectos = proyectos.ToList(),
                Evaluaciones = evaluaciones
            };
        }

        public async Task<EvContratistaDashboardDto> GetDashboardAsync(int? periodoId, int? proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            // Período objetivo: el solicitado o, a falta de uno, el último registrado (no el
            // "activo" — el dashboard es de solo lectura y debe verse aunque hoy no esté
            // abierta la ventana de evaluación).
            int targetPeriodo;
            if (periodoId.HasValue)
            {
                targetPeriodo = periodoId.Value;
            }
            else
            {
                var ultimo = await ResolverUltimoPeriodoIdAsync(conn);
                if (!ultimo.HasValue)
                    return new EvContratistaDashboardDto();
                targetPeriodo = ultimo.Value;
            }

            var contratistas = await ObtenerResumenesAsync(conn, targetPeriodo, proyectoId);

            // Promedios por área
            var promediosArea = await conn.QueryAsync<EvContratistaAreaPromedioDto>(
                @"SELECT
                    area_nombre AS AreaNombre,
                    ROUND(AVG(nota)::NUMERIC, 1) AS Promedio,
                    COUNT(*) AS TotalEvaluaciones
                  FROM ev_evaluacion_contratista
                  WHERE periodo_id = @PeriodoId
                    AND (@ProyectoId IS NULL OR proyecto_id = @ProyectoId)
                  GROUP BY area_nombre
                  ORDER BY area_nombre",
                new { PeriodoId = targetPeriodo, ProyectoId = proyectoId });

            // Tendencia histórica (últimos 6 períodos)
            var tendencia = await conn.QueryAsync<EvContratistaTendenciaRaw>(
                @"SELECT
                    ep.mes AS Mes, ep.anio AS Anio,
                    ec.contributor_id AS ContributorId,
                    c.contributor_name AS ContributorNombre,
                    ROUND(AVG(ec.nota)::NUMERIC, 1) AS NotaTotal
                  FROM ev_evaluacion_contratista ec
                  JOIN ev_periodo ep ON ep.id = ec.periodo_id
                  JOIN contributor c ON c.contributor_id = ec.contributor_id
                  WHERE ec.periodo_id IN (
                      SELECT id FROM ev_periodo ORDER BY anio DESC, mes DESC LIMIT 6
                  )
                    AND (@ProyectoId IS NULL OR ec.proyecto_id = @ProyectoId)
                  GROUP BY ep.mes, ep.anio, ec.contributor_id, c.contributor_name
                  ORDER BY ep.anio, ep.mes, c.contributor_name",
                new { ProyectoId = proyectoId });

            var tendenciaDto = tendencia.Select(t => new EvContratistaTendenciaDto
            {
                Mes = t.Mes,
                Anio = t.Anio,
                NombreMes = new DateTime(t.Anio, t.Mes, 1).ToString("MMM", new System.Globalization.CultureInfo("es-PE")),
                ContributorId = t.ContributorId,
                ContributorNombre = t.ContributorNombre,
                NotaTotal = t.NotaTotal
            }).ToList();

            var aprobados = contratistas.Count(c => c.NotaTotal.HasValue && c.NotaTotal.Value > 15);
            var regulares = contratistas.Count(c => c.NotaTotal.HasValue && c.NotaTotal.Value >= 12 && c.NotaTotal.Value <= 15);
            var desaprobados = contratistas.Count(c => c.NotaTotal.HasValue && c.NotaTotal.Value < 12);

            decimal? promedioGeneral = null;
            var conNota = contratistas.Where(c => c.NotaTotal.HasValue).ToList();
            if (conNota.Any())
                promedioGeneral = Math.Round(conNota.Average(c => c.NotaTotal!.Value), 1);

            return new EvContratistaDashboardDto
            {
                TotalContratistas = contratistas.Count,
                Aprobados = aprobados,
                Regulares = regulares,
                Desaprobados = desaprobados,
                PromedioGeneral = promedioGeneral,
                Contratistas = contratistas,
                PromediosPorArea = promediosArea.ToList(),
                Tendencia = tendenciaDto
            };
        }

        // ─── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Período a usar cuando no se pide uno explícito: siempre el mes calendario anterior
        /// al actual (el último que ya cerró por completo). No toma el mayor (anio, mes) de la
        /// tabla a secas porque puede haber períodos futuros o de prueba sembrados de antemano
        /// (p. ej. para poblar la tendencia histórica de gráficos) sin evaluaciones reales.
        /// </summary>
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

        private static async Task<List<EvContratistaResumenDto>> ObtenerResumenesAsync(
            System.Data.IDbConnection conn, int periodoId, int? proyectoId)
        {
            var notas = await conn.QueryAsync<NotaAreaRaw>(
                @"SELECT
                    ec.contributor_id   AS ContributorId,
                    c.contributor_name  AS ContributorNombre,
                    c.contributor_ruc   AS ContributorRuc,
                    ec.proyecto_id      AS ProyectoId,
                    pr.project_description AS ProyectoNombre,
                    ec.area_nombre      AS AreaNombre,
                    ec.nota             AS Nota
                  FROM ev_evaluacion_contratista ec
                  JOIN contributor c ON c.contributor_id = ec.contributor_id
                  JOIN project pr    ON pr.project_id    = ec.proyecto_id
                  WHERE ec.periodo_id = @PeriodoId
                    AND (@ProyectoId IS NULL OR ec.proyecto_id = @ProyectoId)
                  ORDER BY c.contributor_name, pr.project_description",
                new { PeriodoId = periodoId, ProyectoId = proyectoId });

            return notas
                .GroupBy(n => (n.ContributorId, n.ProyectoId))
                .Select(g =>
                {
                    var first = g.First();
                    decimal? NotaDeArea(string area) =>
                        g.FirstOrDefault(x => x.AreaNombre.Equals(area, StringComparison.OrdinalIgnoreCase))?.Nota;

                    var notasValidas = g.Where(x => x.Nota.HasValue).Select(x => x.Nota!.Value).ToList();
                    decimal? total = notasValidas.Any() ? Math.Round(notasValidas.Average(), 1) : null;

                    string estado = total switch
                    {
                        null => "Sin evaluar",
                        > 15 => "Aprobado",
                        >= 12 => "Regular",
                        _ => "Desaprobado"
                    };

                    return new EvContratistaResumenDto
                    {
                        ContributorId = first.ContributorId,
                        ContributorNombre = first.ContributorNombre,
                        ContributorRuc = first.ContributorRuc,
                        ProyectoId = first.ProyectoId,
                        ProyectoNombre = first.ProyectoNombre,
                        NotaSsoma = NotaDeArea("SSOMA"),
                        NotaOT = NotaDeArea("Oficina Técnica"),
                        NotaProduccion = NotaDeArea("Producción"),
                        NotaResidencia = NotaDeArea("Residencia"),
                        NotaCalidad = NotaDeArea("Calidad"),
                        NotaAdministracion = NotaDeArea("Administración de Obra"),
                        NotaTotal = total,
                        Estado = estado
                    };
                })
                .OrderByDescending(c => c.NotaTotal)
                .ToList();
        }

        // Mapea subarea/area del worker a la área de evaluación de contratistas
        private static (string? AreaNombre, string? PuestoEvaluador) ResolverArea(string subarea)
        {
            if (string.IsNullOrWhiteSpace(subarea)) return (null, null);
            var s = subarea.ToUpperInvariant();
            if (s.Contains("ADMINISTRACI") && s.Contains("OBRA")) return ("Administración de Obra", "Administrador de Obra");
            if (s.Contains("SSOMA")) return ("SSOMA", "Responsable SSOMA");
            if (s.Contains("OFICINA") || s.Contains("TÉCNICA") || s.Contains("TECNICA") || s.Contains("COSTOS Y PRESUPUESTOS")) return ("Oficina Técnica", "Jefe de Oficina Técnica");
            if (s.Contains("PRODUCCI") || s.Contains("ING.PROD") || s.Contains("ING. PROD")) return ("Producción", "Residente / Ingeniero de Producción");
            if (s.Contains("CALIDAD")) return ("Calidad", "Responsable de Calidad");
            if (s.Contains("RESIDEN")) return ("Residencia", "Residente de Obra");
            return (null, null);
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

        // ─── Raw helpers ───────────────────────────────────────────────────────
        private record CandidatoRaw(int UserId, string NombreCompleto, string EmailCorporativo, string? Subarea);
        private record EvPeriodoRaw(int Id, int Mes, int Anio, DateOnly FechaApertura, DateOnly FechaCierre, bool Activo);
        private record EvaluadorInfo(int WorkerId, string? Subarea, string? Area, int? ObraOficinaStaffId, int? CategoriaId);
        public async Task<List<EmpresaResultadoEnvioDto>> GetResultadosParaEnvioAsync(int periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var evaluaciones = await ObtenerResumenesAsync(conn, periodoId, null);

            var supervisoresRaw = (await conn.QueryAsync<SupervisorRaw>(
                @"SELECT esc.contributor_id       AS ContributorId,
                         esc.supervisor_nombre     AS SupervisorNombre,
                         pr.project_description    AS ProyectoNombre,
                         esc.nota                  AS Nota
                  FROM ev_evaluacion_supervisor_contratista esc
                  JOIN project pr ON pr.project_id = esc.proyecto_id
                  WHERE esc.periodo_id = @PeriodoId AND esc.nota IS NOT NULL
                  ORDER BY esc.supervisor_nombre",
                new { PeriodoId = periodoId })).ToList();

            var contributorIds = evaluaciones.Select(e => e.ContributorId)
                .Union(supervisoresRaw.Select(s => s.ContributorId))
                .Distinct()
                .ToList();

            if (contributorIds.Count == 0) return [];

            var empresas = await ctx.Contributor
                .Where(c => contributorIds.Contains(c.ContributorId))
                .Select(c => new { c.ContributorId, c.ContributorName, c.EmailAdministrador })
                .ToDictionaryAsync(x => x.ContributorId);

            return contributorIds.Select(id =>
            {
                empresas.TryGetValue(id, out var info);
                return new EmpresaResultadoEnvioDto
                {
                    ContributorId = id,
                    ContributorNombre = info?.ContributorName ?? "",
                    EmailAdministrador = info?.EmailAdministrador,
                    Evaluaciones = evaluaciones.Where(e => e.ContributorId == id).ToList(),
                    Supervisores = supervisoresRaw
                        .Where(s => s.ContributorId == id)
                        .Select(s => new SupervisorResultadoDto
                        {
                            SupervisorNombre = s.SupervisorNombre,
                            ProyectoNombre = s.ProyectoNombre,
                            Nota = s.Nota
                        })
                        .ToList()
                };
            }).ToList();
        }

        private record SupervisorRaw(int ContributorId, string SupervisorNombre, string ProyectoNombre, decimal? Nota);

        private record ContratistaRaw(int ContributorId, string ContributorNombre, string ContributorRuc, int ProyectoId, string ProyectoNombre, int DiasLaborados);
        private record YaEvaluadaRaw(int ContributorId, int ProyectoId, decimal? Nota, bool NoAplica, string? NoAplicaMotivo);
        private record NotaAreaRaw(int ContributorId, string ContributorNombre, string ContributorRuc, int ProyectoId, string ProyectoNombre, string AreaNombre, decimal? Nota);
        private record EvContratistaTendenciaRaw(int Mes, int Anio, int ContributorId, string ContributorNombre, decimal? NotaTotal);
    }
}
