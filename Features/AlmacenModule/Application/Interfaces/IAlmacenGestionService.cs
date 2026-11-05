using Abril_Backend.Features.AlmacenModule.Application.Dtos;

namespace Abril_Backend.Features.AlmacenModule.Application.Interfaces
{
    public interface IAlmacenGestionService
    {
        Task<List<AlmacenGestionDto>> Listar();

        /// <summary>Actualiza dirección, ubigeo y código de establecimiento SUNAT (datos de la GRE).</summary>
        Task<AlmacenGestionDto> Actualizar(int id, AlmacenActualizarDto dto);
    }
}
