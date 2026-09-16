using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;

namespace Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces
{
    public interface IGuiaRemisionService
    {
        Task<GuiaRemisionDetailDto> Crear(GuiaRemisionCreateDto dto, long creadoPorId);
        Task<GuiaRemisionListResponseDto> List(string? estado, int page, int pageSize);
        Task<GuiaRemisionDetailDto> GetById(long id);

        /// <summary>Firma y transmite la guía a SUNAT (API REST + OAuth2). SUNAT responde con un
        /// ticket de forma asíncrona — este método intenta resolver el CDR con un par de reintentos
        /// cortos; si SUNAT no lo generó a tiempo, la guía queda en ENVIADA con el ticket guardado
        /// y hay que usar ConsultarEstado más tarde.</summary>
        Task<GuiaRemisionDetailDto> Enviar(long id);

        /// <summary>Reintenta resolver el CDR de una guía que quedó en ENVIADA (ticket pendiente).</summary>
        Task<GuiaRemisionDetailDto> ConsultarEstado(long id);
    }
}
