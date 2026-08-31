using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleSimpleDTO>> GetAllAsync()
        {
            return await _context.Role
                .Where(r => r.Active && r.State)
                .OrderBy(r => r.RoleDescription)
                .Select(r => new RoleSimpleDTO
                {
                    RoleId = r.RoleId,
                    RoleDescription = r.RoleDescription
                })
                .ToListAsync();
        }
    }
}
