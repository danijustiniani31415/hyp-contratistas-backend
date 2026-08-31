using Abril_Backend.Features.Habilitacion.Application.Dtos.Restringidos;

namespace Abril_Backend.Features.Habilitacion.Application.Interfaces
{
    public interface ITrabajadorRestringidoService
    {
        Task<bool> EstaRestringidoPorDniAsync(string? dni);
        Task<List<TrabajadorRestringidoListDto>> GetAllAsync(bool soloActivos = true, string? dni = null, bool incluirDescansoMedico = false);
        Task<TrabajadorRestringidoListDto> CreateAsync(TrabajadorRestringidoCreateDto dto, int? userId = null);
        Task DesactivarAsync(int id);
        Task DesactivarPorWorkerIdAsync(int workerId);
    }
}
