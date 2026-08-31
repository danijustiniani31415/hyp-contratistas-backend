using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Interfaces
{
    /// <summary>
    /// File digital del colaborador en SharePoint (RF-ONB-04): la carpeta «{DNI} - {NOMBRE}» dentro de
    /// la biblioteca configurada, con una subcarpeta por tipo de documento.
    ///
    /// Lo usan las dos caras del onboarding —la de GTH, que carga la carta oferta y puede subir la
    /// firmada a mano, y la pública, donde el postulante firma la suya—, y las dos tienen que dejar
    /// los documentos exactamente en el mismo sitio. Por eso resolver la carpeta y subir un documento
    /// se escribe una sola vez acá: si cada servicio armara el nombre de la carpeta por su cuenta, un
    /// colaborador podría terminar con dos files.
    /// </summary>
    public interface IFileDigitalColaboradorService
    {
        /// <summary>
        /// Resuelve (creando si hace falta) el file digital del colaborador dentro de la biblioteca
        /// configurada en <c>gth_carta_oferta_folder</c>. Solo se llama cuando el onboarding no tiene
        /// su carpeta persistida: los que sí la tienen la reusan tal cual.
        /// </summary>
        Task<FileDigitalCarpetaDto> ResolverCarpetaAsync(string dni, string nombre);

        /// <summary>
        /// Sube un documento a la <paramref name="subcarpeta"/> del file del colaborador, creándola si
        /// es la primera vez. <paramref name="queEs"/> solo arma el mensaje de error
        /// ("la carta oferta firmada").
        /// </summary>
        Task<CartaOfertaPersistDto> SubirDocumentoAsync(
            FileDigitalCarpetaDto carpeta,
            string subcarpeta,
            string fileName,
            byte[] content,
            string contentType,
            string queEs);

        /// <summary>
        /// Nombre con el que se guarda un documento del expediente:
        /// <c>{prefijo}_{código del requerimiento}_{sello de tiempo}{extensión}</c>. El sello evita
        /// pisar una versión anterior del mismo documento.
        /// </summary>
        string NombreArchivo(string prefijo, string codigo, string extension);
    }
}
