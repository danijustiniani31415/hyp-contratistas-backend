using System.Security.Claims;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Filters
{
    /// <summary>
    /// Autorización server-side por featureKey, espejo de roleGuard/featureKey en el frontend
    /// (core/guards/role.guard.ts). [Authorize] solo exige JWT válido — sin esto, cualquier
    /// usuario autenticado puede llamar el endpoint aunque su rol no tenga el featureKey
    /// asignado en role_feature (la restricción de rol solo bloqueaba la navegación, no la API).
    ///
    /// Los role_id del usuario viajan como múltiples claims ClaimTypes.Role (ver JWTService.cs).
    /// Se compara contra role_feature/feature igual que RoleFeatureRepository.GetRoleFeatureIds.
    /// </summary>
    /// <summary>
    /// Acepta uno o más featureKeys (OR): basta con que el rol tenga acceso a alguno.
    /// Necesario para controllers compartidos por más de un frontend con distinto
    /// featureKey (p. ej. EmoController/ProgramacionEmoController: SSOMA y Clínica).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireFeatureAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _featureKeys;

        public RequireFeatureAttribute(params string[] featureKeys)
        {
            _featureKeys = featureKeys;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
                return; // [Authorize] ya devuelve 401 antes de llegar aquí

            var roleIds = user.FindAll(ClaimTypes.Role)
                .Select(c => int.TryParse(c.Value, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToArray();

            if (roleIds.Length == 0)
            {
                context.Result = new ObjectResult(new { message = "No tiene un rol asignado." }) { StatusCode = 403 };
                return;
            }

            var factory = context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var ctx = await factory.CreateDbContextAsync();

            var tieneAcceso = await ctx.Database.SqlQuery<int>($"""
                SELECT 1 AS "Value"
                FROM role_feature rf
                JOIN feature f ON f.feature_id = rf.feature_id
                WHERE f.feature_key = ANY({_featureKeys})
                  AND rf.role_id = ANY({roleIds})
                LIMIT 1
                """)
                .AnyAsync();

            if (!tieneAcceso)
            {
                context.Result = new ObjectResult(new { message = "No tiene permiso para acceder a este recurso." }) { StatusCode = 403 };
            }
        }
    }
}
