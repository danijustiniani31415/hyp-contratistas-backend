using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IPersonalHitoService
{
    Task<List<PersonalHitoDto>> ObtenerPorProyectoAsync(int projectId);
    Task<List<HitoCriticoDisponibleDto>> ObtenerHitosCriticosAsync(int projectId);
    Task GuardarAsync(int projectId, PersonalHitoGuardarDto dto, int userId);
}
