using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvGestionSsomaRepository : IEvGestionSsomaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        // Jefe SSOMA, Coordinador SSOMA y Prevencionista se resuelven TODOS por el puesto real
        // de Habilitación (workers.puesto_id -> puesto.categoria_id, ver CategoriaIds), nunca
        // por un user_role. El intento anterior de usar el rol de sistema 9 para Jefe SSOMA
        // salió mal: en la práctica ese rol terminó asignado a ~50 cuentas de todas las áreas
        // (Gerentes, Ingenieros, Asistentes...) sin relación con SSOMA, así que no servía para
        // nada — cualquiera de esas cuentas se veía tratada como Jefe SSOMA. Coordinador SSOMA
        // (categoria_id 41) y Prevencionista (categoria_id 35) sí tienen categoría propia;
        // Jefe SSOMA no (comparte la categoría genérica "JEFE" con jefaturas de otras áreas),
        // así que se identifica por su puesto único en el catálogo (PuestoIds.JefeSsoma).
        private const int CategoriaCoordinadorSsoma = CategoriaIds.CoordinadorSsoma;
        private const int CategoriaPrevencionista = CategoriaIds.Prevencionista;

        public EvGestionSsomaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<int?> ObtenerCategoriaPuestoAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();
            return await ObtenerCategoriaDeAsync(conn, userId);
        }

        public async Task<bool> EsJefeSsomaAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();
            return await EsJefeSsomaAsync(conn, userId);
        }

        public async Task<EvGestionSsomaInicioDto> GetInicioAsync(int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var periodo = await conn.QueryFirstOrDefaultAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo WHERE activo = TRUE LIMIT 1");

            var plantillaCoordinador = await conn.QueryAsync<EvSupervisorContratistaCriterioDto>(
                @"SELECT id AS Id, criterio AS Criterio, orden AS Orden
                  FROM ev_gestion_ssoma_plantilla WHERE activo = TRUE AND rol_evaluado = 'COORDINADOR' ORDER BY orden");
            var plantillaPrevencionista = await conn.QueryAsync<EvSupervisorContratistaCriterioDto>(
                @"SELECT id AS Id, criterio AS Criterio, orden AS Orden
                  FROM ev_gestion_ssoma_plantilla WHERE activo = TRUE AND rol_evaluado = 'PREVENCIONISTA' ORDER BY orden");

            var dto = new EvGestionSsomaInicioDto
            {
                PlantillaCoordinador = plantillaCoordinador.ToList(),
                PlantillaPrevencionista = plantillaPrevencionista.ToList(),
            };
            if (periodo == null) return dto;
            dto.Periodo = MapPeriodo(periodo);

            var esJefeSsoma = await EsJefeSsomaAsync(conn, evaluadorUserId);
            var categoriaEvaluador = await ObtenerCategoriaDeAsync(conn, evaluadorUserId);

            var evaluadas = (await conn.QueryAsync<int>(
                "SELECT evaluado_user_id FROM ev_evaluacion_gestion_ssoma WHERE periodo_id = @PeriodoId AND evaluador_user_id = @UserId",
                new { PeriodoId = periodo.Id, UserId = evaluadorUserId })).ToHashSet();

            if (esJefeSsoma)
            {
                dto.Prevencionistas = (await ObtenerPoolCategoriaAsync(conn, CategoriaPrevencionista, proyectoIds: null))
                    .Select(p => ToAEvaluarDto(p, evaluadas)).ToList();
                dto.Coordinadores = (await ObtenerPoolCategoriaAsync(conn, CategoriaCoordinadorSsoma, proyectoIds: null))
                    .Select(p => ToAEvaluarDto(p, evaluadas)).ToList();
            }
            else if (categoriaEvaluador == CategoriaCoordinadorSsoma)
            {
                var misProyectos = await ObtenerProyectosDeAsync(conn, evaluadorUserId);
                if (misProyectos.Count > 0)
                    dto.Prevencionistas = (await ObtenerPoolCategoriaAsync(conn, CategoriaPrevencionista, misProyectos))
                        .Select(p => ToAEvaluarDto(p, evaluadas)).ToList();
            }

            if (categoriaEvaluador == CategoriaPrevencionista)
            {
                var misProyectos = await ObtenerProyectosDeAsync(conn, evaluadorUserId);
                if (misProyectos.Count > 0)
                {
                    var coordinadores = await ObtenerPoolCategoriaAsync(conn, CategoriaCoordinadorSsoma, misProyectos);
                    var miCoordinador = coordinadores.OrderBy(c => c.NombreCompleto).FirstOrDefault();
                    if (miCoordinador != null)
                    {
                        dto.MiCoordinador = new EvGestionSsomaAEvaluarDto
                        {
                            UserId = miCoordinador.UserId,
                            NombreCompleto = miCoordinador.NombreCompleto,
                            ProyectoId = miCoordinador.ProyectoId,
                            ProyectoNombre = miCoordinador.ProyectoNombre
                        };
                        dto.YaEvalueMiCoordinador = await YaEvaluoAnonimoAsync(periodo.Id, evaluadorUserId);
                    }

                    // D5: Prevencionista -> otros Prevencionistas de su mismo proyecto (identificada,
                    // igual que D3 — no anónima como D4, porque acá evalúan a un par, no a su jefe).
                    dto.Prevencionistas = (await ObtenerPoolCategoriaAsync(conn, CategoriaPrevencionista, misProyectos))
                        .Where(p => p.UserId != evaluadorUserId)
                        .Select(p => ToAEvaluarDto(p, evaluadas)).ToList();
                }
            }

            return dto;
        }

        public async Task<EvGestionSsomaContextoDto> ResolverContextoEvaluacionAsync(int evaluadorUserId, int? evaluadoUserIdSolicitado)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var esJefeSsoma = await EsJefeSsomaAsync(conn, evaluadorUserId);
            var categoriaEvaluador = await ObtenerCategoriaDeAsync(conn, evaluadorUserId);

            // Prioridad Jefe > Coordinador > Prevencionista para el caso (raro) de
            // que alguien tenga más de un puesto/rol del equipo SSOMA a la vez.
            if (esJefeSsoma)
            {
                if (evaluadoUserIdSolicitado is not int evaluadoId)
                    return new EvGestionSsomaContextoDto { Valido = false, Error = "Debe indicar a quién evalúa." };

                var categoriaEvaluado = await ObtenerCategoriaDeAsync(conn, evaluadoId);

                if (categoriaEvaluado == CategoriaPrevencionista)
                    return new EvGestionSsomaContextoDto
                    {
                        Valido = true, EvaluadorRol = Roles.AdministradorSsoma,
                        EvaluadoUserId = evaluadoId, EvaluadoRol = Roles.Prevencionista
                    };
                if (categoriaEvaluado == CategoriaCoordinadorSsoma)
                    return new EvGestionSsomaContextoDto
                    {
                        Valido = true, EvaluadorRol = Roles.AdministradorSsoma,
                        EvaluadoUserId = evaluadoId, EvaluadoRol = Roles.CoordinadorSsoma
                    };
                return new EvGestionSsomaContextoDto { Valido = false, Error = "La persona indicada no es Prevencionista ni Coordinador SSOMA." };
            }

            if (categoriaEvaluador == CategoriaCoordinadorSsoma)
            {
                if (evaluadoUserIdSolicitado is not int evaluadoId)
                    return new EvGestionSsomaContextoDto { Valido = false, Error = "Debe indicar a quién evalúa." };

                var misProyectos = await ObtenerProyectosDeAsync(conn, evaluadorUserId);
                var prevencionistas = await ObtenerPoolCategoriaAsync(conn, CategoriaPrevencionista, misProyectos);
                var objetivo = prevencionistas.FirstOrDefault(p => p.UserId == evaluadoId);
                if (objetivo == null)
                    return new EvGestionSsomaContextoDto { Valido = false, Error = "Ese Prevencionista no pertenece a su(s) proyecto(s)." };

                return new EvGestionSsomaContextoDto
                {
                    Valido = true, EvaluadorRol = Roles.CoordinadorSsoma,
                    EvaluadoUserId = evaluadoId, EvaluadoRol = Roles.Prevencionista,
                    ProyectoId = objetivo.ProyectoId
                };
            }

            if (categoriaEvaluador == CategoriaPrevencionista)
            {
                var misProyectos = await ObtenerProyectosDeAsync(conn, evaluadorUserId);
                if (misProyectos.Count == 0)
                    return new EvGestionSsomaContextoDto { Valido = false, Error = "No tiene un proyecto asignado este período." };

                // D5: si viene un evaluadoUserId, es una evaluación identificada a otro
                // Prevencionista de su mismo proyecto — distinto de D4 (anónima, sin elegir).
                if (evaluadoUserIdSolicitado is int evaluadoPeerId)
                {
                    var prevencionistas = await ObtenerPoolCategoriaAsync(conn, CategoriaPrevencionista, misProyectos);
                    var objetivo = prevencionistas.FirstOrDefault(p => p.UserId == evaluadoPeerId && p.UserId != evaluadorUserId);
                    if (objetivo == null)
                        return new EvGestionSsomaContextoDto { Valido = false, Error = "Ese Prevencionista no pertenece a su(s) proyecto(s)." };

                    return new EvGestionSsomaContextoDto
                    {
                        Valido = true, EvaluadorRol = Roles.Prevencionista,
                        EvaluadoUserId = objetivo.UserId, EvaluadoRol = Roles.Prevencionista,
                        ProyectoId = objetivo.ProyectoId
                    };
                }

                var coordinadores = await ObtenerPoolCategoriaAsync(conn, CategoriaCoordinadorSsoma, misProyectos);
                var miCoordinador = coordinadores.OrderBy(c => c.NombreCompleto).FirstOrDefault();
                if (miCoordinador == null)
                    return new EvGestionSsomaContextoDto { Valido = false, Error = "Su proyecto no tiene un Coordinador SSOMA asignado — no corresponde esta evaluación." };

                return new EvGestionSsomaContextoDto
                {
                    Valido = true, EsAnonimo = true, EvaluadorRol = Roles.Prevencionista,
                    EvaluadoUserId = miCoordinador.UserId, EvaluadoRol = Roles.CoordinadorSsoma,
                    ProyectoId = miCoordinador.ProyectoId
                };
            }

            return new EvGestionSsomaContextoDto { Valido = false, Error = "Su rol no participa de esta evaluación." };
        }

        public async Task RegistrarAsync(
            int periodoId, int evaluadorUserId, string evaluadorRol,
            int evaluadoUserId, string evaluadoRol, int? proyectoId,
            string? fortalezas, string? oportunidadesMejora,
            List<(int? plantillaId, string criterio, int puntaje)> detalles, decimal nota)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.EvEvaluacionesGestionSsoma.Add(new EvEvaluacionGestionSsoma
            {
                PeriodoId = periodoId,
                EvaluadorUserId = evaluadorUserId,
                EvaluadorRol = evaluadorRol,
                EvaluadoUserId = evaluadoUserId,
                EvaluadoRol = evaluadoRol,
                ProyectoId = proyectoId,
                Nota = nota,
                Fortalezas = fortalezas,
                OportunidadesMejora = oportunidadesMejora,
                Detalles = detalles.Select(d => new EvEvaluacionGestionSsomaDetalle
                {
                    PlantillaId = d.plantillaId,
                    Criterio = d.criterio,
                    Puntaje = d.puntaje
                }).ToList()
            });
            await ctx.SaveChangesAsync();
        }

        public async Task RegistrarAnonimoAsync(
            int periodoId, int evaluadorUserId, int evaluadoUserId, int? proyectoId,
            string? fortalezas, string? oportunidadesMejora,
            List<(int? plantillaId, string criterio, int puntaje)> detalles, decimal nota)
        {
            using var ctx = _factory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();
            try
            {
                ctx.EvEvaluacionesGestionSsoma.Add(new EvEvaluacionGestionSsoma
                {
                    PeriodoId = periodoId,
                    EvaluadorUserId = null,
                    EvaluadorRol = Roles.Prevencionista,
                    EvaluadoUserId = evaluadoUserId,
                    EvaluadoRol = Roles.CoordinadorSsoma,
                    ProyectoId = proyectoId,
                    Nota = nota,
                    Fortalezas = fortalezas,
                    OportunidadesMejora = oportunidadesMejora,
                    Detalles = detalles.Select(d => new EvEvaluacionGestionSsomaDetalle
                    {
                        PlantillaId = d.plantillaId,
                        Criterio = d.criterio,
                        Puntaje = d.puntaje
                    }).ToList()
                });

                ctx.EvEvaluacionesGestionSsomaCumplimiento.Add(new EvEvaluacionGestionSsomaCumplimiento
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

        public async Task<bool> ExisteAsync(int periodoId, int evaluadorUserId, int evaluadoUserId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesGestionSsoma.AnyAsync(e =>
                e.PeriodoId == periodoId && e.EvaluadorUserId == evaluadorUserId && e.EvaluadoUserId == evaluadoUserId);
        }

        public async Task<bool> YaEvaluoAnonimoAsync(int periodoId, int evaluadorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesGestionSsomaCumplimiento.AnyAsync(c =>
                c.PeriodoId == periodoId && c.EvaluadorUserId == evaluadorUserId);
        }

        public async Task<EvGestionSsomaCumplimientoDto> GetCumplimientoAsync(int periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            var prevencionistas = await ObtenerPoolCategoriaAsync(conn, CategoriaPrevencionista, proyectoIds: null);
            var coordinadores = await ObtenerPoolCategoriaAsync(conn, CategoriaCoordinadorSsoma, proyectoIds: null);

            var evaluadasIdentificadas = (await conn.QueryAsync<(int EvaluadorUserId, int EvaluadoUserId)>(
                "SELECT evaluador_user_id AS EvaluadorUserId, evaluado_user_id AS EvaluadoUserId FROM ev_evaluacion_gestion_ssoma WHERE periodo_id = @PeriodoId AND evaluador_user_id IS NOT NULL",
                new { PeriodoId = periodoId })).ToHashSet();

            var completaronAnonimo = (await conn.QueryAsync<int>(
                "SELECT evaluador_user_id FROM ev_evaluacion_gestion_ssoma_cumplimiento WHERE periodo_id = @PeriodoId",
                new { PeriodoId = periodoId })).ToHashSet();

            var pendientes = new List<EvGestionSsomaPendienteDto>();
            int totalEsperadas = 0, totalCompletadas = 0;

            // D3: cada Coordinador SSOMA evalúa a los Prevencionistas de su propio proyecto.
            foreach (var coord in coordinadores)
            {
                if (coord.ProyectoId == null) continue;
                var prevsDelProyecto = prevencionistas.Where(p => p.ProyectoId == coord.ProyectoId);
                foreach (var prev in prevsDelProyecto)
                {
                    totalEsperadas++;
                    if (evaluadasIdentificadas.Contains((coord.UserId, prev.UserId))) totalCompletadas++;
                    else pendientes.Add(new EvGestionSsomaPendienteDto
                    {
                        UserId = coord.UserId, NombreCompleto = coord.NombreCompleto,
                        EmailCorporativo = coord.EmailCorporativo, Relacion = "D3"
                    });
                }
            }

            // D4: cada Prevencionista evalúa (anónimo) a su Coordinador SSOMA del mismo proyecto.
            foreach (var prev in prevencionistas)
            {
                if (prev.ProyectoId == null) continue;
                var tieneCoordinador = coordinadores.Any(c => c.ProyectoId == prev.ProyectoId);
                if (!tieneCoordinador) continue;

                totalEsperadas++;
                if (completaronAnonimo.Contains(prev.UserId)) totalCompletadas++;
                else pendientes.Add(new EvGestionSsomaPendienteDto
                {
                    UserId = prev.UserId, NombreCompleto = prev.NombreCompleto,
                    EmailCorporativo = prev.EmailCorporativo, Relacion = "D4"
                });
            }

            // D5: cada Prevencionista evalúa a los demás Prevencionistas de su mismo proyecto.
            foreach (var evaluador in prevencionistas)
            {
                if (evaluador.ProyectoId == null) continue;
                var paresDelProyecto = prevencionistas.Where(p => p.ProyectoId == evaluador.ProyectoId && p.UserId != evaluador.UserId);
                foreach (var par in paresDelProyecto)
                {
                    totalEsperadas++;
                    if (evaluadasIdentificadas.Contains((evaluador.UserId, par.UserId))) totalCompletadas++;
                    else pendientes.Add(new EvGestionSsomaPendienteDto
                    {
                        UserId = evaluador.UserId, NombreCompleto = evaluador.NombreCompleto,
                        EmailCorporativo = evaluador.EmailCorporativo, Relacion = "D5"
                    });
                }
            }

            // D1/D2: el/los Jefe SSOMA evalúan a todos los Prevencionistas y Coordinadores.
            var jefes = await ObtenerPoolJefeSsomaAsync(conn);
            foreach (var jefe in jefes)
            {
                foreach (var prev in prevencionistas)
                {
                    totalEsperadas++;
                    if (evaluadasIdentificadas.Contains((jefe.UserId, prev.UserId))) totalCompletadas++;
                    else pendientes.Add(new EvGestionSsomaPendienteDto
                    {
                        UserId = jefe.UserId, NombreCompleto = jefe.NombreCompleto,
                        EmailCorporativo = jefe.EmailCorporativo, Relacion = "D1"
                    });
                }
                foreach (var coord in coordinadores)
                {
                    totalEsperadas++;
                    if (evaluadasIdentificadas.Contains((jefe.UserId, coord.UserId))) totalCompletadas++;
                    else pendientes.Add(new EvGestionSsomaPendienteDto
                    {
                        UserId = jefe.UserId, NombreCompleto = jefe.NombreCompleto,
                        EmailCorporativo = jefe.EmailCorporativo, Relacion = "D2"
                    });
                }
            }

            return new EvGestionSsomaCumplimientoDto
            {
                TotalEsperadas = totalEsperadas,
                TotalCompletadas = totalCompletadas,
                Pendientes = pendientes
            };
        }

        public async Task<EvGestionSsomaResultadosDto> GetResultadosAsync(int? periodoId)
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
                var ultimo = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM ev_periodo ORDER BY anio DESC, mes DESC LIMIT 1");
                if (!ultimo.HasValue) return new EvGestionSsomaResultadosDto();
                targetPeriodo = ultimo.Value;
            }

            var periodo = await conn.QueryFirstOrDefaultAsync<EvPeriodoRaw>(
                "SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo WHERE id = @Id",
                new { Id = targetPeriodo });

            var evaluaciones = await conn.QueryAsync<EvalRaw>(
                @"SELECT
                    e.id AS Id,
                    e.evaluador_user_id AS EvaluadorUserId,
                    e.evaluador_rol AS EvaluadorRol,
                    e.evaluado_rol AS EvaluadoRol,
                    COALESCE(pev.full_name, auEv.email, 'Sin identificar') AS EvaluadorNombre,
                    COALESCE(pdo.full_name, auDo.email) AS EvaluadoNombre,
                    e.nota AS Nota,
                    e.fortalezas AS Fortalezas,
                    e.oportunidades_mejora AS OportunidadesMejora,
                    e.created_at AS CreatedAt
                  FROM ev_evaluacion_gestion_ssoma e
                  JOIN app_user auDo ON auDo.user_id = e.evaluado_user_id
                  LEFT JOIN person pdo ON pdo.user_id = auDo.user_id
                  LEFT JOIN app_user auEv ON auEv.user_id = e.evaluador_user_id
                  LEFT JOIN person pev ON pev.user_id = auEv.user_id
                  WHERE e.periodo_id = @PeriodoId
                  ORDER BY e.created_at DESC",
                new { PeriodoId = targetPeriodo });

            var promediosCriterio = await conn.QueryAsync<EvGestionSsomaCriterioPromedioDto>(
                @"SELECT d.criterio AS Criterio, ROUND(AVG(d.puntaje)::NUMERIC, 2) AS Promedio
                  FROM ev_evaluacion_gestion_ssoma_detalle d
                  JOIN ev_evaluacion_gestion_ssoma e ON e.id = d.evaluacion_gestion_ssoma_id
                  WHERE e.periodo_id = @PeriodoId
                  GROUP BY d.criterio, d.plantilla_id
                  ORDER BY MIN(d.id)",
                new { PeriodoId = targetPeriodo });

            var conNota = evaluaciones.Where(e => e.Nota.HasValue).ToList();

            return new EvGestionSsomaResultadosDto
            {
                Periodo = periodo != null ? MapPeriodo(periodo) : null,
                TotalRespuestas = evaluaciones.Count(),
                PromedioGeneral = conNota.Count > 0 ? Math.Round(conNota.Average(e => e.Nota!.Value), 2) : null,
                PromediosPorCriterio = promediosCriterio.ToList(),
                Evaluaciones = evaluaciones.Select(e => new EvGestionSsomaResumenDto
                {
                    Relacion = ResolverRelacion(e.EvaluadorUserId, e.EvaluadorRol, e.EvaluadoRol),
                    EvaluadoNombre = e.EvaluadoNombre,
                    EvaluadorNombre = e.EvaluadorUserId.HasValue ? e.EvaluadorNombre : null,
                    Nota = e.Nota,
                    Fortalezas = e.Fortalezas,
                    OportunidadesMejora = e.OportunidadesMejora,
                    CreatedAt = e.CreatedAt
                }).ToList()
            };
        }

        private static string ResolverRelacion(int? evaluadorUserId, string evaluadorRol, string evaluadoRol)
        {
            if (evaluadorUserId == null) return "D4"; // anónima: Prevencionista -> Coordinador
            if (evaluadorRol == Roles.AdministradorSsoma && evaluadoRol == Roles.Prevencionista) return "D1";
            if (evaluadorRol == Roles.AdministradorSsoma && evaluadoRol == Roles.CoordinadorSsoma) return "D2";
            if (evaluadorRol == Roles.CoordinadorSsoma && evaluadoRol == Roles.Prevencionista) return "D3";
            if (evaluadorRol == Roles.Prevencionista && evaluadoRol == Roles.Prevencionista) return "D5";
            return "?";
        }

        private static EvGestionSsomaAEvaluarDto ToAEvaluarDto(PoolRaw p, HashSet<int> evaluadas) => new()
        {
            UserId = p.UserId,
            NombreCompleto = p.NombreCompleto,
            ProyectoId = p.ProyectoId,
            ProyectoNombre = p.ProyectoNombre,
            YaEvalue = evaluadas.Contains(p.UserId)
        };

        // Trabajadores activos con vinculación vigente y el rol de sistema indicado
        // (mismo criterio que EvContratistaRepository.GetEvaluadoresCandidatosAsync /
        // EvJefeSsomaRepository.GetCumplimientoAsync). proyectoIds = null -> sin filtro
        // de proyecto (alcance compañía, para el Jefe SSOMA).
        // Jefe SSOMA no tiene categoría propia (comparte la genérica "JEFE" con otras áreas),
        // así que su pool se arma por puesto único (PuestoIds.JefeSsoma) en vez de categoría.
        // Sin user_role de por medio: el antiguo role_id 9 estaba asignado a ~50 cuentas sin
        // relación con SSOMA (Gerentes, Ingenieros, Asistentes...), así que no servía de nada.
        private static async Task<List<PoolRaw>> ObtenerPoolJefeSsomaAsync(System.Data.IDbConnection conn)
        {
            var rows = await conn.QueryAsync<PoolRaw>(
                @"SELECT DISTINCT au.user_id AS UserId, p.full_name AS NombreCompleto,
                    w.email_corporativo AS EmailCorporativo,
                    wv.proyecto_id AS ProyectoId, pr.project_description AS ProyectoNombre
                  FROM workers w
                  JOIN person p ON p.person_id = w.person_id
                  JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
                  JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
                  LEFT JOIN project pr ON pr.project_id = wv.proyecto_id
                  WHERE w.state AND w.email_corporativo IS NOT NULL AND w.email_corporativo != ''
                    AND w.contrata_casa = 'Casa'
                    AND w.workers_estado_id = @WorkersEstadoActivo
                    AND w.puesto_id = @PuestoJefeSsoma",
                new { PuestoJefeSsoma = PuestoIds.JefeSsoma, WorkersEstadoActivo = WorkersEstadoIds.Activo });

            return rows.ToList();
        }

        // Trabajadores activos con vinculación vigente cuyo PUESTO pertenece a la categoría
        // indicada (workers.puesto_id -> puesto.categoria_id — Coordinador SSOMA=41,
        // Prevencionista=35). Habilitación ya mantiene esto al día, así que no depende de
        // que alguien recuerde asignar un rol de sistema aparte.
        private static async Task<List<PoolRaw>> ObtenerPoolCategoriaAsync(
            System.Data.IDbConnection conn, int categoriaId, List<int>? proyectoIds)
        {
            var filtroProyecto = proyectoIds != null ? "AND wv.proyecto_id = ANY(@ProyectoIds)" : "";

            var rows = await conn.QueryAsync<PoolRaw>(
                @"SELECT DISTINCT au.user_id AS UserId, p.full_name AS NombreCompleto,
                    w.email_corporativo AS EmailCorporativo,
                    wv.proyecto_id AS ProyectoId, pr.project_description AS ProyectoNombre
                  FROM workers w
                  JOIN person p ON p.person_id = w.person_id
                  JOIN puesto pu ON pu.puesto_id = w.puesto_id AND pu.categoria_id = @CategoriaId
                  JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
                  JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
                  LEFT JOIN project pr ON pr.project_id = wv.proyecto_id
                  WHERE w.state AND w.email_corporativo IS NOT NULL AND w.email_corporativo != ''
                    AND w.contrata_casa = 'Casa'
                    AND w.workers_estado_id = @WorkersEstadoActivo
                    " + filtroProyecto,
                new { CategoriaId = categoriaId, ProyectoIds = proyectoIds?.ToArray() ?? [], WorkersEstadoActivo = WorkersEstadoIds.Activo });

            return rows.ToList();
        }

        // Categoría del puesto actual del usuario logueado (null si no tiene worker propio
        // o su puesto no tiene categoría reconocida) — resuelve "es Coordinador SSOMA /
        // Prevencionista" sin pasar por user_role. Matchea por email (au.email =
        // w.email_corporativo), el MISMO criterio que ObtenerPoolCategoriaAsync — no por
        // person.user_id, que no está sincronizado para todas las cuentas (era el bug real
        // detrás de que José Albines apareciera bien en los reportes pero no pudiera
        // acceder a "Evaluar Jefe SSOMA": el reporte matchea por email, este método antes
        // matcheaba por person.user_id y le daba null).
        private static Task<int?> ObtenerCategoriaDeAsync(System.Data.IDbConnection conn, int userId)
            => conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT pu.categoria_id
                  FROM app_user au
                  JOIN workers w ON LOWER(w.email_corporativo) = LOWER(au.email)
                  JOIN puesto pu ON pu.puesto_id = w.puesto_id
                  WHERE au.user_id = @UserId AND w.state AND w.contrata_casa = 'Casa' AND w.workers_estado_id = 1 /* WorkersEstadoIds.Activo */
                  LIMIT 1",
                new { UserId = userId });

        // Jefe SSOMA no tiene categoría propia (comparte la genérica "JEFE" con otras áreas)
        // así que se identifica por su puesto único en el catálogo (PuestoIds.JefeSsoma),
        // con el mismo criterio de matcheo por email que el resto de este archivo.
        private static Task<bool> EsJefeSsomaAsync(System.Data.IDbConnection conn, int userId)
            => conn.QueryFirstOrDefaultAsync<bool>(
                @"SELECT EXISTS (
                    SELECT 1
                    FROM app_user au
                    JOIN workers w ON LOWER(w.email_corporativo) = LOWER(au.email)
                    WHERE au.user_id = @UserId AND w.state AND w.contrata_casa = 'Casa' AND w.workers_estado_id = 1 /* WorkersEstadoIds.Activo */
                      AND w.puesto_id = @PuestoJefeSsoma
                  )",
                new { UserId = userId, PuestoJefeSsoma = PuestoIds.JefeSsoma });

        // Matchea por email (au.email = w.email_corporativo), el MISMO criterio que
        // ObtenerCategoriaDeAsync/EsJefeSsomaAsync — no por person.user_id, que no está
        // sincronizado para todas las cuentas (mismo bug real que hacía que José Albines
        // apareciera bien en los reportes pero no pudiera acceder a "Evaluar Jefe SSOMA").
        private static async Task<List<int>> ObtenerProyectosDeAsync(System.Data.IDbConnection conn, int userId)
        {
            var proyectos = await conn.QueryAsync<int>(
                @"SELECT DISTINCT wv.proyecto_id
                  FROM app_user au
                  JOIN workers w ON LOWER(w.email_corporativo) = LOWER(au.email)
                  JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
                  WHERE au.user_id = @UserId AND w.state AND w.contrata_casa = 'Casa' AND w.workers_estado_id = 1 /* WorkersEstadoIds.Activo */",
                new { UserId = userId });
            return proyectos.ToList();
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
        private record PoolRaw(int UserId, string NombreCompleto, string EmailCorporativo, int? ProyectoId, string? ProyectoNombre);
        private record EvalRaw(
            int Id, int? EvaluadorUserId, string EvaluadorRol, string EvaluadoRol,
            string EvaluadorNombre, string EvaluadoNombre, decimal? Nota,
            string? Fortalezas, string? OportunidadesMejora, DateTime CreatedAt);
    }
}
