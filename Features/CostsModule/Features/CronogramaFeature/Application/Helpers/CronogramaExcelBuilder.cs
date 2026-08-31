using System.Globalization;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Dtos;
using ClosedXML.Excel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Helpers
{
    /// <summary>
    /// Construye el Excel del cronograma de ejecución (diagrama de barras por mes),
    /// con el mismo estilo de la Hoja Resumen de adjudicaciones.
    /// </summary>
    public static class CronogramaExcelBuilder
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static readonly string[] Meses =
        {
            "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
            "JULIO", "AGOSTO", "SETIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE"
        };

        // Geometría de las barras (en píxeles). MonthPx ≈ ancho real de una columna de mes
        // (width 11 → ≈ 78 px en Excel con Calibri 11).
        private const int MonthPx = 78;
        private const double DataRowPt = 16;   // alto de fila de datos (puntos)
        private const int DataRowPx = 21;      // ≈ 16pt en píxeles
        private const int BarHeightPx = 8;

        /// <summary>PNG negro sólido (8×8) reutilizable para dibujar las barras de la línea de tiempo.</summary>
        private static readonly byte[] BarPng = BuildSolidPng(new Rgba32(0, 0, 0));

        private static byte[] BuildSolidPng(Rgba32 color)
        {
            using var img = new Image<Rgba32>(8, 8, color);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }

        private sealed class Fila
        {
            public string Item = "";
            public string Nombre = "";
            public int Depth;
            public DateOnly? Inicio;
            public DateOnly? Fin;
        }

        public static void Build(
            IXLWorksheet ws,
            AdjudicacionSummarySheetDataDto header,
            List<CronogramaNodoDetalleDto> nodos)
        {
            // ── Aplanar el árbol en pre-orden y autogenerar el número de ítem ──────
            var filas = Flatten(nodos);

            // ── Rango de meses (de la fecha de inicio mínima a la fecha fin máxima) ─
            var meses = BuildMeses(nodos);
            int monthCount = meses.Count;

            // Columnas: A=ITEM, B=DESCRIPCIÓN, C=INICIO, D=FIN, luego un mes por columna.
            const int firstMonthCol = 5; // E
            int lastCol = monthCount > 0 ? firstMonthCol + monthCount - 1 : 4; // D si no hay meses
            string lastColLetter = ColumnLetter(lastCol);

            // ── Anchos ─────────────────────────────────────────────────────────────
            ws.Column("A").Width = 14;
            ws.Column("B").Width = 46;
            ws.Column("C").Width = 13;
            ws.Column("D").Width = 13;
            for (int c = firstMonthCol; c <= lastCol; c++)
                ws.Column(c).Width = 11;

            // ── Título (fila 2) ──────────────────────────────────────────────────────
            ws.Range($"A2:{lastColLetter}2").Merge();
            ws.Cell("A2").Value =
                $"CRONOGRAMA DE EJECUCION DEL CONTRATO {header.ContractTypeDescription.ToUpper()} " +
                $"POR {header.WorkItemDescription.ToUpper()}";
            ws.Range($"A2:{lastColLetter}2").Style.Font.Bold = true;
            ws.Range($"A2:{lastColLetter}2").Style.Font.FontSize = 11;
            ws.Range($"A2:{lastColLetter}2").Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
            ws.Range($"A2:{lastColLetter}2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range($"A2:{lastColLetter}2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range($"A2:{lastColLetter}2").Style.Alignment.WrapText = true;
            ws.Range($"A2:{lastColLetter}2").Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            ws.Row(2).Height = 32;

            // ── Bloque de info (filas 4–7) ──────────────────────────────────────────
            ws.Cell("A4").Value = "Proyecto :";    ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("B4").Value = header.ProjectDescription;
            ws.Cell("A5").Value = "Contratista:";  ws.Cell("A5").Style.Font.Bold = true;
            ws.Cell("B5").Value = header.ContributorName;
            ws.Cell("A6").Value = "N° de niveles:"; ws.Cell("A6").Style.Font.Bold = true;
            if (!string.IsNullOrWhiteSpace(header.Niveles)) ws.Cell("B6").Value = header.Niveles;
            ws.Cell("A7").Value = "Fecha:";         ws.Cell("A7").Style.Font.Bold = true;
            if (header.SigningDate.HasValue)
                ws.Cell("B7").Value = header.SigningDate.Value.ToString("dd/MM/yyyy", Inv);

            // ── Cabecera de la tabla (filas 9–11) ───────────────────────────────────
            const int hTop = 9;     // título amarillo / encabezados ITEM..FIN (merge vertical)
            const int hYear = 10;   // grupos de año
            const int hMonth = 11;  // nombres de mes
            const int dataStart = 12;

            void FixedHeader(string col, string text)
            {
                var r = ws.Range($"{col}{hTop}:{col}{hMonth}");
                r.Merge();
                r.Style.Font.Bold = true;
                r.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
                r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                r.Style.Alignment.WrapText = true;
                r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                r.FirstCell().Value = text;
            }
            FixedHeader("A", "ITEM");
            FixedHeader("B", "DESCRIPCIÓN");
            FixedHeader("C", "INICIO");
            FixedHeader("D", "FIN");

            if (monthCount > 0)
            {
                // Fila 9: título amarillo de la partida, sobre todos los meses
                var titleRange = ws.Range(hTop, firstMonthCol, hTop, lastCol);
                titleRange.Merge();
                titleRange.FirstCell().Value = header.WorkItemDescription.ToUpper();
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                titleRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // Fila 10: grupos por año
                int col = firstMonthCol;
                foreach (var grupo in meses.GroupBy(m => m.Year))
                {
                    int span = grupo.Count();
                    var yr = ws.Range(hYear, col, hYear, col + span - 1);
                    if (span > 1) yr.Merge();
                    yr.FirstCell().Value = grupo.Key.ToString();
                    yr.Style.Font.Bold = true;
                    yr.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
                    yr.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    yr.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    yr.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    col += span;
                }

                // Fila 11: nombres de mes
                for (int i = 0; i < monthCount; i++)
                {
                    var cell = ws.Cell(hMonth, firstMonthCol + i);
                    cell.Value = Meses[meses[i].Month - 1];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 8;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            // ── Filas de datos ──────────────────────────────────────────────────────
            int row = dataStart;
            foreach (var f in filas)
            {
                ws.Cell(row, 1).Value = f.Item;
                ws.Cell(row, 1).Style.Font.Bold = f.Depth == 0;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(row, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                var nameCell = ws.Cell(row, 2);
                nameCell.Value = f.Nombre.ToUpper();
                // El indent solo es válido con alineación horizontal Left/Right/Distributed en ClosedXML.
                nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                if (f.Depth > 0)
                    nameCell.Style.Alignment.Indent = f.Depth * 2;
                nameCell.Style.Font.Bold = f.Depth <= 1;
                if (f.Depth == 0)
                    nameCell.Style.Font.Underline = XLFontUnderlineValues.Single;
                nameCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                ws.Cell(row, 3).Value = f.Inicio?.ToString("dd/MM/yyyy", Inv) ?? "";
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                ws.Cell(row, 4).Value = f.Fin?.ToString("dd/MM/yyyy", Inv) ?? "";
                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 4).Style.Font.Bold = true;
                ws.Cell(row, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // Banda celeste de fondo (grilla de meses) + altura uniforme de fila.
                if (monthCount > 0) ws.Row(row).Height = DataRowPt;
                for (int i = 0; i < monthCount; i++)
                {
                    var cell = ws.Cell(row, firstMonthCol + i);
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DAEEF3");
                    // Mismo borde negro fino que el resto de la tabla (antes era celeste y casi
                    // no se veía debajo ni a la derecha de las barras).
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Barra de línea de tiempo como FORMA (imagen), fina y centrada verticalmente
                // — estilo diagrama de Gantt. Se ancla a DOS celdas: la del mes de inicio y la
                // del mes de fin, con un offset horizontal por la fracción del día dentro de cada
                // mes. Excel escala la imagen al ancho REAL de las columnas entre ambos anclajes,
                // así que no hay desfase aunque el rango abarque muchos meses (antes se usaba un
                // ancho fijo en píxeles asumiendo MonthPx por columna, y ese error se acumulaba
                // mes a mes hasta que la barra no llegaba a la columna del mes de fin).
                if (monthCount > 0 && (f.Inicio.HasValue || f.Fin.HasValue))
                {
                    var ini = f.Inicio ?? f.Fin!.Value;
                    var fin = f.Fin ?? f.Inicio!.Value;

                    int miStart = Math.Clamp(MonthIndex(ini, meses[0]), 0, monthCount - 1);
                    int miEnd   = Math.Clamp(MonthIndex(fin, meses[0]), 0, monthCount - 1);

                    double startFrac = (ini.Day - 1.0) / DateTime.DaysInMonth(ini.Year, ini.Month);
                    double endFrac   = (double)fin.Day / DateTime.DaysInMonth(fin.Year, fin.Month);
                    int xStart = (int)Math.Round(startFrac * MonthPx);
                    int xEnd   = (int)Math.Round(endFrac   * MonthPx);

                    int yTop    = (DataRowPx - BarHeightPx) / 2;
                    int yBottom = yTop + BarHeightPx;

                    // Ancho mínimo visible cuando inicio y fin caen casi en el mismo punto.
                    if (miEnd == miStart && xEnd - xStart < 4) xEnd = xStart + 4;

                    ws.AddPicture(new MemoryStream(BarPng))
                        .MoveTo(ws.Cell(row, firstMonthCol + miStart), xStart, yTop,
                                ws.Cell(row, firstMonthCol + miEnd),   xEnd,   yBottom);
                }
                row++;
            }

            // ── Borde exterior general ──────────────────────────────────────────────
            int lastRow = row - 1;
            if (lastRow >= hTop)
                ws.Range(hTop, 1, lastRow, lastCol).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.Margins.Left = 0.4;
            ws.PageSetup.Margins.Right = 0.4;
            ws.PageSetup.Margins.Top = 0.5;
            ws.PageSetup.Margins.Bottom = 0.5;
        }

        // ── Aplanado del árbol + numeración de ítems ────────────────────────────────
        private static List<Fila> Flatten(List<CronogramaNodoDetalleDto> nodos)
        {
            // Clave 0 = raíz (Dictionary no admite claves null aunque el tipo sea int?).
            var hijosPorPadre = nodos
                .GroupBy(n => n.ParentActividadId ?? 0)
                .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Orden).ToList());

            var filas = new List<Fila>();

            void Walk(int parentId, List<int> path)
            {
                if (!hijosPorPadre.TryGetValue(parentId, out var hijos)) return;
                int idx = 1;
                foreach (var n in hijos)
                {
                    var nuevoPath = new List<int>(path) { idx };
                    filas.Add(new Fila
                    {
                        Item = FormatItem(nuevoPath),
                        Nombre = n.Nombre,
                        Depth = nuevoPath.Count - 1,
                        Inicio = n.FechaInicio,
                        Fin = n.FechaFin,
                    });
                    Walk(n.ActividadId, nuevoPath);
                    idx++;
                }
            }

            Walk(0, new List<int>());
            return filas;
        }

        /// <summary>1.00 (raíz), 1.01 (nivel 1), 1.01.01 (nivel 2), …</summary>
        private static string FormatItem(List<int> path)
        {
            if (path.Count == 1) return $"{path[0]}.00";
            return path[0] + string.Concat(path.Skip(1).Select(p => $".{p:D2}"));
        }

        // ── Meses del cronograma ────────────────────────────────────────────────────
        private static List<(int Year, int Month)> BuildMeses(List<CronogramaNodoDetalleDto> nodos)
        {
            DateOnly? min = null, max = null;
            foreach (var n in nodos)
            {
                if (n.FechaInicio.HasValue && (min == null || n.FechaInicio.Value < min)) min = n.FechaInicio;
                if (n.FechaFin.HasValue && (max == null || n.FechaFin.Value > max)) max = n.FechaFin;
                // Una fecha sola también amplía el rango por ambos extremos.
                if (n.FechaInicio.HasValue && (max == null || n.FechaInicio.Value > max)) max = n.FechaInicio;
                if (n.FechaFin.HasValue && (min == null || n.FechaFin.Value < min)) min = n.FechaFin;
            }
            if (min == null || max == null) return new List<(int, int)>();

            var meses = new List<(int Year, int Month)>();
            int y = min.Value.Year, m = min.Value.Month;
            int guard = 0;
            while ((y < max.Value.Year || (y == max.Value.Year && m <= max.Value.Month)) && guard++ < 120)
            {
                meses.Add((y, m));
                m++;
                if (m > 12) { m = 1; y++; }
            }
            return meses;
        }

        private static string ColumnLetter(int col)
        {
            var sb = new System.Text.StringBuilder();
            while (col > 0)
            {
                int rem = (col - 1) % 26;
                sb.Insert(0, (char)('A' + rem));
                col = (col - 1) / 26;
            }
            return sb.ToString();
        }

        private static int MonthIndex(DateOnly d, (int Year, int Month) first)
            => (d.Year - first.Year) * 12 + (d.Month - first.Month);
    }
}
