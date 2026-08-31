using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services;

public static class ConvalidacionPdfService
{
    // ── Paleta corporativa (misma que RAC / Inspecciones) ──────
    private const string ColorPrimario   = "#1B3A6B";
    private const string ColorSecundario = "#2D5AA0";
    private const string ColorGrupo      = "#E8EEF7";
    private static readonly string Border   = "#D8DBE2";
    private static readonly string TextMain = "#1A1A2E";
    private static readonly string TextMuted = "#5A6275";

    private const string Codigo = "SSO-FOR-148";
    private const string Titulo = "CONVALIDACIÓN DE EXÁMENES MÉDICOS";

    public static byte[] GenerarPdf(ConvalidacionDetalleDto d, byte[]? logoBytes)
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
            // Logo — centrado H y V dentro del área fija (idéntico a Amonestaciones)
            row.ConstantItem(90).AlignMiddle().AlignCenter().Padding(4).Element(logoEl =>
            {
                if (logoBytes != null)
                    logoEl.AlignMiddle().AlignCenter().Image(logoBytes).FitArea();
                else
                    logoEl.AlignMiddle().AlignCenter().Text("ABRIL").Bold().FontSize(8).AlignCenter();
            });

            row.ConstantItem(0.5f).Background(Colors.Grey.Lighten1);

            // Título centrado
            row.RelativeItem().AlignMiddle().AlignCenter()
                .Text(Titulo).Bold().FontSize(11).AlignCenter();

            row.ConstantItem(0.5f).Background(Colors.Grey.Lighten1);

            // Columna derecha — Código/Versión/Fecha + Elab/Rev/Apro
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
                MetaRow("Fecha:", "01/07/2026");

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

