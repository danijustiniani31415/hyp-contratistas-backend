using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IRevisionMaterialesService
{
    Task<List<MaterialPendienteDto>> ObtenerPendientesAsync(int projectId);
    Task<RevisionResultDto> ProcesarRevisionAsync(RevisionLoteDto dto, int usuarioId);
    Task<List<BuscarItemDto>> BuscarItemsAsync(string texto);
    Task<List<MaterialPendienteGlobalDto>> ObtenerPendientesGlobalAsync();
    Task<RevisionResultDto> ProcesarRevisionGlobalAsync(List<RevisionDecisionDto> decisiones, int usuarioId);
    Task<List<MaterialNoSsomaDto>> ObtenerNoSsomaAsync();
}
