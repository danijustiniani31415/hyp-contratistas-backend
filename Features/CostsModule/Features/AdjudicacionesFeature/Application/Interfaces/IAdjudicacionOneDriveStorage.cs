using Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos;
using Abril_Backend.Shared.Services.SharePoint.Dtos;

namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Interfaces
{
    /// <summary>
    /// Guarda/lee los documentos de una adjudicación en las carpetas de OneDrive del proyecto
    /// (Configuración → Carpeta de adjudicaciones), siguiendo la estructura:
    /// {CarpetaConfigurada}/{Especialidad}/{Partida}/{RUC - Razón social}/{ADJUDICACIÓN N° X}/{Subcarpeta}.
    /// Todos los documentos van a la carpeta de tipo 04_OBRAS (visible para Of. Técnica), salvo el
    /// contrato firmado escaneado del paso 7 (ScannedDoc1/2/3), que va únicamente a la carpeta de tipo
    /// 07_OT/BACK UP PROYECTO. Las carpetas intermedias se crean si no existen.
    /// </summary>
    public interface IAdjudicacionOneDriveStorage
    {
        /// <summary>
        /// Asegura la cadena de carpetas para la adjudicación y sube el archivo en la subcarpeta
        /// correspondiente al tipo de documento. Devuelve WebUrl + ItemId (+ nombre final usado)
        /// del archivo en la raíz que corresponda al tipo de documento.
        /// </summary>
        Task<SharePointUploadResultDto> UploadAsync(
            int projectSubContractorId,
            AdjudicacionDocumentType documentType,
            string fileName,
            Stream content,
            string contentType,
            bool autoRenameOnLock = false);

        /// <summary>Igual que <see cref="UploadAsync(int, AdjudicacionDocumentType, string, Stream, string, bool)"/>
        /// pero recibiendo el pathData ya cargado (evita una consulta extra a BD).</summary>
        Task<SharePointUploadResultDto> UploadAsync(
            AdjudicacionPathDataDto pathData,
            AdjudicacionDocumentType documentType,
            string fileName,
            Stream content,
            string contentType,
            bool autoRenameOnLock = false);

        /// <summary>
        /// Valida que la ruta de 04_OBRAS se pueda resolver (carpeta configurada + especialidad
        /// asignada) sin tocar OneDrive ni requerir que la adjudicación exista. Lanza la misma
        /// AbrilException que lanzaría <c>UploadAsync</c>, para poder fallar antes del INSERT.
        /// </summary>
        void ValidateUploadPath(AdjudicacionPathDataDto pathData);

        /// <summary>Descarga un documento de la adjudicación a partir de su webUrl de OneDrive.</summary>
        Task<byte[]> DownloadByWebUrlAsync(string webUrl);

        /// <summary>Descarga múltiples documentos de la adjudicación como PDF (driveId de la carpeta 04_OBRAS).</summary>
        Task<Dictionary<string, byte[]>> DownloadMultipleAsPdfAsync(
            int projectSubContractorId,
            IReadOnlyList<(string ItemId, bool AlreadyPdf)> items);
    }
}
