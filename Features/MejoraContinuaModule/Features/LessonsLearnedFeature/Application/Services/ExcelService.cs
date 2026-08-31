using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos;
using ClosedXML.Excel;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Services
{
    public class ExcelService
    {
        public ExcelService() { }

        public async Task<byte[]> GenerateLessonsExcel(List<LessonListDTO> lessons)
        {
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "excel",
                "Lecciones_Aprendidas.xlsx"
            );

            using var workbook = new XLWorkbook(templatePath);
            var worksheet = workbook.Worksheet(1);

            int row = 8;
            foreach (var lesson in lessons)
            {
                var classification = string.Join(
                    " / ",
                    (lesson.ClassificationSegments ?? new List<LessonClassificationSegmentDTO>())
                        .Select(s => s.CatalogItemDescription));

                worksheet.Cell(row, 2).Value = lesson.ProjectDescription;
                worksheet.Cell(row, 3).Value = lesson.Period;
                // El Obra/Oficina se anexa al área: antes formaba parte del path porque era
                // el último nodo del árbol; ahora es una columna propia de la lección y la
                // plantilla del Excel tiene las columnas fijas.
                var areaConObraOficina = string.IsNullOrWhiteSpace(lesson.ObraOficinaStaffName)
                    ? lesson.AreaDescription
                    : $"{lesson.AreaDescription} / {lesson.ObraOficinaStaffName}";
                worksheet.Cell(row, 4).Value = areaConObraOficina;
                worksheet.Cell(row, 5).Value = classification;
                worksheet.Cell(row, 6).Value = lesson.ProblemDescription;
                worksheet.Cell(row, 7).Value = lesson.ReasonDescription;
                worksheet.Cell(row, 8).Value = lesson.LessonDescription;
                worksheet.Cell(row, 9).Value = lesson.ImpactDescription;

                var range = worksheet.Range(row, 2, row, 15);
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range.Style.Alignment.WrapText = true;

                var oportunidadImages = (lesson.Images ?? new List<LessonImageDTO>())
                    .Where(i => i.ImageTypeDescription == "OPORTUNIDAD").Take(3).ToList();
                var mejoraImages = (lesson.Images ?? new List<LessonImageDTO>())
                    .Where(i => i.ImageTypeDescription == "MEJORA").Take(3).ToList();

                for (int i = 0; i < oportunidadImages.Count; i++)
                {
                    var img = oportunidadImages[i];
                    var imagePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        img.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                    );
                    if (File.Exists(imagePath))
                    {
                        worksheet.AddPicture(imagePath)
                            .MoveTo(worksheet.Cell(row, 10 + i))
                            .WithSize(80, 80);
                    }
                }
                for (int i = 0; i < mejoraImages.Count; i++)
                {
                    var img = mejoraImages[i];
                    var imagePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        img.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                    );
                    if (File.Exists(imagePath))
                    {
                        worksheet.AddPicture(imagePath)
                            .MoveTo(worksheet.Cell(row, 13 + i))
                            .WithSize(80, 80);
                    }
                }

                worksheet.Row(row).Height = 120;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return await Task.FromResult(stream.ToArray());
        }
    }
}
