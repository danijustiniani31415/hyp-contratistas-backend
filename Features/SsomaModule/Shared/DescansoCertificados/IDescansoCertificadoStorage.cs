namespace Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados
{
    /// <summary>
    /// Guarda y sirve los certificados médicos de un descanso. Lo usan las dos pantallas
    /// que registran descansos —Mi Salud (lo sube el trabajador) y Descansos de Salud
    /// Ocupacional (lo sube SSOMA)— por eso vive en el Shared del módulo.
    ///
    /// El destino es la carpeta de SharePoint configurada en la tabla singleton
    /// <c>ss_descanso_carpeta</c>: se guarda el link tal cual y aquí se resuelve a
    /// driveId/folderId vía Graph. Cambiar la carpeta es un UPDATE, no un redeploy.
    /// </summary>
    public interface IDescansoCertificadoStorage
    {
        /// <summary>
        /// Sube los archivos recibidos a la carpeta configurada. Devuelve un item por archivo
        /// subido, en el mismo orden. Los archivos vacíos se ignoran; si la lista queda vacía
        /// devuelve una lista vacía sin tocar SharePoint.
        /// </summary>
        /// <param name="prefijo">
        /// Prefijo del nombre con el que se guarda en SharePoint (p. ej. "misalud" o "ssoma"),
        /// para reconocer de dónde vino el archivo dentro de la carpeta.
        /// </param>
        Task<List<DescansoCertificadoSubidoDto>> SubirAsync(
            IEnumerable<IFormFile> archivos,
            string prefijo);

        /// <summary>
        /// Devuelve el contenido de un certificado a partir de lo guardado en su adjunto.
        /// Con driveId + itemId lo baja vía Graph; si el adjunto es anterior a la carpeta
        /// configurable, cae al esquema viejo (ruta relativa del sitio SSOMAApps o URL
        /// absoluta). Null si no se pudo obtener.
        /// </summary>
        Task<DescansoCertificadoArchivoDto?> DescargarAsync(
            string? driveId,
            string? itemId,
            string url,
            string? nombreArchivo);
    }
}
