using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Interfaces;

public interface IInspeccionRepository
{
    Task<List<InspeccionTipoDto>> GetTiposAsync();
    Task<List<InspeccionChecklistItemDto>> GetChecklistItemsAsync(int tipoId);
    Task<List<InspeccionListItemDto>> GetListAsync(int? proyectoId, int? tipoId,
        string? estado, DateTime? fechaDesde, DateTime? fechaHasta, int page, int pageSize,
        int? empresaIdContratista = null);
    Task<int> GetListCountAsync(int? proyectoId, int? tipoId,
        string? estado, DateTime? fechaDesde, DateTime? fechaHasta, int? empresaIdContratista = null);
    Task<InspeccionDetalleDto?> GetDetalleAsync(int id);
    Task<int> CrearInspeccionAsync(CrearInspeccionRequest request,
        string? firmaInspectorUrl, string? firmaRepresentanteUrl,
        Dictionary<int, List<string>> fotosHallazgoUrls, List<string> fotosAreaUrls, int? userId = null);
    Task CerrarHallazgoAsync(int hallazgoId, CerrarHallazgoRequest request, string? evidenciaUrl);
    Task EditarHallazgoAsync(int hallazgoId, EditarHallazgoRequest request);
    Task EliminarHallazgoAsync(int hallazgoId);
    Task ActualizarFirmasYFotosAsync(int id, string? firmaInspectorUrl, string? firmaRepresentanteUrl,
        Dictionary<int, List<string>> fotosHallazgoUrls, List<string> fotosAreaUrls);
    Task<InspeccionDashboardDto> GetDashboardAsync(int? proyectoId, int? anio, int? empresaIdContratista = null);
    Task<List<HallazgoListItemDto>> GetHallazgosAsync(string? estado, string? proyecto, string? area, DateTime? fechaLimiteHasta, int? empresaIdContratista = null);
    Task LevantarHallazgoAsync(int hallazgoId, LevantarHallazgoDto dto);
    Task<(int? EmpresaId, int? EmpresaInspectoraId)> GetEmpresaIdDeHallazgoAsync(int hallazgoId);

    Task<int> AgregarHallazgoAsync(int inspeccionId, InspeccionHallazgoRequest h, int? creadoPorWorkerId, string? creadoPorNombre);
    Task AgregarFotosHallazgoAsync(int hallazgoId, List<string> urls);
    Task UnirseAsync(int inspeccionId, UnirseInspeccionRequest req, int? workerId);
    Task<List<InspeccionAbiertaListItemDto>> GetAbiertasAsync(int? proyectoId);
    Task<int> GetProyectoIdAsync(int inspeccionId);
    Task CerrarInspeccionColaborativaAsync(int inspeccionId, int? userId);
    Task<InspeccionDestinatariosCierreDto> GetDestinatariosCierreColaborativaAsync(int inspeccionId, int? userId);
    Task ReabrirInspeccionColaborativaAsync(int inspeccionId);
}
