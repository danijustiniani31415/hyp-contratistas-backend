using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Dapper;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class PresupuestoRepository : IPresupuestoRepository
{
    private readonly IConfiguration _config;
    public PresupuestoRepository(IConfiguration config) => _config = config;
    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]!);

    public async Task<List<RatioRecomendadoDto>> ObtenerRatiosRecomendadosAsync()
    {
        using var conn = Conn();
        const string sql = """
            SELECT
              f.id                                                                        AS FamiliaId,
              f.nombre                                                                    AS NombreFamilia,
              t.id                                                                        AS TipoId,
              t.nombre                                                                    AS NombreTipo,
              f.variable_base                                                             AS VariableBase,
              COALESCE(PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY r.ratio_cantidad)
                       FILTER (WHERE r.es_outlier = false AND r.incluido_manual_ratio = true), 0)  AS RatioRecomendado,
              COALESCE(AVG(r.precio_unitario_promedio)
                       FILTER (WHERE r.es_outlier = false AND r.incluido_manual_precio = true), 0) AS PrecioRecomendado,
              COUNT(r.id) FILTER (WHERE r.es_outlier = false AND r.incluido_manual_ratio = true)   AS NProyectos
            FROM ss_material_familia f
            JOIN ss_material_tipo t       ON t.id = f.tipo_id
            LEFT JOIN ss_ratio_proyecto r ON r.familia_id = f.id
            WHERE f.pertenece_ssoma = true AND f.activo = true
            GROUP BY f.id, f.nombre, t.id, t.nombre, f.variable_base
            ORDER BY t.nombre, f.nombre
            """;
        var result = await conn.QueryAsync<RatioRecomendadoDto>(sql);
        return result.ToList();
    }

    public async Task<int> SiguienteVersionAsync(int projectId)
    {
        using var conn = Conn();
        var max = await conn.ExecuteScalarAsync<int?>(
            "SELECT MAX(version) FROM ss_presupuesto WHERE project_id = @projectId",
            new { projectId });
        return (max ?? 0) + 1;
    }

    public async Task<int> CrearPresupuestoAsync(int projectId, int version, decimal hh, decimal area,
        int trabajadores, decimal total, int? generadoPor, string? notas)
    {
        using var conn = Conn();
        const string sql = """
            INSERT INTO ss_presupuesto
              (project_id, version, estado, hh_usado, area_usada, trabajadores_usados,
               total_estimado, generado_por, notas)
            VALUES
              (@projectId, @version, 'BORRADOR', @hh, @area, @trabajadores,
               @total, @generadoPor, @notas)
            RETURNING id
            """;
        return await conn.ExecuteScalarAsync<int>(sql,
            new { projectId, version, hh, area, trabajadores, total, generadoPor, notas });
    }

    public async Task InsertarLineasAsync(int presupuestoId, IEnumerable<PresupuestoLineaDto> lineas)
    {
        using var conn = Conn();
        const string sql = """
            INSERT INTO ss_presupuesto_detalle
              (presupuesto_id, familia_id, tipo_id, variable_base, ratio_recomendado,
               n_proyectos_base, valor_driver, cantidad_estimada, precio_unitario,
               total_estimado, tiene_historia)
            VALUES
              (@PresupuestoId, @FamiliaId, @TipoId, @VariableBase, @RatioRecomendado,
               @NProyectosBase, @ValorDriver, @CantidadEstimada, @PrecioUnitario,
               @TotalEstimado, @TieneHistoria)
            """;
        var rows = lineas.Select(l => new
        {
            PresupuestoId    = presupuestoId,
            l.FamiliaId,
            l.TipoId,
            l.VariableBase,
            l.RatioRecomendado,
            l.NProyectosBase,
            l.ValorDriver,
            l.CantidadEstimada,
            l.PrecioUnitario,
            l.TotalEstimado,
            l.TieneHistoria
        });
        await conn.ExecuteAsync(sql, rows);
    }

    public async Task ActualizarTotalAsync(int presupuestoId, decimal total)
    {
        using var conn = Conn();
        await conn.ExecuteAsync(
            "UPDATE ss_presupuesto SET total_estimado = @total WHERE id = @presupuestoId",
            new { presupuestoId, total });
    }

    public async Task<PresupuestoDetalleDto?> ObtenerDetalleAsync(int presupuestoId)
    {
        using var conn = Conn();

        // Header
        const string sqlHeader = """
            SELECT
              p.id, p.project_id AS ProjectId, pr.project_description AS ProjectDescription,
              p.version, p.estado AS Estado, p.hh_usado AS HhUsado,
              p.area_usada AS AreaUsada, p.trabajadores_usados AS TrabajadoresUsados,
              p.total_estimado AS TotalEstimado, p.generado_en AS GeneradoEn, p.notas
            FROM ss_presupuesto p
            JOIN project pr ON pr.project_id = p.project_id
            WHERE p.id = @presupuestoId
            """;
        var header = await conn.QueryFirstOrDefaultAsync<PresupuestoDetalleDto>(sql: sqlHeader, param: new { presupuestoId });
        if (header is null) return null;

        // Líneas
        const string sqlLineas = """
            SELECT
              l.id AS LineaId, l.familia_id AS FamiliaId,
              f.nombre AS NombreFamilia, t.nombre AS NombreTipo, t.id AS TipoId,
              l.variable_base AS VariableBase, l.ratio_recomendado AS RatioRecomendado,
              l.n_proyectos_base AS NProyectosBase, l.valor_driver AS ValorDriver,
              l.cantidad_estimada AS CantidadEstimada, l.precio_unitario AS PrecioUnitario,
              l.total_estimado AS TotalEstimado, l.tiene_historia AS TieneHistoria,
              l.cantidad_manual AS CantidadManual, l.precio_manual AS PrecioManual,
              l.notas_linea AS NotasLinea
            FROM ss_presupuesto_detalle l
            JOIN ss_material_familia f ON f.id = l.familia_id
            JOIN ss_material_tipo t    ON t.id = l.tipo_id
            WHERE l.presupuesto_id = @presupuestoId
            ORDER BY t.nombre, f.nombre
            """;
        var lineas = (await conn.QueryAsync<PresupuestoLineaDto>(sql: sqlLineas, param: new { presupuestoId })).ToList();

        header.TotalFamilias       = lineas.Count;
        header.FamiliasSinHistoria = lineas.Count(l => !l.TieneHistoria);
        header.Tipos = lineas
            .GroupBy(l => new { l.TipoId, l.NombreTipo })
            .OrderBy(g => g.Key.NombreTipo)
            .Select(g => new PresupuestoTipoDto
            {
                TipoId        = g.Key.TipoId,
                NombreTipo    = g.Key.NombreTipo,
                TotalEstimado = g.Sum(l => l.TotalEfectivo),
                Familias      = g.OrderBy(l => l.NombreFamilia).ToList()
            }).ToList();

        return header;
    }

    public async Task<List<PresupuestoResumenDto>> ObtenerPorProyectoAsync(int projectId)
    {
        using var conn = Conn();
        const string sql = """
            SELECT
              p.id, p.project_id AS ProjectId, pr.project_description AS ProjectDescription,
              p.version, p.estado AS Estado, p.hh_usado AS HhUsado,
              p.area_usada AS AreaUsada, p.trabajadores_usados AS TrabajadoresUsados,
              p.total_estimado AS TotalEstimado, p.generado_en AS GeneradoEn,
              (SELECT COUNT(*) FROM ss_presupuesto_detalle WHERE presupuesto_id = p.id)       AS TotalFamilias,
              (SELECT COUNT(*) FROM ss_presupuesto_detalle WHERE presupuesto_id = p.id AND tiene_historia = false) AS FamiliasSinHistoria
            FROM ss_presupuesto p
            JOIN project pr ON pr.project_id = p.project_id
            WHERE p.project_id = @projectId
            ORDER BY p.version DESC
            """;
        var result = await conn.QueryAsync<PresupuestoResumenDto>(sql, new { projectId });
        return result.ToList();
    }

    public async Task ActualizarLineaAsync(int lineaId, decimal? cantidadManual, decimal? precioManual, string? notas)
    {
        using var conn = Conn();
        await conn.ExecuteAsync("""
            UPDATE ss_presupuesto_detalle
            SET cantidad_manual = @cantidadManual,
                precio_manual   = @precioManual,
                notas_linea     = @notas
            WHERE id = @lineaId
            """, new { lineaId, cantidadManual, precioManual, notas });

        // Recalcular total del presupuesto padre
        await conn.ExecuteAsync("""
            UPDATE ss_presupuesto p
            SET total_estimado = (
              SELECT SUM(
                COALESCE(l.cantidad_manual, l.cantidad_estimada) *
                COALESCE(l.precio_manual,   l.precio_unitario)
              )
              FROM ss_presupuesto_detalle l
              WHERE l.presupuesto_id = p.id
            )
            WHERE p.id = (SELECT presupuesto_id FROM ss_presupuesto_detalle WHERE id = @lineaId)
            """, new { lineaId });
    }

    public async Task<string> AprobarAsync(int presupuestoId)
    {
        using var conn = Conn();
        var filas = await conn.ExecuteAsync("""
            UPDATE ss_presupuesto
            SET estado = 'APROBADO', aprobado_en = NOW()
            WHERE id = @presupuestoId AND estado = 'BORRADOR'
            """, new { presupuestoId });

        if (filas == 0)
            throw new Abril_Backend.Application.Exceptions.AbrilException(
                "No se puede aprobar: el presupuesto no existe o ya está aprobado.", 400);

        return "APROBADO";
    }
}
