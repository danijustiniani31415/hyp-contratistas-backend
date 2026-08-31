using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Models;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Repositories
{
    public class AreaScopeRepository : IAreaScopeRepository
    {
        private readonly AppDbContext _context;

        public AreaScopeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AreaScopeTreeDto>> GetTreeAsync()
        {
            var flat = await (
                from s in _context.AreaScope
                join ai in _context.AreaItem on s.AreaItemId equals ai.AreaItemId
                join at in _context.AreaType on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State && at.State && s.Active
                orderby s.DisplayOrder
                select new AreaScopeTreeDto
                {
                    AreaScopeId       = s.AreaScopeId,
                    AreaItemId        = s.AreaItemId,
                    AreaItemName      = ai.AreaItemName,
                    AreaTypeId        = ai.AreaTypeId,
                    AreaTypeName      = at.AreaTypeName,
                    AreaScopeParentId = s.AreaScopeParentId,
                    DisplayOrder      = s.DisplayOrder,
                    Active            = s.Active,
                }
            ).ToListAsync();

            // Trabajadores activos asignados directamente a cada nodo (workers.area_scope_id).
            var workerCounts = await _context.Worker
                .Where(w => w.AreaScopeId != null && w.WorkersEstadoId == WorkersEstadoIds.Activo)
                .GroupBy(w => w.AreaScopeId!.Value)
                .Select(g => new { AreaScopeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.AreaScopeId, g => g.Count);

            var byId = flat.ToDictionary(n => n.AreaScopeId);
            var roots = new List<AreaScopeTreeDto>();
            foreach (var n in flat)
            {
                n.WorkersCount = workerCounts.GetValueOrDefault(n.AreaScopeId);
                if (n.AreaScopeParentId.HasValue && byId.TryGetValue(n.AreaScopeParentId.Value, out var p))
                    p.Children.Add(n);
                else
                    roots.Add(n);
            }
            return roots;
        }

        public async Task<List<AreaScopeWorkerDto>> GetWorkersAsync(int areaScopeId)
        {
            return await (
                from w in _context.Worker
                where w.AreaScopeId == areaScopeId && w.WorkersEstadoId == WorkersEstadoIds.Activo
                join p in _context.Person on w.PersonId equals p.PersonId into pj
                from p in pj.DefaultIfEmpty()
                join pu in _context.Puesto on w.PuestoId equals pu.PuestoId into puj
                from pu in puj.DefaultIfEmpty()
                join wc in _context.Categoria on pu.CategoriaId equals wc.CategoriaId into wcj
                from wc in wcj.DefaultIfEmpty()
                orderby (p != null && p.FullName != null ? p.FullName : w.ApellidoNombre)
                select new AreaScopeWorkerDto
                {
                    WorkerId         = w.Id,
                    FullName         = p != null && p.FullName != null ? p.FullName : w.ApellidoNombre,
                    EmailCorporativo = w.EmailCorporativo,
                    CategoryName     = wc != null ? wc.Nombre : null,
                }
            ).ToListAsync();
        }

        /// <summary>
        /// Crea una nueva rama (subárbol) en area_scope. Procesa los nodos en topo-orden
        /// (padres antes que hijos) usando un mapeo tempId → areaScopeId real.
        ///
        /// Si un nodo (area_item_id, area_scope_parent_id) ya existe en BD, se REUTILIZA
        /// en lugar de duplicarse. Si TODA la rama ya existe (no se insertó ningún nodo
        /// nuevo), se lanza un error.
        /// </summary>
        public async Task CreateBranchAsync(AreaScopeBranchDto dto)
        {
            if (dto.Nodes == null || dto.Nodes.Count == 0)
                throw new AbrilException("La rama no puede estar vacía.");

            // Validar que todos los area_item existen y están vivos
            var areaItemIds = dto.Nodes.Select(n => n.AreaItemId).Distinct().ToList();
            var existingCount = await _context.AreaItem
                .CountAsync(a => a.State && areaItemIds.Contains(a.AreaItemId));
            if (existingCount != areaItemIds.Count)
                throw new AbrilException("Una o más áreas seleccionadas no existen.");

            var idMap = new Dictionary<int, int>(); // tempId → areaScopeId real (existente o nuevo)
            var pending = dto.Nodes.ToList();
            int safety = pending.Count + 5;
            int newlyInserted = 0;

            while (pending.Count > 0 && safety-- > 0)
            {
                var ready = pending
                    .Where(n => !n.ParentTempId.HasValue || idMap.ContainsKey(n.ParentTempId.Value))
                    .OrderBy(n => n.DisplayOrder)
                    .ToList();

                if (ready.Count == 0) break; // huérfanos o ciclo

                foreach (var node in ready)
                {
                    int? parentScopeId = node.ParentTempId.HasValue
                        ? idMap[node.ParentTempId.Value]
                        : (int?)null;

                    // ¿Existe ya un nodo vivo con esta combinación (area_item_id, parent_id)?
                    var existing = await _context.AreaScope.FirstOrDefaultAsync(s =>
                        s.State &&
                        s.AreaItemId == node.AreaItemId &&
                        s.AreaScopeParentId == parentScopeId);

                    if (existing != null)
                    {
                        idMap[node.TempId] = existing.AreaScopeId;
                    }
                    else
                    {
                        var entity = new AreaScope
                        {
                            AreaItemId        = node.AreaItemId,
                            AreaScopeParentId = parentScopeId,
                            DisplayOrder      = node.DisplayOrder,
                            Active            = true,
                            State             = true
                        };
                        _context.AreaScope.Add(entity);
                        await _context.SaveChangesAsync();
                        idMap[node.TempId] = entity.AreaScopeId;
                        newlyInserted++;
                    }
                }

                pending = pending.Except(ready).ToList();
            }

            if (newlyInserted == 0)
                throw new AbrilException("Esta rama ya existe.");
        }

        /// <summary>
        /// Reasigna el padre de un nodo (el nodo se mueve junto con todo su subárbol).
        /// newParentAreaScopeId null = mover a la raíz. Valida existencia, ciclos
        /// (el nuevo padre no puede ser el propio nodo ni un descendiente) y duplicados
        /// (no puede existir otro nodo vivo con la misma área bajo el mismo padre).
        /// </summary>
        public async Task UpdateParentAsync(int areaScopeId, int? newParentAreaScopeId)
        {
            var entity = await _context.AreaScope.FirstOrDefaultAsync(s => s.State && s.AreaScopeId == areaScopeId);
            if (entity == null)
                throw new AbrilException("El nodo no existe.");

            if (entity.AreaScopeParentId == newParentAreaScopeId)
                throw new AbrilException("El nodo ya se encuentra bajo ese padre.");

            if (newParentAreaScopeId.HasValue)
            {
                if (newParentAreaScopeId.Value == areaScopeId)
                    throw new AbrilException("Un nodo no puede ser su propio padre.");

                // Un solo roundtrip: pares (id → parent) vivos para validar existencia y ciclos en memoria.
                var parentById = await _context.AreaScope
                    .Where(s => s.State)
                    .Select(s => new { s.AreaScopeId, s.AreaScopeParentId })
                    .ToDictionaryAsync(s => s.AreaScopeId, s => s.AreaScopeParentId);

                if (!parentById.ContainsKey(newParentAreaScopeId.Value))
                    throw new AbrilException("El nuevo nodo padre no existe.");

                int? cursor = newParentAreaScopeId;
                int safety = parentById.Count + 1;
                while (cursor.HasValue && safety-- > 0)
                {
                    if (cursor.Value == areaScopeId)
                        throw new AbrilException("No se puede mover: el nuevo padre es un descendiente del nodo.");
                    cursor = parentById.GetValueOrDefault(cursor.Value);
                }
            }

            var duplicate = await _context.AreaScope.AnyAsync(s =>
                s.State &&
                s.AreaScopeId != areaScopeId &&
                s.AreaItemId == entity.AreaItemId &&
                s.AreaScopeParentId == newParentAreaScopeId);
            if (duplicate)
                throw new AbrilException("Ya existe un nodo con esta área bajo el padre seleccionado.");

            var maxOrder = await _context.AreaScope
                .Where(s => s.State && s.AreaScopeParentId == newParentAreaScopeId && s.AreaScopeId != areaScopeId)
                .MaxAsync(s => (int?)s.DisplayOrder) ?? 0;

            entity.AreaScopeParentId = newParentAreaScopeId;
            entity.DisplayOrder = maxOrder + 1;
            await _context.SaveChangesAsync();
        }

        /// <summary>Soft delete: marca state = false (el registro se mantiene en BD para auditoría).</summary>
        public async Task DeleteAsync(int areaScopeId)
        {
            var entity = await _context.AreaScope.FirstOrDefaultAsync(s => s.State && s.AreaScopeId == areaScopeId);
            if (entity == null) return;

            var hasChildren = await _context.AreaScope.AnyAsync(c => c.State && c.AreaScopeParentId == areaScopeId);
            if (hasChildren)
                throw new AbrilException("No se puede eliminar: el nodo tiene hijos. Elimina los hijos primero.");

            entity.State = false;
            await _context.SaveChangesAsync();
        }
    }
}
