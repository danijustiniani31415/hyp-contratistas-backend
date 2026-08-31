using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;

public static class AmonestacionPdfService
{
    // ── Paleta corporativa ─────────────────────────────────────────────
    private static readonly string Navy      = "#0D1F3C";   // título, sección headers
    private static readonly string NavyLight = "#1E3A5F";   // acento secundario
    private static readonly string Gold      = "#C9A84C";   // línea de acento
    private static readonly string BgRow     = "#F5F6F8";   // fondo filas etiqueta
    private static readonly string Border    = "#D8DBE2";   // bordes suaves
    private static readonly string TextMain  = "#1A1A2E";   // texto principal
    private static readonly string TextMuted = "#5A6275";   // texto secundario

    public static byte[] GenerarPdf(AmonestacionDetalleDto a, List<byte[]> fotoBytes, byte[]? logoBytes)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(0.6f, Unit.Centimetre);
                page.MarginVertical(0.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial").FontColor(TextMain));

                page.Content().Element(c => ComposeBody(c, a, fotoBytes, logoBytes));
            });
        }).GeneratePdf();
    }

    // ── Dos copias lado a lado — cada una con su propio header ──────
    private static void ComposeBody(IContainer c, AmonestacionDetalleDto a,
        List<byte[]> fotos, byte[]? logoBytes)
    {
        c.Row(row =>
        {
            row.RelativeItem().Element(cell => RenderCopia(cell, a, fotos, logoBytes, "COPIA EMPRESA"));

            // Separador de corte centrado
            row.ConstantItem(14).Layers(layers =>
            {
                layers.Layer().AlignCenter().Width(1).Background(Border).ExtendVertical();
                layers.PrimaryLayer().AlignCenter().AlignMiddle()
                    .Background(Colors.White).PaddingVertical(2)
                    .Text("✂").FontSize(9).FontColor(TextMuted);
            });

            row.RelativeItem().Element(cell => RenderCopia(cell, a, fotos, logoBytes, "COPIA TRABAJADOR"));
        });
    }

    private static void RenderCopia(IContainer container, AmonestacionDetalleDto a,
        List<byte[]> fotos, byte[]? logoBytes, string etiquetaCopia)
    {
        container.Border(0.5f).BorderColor(Border).Column(col =>
        {
            // ── Header: Logo | Título | Metadatos (estructura Flash Report) ──
            col.Item().Border(0.5f).BorderColor(Border).Row(row =>
            {
                // Logo — centrado H y V dentro del área fija
                row.ConstantItem(90).AlignMiddle().AlignCenter().Padding(4).Element(logoEl =>
                {
                    if (logoBytes != null)
                        logoEl.AlignMiddle().AlignCenter().Image(logoBytes).FitArea();
                    else
                        // Sin logo: muestra nombre de empresa centrado
                        logoEl.AlignMiddle().AlignCenter()
                            .Text(a.EsEmpresaAbril ? "ABRIL" : a.EmpresaNombre)
                            .Bold().FontSize(8).AlignCenter();
                });

                row.ConstantItem(0.5f).Background(Colors.Grey.Lighten1);

                // Título centrado
                row.RelativeItem().AlignMiddle().AlignCenter()
                    .Text("PAPELETA DE AMONESTACIÓN Y NOTIFICACIÓN DE RIESGO")
                    .Bold().FontSize(10);

                row.ConstantItem(0.5f).Background(Colors.Grey.Lighten1);

                // Columna derecha — Código/Versión/Fecha al mismo nivel, Elab en una sola línea
                row.ConstantItem(110).Column(metaCol =>
                {
                    // Filas de metadatos — todas igual, Código a la misma altura que Versión y Fecha
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
                    MetaRow("Código:",  "SSO-F-107");
                    MetaRow("Versión:", "01");
                    MetaRow("Fecha:",   "01/01/2025");

                    // Elab / Rev / Apro — cada uno en UNA sola línea: "Label: Valor"
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

            // ── Banner copia ──
            col.Item().Background(Colors.Grey.Darken2)
                .PaddingVertical(2).PaddingHorizontal(6).Row(r =>
            {
                r.RelativeItem().Text(etiquetaCopia).Bold().FontSize(7.5f).FontColor(Colors.White);
                if (a.Inhabilitado)
                    r.AutoItem().Text("INHABILITADO").Bold().FontSize(7).FontColor("#F59E0B");
            });

            // ── Fotos en la parte superior (si existen) ──
            if (fotos.Count > 0)
            {
                var fotoList = fotos.Take(3).ToList();
                float photoW = fotoList.Count == 1 ? 200f : fotoList.Count == 2 ? 155f : 118f;
                col.Item().BorderBottom(0.5f).BorderColor(Border).Padding(6).Row(r =>
                {
                    r.RelativeItem(); // spacer izquierdo
                    for (int i = 0; i < fotoList.Count; i++)
                    {
                        if (i > 0) r.ConstantItem(6);
                        r.ConstantItem(photoW).Height(150).Image(fotoList[i]).FitArea();
                    }
                    r.RelativeItem(); // spacer derecho
                });
            }

            col.Item().Padding(4).Column(inner =>
            {
                // Código / Fecha / Puntaje / Proyecto / Penalización en una sola tabla
                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(0.7f); c.RelativeColumn(1.1f);
                        c.RelativeColumn(0.7f); c.RelativeColumn(0.9f);
                        c.RelativeColumn(0.8f); c.RelativeColumn(0.7f);
                        c.RelativeColumn(0.9f); c.RelativeColumn(1.5f);
                    });
                    void L(string v) => t.Cell().BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Background(BgRow).Text(v).Bold().FontSize(6.5f).FontColor(TextMuted);
                    void V(string v) => t.Cell().BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Text(v).FontSize(7f);

                    L("Código"); V(a.Codigo);
                    L("Fecha"); V(a.Fecha.ToString("dd/MM/yyyy"));
                    L("Puntaje acum.");
                    t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(2f)
                        .Background(a.PuntosAcumulados >= 7 ? "#FEF3C7" : "#F0FDF4")
                        .Text($"{a.PuntosAcumulados}/10").Bold().FontSize(8)
                        .FontColor(a.Inhabilitado ? "#DC2626" : a.PuntosAcumulados >= 7 ? "#92400E" : "#166534");
                    L("Proyecto"); t.Cell().ColumnSpan(3).BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Text(a.ProyectoNombre).FontSize(7f);
                    L("Penalización"); V(a.AplicaPenalizacion ? $"Sí — S/ {a.MontoCalculado:N2}" : "No aplica");
                });

                inner.Item().PaddingTop(3);
                SectionHeader(inner, "DATOS DEL TRABAJADOR NOTIFICADO");

                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(1.2f);
                        c.RelativeColumn(0.9f); c.RelativeColumn(1.5f);
                    });
                    t.Cell().ColumnSpan(4).BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2.5f).Text(a.WorkerNombre).Bold().FontSize(8.5f);

                    void L(string v) => t.Cell().BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Background(BgRow).Text(v).Bold().FontSize(6.5f).FontColor(TextMuted);
                    void V(string v) => t.Cell().BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Text(v).FontSize(7f);

                    L("DNI"); V(a.WorkerDni);
                    L("Empresa"); V(a.EmpresaNombre);
                    L("Cargo / Categoría"); V(a.WorkerCargo ?? a.WorkerCategoria ?? "—");
                    L("Partida"); V(a.PartidaNombre ?? "—");
                });

                inner.Item().PaddingTop(3);
                SectionHeader(inner, "TIPO DE SANCIÓN E INFRACCIÓN");

                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                    void L(string v) => t.Cell().BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Background(BgRow).Text(v).Bold().FontSize(6.5f).FontColor(TextMuted);
                    void V(string v) => t.Cell().BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Text(v).FontSize(7f);
                    L("Tipo de sanción"); V(a.TipoSancionNombre);
                    L("Infracción aplicada"); V(a.InfraccionTipoNombre);
                });

                inner.Item().PaddingTop(3);
                SectionHeader(inner, "DESCRIPCIÓN DE LO OCURRIDO");
                inner.Item().Border(0.5f).BorderColor(Border).Padding(3).MinHeight(18)
                    .Text(a.Descripcion).FontSize(7f).LineHeight(1.3f);

                inner.Item().PaddingTop(3);
                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.3f); c.RelativeColumn(0.6f);
                        c.RelativeColumn(1.2f); c.RelativeColumn(0.8f);
                        c.RelativeColumn(1.2f); c.RelativeColumn(0.8f);
                    });
                    void L(string v) => t.Cell().BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Background(BgRow).Text(v).Bold().FontSize(6.5f).FontColor(TextMuted);
                    void V(string v) => t.Cell().BorderBottom(0.5f).BorderColor(Border)
                        .Padding(2f).Text(v).FontSize(7f);

                    L("Pts. por infracción"); V(a.PuntosInfraccion.ToString());
                    L("Días suspensión"); V(a.DiasSuspension?.ToString() ?? "—");
                    L("Fecha inicio"); V(a.FechaInicioSuspension?.ToString("dd/MM/yyyy") ?? "—");
                    L("Fecha término"); V(a.FechaFinSuspension?.ToString("dd/MM/yyyy") ?? "—");
                });

                // ── Firmas
                inner.Item().PaddingTop(6).Row(row =>
                {
                    row.Spacing(8);
                    foreach (var label in new[] { "Firma del trabajador", "Firma del supervisor", "Persona que reporta" })
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(20).BorderBottom(1f).BorderColor(NavyLight);
                            c.Item().PaddingTop(2).Text(label).FontSize(6.5f).FontColor(TextMuted).AlignCenter();
                        });
                    }
                });

                inner.Item().PaddingTop(2)
                    .Text($"Reportado por: {a.PersonaReportaNombre ?? "—"}")
                    .FontSize(6.5f).FontColor(TextMuted);
            });
        });
    }

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(3).Background(Gold);
            r.ConstantItem(4);
            r.RelativeItem().PaddingVertical(2)
                .Text(title).Bold().FontSize(7.5f).FontColor(Navy);
        });
        col.Item().Height(0.5f).Background(Border);
    }
}
