using Abril_Backend.Shared.Services.SharePoint.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces
{
    /// <summary>
    /// Archivos de un requerimiento de reclutamiento en SharePoint: resuelve su carpeta y sube ahí
    /// los documentos del proceso (los CVs y anexos de la long list, los archivos del informe de la
    /// entrevista y el CV documentado que adjunta el postulante en su formulario).
    ///
    /// Existe como servicio compartido —y no como método privado de un servicio— porque lo usan
    /// tanto <c>ReclutamientoService</c> (la bandeja de GTH) como <c>PostulanteFormularioService</c>
    /// (la página pública del postulante), y los archivos de un requerimiento tienen que caer todos
    /// en la misma carpeta: dos implementaciones se separarían con el primer cambio.
    /// </summary>
    public interface IReclutamientoArchivoStorage
    {
        /// <summary>
        /// Carpeta de SharePoint donde van TODOS los archivos del requerimiento: la subcarpeta
        /// «Long list {codigo}» dentro de la carpeta de reclutamiento configurada en
        /// <c>gth_sustento_folder</c>. Si la subcarpeta no se puede crear, cae a la carpeta raíz
        /// (los nombres de archivo ya incluyen el código, así que no colisionan).
        ///
        /// El nombre se conserva como «Long list {codigo}» porque las carpetas de producción ya
        /// existen así y renombrarlas dejaría huérfanos los enlaces guardados.
        /// </summary>
        Task<ShareLinkResolveDto> ResolverCarpetaRequerimientoAsync(string codigo);

        /// <summary>
        /// Sube un archivo del requerimiento a la carpeta indicada y devuelve el resultado.
        /// El nombre final es <c>{prefijo}_{codigo}_{pos}_{timestamp}{extension}</c>:
        /// <paramref name="pos"/> va como texto porque el CV se numera por candidato ("3"), el anexo
        /// por candidato y posición ("3_2") y los del informe por id de candidato.
        /// </summary>
        Task<SharePointUploadResultDto> SubirArchivoRequerimientoAsync(
            ShareLinkResolveDto carpeta, string prefijo, string codigo, string pos,
            string origFileName, byte[] content, string? contentType);
    }
}
