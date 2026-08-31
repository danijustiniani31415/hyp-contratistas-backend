using System.Security.Cryptography;
using Abril_Backend.Application.DTOs.ArquitecturaComercial;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Application.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Application.Services
{
    public class ArquitecturaComercialTareoService : IArquitecturaComercialTareoService
    {
        private readonly IArquitecturaComercialTareoRepository _repository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IStorageContainerResolver _containerResolver;
        private readonly string[] _logoPaths;

        public ArquitecturaComercialTareoService(
            IArquitecturaComercialTareoRepository repository,
            IFileStorageService fileStorageService,
            IStorageContainerResolver containerResolver,
            IWebHostEnvironment env)
        {
            _repository = repository;
            _fileStorageService = fileStorageService;
            _containerResolver = containerResolver;
            _logoPaths = new[]
            {
                Path.Combine(env.WebRootPath, "images", "abril-logo.png"),
                Path.Combine(env.WebRootPath, "images", "logo-abril.jpg"),
                Path.Combine(env.ContentRootPath, "Templates", "logo-abril.jpg"),
            };
        }

        public Task<int> ResolverWorkerId(int userId)
            => _repository.ResolverWorkerId(userId);

        public Task<List<TareoTrabajadorEnrolamientoDTO>> GetTrabajadoresParaEnrolar()
            => _repository.GetTrabajadoresParaEnrolar();

        public Task<List<TareoTrabajadorEnrolamientoDTO>> GetTrabajadoresDisponiblesParaEnrolar()
            => _repository.GetTrabajadoresDisponiblesParaEnrolar();

        public Task<TareoIdentificacionDTO> Identificar(float[]? embedding)
        {
            if (embedding is not { Length: 128 })
                return Task.FromResult(new TareoIdentificacionDTO { Identificado = false });
            return _repository.IdentificarPorEmbedding(embedding);
        }

        public Task<TareoEnrolamientoEstadoDTO> GetEnrolamientoEstado(int workerId)
            => _repository.GetEnrolamientoEstado(workerId);

        public async Task EnrolarWorker(int workerId, TareoEnrolamientoRequestDTO body)
        {
            if (body.Embedding is not { Length: 128 })
                throw new AbrilException("El embedding facial debe tener 128 valores.", 400);

            // El SSO-FO-150 firmado y escaneado es requisito PREVIO al enrolamiento — nunca se
            // enrola a nadie sin evidencia de que autorizó el tratamiento de sus datos biométricos.
            if (!await _repository.TieneAutorizacion(workerId))
                throw new AbrilException(
                    "Falta subir el SSO-FO-150 (autorización de datos biométricos) firmado por el " +
                    "trabajador antes de poder enrolarlo.", 400);

            var fotoUrl = await SubirFoto(workerId, "enrolamiento", body.FotoBase64);
            await _repository.EnrolarWorker(workerId, fotoUrl, body.Embedding);
        }

        public Task<List<TareoProyectoGeoDTO>> GetProyectosGeo()
            => _repository.GetProyectosGeo();

        public Task SetProyectoGeo(int projectId, TareoProyectoGeoUpdateDTO dto)
            => _repository.SetProyectoGeo(projectId, dto);

        public Task<TareoAutorizacionDetalleDTO> GetAutorizacionDetalle(int workerId)
            => _repository.GetAutorizacionDetalle(workerId);

        public async Task<byte[]> GenerarAutorizacionPdf(int workerId)
        {
            var detalle = await _repository.GetAutorizacionDetalle(workerId);

            byte[]? logoBytes = null;
            var logoPath = _logoPaths.FirstOrDefault(File.Exists);
            if (logoPath != null)
                logoBytes = await File.ReadAllBytesAsync(logoPath);

            return TareoAutorizacionPdfService.GenerarPdf(detalle, logoBytes);
        }

        public async Task<string> SubirAutorizacion(int workerId, Stream fileStream, string fileName, int? subidoPorUserId)
        {
            var container = _containerResolver.GetTareosContainerName();
            var ext = Path.GetExtension(fileName) is { Length: > 0 } e ? e : ".pdf";
            var storageFileName = $"autorizacion_{workerId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
            var urls = await _fileStorageService.UploadFilesAsync([(fileStream, storageFileName)], container);

            await _repository.SetAutorizacion(workerId, urls[0], subidoPorUserId);
            return urls[0];
        }

        public async Task<TareoRegistroDTO> Marcar(Guid idempotencyKey, TareoMarcarRequestDTO body, string? ipOrigen)
        {
            if (string.IsNullOrWhiteSpace(body.FotoBase64))
                throw new AbrilException("La foto es obligatoria para marcar el tareo.", 400);

            var identificacion = await _repository.IdentificarPorEmbedding(body.Embedding ?? []);
            if (!identificacion.Identificado || identificacion.WorkerId is not int workerId)
                throw new AbrilException("No pudimos reconocerte con la cámara. Acércate más, mejora la luz, o pide ayuda a tu coordinador.", 422);

            var (fotoUrl, fotoHash) = await SubirFotoConHash(workerId, body.Tipo, body.FotoBase64);
            return await _repository.Marcar(workerId, idempotencyKey, body, fotoUrl, fotoHash, ipOrigen);
        }

        public async Task<TareoMiTareoHoyDTO> GetMiTareoHoy(int workerId)
        {
            if (!await _repository.EsObreroAc(workerId))
                throw new AbrilException("Trabajador no válido para este módulo.", 403);
            return await _repository.GetMiTareoHoy(workerId);
        }

        public Task<TareoRegistroListResponseDTO> GetRegistros(TareoFiltroDTO filtro)
            => _repository.GetRegistros(filtro);

        public Task<bool> Revisar(int id, int revisorUserId, TareoRevisarRequestDTO body)
            => _repository.Revisar(id, revisorUserId, body);

        public Task<List<TareoReporteSemanalDTO>> GetReporteSemanal(int? proyectoId, DateOnly semanaLunes)
            => _repository.GetReporteSemanal(proyectoId, semanaLunes);

        private async Task<string> SubirFoto(int workerId, string prefijo, string fotoBase64)
        {
            var (url, _) = await SubirFotoConHash(workerId, prefijo, fotoBase64);
            return url;
        }

        private async Task<(string Url, string Hash)> SubirFotoConHash(int workerId, string prefijo, string fotoBase64)
        {
            byte[] bytes;
            try
            {
                var payload = fotoBase64.Contains(',') ? fotoBase64[(fotoBase64.IndexOf(',') + 1)..] : fotoBase64;
                bytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                throw new AbrilException("La foto enviada no es una imagen válida.", 400);
            }

            if (bytes.Length == 0)
                throw new AbrilException("La foto enviada está vacía.", 400);

            var hash = Convert.ToHexString(SHA256.HashData(bytes));

            var container = _containerResolver.GetTareosContainerName();
            using var stream = new MemoryStream(bytes);
            var fileName = $"{prefijo}_{workerId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";
            var urls = await _fileStorageService.UploadFilesAsync([(stream, fileName)], container);

            return (urls[0], hash);
        }
    }
}
