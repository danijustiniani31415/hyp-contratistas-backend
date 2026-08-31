using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;

namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Services;

public class InspeccionService : IInspeccionService
{
    private readonly IInspeccionRepository _repo;
    private readonly IInspeccionSharePointService _sp;
    private readonly IWorkerSearchService _workerSearch;

    public InspeccionService(IInspeccionRepository repo, IInspeccionSharePointService sp, IWorkerSearchService workerSearch)
    {
        _repo = repo;
        _sp = sp;
        _workerSearch = workerSearch;
    }

    public async Task<object> GetCatalogosAsync()
    {
        var tipos = await _repo.GetTiposAsync();
        return new { tipos };
    }

    public async Task<List<InspeccionChecklistItemDto>> GetChecklistAsync(int tipoId)
        => await _repo.GetChecklistItemsAsync(tipoId);

    public async Task<object> GetListAsync(int? proyectoId, int? tipoId,
        string? estado, DateTime? fechaDesde, DateTime? fechaHasta, int page, int pageSize,
        int? empresaIdContratista = null)
    {
        var items = await _repo.GetListAsync(proyectoId, tipoId, estado, fechaDesde, fechaHasta, page, pageSize, empresaIdContratista);
        var total = await _repo.GetListCountAsync(proyectoId, tipoId, estado, fechaDesde, fechaHasta, empresaIdContratista);
        return new { items, total, page, pageSize };
    }

    public async Task<InspeccionDetalleDto> GetDetalleAsync(int id)
    {
        var result = await _repo.GetDetalleAsync(id);
        if (result == null) throw new AbrilException("Inspección no encontrada.", 404);
        return result;
    }

    public async Task<InspeccionDashboardDto> GetDashboardAsync(int? proyectoId, int? anio, int? empresaIdContratista = null)
        => await _repo.GetDashboardAsync(proyectoId, anio, empresaIdContratista);

    public async Task<int> CrearInspeccionAsync(CrearInspeccionRequest request, int? userId = null)
    {
        if (request.TipoId <= 0)
            throw new AbrilException("El tipo de inspección es requerido.", 400);

        // PASO 1: Crear inspección sin firmas para obtener el ID
        var fotosHallazgoUrls = new Dictionary<int, List<string>>();
        var id = await _repo.CrearInspeccionAsync(request, null, null, fotosHallazgoUrls, [], userId);

        // PASO 2: Subir firmas y fotos con el ID real
        string? firmaInspectorUrl = null;
        if (!string.IsNullOrEmpty(request.FirmaInspectorBase64))
        {
            var bytes = Convert.FromBase64String(
                request.FirmaInspectorBase64.Contains(",")
                    ? request.FirmaInspectorBase64.Split(',')[1]
                    : request.FirmaInspectorBase64);
            using var stream = new MemoryStream(bytes);
            firmaInspectorUrl = await _sp.SubirFirmaInspectorAsync(
                stream, $"inspector_{DateTime.UtcNow:yyyyMMddHHmmss}.png", id);
        }

        string? firmaRepresentanteUrl = null;
        if (!string.IsNullOrEmpty(request.FirmaRepresentanteBase64))
        {
            var bytes = Convert.FromBase64String(
                request.FirmaRepresentanteBase64.Contains(",")
                    ? request.FirmaRepresentanteBase64.Split(',')[1]
                    : request.FirmaRepresentanteBase64);
            using var stream = new MemoryStream(bytes);
            firmaRepresentanteUrl = await _sp.SubirFirmaRepresentanteAsync(
                stream, $"representante_{DateTime.UtcNow:yyyyMMddHHmmss}.png", id);
        }

        for (int i = 0; i < request.Hallazgos.Count; i++)
        {
            var urls = new List<string>();
            for (int j = 0; j < request.Hallazgos[i].FotosBase64.Count; j++)
            {
                var base64 = request.Hallazgos[i].FotosBase64[j];
                var data = base64.Contains(",") ? base64.Split(',')[1] : base64;
                var bytes = Convert.FromBase64String(data);
                using var stream = new MemoryStream(bytes);
                var url = await _sp.SubirFotoHallazgoAsync(
                    stream, $"foto_{i}_{j}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg", id, i);
                urls.Add(url);
            }
            if (urls.Any()) fotosHallazgoUrls[i] = urls;
        }

        var fotosAreaUrls = new List<string>();
        for (int j = 0; j < request.FotosAreaBase64.Count; j++)
        {
            var base64 = request.FotosAreaBase64[j];
            var data = base64.Contains(",") ? base64.Split(',')[1] : base64;
            var bytes = Convert.FromBase64String(data);
            using var stream = new MemoryStream(bytes);
            var url = await _sp.SubirFotoAreaAsync(
                stream, $"area_{j}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg", id, j);
            fotosAreaUrls.Add(url);
        }

        // PASO 3: Actualizar con firmas y fotos
        if (firmaInspectorUrl != null || firmaRepresentanteUrl != null || fotosHallazgoUrls.Any() || fotosAreaUrls.Any())
            await _repo.ActualizarFirmasYFotosAsync(id, firmaInspectorUrl, firmaRepresentanteUrl, fotosHallazgoUrls, fotosAreaUrls);

        return id;
    }

