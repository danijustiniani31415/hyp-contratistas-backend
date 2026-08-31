using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Abril_Backend.Features.SsomaModule.PetsFeature.Application.Services;

// Vuelca el estado ACTUAL del PETS a un PDF, en el mismo orden que las pestañas de
// la pantalla de detalle — para que quien lo va completando pueda ver "cómo va
// quedando" sin abrir cada sección por separado. No reemplaza el documento oficial
// en SharePoint ni intenta replicar su formato exacto (esa plantilla vive en Word);
// esto es una vista de trabajo.
public static class PetsPdfService
{
    private static readonly (string Tipo, string Etiqueta)[] TiposEpp =
        [("basico", "Básico"), ("especifico", "Específico según la tarea"), ("emergencia", "Emergencia")];

    private static readonly (string Tipo, string Etiqueta)[] TiposRecurso =
        [("equipo", "Equipos"), ("herramienta", "Herramientas"), ("material", "Materiales")];

    public static Task<byte[]> GenerarPdfAsync(PetDetalleDto pet)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Header().PaddingBottom(8).BorderBottom(1).BorderColor(Colors.Grey.Medium).Column(col =>
                {
                    col.Item().Text(pet.Nombre).Bold().FontSize(14);
                    if (!string.IsNullOrWhiteSpace(pet.Codigo))
                        col.Item().Text($"Código: {pet.Codigo}").FontSize(9).FontColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    void Titulo(string texto) =>
                        col.Item().PaddingTop(10).Background(Colors.Grey.Lighten3).Padding(4).Text(texto).Bold().FontSize(11);

                    void Parrafo(string texto, int nivel = 0) =>
                        col.Item().PaddingLeft(nivel * 14).PaddingTop(2).Text(texto ?? string.Empty).FontSize(9);

                    void TextoLibre(string? texto)
                    {
                        if (string.IsNullOrWhiteSpace(texto)) { Parrafo("(Sin contenido)"); return; }
                        foreach (var linea in texto.Split('\n'))
                        {
                            if (!string.IsNullOrWhiteSpace(linea)) Parrafo(linea);
                        }
                    }

                    void Arbol(List<PetPasoDto> pasos)
                    {
                        if (pasos.Count == 0) { Parrafo("(Sin contenido)"); return; }
                        var hijosPorPadre = pasos.GroupBy(p => p.ParentId).ToDictionary(g => g.Key, g => g.OrderBy(p => p.Orden).ToList());

                        void Render(int? parentId, int nivel, string prefijoSubtitulo)
                        {
                            if (!hijosPorPadre.TryGetValue(parentId, out var hijos)) return;
                            var nSubtitulo = 0;
                            var nLetra = 0;
                            foreach (var h in hijos)
                            {
                                var numero = string.Empty;
                                if (h.Tipo == "subtitulo")
                                {
                                    nSubtitulo++;
                                    numero = string.IsNullOrEmpty(prefijoSubtitulo) ? $"{nSubtitulo}" : $"{prefijoSubtitulo}.{nSubtitulo}";
                                }
                                else if (h.Tipo == "letra")
                                {
                                    nLetra++;
                                    numero = LetraDesdeIndice(nLetra - 1);
                                }

                                var prefijo = h.Tipo switch
                                {
                                    "subtitulo" => $"{numero}. ",
                                    "letra" => $"{numero}. ",
                                    "guion" => "- ",
                                    _ => string.Empty
                                };
                                Parrafo($"{prefijo}{h.Descripcion}", nivel);
                                Render(h.Id, nivel + 1, h.Tipo == "subtitulo" ? numero : prefijoSubtitulo);
                            }
                        }
                        Render(null, 0, string.Empty);
                    }

                    void CatalogoPorTipo(List<PetItemSeleccionadoDto> items, (string Tipo, string Etiqueta)[] tipos)
                    {
                        foreach (var (tipo, etiqueta) in tipos)
                        {
                            Parrafo($"{etiqueta}:");
                            var delTipo = items.Where(i => i.Tipo == tipo).ToList();
                            if (delTipo.Count == 0) { Parrafo("(Sin ítems seleccionados)", 1); continue; }
                            foreach (var i in delTipo) Parrafo($"- {i.Descripcion}", 1);
                        }
                    }

                    Titulo("1. Introducción");
                    TextoLibre(pet.SeccionesTexto.GetValueOrDefault("introduccion"));

                    Titulo("2. Alcance");
                    TextoLibre(pet.SeccionesTexto.GetValueOrDefault("alcance"));

                    Titulo("3. Objetivo");
                    TextoLibre(pet.SeccionesTexto.GetValueOrDefault("objetivo"));

                    Titulo("4. Marco Legal");
                    if (pet.MarcoLegal.Count == 0) Parrafo("(Sin ítems seleccionados)");
                    foreach (var m in pet.MarcoLegal) Parrafo($"- {m.Descripcion}");

                    Titulo("5. Definiciones");
                    TextoLibre(pet.SeccionesTexto.GetValueOrDefault("definiciones"));

                    Titulo("6. Responsabilidades");
                    Arbol(pet.Responsabilidades);

                    Titulo("7. Gestión de personal — EPP");
                    CatalogoPorTipo(pet.Epp, TiposEpp);

                    Titulo("7. Gestión de personal — Recursos");
                    CatalogoPorTipo(pet.Recursos, TiposRecurso);

                    Titulo("8. Procedimiento de trabajo");
                    Arbol(pet.Pasos);

                    Titulo("9. Restricciones");
                    TextoLibre(pet.SeccionesTexto.GetValueOrDefault("restricciones"));

                    Titulo("10. Anexos");
                    if (pet.Anexos.Count == 0) Parrafo("(Sin anexos)");
                    foreach (var a in pet.Anexos) Parrafo($"- {a.Nombre}");

                    Titulo("Firmas");
                    foreach (var rol in new[] { "elaborado", "revisado", "aprobado" })
                    {
                        var f = pet.Firmas.GetValueOrDefault(rol);
                        var etiqueta = rol switch { "elaborado" => "Elaborado por", "revisado" => "Revisado por", _ => "Aprobado por" };
                        var fecha = f?.Fecha?.ToString("dd/MM/yyyy") ?? "-";
                        Parrafo($"{etiqueta}: {f?.Nombre ?? "-"} — {f?.Cargo ?? "-"} — {fecha}");
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Generado el ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm") + " UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }

    // a, b, c, ..., z, aa, ab, ... — igual que la numeración de letras en la pantalla.
    private static string LetraDesdeIndice(int i)
    {
        var s = string.Empty;
        var n = i;
        do
        {
            s = (char)('a' + (n % 26)) + s;
            n = (n / 26) - 1;
        } while (n >= 0);
        return s;
    }
}
