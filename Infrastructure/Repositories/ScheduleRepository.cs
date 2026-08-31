using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using System.Linq;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Infrastructure.Repositories {
    public class ScheduleRepository {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;
        public ScheduleRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory) {
            _context = contexto;
            _factory = factory;
        }

        /*public async Task<object> GetPaged(int page)
        {
            const int pageSize = 10;

            var query = from schedule in _context.Schedule
                        join project in _context.Project on schedule.ProjectId equals project.ProjectId
                        join user in _context.User on schedule.CreatedUserId equals user.UserId
                        join person in _context.Person on user.PersonId equals person.PersonId
                        where schedule.State == true
                        orderby schedule.ScheduleId descending
                        select new ScheduleDTO
                        {
                            ScheduleId = schedule.ScheduleId,
                            ScheduleDescription = schedule.ScheduleDescription,
                            ProjectId = schedule.ProjectId,
                            ProjectDescription = project.ProjectDescription,
                            CreatedDateTime = schedule.CreatedDateTime,
                            CreatedUserId = schedule.CreatedUserId,
                            CreatedUserFullName = person.FullName,
                            UpdatedDateTime = schedule.UpdatedDateTime,
                            UpdatedUserId = schedule.UpdatedUserId,
                            Active = schedule.Active
                        };

            var totalRecords = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new
            {
                page,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                data
            };
        }

        public async Task<Schedule> Create(ScheduleCreateDTO dto, int userId)
        {
            var schedule = await _context.Schedule.FirstOrDefaultAsync(a => a.ScheduleDescription == dto.ScheduleDescription.Trim());

            if (schedule != null && schedule.State)
                throw new AbrilException("El cronograma ya existe");

            if (schedule != null && !schedule.State)
            {
                schedule.State = true;
                schedule.Active = dto.Active;
                schedule.UpdatedDateTime = DateTime.UtcNow;
                schedule.UpdatedUserId = userId;

                await _context.SaveChangesAsync();
                return schedule;
            }

            schedule = new Schedule
            {
                ScheduleDescription = dto.ScheduleDescription.Trim(),
                ProjectId = dto.ProjectId,
                Active = dto.Active,
                State = true,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId
            };

            _context.Schedule.Add(schedule);
            await _context.SaveChangesAsync();

            return schedule;
        }*/
    }
}