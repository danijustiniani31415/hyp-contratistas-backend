using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat
{
    public interface IGuiaRemisionXmlBuilder
    {
        /// <summary>Construye el XML UBL 2.1 (DespatchAdvice) de la guía, sin firmar todavía.</summary>
        string Build(GuiaRemision guia);
    }
}
