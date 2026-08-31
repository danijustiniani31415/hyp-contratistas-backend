using System.Text.RegularExpressions;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    /// <summary>
    /// Configuración de los destinatarios de los 4 correos de EMO: programación
    /// automática, programación manual, aceptada por la clínica y rechazada por la
    /// clínica. Cada correo se configura por perfil de trabajador
    /// (Oficina Central / Staff / Obra / Contratista).
    /// </summary>
    public class EmoCorreoConfigService : IEmoCorreoConfigService
    {
        private static readonly Regex EmailRegex =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private static readonly string[] TiposValidos =
            { EmoCorreoTipoCodigo.Principal, EmoCorreoTipoCodigo.Copia };

        private readonly IEmoCorreoConfigRepository _repo;

        public EmoCorreoConfigService(IEmoCorreoConfigRepository repo)
        {
            _repo = repo;
        }

        public Task<EmoCorreosConfigDto> GetConfig() => _repo.GetConfigAsync();

        public Task<int> CrearAdicional(EmoCorreoAdicionalCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EventoCodigo))
                throw new AbrilException("Falta indicar a qué correo se agrega el destinatario.", 400);

            var email = ValidarEmail(dto.Email);
            return _repo.CreateAdicionalAsync(
                dto.EventoCodigo.Trim(), ValidarTipo(dto.Tipo), email, dto.Nombre);
        }

        public Task ActualizarDestinatario(int id, EmoCorreoDestinatarioUpdateDto dto)
        {
            var email = ValidarEmail(dto.Email);
            var tipo  = string.IsNullOrWhiteSpace(dto.Tipo) ? null : ValidarTipo(dto.Tipo);
            return _repo.UpdateDestinatarioAsync(id, email, dto.Nombre, tipo);
        }

        public Task SetReglaActive(int reglaId, bool active) => _repo.SetReglaActiveAsync(reglaId, active);

        public Task EliminarAdicional(int id) => _repo.DeleteAdicionalAsync(id);

        private static string ValidarTipo(string? tipo)
        {
            var valor = (tipo ?? EmoCorreoTipoCodigo.Principal).Trim().ToUpperInvariant();
            if (!TiposValidos.Contains(valor))
                throw new AbrilException("El tipo de destinatario debe ser PRINCIPAL o COPIA.", 400);
            return valor;
        }

        private static string ValidarEmail(string? email)
        {
            var valor = (email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(valor))
                throw new AbrilException("El correo es obligatorio.", 400);
            if (valor.Length > 150)
                throw new AbrilException("El correo no puede superar los 150 caracteres.", 400);
            if (!EmailRegex.IsMatch(valor))
                throw new AbrilException("El correo no tiene un formato válido.", 400);
            return valor;
        }
    }
}
