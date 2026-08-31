using Abril_Backend.Features.Ssoma.Rac.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.Ssoma.Rac.Services;

public static class RacPdfService
{
    public static Task<byte[]> GenerarPdfAsync(RacDetalleDto rac, List<(string Tipo, byte[] Bytes)> fotoBytes)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));

                page.Header().PaddingBottom(8).BorderBottom(1).BorderColor(Colors.Grey.Medium).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("REGISTRO DE ACTO / CONDICIÓN SUBESTÁNDAR (RAC)")
                            .Bold().FontSize(13);
                        col.Item().Text($"Código: {rac.Codigo}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    });
                    row.ConstantItem(100).AlignRight().AlignMiddle()
                        .Text(rac.Estado).Bold().FontSize(11)
                        .FontColor(rac.Estado == "Cerrado" ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });

                        void Fila(string label1, string val1, string label2, string val2)
                        {
                            table.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text(label1).Bold().FontSize(9);
                            table.Cell().Padding(4).Text(val1).FontSize(9);
                            table.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text(label2).Bold().FontSize(9);
                            table.Cell().Padding(4).Text(val2).FontSize(9);
                        }

                        Fila("Tipo", rac.Tipo, "Severidad", rac.Severidad);
                        Fila("Categoría", rac.CategoriaNombre, "Ámbito", rac.CategoriaAmbito);
                        Fila("Proyecto", rac.ProyectoNombre ?? "-", "Piso / Zona", rac.ProyectoPiso ?? "-");
                        Fila("Fecha Reporte", rac.FechaReporte.ToString("dd/MM/yyyy HH:mm"),
                             "Plazo Levantamiento", rac.PlazoLevantamiento?.ToString("dd/MM/yyyy") ?? "-");
                        Fila("Empresa Reportante", rac.EmpresaReportanteNombre ?? "-",
                             "Empresa Reportada", rac.EmpresaReportadaNombre ?? "-");
                        Fila("Reportante", rac.EsAnonimoReportante ? "Anónimo" : (rac.ReportanteNombre ?? "-"),
                             "Observado", rac.EsAnonimoObservado ? "Anónimo" : (rac.ObservadoNombre ?? "-"));
                        Fila("Aplica Penalidad", rac.AplicaPenalidad ? "Sí" : "No",
                             "Fecha Cierre", rac.FechaCierre?.ToString("dd/MM/yyyy") ?? "-");
                    });

                    col.Item().PaddingTop(10).Column(inner =>
                    {
                        inner.Item().Background(Colors.Grey.Lighten3).Padding(4).Text("Descripción").Bold().FontSize(9);
                        inner.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                            .Text(rac.Descripcion).FontSize(9);
                    });

                    if (!string.IsNullOrEmpty(rac.PlanAccion))
                    {
                        col.Item().PaddingTop(8).Column(inner =>
                        {
                            inner.Item().Background(Colors.Grey.Lighten3).Padding(4).Text("Plan de Acción").Bold().FontSize(9);
                            inner.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(rac.PlanAccion).FontSize(9);
                        });
                    }

                    if (!string.IsNullOrEmpty(rac.CierreDescripcion))
                    {
                        col.Item().PaddingTop(8).Column(inner =>
                        {
                            inner.Item().Background(Colors.Grey.Lighten3).Padding(4).Text("Descripción de Cierre").Bold().FontSize(9);
                            inner.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(rac.CierreDescripcion).FontSize(9);
                        });
                    }

                    if (fotoBytes.Count > 0)
                    {
                        col.Item().PaddingTop(10).Column(inner =>
                        {
                            inner.Item().Background(Colors.Grey.Lighten3).Padding(4)
                                .Text($"Fotos ({fotoBytes.Count})").Bold().FontSize(9);

                            // Grilla de 3 columnas y altura fija por foto: con FitWidth() sin límite de
                            // alto, 1-2 fotos ya empujaban el documento a una segunda hoja. El RAC debe
                            // caber siempre en una sola página.
                            inner.Item().PaddingTop(6).Grid(grid =>
                            {
                                grid.Columns(3);
                                grid.Spacing(6);
                                foreach (var (tipo, bytes) in fotoBytes)
                                {
                                    grid.Item().Column(c =>
                                    {
                                        c.Item().MaxHeight(110).Image(bytes).FitArea();
                                        c.Item().AlignCenter().Text(tipo).FontSize(8)
                                            .FontColor(Colors.Grey.Darken1);
                                    });
                                }
                            });
                        });
                    }
                });

                page.Footer().AlignRight()
                    .Text(t =>
                    {
                        t.Span("Generado el ").FontSize(8).FontColor(Colors.Grey.Medium);
                        t.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm") + " UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }
}
