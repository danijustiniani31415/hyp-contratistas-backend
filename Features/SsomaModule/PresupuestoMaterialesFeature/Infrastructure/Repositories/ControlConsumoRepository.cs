using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Dapper;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class ControlConsumoRepository : IControlConsumoRepository
{
    private readonly IConfiguration _config;
    public ControlConsumoRepository(IConfiguration config) => _config = config;
    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]!);

    public async Task<int> SiguienteSemanaNumAsync(int presupuestoId)
    {
        using var conn = Conn();
        var max = await conn.ExecuteScalarAsync<int?>(
            "SELECT MAX(semana_num) FROM ss_control_semana WHERE presupuesto_id = @presupuestoId",
            new { presupuestoId });
        return (max ?? 0) + 1;
    }

    public async Task<int> CrearSemanaAsync(int presupuestoId, int projectId, int semanaNum,
        DateOnly fechaInicio, DateOnly fechaFin, string? obs, int? userId)
    {
        using var conn = Conn();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO ss_control_semana
              (presupuesto_id, project_id, semana_num, fecha_inicio, fecha_fin, observaciones, registrado_por)
            VALUES
              (@presupuestoId, @projectId, @semanaNum, @fechaInicio, @fechaFin, @obs, @userId)
            RETURNING id
            """,
            new { presupuestoId, projectId, semanaNum,
                  fechaInicio = fechaInicio.ToDateTime(TimeOnly.MinValue),
                  fechaFin    = fechaFin.ToDateTime(TimeOnly.MinValue),
                  obs, userId });
    }

    public async Task UpsertLineasAsync(int controlId, IEnumerable<RegistrarConsumoLineaDto> lineas)
    {
        using var conn = Conn();
        // Eliminar las líneas existentes del control y reinsertarlas (simplicity > partial upsert)
        await conn.ExecuteAsync(
            "DELETE FROM ss_control_semana_linea WHERE control_id = @controlId",
            new { controlId });

        var rows = lineas.Where(l => l.CantidadReal > 0).Select(l => new
        {
            controlId,
            l.FamiliaId,
            l.CantidadReal,
            l.PrecioUnitario,
            l.Notas
        });

        if (!rows.Any()) return;

        await conn.ExecuteAsync("""
            INSERT INTO ss_control_semana_linea
              (control_id, familia_id, cantidad_real, precio_unitario, notas)
            VALUES
              (@controlId, @FamiliaId, @CantidadReal, @PrecioUnitario, @Notas)
            """, rows);
    }

    public async Task CerrarSemanaAsync(int controlId)
    {
        using var conn = Conn();
        await conn.ExecuteAsync("""
            UPDATE ss_control_semana
            SET estado = 'CERRADO', cerrado_en = NOW()
            WHERE id = @controlId AND estado = 'ABIERTO'
            """, new { controlId });
    }

    public async Task<ControlSemanaDto?> ObtenerSemanaAsync(int controlId)
    {
        using var conn = Conn();
        var semana = await conn.QueryFirstOrDefaultAsync<ControlSemanaDto>("""
            SELECT cs.id, cs.presupuesto_id AS PresupuestoId, cs.project_id AS ProjectId,
                   p.project_description AS ProjectDescription,
                   cs.semana_num AS SemanaNum, cs.fecha_inicio AS FechaInicio,
                   cs.fecha_fin AS FechaFin, cs.estado AS Estado,
                   cs.observaciones AS Observaciones, cs.registrado_en AS RegistradoEn
            FROM ss_control_semana cs
            JOIN project p ON p.project_id = cs.project_id
            WHERE cs.id = @controlId
            """, new { controlId });

        if (semana is null) return null;

        semana.Lineas = (await conn.QueryAsync<ControlSemanaLineaDto>("""
            SELECT l.id, l.familia_id AS FamiliaId, f.nombre AS NombreFamilia,
                   t.nombre AS NombreTipo, l.cantidad_real AS CantidadReal,
                   l.precio_unitario AS PrecioUnitario, l.total_real AS TotalReal, l.notas AS Notas
            FROM ss_control_semana_linea l
            JOIN ss_material_familia f ON f.id = l.familia_id
            JOIN ss_material_tipo t    ON t.id = f.tipo_id
            WHERE l.control_id = @controlId
            ORDER BY t.nombre, f.nombre
            """, new { controlId })).ToList();

        return semana;
    }

    public async Task<List<ControlSemanaDto>> ListarSemanasPorPresupuestoAsync(int presupuestoId)
    {
        using var conn = Conn();
        var semanas = (await conn.QueryAsync<ControlSemanaDto>("""
            SELECT cs.id, cs.presupuesto_id AS PresupuestoId, cs.project_id AS ProjectId,
                   p.project_description AS ProjectDescription,
                   cs.semana_num AS SemanaNum, cs.fecha_inicio AS FechaInicio,
                   cs.fecha_fin AS FechaFin, cs.estado AS Estado,
                   cs.observaciones AS Observaciones, cs.registrado_en AS RegistradoEn
            FROM ss_control_semana cs
            JOIN project p ON p.project_id = cs.project_id
            WHERE cs.presupuesto_id = @presupuestoId
            ORDER BY cs.semana_num DESC
            """, new { presupuestoId })).ToList();
        return semanas;
    }

    public async Task<DashboardPresupuestoDto?> ObtenerDashboardAsync(int presupuestoId)
    {
        using var conn = Conn();

        // Header del presupuesto
        var header = await conn.QueryFirstOrDefaultAsync<DashboardPresupuestoDto>("""
            SELECT p.id AS PresupuestoId, p.project_id AS ProjectId,
                   pr.project_description AS ProjectDescription, p.version,
                   p.total_estimado AS TotalPresupuestado,
                   (SELECT COUNT(*) FROM ss_control_semana WHERE presupuesto_id = p.id) AS SemanasRegistradas
            FROM ss_presupuesto p
            JOIN project pr ON pr.project_id = p.project_id
            WHERE p.id = @presupuestoId
            """, new { presupuestoId });

        if (header is null) return null;

        // Líneas con consumo acumulado
        var lineas = (await conn.QueryAsync<DashboardLineaDto>("""
            SELECT
              pl.familia_id                                                    AS FamiliaId,
              f.nombre                                                         AS NombreFamilia,
              t.id                                                             AS TipoId,
              pl.variable_base                                                 AS VariableBase,
              COALESCE(pl.cantidad_manual, pl.cantidad_estimada)               AS CantidadPresupuestada,
              COALESCE(SUM(cl.cantidad_real), 0)                               AS CantidadConsumida,
              COALESCE(pl.precio_manual, pl.precio_unitario)                   AS PrecioUnitario,
              COALESCE(pl.cantidad_manual, pl.cantidad_estimada)
                * COALESCE(pl.precio_manual, pl.precio_unitario)               AS TotalPresupuestado,
              COALESCE(SUM(cl.total_real), 0)                                  AS TotalConsumido,
              CASE
                WHEN COALESCE(pl.cantidad_manual, pl.cantidad_estimada) = 0        THEN 'SIN_PRESUPUESTO'
                WHEN COALESCE(SUM(cl.cantidad_real), 0)
                       >= COALESCE(pl.cantidad_manual, pl.cantidad_estimada)       THEN 'ALERTA'
                WHEN COALESCE(SUM(cl.cantidad_real), 0)
                       >= COALESCE(pl.cantidad_manual, pl.cantidad_estimada) * 0.8 THEN 'ADVERTENCIA'
                ELSE 'OK'
              END                                                              AS Semaforo
            FROM ss_presupuesto_detalle pl
            JOIN ss_material_familia f ON f.id = pl.familia_id
            JOIN ss_material_tipo t    ON t.id = pl.tipo_id
            LEFT JOIN ss_control_semana   cs ON cs.presupuesto_id = pl.presupuesto_id
            LEFT JOIN ss_control_semana_linea cl
                   ON cl.control_id = cs.id AND cl.familia_id = pl.familia_id
            WHERE pl.presupuesto_id = @presupuestoId
            GROUP BY pl.familia_id, f.nombre, t.id, pl.variable_base,
                     pl.cantidad_manual, pl.cantidad_estimada,
                     pl.precio_manual, pl.precio_unitario
            ORDER BY
              CASE WHEN COALESCE(SUM(cl.cantidad_real),0) >= COALESCE(pl.cantidad_manual,pl.cantidad_estimada)       THEN 1
                   WHEN COALESCE(SUM(cl.cantidad_real),0) >= COALESCE(pl.cantidad_manual,pl.cantidad_estimada)*0.8   THEN 2
                   ELSE 3 END,
              t.nombre, f.nombre
            """, new { presupuestoId })).ToList();

        header.TotalConsumido        = lineas.Sum(l => l.TotalConsumido);
        header.FamiliasEnAlerta      = lineas.Count(l => l.Semaforo == "ALERTA");
        header.FamiliasEnAdvertencia = lineas.Count(l => l.Semaforo == "ADVERTENCIA");

        // Necesitamos NombreTipo para agrupar — segunda query ligera
        var tipos = (await conn.QueryAsync<(int Id, string Nombre)>(
            "SELECT id, nombre FROM ss_material_tipo ORDER BY nombre")).ToList();
        var tipoMap = tipos.ToDictionary(t => t.Id, t => t.Nombre);

        header.Tipos = lineas
            .GroupBy(l => l.TipoId)
            .Select(g => new DashboardTipoDto
            {
                TipoId             = g.Key,
                NombreTipo         = tipoMap.GetValueOrDefault(g.Key, g.Key.ToString()),
                TotalPresupuestado = g.Sum(l => l.TotalPresupuestado),
                TotalConsumido     = g.Sum(l => l.TotalConsumido),
                Familias           = g.ToList()
            })
            .OrderByDescending(t => t.PctConsumido)
            .ToList();

        return header;
    }
}
