using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Services;

public class InspeccionPdfService
{
    private readonly IInspeccionSharePointService _sp;

    private const string ColorPrimario = "#1B3A6B";
    private const string ColorSecundario = "#2D5AA0";
    private const string ColorGrupo = "#E8EEF7";

    public InspeccionPdfService(IInspeccionSharePointService sp)
    {
        _sp = sp;
    }

    public async Task<byte[]> GenerarPdfAsync(InspeccionDetalleDto d)
    {
        if (d.EsColaborativa) return await GenerarPdfColaborativaAsync(d);

        var firmaInspector = await DescargarImagenAsync(d.FirmaInspectorUrl, "inspeccion-firmas");
        var firmaRepresentante = await DescargarImagenAsync(d.FirmaRepresentanteUrl, "inspeccion-firmas");

        var fotosHallazgos = new List<(InspeccionHallazgoDto Hallazgo, List<byte[]> Fotos)>();
        foreach (var h in d.Hallazgos)
        {
            var fotos = new List<byte[]>();
            foreach (var foto in h.Fotos.OrderBy(f => f.Orden))
            {
                var bytes = await DescargarImagenAsync(foto.Url, "inspeccion-fotos");
                if (bytes != null) fotos.Add(bytes);
            }
            if (fotos.Count > 0)
                fotosHallazgos.Add((h, fotos));
        }

        var grupos = d.Respuestas
            .OrderBy(r => r.Orden)
            .GroupBy(r => r.Categoria ?? "General")
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Header().Column(hdr =>
                {
                    hdr.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("REGISTRO DE INSPECCIONES — RM 050-2013-TR")
                                .Bold().FontSize(12).FontColor(ColorPrimario);
                            col.Item().Text($"Código: REG-SSOMA-INS-{d.Id:D4}  |  Fecha emisión: {DateTime.UtcNow:dd/MM/yyyy}")
                                .FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(90).AlignRight().AlignMiddle().Column(col =>
                        {
                            col.Item().Text("Abril Grupo").Bold().FontSize(9).AlignRight();
                            col.Item().Text("Inmobiliario").Bold().FontSize(9).AlignRight();
                        });
                    });
                    hdr.Item().PaddingTop(3).BorderBottom(2).BorderColor(ColorPrimario);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    // ── DATOS GENERALES ──────────────────────────────────────────────────
                    col.Item().Background(ColorSecundario).Padding(4)
                        .Text("DATOS GENERALES").Bold().FontSize(10).FontColor(Colors.White);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });

                        void Fila(string l1, string v1, string l2, string v2)
                        {
                            table.Cell().Background(ColorGrupo).Padding(3).Text(l1).Bold().FontSize(8);
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(v1).FontSize(8);
                            table.Cell().Background(ColorGrupo).Padding(3).Text(l2).Bold().FontSize(8);
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(v2).FontSize(8);
                        }

                        Fila("Empresa:", "Abril Grupo Inmobiliario", "Proyecto:", d.ProyectoNombre);
                        Fila("Tipo Inspección:", d.TipoNombre, "Ámbito:", d.TipoAmbito);
                        Fila("Fecha:", d.Fecha.ToString("dd/MM/yyyy"), "Modalidad:", d.EsPlanificada ? "Planificada" : "No Planificada");
                        Fila("Hora Inicio:", d.HoraInicio ?? "-", "Hora Fin:", d.HoraFin ?? "-");
                        Fila("Área:", d.Area ?? "-", "Responsable Área:", d.ResponsableArea ?? "-");
                        Fila("Inspector:", d.InspectorNombre ?? "-", "Cargo Inspector:", d.InspectorCargo ?? "-");
                        Fila("Empresa Inspector:", d.InspectorEmpresa ?? "-", "Estado:", d.Estado);
                        Fila("Representante:", d.RepresentanteNombre ?? "-", "Cargo:", d.RepresentanteCargo ?? "-");
                    });

                    // Resumen numérico
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        foreach (var label in new[] { "Total Items", "Cumple", "No Cumple", "N/A", "Tasa Cumplimiento" })
                            table.Cell().Background(ColorPrimario).Padding(4).AlignCenter()
                                .Text(label).FontColor(Colors.White).Bold().FontSize(8);

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(d.TotalItems.ToString()).FontSize(9);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(d.TotalCumple.ToString()).FontSize(9);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(d.TotalNoCumple.ToString()).FontSize(9);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(d.TotalNa.ToString()).FontSize(9);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter()
                            .Text(d.TasaCumplimiento.HasValue ? $"{d.TasaCumplimiento:F1}%" : "-").FontSize(9);
                    });

                    // ── CHECKLIST ────────────────────────────────────────────────────────
                    if (grupos.Count > 0)
                    {
                        col.Item().PaddingTop(10).Background(ColorSecundario).Padding(4)
                            .Text("LISTA DE VERIFICACIÓN").Bold().FontSize(10).FontColor(Colors.White);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(24);
                                c.RelativeColumn(5);
                                c.ConstantColumn(44);
                                c.ConstantColumn(52);
                                c.ConstantColumn(30);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                foreach (var label in new[] { "N°", "Descripción", "Cumple", "No Cumple", "N/A", "Observaciones" })
                                    h.Cell().Background(ColorPrimario).Padding(3).AlignCenter()
                                        .Text(label).FontColor(Colors.White).Bold().FontSize(8);
                            });

                            int n = 1;
                            foreach (var grupo in grupos)
                            {
                                table.Cell().ColumnSpan(6).Background(ColorGrupo).Padding(3)
                                    .Text(grupo.Key).Bold().FontSize(8);

                                foreach (var r in grupo)
                                {
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(n.ToString()).FontSize(8);
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(r.Pregunta).FontSize(8);
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter()
                                        .Text(r.Resultado == "Cumple" ? "OK" : "").Bold().FontSize(9).FontColor(Colors.Green.Darken2);
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter()
                                        .Text(r.Resultado == "NoCumple" ? "X" : "").Bold().FontSize(9).FontColor(Colors.Red.Darken2);
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter()
                                        .Text(r.Resultado == "NA" ? "N/A" : "").FontSize(8);
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(r.Observacion ?? "").FontSize(8);
                                    n++;
                                }
                            }
                        });
                    }

                    // ── HALLAZGOS ────────────────────────────────────────────────────────
                    if (d.Hallazgos.Count > 0)
                    {
                        col.Item().PaddingTop(10).Background(ColorSecundario).Padding(4)
                            .Text("HALLAZGOS").Bold().FontSize(10).FontColor(Colors.White);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.ConstantColumn(46);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.ConstantColumn(58);
                                c.RelativeColumn(3);
                                c.ConstantColumn(52);
                            });

                            table.Header(h =>
                            {
                                foreach (var label in new[] { "Descripción", "Tipo", "Área", "Responsable", "Fecha Límite", "Acción Correctiva", "Estado" })
                                    h.Cell().Background(ColorPrimario).Padding(3)
                                        .Text(label).FontColor(Colors.White).Bold().FontSize(8);
                            });

                            foreach (var h in d.Hallazgos)
                            {
                                var vencido = h.FechaLimite.HasValue && h.FechaLimite < DateTime.UtcNow && h.Estado != "Cerrado";
                                var estadoColor = h.Estado == "Cerrado" ? Colors.Green.Darken2
                                    : vencido ? Colors.Red.Darken2 : Colors.Orange.Darken2;

                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(h.Descripcion).FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(h.Tipo).FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(h.Area ?? "-").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(h.ResponsableNombre ?? "-").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter()
                                    .Text(h.FechaLimite?.ToString("dd/MM/yyyy") ?? "-").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(h.AccionCorrectiva ?? "-").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter()
                                    .Text(h.Estado).FontColor(estadoColor).Bold().FontSize(8);
                            }
                        });
                    }

                    // ── REGISTRO FOTOGRÁFICO ─────────────────────────────────────────────
                    if (fotosHallazgos.Count > 0)
                    {
                        col.Item().PaddingTop(10).Background(ColorSecundario).Padding(4)
                            .Text("REGISTRO FOTOGRÁFICO").Bold().FontSize(10).FontColor(Colors.White);

                        col.Item().PaddingTop(4).Grid(grid =>
                        {
                            grid.Columns(2);
                            grid.Spacing(6);
                            foreach (var (hallazgo, fotos) in fotosHallazgos)
                            {
                                foreach (var fotoBytes in fotos)
                                {
                                    grid.Item().Column(c =>
                                    {
                                        c.Item().Image(fotoBytes).FitWidth();
                                        c.Item().PaddingTop(2).AlignCenter()
                                            .Text(hallazgo.Descripcion.Length > 70
                                                ? hallazgo.Descripcion[..67] + "..."
                                                : hallazgo.Descripcion)
                                            .FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
                                    });
                                }
                            }
                        });
                    }

                    // ── CONCLUSIONES Y CAUSAS ────────────────────────────────────────────
                    if (!string.IsNullOrEmpty(d.DescripcionCausas) || !string.IsNullOrEmpty(d.Conclusiones))
                    {
                        col.Item().PaddingTop(10).Background(ColorSecundario).Padding(4)
                            .Text("CONCLUSIONES Y CAUSAS").Bold().FontSize(10).FontColor(Colors.White);

                        if (!string.IsNullOrEmpty(d.DescripcionCausas))
                        {
                            col.Item().PaddingTop(4).Background(ColorGrupo).Padding(3)
                                .Text("CAUSAS DE RESULTADOS DESFAVORABLES").Bold().FontSize(9);
                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(d.DescripcionCausas).FontSize(9);
                        }

                        if (!string.IsNullOrEmpty(d.Conclusiones))
                        {
                            col.Item().PaddingTop(4).Background(ColorGrupo).Padding(3)
                                .Text("CONCLUSIONES Y RECOMENDACIONES").Bold().FontSize(9);
                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(d.Conclusiones).FontSize(9);
                        }
                    }

                    // ── FIRMAS ───────────────────────────────────────────────────────────
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        void FirmaBloque(string titulo, string nombre, string cargo, byte[]? imgBytes)
                        {
                            row.RelativeItem().Column(bloque =>
                            {
                                bloque.Item().AlignCenter().Text(titulo).Bold().FontSize(9).FontColor(ColorPrimario);
                                bloque.Item().PaddingTop(4).Height(65).Border(1).BorderColor(Colors.Grey.Lighten2)
                                    .AlignCenter().AlignMiddle()
                                    .Element(c =>
                                    {
                                        if (imgBytes != null)
                                            c.Padding(4).Image(imgBytes).FitArea();
                                        else
                                            c.Text("......................................").FontColor(Colors.Grey.Medium).FontSize(9);
                                    });
                                bloque.Item().BorderTop(1).BorderColor(Colors.Grey.Medium);
                                bloque.Item().PaddingTop(3).AlignCenter().Text(nombre).Bold().FontSize(9);
                                bloque.Item().AlignCenter().Text(cargo).FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }

                        FirmaBloque("INSPECTOR", d.InspectorNombre ?? "", d.InspectorCargo ?? "", firmaInspector);
                        row.ConstantItem(24);
                        FirmaBloque("REPRESENTANTE DEL ÁREA", d.RepresentanteNombre ?? "", d.RepresentanteCargo ?? "", firmaRepresentante);
                    });
                });

                page.Footer().AlignRight()
                    .Text(t =>
                    {
                        t.Span("Generado el ").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm") + " UTC").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.Span("  Pág. ").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        t.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// Inspecciones gerenciales/cruzadas/coordinadores SSOMA: sin checklist, un hallazgo por fila,
    /// horizontal (A4 landscape) para que quepan fotos + recomendación + levantamiento en una sola tabla,
    /// igual al formato Excel "Inspecciones Cruzadas" que ya usan en obra.
    /// </summary>
    private async Task<byte[]> GenerarPdfColaborativaAsync(InspeccionDetalleDto d)
    {
        var fotosPorHallazgo = new Dictionary<int, List<byte[]>>();
        var evidenciaCierrePorHallazgo = new Dictionary<int, byte[]>();
        foreach (var h in d.Hallazgos)
        {
            var fotos = new List<byte[]>();
            foreach (var foto in h.Fotos.OrderBy(f => f.Orden))
            {
                var bytes = await DescargarImagenAsync(foto.Url, "inspeccion-fotos");
                if (bytes != null) fotos.Add(bytes);
            }
            if (fotos.Count > 0) fotosPorHallazgo[h.Id] = fotos;

            if (!string.IsNullOrEmpty(h.EvidenciaCierreUrl))
            {
                var evidencia = await DescargarImagenAsync(h.EvidenciaCierreUrl, "inspeccion-fotos");
                if (evidencia != null) evidenciaCierrePorHallazgo[h.Id] = evidencia;
            }
        }

        var hallazgosOrdenados = d.Hallazgos.OrderBy(h => h.Id).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

                page.Header().Column(hdr =>
                {
                    hdr.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("INSPECCIONES " + d.TipoNombre.ToUpperInvariant())
                                .Bold().FontSize(13).FontColor(ColorPrimario);
                            col.Item().Text($"{d.ProyectoNombre}  |  Fecha: {d.Fecha:dd/MM/yyyy}  |  Código: REG-SSOMA-INS-{d.Id:D4}")
                                .FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(140).AlignRight().AlignMiddle().Column(col =>
                        {
                            col.Item().Text("Abril Grupo Inmobiliario").Bold().FontSize(9).AlignRight();
                            col.Item().Text($"Estado: {d.Estado}").FontSize(8).AlignRight().FontColor(Colors.Grey.Darken2);
                        });
                    });
                    hdr.Item().PaddingTop(3).BorderBottom(2).BorderColor(ColorPrimario);
                    if (d.Participantes.Count > 0)
                    {
                        hdr.Item().PaddingTop(3).Text(
                            "Participantes: " + string.Join(" · ", d.Participantes.Select(p => p.Nombre)))
                            .FontSize(7).FontColor(Colors.Grey.Darken2);
                    }
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(18);   // N°
                        c.RelativeColumn(3);    // Descripción
                        c.RelativeColumn(3);    // Foto(s) observación
                        c.RelativeColumn(3);    // Recomendación
                        c.ConstantColumn(48);   // Criticidad
                        c.RelativeColumn(2);    // Responsable
                        c.ConstantColumn(48);   // Fecha límite
                        c.ConstantColumn(48);   // Estado
                        c.RelativeColumn(3);    // Foto de levantamiento
                    });

                    table.Header(h =>
                    {
                        foreach (var label in new[] { "N°", "Descripción", "Fotografía(s)", "Recomendación / Acción",
                            "Criticidad", "Responsable", "Fecha límite", "Estado", "Evidencia de levantamiento" })
                            h.Cell().Background(ColorPrimario).Padding(3).AlignCenter()
                                .Text(label).FontColor(Colors.White).Bold().FontSize(7.5f);
                    });

                    int n = 1;
                    foreach (var hz in hallazgosOrdenados)
                    {
                        var critColor = hz.Tipo == "Critico" ? Colors.Red.Darken1
                            : hz.Tipo == "Mayor" ? Colors.Orange.Darken1 : Colors.Grey.Darken1;
                        var estadoColor = hz.Estado == "Cerrado" ? Colors.Green.Darken2
                            : hz.Estado == "EnProceso" ? Colors.Orange.Darken2 : Colors.Red.Darken2;

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(n.ToString()).FontSize(8);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(hz.Descripcion).FontSize(7.5f);

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Element(c =>
                        {
                            if (fotosPorHallazgo.TryGetValue(hz.Id, out var fotos) && fotos.Count > 0)
                            {
                                c.Row(r =>
                                {
                                    foreach (var foto in fotos.Take(2))
                                        r.RelativeItem().Padding(1).Image(foto).FitWidth();
                                });
                            }
                            else c.Text("-").FontSize(7.5f);
                        });

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(hz.AccionCorrectiva ?? "-").FontSize(7.5f);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter()
                            .Text(hz.Tipo).Bold().FontSize(7.5f).FontColor(critColor);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(hz.ResponsableNombre ?? "-").FontSize(7.5f);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter()
                            .Text(hz.FechaLimite?.ToString("dd/MM/yy") ?? "-").FontSize(7.5f);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter()
                            .Text(hz.Estado).Bold().FontSize(7.5f).FontColor(estadoColor);

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Element(c =>
                        {
                            if (evidenciaCierrePorHallazgo.TryGetValue(hz.Id, out var evidencia))
                                c.Image(evidencia).FitWidth();
                            else c.Text("-").FontSize(7.5f);
                        });

                        n++;
                    }
                });

                page.Footer().AlignRight()
                    .Text(t =>
                    {
                        t.Span("Generado el ").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm") + " UTC").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.Span("  Pág. ").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        t.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
            });
        }).GeneratePdf();
    }

    private async Task<byte[]?> DescargarImagenAsync(string? url, string libraryContexto)
    {
        if (string.IsNullOrEmpty(url)) return null;
        try { return await _sp.DescargarAsync(url, libraryContexto); }
        catch { return null; }
    }
}
