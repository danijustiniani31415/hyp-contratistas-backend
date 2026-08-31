namespace Abril_Backend.Features.Habilitacion.Application.Interfaces
{
    public interface ISharePointHabService
    {
        Task<string?> GetDownloadUrlAsync(string archivoUrl, string? libraryContexto = null);
        Task<string> SubirArchivoAsync(Stream fileStream, string fileName, string contexto);
        Task<string> SubirArchivoEnRutaAsync(Stream fileStream, string fileName, string libraryContexto, string carpetaPath);
        Task<string> SubirArchivoYObtenerUrlAsync(Stream fileStream, string fileName, string libraryContexto, string carpetaPath);
        Task<byte[]?> DescargarContenidoAsync(string path, string libraryContexto);

        /// <summary>
        /// Descarga el contenido de un archivo de SharePoint a partir de su `webUrl`
        /// (el link de la página que devuelve SharePoint al subir, guardado tal cual
        /// en BD). Usa la API de Graph "shares" para resolver el driveItem sin
        /// depender de que el navegador tenga sesión interactiva de Microsoft 365 —
        /// a diferencia de mostrar el `webUrl` directo en un &lt;img&gt;, que solo
        /// funciona para quien ya está logueado en SharePoint en ese navegador.
        /// </summary>
        Task<(byte[] Bytes, string ContentType, string FileName)?> DescargarPorWebUrlAsync(string webUrl);
    }
}
