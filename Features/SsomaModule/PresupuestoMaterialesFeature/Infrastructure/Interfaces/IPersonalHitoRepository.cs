using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

public interface IPersonalHitoRepository
{
    Task<List<PersonalHitoDto>> ObtenerPorProyectoAsync(int projectId);
    Task<List<HitoCriticoDisponibleDto>> ObtenerHitosCriticosAsync(int projectId);
    Task GuardarAsync(int projectId, List<PersonalHitoItemInputDto> items, int userId);
}
