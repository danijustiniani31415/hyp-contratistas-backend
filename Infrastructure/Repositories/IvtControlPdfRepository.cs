using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Infrastructure.Repositories {
    public class IvtControlPdfRepository : IIvtControlPdfRepository {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;
        public IvtControlPdfRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory) {
            _context = contexto;
            _factory = factory;
        }

        public async Task<bool> Create(IvtControlPdfCreateDTO dto, List<string> fileUrls, int userId, List<string> fileDescriptions)
        {
            for (int i = 0; i < fileUrls.Count; i++)
            {
                var entity = new IvtControlPdf
                {
                    ProjectId = dto.ProjectId,
                    FileUrl = fileUrls[i],
                    FileDescription = fileDescriptions[i],
                    PeriodDate = dto.PeriodDate,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = userId,
                    Active = true,
                    State = true
                };

                _context.IvtControlPdf.Add(entity);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountByScheduleAndPeriod(int projectId, DateOnly periodDate)
        {
            var startDate = new DateOnly(periodDate.Year, periodDate.Month, 1);
            var endDate = startDate.AddMonths(1);

            return await _context.IvtControlPdf
                .Where(x =>
                    x.ProjectId == projectId &&
                    x.PeriodDate >= startDate &&
                    x.PeriodDate < endDate &&
                    x.Active)
                .CountAsync();
        }

        public async Task<PagedResult<IvtControlPdfGetDTO>> GetPaged(int page, DateOnly? periodDate, int? userId)
        {
            const int pageSize = 10;

            var query = _context.IvtControlPdf.Where(x => x.State);
                
            if (userId.HasValue)
            {
                query = query.Where(x => x.CreatedUserId == userId.Value);
            }

            if (periodDate.HasValue)
            {
                query = query.Where(x => x.PeriodDate == periodDate);
            }

            var totalRecords = await query.CountAsync();

            var data = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new IvtControlPdfGetDTO
                {
                    FileUrl = x.FileUrl,
                    FileDescription = x.FileDescription
                }).ToListAsync();

            return new PagedResult<IvtControlPdfGetDTO>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }

        public async Task<List<DateOnly>> GetIvtControlPeriods ()
        {
            using var ctx = _factory.CreateDbContext();
            var query = from item in ctx.IvtControlPdf
                where (item.State == true)
                select item.PeriodDate;
            return await query.Distinct().ToListAsync();
        }
    }
}