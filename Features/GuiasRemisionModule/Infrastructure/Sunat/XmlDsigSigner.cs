using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat
{
    /// <summary>
    /// Firma XMLDSig enveloped, requisito de SUNAT para todo CPE/GRE electrónico. Usa el
    /// certificado configurado en SunatGreSettings (.pfx) — mientras no se tenga uno real para
    /// Producción, sirve perfectamente un certificado autofirmado para probar el flujo completo
    /// contra el ambiente Beta (SUNAT no valida cadena de confianza ahí, solo que el XML esté
    /// firmado correctamente).
    /// </summary>
    public class XmlDsigSigner : IXmlSigner
    {
        private readonly SunatGreSettings _settings;

        public XmlDsigSigner(IOptions<SunatGreSettings> settings)
        {
            _settings = settings.Value;
        }

        public (string XmlFirmado, string DigestValue) Firmar(string xmlSinFirmar)
        {
            if (string.IsNullOrWhiteSpace(_settings.CertificadoPath))
                throw new InvalidOperationException(
                    "SunatGre:CertificadoPath no está configurado — falta el certificado digital (.pfx) en appsettings.Local.json.");

            // [CORREGIDO] MachineKeySet exige permisos de administrador en Windows para el
            // almacén de claves a nivel de máquina — con un usuario normal corriendo `dotnet run`
            // esto fallaba con "Error occurred during a cryptographic operation" (mensaje genérico
            // de .NET que no delata la causa real). Sin ese flag, la clave se maneja en memoria del
            // proceso actual, sin tocar ningún almacén de Windows — es lo que corresponde acá.
            var cert = X509CertificateLoader.LoadPkcs12FromFile(
                _settings.CertificadoPath, _settings.CertificadoPassword,
                X509KeyStorageFlags.Exportable);

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xmlSinFirmar);

            var signedXml = new SignedXml(doc)
            {
                SigningKey = cert.GetRSAPrivateKey(),
            };

            var reference = new Reference("")
            {
                DigestMethod = SignedXml.XmlDsigSHA256Url,
            };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigC14NTransform());
            signedXml.AddReference(reference);
            signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();
            var signatureXml = signedXml.GetXml();

            var extensionContent = doc.GetElementsByTagName("ext:ExtensionContent")[0]
                ?? throw new InvalidOperationException("No se encontró ext:ExtensionContent en el XML — revisar GuiaRemisionUblXmlBuilder.");
            extensionContent.AppendChild(doc.ImportNode(signatureXml, true));

            var digestValue = ((XmlElement)signatureXml).GetElementsByTagName("DigestValue")[0]?.InnerText ?? "";

            using var writer = new StringWriter(new StringBuilder());
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Encoding = Encoding.UTF8, OmitXmlDeclaration = false }))
            {
                doc.Save(xmlWriter);
            }

            return (writer.ToString(), digestValue);
        }
    }
}
