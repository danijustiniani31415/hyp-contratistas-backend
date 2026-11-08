using Abril_Backend.Features.FacturacionElectronicaModule.Application.Dtos;

namespace Abril_Backend.Features.FacturacionElectronicaModule.Application.Interfaces
{
    public interface ICpeService
    {
        /// <summary>Crea un borrador, o reemplaza el contenido de uno existente (BORRADOR o RECHAZADO) si se manda el id.</summary>
        Task<CpeDetailDto> Guardar(long? id, CpeCreateDto dto, long usuarioId);
        Task<CpeListResponseDto> List(string? search, string? estado, string? tipo, int page, int pageSize);
        Task<CpeDetailDto> GetById(long id);
        /// <summary>Solo borradores que nunca tuvieron número.</summary>
        Task Eliminar(long id);

        /// <summary>Asigna el correlativo (con bloqueo), firma, envía a SUNAT y guarda el CDR.</summary>
        Task<CpeDetailDto> Emitir(long id, long usuarioId);
        /// <summary>Comunicación de baja de un comprobante aceptado. Queda BAJA_PENDIENTE hasta consultar el ticket.</summary>
        Task<CpeDetailDto> DarDeBaja(long id, CpeBajaDto dto, long usuarioId);
        Task<CpeDetailDto> ConsultarBaja(long id);

        Task<string> ObtenerXml(long id);
        Task<string> ObtenerCdr(long id);

        Task<List<CpeSerieDto>> ListSeries();
        Task<CpeSerieDto> GuardarSerie(CpeSerieUpsertDto dto);
    }
}
