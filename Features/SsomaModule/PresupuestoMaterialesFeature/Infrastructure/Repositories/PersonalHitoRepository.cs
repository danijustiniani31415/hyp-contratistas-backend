using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Dapper;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class PersonalHitoRepository : IPersonalHitoRepository
{
    private readonly IConfiguration _config;
    public PersonalHitoRepository(IConfiguration config) => _config = config;
    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]!);

    // Cronograma vigente de un proyecto: la versión de historial marcada como la actual.
    private const string CronogramaVigenteCte = """
        cronograma_vigente AS (
            SELECT ms.milestone_schedule_id, ms.milestone_id, ms.custom_description,
                   ms.planned_start_date, ms.es_hito_critico
            FROM milestone_schedule ms
            JOIN milestone_schedule_history msh
              ON msh.milestone_schedule_history_id = ms.milestone_schedule_history_id
            WHERE msh.project_id = @projectId
              AND msh.is_equal_to_last_version = true
              AND msh.active = true
              AND ms.active = true
              AND ms.es_hito_critico = true
        )
        """;

    public async Task<List<HitoCriticoDisponibleDto>> ObtenerHitosCriticosAsync(int projectId)
    {
        using var conn = Conn();
        var sql = $"""
            WITH {CronogramaVigenteCte}
            SELECT cv.milestone_schedule_id AS HitoId,
                   COALESCE(m.milestone_description, cv.custom_description, 'Hito') AS HitoDescripcion,
                   cv.planned_start_date AS HitoFecha
            FROM cronograma_vigente cv
            LEFT JOIN milestone m ON m.milestone_id = cv.milestone_id
            ORDER BY cv.planned_start_date NULLS LAST
            """;
        var result = await conn.QueryAsync<HitoCriticoDisponibleDto>(sql, new { projectId });
        return result.ToList();
    }

    public async Task<List<PersonalHitoDto>> ObtenerPorProyectoAsync(int projectId)
    {
        using var conn = Conn();
        var sql = $"""
            WITH {CronogramaVigenteCte}
            SELECT ph.id AS Id, ph.hito_id AS HitoId,
                   COALESCE(m.milestone_description, cv.custom_description, 'Hito') AS HitoDescripcion,
                   cv.planned_start_date AS HitoFecha,
                   cv.es_hito_critico AS EsHitoCritico,
                   ph.rol AS Rol, ph.cantidad AS Cantidad, ph.semanas AS Semanas,
                   ph.costo_mensual AS CostoMensual, ph.total AS Total
            FROM ss_presupuesto_personal_hito ph
            JOIN ss_presupuesto p ON p.id = ph.presupuesto_id
            JOIN cronograma_vigente cv ON cv.milestone_schedule_id = ph.hito_id
            LEFT JOIN milestone m ON m.milestone_id = cv.milestone_id
            WHERE p.project_id = @projectId
            ORDER BY cv.planned_start_date NULLS LAST, ph.rol
            """;
        var result = await conn.QueryAsync<PersonalHitoDto>(sql, new { projectId });
        return result.ToList();
    }

    public async Task GuardarAsync(int projectId, List<PersonalHitoItemInputDto> items, int userId)
    {
        using var conn = Conn();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var presupuestoId = await conn.QuerySingleOrDefaultAsync<int?>(
            """
            SELECT id FROM ss_presupuesto WHERE project_id = @projectId
            ORDER BY generado_en DESC LIMIT 1
            """, new { projectId }, tx);

        if (presupuestoId == null)
        {
            presupuestoId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO ss_presupuesto (project_id, version, estado, hh_usado, area_usada,
                    trabajadores_usados, total_estimado, generado_por, generado_en, notas)
                VALUES (@projectId, 1, 'BORRADOR', 0, 0, 0, 0, @userId, now(),
                    'Generado automáticamente para dotación de personal por hito')
                RETURNING id
                """, new { projectId, userId }, tx);
        }

        await conn.ExecuteAsync(
            "DELETE FROM ss_presupuesto_personal_hito WHERE presupuesto_id = @presupuestoId",
            new { presupuestoId }, tx);

        const decimal semanasPorMes = 4.345m;
        var filas = items.Select(i => new
        {
            presupuestoId,
            i.HitoId,
            i.Rol,
            i.Cantidad,
            i.Semanas,
            i.CostoMensual,
            Total = i.Cantidad * i.CostoMensual * (i.Semanas / semanasPorMes),
        });

        await conn.ExecuteAsync(
            """
            INSERT INTO ss_presupuesto_personal_hito
                (presupuesto_id, hito_id, rol, cantidad, semanas, costo_mensual, total)
            VALUES (@presupuestoId, @HitoId, @Rol, @Cantidad, @Semanas, @CostoMensual, @Total)
            """, filas, tx);

        await tx.CommitAsync();
    }
}
