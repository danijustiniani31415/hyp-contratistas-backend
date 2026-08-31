using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<List<RoleSimpleDTO>> GetAllAsync()
        {
            return await _roleRepository.GetAllAsync();
        }
    }
}
