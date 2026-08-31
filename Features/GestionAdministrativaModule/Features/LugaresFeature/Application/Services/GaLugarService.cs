using Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Infrastructure.Interfaces;

namespace Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Services
{
    public class GaLugarService : IGaLugarService
    {
        private readonly IGaLugarRepository _repo;
        public GaLugarService(IGaLugarRepository repo) => _repo = repo;

        public Task<List<GaLugarConfigItemDto>>  GetAll()                              => _repo.GetAll();
        public Task                              CreateBatch(GaLugarCreateBatchDto dto) => _repo.CreateBatch(dto);
        public Task<bool>                        ToggleActivo(int id)                  => _repo.ToggleActivo(id);
        public Task<ToggleProyectoResultDto>     ToggleProyecto(int projectId)         => _repo.ToggleProyecto(projectId);
        public Task                              Edit(int id, GaLugarEditDto dto)       => _repo.Edit(id, dto);
    }
}
