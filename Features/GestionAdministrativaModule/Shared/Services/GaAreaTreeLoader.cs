using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Services
{
    /// <summary>
    /// Carga el árbol area_scope como lista plana para los filtros/modales en cascada
    /// del módulo (visibilidad de salidas, revisor de salidas).
    /// </summary>
    public static class GaAreaTreeLoader
    {
        public static async Task<List<GaAreaNodeDto>> LoadAsync(AppDbContext ctx)
        {
            return await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State && at.State
                orderby s.DisplayOrder
                select new GaAreaNodeDto
                {
                    AreaScopeId = s.AreaScopeId,
                    AreaItemId = s.AreaItemId,
                    AreaItemName = ai.AreaItemName,
                    AreaTypeId = ai.AreaTypeId,
                    AreaTypeName = at.AreaTypeName,
                    AreaScopeParentId = s.AreaScopeParentId,
                    DisplayOrder = s.DisplayOrder,
                }
            ).ToListAsync();
        }
    }
}