    private static void ComposeBody(IContainer container, ConvalidacionDetalleDto d)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "DATOS DEL TRABAJADOR");
                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1.6f); c.RelativeColumn(1); c.RelativeColumn(1.6f); });
                    void L(string v) => t.Cell().Background(ColorGrupo).Padding(5).Text(v).Bold().FontSize(8.5f);
                    void V(string v) => t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(v).FontSize(9.5f);

                    t.Cell().ColumnSpan(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                        .Text(d.WorkerNombre).Bold().FontSize(12);

                    L("DNI:"); V(d.WorkerDni);
                    L("Puesto:"); V(d.WorkerPuesto ?? "—");
                    L("Puesto de origen:"); V(d.PuestoOrigen ?? "—");
                    L("Puesto de destino:"); V(d.PuestoDestino ?? "—");
                    L("Categoría de origen:"); V(d.CategoriaOrigen ?? "—");
                    L("Categoría de destino:"); V(d.CategoriaDestino ?? "—");
                });
            });

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "EXAMEN MÉDICO OCUPACIONAL DE ORIGEN");
                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1.6f); c.RelativeColumn(1); c.RelativeColumn(1.6f); });
                    void L(string v) => t.Cell().Background(ColorGrupo).Padding(5).Text(v).Bold().FontSize(8.5f);
                    void V(string v) => t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(v).FontSize(9.5f);

                    L("Tipo de EMO:"); V(d.TipoEmo ?? "—");
                    L("Fecha del EMO:"); V(d.FechaEmoOrigen?.ToString("dd/MM/yyyy") ?? "—");
                    L("Aptitud emitida:"); V(d.AptitudOrigen ?? "—");
                    L("Empresa de origen:"); V(d.EmpresaOrigen ?? "—");
                });
            });

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "CONVALIDACIÓN");
                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1.6f); c.RelativeColumn(1); c.RelativeColumn(1.6f); });
                    void L(string v) => t.Cell().Background(ColorGrupo).Padding(5).Text(v).Bold().FontSize(8.5f);
                    void V(string v, string? color = null) => t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(5).Text(v).FontSize(9.5f).FontColor(color ?? TextMain);

                    L("Empresa de destino:"); V(d.EmpresaDestino ?? "—");
                    L("Fecha de convalidación:"); V(d.FechaConvalidacion.ToString("dd/MM/yyyy"));
                    L("Resultado:");
                    V(d.Resultado, d.Resultado switch
                    {
                        "Aprobada" or "Aprobada con Observaciones" => "#166534",
                        "Rechazada" => "#B91C1C",
                        _ => TextMuted,
                    });
                    L("Vigencia hasta:"); V(d.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "—");
                });
            });

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "ANÁLISIS DE RIESGO POR CAMBIO DE PUESTO");
                inner.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1.6f); c.RelativeColumn(1); c.RelativeColumn(1.6f); });
                    void L(string v) => t.Cell().Background(ColorGrupo).Padding(5).Text(v).Bold().FontSize(8.5f);
                    void V(string v, string? color = null) => t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(5).Text(v).FontSize(9.5f).FontColor(color ?? TextMain);

                    L("Clasificación origen:"); V(d.ObraOficinaStaffOrigenNombre ?? "—");
                    L("Clasificación destino:"); V(d.ObraOficinaStaffDestinoNombre ?? "—");
                    L("Riesgo origen:"); V(d.RiesgoOrigen ?? "—", RiesgoColor(d.RiesgoOrigen));
                    L("Riesgo destino:"); V(d.RiesgoDestino ?? "—", RiesgoColor(d.RiesgoDestino));
                });
                inner.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                    .Text(DeclaracionRiesgo(d))
                    .FontSize(8.5f).FontColor(d.CambioRiesgo ? "#B91C1C" : TextMuted).LineHeight(1.4f);
            });

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "OBSERVACIONES");
                inner.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).MinHeight(60)
                    .Text(string.IsNullOrWhiteSpace(d.Notas) ? "Sin observaciones." : d.Notas)
                    .FontSize(9.5f).LineHeight(1.4f)
                    .FontColor(string.IsNullOrWhiteSpace(d.Notas) ? Colors.Grey.Medium : TextMain);
            });

            col.Item().Column(inner =>
            {
                SectionHeader(inner, "DECLARACIÓN");
                inner.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                    .Text("El médico ocupacional que suscribe declara haber evaluado la documentación del " +
                          "examen médico ocupacional de origen del trabajador indicado, y certifica el resultado " +
                          "de convalidación consignado en el presente documento conforme a los protocolos vigentes " +
                          "(R.M. N° 312-2011/MINSA).")
                    .FontSize(8.5f).FontColor(TextMuted).LineHeight(1.4f);
            });

            col.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(240).Column(c =>
                {
                    c.Item().Height(36).BorderBottom(1.2f).BorderColor(ColorPrimario);
                    c.Item().PaddingTop(4).Text(d.MedicoNombre ?? "—").Bold().FontSize(9.5f).AlignCenter();
                    c.Item().AlignCenter().Text(t =>
                    {
                        t.AlignCenter();
                        if (!string.IsNullOrWhiteSpace(d.MedicoRegistroCmp))
                            t.Span($"CMP {d.MedicoRegistroCmp}").FontSize(8).FontColor(TextMuted);
                        if (!string.IsNullOrWhiteSpace(d.MedicoEspecialidad))
                            t.Span($" — {d.MedicoEspecialidad}").FontSize(8).FontColor(TextMuted);
                    });
                    c.Item().Text("Médico Ocupacional").FontSize(8).FontColor(TextMuted).AlignCenter();
                });
                row.RelativeItem();
            });
        });
    }

    private static string RiesgoColor(string? riesgo) => riesgo switch
    {
        "Alto" => "#B91C1C",
        "Bajo" => "#166534",
        _ => "#5A6275",
    };

    /// <summary>
    /// Texto legal del análisis de riesgo por cambio de puesto, conforme a la R.M. 312-2011/MINSA:
    /// el EMO evalúa al trabajador para los agentes de riesgo específicos de un puesto. Si el
    /// puesto destino mantiene o reduce esa exposición, la convalidación es procedente; si la
    /// aumenta (Oficina Central → Staff/Obra), no lo es y corresponde un EMO nuevo.
    /// </summary>
    private static string DeclaracionRiesgo(ConvalidacionDetalleDto d)
    {
        if (string.IsNullOrEmpty(d.RiesgoOrigen) || string.IsNullOrEmpty(d.RiesgoDestino))
            return "No se registró la clasificación de riesgo del puesto de origen y/o destino.";

        if (d.CambioRiesgo)
            return "El puesto de destino implica mayor exposición a riesgo (Staff/Obra) que el evaluado " +
                   "en el EMO de origen (Oficina Central). Conforme a la R.M. N° 312-2011/MINSA, no procede " +
                   "la convalidación: corresponde realizar un nuevo Examen Médico Ocupacional bajo el " +
                   "protocolo aplicable al puesto destino.";

        if (d.RiesgoOrigen == d.RiesgoDestino)
            return "El puesto de destino mantiene un perfil de riesgo similar al evaluado en el EMO de " +
                   "origen, por lo que la convalidación es procedente conforme a la R.M. N° 312-2011/MINSA.";

        return "El puesto de destino implica un perfil de riesgo igual o menor al evaluado en el EMO de " +
               "origen (Staff/Obra → Oficina Central), por lo que la convalidación es procedente conforme " +
               "a la R.M. N° 312-2011/MINSA.";
    }

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().Background(ColorSecundario).Padding(5)
            .Text(title).Bold().FontSize(9.5f).FontColor(Colors.White);
    }
}
