using Abril_Backend.Features.AlmacenModule.Application.Dtos;

namespace Abril_Backend.Features.AlmacenModule.Application.Interfaces
{
    public interface IAlmacenCargaMasivaService
    {
        /// <summary>
        /// Registra como INGRESO una lista de productos en un almacén (p. ej. desde un Excel), creando en el
        /// catálogo los que no existan. Cada fila se procesa por separado: las que fallan se informan y no
        /// impiden las demás.
        /// </summary>
        Task<CargaMasivaResultadoDto> Cargar(CargaMasivaDto dto, long? usuarioSistemaId);
    }
}
