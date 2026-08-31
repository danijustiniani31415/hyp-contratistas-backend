namespace Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados
{
    /// <summary>Certificado ya subido a la carpeta configurada, listo para guardarse como adjunto.</summary>
    public class DescansoCertificadoSubidoDto
    {
        /// <summary>webUrl de SharePoint (lo que se guarda en ss_descanso_medico_adjunto.url).</summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>Nombre original del archivo elegido por el usuario (lo que se le muestra).</summary>
        public string Nombre { get; set; } = string.Empty;
        /// <summary>Ubicación estable para descargarlo luego vía Graph.</summary>
        public string DriveId { get; set; } = string.Empty;
        public string? ItemId { get; set; }
    }

    /// <summary>Contenido de un certificado servido por el backend (proxy de descarga).</summary>
    public class DescansoCertificadoArchivoDto
    {
        public byte[] Contenido { get; set; } = [];
        public string ContentType { get; set; } = "application/octet-stream";
        public string NombreArchivo { get; set; } = "certificado";
    }
}
