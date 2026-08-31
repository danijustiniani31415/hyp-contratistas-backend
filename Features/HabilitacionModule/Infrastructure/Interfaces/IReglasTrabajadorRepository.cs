using Abril_Backend.Features.Habilitacion.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IReglasTrabajadorRepository
    {
        Task<List<SsItemTrabajadorRegla>> GetAllAsync();
        Task<SsItemTrabajadorRegla> CreateAsync(SsItemTrabajadorRegla regla);
        Task<SsItemTrabajadorRegla> UpdateAsync(SsItemTrabajadorRegla regla);
        Task DeleteAsync(int id);
    }
}
