using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Interfaces;

public interface ICatalogoRepository
{
    Task<List<AcCatalogoItem>> GetByTipo(string tipo, bool soloActivos);
    Task<AcCatalogoItem> Create(string tipo, string nombre);
    Task<AcCatalogoItem?> Update(int id, string nombre, bool activo);
    Task<bool> Delete(int id);
}
