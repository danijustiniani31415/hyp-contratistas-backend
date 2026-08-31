using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Infrastructure.Interfaces;

namespace Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Services
{
    public class GaMotivoSalidaService : IGaMotivoSalidaService
    {
        private readonly IGaMotivoSalidaRepository _repo;

        public GaMotivoSalidaService(IGaMotivoSalidaRepository repo)
        {
            _repo = repo;
        }

        public Task<List<GaMotivoSalidaConfigItemDto>> GetAll() => _repo.GetAll();
        public Task Create(GaMotivoSalidaCreateDto dto)        => _repo.Create(dto);
        public Task<bool> Toggle(int id)                       => _repo.Toggle(id);
        public Task Edit(int id, GaMotivoSalidaEditDto dto)    => _repo.Edit(id, dto);
    }
}
