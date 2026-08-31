using Microsoft.EntityFrameworkCore;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Helpers
{
    public class LessonEnrichmentData
    {
        /// <summary>Path completo (Gerencia / Estándar / …). Para detalle.</summary>
        public string? AreaDescription { get; set; }
        /// <summary>Path "corto" sin Gerencia, MAYÚSCULAS. Para lista/tarjetas.</summary>
        public string? AreaListDescription { get; set; }
        /// <summary>
        /// Segmentos de la clasificación caminando scope_item hacia arriba.
        /// Cada entrada trae (catalog_type_name, catalog_item_description) ordenados
        /// de raíz a hoja.
        /// </summary>
        public List<LessonClassificationSegmentDTO> ClassificationSegments { get; set; } = new();
    }

    /// <summary>Nombre del area_type que debe ocultarse en listas/tarjetas (se conserva en detalle).</summary>
    internal static class AreaTypeNames
    {
        public const string Gerencia = "Área de Gerencia";
    }

    /// <summary>
    /// Calcula:
    /// • El path de área (via lesson_area → area_scope → area_item) en dos formatos
    ///   (completo y "sin Gerencia + MAYÚSCULAS").
    /// • La clasificación (Fase / Etapa / … / Partida) caminando scope_item hacia arriba
    ///   desde el catalog_item de la lección.
    /// Reemplaza la vieja lógica basada en phase_stage_sub_stage_sub_specialty.
    /// </summary>
    public static class LessonEnrichmentHelper
    {
        public static async Task<Dictionary<int, LessonEnrichmentData>> ComputeAsync(
            AppDbContext ctx,
            IReadOnlyList<(int LessonId, int? LessonAreaId, int? CatalogItemId)> lessons)
        {
            var result = new Dictionary<int, LessonEnrichmentData>();
            if (lessons.Count == 0) return result;

            // ── 1. Área (path full + path corto sin Gerencia) ───────────────────
            var lessonAreaIds = lessons
                .Where(l => l.LessonAreaId.HasValue)
                .Select(l => l.LessonAreaId!.Value)
                .Distinct()
                .ToList();

            var pathByLessonAreaId = new Dictionary<int, string>();
            var pathListByLessonAreaId = new Dictionary<int, string>();
            if (lessonAreaIds.Count > 0)
            {
                var laToScope = await ctx.LessonArea
                    .Where(la => lessonAreaIds.Contains(la.LessonAreaId))
                    .Select(la => new { la.LessonAreaId, la.AreaScopeId })
                    .ToListAsync();

                var allAreaScope = await (
                    from s in ctx.AreaScope
                    join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                    join at in ctx.AreaType on ai.AreaTypeId equals at.AreaTypeId
                    where s.State && ai.State
                    select new
                    {
                        s.AreaScopeId,
                        s.AreaScopeParentId,
                        ai.AreaItemName,
                        AreaTypeName = at.AreaTypeName
                    }
                ).ToListAsync();
                var areaScopeById = allAreaScope.ToDictionary(n => n.AreaScopeId);

                foreach (var la in laToScope)
                {
                    var fullParts = new List<string>();
                    var listParts = new List<string>();
                    int? cur = la.AreaScopeId;
                    int safety = 50;
                    while (cur.HasValue && safety-- > 0 && areaScopeById.TryGetValue(cur.Value, out var n))
                    {
                        fullParts.Insert(0, n.AreaItemName);
                        if (!string.Equals(n.AreaTypeName, AreaTypeNames.Gerencia, StringComparison.OrdinalIgnoreCase))
                        {
                            listParts.Insert(0, n.AreaItemName.ToUpperInvariant());
                        }
                        cur = n.AreaScopeParentId;
                    }
                    if (fullParts.Count > 0)
                        pathByLessonAreaId[la.LessonAreaId] = string.Join(" / ", fullParts);
                    if (listParts.Count > 0)
                        pathListByLessonAreaId[la.LessonAreaId] = string.Join(" / ", listParts);
                }
            }

            // ── 2. Clasificación: segmentos (catalog_type, catalog_item_description)
            //    caminando scope_item hacia arriba desde el catalog_item de la lección.
            var pairs = lessons
                .Where(l => l.LessonAreaId.HasValue && l.CatalogItemId.HasValue)
                .Select(l => (LessonAreaId: l.LessonAreaId!.Value, CatalogItemId: l.CatalogItemId!.Value))
                .Distinct()
                .ToList();

            var segmentsByPair = new Dictionary<(int, int), List<LessonClassificationSegmentDTO>>();

            if (pairs.Count > 0)
            {
                var activeLessonAreaIds = pairs.Select(p => p.LessonAreaId).Distinct().ToList();
                // OrderBy ScopeItemId ascending = MISMA criterio que el filtro en
                // LessonRepository.BuildAncestorCatalogItemsByPairAsync. Si el mismo
                // par (lesson_area_id, catalog_item_id) aparece en varios scope_items
                // (caso ambiguo cuando no se guarda scope_item_id en lesson), ambos
                // toman el de menor id → display y filtro nunca divergen.
                var allScope = await (
                    from si in ctx.ScopeItem
                    join ci in ctx.CatalogItem on si.CatalogItemId equals ci.CatalogItemId
                    join ct in ctx.CatalogType on ci.CatalogTypeId equals ct.CatalogTypeId
                    where activeLessonAreaIds.Contains(si.LessonAreaId) && si.Active
                    orderby si.ScopeItemId
                    select new
                    {
                        si.ScopeItemId,
                        si.LessonAreaId,
                        si.CatalogItemId,
                        si.ScopeItemParentId,
                        ci.CatalogItemDescription,
                        ct.CatalogTypeName
                    }
                ).ToListAsync();
                var scopeById = allScope.ToDictionary(s => s.ScopeItemId);

                foreach (var pair in pairs)
                {
                    // FirstOrDefault sobre lista ya ordenada por ScopeItemId asc
                    // garantiza la misma elección que el filtro.
                    var leaf = allScope.FirstOrDefault(s =>
                        s.LessonAreaId == pair.LessonAreaId && s.CatalogItemId == pair.CatalogItemId);
                    if (leaf == null) continue;

                    var segments = new List<LessonClassificationSegmentDTO>();
                    int? cur = leaf.ScopeItemId;
                    int safety = 50;
                    while (cur.HasValue && safety-- > 0 && scopeById.TryGetValue(cur.Value, out var s))
                    {
                        segments.Insert(0, new LessonClassificationSegmentDTO
                        {
                            CatalogTypeName = s.CatalogTypeName,
                            CatalogItemDescription = s.CatalogItemDescription
                        });
                        cur = s.ScopeItemParentId;
                    }
                    if (segments.Count > 0)
                        segmentsByPair[pair] = segments;
                }

                // Fallback: catalog_item no presente en scope_item del área — solo
                // exponemos un segmento con (catalog_type, descripción) del catálogo.
                var unmappedCatIds = pairs
                    .Where(p => !segmentsByPair.ContainsKey(p))
                    .Select(p => p.CatalogItemId)
                    .Distinct()
                    .ToList();
                if (unmappedCatIds.Count > 0)
                {
                    var fallback = await (
                        from ci in ctx.CatalogItem
                        join ct in ctx.CatalogType on ci.CatalogTypeId equals ct.CatalogTypeId
                        where unmappedCatIds.Contains(ci.CatalogItemId)
                        select new { ci.CatalogItemId, ci.CatalogItemDescription, ct.CatalogTypeName }
                    ).ToListAsync();
                    var fallbackByCat = fallback.ToDictionary(f => f.CatalogItemId);
                    foreach (var pair in pairs.Where(p => !segmentsByPair.ContainsKey(p)))
                    {
                        if (fallbackByCat.TryGetValue(pair.CatalogItemId, out var f))
                        {
                            segmentsByPair[pair] = new List<LessonClassificationSegmentDTO>
                            {
                                new LessonClassificationSegmentDTO
                                {
                                    CatalogTypeName = f.CatalogTypeName,
                                    CatalogItemDescription = f.CatalogItemDescription
                                }
                            };
                        }
                    }
                }
            }

            // ── 3. Armar resultado ──────────────────────────────────────────────
            foreach (var l in lessons)
            {
                var e = new LessonEnrichmentData();
                if (l.LessonAreaId.HasValue
                    && pathByLessonAreaId.TryGetValue(l.LessonAreaId.Value, out var path))
                {
                    e.AreaDescription = path;
                }
                if (l.LessonAreaId.HasValue
                    && pathListByLessonAreaId.TryGetValue(l.LessonAreaId.Value, out var listPath))
                {
                    e.AreaListDescription = listPath;
                }
                if (l.LessonAreaId.HasValue && l.CatalogItemId.HasValue
                    && segmentsByPair.TryGetValue((l.LessonAreaId.Value, l.CatalogItemId.Value), out var segs))
                {
                    e.ClassificationSegments = segs;
                }
                result[l.LessonId] = e;
            }

            return result;
        }
    }
}
