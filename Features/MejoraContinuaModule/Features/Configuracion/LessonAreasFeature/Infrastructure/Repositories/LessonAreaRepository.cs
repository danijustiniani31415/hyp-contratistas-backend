using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Infrastructure.Repositories
{
    public class LessonAreaRepository : ILessonAreaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public LessonAreaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Lista CADA NODO del árbol area_scope (hoja o intermedio) como una rama con su
        /// path completo (desde la raíz hasta ese nodo). Cada rama trae su estado en
        /// lesson_area (active=false si todavía no se ha togglado). Antes solo se listaban
        /// las hojas, lo que ocultaba un nodo intermedio (p. ej. "Unidad de Proyectos")
        /// en cuanto se le agregaba un hijo, aunque tuviera relaciones propias.
        /// </summary>
        public async Task<List<LessonAreaConfigItemDTO>> GetAllAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Cargar todos los nodos del scope con su area_item + area_type (solo vivos)
            var scopeNodes = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State && at.State && s.Active
                orderby s.DisplayOrder
                select new
                {
                    s.AreaScopeId,
                    s.AreaScopeParentId,
                    AreaItemName = ai.AreaItemName,
                    AreaTypeName = at.AreaTypeName
                }
            ).ToListAsync();

            var byId = scopeNodes.ToDictionary(n => n.AreaScopeId);

            var lessonAreas = await ctx.LessonArea.ToListAsync();
            var laByScopeId = lessonAreas.ToDictionary(l => l.AreaScopeId);

            // Nodos que tienen hijos (para habilitar "Agrupar subáreas").
            var nodesWithChildren = scopeNodes
                .Where(n => n.AreaScopeParentId.HasValue)
                .Select(n => n.AreaScopeParentId!.Value)
                .ToHashSet();

            // lesson_area_ids con plantilla (scope_item activo) — para habilitar "En formulario".
            var scopeLessonAreaIds = (await ctx.ScopeItem
                .Where(si => si.Active)
                .Select(si => si.LessonAreaId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            var result = new List<LessonAreaConfigItemDTO>();
            foreach (var node in scopeNodes)
            {
                // Se incluye CUALQUIER nodo (hoja o intermedio), no solo las hojas:
                // un nodo intermedio también es una rama válida y puede tener relaciones propias.

                // Reconstruir path desde la raíz hasta este nodo
                var path = new List<LessonAreaSegmentDTO>();
                int? cur = node.AreaScopeId;
                int safety = 50;
                while (cur.HasValue && safety-- > 0 && byId.TryGetValue(cur.Value, out var n))
                {
                    path.Insert(0, new LessonAreaSegmentDTO
                    {
                        AreaItemName = n.AreaItemName,
                        AreaTypeName = n.AreaTypeName
                    });
                    cur = n.AreaScopeParentId;
                }

                laByScopeId.TryGetValue(node.AreaScopeId, out var la);
                result.Add(new LessonAreaConfigItemDTO
                {
                    LessonAreaId       = la?.LessonAreaId,
                    AreaScopeId        = node.AreaScopeId,
                    Path               = path,
                    Active               = la != null && la.Active,
                    IncludeInForm        = la != null && la.IncludeInForm,
                    IncludeDescendants   = la != null && la.IncludeDescendants,
                    IncludeAsIndependent = la != null && la.IncludeAsIndependent,
                    HasScope             = la != null && scopeLessonAreaIds.Contains(la.LessonAreaId),
                    HasChildren          = nodesWithChildren.Contains(node.AreaScopeId)
                });
            }

            // Ordenar por path concatenado para visualización estable
            return result
                .OrderBy(r => string.Join(" > ", r.Path.Select(p => p.AreaItemName)))
                .ToList();
        }

        /// <summary>
        /// Áreas que aparecen en el FORMULARIO de creación de lecciones: ACTIVAS
        /// (lesson_area.active=true), marcadas para el formulario (include_in_form=true)
        /// Y con al menos un scope_item configurado (plantilla). Esto se usa en el
        /// dropdown de "Área" al crear una lección.
        /// </summary>
        public async Task<List<LessonAreaConfigItemDTO>> GetAllWithScopeAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Lesson_areas activas que tienen al menos un scope_item activo
            var validLessonAreaIds = await ctx.ScopeItem
                .Where(si => si.Active)
                .Select(si => si.LessonAreaId)
                .Distinct()
                .ToListAsync();

            var activeLessonAreas = await ctx.LessonArea
                .Where(la => la.Active && la.IncludeInForm && validLessonAreaIds.Contains(la.LessonAreaId))
                .ToListAsync();

            if (activeLessonAreas.Count == 0)
                return new List<LessonAreaConfigItemDTO>();

            var areaScopeIds = activeLessonAreas.Select(la => la.AreaScopeId).Distinct().ToList();

            // Cargar todos los nodos del scope (todos los antecesores también)
            var allScopeNodes = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State && at.State
                select new
                {
                    s.AreaScopeId,
                    s.AreaScopeParentId,
                    AreaItemName = ai.AreaItemName,
                    AreaTypeName = at.AreaTypeName
                }
            ).ToListAsync();
            var byId = allScopeNodes.ToDictionary(n => n.AreaScopeId);

            var result = new List<LessonAreaConfigItemDTO>();
            foreach (var la in activeLessonAreas)
            {
                // Construir path desde la raíz hasta el nodo
                var path = new List<LessonAreaSegmentDTO>();
                int? cur = la.AreaScopeId;
                int safety = 50;
                while (cur.HasValue && safety-- > 0 && byId.TryGetValue(cur.Value, out var n))
                {
                    path.Insert(0, new LessonAreaSegmentDTO
                    {
                        AreaItemName = n.AreaItemName,
                        AreaTypeName = n.AreaTypeName
                    });
                    cur = n.AreaScopeParentId;
                }
                if (path.Count == 0) continue; // scope inválido, lo saltamos

                result.Add(new LessonAreaConfigItemDTO
                {
                    LessonAreaId         = la.LessonAreaId,
                    AreaScopeId          = la.AreaScopeId,
                    Path                 = path,
                    Active               = la.Active,
                    IncludeInForm        = la.IncludeInForm,
                    IncludeAsIndependent = la.IncludeAsIndependent
                });
            }

            return result
                .OrderBy(r => string.Join(" > ", r.Path.Select(p => p.AreaItemName)))
                .ToList();
        }

        /// <summary>
        /// Áreas para el FILTRO del listado de lecciones: activas y marcadas para el
        /// formulario (include_in_form) O como contenedor de filtro (include_descendants).
        /// A diferencia de GetAllWithScopeAsync, INCLUYE contenedores aunque no estén en el
        /// formulario, para poder filtrar por un área padre que agrupa lecciones (p. ej.
        /// "Unidad de Proyectos" con include_in_form=false pero con lecciones propias).
        /// </summary>
        public async Task<List<LessonAreaConfigItemDTO>> GetAllForFilterAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var areas = await ctx.LessonArea
                .Where(la => la.Active && (la.IncludeInForm || la.IncludeDescendants))
                .ToListAsync();
            if (areas.Count == 0)
                return new List<LessonAreaConfigItemDTO>();

            var allScopeNodes = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State && at.State
                select new
                {
                    s.AreaScopeId,
                    s.AreaScopeParentId,
                    AreaItemName = ai.AreaItemName,
                    AreaTypeName = at.AreaTypeName
                }
            ).ToListAsync();
            var byId = allScopeNodes.ToDictionary(n => n.AreaScopeId);

            var result = new List<LessonAreaConfigItemDTO>();
            foreach (var la in areas)
            {
                var path = new List<LessonAreaSegmentDTO>();
                int? cur = la.AreaScopeId;
                int safety = 50;
                while (cur.HasValue && safety-- > 0 && byId.TryGetValue(cur.Value, out var n))
                {
                    path.Insert(0, new LessonAreaSegmentDTO { AreaItemName = n.AreaItemName, AreaTypeName = n.AreaTypeName });
                    cur = n.AreaScopeParentId;
                }
                if (path.Count == 0) continue;

                result.Add(new LessonAreaConfigItemDTO
                {
                    LessonAreaId         = la.LessonAreaId,
                    AreaScopeId          = la.AreaScopeId,
                    Path                 = path,
                    Active               = la.Active,
                    IncludeInForm        = la.IncludeInForm,
                    IncludeAsIndependent = la.IncludeAsIndependent
                });
            }

            return result
                .OrderBy(r => string.Join(" > ", r.Path.Select(p => p.AreaItemName)))
                .ToList();
        }

        /// <summary>
        /// Toggle del flag activo para un area_scope.
        /// Si no existe fila en lesson_area, la crea con active=true.
        /// Si ya existe, invierte el flag.
        /// </summary>
        public async Task<ToggleLessonAreaResultDTO> ToggleAsync(int areaScopeId)
        {
            using var ctx = _factory.CreateDbContext();

            var scopeExists = await ctx.AreaScope.AnyAsync(s => s.AreaScopeId == areaScopeId && s.State && s.Active);
            if (!scopeExists)
                throw new AbrilException("La rama no existe o está inactiva.", 404);

            var row = await ctx.LessonArea.FirstOrDefaultAsync(la => la.AreaScopeId == areaScopeId);

            if (row == null)
            {
                row = new LessonArea
                {
                    AreaScopeId = areaScopeId,
                    Active      = true,
                    CreatedAt   = DateTimeOffset.UtcNow
                };
                ctx.LessonArea.Add(row);
            }
            else
            {
                row.Active = !row.Active;
            }

            await ctx.SaveChangesAsync();
            return new ToggleLessonAreaResultDTO { LessonAreaId = row.LessonAreaId, Active = row.Active };
        }

        public Task<SetLessonAreaFlagResultDTO> SetIncludeInFormAsync(int areaScopeId, bool value)
            => SetFlagAsync(areaScopeId, value, isForm: true);

        public Task<SetLessonAreaFlagResultDTO> SetIncludeDescendantsAsync(int areaScopeId, bool value)
            => SetFlagAsync(areaScopeId, value, isForm: false);

        /// <summary>
        /// Prende/apaga include_in_form o include_descendants. Solo válido sobre un área
        /// que ya exista y esté ACTIVA (los flags no aplican si está inactiva).
        /// Si se apaga include_in_form, también se apaga include_as_independent (no puede
        /// ser independiente un área que no aparece en el formulario).
        /// </summary>
        private async Task<SetLessonAreaFlagResultDTO> SetFlagAsync(int areaScopeId, bool value, bool isForm)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await ctx.LessonArea.FirstOrDefaultAsync(la => la.AreaScopeId == areaScopeId);
            if (row == null || !row.Active)
                throw new AbrilException("Primero activa el área.", 400);

            if (isForm)
            {
                row.IncludeInForm = value;
                if (!value) row.IncludeAsIndependent = false;
            }
            else row.IncludeDescendants = value;

            await ctx.SaveChangesAsync();
            return new SetLessonAreaFlagResultDTO { LessonAreaId = row.LessonAreaId, Value = value };
        }

        /// <summary>
        /// Prende/apaga include_as_independent. Requiere que el área esté ACTIVA y marcada
        /// "En formulario" (include_in_form). Un área independiente se muestra como opción
        /// de primer nivel en el formulario y no despliega a sus áreas hijas.
        /// </summary>
        public async Task<SetLessonAreaFlagResultDTO> SetIncludeAsIndependentAsync(int areaScopeId, bool value)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await ctx.LessonArea.FirstOrDefaultAsync(la => la.AreaScopeId == areaScopeId);
            if (row == null || !row.Active)
                throw new AbrilException("Primero activa el área.", 400);

            if (value && !row.IncludeInForm)
                throw new AbrilException("El área debe estar marcada 'En formulario' antes de ser independiente.", 400);

            row.IncludeAsIndependent = value;

            await ctx.SaveChangesAsync();
            return new SetLessonAreaFlagResultDTO { LessonAreaId = row.LessonAreaId, Value = value };
        }
    }
}
