using Abril_Backend.Features.Ssoma.Rac.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.Ssoma.Rac.Services;

public interface IRacService
{
    Task<RacPagedResult<RacListItemDto>> GetListAsync(RacListQuery q);
    Task<RacDetalleDto?> GetDetalleAsync(int id);
    Task<RacCreadoDto> CrearAsync(RacCreateRequest req, int userId);
    Task<RacDetalleDto> CerrarAsync(int id, RacCerrarRequest req, int userId);
    Task<RacFotoUploadResult> SubirFotoAsync(int racId, IFormFile file, string tipo, int userId);
    Task<byte[]> GetPdfAsync(int id);
    Task<byte[]?> GetFotoBytesAsync(int fotoId);
    Task<RacDashboardDto> GetDashboardAsync(int? empresaIdContratista = null);
    Task<List<RacCategoriaDto>> GetCategoriasAsync();
    Task<List<RacInfraccionDto>> GetInfraccionesAsync();
    Task<List<string>> GetNivelesProyectoAsync(int projectId);
}
