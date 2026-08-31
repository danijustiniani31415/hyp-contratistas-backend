using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace Abril_Backend.Shared.Services.Pdf
{
    /// <summary>
    /// En qué páginas del documento se estampa la firma.
    /// </summary>
    public enum SignatureStampScope
    {
        /// <summary>
        /// Todas las páginas. Es el visado de una factura: la firma del Gerente General vale como
        /// conformidad de cada hoja del documento.
        /// </summary>
        AllPages,

        /// <summary>
        /// Solo la última página. Es donde va la línea de firma de un documento que se suscribe
        /// (la carta oferta que firma el nuevo colaborador).
        /// </summary>
        LastPage,
    }

    /// <summary>
    /// Estampa una firma (PNG) en la esquina inferior derecha del documento. El resultado es SIEMPRE
    /// un PDF: si el documento original es una imagen (PNG/JPG/WEBP) se convierte a un PDF de una
    /// página y se estampa.
    ///
    /// Vive en Shared porque lo usan dos módulos: Contabilidad (la firma del Gerente General sobre
    /// una factura, en todas las páginas) y Gestión GTH (la firma del postulante sobre su carta
    /// oferta, solo en la última). Lo único que cambia entre los dos es
    /// <see cref="SignatureStampScope"/>.
    /// </summary>
    public static class SignaturePdfStamper
    {
        private const double SignatureWidthPt = 140; // ancho objetivo de la firma
        private const double MarginPt = 24;          // margen respecto al borde inferior/derecho

        public static byte[] Stamp(
            byte[] source,
            byte[] signaturePng,
            SignatureStampScope scope = SignatureStampScope.AllPages)
        {
            return IsPdf(source)
                ? StampPdf(source, signaturePng, scope)
                // Una imagen se convierte en un PDF de una sola página, así que su única página es
                // también la última: el alcance no cambia nada y no hace falta propagarlo.
                : StampImageAsPdf(source, signaturePng);
        }

        private static bool IsPdf(byte[] b)
            => b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46; // "%PDF"

        private static byte[] StampPdf(byte[] pdfBytes, byte[] signaturePng, SignatureStampScope scope)
        {
            using var input = new MemoryStream(pdfBytes);
            var doc = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

            var desde = scope == SignatureStampScope.LastPage ? doc.Pages.Count - 1 : 0;
            for (var i = Math.Max(desde, 0); i < doc.Pages.Count; i++)
            {
                var page = doc.Pages[i];
                using var gfx = XGraphics.FromPdfPage(page);
                DrawSignatureBottomRight(gfx, signaturePng, page.Width.Point, page.Height.Point);
            }

            using var output = new MemoryStream();
            doc.Save(output, false);
            return output.ToArray();
        }

        private static byte[] StampImageAsPdf(byte[] imageBytes, byte[] signaturePng)
        {
            // Normalizar a PNG (ImageSharp soporta png/jpg/webp) y obtener dimensiones en píxeles.
            byte[] pngBytes;
            int pxW, pxH;
            using (var img = Image.Load(imageBytes))
            {
                pxW = img.Width;
                pxH = img.Height;
                using var ms = new MemoryStream();
                img.Save(ms, new PngEncoder());
                pngBytes = ms.ToArray();
            }

            // Página del tamaño de la imagen (96 DPI → puntos).
            double pageW = pxW * 72.0 / 96.0;
            double pageH = pxH * 72.0 / 96.0;

            var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(pageW);
            page.Height = XUnit.FromPoint(pageH);

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                using (var pageStream = new MemoryStream(pngBytes))
                using (var pageImg = XImage.FromStream(pageStream))
                    gfx.DrawImage(pageImg, 0, 0, pageW, pageH);

                DrawSignatureBottomRight(gfx, signaturePng, pageW, pageH);
            }

            using var output = new MemoryStream();
            doc.Save(output, false);
            return output.ToArray();
        }

        private static void DrawSignatureBottomRight(XGraphics gfx, byte[] signaturePng, double pageW, double pageH)
        {
            // PDFsharp 6 recibe el Stream directamente (en PdfSharpCore era un Func<Stream>).
            // El XImage ya tiene la imagen decodificada en memoria, así que el stream puede
            // cerrarse en el mismo scope sin afectar al dibujado ni al Save posterior.
            using var sigStream = new MemoryStream(signaturePng);
            using var sig = XImage.FromStream(sigStream);

            double w = SignatureWidthPt;
            double h = SignatureWidthPt * sig.PixelHeight / sig.PixelWidth;

            // No dejar que la firma ocupe más del 40% del ancho de páginas pequeñas.
            if (w > pageW * 0.4)
            {
                w = pageW * 0.4;
                h = w * sig.PixelHeight / sig.PixelWidth;
            }

            double x = pageW - MarginPt - w;
            double y = pageH - MarginPt - h;
            gfx.DrawImage(sig, x, y, w, h);
        }
    }
}
