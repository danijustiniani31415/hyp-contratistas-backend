using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Infrastructure.Repositories
{
    public class ScopeRepository : IScopeRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ScopeRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ── ScopeItem ────────────────────────────────────────────────────────────

        public async Task<List<ScopeItemDTO>> GetScopeTreeAsync(int lessonAreaId)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildScopeTreeAsync(ctx, lessonAreaId);
        }

        public async Task<List<ScopeItemDTO>> GetScopeForLessonAsync(int lessonAreaId)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildScopeTreeAsync(ctx, lessonAreaId);
        }

        private static async Task<List<ScopeItemDTO>> BuildScopeTreeAsync(AppDbContext ctx, int lessonAreaId)
        {
            var flatItems = await (
                from si in ctx.ScopeItem
                join ci in ctx.CatalogItem on si.CatalogItemId equals ci.CatalogItemId
                join ct in ctx.CatalogType on ci.CatalogTypeId equals ct.CatalogTypeId
                where si.LessonAreaId == lessonAreaId && si.Active
                orderby si.DisplayOrder
                select new ScopeItemDTO
                {
                    ScopeItemId = si.ScopeItemId,
                    LessonAreaId = si.LessonAreaId,
                    CatalogItemId = si.CatalogItemId,
                    CatalogItemDescription = ci.CatalogItemDescription,
                    CatalogTypeName = ct.CatalogTypeName,
                    ScopeItemParentId = si.ScopeItemParentId,
                    DisplayOrder = si.DisplayOrder,
                    Active = si.Active
                }
            ).ToListAsync();

            return BuildScopeTreeFromFlat(flatItems, null);
        }

        private static List<ScopeItemDTO> BuildScopeTreeFromFlat(List<ScopeItemDTO> all, int? parentId)
        {
            return all
                .Where(i => i.ScopeItemParentId == parentId)
                .OrderBy(i => i.DisplayOrder)
                .Select(i =>
                {
                    i.Children = BuildScopeTreeFromFlat(all, i.ScopeItemId);
                    return i;
                })
                .ToList();
        }

        public async Task UpsertScopeAsync(ScopeItemUpsertDTO dto)
        {
            using var ctx = _factory.CreateDbContext();

            var lessonAreaExists = await ctx.LessonArea.AnyAsync(la => la.LessonAreaId == dto.LessonAreaId);
            if (!lessonAreaExists)
                throw new AbrilException("El área no está habilitada para Lecciones Aprendidas.", 400);

            // Romper FK self-referential antes de borrar, luego eliminar todo
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE scope_item SET scope_item_parent_id = NULL WHERE lesson_area_id = {dto.LessonAreaId}"
            );
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM scope_item WHERE lesson_area_id = {dto.LessonAreaId}"
            );

            if (dto.Items.Count == 0) return;

            // nodeId (del cliente: real o temporal negativo) → scope_item_id recién creado
            var inserted = new Dictionary<int, int>();
            var pending = dto.Items.ToList();
            int safety = pending.Count + 5;

            while (pending.Count > 0 && safety-- > 0)
            {
                var ready = pending
                    .Where(n => !n.ParentNodeId.HasValue
                                || inserted.ContainsKey(n.ParentNodeId.Value))
                    .OrderBy(n => n.DisplayOrder)
                    .ToList();

                if (ready.Count == 0) break; // huérfanos o ciclo

                foreach (var node in ready)
                {
                    int? parentScopeItemId = node.ParentNodeId.HasValue
                        ? inserted[node.ParentNodeId.Value]
                        : (int?)null;

                    var scopeItem = new ScopeItem
                    {
                        LessonAreaId = dto.LessonAreaId,
                        CatalogItemId = node.CatalogItemId,
                        ScopeItemParentId = parentScopeItemId,
                        DisplayOrder = node.DisplayOrder,
                        Active = true
                    };

                    ctx.ScopeItem.Add(scopeItem);
                    await ctx.SaveChangesAsync();
                    inserted[node.NodeId] = scopeItem.ScopeItemId;
                }

                pending = pending.Except(ready).ToList();
            }
        }

        // ── ScopeTemplate ────────────────────────────────────────────────────────

        public async Task<List<ScopeTemplateDTO>> GetTemplatesAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var templates = await ctx.ScopeTemplate
                .Where(t => t.Active)
                .OrderBy(t => t.TemplateName)
                .ToListAsync();

            var templateIds = templates.Select(t => t.ScopeTemplateId).ToList();

            var rawItems = await (
                from sti in ctx.ScopeTemplateItem
                join ci in ctx.CatalogItem on sti.CatalogItemId equals ci.CatalogItemId
                where templateIds.Contains(sti.ScopeTemplateId) && sti.Active
                orderby sti.ScopeTemplateId, sti.DisplayOrder
                select new
                {
                    sti.ScopeTemplateId,
                    sti.ScopeTemplateItemId,
                    sti.ScopeTemplateItemParentId,
                    sti.DisplayOrder,
                    sti.CatalogItemId,
                    ci.CatalogItemDescription
                }
            ).ToListAsync();

            return templates.Select(t => new ScopeTemplateDTO
            {
                ScopeTemplateId = t.ScopeTemplateId,
                TemplateName = t.TemplateName,
                Active = t.Active,
                Items = rawItems
                    .Where(i => i.ScopeTemplateId == t.ScopeTemplateId)
                    .Select(i => new ScopeTemplateItemNodeDTO
                    {
                        NodeId = i.ScopeTemplateItemId,
                        ParentNodeId = i.ScopeTemplateItemParentId,
                        CatalogItemId = i.CatalogItemId,
                        CatalogItemDescription = i.CatalogItemDescription,
                        DisplayOrder = i.DisplayOrder
                    })
                    .ToList()
            }).ToList();
        }

        public async Task CreateTemplateAsync(ScopeTemplateCreateDTO dto)
        {
            using var ctx = _factory.CreateDbContext();

            var template = new ScopeTemplate
            {
                TemplateName = dto.TemplateName.Trim(),
                Active = true,
                CreatedDateTime = DateTimeOffset.UtcNow
            };

            ctx.ScopeTemplate.Add(template);
            await ctx.SaveChangesAsync();

            if (dto.Items.Count > 0)
                await InsertTemplateItemsAsync(ctx, template.ScopeTemplateId, dto.Items);
        }

        public async Task UpdateTemplateAsync(ScopeTemplateUpdateDTO dto)
        {
            using var ctx = _factory.CreateDbContext();

            var template = await ctx.ScopeTemplate.FirstOrDefaultAsync(t => t.ScopeTemplateId == dto.ScopeTemplateId);
            if (template == null)
                throw new AbrilException("La plantilla no existe.", 404);

            template.TemplateName = dto.TemplateName.Trim();
            template.UpdatedDateTime = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE scope_template_item SET scope_template_item_parent_id = NULL WHERE scope_template_id = {dto.ScopeTemplateId}"
            );
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM scope_template_item WHERE scope_template_id = {dto.ScopeTemplateId}"
            );

            if (dto.Items.Count > 0)
                await InsertTemplateItemsAsync(ctx, dto.ScopeTemplateId, dto.Items);
        }

        private static async Task InsertTemplateItemsAsync(
            AppDbContext ctx,
            int scopeTemplateId,
            List<ScopeTemplateItemNodeDTO> nodes)
        {
            // nodeId (del cliente: real o temporal negativo) → scope_template_item_id recién creado
            var inserted = new Dictionary<int, int>();
            var pending = nodes.ToList();
            int safety = pending.Count + 5;

            while (pending.Count > 0 && safety-- > 0)
            {
                var ready = pending
                    .Where(n => !n.ParentNodeId.HasValue
                                || inserted.ContainsKey(n.ParentNodeId.Value))
                    .OrderBy(n => n.DisplayOrder)
                    .ToList();

                if (ready.Count == 0) break;

                foreach (var node in ready)
                {
                    int? parentStiId = node.ParentNodeId.HasValue
                        ? inserted[node.ParentNodeId.Value]
                        : (int?)null;

                    var entity = new ScopeTemplateItem
                    {
                        ScopeTemplateId = scopeTemplateId,
                        CatalogItemId = node.CatalogItemId,
                        ScopeTemplateItemParentId = parentStiId,
                        DisplayOrder = node.DisplayOrder,
                        Active = true
                    };

                    ctx.ScopeTemplateItem.Add(entity);
                    await ctx.SaveChangesAsync();
                    inserted[node.NodeId] = entity.ScopeTemplateItemId;
                }

                pending = pending.Except(ready).ToList();
            }
        }

        public async Task DeleteTemplateAsync(int scopeTemplateId)
        {
            using var ctx = _factory.CreateDbContext();

            var template = await ctx.ScopeTemplate.FirstOrDefaultAsync(t => t.ScopeTemplateId == scopeTemplateId);
            if (template == null)
                throw new AbrilException("La plantilla no existe.", 404);

            template.Active = false;
            template.UpdatedDateTime = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
