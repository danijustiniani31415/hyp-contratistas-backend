using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

public interface IHhCargaRepository
{
    Task<SsHhCarga> CrearCargaAsync(SsHhCarga carga);
    Task<List<SsHhCargaLinea>> ObtenerLineasActivasPorProyectoAsync(int projectId);
    Task AplicarDiffCargaAsync(
        IEnumerable<SsHhCargaLinea> nuevas,
        IEnumerable<(long LineaId, decimal HorasLaboradas, decimal? CostoHhNormal, decimal? Parcial)> actualizaciones,
        IEnumerable<long> idsDarDeBaja,
        string motivoBaja);
    Task ActualizarResumenCargaAsync(int cargaId, int totalLineas, int nuevas, int actualizadas, int eliminadas);
    Task<List<HhCargaResumenDto>> ObtenerCargasPorProyectoAsync(int projectId);
    /// <summary>Suma de Horas laboradas activas del proyecto y cantidad de semanas distintas con carga.</summary>
    Task<(decimal HhTotal, int SemanasRegistradas)> ObtenerHhTotalPorProyectoAsync(int projectId);
}
