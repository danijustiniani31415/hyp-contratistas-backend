using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class PersonalHitoService : IPersonalHitoService
{
    private readonly IPersonalHitoRepository _repo;
    public PersonalHitoService(IPersonalHitoRepository repo) => _repo = repo;

    public Task<List<PersonalHitoDto>> ObtenerPorProyectoAsync(int projectId)
        => _repo.ObtenerPorProyectoAsync(projectId);

    public Task<List<HitoCriticoDisponibleDto>> ObtenerHitosCriticosAsync(int projectId)
        => _repo.ObtenerHitosCriticosAsync(projectId);

    public Task GuardarAsync(int projectId, PersonalHitoGuardarDto dto, int userId)
        => _repo.GuardarAsync(projectId, dto.Items, userId);
}
