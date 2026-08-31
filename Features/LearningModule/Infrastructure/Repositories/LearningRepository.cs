using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.LearningModule.Application.Dtos;
using Abril_Backend.Features.LearningModule.Infrastructure.Interfaces;
using Abril_Backend.Features.LearningModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.LearningModule.Infrastructure.Repositories
{
    public class LearningRepository : ILearningRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public LearningRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ─────────────────────────────── Display ───────────────────────────────

        public async Task<List<LearningCategoryDto>> GetLoginCategories()
        {
            using var ctx = _factory.CreateDbContext();

            // /auth/login es público: se muestran todas las categorías de superficie LOGIN
            // activas, sin filtrar por rol (no hay sesión). Los videos de contratistas caen aquí.
            return await ctx.LearningCategory
                .Where(c => c.State && c.Active && c.Surface!.Code == "LOGIN")
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                .Select(c => new LearningCategoryDto
                {
                    Id = c.LearningCategoryId,
                    Nombre = c.Name,
                    AccentColor = c.AccentColor,
                    Videos = c.Videos
                        .Where(v => v.State && v.Active)
                        .OrderBy(v => v.DisplayOrder).ThenBy(v => v.LearningVideoId)
                        .Select(v => new LearningVideoDto { Titulo = v.Title, Url = v.Url, Img = v.ThumbnailUrl })
                        .ToList(),
                })
                .ToListAsync();
        }

        public async Task<List<LearningCategoryDto>> GetInicioCategories(int[] roleIds)
        {
            using var ctx = _factory.CreateDbContext();

            // /inicio requiere sesión: una categoría es visible si es "pública interna"
            // (todo Abril) o si el usuario tiene alguno de sus roles autorizados.
            // Se muestran todos los grupos visibles aunque no tengan videos asociados
            // (los encabezados vacíos son intencionales, igual que en la superficie LOGIN).
            return await ctx.LearningCategory
                .Where(c => c.State && c.Active && c.Surface!.Code == "INICIO"
                    && (c.EsPublicoInterno || c.Roles.Any(r => roleIds.Contains(r.RoleId))))
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                .Select(c => new LearningCategoryDto
                {
                    Id = c.LearningCategoryId,
                    Nombre = c.Name,
                    AccentColor = c.AccentColor,
                    Videos = c.Videos
                        .Where(v => v.State && v.Active)
                        .OrderBy(v => v.DisplayOrder).ThenBy(v => v.LearningVideoId)
                        .Select(v => new LearningVideoDto { Titulo = v.Title, Url = v.Url, Img = v.ThumbnailUrl })
                        .ToList(),
                })
                .ToListAsync();
        }

        // ─────────────────────────────── Admin ───────────────────────────────

        public async Task<LearningAdminDataDto> GetAdminData()
        {
            using var ctx = _factory.CreateDbContext();

            var categorias = await ctx.LearningCategory
                .Where(c => c.State)
                .OrderBy(c => c.Surface!.Code).ThenBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                .Select(c => new LearningCategoryAdminDto
                {
                    Id = c.LearningCategoryId,
                    Nombre = c.Name,
                    AccentColor = c.AccentColor,
                    Orden = c.DisplayOrder,
                    SurfaceId = c.LearningSurfaceId,
                    SurfaceCode = c.Surface!.Code,
                    SurfaceNombre = c.Surface!.Name,
                    EsPublicoInterno = c.EsPublicoInterno,
                    Activo = c.Active,
                    RoleIds = c.Roles.Select(r => r.RoleId).ToList(),
                    Videos = c.Videos
                        .Where(v => v.State)
                        .OrderBy(v => v.DisplayOrder).ThenBy(v => v.LearningVideoId)
                        .Select(v => new LearningVideoAdminDto
                        {
                            Id = v.LearningVideoId,
                            Titulo = v.Title,
                            Url = v.Url,
                            Img = v.ThumbnailUrl,
                            Orden = v.DisplayOrder,
                            Activo = v.Active,
                        }).ToList(),
                })
                .ToListAsync();

            var superficies = await ctx.LearningSurface
                .OrderBy(s => s.LearningSurfaceId)
                .Select(s => new LearningSurfaceDto { Id = s.LearningSurfaceId, Code = s.Code, Nombre = s.Name })
                .ToListAsync();

            var roles = await ctx.Role
                .Where(r => r.State && r.Active)
                .OrderBy(r => r.RoleDescription)
                .Select(r => new LearningRoleOptionDto { Id = r.RoleId, Descripcion = r.RoleDescription })
                .ToListAsync();

            return new LearningAdminDataDto { Categorias = categorias, Superficies = superficies, Roles = roles };
        }

        public async Task<int> CreateCategory(LearningCategoryCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var nombre = (dto.Nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                throw new AbrilException("El nombre del grupo no puede estar vacío.", 400);

            var superficieExiste = await ctx.LearningSurface.AnyAsync(s => s.LearningSurfaceId == dto.SurfaceId);
            if (!superficieExiste)
                throw new AbrilException("La superficie indicada no existe.", 400);

            var duplicado = await ctx.LearningCategory.AnyAsync(c =>
                c.State && c.LearningSurfaceId == dto.SurfaceId && c.Name.ToLower() == nombre.ToLower());
            if (duplicado)
                throw new AbrilException("Ya existe un grupo con ese nombre en esa superficie.", 409);

            var cat = new LearningCategory
            {
                Name = nombre,
                LearningSurfaceId = dto.SurfaceId,
                AccentColor = string.IsNullOrWhiteSpace(dto.AccentColor) ? null : dto.AccentColor.Trim(),
                DisplayOrder = dto.Orden,
                EsPublicoInterno = dto.EsPublicoInterno,
                Active = true,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
                Roles = BuildRoles(dto.RoleIds, dto.EsPublicoInterno),
            };

            ctx.LearningCategory.Add(cat);
            await ctx.SaveChangesAsync();
            return cat.LearningCategoryId;
        }

        public async Task EditCategory(int id, LearningCategoryEditDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var nombre = (dto.Nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                throw new AbrilException("El nombre del grupo no puede estar vacío.", 400);

            var cat = await ctx.LearningCategory
                .Include(c => c.Roles)
                .FirstOrDefaultAsync(c => c.LearningCategoryId == id && c.State)
                ?? throw new AbrilException("Grupo no encontrado.", 404);

            var superficieExiste = await ctx.LearningSurface.AnyAsync(s => s.LearningSurfaceId == dto.SurfaceId);
            if (!superficieExiste)
                throw new AbrilException("La superficie indicada no existe.", 400);

            var duplicado = await ctx.LearningCategory.AnyAsync(c =>
                c.State && c.LearningCategoryId != id
                && c.LearningSurfaceId == dto.SurfaceId && c.Name.ToLower() == nombre.ToLower());
            if (duplicado)
                throw new AbrilException("Ya existe un grupo con ese nombre en esa superficie.", 409);

            cat.Name = nombre;
            cat.LearningSurfaceId = dto.SurfaceId;
            cat.AccentColor = string.IsNullOrWhiteSpace(dto.AccentColor) ? null : dto.AccentColor.Trim();
            cat.DisplayOrder = dto.Orden;
            cat.EsPublicoInterno = dto.EsPublicoInterno;
            cat.UpdatedDateTime = DateTimeOffset.UtcNow;

            ctx.LearningCategoryRole.RemoveRange(cat.Roles);
            cat.Roles = BuildRoles(dto.RoleIds, dto.EsPublicoInterno);

            await ctx.SaveChangesAsync();
        }

        public async Task<bool> ToggleCategory(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var cat = await ctx.LearningCategory.FirstOrDefaultAsync(c => c.LearningCategoryId == id && c.State)
                ?? throw new AbrilException("Grupo no encontrado.", 404);

            cat.Active = !cat.Active;
            cat.UpdatedDateTime = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
            return cat.Active;
        }

        public async Task DeleteCategory(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var cat = await ctx.LearningCategory
                .Include(c => c.Videos)
                .FirstOrDefaultAsync(c => c.LearningCategoryId == id && c.State)
                ?? throw new AbrilException("Grupo no encontrado.", 404);

            // Soft delete del grupo y de sus videos (auditoría: nada se borra físicamente).
            cat.State = false;
            cat.UpdatedDateTime = DateTimeOffset.UtcNow;
            foreach (var v in cat.Videos.Where(v => v.State))
            {
                v.State = false;
                v.UpdatedDateTime = DateTimeOffset.UtcNow;
            }
            await ctx.SaveChangesAsync();
        }

        public async Task<int> CreateVideo(LearningVideoCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var titulo = (dto.Titulo ?? string.Empty).Trim();
            var url = (dto.Url ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(titulo))
                throw new AbrilException("El título del video no puede estar vacío.", 400);
            if (string.IsNullOrWhiteSpace(url))
                throw new AbrilException("El enlace del video no puede estar vacío.", 400);

            var catExiste = await ctx.LearningCategory.AnyAsync(c => c.LearningCategoryId == dto.CategoriaId && c.State);
            if (!catExiste)
                throw new AbrilException("Grupo no encontrado.", 404);

            var video = new LearningVideo
            {
                LearningCategoryId = dto.CategoriaId,
                Title = titulo,
                Url = url,
                ThumbnailUrl = string.IsNullOrWhiteSpace(dto.Img) ? null : dto.Img.Trim(),
                DisplayOrder = dto.Orden,
                Active = true,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
            };

            ctx.LearningVideo.Add(video);
            await ctx.SaveChangesAsync();
            return video.LearningVideoId;
        }

        public async Task EditVideo(int id, LearningVideoEditDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var titulo = (dto.Titulo ?? string.Empty).Trim();
            var url = (dto.Url ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(titulo))
                throw new AbrilException("El título del video no puede estar vacío.", 400);
            if (string.IsNullOrWhiteSpace(url))
                throw new AbrilException("El enlace del video no puede estar vacío.", 400);

            var video = await ctx.LearningVideo.FirstOrDefaultAsync(v => v.LearningVideoId == id && v.State)
                ?? throw new AbrilException("Video no encontrado.", 404);

            video.Title = titulo;
            video.Url = url;
            video.ThumbnailUrl = string.IsNullOrWhiteSpace(dto.Img) ? null : dto.Img.Trim();
            video.DisplayOrder = dto.Orden;
            video.UpdatedDateTime = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<bool> ToggleVideo(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var video = await ctx.LearningVideo.FirstOrDefaultAsync(v => v.LearningVideoId == id && v.State)
                ?? throw new AbrilException("Video no encontrado.", 404);

            video.Active = !video.Active;
            video.UpdatedDateTime = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
            return video.Active;
        }

        public async Task DeleteVideo(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var video = await ctx.LearningVideo.FirstOrDefaultAsync(v => v.LearningVideoId == id && v.State)
                ?? throw new AbrilException("Video no encontrado.", 404);

            video.State = false;
            video.UpdatedDateTime = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Construye las filas de rol para una categoría. Si es pública interna, no se
        /// persiste ninguna (la visibilidad ignora roles); si no, se dedup­lican los IDs.
        /// </summary>
        private static List<LearningCategoryRole> BuildRoles(List<int>? roleIds, bool esPublicoInterno)
        {
            if (esPublicoInterno || roleIds == null || roleIds.Count == 0)
                return new List<LearningCategoryRole>();

            return roleIds.Distinct()
                .Select(rid => new LearningCategoryRole { RoleId = rid })
                .ToList();
        }
    }
}
