using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Services
{
    /// <summary>
    /// Resuelve la jefatura (revisor) de una lección con la asignación DIRECTA
    /// <c>workers.worker_lesson_jefe_id</c> (sección "Revisor de Trabajadores" en la
    /// configuración de recordatorios). Solo el jefe ahí asignado puede
    /// aprobar/rechazar/editar las lecciones de su subordinado; sin asignación no hay
    /// revisor. (Antes se resolvía caminando el árbol area_scope; ese mecanismo quedó
    /// reemplazado por la asignación directa, que es editable por trabajador.)
    /// </summary>
    public class LessonJefeResolver : ILessonJefeResolver
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public LessonJefeResolver(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<int?> ResolveJefeUserIdAsync(int autorUserId)
        {
            using var ctx = _factory.CreateDbContext();

            // autor (user → person → worker) → worker_lesson_jefe_id → jefe (worker → person → user).
            return await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId == autorUserId && w.WorkerLessonJefeId != null
                join jw in ctx.Worker on w.WorkerLessonJefeId equals jw.Id
                join jp in ctx.Person on jw.PersonId equals jp.PersonId
                where jp.UserId != null
                select jp.UserId
            ).FirstOrDefaultAsync();
        }

        public async Task<List<int>> GetSubordinateUserIdsAsync(int jefeUserId)
        {
            using var ctx = _factory.CreateDbContext();

            // workers del jefe (un user puede mapear a más de un worker).
            var jefeWorkerIds = await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId == jefeUserId
                select w.Id
            ).ToListAsync();

            if (jefeWorkerIds.Count == 0) return new List<int>();

            return await (
                from w in ctx.Worker
                where w.WorkerLessonJefeId != null && jefeWorkerIds.Contains(w.WorkerLessonJefeId.Value)
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId != null && p.UserId != jefeUserId
                select p.UserId!.Value
            ).Distinct().ToListAsync();
        }

        public async Task<bool> CanReviewProjectAsync(int reviewerUserId, int? projectId)
        {
            using var ctx = _factory.CreateDbContext();

            var categoriaId = await GetCategoriaIdByUserIdAsync(ctx, reviewerUserId);

            // Solo el Residente está acotado por proyecto; el resto no tiene restricción.
            if (categoriaId != CategoriaIds.Residente)
                return true;

            if (!projectId.HasValue) return false;

            return await (
                from up in ctx.UserProject
                join w in ctx.Worker on up.WorkerId equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId == reviewerUserId
                      && up.ProjectId == projectId.Value
                      && up.State && up.Active
                select up.UserProjectId
            ).AnyAsync();
        }

        public async Task<List<int>?> GetResidenteProjectScopeAsync(int reviewerUserId)
        {
            using var ctx = _factory.CreateDbContext();

            var categoriaId = await GetCategoriaIdByUserIdAsync(ctx, reviewerUserId);
            if (categoriaId != CategoriaIds.Residente)
                return null; // no es Residente → sin restricción de proyecto

            return await (
                from up in ctx.UserProject
                join w in ctx.Worker on up.WorkerId equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId == reviewerUserId && up.State && up.Active
                select up.ProjectId
            ).Distinct().ToListAsync();
        }

        /// <summary>Categoría del usuario revisor, leída de su puesto
        /// (<c>workers.puesto_id -> puesto.categoria_id</c>).</summary>
        private static async Task<int?> GetCategoriaIdByUserIdAsync(AppDbContext ctx, int userId)
        {
            return await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                join pu in ctx.Puesto on w.PuestoId equals pu.PuestoId
                where p.UserId == userId
                select (int?)pu.CategoriaId
            ).FirstOrDefaultAsync();
        }
    }
}
