using Abril_Backend.Features.HerramientasModule.Application.Dtos;

namespace Abril_Backend.Features.HerramientasModule.Application.Interfaces
{
    public interface IPrestamoService
    {
        Task<PrestamoDetailDto> Crear(PrestamoCreateDto dto, long prestadoPorId);
        Task<PrestamoListResponseDto> List(bool soloAbiertos, int page, int pageSize);
        Task<PrestamoDetailDto> GetById(long id);

        /// <summary>Devuelve, marca perdido o dañado UN ítem del préstamo — cada uno con su propia fecha.</summary>
        Task<PrestamoDetailDto> DevolverItem(long prestamoId, long itemId, DevolverItemDto dto, long devueltoPorId);
    }
}
