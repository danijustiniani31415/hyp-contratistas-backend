namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Models
{
    /// <summary>
    /// Carpeta única (singleton) de SharePoint donde Onboarding guarda los documentos de los nuevos
    /// colaboradores (tabla <c>gth_carta_oferta_folder</c>). Hoy apunta al «File de colaboradores»
    /// del sitio de la biblioteca: dentro se crea una carpeta por trabajador —«{DNI} - {NOMBRE}»— y,
    /// dentro de esa, una subcarpeta por tipo de documento («Carta Oferta Enviada», «Carta Oferta
    /// Firmada»). Se define por base de datos —no por appsettings— para poder cambiar la biblioteca
    /// destino sin redeploy: basta con actualizar <c>link_url</c> de la fila vigente. Existe a lo
    /// sumo una fila vigente (<c>state = true</c> y <c>active = true</c>).
    ///
    /// El nombre de la tabla quedó de cuando solo se guardaba la carta oferta; el link es el de la
    /// biblioteca del file completo.
    ///
    /// Es una tabla aparte de <c>gth_sustento_folder</c> (Reclutamiento) a propósito: la carta oferta
    /// es un documento del colaborador, no un sustento de la solicitud de personal, y GTH las guarda
    /// en bibliotecas distintas. Compartir la tabla ataba las dos a la misma carpeta.
    ///
    /// Cambiar el link NO afecta a las cartas ya subidas: cada onboarding guarda en su propia fila el
    /// <c>carta_oferta_url</c> / <c>carta_oferta_item_id</c> / <c>carta_oferta_drive_id</c> con los
    /// que se subió, así que se siguen abriendo desde donde quedaron. El link de acá solo decide
    /// dónde se suben las cartas NUEVAS.
    /// </summary>
    public class GthCartaOfertaFolder
    {
        public int GthCartaOfertaFolderId { get; set; }

        /// <summary>Link de la carpeta de SharePoint (se resuelve a driveId/folderId al subir).</summary>
        public string LinkUrl { get; set; } = null!;

        /// <summary>Nombre legible de la carpeta (opcional, solo referencia).</summary>
        public string? FolderName { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