    public Task<List<HallazgoListItemDto>> GetHallazgosAsync(string? estado, string? proyecto, string? area, DateTime? fechaLimiteHasta, int? empresaIdContratista = null)
        => _repo.GetHallazgosAsync(estado, proyecto, area, fechaLimiteHasta, empresaIdContratista);

    public Task<(int? EmpresaId, int? EmpresaInspectoraId)> GetEmpresaIdDeHallazgoAsync(int hallazgoId) => _repo.GetEmpresaIdDeHallazgoAsync(hallazgoId);

    public async Task LevantarHallazgoAsync(int hallazgoId, LevantarHallazgoDto dto)
    {
        if (string.IsNullOrEmpty(dto.Estado))
            throw new AbrilException("El estado es requerido.", 400);
        if (dto.Estado != "En proceso" && dto.Estado != "Cerrado")
            throw new AbrilException("Estado inválido. Use 'En proceso' o 'Cerrado'.", 400);
        await _repo.LevantarHallazgoAsync(hallazgoId, dto);
    }

    public async Task AgregarHallazgoAsync(int inspeccionId, InspeccionHallazgoRequest hallazgo, int? userId, bool esContratista)
    {
        if (string.IsNullOrWhiteSpace(hallazgo.Descripcion))
            throw new AbrilException("La descripción del hallazgo es requerida.", 400);

        var worker = userId.HasValue ? await _workerSearch.GetByUserId(userId.Value, esContratista) : null;
        var hallazgoId = await _repo.AgregarHallazgoAsync(inspeccionId, hallazgo, worker?.Id, worker?.ApellidoNombre);

        var urls = new List<string>();
        for (int j = 0; j < hallazgo.FotosBase64.Count; j++)
        {
            var base64 = hallazgo.FotosBase64[j];
            var data = base64.Contains(",") ? base64.Split(',')[1] : base64;
            var bytes = Convert.FromBase64String(data);
            using var stream = new MemoryStream(bytes);
            var url = await _sp.SubirFotoHallazgoAsync(stream, $"foto_{j}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg", inspeccionId, hallazgoId);
            urls.Add(url);
        }
        if (urls.Count > 0) await _repo.AgregarFotosHallazgoAsync(hallazgoId, urls);
    }

    public async Task UnirseAsync(int inspeccionId, int? userId, bool esContratista)
    {
        var worker = userId.HasValue ? await _workerSearch.GetByUserId(userId.Value, esContratista) : null;
        if (worker == null)
            throw new AbrilException("Tu usuario no está vinculado a una ficha de trabajador.", 400);

        var req = new UnirseInspeccionRequest
        {
            Nombre = worker.ApellidoNombre ?? "",
            Cargo = worker.Cargo ?? worker.Puesto,
            Empresa = worker.EmpresaActual,
        };
        await _repo.UnirseAsync(inspeccionId, req, worker.Id);
    }

    public Task<List<InspeccionAbiertaListItemDto>> GetAbiertasAsync(int? proyectoId)
        => _repo.GetAbiertasAsync(proyectoId);

    public Task<int> GetProyectoIdAsync(int inspeccionId) => _repo.GetProyectoIdAsync(inspeccionId);

    public Task CerrarInspeccionColaborativaAsync(int inspeccionId, int? userId) => _repo.CerrarInspeccionColaborativaAsync(inspeccionId, userId);

    public Task<InspeccionDestinatariosCierreDto> GetDestinatariosCierreColaborativaAsync(int inspeccionId, int? userId) => _repo.GetDestinatariosCierreColaborativaAsync(inspeccionId, userId);

    public Task ReabrirInspeccionColaborativaAsync(int inspeccionId) => _repo.ReabrirInspeccionColaborativaAsync(inspeccionId);

    public async Task CerrarHallazgoAsync(int hallazgoId, CerrarHallazgoRequest request)
    {
        string? evidenciaUrl = null;
        if (!string.IsNullOrEmpty(request.EvidenciaCierreBase64))
        {
            var data = request.EvidenciaCierreBase64.Contains(",")
                ? request.EvidenciaCierreBase64.Split(',')[1]
                : request.EvidenciaCierreBase64;
            var bytes = Convert.FromBase64String(data);
            using var stream = new MemoryStream(bytes);
            evidenciaUrl = await _sp.SubirFotoHallazgoAsync(
                stream, $"evidencia_{hallazgoId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg",
                0, hallazgoId);
        }
        await _repo.CerrarHallazgoAsync(hallazgoId, request, evidenciaUrl);
    }

    public async Task EditarHallazgoAsync(int hallazgoId, EditarHallazgoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            throw new AbrilException("La descripción del hallazgo es requerida.", 400);
        await _repo.EditarHallazgoAsync(hallazgoId, request);
    }

    public async Task EliminarHallazgoAsync(int hallazgoId)
        => await _repo.EliminarHallazgoAsync(hallazgoId);
}
