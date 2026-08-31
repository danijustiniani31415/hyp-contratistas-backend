using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Interfaces;

public interface IInspeccionService
{
    Task<object> GetCatalogosAsync();
    Task<List<InspeccionChecklistItemDto>> GetChecklistAsync(int tipoId);
    Task<object> GetListAsync(int? proyectoId, int? tipoId,
        string? estado, DateTime? fechaDesde, DateTime? fechaHasta, int page, int pageSize,
        int? empresaIdContratista = null);
    Task<InspeccionDetalleDto> GetDetalleAsync(int id);
    Task<int> CrearInspeccionAsync(CrearInspeccionRequest request, int? userId = null);
    Task CerrarHallazgoAsync(int hallazgoId, CerrarHallazgoRequest request);
    Task EditarHallazgoAsync(int hallazgoId, EditarHallazgoRequest request);
    Task EliminarHallazgoAsync(int hallazgoId);
    Task<InspeccionDashboardDto> GetDashboardAsync(int? proyectoId, int? anio, int? empresaIdContratista = null);
    Task<List<HallazgoListItemDto>> GetHallazgosAsync(string? estado, string? proyecto, string? area, DateTime? fechaLimiteHasta, int? empresaIdContratista = null);
    Task LevantarHallazgoAsync(int hallazgoId, LevantarHallazgoDto dto);
    Task<(int? EmpresaId, int? EmpresaInspectoraId)> GetEmpresaIdDeHallazgoAsync(int hallazgoId);

    Task AgregarHallazgoAsync(int inspeccionId, InspeccionHallazgoRequest hallazgo, int? userId, bool esContratista);
    Task UnirseAsync(int inspeccionId, int? userId, bool esContratista);
    Task<List<InspeccionAbiertaListItemDto>> GetAbiertasAsync(int? proyectoId);
    Task<int> GetProyectoIdAsync(int inspeccionId);
    Task CerrarInspeccionColaborativaAsync(int inspeccionId, int? userId);
    Task<InspeccionDestinatariosCierreDto> GetDestinatariosCierreColaborativaAsync(int inspeccionId, int? userId);
    Task ReabrirInspeccionColaborativaAsync(int inspeccionId);
}
