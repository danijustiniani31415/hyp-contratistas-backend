using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Services
{
    public class PlaneamientoBimCargaDiariaService : IPlaneamientoBimCargaDiariaService
    {
        private const int VentanaDiasEdicion = 5; // hoy y los 4 días anteriores
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB por foto
        private const int MaxArchivosPorSubida = 20;

        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp",
        };

        private static readonly string[] CategoriasValidas = { "GENERAL", "PROCURA" };

        /// <summary>Set fijo de porcentajes de avance permitidos por celda — confirmado con
        /// Planeamiento (punto 3 de sus observaciones): no numérico libre, para no perder
        /// comparabilidad en Avance/PPC/Pareto.</summary>
        public static readonly decimal[] PorcentajesValidos = { 0m, 25m, 50m, 75m, 100m };

        private readonly IPlaneamientoBimCargaDiariaRepository _repository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IStorageContainerResolver _containerResolver;

        public PlaneamientoBimCargaDiariaService(
            IPlaneamientoBimCargaDiariaRepository repository,
            IFileStorageService fileStorageService,
            IStorageContainerResolver containerResolver)
        {
            _repository = repository;
            _fileStorageService = fileStorageService;
            _containerResolver = containerResolver;
        }

        public async Task<CargaDiariaDto> GetCargaDiaria(int projectId, DateOnly fecha, string categoria = "GENERAL")
        {
            ValidarCategoria(categoria);

            var dto = await _repository.GetCargaDiaria(projectId, fecha, categoria);
            if (dto == null)
                throw new AbrilException("El proyecto no existe.", 404);

            dto.EsEditable = EsFechaEditable(fecha);
            return dto;
        }

        public async Task GuardarCargaDiaria(int projectId, DateOnly fecha, CargaDiariaUpdateDto dto, int userId)
        {
            ValidarVentanaDeEdicion(fecha);

            var invalidos = dto.Celdas.Where(c => !PorcentajesValidos.Contains(c.PorcentajeAvance)).ToList();
            if (invalidos.Count > 0)
                throw new AbrilException($"Porcentaje de avance inválido. Use uno de: {string.Join("%, ", PorcentajesValidos)}%.", 400);

            if (dto.Celdas.Any(c => c.PorcentajeAvance < 100 && c.CausaId == null))
                throw new AbrilException("Debe indicar la causa de no cumplimiento para las celdas con avance menor a 100%.", 400);

            await _repository.GuardarCargaDiaria(projectId, fecha, dto, userId);
        }

        public async Task<List<EvidenciaFotoDto>> SubirEvidencias(int projectId, DateOnly fecha, IFormFileCollection files, int userId, string categoria = "GENERAL")
        {
            ValidarCategoria(categoria);
            ValidarVentanaDeEdicion(fecha);

            if (files is null || files.Count == 0)
                throw new AbrilException("No se adjuntó ninguna foto.", 400);
            if (files.Count > MaxArchivosPorSubida)
                throw new AbrilException($"Solo se pueden subir hasta {MaxArchivosPorSubida} fotos por vez.", 400);

            foreach (var file in files)
            {
                if (file.Length == 0)
                    throw new AbrilException($"El archivo \"{file.FileName}\" está vacío.", 400);
                if (file.Length > MaxFileSizeBytes)
                    throw new AbrilException($"El archivo \"{file.FileName}\" supera el tamaño máximo permitido (10 MB).", 400);
                var extension = Path.GetExtension(file.FileName);
                if (!ExtensionesPermitidas.Contains(extension))
                    throw new AbrilException($"El tipo de archivo \"{extension}\" no está permitido. Use JPG, PNG o WEBP.", 400);
            }

            var container = _containerResolver.GetProjectFotosContainerName();
            var streams = new List<Stream>();
            try
            {
                var toUpload = new List<(Stream Stream, string FileName)>();
                foreach (var file in files)
                {
                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    var extension = Path.GetExtension(file.FileName);
                    toUpload.Add((stream, $"{Guid.NewGuid()}{extension}"));
                }

                var urls = await _fileStorageService.UploadFilesAsync(toUpload, container);
                return await _repository.AgregarEvidencias(projectId, fecha, urls, userId, categoria);
            }
            finally
            {
                foreach (var stream in streams)
                    stream.Dispose();
            }
        }

        private static readonly TimeZoneInfo LimaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

        private static DateOnly HoyLima()
            => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LimaZone));

        private static bool EsFechaEditable(DateOnly fecha)
        {
            var hoy = HoyLima();
            return fecha <= hoy && fecha >= hoy.AddDays(-(VentanaDiasEdicion - 1));
        }

        private static void ValidarVentanaDeEdicion(DateOnly fecha)
        {
            var hoy = HoyLima();
            if (fecha > hoy)
                throw new AbrilException("No se puede cargar información de una fecha futura.", 400);
            if (!EsFechaEditable(fecha))
                throw new AbrilException($"La fecha está fuera de la ventana de edición (últimos {VentanaDiasEdicion} días).", 409);
        }

        private static void ValidarCategoria(string categoria)
        {
            if (!CategoriasValidas.Contains(categoria))
                throw new AbrilException($"Categoría inválida. Use {string.Join(" o ", CategoriasValidas)}.", 400);
        }
    }
}
