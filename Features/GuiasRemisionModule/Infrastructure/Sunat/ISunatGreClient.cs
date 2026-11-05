namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat
{
    public class SunatGreEnvioResultado
    {
        public bool Aceptado { get; set; }
        /// <summary>true si SUNAT ya generó el ticket de recepción (el envío en sí fue válido) — no
        /// implica que el CDR ya esté resuelto, eso se sabe recién con ConsultarEstadoAsync.</summary>
        public bool Enviado { get; set; }
        public string? NumTicket { get; set; }
        public string? ErrorMensaje { get; set; }
    }

    public class SunatGreConsultaResultado
    {
        public bool CdrGenerado { get; set; }
        public bool Aceptado { get; set; }
        public string? CodigoRespuesta { get; set; }
        public string? Descripcion { get; set; }
        /// <summary>XML del CDR tal como lo entregó SUNAT: constancia legal, se conserva.</summary>
        public string? CdrXml { get; set; }
        /// <summary>Enlace oficial del QR (descargaqr?hashqr=...) si SUNAT lo entregó en su respuesta o en el CDR.</summary>
        public string? QrUrl { get; set; }
    }

    public interface ISunatGreClient
    {
        /// <summary>Zipea el XML firmado y lo envía a SUNAT (API REST nueva, con OAuth2). Devuelve
        /// el numTicket para consultar el CDR después — SUNAT ya no responde el CDR en la misma
        /// llamada, es asíncrono.</summary>
        Task<SunatGreEnvioResultado> EnviarAsync(string xmlFirmado, string nombreArchivoSinExtension);

        /// <summary>Consulta si SUNAT ya procesó el ticket y, si sí, el resultado (aceptado/rechazado).</summary>
        Task<SunatGreConsultaResultado> ConsultarEstadoAsync(string numTicket);
    }
}
