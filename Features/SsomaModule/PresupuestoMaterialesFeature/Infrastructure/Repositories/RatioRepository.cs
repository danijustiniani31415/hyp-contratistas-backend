using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Dapper;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class RatioRepository : IRatioRepository
{
    private readonly IConfiguration _config;
    public RatioRepository(IConfiguration config) => _config = config;
    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]!);

    private record ProyectoConConsumoRow(int ProjectId, string ProjectDescription);

    public async Task<List<(int ProjectId, string ProjectDescription)>> ObtenerProyectosConConsumoEstandarizadoAsync()
    {
        using var conn = Conn();
        const string sql = """
            SELECT DISTINCT p.project_id AS ProjectId, p.project_description AS ProjectDescription
            FROM ss_consumo_linea l
            JOIN project p ON p.project_id = l.project_id
            WHERE l.activo = true AND l.estandarizado = true AND l.pertenece_ssoma = true
            ORDER BY p.project_description
            """;
        var filas = await conn.QueryAsync<ProyectoConConsumoRow>(sql);
        return filas.Select(f => (f.ProjectId, f.ProjectDescription)).ToList();
    }

    public async Task<List<RatioRawData>> ObtenerConsumosPorProyectoAsync(int projectId)
    {
        using var conn = Conn();
        const string sql = """
            SELECT
                f.id                                        AS FamiliaId,
                f.nombre                                    AS NombreFamilia,
                t.nombre                                    AS TipoMaterial,
                f.variable_base                             AS VariableBase,
                SUM(COALESCE(l.cantidad_real, l.cantidad))  AS CantidadTotal,
                CASE WHEN SUM(COALESCE(l.cantidad_real, l.cantidad)) > 0
                     THEN SUM(l.precio_total) / SUM(COALESCE(l.cantidad_real, l.cantidad))
                     ELSE 0 END                             AS PrecioUnitarioPromedio,
                SUM(l.precio_total)                         AS PrecioTotal
            FROM ss_consumo_linea l
            JOIN ss_material_item i   ON i.id = l.item_id
            JOIN ss_material_familia f ON f.id = i.familia_id
            JOIN ss_material_tipo t   ON t.id = f.tipo_id
            WHERE l.project_id = @projectId
              AND l.activo = true
              AND l.estandarizado = true
              AND l.pertenece_ssoma = true
              AND (l.estado_revision IS NULL OR l.estado_revision = 'AUTORIZADO')
            GROUP BY f.id, f.nombre, t.nombre, f.variable_base
            ORDER BY SUM(l.precio_total) DESC
            """;
        var result = await conn.QueryAsync<RatioRawData>(sql, new { projectId });
        return result.ToList();
    }


    public async Task UpsertRatiosBulkAsync(List<RatioUpsertItem> items)
    {
        if (items.Count == 0) return;
        using var conn = Conn();
        const string sql = """
            INSERT INTO ss_ratio_proyecto
              (familia_id, project_id, variable_base, cantidad_total, precio_unitario_promedio, valor_driver, ratio_cantidad, es_outlier)
            VALUES
              (@FamiliaId, @ProjectId, @VariableBase, @CantidadTotal, @PrecioUnitarioPromedio, @ValorDriver, @RatioCantidad, false)
            ON CONFLICT (familia_id, project_id)
            DO UPDATE SET
              variable_base            = EXCLUDED.variable_base,
              cantidad_total           = EXCLUDED.cantidad_total,
              precio_unitario_promedio = EXCLUDED.precio_unitario_promedio,
              valor_driver             = EXCLUDED.valor_driver,
              ratio_cantidad           = EXCLUDED.ratio_cantidad,
              es_outlier               = false
            """;
        // Misma conexion para todas las filas (antes se abria una conexion nueva por familia).
        await conn.ExecuteAsync(sql, items);
    }

    public async Task<List<RatioProyectoDto>> ObtenerRatiosPorProyectoAsync(int projectId)
    {
        using var conn = Conn();
        const string sql = """
            SELECT r.id, r.familia_id AS FamiliaId, f.nombre AS NombreFamilia, t.nombre AS TipoMaterial,
                   r.project_id AS ProjectId, p.project_description AS ProjectDescription,
                   r.variable_base AS VariableBase, r.cantidad_total AS CantidadTotal,
                   r.precio_unitario_promedio AS PrecioUnitarioPromedio,
                   r.valor_driver AS ValorDriver, r.ratio_cantidad AS RatioCantidad,
                   r.es_outlier AS EsOutlier, r.incluido_manual_ratio AS IncluidoManualRatio,
                   r.incluido_manual_precio AS IncluidoManualPrecio
            FROM ss_ratio_proyecto r
            JOIN ss_material_familia f ON f.id = r.familia_id
            JOIN ss_material_tipo t    ON t.id = f.tipo_id
            JOIN project p             ON p.project_id = r.project_id
            WHERE r.project_id = @projectId
            ORDER BY r.ratio_cantidad DESC
            """;
        var result = await conn.QueryAsync<RatioProyectoDto>(sql, new { projectId });
        return result.ToList();
    }

    public async Task<List<RatioProyectoDto>> ObtenerRatiosPorFamiliaAsync(int familiaId)
    {
        using var conn = Conn();
        const string sql = """
            SELECT r.id, r.familia_id AS FamiliaId, f.nombre AS NombreFamilia, t.nombre AS TipoMaterial,
                   r.project_id AS ProjectId, p.project_description AS ProjectDescription,
                   r.variable_base AS VariableBase, r.cantidad_total AS CantidadTotal,
                   r.precio_unitario_promedio AS PrecioUnitarioPromedio,
                   r.valor_driver AS ValorDriver, r.ratio_cantidad AS RatioCantidad,
                   r.es_outlier AS EsOutlier, r.incluido_manual_ratio AS IncluidoManualRatio,
                   r.incluido_manual_precio AS IncluidoManualPrecio
            FROM ss_ratio_proyecto r
            JOIN ss_material_familia f ON f.id = r.familia_id
            JOIN ss_material_tipo t    ON t.id = f.tipo_id
            JOIN project p             ON p.project_id = r.project_id
            WHERE r.familia_id = @familiaId
            ORDER BY r.ratio_cantidad
            """;
        var result = await conn.QueryAsync<RatioProyectoDto>(sql, new { familiaId });
        return result.ToList();
    }

    public async Task ActualizarIncluidoManualAsync(int familiaId, int projectId, bool incluir, string campo)
    {
        var columna = campo.Equals("PRECIO", StringComparison.OrdinalIgnoreCase)
            ? "incluido_manual_precio"
            : "incluido_manual_ratio";
        using var conn = Conn();
        var sql = $"""
            UPDATE ss_ratio_proyecto
            SET {columna} = @incluir
            WHERE familia_id = @familiaId AND project_id = @projectId
            """;
        await conn.ExecuteAsync(sql, new { familiaId, projectId, incluir });
    }

    public async Task<List<FamiliaConRatioDto>> ListarFamiliasConRatioAsync()
    {
        using var conn = Conn();
        const string sql = """
            SELECT f.id AS FamiliaId, f.nombre AS NombreFamilia, t.nombre AS TipoMaterial,
                   f.variable_base AS VariableBase,
                   COUNT(r.id) AS NProyectos,
                   COUNT(r.id) FILTER (WHERE r.es_outlier) AS NOutliers
            FROM ss_material_familia f
            JOIN ss_material_tipo t ON t.id = f.tipo_id
            JOIN ss_ratio_proyecto r ON r.familia_id = f.id
            WHERE f.pertenece_ssoma = true AND f.activo = true
            GROUP BY f.id, f.nombre, t.nombre, f.variable_base
            ORDER BY t.nombre, f.nombre
            """;
        var result = await conn.QueryAsync<FamiliaConRatioDto>(sql);
        return result.ToList();
    }

    public async Task<List<ResumenProyectoRatioDto>> ObtenerResumenAsync()
    {
        using var conn = Conn();
        const string sql = """
            SELECT r.project_id AS ProjectId, p.project_description AS ProjectDescription,
                   COUNT(DISTINCT r.familia_id) AS FamiliasCalculadas,
                   SUM(r.cantidad_total * r.precio_unitario_promedio) AS TotalGastoSsoma,
                   c.fecha_min AS FechaMin, c.fecha_max AS FechaMax
            FROM ss_ratio_proyecto r
            JOIN project p ON p.project_id = r.project_id
            LEFT JOIN (
                SELECT project_id, MIN(fecha_min) AS fecha_min, MAX(fecha_max) AS fecha_max
                FROM ss_consumo_carga WHERE estado = 'ACTIVA' GROUP BY project_id
            ) c ON c.project_id = r.project_id
            GROUP BY r.project_id, p.project_description, c.fecha_min, c.fecha_max
            ORDER BY TotalGastoSsoma DESC
            """;
        var result = await conn.QueryAsync<ResumenProyectoRatioDto>(sql);
        return result.ToList();
    }
}
