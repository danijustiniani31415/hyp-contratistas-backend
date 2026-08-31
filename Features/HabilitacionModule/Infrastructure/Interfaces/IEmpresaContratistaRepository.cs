using Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IEmpresaContratistaRepository
    {
        Task<EmpresaContratistaDetalleDto?> GetByIdAsync(int id);
        Task<(List<EmpresaContratistaListDto> Items, int Total)> GetPagedAsync(
            string? search, bool? activo, bool? soloContratistas, int page, int pageSize);
        Task<bool> ExisteRucAsync(string ruc);
        Task<int?> GetContributorIdByRucAsync(string ruc);
        Task<EmpresaContratistaListDto> CreateAsync(EmpresaContratistaCreateDto dto);
        Task UpdateAsync(int id, EmpresaContratistaUpdateDto dto);
        Task UpdatePasswordAsync(int id, string passwordHash);
        Task<List<SsEmpresaProyecto>> GetProyectosAsync(int empresaId);
        Task AddProyectoAsync(SsEmpresaProyecto ep);
        Task RemoveProyectoAsync(int empresaId, int proyectoId);
        Task<int?> GetProyectoActualDeUsuarioAsync(int userId);
    }
}
