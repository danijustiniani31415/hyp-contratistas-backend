using Abril_Backend.Features.Habilitacion.Application.Dtos.Bandeja;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Proyectos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Shared.DTOs;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IBandejaRepository
    {
        Task<(List<BandejaItemDto> Items, int Total)> GetPendientesAsync(
            string? tipo, int? proyectoId, int? empresaId,
            string? responsable, string? search, int page, int pageSize,
            int? areaScopeId = null);

        Task<CursorPagedResult<BandejaItemDto>> GetPendientesCursorAsync(
            string? tipo, int? proyectoId, int? empresaId,
            string? responsable, string? search, string? cursor, int pageSize,
            int? areaScopeId = null);

        Task<List<string>> GetEmpresasUnicasAsync();
        Task<List<ProyectoSimpleDto>> GetProyectosUnicosAsync();
        Task<SsHabTrabajador?> AprobarTrabajadorAsync(int id, BandejaAprobarDto dto, int userId);
        Task<SsHabEmpresa?> AprobarEmpresaAsync(int id, BandejaAprobarDto dto, int userId);
        Task<SsHabEquipo?> AprobarEquipoAsync(int id, BandejaAprobarDto dto, int userId);
    }
}
