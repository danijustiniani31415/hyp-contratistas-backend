using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Services
{
    /// <summary>Reporte de Programación Diaria de un proyecto/fecha — formato inspirado en el
    /// reporte original en PDF ("GMZ_..._Prog_Diaria_Sem_NN_Casco.pdf") que Planeamiento BIM
    /// reemplaza. No embebe las fotos de evidencia (solo las referencia por cantidad) para
    /// mantener el reporte liviano.</summary>
    public class PlaneamientoBimReportePdfService
    {
        private const string ColorPrimario = "#1B3A6B";
        private const string ColorSecundario = "#2D5AA0";
        private const string ColorGrupo = "#E8EEF7";

        public byte[] GenerarPdf(string proyectoNombre, string faseActual, CargaDiariaDto carga, PpcHistoricoDto ppc)
        {
            var nivelNombrePorId = carga.Zonas.SelectMany(z => z.Niveles).ToDictionary(n => n.Id, n => n.Nombre);
            var sectorNombrePorId = carga.Zonas.SelectMany(z => z.Niveles).SelectMany(n => n.Sectores)
                .GroupBy(s => s.Id).ToDictionary(g => g.Key, g => g.First().Nombre);
            var zonaNombrePorId = carga.Zonas.ToDictionary(z => z.Id, z => z.Nombre);
            var actividadNombrePorId = carga.Actividades.ToDictionary(a => a.Id, a => a.Nombre);

            var ppcDia = ppc.Dias.FirstOrDefault();
            var totalProgramadas = ppcDia?.TotalProgramadas ?? 0;
            var cumplidas = ppcDia?.Cumplidas ?? 0;
            var porcentajePpc = ppcDia?.PorcentajePpc ?? 0;

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
                                col.Item().Text("PROGRAMACIÓN DIARIA — PLANEAMIENTO BIM")
                                    .Bold().FontSize(12).FontColor(ColorPrimario);
                                col.Item().Text($"Proyecto: {proyectoNombre}  |  Fase: {faseActual}  |  Fecha: {carga.Fecha:dd/MM/yyyy}")
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
                        col.Item().Background(ColorSecundario).Padding(4)
                            .Text("RESUMEN DEL DÍA").Bold().FontSize(10).FontColor(Colors.White);

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                            });

                            void Celda(string label, string valor)
                            {
                                table.Cell().Background(ColorGrupo).Padding(3).Text(label).Bold().FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor).FontSize(8);
                            }

                            Celda("Programadas:", totalProgramadas.ToString());
                            Celda("Cumplidas:", cumplidas.ToString("0.##"));
                            Celda("PPC del día:", $"{porcentajePpc:0.##}%");
                            Celda("Meta PPC:", ppc.MetaPpc.HasValue ? $"{ppc.MetaPpc:0.##}%" : "-");
                        });

                        col.Item().PaddingTop(10).Background(ColorSecundario).Padding(4)
                            .Text("DETALLE POR ZONA / NIVEL / SECTOR / ACTIVIDAD").Bold().FontSize(10).FontColor(Colors.White);

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f); c.RelativeColumn(1); c.RelativeColumn(1);
                                c.RelativeColumn(1.5f); c.RelativeColumn(0.8f); c.RelativeColumn(1.5f);
                            });

                            void Encabezado(string texto) => table.Cell().Background(ColorGrupo).Padding(3).Text(texto).Bold().FontSize(8);
                            Encabezado("Zona"); Encabezado("Nivel"); Encabezado("Sector");
                            Encabezado("Actividad"); Encabezado("% Avance"); Encabezado("Causa");

                            foreach (var celda in carga.Celdas.OrderBy(c => c.ZonaId).ThenBy(c => c.NivelId).ThenBy(c => c.SectorId))
                            {
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(zonaNombrePorId.GetValueOrDefault(celda.ZonaId, "-")).FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(nivelNombrePorId.GetValueOrDefault(celda.NivelId, "-")).FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(sectorNombrePorId.GetValueOrDefault(celda.SectorId, "-")).FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(actividadNombrePorId.GetValueOrDefault(celda.ActividadId, "-")).FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{celda.PorcentajeAvance:0}%").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(celda.PorcentajeAvance == 100 ? "-" : (celda.CausaNombre ?? "-")).FontSize(8);
                            }

                            if (carga.Celdas.Count == 0)
                            {
                                table.Cell().ColumnSpan(6).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text("Sin registros de avance para esta fecha.").FontSize(8).Italic();
                            }
                        });

                        col.Item().PaddingTop(10).Background(ColorSecundario).Padding(4)
                            .Text("RESTRICCIONES ACTIVAS").Bold().FontSize(10).FontColor(Colors.White);

                        col.Item().PaddingTop(4).Column(restriccionesCol =>
                        {
                            if (carga.RestriccionesActivas.Count == 0)
                            {
                                restriccionesCol.Item().Text("Sin restricciones activas.").FontSize(8).Italic();
                            }
                            else
                            {
                                foreach (var restriccion in carga.RestriccionesActivas)
                                {
                                    restriccionesCol.Item().Text($"• [{restriccion.Estado}] {restriccion.Descripcion} (desde {restriccion.FechaCreacion:dd/MM/yyyy})")
                                        .FontSize(8);
                                }
                            }
                        });

                        col.Item().PaddingTop(10)
                            .Text($"Evidencia fotográfica: {carga.Evidencias.Count} foto(s) adjunta(s) para esta fecha.")
                            .FontSize(8).FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generado el ").FontSize(7).FontColor(Colors.Grey.Darken1);
                        t.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf();
        }
    }
}
