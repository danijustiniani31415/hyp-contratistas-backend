using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvPlantillaRepository
    {
        Task<List<EvPlantilla>> GetByAreaAsync(string area);
        Task<List<EvPlantilla>> GetAllActivasAsync();
        Task<List<string>> GetAreasAsync();
        Task<EvPlantilla?> GetByIdAsync(int id);
        Task<EvPlantilla> CreateAsync(EvPlantilla plantilla);
        Task UpdateAsync(EvPlantilla plantilla);
    }
}
