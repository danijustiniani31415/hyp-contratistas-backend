using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Services;

public static class MintraPdfService
{
    public static byte[] Generar(FlashReportDetalleDto fr)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("REGISTRO DE ACCIDENTES DE TRABAJO").Bold().FontSize(11);
                    col.Item().AlignCenter().Text("(Art. 33° D.S. N° 005-2012-TR)").FontSize(8);
                    col.Item().Height(4);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Razón Social: ABRIL GRUPO INMOBILIARIO").Bold();
                        row.ConstantItem(120).AlignRight().Text($"Código: {fr.Codigo}").Bold();
                    });
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Spacing(6);

                    // SECCIÓN 1: DATOS DEL EMPLEADOR
                    col.Item().Element(e => SeccionHeader(e, "1. DATOS DEL EMPLEADOR"));
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                        Celda(t, "Razón Social", "ABRIL GRUPO INMOBILIARIO");
                        Celda(t, "RUC", "—");
                        Celda(t, "Actividad Económica", "Construcción / Inmobiliaria");
                        Celda(t, "Dirección", "—");
                        Celda(t, "Proyecto / Centro de Trabajo", fr.ProyectoNombre);
                        Celda(t, "Empresa Contratista", fr.ContributorNombre ?? fr.EmpresaAbrilNombre ?? "—");
                    });

                    // SECCIÓN 2: DATOS DEL TRABAJADOR ACCIDENTADO
                    var trab = fr.Trabajadores.Count > 0 ? fr.Trabajadores[0] : null;
                    col.Item().Element(e => SeccionHeader(e, "2. DATOS DEL TRABAJADOR ACCIDENTADO"));
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                        Celda(t, "Apellidos y Nombres", trab?.TrabajadorNombre ?? fr.TrabajadorNombre ?? "—");
                        Celda(t, "DNI", "—");
                        Celda(t, "Cargo / Ocupación", trab?.PuestoTrabajo ?? fr.PuestoTrabajo ?? "—");
                        Celda(t, "Edad", (trab?.Edad ?? fr.Edad)?.ToString() ?? "—");
                        Celda(t, "Años de Experiencia", (trab?.AniosExperiencia ?? fr.AniosExperiencia)?.ToString() ?? "—");
                        Celda(t, "Turno de Trabajo", fr.Turno ?? "—");
                    });

                    // SECCIÓN 3: DATOS DEL ACCIDENTE DE TRABAJO
                    col.Item().Element(e => SeccionHeader(e, "3. DATOS DEL ACCIDENTE DE TRABAJO"));
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                        Celda(t, "Fecha del Accidente", fr.Fecha.ToString("dd/MM/yyyy"));
                        Celda(t, "Hora del Accidente", fr.Hora?.ToString() ?? "—");
                        Celda(t, "Lugar Exacto", fr.LugarExacto);
                        Celda(t, "Tipo de Accidente", fr.TipoNombre);
                        Celda(t, "Mecanismo / Tipo de Contacto", fr.TipoContacto ?? "—");
                        Celda(t, "Parte del Cuerpo Afectada", (trab?.ParteAfectadaNombre ?? fr.ParteAfectadaNombre) ?? "—");
                        Celda(t, "Consecuencia Real", NivelConsecuencia(fr.ConsecuenciaRealPersonal));
                        Celda(t, "Consecuencia Potencial", NivelConsecuencia(fr.ConsecuenciaPotencialPersonal));
                        Celda(t, "Atención Médica", fr.AtencionMedica ?? "—");
                    });

                    // SECCIÓN 4: DESCRIPCIÓN DEL ACCIDENTE
                    col.Item().Element(e => SeccionHeader(e, "4. DESCRIPCIÓN DEL ACCIDENTE / INCIDENTE"));
                    col.Item().Border(0.5f).Padding(6).MinHeight(60).Text(fr.Descripcion).FontSize(8);

                    // SECCIÓN 5: ACCIONES INMEDIATAS
                    col.Item().Element(e => SeccionHeader(e, "5. MEDIDAS CORRECTIVAS INMEDIATAS"));
                    col.Item().Border(0.5f).Padding(6).MinHeight(40).Text(fr.AccionesInmediatas ?? "—").FontSize(8);

                    // SECCIÓN 6: RESPONSABLE DEL REGISTRO
                    col.Item().Element(e => SeccionHeader(e, "6. RESPONSABLE DEL REGISTRO"));
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                        Celda(t, "Nombre", fr.ElaboradoPorNombre ?? "—");
                        Celda(t, "Cargo", fr.ElaboradoPorCargo ?? "—");
                        Celda(t, "Fecha", fr.CreatedAt.ToString("yyyy-MM-dd")[..10]);
                    });

                    // Firma
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderTop(0.5f).PaddingTop(4).AlignCenter()
                                .Text("Firma del Responsable de SSOMA").FontSize(7);
                        });
                        row.ConstantItem(60);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderTop(0.5f).PaddingTop(4).AlignCenter()
                                .Text("Firma del Trabajador").FontSize(7);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Registro de Accidentes de Trabajo | D.S. N° 005-2012-TR | Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void SeccionHeader(IContainer c, string title)
    {
        c.Background(Colors.Grey.Darken2).Padding(4)
            .Text(title).Bold().FontColor(Colors.White).FontSize(8);
    }

    private static void Celda(TableDescriptor t, string label, string valor)
    {
        t.Cell().Border(0.5f).Padding(3).Text(label).Bold().FontSize(7.5f);
        t.Cell().ColumnSpan(2).Border(0.5f).Padding(3).Text(valor).FontSize(7.5f);
    }

    private static string NivelConsecuencia(int? n) => n switch
    {
        1 => "N1 — Sin daño",
        2 => "N2 — Primeros auxilios",
        3 => "N3 — Lesión leve (Accidente leve)",
        4 => "N4 — Lesión grave / Tiempo perdido",
        5 => "N5 — Fatalidad / Incapacidad permanente",
        6 => "N6 — Fatalidades múltiples",
        _ => "—"
    };
}
