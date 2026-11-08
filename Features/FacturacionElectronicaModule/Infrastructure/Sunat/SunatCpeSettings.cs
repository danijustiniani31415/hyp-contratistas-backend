namespace Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Sunat
{
    /// <summary>
    /// Configuración de la facturación electrónica ante SUNAT (servicio SOAP billService). Se llena en
    /// appsettings.Local.json bajo la clave "SunatCpe". Nunca commitear valores reales.
    ///
    /// AMBIENTE: por defecto es Beta. Para emitir en Producción hay que poner Ambiente = "Produccion" Y
    /// PermitirProduccion = true: así una PC de desarrollo nunca emite documentos reales por accidente
    /// (las guías de remisión sí están hoy en Producción en el Local; esto es independiente de ellas).
    /// </summary>
    public class SunatCpeSettings
    {
        /// <summary>"Beta" (por defecto) o "Produccion".</summary>
        public string Ambiente { get; set; } = "Beta";
        /// <summary>Candado: sin esto en true, el ambiente Producción se rechaza al emitir.</summary>
        public bool PermitirProduccion { get; set; } = false;

        public string Ruc { get; set; } = "";
        public string RazonSocial { get; set; } = "";
        public string NombreComercial { get; set; } = "";

        /// <summary>Domicilio fiscal que sale en el XML y el PDF. [PENDIENTE] confirmar con H&amp;P.</summary>
        public string Direccion { get; set; } = "";
        public string Departamento { get; set; } = "";
        public string Provincia { get; set; } = "";
        public string Distrito { get; set; } = "";
        /// <summary>Ubigeo INEI de 6 dígitos del domicilio fiscal. [PENDIENTE] confirmar con H&amp;P.</summary>
        public string Ubigeo { get; set; } = "";
        /// <summary>Código del establecimiento anexo (catálogo SUNAT); 0000 = domicilio fiscal.</summary>
        public string CodigoEstablecimiento { get; set; } = "0000";

        /// <summary>Usuario SOL secundario con permiso de envío de comprobantes (sin el RUC delante). Solo Producción.</summary>
        public string SolUsuario { get; set; } = "";
        public string SolClave { get; set; } = "";

        /// <summary>
        /// Beta: SUNAT publica un RUC y usuario genéricos de pruebas; el XML debe llevar ese RUC como emisor
        /// [VERIFICAR credenciales y URL vigentes en la documentación de SUNAT].
        /// </summary>
        public string BetaRuc { get; set; } = "20000000001";
        public string BetaUsuario { get; set; } = "MODDATOS";
        public string BetaClave { get; set; } = "moddatos";

        public string UrlBeta { get; set; } = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService";
        public string UrlProduccion { get; set; } = "https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService";

        /// <summary>Servicio de consulta de comprobantes (getStatusCdr): dice si un número ya existe en SUNAT. [VERIFICAR URL vigente]</summary>
        public string UrlConsultaBeta { get; set; } = "https://e-beta.sunat.gob.pe/ol-it-wsconscpegem-beta/billConsultService";
        public string UrlConsultaProduccion { get; set; } = "https://e-factura.sunat.gob.pe/ol-it-wsconscpegem/billConsultService";
        public string UrlConsultaActiva => EsProduccion ? UrlConsultaProduccion : UrlConsultaBeta;

        /// <summary>
        /// Antes de enviar un comprobante nuevo, pregunta a SUNAT si ese número ya existe (otro sistema o el portal usando la misma serie)
        /// y salta al siguiente libre. Si SUNAT no responde o la consulta falla, NO bloquea la emisión. El Beta no tiene este servicio.
        /// </summary>
        public bool VerificarExistenciaAntesDeEmitir { get; set; } = true;

        public bool EsProduccion => Ambiente == "Produccion";
        public string UrlActiva => EsProduccion ? UrlProduccion : UrlBeta;

        /// <summary>RUC que va como emisor en el XML y en el nombre del archivo.</summary>
        public string RucEmisor => EsProduccion ? Ruc : BetaRuc;
        public string RazonSocialEmisor => EsProduccion ? RazonSocial : "EMPRESA DE PRUEBAS BETA";
        /// <summary>Usuario WS-Security: RUC + usuario SOL.</summary>
        public string UsuarioWs => EsProduccion ? Ruc + SolUsuario : BetaRuc + BetaUsuario;
        public string ClaveWs => EsProduccion ? SolClave : BetaClave;

        /// <summary>Cuenta de detracciones de H&amp;P en el Banco de la Nación (se usa por defecto en las facturas con detracción). [PENDIENTE]</summary>
        public string CuentaDetracciones { get; set; } = "";
    }
}
