using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Application.Interfaces;
using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Application.Services
{
    public class ProjectResidentService : IProjectResidentService
    {
        private readonly IProjectResidentRepository _repository;
        public ProjectResidentService(IProjectResidentRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<ProjectSimpleDTO>> GetProjectByResidentUserId(int userId)
        {
            var registros = await _repository.GetProjectByResidentUserId(userId);
            return registros;
        }
        public async Task<List<ProjectSimpleDTO>> GetProjectsDescription()
        {
            return await _repository.GetProjectsDescription();
        }
    }
}