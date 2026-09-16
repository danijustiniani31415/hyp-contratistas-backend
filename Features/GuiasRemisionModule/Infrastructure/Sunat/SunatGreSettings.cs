namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat
{
    /// <summary>
    /// Configuración de la emisión electrónica real de GRE ante SUNAT — plataforma nueva (API REST +
    /// OAuth2, "credenciales API" del portal SOL), NO el billService SOAP viejo. Se llena en
    /// appsettings.Local.json bajo la clave "SunatGre" — nunca commitear valores reales, igual que
    /// ya se hace con SendGrid/Azure/Reniec en este proyecto.
    ///
    /// Flujo: 1) POST a AuthUrl (form-urlencoded, grant_type=password) → access_token.
    /// 2) POST a {ApiBaseActivo}/contribuyente/gem/comprobantes/{filename} con el ZIP en base64 +
    /// hash SHA-256 → numTicket. 3) GET {ApiBaseActivo}/contribuyente/gem/comprobantes/envios/{numTicket}
    /// hasta que indCdrGenerado=true → ahí viene el CDR real (aceptado/rechazado).
    /// </summary>
    public class SunatGreSettings
    {
        /// <summary>RUC de HP Constructores Generales (11 dígitos) — emisor de la GRE.</summary>
        public string Ruc { get; set; } = "";
        public string RazonSocial { get; set; } = "";

        /// <summary>Serie del talonario electrónico GRE, formato SUNAT: letra 'T' + 3 dígitos (ej. T001).</summary>
        public string Serie { get; set; } = "T001";

        /// <summary>"Beta" (ambiente de pruebas, host api-cpe-test) o "Produccion".</summary>
        public string Ambiente { get; set; } = "Beta";

        /// <summary>Usuario SOL secundario (sin el RUC concatenado — el username del OAuth es RUC+usuario).</summary>
        public string SolUsuario { get; set; } = "";
        public string SolClave { get; set; } = "";

        /// <summary>"Credenciales API" generadas en el portal SOL (Menú SOL &gt; Empresas &gt; API) — distintas del usuario/clave SOL.</summary>
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";

        /// <summary>Ruta absoluta al certificado digital (.pfx/.p12) usado para firmar el XML (XMLDSig) — sigue siendo obligatorio, la API nueva no reemplaza la firma del documento, solo el transporte.</summary>
        public string CertificadoPath { get; set; } = "";
        public string CertificadoPassword { get; set; } = "";

        public string AuthUrl => $"https://api-seguridad.sunat.gob.pe/v1/clientessol/{ClientId}/oauth2/token/";

        // [PENDIENTE VERIFICAR] api-cpe-test.sunat.gob.pe es el host de pruebas de la plataforma
        // nueva (confirmado por búsqueda), pero SUNAT suele exigir un RUC de prueba propio asignado
        // para homologación en ese ambiente — probar primero si el RUC real responde ahí, y si no,
        // pedir a SUNAT el RUC/credenciales de homologación específicos para GRE.
        public string ApiBaseBeta { get; set; } = "https://api-cpe-test.sunat.gob.pe/v1";
        public string ApiBaseProduccion { get; set; } = "https://api-cpe.sunat.gob.pe/v1";

        public string ApiBaseActivo => Ambiente == "Produccion" ? ApiBaseProduccion : ApiBaseBeta;
    }
}
