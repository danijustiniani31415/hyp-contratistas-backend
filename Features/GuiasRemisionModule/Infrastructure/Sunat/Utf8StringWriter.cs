using System.Text;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat
{
    /// <summary>
    /// StringWriter que declara UTF-8. Un StringWriter normal es UTF-16, y XmlWriter escribe en la
    /// declaración <c>encoding="utf-16"</c> aunque se le pida UTF-8; el XML se envía a SUNAT en bytes UTF-8
    /// y la contradicción la rechaza (CDR 0306: «Encoding del XML no cumple con lo establecido»).
    /// </summary>
    public sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(false);
    }
}
