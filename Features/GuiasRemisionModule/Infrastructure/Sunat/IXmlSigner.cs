namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat
{
    public interface IXmlSigner
    {
        /// <summary>Firma el XML (XMLDSig enveloped) e inserta la firma dentro de
        /// ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent. Devuelve el XML firmado
        /// y el DigestValue calculado (para trazabilidad en lb_guia_remision.xml_hash_firma).</summary>
        (string XmlFirmado, string DigestValue) Firmar(string xmlSinFirmar);
    }
}
