using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IEstandarizacionService
{
    Task<EstandarizacionLoteResultDto> EstandarizarCargaAsync(int cargaId);
    /// <summary>Progreso en vivo de un EstandarizarCargaAsync en curso para esta carga — null si no hay ninguno corriendo ahora mismo.</summary>
    (int Procesadas, int Total)? ObtenerProgreso(int cargaId);
}
