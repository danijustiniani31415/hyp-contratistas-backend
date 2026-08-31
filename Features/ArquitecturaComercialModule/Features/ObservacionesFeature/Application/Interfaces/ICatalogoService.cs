using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;

public interface ICatalogoService
{
    Task<List<CatalogoItemDTO>> GetByTipo(string tipo, bool soloActivos);
    Task<CatalogoItemDTO> Create(string tipo, CreateCatalogoItemDTO body);
    Task<CatalogoItemDTO?> Update(int id, UpdateCatalogoItemDTO body);
    Task<bool> Delete(int id);
}
