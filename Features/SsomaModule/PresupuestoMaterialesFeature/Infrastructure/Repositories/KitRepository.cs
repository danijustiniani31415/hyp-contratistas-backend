using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Dapper;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class KitRepository : IKitRepository
{
    private readonly IConfiguration _config;
    public KitRepository(IConfiguration config) => _config = config;
    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]!);

    public async Task<List<KitResumenDto>> ListarAsync(int? tipoId)
    {
        using var conn = Conn();
        const string sql = """
            SELECT k.id AS Id, k.nombre AS Nombre, k.tipo_id AS TipoId, t.nombre AS NombreTipo
            FROM ss_kit k
            JOIN ss_material_tipo t ON t.id = k.tipo_id
            WHERE k.activo = true AND (@tipoId IS NULL OR k.tipo_id = @tipoId)
            ORDER BY k.nombre
            """;
        var result = await conn.QueryAsync<KitResumenDto>(sql, new { tipoId });
        return result.ToList();
    }

    public async Task<KitDetalleDto?> ObtenerAsync(int kitId)
    {
        using var conn = Conn();
        var kit = await conn.QuerySingleOrDefaultAsync<KitResumenDto>(
            """
            SELECT k.id AS Id, k.nombre AS Nombre, k.tipo_id AS TipoId, t.nombre AS NombreTipo
            FROM ss_kit k JOIN ss_material_tipo t ON t.id = k.tipo_id
            WHERE k.id = @kitId
            """, new { kitId });
        if (kit == null) return null;

        var items = await conn.QueryAsync<KitItemDto>(
            """
            SELECT ki.id AS Id, ki.familia_id AS FamiliaId, f.nombre AS NombreFamilia,
                   ki.cantidad_por_kit AS CantidadPorKit, ki.es_consumible AS EsConsumible
            FROM ss_kit_item ki
            JOIN ss_material_familia f ON f.id = ki.familia_id
            WHERE ki.kit_id = @kitId
            ORDER BY f.nombre
            """, new { kitId });

        return new KitDetalleDto
        {
            Id = kit.Id,
            Nombre = kit.Nombre,
            TipoId = kit.TipoId,
            NombreTipo = kit.NombreTipo,
            Items = items.ToList(),
        };
    }

    public async Task<int> CrearAsync(KitCreateDto dto)
    {
        using var conn = Conn();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var kitId = await conn.QuerySingleAsync<int>(
            """
            INSERT INTO ss_kit (nombre, tipo_id, activo, creado_en)
            VALUES (@Nombre, @TipoId, true, now())
            RETURNING id
            """, new { dto.Nombre, dto.TipoId }, tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO ss_kit_item (kit_id, familia_id, cantidad_por_kit, es_consumible)
            VALUES (@kitId, @FamiliaId, @CantidadPorKit, @EsConsumible)
            """,
            dto.Items.Select(i => new { kitId, i.FamiliaId, i.CantidadPorKit, i.EsConsumible }),
            tx);

        await tx.CommitAsync();
        return kitId;
    }

    public async Task<List<KitCalculoLineaDto>> CalcularAsync(int kitId, decimal cantidadKits)
    {
        using var conn = Conn();
        const string sql = """
            SELECT ki.familia_id AS FamiliaId, f.nombre AS NombreFamilia,
                   ki.cantidad_por_kit AS CantidadPorKit,
                   ki.cantidad_por_kit * @cantidadKits AS CantidadTotal,
                   ki.es_consumible AS EsConsumible
            FROM ss_kit_item ki
            JOIN ss_material_familia f ON f.id = ki.familia_id
            WHERE ki.kit_id = @kitId
            ORDER BY f.nombre
            """;
        var result = await conn.QueryAsync<KitCalculoLineaDto>(sql, new { kitId, cantidadKits });
        return result.ToList();
    }
}
