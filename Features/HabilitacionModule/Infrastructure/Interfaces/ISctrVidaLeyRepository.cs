using Abril_Backend.Features.Habilitacion.Application.Dtos.SctrVidaley;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface ISctrVidaLeyRepository
    {
        Task<(List<SctrVidaLeyDto> Items, int Total)> GetPagedAsync(
            int? empresaId, int? proyectoId, string? tipo,
            int? mes, int? anio, string? estado, int page, int pageSize);

        Task<SctrVidaLeyDto?> GetByIdAsync(int id);
        Task<SctrVidaLeyDto> CreateAsync(SctrVidaLeyCreateDto dto, int empresaId);
        Task<SctrVidaLeyDto> UpdateAsync(int id, SctrVidaLeyCreateDto dto, int empresaId);
        Task<SctrVidaLeyDto> AprobarAsync(int id, SctrVidaLeyAprobarDto dto, int userId);
        Task<List<SctrVidaLeyDto>> GetPorTrabajadorAsync(int workerId);
        Task<List<SctrVidaLeyDto>> GetProximosVencerAsync(int dias);
        Task<List<SctrTrabajadorEstadoDto>> GetTrabajadoresPorEmpresaAsync(
            int? empresaId, int? proyectoId, string? tipo, string? tipoPoliza,
            string? estadoSctr, string? estadoVidaLey, string? obraOficina = null,
            int? areaScopeId = null);

        Task RecalcularEstadoPolizasAsync();
    }
}
