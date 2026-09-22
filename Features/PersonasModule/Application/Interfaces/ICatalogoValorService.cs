using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule.Application.Interfaces
{
    public interface ICatalogoValorService
    {
        Task<List<CatalogoValorDto>> List(string tipo);
        Task<CatalogoValorDto> Crear(CatalogoValorCreateDto dto);
        Task<CatalogoValorDto> Actualizar(int id, CatalogoValorUpdateDto dto);
    }
}
