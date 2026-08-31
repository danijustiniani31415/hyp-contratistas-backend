using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Shared.Services;
using Abril_Backend.Shared.Services.Sunat.Dtos;
using Abril_Backend.Shared.Services.Sunat.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class CatalogosService : ICatalogosService
    {
        private readonly ICatalogosRepository _repo;
        private readonly ISunatService _sunat;
        private readonly ISharePointHabService _sharePoint;
        private readonly string[] _logoPaths;

        public CatalogosService(
            ICatalogosRepository repo, ISunatService sunat, ISharePointHabService sharePoint, IWebHostEnvironment env)
        {
            _repo = repo;
            _sunat = sunat;
            _sharePoint = sharePoint;
            _logoPaths = new[]
            {
                Path.Combine(env.WebRootPath, "images", "abril-logo.png"),
                Path.Combine(env.WebRootPath, "images", "logo-abril.jpg"),
                Path.Combine(env.ContentRootPath, "Templates", "logo-abril.jpg"),
            };
        }

        /// <summary>Solo el propio médico (email de su sesión == email registrado) puede tocar
        /// su firma/PIN — mismo control en las tres operaciones de este flujo.</summary>
        private async Task AsegurarEsElPropioMedicoAsync(int medicoId, string? callerEmail, string medicoEmail)
        {
            if (string.IsNullOrWhiteSpace(medicoEmail)
                || string.IsNullOrWhiteSpace(callerEmail)
                || !string.Equals(medicoEmail.Trim(), callerEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new AbrilException("Solo el propio médico puede configurar su firma.", 403);
            await Task.CompletedTask;
        }

        public Task<List<ClinicaDto>> ListClinicas(bool soloActivos) => _repo.ListClinicas(soloActivos);

        public Task<ClinicaDto> GetClinicaById(int id) => _repo.GetClinicaById(id);

        public Task<ClinicaDto> CreateClinica(ClinicaUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre de la clínica es obligatorio.", 400);
            return _repo.CreateClinica(dto);
        }

        public Task<ClinicaDto> UpdateClinica(int id, ClinicaUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre de la clínica es obligatorio.", 400);
            return _repo.UpdateClinica(id, dto);
        }

        public Task<List<MedicoOcupacionalDto>> ListMedicos(bool soloActivos) => _repo.ListMedicos(soloActivos);

        public Task<MedicoOcupacionalDto> CreateMedico(MedicoOcupacionalUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ApellidoNombre))
                throw new AbrilException("El nombre del médico es obligatorio.", 400);
            return _repo.CreateMedico(dto);
        }

        public Task<MedicoOcupacionalDto> UpdateMedico(int id, MedicoOcupacionalUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ApellidoNombre))
                throw new AbrilException("El nombre del médico es obligatorio.", 400);
            return _repo.UpdateMedico(id, dto);
        }

        public async Task<byte[]> GenerarAutorizacionFirmaPdfAsync(int medicoId)
        {
            var detalle = await _repo.GetAutorizacionFirmaDetalleAsync(medicoId)
                ?? throw new AbrilException("Médico no encontrado.", 404);

            byte[]? logoBytes = null;
            var logoPath = _logoPaths.FirstOrDefault(File.Exists);
            if (logoPath != null)
                logoBytes = await File.ReadAllBytesAsync(logoPath);

            // La firma digital se imprime junto a un recuadro en blanco para la firma
            // manuscrita de comparación. Si por lo que sea no se puede descargar de SharePoint,
            // el PDF igual se genera — solo queda sin la imagen (el recuadro en blanco sigue).
            byte[]? firmaDigitalBytes = null;
            if (!string.IsNullOrWhiteSpace(detalle.FirmaDigitalUrl))
            {
                try { firmaDigitalBytes = await _sharePoint.DescargarContenidoAsync(detalle.FirmaDigitalUrl, "firma-digital-medico"); }
                catch { /* se degrada a recuadro vacío, no bloquea la generación del PDF */ }
            }

            return AutorizacionFirmaPdfService.GenerarPdf(detalle, logoBytes, firmaDigitalBytes);
        }

        public async Task SetPinFirmaAsync(int medicoId, string pin, string? callerEmail)
        {
            if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
                throw new AbrilException("El PIN de firma debe tener al menos 4 dígitos.", 400);

            var archivos = await _repo.GetMedicoFirmaArchivosAsync(medicoId)
                ?? throw new AbrilException("Médico no encontrado.", 404);
            await AsegurarEsElPropioMedicoAsync(medicoId, callerEmail, archivos.Email ?? "");

            // Orden obligatorio del flujo de firma: primero la firma digital y la autorización
            // ya escaneada, recién después el PIN. Sin esto, el médico podría empezar a
            // convalidar con un PIN configurado pero sin haber dejado nunca la evidencia física
            // que respalda su firma electrónica.
            if (string.IsNullOrWhiteSpace(archivos.FirmaDigitalUrl))
                throw new AbrilException(
                    "Antes de configurar el PIN debes registrar tu firma digital " +
                    "(Catálogo de Médicos → Firma digital).", 400);
            if (string.IsNullOrWhiteSpace(archivos.UrlAutorizacionFirmada))
                throw new AbrilException(
                    "Antes de configurar el PIN debes imprimir el SSO-FO-149, firmarlo a mano " +
                    "junto a tu firma digital, escanearlo y subirlo (Catálogo de Médicos → " +
                    "Autorización de firma → Subir escaneado).", 400);

            await _repo.SetPinFirmaAsync(medicoId, PinHasher.Hash(pin));
        }

        public async Task<string> SetFirmaDigitalAsync(int medicoId, Stream fileStream, string fileName, string? callerEmail)
        {
            var medicoEmail = await _repo.GetMedicoEmailAsync(medicoId)
                ?? throw new AbrilException("Médico no encontrado.", 404);
            await AsegurarEsElPropioMedicoAsync(medicoId, callerEmail, medicoEmail);

            var url = await _sharePoint.SubirArchivoAsync(fileStream, fileName, "firma-digital-medico");
            await _repo.SetFirmaDigitalAsync(medicoId, url);
            return url;
        }

        public async Task<string> SetAutorizacionFirmadaAsync(int medicoId, Stream fileStream, string fileName, string? callerEmail)
        {
            var medicoEmail = await _repo.GetMedicoEmailAsync(medicoId)
                ?? throw new AbrilException("Médico no encontrado.", 404);
            await AsegurarEsElPropioMedicoAsync(medicoId, callerEmail, medicoEmail);

            var archivos = await _repo.GetMedicoFirmaArchivosAsync(medicoId);
            if (string.IsNullOrWhiteSpace(archivos?.FirmaDigitalUrl))
                throw new AbrilException(
                    "Debes registrar primero tu firma digital — el SSO-FO-149 debe imprimirse " +
                    "con ella ya incluida para poder firmarlo a mano al costado y compararlas.", 400);

            var url = await _sharePoint.SubirArchivoAsync(fileStream, fileName, "autorizacion-firma-medico");
            await _repo.SetAutorizacionFirmadaAsync(medicoId, url);
            return url;
        }

        public Task<List<EmoTipoDto>> ListEmoTipos(bool soloActivos) => _repo.ListEmoTipos(soloActivos);

        public Task<EmoTipoDto> CreateEmoTipo(EmoTipoUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre del tipo de EMO es obligatorio.", 400);
            if (dto.VigenciaMeses < 0)
                throw new AbrilException("La vigencia en meses debe ser mayor o igual a 0.", 400);
            return _repo.CreateEmoTipo(dto);
        }

        public Task<EmoTipoDto> UpdateEmoTipo(int id, EmoTipoUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre del tipo de EMO es obligatorio.", 400);
            if (dto.VigenciaMeses < 0)
                throw new AbrilException("La vigencia en meses debe ser mayor o igual a 0.", 400);
            return _repo.UpdateEmoTipo(id, dto);
        }

        public Task<List<ExamenTipoDto>> ListExamenTipos(bool soloActivos) => _repo.ListExamenTipos(soloActivos);

        public Task<ExamenTipoDto> CreateExamenTipo(ExamenTipoUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre del tipo de examen es obligatorio.", 400);
            return _repo.CreateExamenTipo(dto);
        }

        public Task<ExamenTipoDto> UpdateExamenTipo(int id, ExamenTipoUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre del tipo de examen es obligatorio.", 400);
            return _repo.UpdateExamenTipo(id, dto);
        }

        public Task<List<RestriccionTipoDto>> ListRestriccionTipos(bool soloActivos) => _repo.ListRestriccionTipos(soloActivos);

        public Task<RestriccionTipoDto> CreateRestriccionTipo(RestriccionTipoUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                throw new AbrilException("La descripción de la restricción es obligatoria.", 400);
            return _repo.CreateRestriccionTipo(dto);
        }

        public Task<RestriccionTipoDto> UpdateRestriccionTipo(int id, RestriccionTipoUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                throw new AbrilException("La descripción de la restricción es obligatoria.", 400);
            return _repo.UpdateRestriccionTipo(id, dto);
        }

        public Task<List<AgenteRiesgoDto>> ListAgentesRiesgo(bool soloActivos) => _repo.ListAgentesRiesgo(soloActivos);

        public Task<AgenteRiesgoDto> CreateAgenteRiesgo(AgenteRiesgoUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre del agente de riesgo es obligatorio.", 400);
            if (string.IsNullOrWhiteSpace(dto.Tipo))
                throw new AbrilException("El tipo de agente de riesgo es obligatorio.", 400);
            return _repo.CreateAgenteRiesgo(dto);
        }

        public Task<AgenteRiesgoDto> UpdateAgenteRiesgo(int id, AgenteRiesgoUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre del agente de riesgo es obligatorio.", 400);
            if (string.IsNullOrWhiteSpace(dto.Tipo))
                throw new AbrilException("El tipo de agente de riesgo es obligatorio.", 400);
            return _repo.UpdateAgenteRiesgo(id, dto);
        }

        public Task<List<EmpresaCatalogoDto>> ListEmpresas(bool soloActivas) => _repo.ListEmpresas(soloActivas);

        public Task<SunatContributorDto?> GetEmpresaByRuc(string ruc) => _sunat.GetByRucAsync(ruc);

        public Task<EmpresaCatalogoDto> CreateEmpresa(EmpresaCreateDto dto, int? userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Ruc) || dto.Ruc.Trim().Length != 11)
                throw new AbrilException("El RUC debe tener 11 dígitos.", 400);
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("La razón social es obligatoria.", 400);
            if (string.IsNullOrWhiteSpace(dto.Direccion))
                throw new AbrilException("La dirección es obligatoria.", 400);
            if (string.IsNullOrWhiteSpace(dto.TipoActividad))
                throw new AbrilException("El tipo de actividad es obligatorio.", 400);
            if (string.IsNullOrWhiteSpace(dto.Distrito))
                throw new AbrilException("El distrito es obligatorio.", 400);
            if (string.IsNullOrWhiteSpace(dto.Provincia))
                throw new AbrilException("La provincia es obligatoria.", 400);
            if (string.IsNullOrWhiteSpace(dto.Departamento))
                throw new AbrilException("El departamento es obligatorio.", 400);

            return _repo.CreateEmpresa(dto, userId);
        }

        public Task<List<ClinicaEmailDto>> ListClinicaEmails(int clinicaId) =>
            _repo.ListClinicaEmails(clinicaId);

        public Task<ClinicaEmailDto> CreateClinicaEmail(int clinicaId, ClinicaEmailCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new AbrilException("El email es obligatorio.", 400);
            return _repo.CreateClinicaEmail(clinicaId, dto);
        }

        public Task DeleteClinicaEmail(int clinicaId, int emailId) =>
            _repo.DeleteClinicaEmail(clinicaId, emailId);
    }
}
