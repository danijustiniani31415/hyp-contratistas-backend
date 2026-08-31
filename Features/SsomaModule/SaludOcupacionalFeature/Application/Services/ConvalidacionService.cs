using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services;
using Microsoft.AspNetCore.Hosting;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class ConvalidacionService : IConvalidacionService
    {
        private static readonly HashSet<string> ResultadosValidos = new()
        {
            "Pendiente", "Aprobada", "Aprobada con Observaciones", "Rechazada", "Descartada"
        };

        /// <summary>Resultados que constituyen una decisión médica final y por tanto exigen
        /// firma (PIN + reautenticación de Microsoft). "Pendiente" es un borrador sin
        /// decisión, no requiere firma.</summary>
        private static readonly HashSet<string> ResultadosQueRequierenFirma = new()
        {
            "Aprobada", "Aprobada con Observaciones", "Rechazada"
        };

        private readonly IConvalidacionRepository _repo;
        private readonly IMicrosoftProfileService _microsoftProfile;
        private readonly string[] _logoPaths;

        public ConvalidacionService(
            IConvalidacionRepository repo,
            IMicrosoftProfileService microsoftProfile,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _microsoftProfile = microsoftProfile;
            _logoPaths = new[]
            {
                Path.Combine(env.WebRootPath, "images", "abril-logo.png"),
                Path.Combine(env.WebRootPath, "images", "logo-abril.jpg"),
                Path.Combine(env.ContentRootPath, "Templates", "logo-abril.jpg"),
            };
        }

        public Task<PagedResponseDto<ConvalidacionListDto>> List(ConvalidacionFilterDto filter) => _repo.List(filter);

        public async Task<int> Create(ConvalidacionCreateDto dto, int? userId, string? ip, string? userAgent)
        {
            if (dto.EmoOrigenId <= 0) throw new AbrilException("El EMO es obligatorio.", 400);
            if (!ResultadosValidos.Contains(dto.Resultado))
                throw new AbrilException("El resultado de la convalidación no es válido.", 400);

            await ValidarFirmaAsync(dto.MedicoId, dto.Resultado, dto.PinFirma, dto.MicrosoftAccessToken);

            return await _repo.Create(dto, userId, ip, userAgent);
        }

        public async Task Update(int id, ConvalidacionUpdateDto dto, int? userId, string? ip, string? userAgent)
        {
            if (!ResultadosValidos.Contains(dto.Resultado))
                throw new AbrilException("El resultado de la convalidación no es válido.", 400);

            await ValidarFirmaAsync(dto.MedicoId, dto.Resultado, dto.PinFirma, dto.MicrosoftAccessToken);

            await _repo.Update(id, dto, userId, ip, userAgent);
        }

        /// <summary>
        /// Exige ambos factores para toda decisión final (Aprobada / Aprobada con
        /// Observaciones / Rechazada): el PIN de firma del médico (algo que solo él sabe) y
        /// un access token de Microsoft obtenido con reautenticación fresca (prompt=login),
        /// verificado contra el perfil real vía Graph — no basta con tener sesión abierta.
        /// "Pendiente" es un borrador y no exige firma.
        /// </summary>
        private async Task ValidarFirmaAsync(int? medicoId, string resultado, string? pin, string? microsoftAccessToken)
        {
            if (!ResultadosQueRequierenFirma.Contains(resultado)) return;

            if (medicoId is null)
                throw new AbrilException("Debe seleccionar el médico que autoriza la convalidación.", 400);
            if (string.IsNullOrWhiteSpace(pin))
                throw new AbrilException("Debe ingresar el PIN de firma del médico.", 400);
            if (string.IsNullOrWhiteSpace(microsoftAccessToken))
                throw new AbrilException("Debe reautenticar la sesión de Microsoft para firmar.", 400);

            var estado = await _repo.GetMedicoFirmaEstadoAsync(medicoId.Value)
                ?? throw new AbrilException("Médico no encontrado.", 404);

            if (string.IsNullOrWhiteSpace(estado.PinFirmaHash))
                throw new AbrilException(
                    "El médico debe configurar su PIN de firma antes de autorizar convalidaciones.", 400);

            if (estado.PinFirmaBloqueadoHasta.HasValue && estado.PinFirmaBloqueadoHasta > DateTimeOffset.UtcNow)
            {
                var minutos = Math.Ceiling((estado.PinFirmaBloqueadoHasta.Value - DateTimeOffset.UtcNow).TotalMinutes);
                throw new AbrilException(
                    $"PIN de firma bloqueado por intentos fallidos. Vuelva a intentar en {minutos} minuto(s).", 423);
            }

            if (!PinHasher.Verify(pin, estado.PinFirmaHash))
            {
                await _repo.RegistrarIntentoFirmaAsync(medicoId.Value, exito: false);
                throw new AbrilException("PIN de firma incorrecto.", 401);
            }

            var profile = await _microsoftProfile.GetProfile(microsoftAccessToken);
            var msEmail = profile?.Mail ?? profile?.UserPrincipalName;
            if (profile is null || string.IsNullOrWhiteSpace(msEmail))
            {
                await _repo.RegistrarIntentoFirmaAsync(medicoId.Value, exito: false);
                throw new AbrilException(
                    "No se pudo validar la sesión de Microsoft. Vuelva a iniciar sesión para firmar.", 401);
            }

            if (string.IsNullOrWhiteSpace(estado.Email)
                || !string.Equals(estado.Email.Trim(), msEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await _repo.RegistrarIntentoFirmaAsync(medicoId.Value, exito: false);
                throw new AbrilException(
                    "La cuenta de Microsoft reautenticada no coincide con el médico seleccionado.", 403);
            }

            await _repo.RegistrarIntentoFirmaAsync(medicoId.Value, exito: true);
        }

        public async Task<byte[]> GenerarPdfAsync(int id)
        {
            var detalle = await _repo.GetDetalleAsync(id)
                ?? throw new AbrilException("Convalidación no encontrada.", 404);

            byte[]? logoBytes = null;
            var logoPath = _logoPaths.FirstOrDefault(File.Exists);
            if (logoPath != null)
                logoBytes = await File.ReadAllBytesAsync(logoPath);

            return ConvalidacionPdfService.GenerarPdf(detalle, logoBytes);
        }
    }
}
