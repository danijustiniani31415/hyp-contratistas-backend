using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Services
{
    public class ProyectoHabilitadoService : IProyectoHabilitadoService
    {
        private readonly IProyectoHabilitadoRepository _repository;

        public ProyectoHabilitadoService(IProyectoHabilitadoRepository repository)
        {
            _repository = repository;
        }

        public Task<List<ProyectoHabilitadoListDto>> GetTodosAsync() => _repository.GetTodosAsync();

        public Task<List<ProyectoSsomaSimpleDto>> GetHabilitadosAsync() => _repository.GetHabilitadosAsync();

        public Task SetHabilitadoAsync(int proyectoId, bool habilitado, int userId) =>
            _repository.SetHabilitadoAsync(proyectoId, habilitado, userId);
    }
}
