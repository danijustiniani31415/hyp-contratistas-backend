using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class EstandarizacionRepository : IEstandarizacionRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IConfiguration _config;

    public EstandarizacionRepository(IDbContextFactory<AppDbContext> factory, IConfiguration config)
    {
        _factory = factory;
        _config = config;
    }

    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]!);

    public async Task<MatchResult?> BuscarPorAliasExactoAsync(string textoCrudoNorm)
    {
        using var conn = Conn();
        const string sql = """
            SELECT i.id AS ItemId, i.nombre AS NombreItem,
                   f.id AS FamiliaId, f.nombre AS NombreFamilia,
                   f.pertenece_ssoma AS PerteneceSsoma,
                   1.0 AS Score, 'ALIAS' AS Metodo,
                   a.factor_conversion AS FactorConversion
            FROM ss_material_alias a
            JOIN ss_material_item i ON i.id = a.item_id
            JOIN ss_material_familia f ON f.id = i.familia_id
            WHERE a.texto_crudo_norm = @norm AND i.no_usar = false AND i.activo = true
            LIMIT 1
            """;
        return await conn.QueryFirstOrDefaultAsync<MatchResult>(sql, new { norm = textoCrudoNorm });
    }

    public async Task<MatchResult?> BuscarPorNombreExactoAsync(string nombreNorm)
    {
        using var conn = Conn();
        const string sql = """
            SELECT i.id AS ItemId, i.nombre AS NombreItem,
                   f.id AS FamiliaId, f.nombre AS NombreFamilia,
                   f.pertenece_ssoma AS PerteneceSsoma,
                   1.0 AS Score, 'EXACTO' AS Metodo
            FROM ss_material_item i
            JOIN ss_material_familia f ON f.id = i.familia_id
            WHERE i.nombre_normalizado = @norm AND i.no_usar = false AND i.activo = true
            LIMIT 1
            """;
        return await conn.QueryFirstOrDefaultAsync<MatchResult>(sql, new { norm = nombreNorm });
    }

    public async Task<List<MatchResult>> BuscarPorTrigramAsync(string textoCrudoNorm, decimal umbralMinimo, int topN = 5)
    {
        using var conn = Conn();
        const string sql = """
            SELECT i.id AS ItemId, i.nombre AS NombreItem,
                   f.id AS FamiliaId, f.nombre AS NombreFamilia,
                   f.pertenece_ssoma AS PerteneceSsoma,
                   similarity(i.nombre_normalizado, @texto) AS Score,
                   'FUZZY' AS Metodo
            FROM ss_material_item i
            JOIN ss_material_familia f ON f.id = i.familia_id
            WHERE i.no_usar = false AND i.activo = true
              AND similarity(i.nombre_normalizado, @texto) >= @umbral
            ORDER BY similarity(i.nombre_normalizado, @texto) DESC
            LIMIT @topN
            """;
        var resultados = await conn.QueryAsync<MatchResult>(sql,
            new { texto = textoCrudoNorm, umbral = (double)umbralMinimo, topN });
        return resultados.ToList();
    }

    public async Task CrearAliasAsync(string textoCrudo, string textoCrudoNorm, int itemId, string origen, decimal confianza)
    {
        using var conn = Conn();
        const string sql = """
            INSERT INTO ss_material_alias (texto_crudo, texto_crudo_norm, item_id, origen, confianza, creado_en)
            VALUES (@textoCrudo, @textoCrudoNorm, @itemId, @origen, @confianza, now())
            ON CONFLICT (texto_crudo_norm) DO NOTHING
            """;
        await conn.ExecuteAsync(sql, new { textoCrudo, textoCrudoNorm, itemId, origen, confianza = (double)confianza });
    }

    public async Task<bool> EsRechazoConocidoAsync(string textoCrudoNorm)
    {
        using var conn = Conn();
        const string sql = "SELECT 1 FROM ss_material_alias WHERE texto_crudo_norm = @norm AND item_id IS NULL LIMIT 1";
        var fila = await conn.QueryFirstOrDefaultAsync<int?>(sql, new { norm = textoCrudoNorm });
        return fila.HasValue;
    }

    public async Task CrearAliasRechazoAsync(string textoCrudo, string textoCrudoNorm)
    {
        using var conn = Conn();
        const string sql = """
            INSERT INTO ss_material_alias (texto_crudo, texto_crudo_norm, item_id, origen, confianza, creado_en)
            VALUES (@textoCrudo, @textoCrudoNorm, NULL, 'RECHAZO_CONFIRMADO', 1.0, now())
            ON CONFLICT (texto_crudo_norm) DO NOTHING
            """;
        await conn.ExecuteAsync(sql, new { textoCrudo, textoCrudoNorm });
    }

    public async Task<List<MatchResult>> BuscarItemsSimilaresAsync(string textoNorm)
    {
        using var conn = Conn();
        const string sql = """
            SELECT i.id AS ItemId, i.nombre AS NombreItem,
                   f.id AS FamiliaId, f.nombre AS NombreFamilia,
                   f.pertenece_ssoma AS PerteneceSsoma,
                   t.nombre AS TipoMaterial,
                   similarity(i.nombre_normalizado, @texto) AS Score,
                   'BUSQUEDA_MANUAL' AS Metodo
            FROM ss_material_item i
            JOIN ss_material_familia f ON f.id = i.familia_id
            JOIN ss_material_tipo t ON t.id = f.tipo_id
            WHERE i.no_usar = false AND i.activo = true
              AND (i.nombre_normalizado ILIKE '%' || @texto || '%' OR similarity(i.nombre_normalizado, @texto) > 0.2)
            ORDER BY similarity(i.nombre_normalizado, @texto) DESC, i.nombre
            LIMIT 20
            """;
        var resultados = await conn.QueryAsync<MatchResult>(sql, new { texto = textoNorm });
        return resultados.ToList();
    }

    public async Task<bool> ObtenerPerteneceSsomaDeItemAsync(int itemId)
    {
        using var conn = Conn();
        const string sql = """
            SELECT f.pertenece_ssoma
            FROM ss_material_item i
            JOIN ss_material_familia f ON f.id = i.familia_id
            WHERE i.id = @itemId
            """;
        return await conn.QueryFirstOrDefaultAsync<bool>(sql, new { itemId });
    }

    public async Task<HashSet<string>> ObtenerRechazosConocidosAsync()
    {
        using var conn = Conn();
        var textos = await conn.QueryAsync<string>(
            "SELECT texto_crudo_norm FROM ss_material_alias WHERE item_id IS NULL");
        return textos.ToHashSet();
    }

    private record AliasRow(string Clave, int ItemId, string NombreItem, int FamiliaId,
        string NombreFamilia, bool PerteneceSsoma, decimal FactorConversion);

    public async Task<Dictionary<string, MatchResult>> ObtenerAliasesActivosAsync()
    {
        using var conn = Conn();
        const string sql = """
            SELECT a.texto_crudo_norm AS Clave, i.id AS ItemId, i.nombre AS NombreItem,
                   f.id AS FamiliaId, f.nombre AS NombreFamilia, f.pertenece_ssoma AS PerteneceSsoma,
                   a.factor_conversion AS FactorConversion
            FROM ss_material_alias a
            JOIN ss_material_item i ON i.id = a.item_id
            JOIN ss_material_familia f ON f.id = i.familia_id
            WHERE i.no_usar = false AND i.activo = true
            """;
        var filas = await conn.QueryAsync<AliasRow>(sql);
        var mapa = new Dictionary<string, MatchResult>();
        foreach (var f in filas)
        {
            mapa[f.Clave] = new MatchResult
            {
                ItemId = f.ItemId, NombreItem = f.NombreItem, FamiliaId = f.FamiliaId,
                NombreFamilia = f.NombreFamilia, PerteneceSsoma = f.PerteneceSsoma,
                Score = 1.0m, Metodo = "ALIAS", FactorConversion = f.FactorConversion
            };
        }
        return mapa;
    }

    private record NombreRow(string Clave, int ItemId, string NombreItem, int FamiliaId,
        string NombreFamilia, bool PerteneceSsoma);

    public async Task<Dictionary<string, MatchResult>> ObtenerNombresItemActivosAsync()
    {
        using var conn = Conn();
        const string sql = """
            SELECT i.nombre_normalizado AS Clave, i.id AS ItemId, i.nombre AS NombreItem,
                   f.id AS FamiliaId, f.nombre AS NombreFamilia, f.pertenece_ssoma AS PerteneceSsoma
            FROM ss_material_item i
            JOIN ss_material_familia f ON f.id = i.familia_id
            WHERE i.no_usar = false AND i.activo = true
            """;
        var filas = await conn.QueryAsync<NombreRow>(sql);
        var mapa = new Dictionary<string, MatchResult>();
        foreach (var f in filas)
        {
            mapa[f.Clave] = new MatchResult
            {
                ItemId = f.ItemId, NombreItem = f.NombreItem, FamiliaId = f.FamiliaId,
                NombreFamilia = f.NombreFamilia, PerteneceSsoma = f.PerteneceSsoma,
                Score = 1.0m, Metodo = "EXACTO"
            };
        }
        return mapa;
    }
}
