using Abril_Backend.Features.Habilitacion.Application.Dtos.Dashboard;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Dapper;
using Npgsql;
using System.Data;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class DashboardHabRepository : IDashboardHabRepository
    {
        static DashboardHabRepository()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        private readonly IConfiguration _configuration;
        private readonly IHabTrabajadorRepository _habTrabajadorRepo;

        public DashboardHabRepository(
            IConfiguration configuration,
            IHabTrabajadorRepository habTrabajadorRepo)
        {
            _configuration = configuration;
            _habTrabajadorRepo = habTrabajadorRepo;
        }

        private IDbConnection CreateConnection()
            => new NpgsqlConnection(_configuration["Database:PostgreSQL"]);

        private const int TopN = 30;

        private class EntregableRaw
        {
            public string Entidad { get; set; } = "";
            public string Item { get; set; } = "";
            public DateOnly? Vigencia { get; set; }
            public string? ContrataCasa { get; set; }
        }

        private class InterconsultaRaw
        {
            public int WorkerId { get; set; }
            public string Nombre { get; set; } = "";
            public string Empresa { get; set; } = "";
            public string Especialidad { get; set; } = "";
            public int DiasDesdeDerivacion { get; set; }
        }

        public async Task<DashboardAdminDto> GetResumenAsync(int proyectoId)
        {
            // ── Trabajadores (fuente canónica compartida con "Trabajadores" y "Control de Acceso") ──
            var (workers, _) = await _habTrabajadorRepo.GetWorkersHabilitacionAsync(
                search: null, empresaId: null, proyectoId: proyectoId,
                estadoHabilitacion: null, contratistaCasa: null,
                page: 1, pageSize: 5000);

            var porEmpresa = workers
                .Where(w => w.EmpresaId.HasValue)
                .GroupBy(w => new { Id = w.EmpresaId!.Value, Nombre = w.EmpresaNombre ?? "" })
                .Select(g => new EmpresaResumenDto
                {
                    EmpresaId = g.Key.Id,
                    Nombre = g.Key.Nombre,
                    Habilitada = g.First().EmpresaHabilitada,
                    WorkersTotal = g.Count(),
                    WorkersHabilitados = g.Count(w => w.EstadoHabilitacion == "Habilitado"),
                    WorkersNoAutorizados = g.Count(w => w.EstadoHabilitacion == "No Autorizado"),
                })
                .OrderBy(e => e.Nombre)
                .ToList();

            var noAutorizados = workers
                .Where(w => w.EstadoHabilitacion == "No Autorizado")
                .OrderBy(w => w.ApellidoNombre)
                .Take(TopN)
                .Select(w => new WorkerNombradoDto
                {
                    WorkerId = w.WorkerId,
                    Nombre = w.ApellidoNombre,
                    Dni = w.Dni,
                    Empresa = w.EmpresaNombre ?? "",
                    Motivo = !w.EmpresaHabilitada ? "Empresa no habilitada (SSOMA)" : "Documentación pendiente",
                })
                .ToList();

            var casa = workers.Where(w => w.ContrataCasa == "Casa").ToList();
            var contratista = workers.Where(w => w.ContrataCasa != "Casa").ToList();
            var casaNoHabilitados = casa
                .Where(w => w.EstadoHabilitacion != "Habilitado")
                .OrderBy(w => w.ApellidoNombre)
                .Take(TopN)
                .Select(w => new WorkerNombradoDto
                {
                    WorkerId = w.WorkerId,
                    Nombre = w.ApellidoNombre,
                    Dni = w.Dni,
                    Empresa = w.EmpresaNombre ?? "",
                    Motivo = w.EstadoHabilitacion,
                })
                .ToList();

            // ── EMOs vencidos (mismo filtro que usa la pantalla Trabajadores) ──
            var (emoVencidosWorkers, emoVencidosTotal) = await _habTrabajadorRepo.GetWorkersHabilitacionAsync(
                search: null, empresaId: null, proyectoId: proyectoId,
                estadoHabilitacion: null, contratistaCasa: null,
                page: 1, pageSize: 200, soloEmoVencido: true);

            var emosVencidos = emoVencidosWorkers
                .OrderBy(w => w.ApellidoNombre)
                .Take(TopN)
                .Select(w => new WorkerNombradoDto
                {
                    WorkerId = w.WorkerId,
                    Nombre = w.ApellidoNombre,
                    Dni = w.Dni,
                    Empresa = w.EmpresaNombre ?? "",
                    Motivo = "EMO vencido",
                })
                .ToList();

            // ── Interconsultas pendientes, acotadas al proyecto ──
            const string interconsultasSql = @"
SELECT
    w.id                                                            AS worker_id,
    COALESCE(per.full_name, '')                                     AS nombre,
    COALESCE(ec.contributor_name, '')                                AS empresa,
    ic.especialidad                                                 AS especialidad,
    (CURRENT_DATE - ic.fecha_derivacion)::int                       AS dias_desde_derivacion
FROM ss_interconsultas ic
JOIN workers w ON w.id = ic.worker_id AND w.state
LEFT JOIN person per ON per.person_id = w.person_id
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL AND wv.proyecto_id = @ProyectoId
LEFT JOIN contributor ec ON ec.contributor_id = wv.empresa_id
WHERE ic.estado = 'Pendiente'
ORDER BY ic.fecha_derivacion ASC";

            // ── Entregables de empresa y de trabajador, acotados al proyecto ──
            const string entregablesSql = @"
SELECT ec.contributor_name AS entidad, i.nombre AS item, he.vigencia AS vigencia
FROM ss_hab_empresa he
JOIN ss_item_empresa i ON i.id = he.item_id
JOIN contributor ec ON ec.contributor_id = he.empresa_id
WHERE he.proyecto_id = @ProyectoId AND he.estado = 'Enviado' AND he.vigencia < NOW()
ORDER BY he.vigencia ASC;

SELECT ec.contributor_name AS entidad, i.nombre AS item, he.vigencia AS vigencia
FROM ss_hab_empresa he
JOIN ss_item_empresa i ON i.id = he.item_id
JOIN contributor ec ON ec.contributor_id = he.empresa_id
WHERE he.proyecto_id = @ProyectoId AND he.estado = 'Falta'
ORDER BY ec.contributor_name;

SELECT COALESCE(per.full_name, '') AS entidad, i.nombre AS item, ht.vigencia AS vigencia, w.contrata_casa AS contrata_casa
FROM ss_hab_trabajador ht
JOIN ss_item_trabajador i ON i.id = ht.item_id
JOIN workers w ON w.id = ht.worker_id AND w.state
LEFT JOIN person per ON per.person_id = w.person_id
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL AND wv.proyecto_id = @ProyectoId
WHERE ht.estado = 'Enviado' AND ht.vigencia < NOW()
  AND i.id <> 25 AND NOT (w.contrata_casa = 'Casa' AND i.id = 4)
ORDER BY ht.vigencia ASC;

SELECT COALESCE(per.full_name, '') AS entidad, i.nombre AS item, ht.vigencia AS vigencia, w.contrata_casa AS contrata_casa
FROM ss_hab_trabajador ht
JOIN ss_item_trabajador i ON i.id = ht.item_id
JOIN workers w ON w.id = ht.worker_id AND w.state
LEFT JOIN person per ON per.person_id = w.person_id
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL AND wv.proyecto_id = @ProyectoId
WHERE ht.estado = 'Falta'
  AND i.id <> 25 AND NOT (w.contrata_casa = 'Casa' AND i.id = 4)
ORDER BY per.full_name;

SELECT project_description FROM project WHERE project_id = @ProyectoId;";

            using var conn = CreateConnection();

            var interconsultasRaw = (await conn.QueryAsync<InterconsultaRaw>(interconsultasSql, new { ProyectoId = proyectoId })).ToList();

            using var multi = await conn.QueryMultipleAsync(entregablesSql, new { ProyectoId = proyectoId });
            var empresaVencidosRaw = (await multi.ReadAsync<EntregableRaw>()).ToList();
            var empresaFaltaRaw = (await multi.ReadAsync<EntregableRaw>()).ToList();
            var trabajadorVencidosRaw = (await multi.ReadAsync<EntregableRaw>()).ToList();
            var trabajadorFaltaRaw = (await multi.ReadAsync<EntregableRaw>()).ToList();
            var proyectoNombre = await multi.ReadFirstOrDefaultAsync<string>() ?? "";

            // Entregables de trabajador se reportan por separado para contratista y
            // personal casa (antes venían mezclados en un solo conteo).
            var contratistaVencidosRaw = trabajadorVencidosRaw.Where(r => r.ContrataCasa != "Casa").ToList();
            var casaVencidosRaw = trabajadorVencidosRaw.Where(r => r.ContrataCasa == "Casa").ToList();
            var contratistaFaltaRaw = trabajadorFaltaRaw.Where(r => r.ContrataCasa != "Casa").ToList();
            var casaFaltaRaw = trabajadorFaltaRaw.Where(r => r.ContrataCasa == "Casa").ToList();

            static List<EntregableNombradoDto> ToDto(List<EntregableRaw> raw) => raw
                .Take(TopN)
                .Select(r => new EntregableNombradoDto { Entidad = r.Entidad, Item = r.Item, Vigencia = r.Vigencia?.ToDateTime(TimeOnly.MinValue) })
                .ToList();

            return new DashboardAdminDto
            {
                ProyectoId = proyectoId,
                ProyectoNombre = proyectoNombre,
                Kpis = new DashboardKpisDto
                {
                    EmpresasActivas = porEmpresa.Count,
                    EmpresasHabilitadas = porEmpresa.Count(e => e.Habilitada),
                    EmpresasNoHabilitadas = porEmpresa.Count(e => !e.Habilitada),
                    WorkersTotal = contratista.Count,
                    WorkersHabilitados = contratista.Count(w => w.EstadoHabilitacion == "Habilitado"),
                    WorkersNoAutorizados = contratista.Count(w => w.EstadoHabilitacion == "No Autorizado"),
                    WorkersAutorizadoTemporal = contratista.Count(w => w.EstadoHabilitacion == "Autorizado Temporalmente"),
                    EntregablesEmpresaVencidos = empresaVencidosRaw.Count,
                    EntregablesEmpresaFalta = empresaFaltaRaw.Count,
                    EntregablesTrabajadorVencidos = contratistaVencidosRaw.Count,
                    EntregablesTrabajadorFalta = contratistaFaltaRaw.Count,
                    EntregablesCasaVencidos = casaVencidosRaw.Count,
                    EntregablesCasaFalta = casaFaltaRaw.Count,
                    EmosVencidos = emoVencidosTotal,
                    InterconsultasPendientes = interconsultasRaw.Count,
                    PersonalCasaTotal = casa.Count,
                    PersonalCasaHabilitado = casa.Count(w => w.EstadoHabilitacion == "Habilitado"),
                    PersonalCasaNoHabilitado = casa.Count(w => w.EstadoHabilitacion != "Habilitado"),
                },
                Empresas = porEmpresa,
                TrabajadoresNoAutorizados = noAutorizados,
                EntregablesEmpresaVencidos = ToDto(empresaVencidosRaw),
                EntregablesEmpresaFalta = ToDto(empresaFaltaRaw),
                EntregablesTrabajadorVencidos = ToDto(contratistaVencidosRaw),
                EntregablesTrabajadorFalta = ToDto(contratistaFaltaRaw),
                EntregablesCasaVencidos = ToDto(casaVencidosRaw),
                EntregablesCasaFalta = ToDto(casaFaltaRaw),
                EmosVencidos = emosVencidos,
                Interconsultas = interconsultasRaw.Take(TopN).Select(r => new InterconsultaNombradaDto
                {
                    WorkerId = r.WorkerId,
                    Nombre = r.Nombre,
                    Empresa = r.Empresa,
                    Especialidad = r.Especialidad,
                    DiasDesdeDerivacion = r.DiasDesdeDerivacion,
                }).ToList(),
                PersonalCasaNoHabilitado = casaNoHabilitados,
            };
        }
    }
}
