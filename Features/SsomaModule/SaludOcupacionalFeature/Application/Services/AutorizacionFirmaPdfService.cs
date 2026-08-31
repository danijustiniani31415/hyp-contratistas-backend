using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services;

/// <summary>
/// SSO-FO-149 — Autorización de firma electrónica del médico ocupacional. El médico la
/// acepta una vez en el sistema, la imprime, la firma a mano y sube el escaneado como
/// evidencia (ver <see cref="Abril_Backend.Infrastructure.Models.SsMedicoOcupacional.UrlAutorizacionFirmada"/>).
/// </summary>
public static class AutorizacionFirmaPdfService
{
    // ── Paleta corporativa (misma que Convalidación / RAC / Inspecciones) ──────
    private const string ColorPrimario   = "#1B3A6B";
    private const string ColorSecundario = "#2D5AA0";
    private const string ColorGrupo      = "#E8EEF7";
    private static readonly string Border    = "#D8DBE2";
    private static readonly string TextMain  = "#1A1A2E";
    private static readonly string TextMuted = "#5A6275";

    private const string Codigo = "SSO-FO-149";
    private const string Titulo = "AUTORIZACIÓN DE FIRMA ELECTRÓNICA — MÉDICO OCUPACIONAL";

    public static byte[] GenerarPdf(AutorizacionFirmaDetalleDto d, byte[]? logoBytes, byte[]? firmaDigitalBytes = null)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9).FontColor(TextMain));

                page.Header().Element(c => ComposeHeader(c, logoBytes));

                page.Content().PaddingTop(12).Element(c => ComposeBody(c, d, firmaDigitalBytes));

                page.Footer().AlignCenter().PaddingTop(6)
                    .Text(t =>
                    {
                        t.Span("Documento generado por el sistema Abril — ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                        t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(7.5f).FontColor(Colors.Grey.Medium);
                    });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, byte[]? logoBytes)
    {
        container.Border(0.5f).BorderColor(Border).Row(row =>
        {
            row.ConstantItem(90).AlignMiddle().AlignCenter().Padding(4).Element(logoEl =>
            {
                if (logoBytes != null)
                    logoEl.AlignMiddle().AlignCenter().Image(logoBytes).FitArea();
                else
                    logoEl.AlignMiddle().AlignCenter().Text("ABRIL").Bold().FontSize(8).AlignCenter();
            });

            row.ConstantItem(0.5f).Background(Colors.Grey.Lighten1);

            row.RelativeItem().AlignMiddle().AlignCenter()
                .Text(Titulo).Bold().FontSize(11).AlignCenter();

            row.ConstantItem(0.5f).Background(Colors.Grey.Lighten1);

            row.ConstantItem(120).Column(metaCol =>
            {
                void MetaRow(string label, string valor, bool last = false)
                {
                    metaCol.Item()
                        .BorderBottom(last ? 0f : 0.5f)
                        .Padding(2).Row(r =>
                    {
                        r.AutoItem().Text(label).Bold().FontSize(7);
                        r.ConstantItem(2);
                        r.RelativeItem().Text(valor).FontSize(7);
                    });
                }
                MetaRow("Código:", Codigo);
                MetaRow("Versión:", "01");
                MetaRow("Fecha:", "10/08/2026");

                metaCol.Item().BorderTop(0.5f).Row(subRow =>
                {
                    foreach (var (lbl, val, last) in new[]
                    {
                        ("Elab.:", "SSOMA",  false),
                        ("Rev.:",  "JSSOMA", false),
                        ("Apro.:", "GP",     true ),
                    })
                    {
                        var cell = subRow.RelativeItem().Padding(2);
                        if (!last) cell = cell.BorderRight(0.5f);
                        cell.Text(t =>
                        {
                            t.Span(lbl + " ").Bold().FontSize(5.5f);
                            t.Span(val).FontSize(5.5f);
                        });
                    }
                });
            });
        });
    }

    private static void ComposeBody(IContainer container, AutorizacionFirmaDetalleDto d, byte[]? firmaDigitalBytes)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "DATOS DEL MÉDICO OCUPACIONAL");
                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1.6f); c.RelativeColumn(1); c.RelativeColumn(1.6f); });
                    void L(string v) => t.Cell().Background(ColorGrupo).Padding(5).Text(v).Bold().FontSize(8.5f);
                    void V(string v) => t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(v).FontSize(9.5f);

                    t.Cell().ColumnSpan(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                        .Text(d.MedicoNombre).Bold().FontSize(12);

                    L("DNI:"); V(d.MedicoDni ?? "—");
                    L("Colegiatura CMP:"); V(d.MedicoCmp ?? "—");
                    L("Especialidad:"); V(d.MedicoEspecialidad ?? "—");
                });
            });

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "DECLARACIÓN Y AUTORIZACIÓN");
                inner.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(dc =>
                {
                    dc.Spacing(8);
                    void P(string texto) => dc.Item().Text(texto).FontSize(9).LineHeight(1.45f).FontColor(TextMain);

                    P($"Yo, {(string.IsNullOrWhiteSpace(d.MedicoNombre) ? "____________________" : d.MedicoNombre)}, " +
                      $"identificado con DNI N° {(string.IsNullOrWhiteSpace(d.MedicoDni) ? "____________" : d.MedicoDni)}, " +
                      $"Colegiatura CMP N° {(string.IsNullOrWhiteSpace(d.MedicoCmp) ? "____________" : d.MedicoCmp)}, " +
                      $"especialidad {(string.IsNullOrWhiteSpace(d.MedicoEspecialidad) ? "____________" : d.MedicoEspecialidad)}, " +
                      "en mi calidad de médico ocupacional autorizado del sistema Abril, DECLARO Y AUTORIZO lo siguiente:");

                    P("1. Que el uso de mis credenciales de acceso corporativo (cuenta Microsoft/Azure AD) en conjunto " +
                      "con una clave de firma personal (\"PIN de firma\"), definida únicamente por mí y de mi exclusiva " +
                      "responsabilidad, constituye mi firma electrónica para efectos de autorizar, aprobar, observar o " +
                      "rechazar convalidaciones de exámenes médicos ocupacionales (EMO) dentro del sistema Abril, conforme " +
                      "a los alcances del artículo 141° del Código Civil y la Ley N° 27269 (Ley de Firmas y Certificados " +
                      "Digitales) y su reglamento.");

                    P("2. Que entiendo que cada convalidación autorizada mediante este mecanismo queda registrada con " +
                      "fecha, hora, dirección IP y el detalle completo del acto médico, y que dicho registro tiene el " +
                      "mismo valor probatorio que mi firma manuscrita para efectos internos y ante las autoridades " +
                      "competentes (SUNAFIL, MINSA, y otras que correspondan).");

                    P("3. Que soy el único responsable de la confidencialidad de mi PIN de firma, que no lo compartiré " +
                      "con terceros, y que cualquier convalidación autorizada bajo mis credenciales se entenderá " +
                      "realizada por mí, salvo que reporte de inmediato su compromiso o pérdida para su revocación y " +
                      "reemplazo.");

                    P("4. Que la presente autorización se formaliza además con mi firma manuscrita al pie del presente " +
                      "documento, el cual — una vez firmado en físico — será escaneado y cargado al sistema como " +
                      "evidencia documental de mi conformidad, sin perjuicio de la validez de las convalidaciones que " +
                      "realice electrónicamente desde la fecha de mi aceptación digital en el sistema.");

                    P("5. Que esta autorización es indefinida en el tiempo, mientras mantenga vínculo activo como " +
                      "médico ocupacional autorizado de Abril, y puede ser revocada por cualquiera de las partes " +
                      "mediante comunicación escrita.");
                });
            });

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "FIRMAS PARA COMPARACIÓN");
                inner.Item().Text(
                    "El recuadro izquierdo es la firma digital ya registrada en el sistema. El " +
                    "médico debe firmar de su puño y letra en el recuadro derecho, lo más parecido " +
                    "posible a su firma de DNI, para que ambas puedan compararse.")
                    .FontSize(8).FontColor(TextMuted).LineHeight(1.35f);

                inner.Item().PaddingTop(10).Row(row =>
                {
                    void FirmaBox(RowDescriptor r, string titulo, Action<IContainer> contenido)
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Height(80).Border(1).BorderColor(Border).Element(contenido);
                            c.Item().PaddingTop(4).Text(titulo).Bold().FontSize(8).AlignCenter();
                        });
                    }

                    FirmaBox(row, "Firma digital (sistema)", box =>
                    {
                        if (firmaDigitalBytes != null)
                            box.Padding(4).Image(firmaDigitalBytes).FitArea();
                        else
                            box.AlignMiddle().AlignCenter()
                                .Text("Sin firma digital registrada").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                    });

                    row.ConstantItem(16);

                    FirmaBox(row, "Firma manuscrita (comparar)", box => { });
                });

                inner.Item().PaddingTop(10).AlignCenter().Column(c =>
                {
                    c.Item().Text(d.MedicoNombre).Bold().FontSize(9.5f).AlignCenter();
                    c.Item().AlignCenter().Text(t =>
                    {
                        t.AlignCenter();
                        if (!string.IsNullOrWhiteSpace(d.MedicoCmp))
                            t.Span($"CMP {d.MedicoCmp}").FontSize(8).FontColor(TextMuted);
                        if (!string.IsNullOrWhiteSpace(d.MedicoDni))
                            t.Span($" — DNI {d.MedicoDni}").FontSize(8).FontColor(TextMuted);
                    });
                    c.Item().Text("Médico Ocupacional").FontSize(8).FontColor(TextMuted).AlignCenter();
                });
            });
        });
    }

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().Background(ColorSecundario).Padding(5)
            .Text(title).Bold().FontSize(9.5f).FontColor(Colors.White);
    }
}
