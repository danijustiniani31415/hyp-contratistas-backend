namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Metadatos (campo <c>data</c>, JSON) del envío de la long list. El CV viaja aparte en el
    /// multipart y se enlaza por <see cref="LongListCandidatoMetaDto.CvKey"/> (nombre del campo
    /// del form file).
    /// </summary>
    public class LongListEnviarMetaDto
    {
        public List<LongListCandidatoMetaDto> Candidatos { get; set; } = new();
    }

    /// <summary>
    /// Metadatos de un candidato de la long list (sin los binarios de los archivos). El puesto no
    /// viaja: es el del requerimiento (el que registró el solicitante) y lo resuelve el repositorio.
    /// </summary>
    public class LongListCandidatoMetaDto
    {
        /// <summary>Nombre y apellido del candidato (lo captura/corrige GTH).</summary>
        public string? Nombre { get; set; }

        /// <summary>Comentario interno de GTH sobre el candidato.</summary>
        public string? Comentario { get; set; }

        /// <summary>Nombre del campo del multipart con el CV de este candidato (ej. "cv_0").</summary>
        public string? CvKey { get; set; }

        /// <summary>
        /// Nombres de los campos del multipart con el portafolio/anexos de este candidato
        /// (ej. <c>anexo_0_0</c>, <c>anexo_0_1</c>). Vacío si no cargó ninguno: los anexos son
        /// opcionales, a diferencia del CV.
        /// </summary>
        public List<string> AnexoKeys { get; set; } = new();
    }

    /// <summary>Un archivo del multipart ya leído a memoria (CV o anexo).</summary>
    public class LongListArchivoDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Candidato de la long list ya resuelto con los binarios de sus archivos (lo arma el
    /// controller a partir del multipart y lo consume el servicio para el correo).
    /// </summary>
    public class LongListCandidatoArchivoDto
    {
        public string? Nombre { get; set; }
        public string? Comentario { get; set; }

        public string CvFileName { get; set; } = string.Empty;
        public string CvContentType { get; set; } = "application/octet-stream";
        public byte[] CvContent { get; set; } = Array.Empty<byte>();

        /// <summary>Portafolio/anexos del candidato, en el orden en que GTH los cargó.</summary>
        public List<LongListArchivoDto> Anexos { get; set; } = new();
    }

    /// <summary>
    /// Candidato de la long list ya con su CV subido a SharePoint (urls resueltas por el
    /// servicio). Es lo que persiste el repositorio en <c>gth_candidato</c>.
    /// </summary>
    public class LongListCandidatoPersistDto
    {
        public string? Nombre { get; set; }
        public string? Comentario { get; set; }

        public string? CvNombre { get; set; }
        public string? CvUrl { get; set; }
        public string? CvItemId { get; set; }
        public string? CvDriveId { get; set; }

        /// <summary>Anexos ya subidos, en el orden de carga (van a <c>gth_candidato_anexo</c>).</summary>
        public List<LongListAnexoPersistDto> Anexos { get; set; } = new();
    }

    /// <summary>Un anexo del candidato ya subido a SharePoint, listo para persistir.</summary>
    public class LongListAnexoPersistDto
    {
        /// <summary>Nombre con el que quedó en SharePoint.</summary>
        public string? Nombre { get; set; }

        /// <summary>Nombre con el que GTH lo subió (el que se muestra y el que viaja en el correo).</summary>
        public string? NombreOriginal { get; set; }

        public string? Url { get; set; }
        public string? ItemId { get; set; }
        public string? DriveId { get; set; }
    }

    /// <summary>
    /// Cabecera del requerimiento necesaria para construir el correo de la long list.
    /// La devuelve el repositorio junto con la transición de estado (1 roundtrip).
    /// </summary>
    public class LongListEnvioContextoDto
    {
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }
        /// <summary>SLA del tipo de proceso asignado (null si aún no se clasificó).</summary>
        public int? SlaDias { get; set; }
        /// <summary>
        /// Correo del solicitante que registró la solicitud (app_user del <c>SolicitanteUserId</c>).
        /// Es SIEMPRE el destinatario principal de la long list; null si no se pudo resolver.
        /// </summary>
        public string? SolicitanteEmail { get; set; }
    }
}
