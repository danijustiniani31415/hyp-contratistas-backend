using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Services;

/// <summary>Genera el PDF del acta (SIG-FO-17) que se envía por correo al marcar la reunión como
/// Realizada: encabezado, participantes con asistencia, y acuerdos con sus responsables.</summary>
public static class ActasReunionPdfService
{
    private static readonly string Primary = "#0F6E56";
    private static readonly string PrimaryDark = "#0A4F3E";
    private static readonly string BgRow = "#F5F8FA";
    private static readonly string Border = "#D8DBE2";
    private static readonly string TextMain = "#1A1A2E";
    private static readonly string TextMuted = "#5A6275";

    // ── Encabezado estandarizado (mismo formato que Convalidación/Amonestaciones) ──
    private const string Codigo = "SIG-FO-17";
    private const string Titulo = "ACTA DE REUNIÓN";

    public static byte[] GenerarPdf(ReunionDetalleDto d, byte[]? logoBytes)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor(TextMain));

                page.Header().Element(c => ComposeHeader(c, logoBytes));
                page.Content().PaddingTop(8).Element(c => ComposeBody(c, d));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Acta de Reunión — generada automáticamente por el sistema el ").FontSize(7).FontColor(TextMuted);
                    t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(7).FontColor(TextMuted);
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, byte[]? logoBytes)
    {
        container.Border(0.5f).BorderColor(Border).Row(row =>
        {
            // Logo — centrado H y V dentro del área fija (idéntico a Convalidación/Amonestaciones).
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

            // Columna derecha — Código/Versión/Fecha + Elab/Rev/Apro.
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
                MetaRow("Fecha:", "02/11/2023");

                metaCol.Item().BorderTop(0.5f).Row(subRow =>
                {
                    foreach (var (lbl, val, last) in new[]
                    {
                        ("Elab.:", "AGP", false),
                        ("Rev.:",  "GP",  false),
                        ("Apro.:", "GG",  true ),
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

    private static void ComposeBody(IContainer c, ReunionDetalleDto d)
    {
        c.Column(col =>
        {
            col.Item().Text($"N° {d.Numero} — {d.Tema}").Bold().FontSize(13).FontColor(PrimaryDark);
            col.Item().PaddingTop(6);
            col.Item().Element(cc => DatosGenerales(cc, d));
            col.Item().PaddingTop(10);
            SectionHeader(col, "PARTICIPANTES");
            col.Item().Element(cc => TablaParticipantes(cc, d));

            if (d.Acuerdos.Count > 0)
            {
                col.Item().PaddingTop(10);
                SectionHeader(col, "ACUERDOS");
                col.Item().Element(cc => TablaAcuerdos(cc, d));
            }

            if (!string.IsNullOrWhiteSpace(d.Observaciones))
            {
                col.Item().PaddingTop(10);
                SectionHeader(col, "OBSERVACIONES");
                col.Item().Text(d.Observaciones).FontSize(8.5f);
            }
        });
    }

    private static void DatosGenerales(IContainer c, ReunionDetalleDto d)
    {
        var ambito = d.ProjectDescription ?? d.AreaScopeDescripcion ?? "Toda la organización";
        var hora = d.HoraInicio.HasValue
            ? d.HoraInicio.Value.ToString("HH:mm") + (d.HoraFin.HasValue ? $" – {d.HoraFin.Value:HH:mm}" : "")
            : "Por confirmar";

        c.Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(1); cd.RelativeColumn(2); cd.RelativeColumn(1); cd.RelativeColumn(2); });
            void L(string v) => t.Cell().Padding(4).Background(BgRow).Text(v).Bold().FontSize(8).FontColor(TextMuted);
            void V(string v) => t.Cell().Padding(4).Text(v).FontSize(8.5f);

            L("Ámbito"); V(ambito);
            L("Fecha"); V(d.Fecha.ToString("dd/MM/yyyy"));
            L("Hora"); V(hora);
            L("Lugar"); V(d.Lugar ?? "—");
            L("Convocado por"); V(d.ConvocadoPor ?? "—");
            L("Estado"); V(d.ReunionEstado);
        });
    }

    private static void TablaParticipantes(IContainer c, ReunionDetalleDto d)
    {
        c.Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.ConstantColumn(24); cd.RelativeColumn(2.2f); cd.RelativeColumn(1.6f); cd.ConstantColumn(60); });

            t.Header(h =>
            {
                void H(string v) => h.Cell().Background(PrimaryDark).Padding(4).Text(v).Bold().FontSize(7.5f).FontColor(Colors.White);
                H("N°"); H("Nombre"); H("Cargo"); H("Asistió");
            });

            var n = 1;
            foreach (var p in d.Participantes)
            {
                t.Cell().Padding(3).BorderBottom(0.5f).BorderColor(Border).Text(n.ToString()).FontSize(8);
                t.Cell().Padding(3).BorderBottom(0.5f).BorderColor(Border).Text(p.Nombre).FontSize(8);
                t.Cell().Padding(3).BorderBottom(0.5f).BorderColor(Border).Text(p.Cargo ?? "—").FontSize(8);
                t.Cell().Padding(3).BorderBottom(0.5f).BorderColor(Border)
                    .Text(p.Asistio ? "Sí" : "No").FontSize(8).FontColor(p.Asistio ? PrimaryDark : "#DC2626");
                n++;
            }
        });
    }

    private static void TablaAcuerdos(IContainer c, ReunionDetalleDto d)
    {
        c.Column(col =>
        {
            var n = 1;
            foreach (var a in d.Acuerdos)
            {
                col.Item().PaddingBottom(6).Border(0.5f).BorderColor(Border).Padding(6).Column(item =>
                {
                    item.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"{n}. {a.Descripcion}").Bold().FontSize(8.5f);
                        r.ConstantItem(90).AlignRight().Text(a.ReunionAcuerdoEstado).FontSize(7.5f).FontColor(TextMuted);
                    });

                    if (!string.IsNullOrWhiteSpace(a.Acciones))
                        item.Item().PaddingTop(2).Text($"Acciones: {a.Acciones}").FontSize(8);

                    var responsables = a.Responsables.Count > 0
                        ? string.Join(", ", a.Responsables.Select(r => r.WorkerNombre))
                        : "—";
                    item.Item().PaddingTop(2).Text($"Responsable(s): {responsables}").FontSize(8).FontColor(TextMuted);

                    if (a.FechaProgramada.HasValue)
                        item.Item().PaddingTop(1).Text($"Fecha programada: {a.FechaProgramada:dd/MM/yyyy}").FontSize(7.5f).FontColor(TextMuted);
                });
                n++;
            }
        });
    }

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(3).Background(Primary);
            r.ConstantItem(4);
            r.RelativeItem().PaddingVertical(2).Text(title).Bold().FontSize(9).FontColor(PrimaryDark);
        });
        col.Item().Height(0.5f).Background(Border);
    }
}
