using Abril_Backend.Features.ComprasModule.Application.Dtos;

namespace Abril_Backend.Features.ComprasModule.Application.Interfaces
{
    /// <summary>Lee un comprobante (PDF o foto) con un modelo de IA. El proveedor de IA es intercambiable.</summary>
    public interface IComprobanteLectorService
    {
        /// <summary>Nombre del modelo rápido/barato y del preciso, en ese orden.</summary>
        (string Rapido, string Preciso) Modelos { get; }

        Task<ComprobanteLeidoDto> Leer(byte[] contenido, string mimeType, string modelo);
    }

    public interface IComprobanteCompraService
    {
        Task<ComprobanteLecturaResultDto> Leer(Stream archivo, string nombreArchivo, string mimeType, long usuarioId);
        Task<ComprobanteGuardadoDto> Guardar(ComprobanteGuardarDto dto, long usuarioId, Abril_Backend.Features.PersonasModule.LbScopeProyectos scope);
    }
}
