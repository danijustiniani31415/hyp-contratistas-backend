using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

public class MatchResult
{
    public int ItemId { get; set; }
    public string NombreItem { get; set; } = null!;
    public int FamiliaId { get; set; }
    public string NombreFamilia { get; set; } = null!;
    public bool PerteneceSsoma { get; set; }
    public decimal Score { get; set; }
    public string Metodo { get; set; } = null!;
    /// <summary>Del alias matcheado (si aplica). Default 1 cuando el match no viene de un alias específico.</summary>
    public decimal FactorConversion { get; set; } = 1;
    /// <summary>Solo lo llena BuscarItemsSimilaresAsync (búsqueda manual del selector de revisión).</summary>
    public string? TipoMaterial { get; set; }
}

public interface IEstandarizacionRepository
{
    /// <summary>Busca en ss_material_alias por texto_crudo_norm exacto — O(1) con índice único.</summary>
    Task<MatchResult?> BuscarPorAliasExactoAsync(string textoCrudoNorm);
    /// <summary>Busca en ss_material_item por nombre_normalizado exacto.</summary>
    Task<MatchResult?> BuscarPorNombreExactoAsync(string nombreNorm);
    /// <summary>Búsqueda trigram en ss_material_item.nombre_normalizado vía pg_trgm.</summary>
    Task<List<MatchResult>> BuscarPorTrigramAsync(string textoCrudoNorm, decimal umbralMinimo, int topN = 5);
    Task CrearAliasAsync(string textoCrudo, string textoCrudoNorm, int itemId, string origen, decimal confianza);
    /// <summary>true si este texto ya se rechazó antes en Revisión ("no es SSOMA") — no hace falta volver a preguntar.</summary>
    Task<bool> EsRechazoConocidoAsync(string textoCrudoNorm);
    /// <summary>Recuerda que este texto no pertenece a SSOMA, para que futuras cargas lo auto-rechacen sin pasar por Revisión.</summary>
    Task CrearAliasRechazoAsync(string textoCrudo, string textoCrudoNorm);
    /// <summary>Búsqueda manual del selector de revisión: substring + similarity trigram (más tolerante que un Contains exacto — "BARRA EXTENSIBLE" sí encuentra "BARRA EXPANDIBLE").</summary>
    Task<List<MatchResult>> BuscarItemsSimilaresAsync(string textoNorm);
    /// <summary>PerteneceSsoma de la família de un ítem — para aplicar retroactivamente la misma decisión a otras líneas con el mismo texto crudo.</summary>
    Task<bool> ObtenerPerteneceSsomaDeItemAsync(int itemId);

    // ─── Precarga en lote para EstandarizarCargaAsync ──────────────────────────
    // Un lote grande (miles de líneas) haciendo 3-5 consultas POR LÍNEA para las etapas 0-2
    // (que son búsquedas exactas, no fuzzy) es innecesariamente lento y fragiliza el proceso
    // completo ante cualquier corte breve de conexión. Estas tres cargan TODO el catálogo/alias
    // una sola vez al inicio del lote; las etapas 0-2 pasan a ser lookups en memoria (O(1)).
    // Solo la Etapa 4 (fuzzy/pg_trgm) sigue yendo a la base por línea, porque depende del motor
    // de similitud de Postgres.

    /// <summary>Textos ya confirmados como "no es SSOMA" (alias con item_id NULL) — Etapa 0.</summary>
    Task<HashSet<string>> ObtenerRechazosConocidosAsync();
    /// <summary>Alias texto_crudo_norm -> ítem, de aliases activos — Etapa 1.</summary>
    Task<Dictionary<string, MatchResult>> ObtenerAliasesActivosAsync();
    /// <summary>Nombre normalizado del ítem -> ítem, del catálogo activo — Etapas 2 y 3.</summary>
    Task<Dictionary<string, MatchResult>> ObtenerNombresItemActivosAsync();
}
