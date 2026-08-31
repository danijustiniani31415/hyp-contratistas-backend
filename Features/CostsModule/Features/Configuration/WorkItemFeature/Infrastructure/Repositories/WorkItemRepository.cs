using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Infrastructure.Repositories
{
    public class WorkItemRepository : IWorkItemRepository
    {
        private readonly AppDbContext _context;

        public WorkItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<WorkItemDto>> GetPaged(WorkItemFilterDto filter)
        {
            const int pageSize = 10;

            var query = _context.WorkItem.Where(x => x.State);

            // Búsqueda por palabras en cualquier orden, insensible a mayúsculas y tildes
            // (alineado con app-search-input del front: "instalacion papel" coincide con
            // "INSTALACIÓN DE PAPEL MURAL").
            if (!string.IsNullOrWhiteSpace(filter.Description))
            {
                foreach (var word in filter.Description.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var pattern = $"%{word}%";
                    query = query.Where(x => EF.Functions.ILike(
                        AppDbContext.Unaccent(x.WorkItemDescription), AppDbContext.Unaccent(pattern)));
                }
            }

            if (filter.HasValorizationForm.HasValue)
            {
                if (filter.HasValorizationForm.Value)
                    query = query.Where(x => _context.WorkItemValorizationForm
                        .Any(f => f.WorkItemId == x.WorkItemId && f.State));
                else
                    query = query.Where(x => !_context.WorkItemValorizationForm
                        .Any(f => f.WorkItemId == x.WorkItemId && f.State));
            }

            if (filter.WorkItemCategoryId.HasValue)
                query = query.Where(x => x.WorkItemCategoryId == filter.WorkItemCategoryId.Value);

            if (filter.Active.HasValue)
                query = query.Where(x => x.Active == filter.Active.Value);

            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.WorkItemId)
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new WorkItemDto
                {
                    WorkItemId = x.WorkItemId,
                    WorkItemDescription = x.WorkItemDescription,
                    WorkItemCategoryId = x.WorkItemCategoryId,
                    WorkItemCategoryDescription = _context.WorkItemCategory
                        .Where(c => c.WorkItemCategoryId == x.WorkItemCategoryId)
                        .Select(c => c.WorkItemCategoryDescription)
                        .FirstOrDefault(),
                    WorkSpecialtyDescription = _context.WorkItemCategory
                        .Where(c => c.WorkItemCategoryId == x.WorkItemCategoryId)
                        .Join(_context.WorkSpecialty,
                            c => c.WorkSpecialtyId,
                            s => (int?)s.WorkSpecialtyId,
                            (c, s) => s.WorkSpecialtyDescription)
                        .FirstOrDefault(),
                    // Mismo criterio que el filtro "Instructivo" de partidas de control:
                    // tiene instructivo = carpeta asociada (sync automático o manual).
                    CategoryHasInstructivo = _context.WorkItemCategory
                        .Where(c => c.WorkItemCategoryId == x.WorkItemCategoryId)
                        .Select(c => (bool?)(c.InstructivosFolderId != null))
                        .FirstOrDefault(),
                    CreatedDateTime = x.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    CreatedUserId = x.CreatedUserId,
                    UpdatedDateTime = x.UpdatedDateTime.HasValue
                        ? x.UpdatedDateTime.Value.ToOffset(TimeSpan.FromHours(-5)).DateTime
                        : null,
                    UpdatedUserId = x.UpdatedUserId,
                    Active = x.Active,
                    ValorizationForms = _context.WorkItemValorizationForm
                        .Where(f => f.WorkItemId == x.WorkItemId && f.State)
                        .OrderBy(f => f.SortOrder)
                        .Select(f => new WorkItemValorizationFormDto
                        {
                            WorkItemValorizationFormId = f.WorkItemValorizationFormId,
                            Concept    = f.Concept,
                            Percentage = f.Percentage,
                            SortOrder  = f.SortOrder,
                        })
                        .ToList()
                })
                .ToListAsync();

            return new PagedResult<WorkItemDto>
            {
                Page = filter.Page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }

        public async Task Create(WorkItemCreateDto dto, int userId)
        {
            var exists = await _context.WorkItem
                .AnyAsync(x => x.WorkItemDescription.ToLower() == dto.WorkItemDescription.Trim().ToLower()
                             && x.State);

            if (exists)
                throw new AbrilException("Ya existe una partida con esa descripción.");

            await ValidateCategory(dto.WorkItemCategoryId);

            var record = new WorkItem
            {
                WorkItemDescription = dto.WorkItemDescription.Trim(),
                WorkItemCategoryId = dto.WorkItemCategoryId,
                Active = true,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId
            };

            _context.WorkItem.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task Update(WorkItemEditDto dto, int userId)
        {
            var record = await _context.WorkItem
                .FirstOrDefaultAsync(x => x.WorkItemId == dto.WorkItemId && x.State);

            if (record == null)
                throw new AbrilException("La partida no existe.");

            var duplicate = await _context.WorkItem
                .AnyAsync(x => x.WorkItemDescription.ToLower() == dto.WorkItemDescription.Trim().ToLower()
                             && x.WorkItemId != dto.WorkItemId
                             && x.State);

            if (duplicate)
                throw new AbrilException("Ya existe una partida con esa descripción.");

            await ValidateCategory(dto.WorkItemCategoryId);

            record.WorkItemDescription = dto.WorkItemDescription.Trim();
            record.WorkItemCategoryId = dto.WorkItemCategoryId;
            record.Active = dto.Active;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            // ── Formas de valorización (cláusula 5.1): upsert completo + soft-delete ──
            var now = DateTimeOffset.UtcNow;
            var incomingIds = dto.ValorizationForms
                .Where(f => f.WorkItemValorizationFormId.HasValue)
                .Select(f => f.WorkItemValorizationFormId!.Value)
                .ToHashSet();

            var toDelete = await _context.WorkItemValorizationForm
                .Where(f => f.WorkItemId == dto.WorkItemId
                         && f.State
                         && !incomingIds.Contains(f.WorkItemValorizationFormId))
                .ToListAsync();
            foreach (var f in toDelete)
            {
                f.State           = false;
                f.UpdatedDatetime = now;
                f.UpdatedUserId   = userId;
            }

            foreach (var formDto in dto.ValorizationForms)
            {
                if (formDto.WorkItemValorizationFormId.HasValue)
                {
                    var existing = await _context.WorkItemValorizationForm
                        .FirstOrDefaultAsync(f => f.WorkItemValorizationFormId == formDto.WorkItemValorizationFormId.Value);
                    if (existing is not null)
                    {
                        existing.Concept         = formDto.Concept.Trim();
                        existing.Percentage      = formDto.Percentage;
                        existing.SortOrder       = formDto.SortOrder;
                        existing.UpdatedDatetime = now;
                        existing.UpdatedUserId   = userId;
                    }
                }
                else
                {
                    _context.WorkItemValorizationForm.Add(new WorkItemValorizationForm
                    {
                        WorkItemId      = dto.WorkItemId,
                        Concept         = formDto.Concept.Trim(),
                        Percentage      = formDto.Percentage,
                        SortOrder       = formDto.SortOrder,
                        State           = true,
                        CreatedDatetime = now,
                        CreatedUserId   = userId,
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> Delete(int workItemId, int userId)
        {
            var record = await _context.WorkItem
                .FirstOrDefaultAsync(x => x.WorkItemId == workItemId && x.State);

            if (record == null)
                return false;

            record.State = false;
            record.Active = false;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WorkItemCategoryOptionDto>> GetActiveCategories()
            => await _context.WorkItemCategory
                .Where(c => c.State && c.Active)
                .OrderBy(c => c.WorkItemCategoryDescription)
                .Select(c => new WorkItemCategoryOptionDto
                {
                    WorkItemCategoryId = c.WorkItemCategoryId,
                    WorkItemCategoryDescription = c.WorkItemCategoryDescription
                })
                .ToListAsync();

        // La partida de control es obligatoria y debe existir y estar activa.
        private async Task ValidateCategory(int? workItemCategoryId)
        {
            if (!workItemCategoryId.HasValue)
                throw new AbrilException("Debe seleccionar una partida de control.");

            var ok = await _context.WorkItemCategory
                .AnyAsync(c => c.WorkItemCategoryId == workItemCategoryId.Value && c.State && c.Active);
            if (!ok)
                throw new AbrilException("La partida de control seleccionada no existe.");
        }

        public async Task<List<AdjudicacionFolderRootDto>> GetActiveAdjudicacionFolderRoots()
        {
            return await (
                from f in _context.ProjectAdjudicacionFolder
                join p in _context.Project on f.ProjectId equals p.ProjectId
                where f.State && f.Active
                select new AdjudicacionFolderRootDto
                {
                    ProjectId = f.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    DriveId = f.DriveId,
                    FolderId = f.FolderId
                })
                .ToListAsync();
        }

        public async Task<List<ExistingWorkItemDto>> GetActivePartidas()
        {
            // Solo activas: el índice único parcial (WHERE state) permite N soft-deleted con el mismo nombre.
            return await _context.WorkItem
                .Where(x => x.State)
                .Select(x => new ExistingWorkItemDto
                {
                    WorkItemId = x.WorkItemId,
                    WorkItemDescription = x.WorkItemDescription
                })
                .ToListAsync();
        }

        public async Task<List<string>> BulkCreate(IEnumerable<string> descriptions, int userId)
        {
            var now = DateTimeOffset.UtcNow;
            var created = new List<string>();

            // Inserción defensiva: cada partida en su propio SaveChanges; si choca con el índice
            // único (duplicado que se haya colado), se descarta esa fila y se continúa con el resto.
            foreach (var description in descriptions)
            {
                var record = new WorkItem
                {
                    WorkItemDescription = description.Trim(),
                    Active = true,
                    State = true,
                    CreatedDateTime = now,
                    CreatedUserId = userId
                };

                _context.WorkItem.Add(record);
                try
                {
                    await _context.SaveChangesAsync();
                    created.Add(record.WorkItemDescription);
                }
                catch (DbUpdateException)
                {
                    // Quitar la entidad fallida del tracker para no arrastrar el error a la siguiente.
                    _context.Entry(record).State = EntityState.Detached;
                }
            }

            return created;
        }
    }
}
