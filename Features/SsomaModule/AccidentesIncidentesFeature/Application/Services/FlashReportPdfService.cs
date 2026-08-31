using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Services;

public static class FlashReportPdfService
{
    // ── Paleta corporativa (igual que AmonestacionPdfService) ──────────
    private static readonly string Navy     = "#0D1F3C";
    private static readonly string NavyMid  = "#1E3A5F";
    private static readonly string Gold     = "#C9A84C";
    private static readonly string BgRow    = "#F5F6F8";
    private static readonly string Border   = "#D8DBE2";
    private static readonly string TextMain = "#1A1A2E";
    private static readonly string TextMute = "#5A6275";

    private static readonly string LogoPath = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "abril-logo.png"),
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "abril-logo.png"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot", "images", "abril-logo.png"),
    }.FirstOrDefault(File.Exists) ?? "";

    private static readonly string[] NivelConsecuencia =
    [
        "", "N1 — Sin daño", "N2 — Primeros auxilios",
        "N3 — Lesión leve (Accidente leve)", "N4 — Lesión grave / Tiempo perdido",
        "N5 — Fatalidad / Incapacidad permanente", "N6 — Fatalidades múltiples"
    ];

    public static byte[] Generar(FlashReportDetalleDto fr, byte[]? foto1, byte[]? foto2)
    {
        var logo = File.Exists(LogoPath) ? File.ReadAllBytes(LogoPath) : null;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.2f, Unit.Centimetre);
                page.MarginVertical(0.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Arial").FontColor(TextMain));

                page.Header().Element(c => ComposeHeader(c, fr, logo));
                page.Content().PaddingTop(6).Element(c => ComposeContent(c, fr, foto1, foto2));
                page.Footer().BorderTop(0.5f).BorderColor(Border).PaddingTop(3)
                    .Row(row =>
                    {
                        row.RelativeItem().Text($"SSO-FO-035 | Flash Report | {fr.Codigo}").FontSize(6.5f).FontColor(TextMute);
                        row.ConstantItem(80).AlignRight().Text(x =>
                        {
                            x.Span("Página ").FontSize(6.5f).FontColor(TextMute);
                            x.CurrentPageNumber().FontSize(6.5f).FontColor(TextMute);
                            x.Span(" de ").FontSize(6.5f).FontColor(TextMute);
                            x.TotalPages().FontSize(6.5f).FontColor(TextMute);
                        });
                    });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer c, FlashReportDetalleDto fr, byte[]? logo)
    {
        c.Column(col =>
        {
            col.Item().Height(1).Background(Border);

            col.Item().Border(0.5f).BorderColor(Border).Row(row =>
            {
                // Logo
                row.ConstantItem(90).AlignMiddle().AlignCenter().Padding(4).Element(logoEl =>
                {
                    if (logo != null)
                        logoEl.AlignMiddle().AlignCenter().Image(logo).FitArea();
                    else
                        logoEl.AlignMiddle().AlignCenter()
                            .Text("ABRIL GRUPO INMOBILIARIO")
                            .Bold().FontSize(8).FontColor(Navy);
                });

                row.ConstantItem(0.5f).Background(Colors.Grey.Lighten1);

                row.RelativeItem().AlignMiddle().AlignCenter().Column(tc =>
                {
                    tc.Item().AlignCenter().Text("FLASH REPORT").Bold().FontSize(15).FontColor(Navy);
                    tc.Item().AlignCenter().Text(fr.Codigo).Bold().FontSize(10).FontColor(NavyMid);
                    tc.Item().AlignCenter().Text(fr.TipoNombre).FontSize(7.5f).FontColor(TextMute);
                });

                row.ConstantItem(0.5f).Background(Colors.Grey.Lighten1);

                row.ConstantItem(110).Column(metaCol =>
                {
                    void MetaRow(string label, string valor, bool last = false)
                    {
                        metaCol.Item()
                            .BorderBottom(last ? 0f : 0.5f).BorderColor(Border)
                            .Padding(2).Row(r =>
                            {
                                r.AutoItem().Text(label).Bold().FontSize(7).FontColor(TextMute);
                                r.ConstantItem(2);
                                r.RelativeItem().Text(valor).FontSize(7);
                            });
                    }
                    MetaRow("Código:",  "SSO-FO-035");
                    MetaRow("Versión:", "01");
                    MetaRow("Fecha:",   "01/12/2023");
                    metaCol.Item().BorderTop(0.5f).BorderColor(Border).Row(subRow =>
                    {
                        foreach (var (lbl, val, last) in new[]
                        {
                            ("Elab.:", "SSOMA",  false),
                            ("Rev.:",  "JSSOMA", false),
                            ("Apro.:", "GP",     true ),
                        })
                        {
                            subRow.RelativeItem()
                                .BorderRight(last ? 0f : 0.5f).BorderColor(Border)
                                .Padding(2).Column(cell =>
                                {
                                    cell.Item().Text(lbl).Bold().FontSize(6).FontColor(TextMute);
                                    cell.Item().Text(val).FontSize(6.5f);
                                });
                        }
                    });
                });
            });

            col.Item().Height(1).Background(Border);
        });
    }


    private static void ComposeContent(IContainer c, FlashReportDetalleDto fr, byte[]? foto1, byte[]? foto2)
    {
        c.Column(col =>
        {
            col.Spacing(4);

            // Fila 1: Proyecto + Empresa (lado a lado)
            col.Item().Row(row =>
            {
                row.Spacing(6);
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Element(e => SecHeader(e, "DATOS DEL EVENTO"));
                    inner.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(3); });
                        DataRow(t, "Proyecto", fr.ProyectoNombre);
                        DataRow(t, "Fecha / Hora", $"{fr.Fecha:dd/MM/yyyy} {(fr.Hora.HasValue ? fr.Hora.Value.ToString(@"hh\:mm") : "")}");
                        DataRow(t, "Etapa", fr.EtapaProyectoNombre ?? "—");
                        DataRow(t, "Partida", fr.PartidaNombre ?? "—");
                        DataRow(t, "Lugar exacto", fr.LugarExacto);
                        DataRow(t, "Turno", fr.Turno ?? "—");
                        DataRow(t, "Tipo contacto", fr.TipoContacto ?? "—");
                    });
                });

                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Element(e => SecHeader(e, "EMPRESA INVOLUCRADA"));
                    inner.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(3); });
                        DataRow(t, "Razón social", fr.EmpresaAbrilNombre ?? fr.ContributorNombre ?? "—");
                        DataRow(t, "Jefe inmediato", fr.JefeInmediatoNombre ?? "—");
                    });

                    inner.Item().PaddingTop(4).Element(e => SecHeader(e, "DAÑOS Y CONSECUENCIAS"));
                    inner.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(3); });
                        DataRow(t, "Daño proceso/equipo", fr.DanoProceso ?? "—");
                        DataRow(t, "Consecuencia real", Nivel(fr.ConsecuenciaRealPersonal));
                        DataRow(t, "Consecuencia potencial", Nivel(fr.ConsecuenciaPotencialPersonal));
                        DataRow(t, "Atención médica", fr.AtencionMedica ?? "—");
                        if (!string.IsNullOrEmpty(fr.CentroAtencion))
                            DataRow(t, "Centro", fr.CentroAtencion);
                    });
                });
            });

            // Trabajadores
            col.Item().Element(e => SecHeader(e, "TRABAJADOR(ES) AFECTADO(S)"));
            var trabajadores = fr.Trabajadores.Count > 0
                ? fr.Trabajadores
                : fr.TrabajadorNombre != null
                    ? new[] { new TrabajadorAfectadoDto {
                        TrabajadorNombre = fr.TrabajadorNombre,
                        PuestoTrabajo = fr.PuestoTrabajo,
                        Edad = fr.Edad,
                        AniosExperiencia = fr.AniosExperiencia,
                        CelularTrabajador = fr.CelularTrabajador,
                        ParteAfectadaNombre = fr.ParteAfectadaNombre
                      }}.ToList()
                    : new List<TrabajadorAfectadoDto>();

            if (trabajadores.Count == 0)
            {
                col.Item().Padding(3).Text("Sin trabajador afectado (incidente patrimonial)").FontSize(7.5f).Italic().FontColor(TextMute);
            }
            else
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3); cd.RelativeColumn(2);
                        cd.RelativeColumn(1); cd.RelativeColumn(1); cd.RelativeColumn(2);
                    });
                    t.Header(h =>
                    {
                        foreach (var lbl in new[] { "Nombre", "Puesto", "Edad", "Años exp.", "Parte afectada" })
                            h.Cell().Background(Navy).Padding(3).Text(lbl).FontColor(Colors.White).Bold().FontSize(7);
                    });
                    foreach (var tr in trabajadores)
                    {
                        t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(2).Text(tr.TrabajadorNombre);
                        t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(2).Text(tr.PuestoTrabajo ?? "—");
                        t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(2).Text(tr.Edad?.ToString() ?? "—");
                        t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(2).Text(tr.AniosExperiencia?.ToString() ?? "—");
                        t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(2).Text(tr.ParteAfectadaNombre ?? "—");
                    }
                });
            }

            // Descripción y acciones (columnas)
            col.Item().Row(row =>
            {
                row.Spacing(6);
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Element(e => SecHeader(e, "DESCRIPCIÓN DEL EVENTO"));
                    inner.Item().Border(0.5f).BorderColor(Border).Padding(4).MinHeight(40)
                        .Text(fr.Descripcion).FontSize(7.5f);
                });
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Element(e => SecHeader(e, "ACCIONES INMEDIATAS"));
                    inner.Item().Border(0.5f).BorderColor(Border).Padding(4).MinHeight(40)
                        .Text(fr.AccionesInmediatas ?? "—").FontSize(7.5f);
                });
            });

            // Fotos
            if (foto1 != null || foto2 != null)
            {
                col.Item().Element(e => SecHeader(e, "FOTOGRAFÍAS"));
                col.Item().Row(row =>
                {
                    row.Spacing(8);
                    if (foto1 != null) row.RelativeItem().MaxHeight(80).Image(foto1).FitArea();
                    if (foto2 != null) row.RelativeItem().MaxHeight(80).Image(foto2).FitArea();
                    if (foto1 == null || foto2 == null) row.RelativeItem();
                });
            }

            // Elaborado por + firma
            col.Item().Row(row =>
            {
                row.Spacing(6);
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Element(e => SecHeader(e, "ELABORADO POR"));
                    inner.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(3); });
                        DataRow(t, "Nombre", fr.ElaboradoPorNombre ?? "—");
                        DataRow(t, "Cargo", fr.ElaboradoPorCargo ?? "—");
                        DataRow(t, "Email", fr.ElaboradoPorEmail ?? "—");
                        DataRow(t, "Teléfono", fr.ElaboradoPorTelefono ?? "—");
                    });
                });
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Element(e => SecHeader(e, "FIRMA Y APROBACIÓN"));
                    inner.Item().Border(0.5f).BorderColor(Border).Padding(4).MinHeight(35)
                        .Column(fc =>
                        {
                            fc.Item().AlignCenter().Text("").FontSize(20); // espacio para firma
                            fc.Item().BorderTop(0.5f).BorderColor(Border).PaddingTop(2)
                                .AlignCenter().Text("Firma del Responsable de SSOMA").FontSize(6.5f).FontColor(TextMute);
                        });
                });
            });
        });
    }

    private static void SecHeader(IContainer c, string title)
    {
        c.Background(Navy).PaddingVertical(2).PaddingHorizontal(4)
         .Text(title).Bold().FontColor(Colors.White).FontSize(7.5f);
    }

    private static void DataRow(TableDescriptor t, string label, string value)
    {
        t.Cell().Background(BgRow).Border(0.5f).BorderColor(Border).Padding(2)
            .Text(label).Bold().FontSize(7).FontColor(TextMute);
        t.Cell().Border(0.5f).BorderColor(Border).Padding(2)
            .Text(value).FontSize(7.5f);
    }

    private static void MetaRow(TableDescriptor t, string label, string value)
    {
        t.Cell().Padding(1).Text(label).Bold().FontSize(6.5f).FontColor(TextMute);
        t.Cell().Padding(1).Text(value).FontSize(6.5f);
    }

    private static string Nivel(int? n) =>
        n.HasValue && n.Value >= 1 && n.Value <= 6 ? NivelConsecuencia[n.Value] : "—";
}
