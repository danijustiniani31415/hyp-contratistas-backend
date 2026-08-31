using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.OptFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.OptFeature.Application.Interfaces;

namespace Abril_Backend.Features.SsomaModule.OptFeature.Application.Services;

public class OptService : IOptService
{
    private readonly IOptRepository _repo;
    private readonly IOptSharePointService _sp;

    public OptService(IOptRepository repo, IOptSharePointService sp)
    {
        _repo = repo;
        _sp   = sp;
    }

    public Task<List<OptPetDto>> GetPetsAsync() => _repo.GetPetsAsync();

    public Task<List<OptCriterioVerificacionDto>> GetCriteriosVerificacionAsync()
        => _repo.GetCriteriosVerificacionAsync();

    public async Task<PagedResult<OptListItemDto>> GetListAsync(int? proyectoId, int? petId,
        string? tipoObservacion, DateTime? fechaDesde, DateTime? fechaHasta,
        int? trabajadorId, int page, int pageSize, int? empresaIdContratista = null,
        int? empresaObservadorId = null, int? empresaTrabajadorId = null)
    {
        var items = await _repo.GetListAsync(
            proyectoId, petId, tipoObservacion, fechaDesde, fechaHasta, trabajadorId, page, pageSize,
            empresaIdContratista, empresaObservadorId, empresaTrabajadorId);
        var total = await _repo.GetListCountAsync(
            proyectoId, petId, tipoObservacion, fechaDesde, fechaHasta, trabajadorId,
            empresaIdContratista, empresaObservadorId, empresaTrabajadorId);

        return new PagedResult<OptListItemDto>
        {
            Data         = items,
            Page         = page,
            PageSize     = pageSize,
            TotalRecords = total,
            TotalPages   = (int)Math.Ceiling((double)total / pageSize)
        };
    }

    public async Task<OptDetalleDto> GetDetalleAsync(int id)
    {
        var detalle = await _repo.GetDetalleAsync(id);
        if (detalle == null) throw new KeyNotFoundException($"OPT {id} no encontrado.");
        return detalle;
    }

    private const int MinimoFotosArea = 3;

    public async Task<int> CrearOptAsync(CrearOptRequest request, int userId = 0)
    {
        if (request.FotosAreaBase64.Count < MinimoFotosArea)
            throw new AbrilException($"Debes adjuntar al menos {MinimoFotosArea} fotos de la actividad observada.", 400);

        // 1. Crear OPT sin firmas para obtener el optId real
        var optId = await _repo.CrearOptAsync(request, null, new Dictionary<int, string>(), [], userId);

        // 2. Subir firmas usando el optId real (base64 → MemoryStream)
        string? firmaObservadorUrl = null;
        if (!string.IsNullOrEmpty(request.FirmaObservadorBase64))
        {
            var bytes = Convert.FromBase64String(request.FirmaObservadorBase64);
            using var stream = new MemoryStream(bytes);
            firmaObservadorUrl = await _sp.SubirFirmaObservadorAsync(stream, "firma_observador.png", optId);
        }

        var firmasTrabajadorUrls = new Dictionary<int, string>();
        foreach (var t in request.Trabajadores)
        {
            if (!string.IsNullOrEmpty(t.FirmaTrabajadorBase64))
            {
                var bytes = Convert.FromBase64String(t.FirmaTrabajadorBase64);
                using var stream = new MemoryStream(bytes);
                var url = await _sp.SubirFirmaTrabajadorAsync(
                    stream, $"firma_trabajador_{t.TrabajadorId}.png", optId, t.TrabajadorId);
                firmasTrabajadorUrls[t.TrabajadorId] = url;
            }
        }

        var fotosAreaUrls = new List<string>();
        for (int j = 0; j < request.FotosAreaBase64.Count; j++)
        {
            var base64 = request.FotosAreaBase64[j];
            var data = base64.Contains(",") ? base64.Split(',')[1] : base64;
            var bytes2 = Convert.FromBase64String(data);
            using var stream2 = new MemoryStream(bytes2);
            var url2 = await _sp.SubirFotoAreaAsync(stream2, $"area_{j}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg", optId, j);
            fotosAreaUrls.Add(url2);
        }

        // 3. Actualizar URLs en DB si se subió algo
        if (firmaObservadorUrl != null || firmasTrabajadorUrls.Count > 0 || fotosAreaUrls.Any())
            await _repo.UpdateFirmasAsync(optId, firmaObservadorUrl, firmasTrabajadorUrls, fotosAreaUrls);

        return optId;
    }

    public Task<OptDashboardDto> GetDashboardAsync(int? proyectoId, int? anio, int? empresaIdContratista = null)
        => _repo.GetDashboardAsync(proyectoId, anio, empresaIdContratista);
}
