using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Dtos;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Services
{
    public class GestionVecinosService : IGestionVecinosService
    {
        private const long MaxBytes = 15 * 1024 * 1024;
        private static readonly string[] AllowedExtensions =
            { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".webp" };
        private static readonly string[] AllowedImageExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

        private readonly IGestionVecinosRepository _repository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IStorageContainerResolver _containerResolver;

        public GestionVecinosService(
            IGestionVecinosRepository repository,
            IFileStorageService fileStorageService,
            IStorageContainerResolver containerResolver)
        {
            _repository = repository;
            _fileStorageService = fileStorageService;
            _containerResolver = containerResolver;
        }

        public async Task<VecinosPageDto> GetPageData(VecinoFilterDto filter)
        {
            var options = await _repository.GetOptions();
            var vecinos = await _repository.GetPaged(filter);
            return new VecinosPageDto { Options = options, Vecinos = vecinos };
        }

        public Task<PagedResult<VecinoListItemDto>> GetList(VecinoFilterDto filter)
            => _repository.GetPaged(filter);

        public Task<VecinoLoteRegisterResultDto> RegisterVecinos(VecinoLoteRegisterDto dto, int userId)
        {
            if (dto.ProjectCroquisLoteId <= 0)
                throw new AbrilException("Debe seleccionar el lote en el croquis.", 400);
            if (string.IsNullOrWhiteSpace(dto.Direccion))
                throw new AbrilException("La dirección del lote es obligatoria.", 400);
            if (dto.Vecinos is null || dto.Vecinos.Count == 0)
                throw new AbrilException("Debe agregar al menos un vecino.", 400);

            foreach (var dep in dto.Vecinos)
            {
                if (dep.VecinoUsoId <= 0)
                    throw new AbrilException("Debe seleccionar el uso de cada vecino.", 400);
                if (dep.VecinoColindanciaId <= 0)
                    throw new AbrilException("Debe seleccionar si cada vecino es colindante o no colindante.", 400);
                if (dep.VecinoTipoConstruccionId <= 0)
                    throw new AbrilException("Debe seleccionar el tipo de construcción de cada vecino.", 400);

                if (dep.Personas is null || dep.Personas.Count == 0)
                    throw new AbrilException("Cada vecino debe tener al menos una persona.", 400);

                foreach (var per in dep.Personas)
                {
                    if (string.IsNullOrWhiteSpace(per.Nombre))
                        throw new AbrilException("El nombre de cada persona es obligatorio.", 400);
                    if (string.IsNullOrWhiteSpace(per.Celular))
                        throw new AbrilException("El celular de cada persona es obligatorio.", 400);
                    if (per.VecinoRelacionTipoId <= 0)
                        throw new AbrilException("Debe seleccionar la relación (propietario, inquilino u otro) de cada persona.", 400);

                    // El DNI es opcional, pero si se ingresa debe tener 8 dígitos.
                    var dni = per.Dni?.Trim();
                    if (!string.IsNullOrEmpty(dni) && (dni.Length != 8 || !dni.All(char.IsDigit)))
                        throw new AbrilException("El DNI debe tener 8 dígitos.", 400);
                }
            }

            return _repository.RegisterVecinos(dto, userId);
        }

        public async Task<VecinoListItemDto> GetById(int vecinoId)
        {
            var item = await _repository.GetById(vecinoId);
            if (item is null)
                throw new AbrilException("La propiedad no existe.", 404);
            return item;
        }

        public async Task Update(int vecinoId, VecinoUpdateDto dto, int userId)
        {
            if (dto.VecinoUsoId <= 0)
                throw new AbrilException("Debe seleccionar el uso.", 400);
            if (dto.VecinoColindanciaId <= 0)
                throw new AbrilException("Debe seleccionar si es colindante o no colindante.", 400);
            if (dto.VecinoTipoConstruccionId <= 0)
                throw new AbrilException("Debe seleccionar el tipo de construcción.", 400);

            if (dto.Personas is null || dto.Personas.Count == 0)
                throw new AbrilException("Debe quedar al menos una persona.", 400);

            foreach (var per in dto.Personas)
            {
                if (string.IsNullOrWhiteSpace(per.Nombre))
                    throw new AbrilException("El nombre de cada persona es obligatorio.", 400);
                if (string.IsNullOrWhiteSpace(per.Celular))
                    throw new AbrilException("El celular de cada persona es obligatorio.", 400);
                if (per.VecinoRelacionTipoId <= 0)
                    throw new AbrilException("Debe seleccionar la relación (propietario, inquilino u otro) de cada persona.", 400);

                var dni = per.Dni?.Trim();
                if (!string.IsNullOrEmpty(dni) && (dni.Length != 8 || !dni.All(char.IsDigit)))
                    throw new AbrilException("El DNI debe tener 8 dígitos.", 400);
            }

            var ok = await _repository.Update(vecinoId, dto, userId);
            if (!ok)
                throw new AbrilException("El vecino no existe.", 404);
        }

        public async Task UpdateLote(int vecinoLoteId, VecinoLoteUpdateDto dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Direccion))
                throw new AbrilException("La dirección del lote es obligatoria.", 400);

            var ok = await _repository.UpdateLote(vecinoLoteId, dto, userId);
            if (!ok)
                throw new AbrilException("El lote no existe.", 404);
        }

        public async Task DeleteImagen(int imagenId, int userId)
        {
            var ok = await _repository.DeleteImagen(imagenId, userId);
            if (!ok)
                throw new AbrilException("La imagen no existe.", 404);
        }

        public async Task<List<VecinoImagenDto>> UploadImagenes(int vecinoId, IFormFileCollection files, int userId)
        {
            if (!await _repository.VecinoExists(vecinoId))
                throw new AbrilException("La propiedad no existe.", 404);
            if (files == null || files.Count == 0)
                throw new AbrilException("No se adjuntó ninguna imagen.", 400);

            foreach (var file in files)
            {
                if (file.Length == 0)
                    throw new AbrilException("Una de las imágenes está vacía.", 400);
                if (file.Length > MaxBytes)
                    throw new AbrilException("Cada imagen supera el tamaño máximo permitido (15 MB).", 400);

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                    throw new AbrilException("Formato no válido. Use PNG, JPG o WEBP.", 400);
            }

            var container = _containerResolver.GetVecinoPropiedadImagenesContainerName();

            var toUpload = new List<(Stream, string)>();
            var streams = new List<Stream>();
            try
            {
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    toUpload.Add((stream, $"{Guid.NewGuid()}{extension}"));
                }

                var urls = (await _fileStorageService.UploadFilesAsync(toUpload, container)).ToList();

                var imagenes = urls
                    .Select((url, i) => (ArchivoUrl: url, OriginalFileName: (string?)files[i].FileName))
                    .ToList();

                return await _repository.AddImagenes(vecinoId, imagenes, userId);
            }
            finally
            {
                foreach (var s in streams) s.Dispose();
            }
        }

        // ── Solicitudes ─────────────────────────────────────────────────────
        public async Task<VecinoSolicitudesResponseDto> GetSolicitudes(int vecinoId)
        {
            if (!await _repository.VecinoExists(vecinoId))
                throw new AbrilException("El vecino no existe.", 404);

            return await _repository.GetSolicitudes(vecinoId);
        }

        public async Task<int> CreateSolicitud(int vecinoId, VecinoSolicitudCreateDto dto, int userId)
        {
            if (!await _repository.VecinoExists(vecinoId))
                throw new AbrilException("El vecino no existe.", 404);
            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                throw new AbrilException("La descripción de la solicitud es obligatoria.", 400);

            return await _repository.CreateSolicitud(vecinoId, dto, userId);
        }

        public async Task UpdateSolicitudEstado(int solicitudId, int estadoId, int userId)
        {
            if (estadoId <= 0)
                throw new AbrilException("Debe seleccionar un estado válido.", 400);

            var ok = await _repository.UpdateSolicitudEstado(solicitudId, estadoId, userId);
            if (!ok)
                throw new AbrilException("No se pudo actualizar el estado de la solicitud.", 404);
        }

        public async Task UpdateSolicitudDescripcion(int solicitudId, string descripcion, int userId)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new AbrilException("La descripción de la solicitud es obligatoria.", 400);

            var ok = await _repository.UpdateSolicitudDescripcion(solicitudId, descripcion, userId);
            if (!ok)
                throw new AbrilException("No se pudo actualizar la descripción de la solicitud.", 404);
        }

        // ── Compromisos ─────────────────────────────────────────────────────
        public async Task<List<VecinoCompromisoItemDto>> GetCompromisos(int solicitudId)
        {
            if (!await _repository.SolicitudExists(solicitudId))
                throw new AbrilException("La solicitud no existe.", 404);

            return await _repository.GetCompromisos(solicitudId);
        }

        public async Task<int> CreateCompromiso(int solicitudId, VecinoCompromisoCreateDto dto, int userId)
        {
            if (!await _repository.SolicitudExists(solicitudId))
                throw new AbrilException("La solicitud no existe.", 404);
            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                throw new AbrilException("La descripción del compromiso es obligatoria.", 400);
            if (dto.FechaInicio.HasValue && dto.FechaFin.HasValue && dto.FechaFin.Value < dto.FechaInicio.Value)
                throw new AbrilException("La fecha fin no puede ser anterior a la fecha de inicio.", 400);

            return await _repository.CreateCompromiso(solicitudId, dto, userId);
        }

        public async Task UpdateCompromisoEstado(int compromisoId, int estadoId, int userId)
        {
            if (estadoId <= 0)
                throw new AbrilException("Debe seleccionar un estado válido.", 400);

            var ok = await _repository.UpdateCompromisoEstado(compromisoId, estadoId, userId);
            if (!ok)
                throw new AbrilException("No se pudo actualizar el estado del compromiso.", 404);
        }

        public async Task UpdateCompromisoObservaciones(int compromisoId, string? observaciones, int userId)
        {
            var ok = await _repository.UpdateCompromisoObservaciones(compromisoId, observaciones, userId);
            if (!ok)
                throw new AbrilException("No se pudo actualizar las observaciones del compromiso.", 404);
        }

        public async Task UpdateCompromisoFechaMunicipalidad(int compromisoId, DateOnly? fechaFinMunicipalidad, int userId)
        {
            var ok = await _repository.UpdateCompromisoFechaMunicipalidad(compromisoId, fechaFinMunicipalidad, userId);
            if (!ok)
                throw new AbrilException("No se pudo actualizar la fecha límite por municipalidad/fiscalización del compromiso.", 404);
        }

        public async Task UpdateEntregableEstado(int entregableId, int estadoId, int userId)
        {
            if (estadoId <= 0)
                throw new AbrilException("Debe seleccionar un estado válido.", 400);

            var ok = await _repository.UpdateEntregableEstado(entregableId, estadoId, userId);
            if (!ok)
                throw new AbrilException("No se pudo actualizar el estado del entregable.", 404);
        }

        public async Task<string> UploadEntregable(int entregableId, IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new AbrilException("No se adjuntó ningún archivo.", 400);
            if (file.Length > MaxBytes)
                throw new AbrilException("El archivo supera el tamaño máximo permitido (15 MB).", 400);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new AbrilException("Formato no válido. Use PDF, Word, Excel o imagen.", 400);

            var container = _containerResolver.GetVecinoEntregablesContainerName();

            string archivoUrl;
            using (var stream = file.OpenReadStream())
            {
                var uploaded = await _fileStorageService.UploadFilesAsync(
                    new[] { (stream, $"{Guid.NewGuid()}{extension}") },
                    container);
                archivoUrl = uploaded.First();
            }

            var ok = await _repository.UploadEntregable(entregableId, archivoUrl, file.FileName, userId);
            if (!ok)
                throw new AbrilException("El entregable no existe.", 404);

            return archivoUrl;
        }

        public async Task<List<VecinoNormativaDto>> UploadNormativas(int compromisoId, IFormFileCollection files, int userId)
        {
            if (!await _repository.CompromisoExists(compromisoId))
                throw new AbrilException("El compromiso no existe.", 404);
            if (files == null || files.Count == 0)
                throw new AbrilException("No se adjuntó ningún archivo.", 400);

            foreach (var file in files)
            {
                if (file.Length == 0)
                    throw new AbrilException("Uno de los archivos está vacío.", 400);
                if (file.Length > MaxBytes)
                    throw new AbrilException("Cada archivo supera el tamaño máximo permitido (15 MB).", 400);

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                    throw new AbrilException("Formato no válido. Use PDF, Word, Excel o imagen.", 400);
            }

            var container = _containerResolver.GetVecinoEntregablesContainerName();

            var toUpload = new List<(Stream, string)>();
            var streams = new List<Stream>();
            try
            {
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    toUpload.Add((stream, $"{Guid.NewGuid()}{ext}"));
                }

                var urls = (await _fileStorageService.UploadFilesAsync(toUpload, container)).ToList();

                var archivos = urls
                    .Select((url, i) => (ArchivoUrl: url, OriginalFileName: (string?)files[i].FileName))
                    .ToList();

                return await _repository.AddNormativas(compromisoId, archivos, userId);
            }
            finally
            {
                foreach (var s in streams) s.Dispose();
            }
        }

        public async Task DeleteNormativa(int normativaId, int userId)
        {
            var ok = await _repository.DeleteNormativa(normativaId, userId);
            if (!ok)
                throw new AbrilException("El archivo no existe.", 404);
        }

        // ── Calendario de limpiezas ────────────────────────────────────────────
        public Task<VecinoLimpiezasResponseDto> GetLimpiezas(int projectId, int year, int month)
        {
            if (month < 1 || month > 12)
                throw new AbrilException("Mes inválido.", 400);
            return _repository.GetLimpiezas(projectId, year, month);
        }

        public Task<VecinoLimpiezaDto> CreateLimpieza(int projectId, VecinoLimpiezaCreateDto dto, int userId)
        {
            if (dto.VecinoLimpiezaTipoId <= 0)
                throw new AbrilException("Debe seleccionar el tipo de limpieza.", 400);
            return _repository.CreateLimpieza(projectId, dto, userId);
        }

        public async Task DeleteLimpieza(int limpiezaId, int userId)
        {
            var ok = await _repository.DeleteLimpieza(limpiezaId, userId);
            if (!ok)
                throw new AbrilException("La limpieza no existe.", 404);
        }

        public Task<VecinoLimpiezaCumplimientoDto> GetCumplimiento(int projectId)
            => _repository.GetCumplimiento(projectId);

        public Task<List<VecinoCompromisoSelectDto>> GetCompromisosSelect(int vecinoId)
            => _repository.GetCompromisosSelect(vecinoId);

        public async Task<string> UploadAtencion(int limpiezaId, IFormFile file, int? vecinoCompromisoId, int userId)
        {
            if (file == null || file.Length == 0)
                throw new AbrilException("No se adjuntó ningún archivo.", 400);
            if (file.Length > MaxBytes)
                throw new AbrilException("El archivo supera el tamaño máximo permitido (15 MB).", 400);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new AbrilException("Formato no válido. Use PDF, Word, Excel o imagen.", 400);

            var container = _containerResolver.GetVecinoEntregablesContainerName();

            string archivoUrl;
            using (var stream = file.OpenReadStream())
            {
                var uploaded = await _fileStorageService.UploadFilesAsync(
                    new[] { (stream, $"{Guid.NewGuid()}{extension}") },
                    container);
                archivoUrl = uploaded.First();
            }

            var ok = await _repository.UploadAtencion(limpiezaId, archivoUrl, file.FileName, vecinoCompromisoId, userId);
            if (!ok)
                throw new AbrilException("La limpieza no existe.", 404);

            return archivoUrl;
        }

        // ── Dashboard ────────────────────────────────────────────────────────
        public Task<VecinosDashboardDto> GetDashboard() => _repository.GetDashboard();

        // ── Requisitos ───────────────────────────────────────────────────────
        public async Task<VecinoRequisitosResponseDto> GetRequisitos(int vecinoId)
        {
            if (!await _repository.VecinoExists(vecinoId))
                throw new AbrilException("El vecino no existe.", 404);

            return await _repository.GetRequisitos(vecinoId);
        }

        public async Task<string> UploadRequisito(int vecinoId, int tipoId, IFormFile file, int userId)
        {
            if (!await _repository.VecinoExists(vecinoId))
                throw new AbrilException("El vecino no existe.", 404);
            if (!await _repository.TipoRequisitoExists(tipoId))
                throw new AbrilException("El tipo de requisito no existe.", 404);

            if (file == null || file.Length == 0)
                throw new AbrilException("No se adjuntó ningún archivo.", 400);
            if (file.Length > MaxBytes)
                throw new AbrilException("El archivo supera el tamaño máximo permitido (15 MB).", 400);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new AbrilException("Formato no válido. Use PDF, Word, Excel o imagen.", 400);

            var container = _containerResolver.GetVecinoRequisitosContainerName();

            string archivoUrl;
            using (var stream = file.OpenReadStream())
            {
                var uploaded = await _fileStorageService.UploadFilesAsync(
                    new[] { (stream, $"{Guid.NewGuid()}{extension}") },
                    container);
                archivoUrl = uploaded.First();
            }

            await _repository.UploadRequisito(vecinoId, tipoId, archivoUrl, file.FileName, userId);
            return archivoUrl;
        }

        public async Task SetRequisitoNoAplica(int vecinoId, int tipoId, bool noAplica, int userId)
        {
            if (!await _repository.VecinoExists(vecinoId))
                throw new AbrilException("El vecino no existe.", 404);
            if (!await _repository.TipoRequisitoExists(tipoId))
                throw new AbrilException("El tipo de requisito no existe.", 404);

            await _repository.SetRequisitoNoAplica(vecinoId, tipoId, noAplica, userId);
        }
    }
}
