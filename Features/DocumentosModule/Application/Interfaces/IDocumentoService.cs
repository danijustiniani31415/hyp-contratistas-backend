using Abril_Backend.Features.DocumentosModule.Application.Dtos;

namespace Abril_Backend.Features.DocumentosModule.Application.Interfaces
{
    public interface IDocumentoService
    {
        /// <summary>Busca primero entre los trabajadores registrados y, si no está, consulta RENIEC. Null si no existe.</summary>
        Task<DocumentoDto?> BuscarDni(string dni);

        /// <summary>Busca primero entre transportistas y proveedores registrados y, si no está, consulta SUNAT. Null si no existe.</summary>
        Task<DocumentoDto?> BuscarRuc(string ruc);
    }
}
