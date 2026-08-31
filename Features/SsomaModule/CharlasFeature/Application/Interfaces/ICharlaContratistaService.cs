using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Interfaces;

public interface ICharlaContratistaService
{
    Task<List<CharlaContratistaPendienteDto>> GetPendientesAsync(int empresaId, DateOnly fecha);

    /// <summary>Días anteriores a hoy en que la empresa fue tareada y no subió su charla.</summary>
    Task<List<CharlaContratistaPendienteDto>> GetDiasFaltantesAsync(int empresaId);
    Task<List<CharlaContratistaDto>> GetHistorialAsync(int empresaId, int page, int pageSize);
    Task<CharlaContratistaDto> SubirAsync(int empresaId, CharlaContratistaUploadRequest req, int userId);

    /// <summary>Para SSOMA/admin: incumplimientos de una fecha (tareados que no subieron charla).</summary>
    Task<List<CharlaContratistaPendienteDto>> GetIncumplimientosAsync(DateOnly fecha, int? proyectoId);

    /// <summary>Para SSOMA/prevencionista: charlas de contratistas pendientes (o filtradas por estado) para revisar.</summary>
    Task<CharlaContratistaRevisionResultDto> GetRevisionAsync(string? estado, int? proyectoId, int page, int pageSize);
    Task<CharlaContratistaDto> AprobarAsync(int id, int userId);
    Task<CharlaContratistaDto> RechazarAsync(int id, string motivo, int userId);
}
