using Abril_Backend.Application.DTOs.ArquitecturaComercial;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Application.Services;

/// <summary>
/// SSO-FO-150 — Autorización para el tratamiento de datos biométricos (reconocimiento facial de
/// Tareo). El trabajador la firma en físico; el coordinador de Arquitectura Comercial la escanea
/// y la sube como evidencia (ver <see cref="Infrastructure.Models.AcTareoAutorizacion"/>), lo cual
/// habilita recién ahí el enrolamiento facial. Mismo patrón que AutorizacionFirmaPdfService
/// (SSO-FO-149, SsomaModule) — header/paleta replicados a propósito para que los formatos
/// oficiales se vean consistentes entre módulos.
/// </summary>
public static class TareoAutorizacionPdfService
{
    private const string ColorPrimario   = "#1B3A6B";
    private const string ColorSecundario = "#2D5AA0";
    private const string ColorGrupo      = "#E8EEF7";
    private static readonly string Border    = "#D8DBE2";
    private static readonly string TextMain  = "#1A1A2E";
    private static readonly string TextMuted = "#5A6275";

    private const string Codigo = "SSO-FO-150";
    private const string Titulo = "AUTORIZACIÓN PARA TRATAMIENTO DE DATOS BIOMÉTRICOS";

    public static byte[] GenerarPdf(TareoAutorizacionDetalleDTO d, byte[]? logoBytes)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9).FontColor(TextMain));

                page.Header().Element(c => ComposeHeader(c, logoBytes));

                page.Content().PaddingTop(12).Element(c => ComposeBody(c, d));

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
                MetaRow("Fecha:", DateTime.Now.ToString("dd/MM/yyyy"));

                metaCol.Item().BorderTop(0.5f).Row(subRow =>
                {
                    foreach (var (lbl, val, last) in new[]
                    {
                        ("Elab.:", "AC",   false),
                        ("Rev.:",  "SSOMA", false),
                        ("Apro.:", "GP",   true ),
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

    private static void ComposeBody(IContainer container, TareoAutorizacionDetalleDTO d)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "DATOS DEL TRABAJADOR");
                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2.6f); });
                    void L(string v) => t.Cell().Background(ColorGrupo).Padding(5).Text(v).Bold().FontSize(8.5f);
                    void V(string v) => t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(v).FontSize(9.5f);

                    t.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                        .Text(d.Nombre).Bold().FontSize(12);

                    L("DNI:"); V(d.Dni ?? "—");
                });
            });

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "DECLARACIÓN Y AUTORIZACIÓN");
                inner.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(dc =>
                {
                    dc.Spacing(8);
                    void P(string texto) => dc.Item().Text(texto).FontSize(9).LineHeight(1.45f).FontColor(TextMain);

                    P($"Yo, {(string.IsNullOrWhiteSpace(d.Nombre) ? "____________________" : d.Nombre)}, " +
                      $"identificado con DNI N° {(string.IsNullOrWhiteSpace(d.Dni) ? "____________" : d.Dni)}, " +
                      "trabajador del proyecto Arquitectura Comercial, DECLARO Y AUTORIZO lo siguiente:");

                    P("1. Que autorizo a Abril Grupo Inmobiliario a capturar, almacenar y procesar una " +
                      "fotografía de mi rostro y el patrón biométrico (embedding facial) derivado de ella, " +
                      "exclusivamente para verificar mi identidad al momento de registrar mi asistencia " +
                      "(tareo) mediante reconocimiento facial.");

                    P("2. Que entiendo que este tratamiento de datos biométricos se rige por la Ley N° 29733 " +
                      "(Ley de Protección de Datos Personales) y su reglamento, y que mis datos NO serán " +
                      "usados para ningún otro fin distinto al registro de mi asistencia.");

                    P("3. Que esta autorización es voluntaria y puedo revocarla en cualquier momento mediante " +
                      "comunicación escrita a mi coordinador de Arquitectura Comercial, lo que dejaría sin " +
                      "efecto mi enrolamiento facial (mis marcaciones seguirían registrándose por los demás " +
                      "medios disponibles, sujetas a revisión manual).");

                    P("4. Que la presente autorización se formaliza con mi firma manuscrita al pie del presente " +
                      "documento, el cual — una vez firmado en físico — será escaneado y cargado al sistema " +
                      "por mi coordinador como evidencia de mi conformidad. Mi enrolamiento facial no se " +
                      "habilitará hasta que esta evidencia esté registrada.");
                });
            });

            col.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Height(60).BorderBottom(1).BorderColor(Border);
                    c.Item().PaddingTop(4).Text("Firma del trabajador").FontSize(8).AlignCenter();
                });
                row.ConstantItem(24);
                row.RelativeItem().Column(c =>
                {
                    c.Item().Height(60).BorderBottom(1).BorderColor(Border);
                    c.Item().PaddingTop(4).Text("Huella dactilar (opcional)").FontSize(8).AlignCenter();
                });
            });

            col.Item().PaddingTop(6).AlignCenter().Column(c =>
            {
                c.Item().Text(d.Nombre).Bold().FontSize(9.5f).AlignCenter();
                if (!string.IsNullOrWhiteSpace(d.Dni))
                    c.Item().Text($"DNI {d.Dni}").FontSize(8).FontColor(TextMuted).AlignCenter();
            });
        });
    }

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().Background(ColorSecundario).Padding(5)
            .Text(title).Bold().FontSize(9.5f).FontColor(Colors.White);
    }
}
