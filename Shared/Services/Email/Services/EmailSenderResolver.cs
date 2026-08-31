using Microsoft.Extensions.Options;
using Abril_Backend.Shared.Services.Email.Configuration;
using Abril_Backend.Shared.Services.Email.Interfaces;

namespace Abril_Backend.Shared.Services.Email.Services
{
    /// <inheritdoc cref="IEmailSenderResolver"/>
    public class EmailSenderResolver : IEmailSenderResolver
    {
        private readonly Dictionary<string, EmailSenderOptions> _byKey;
        private readonly Dictionary<string, EmailSenderOptions> _byAddress;
        private readonly ILogger<EmailSenderResolver> _logger;

        public EmailSenderOptions Default { get; }

        public EmailSenderResolver(IOptions<EmailOptions> options, ILogger<EmailSenderResolver> logger)
        {
            _logger = logger;
            var settings = options.Value;

            if (settings.Senders is null || settings.Senders.Count == 0)
                throw new InvalidOperationException(
                    "Email:Senders está vacío. Configura al menos un remitente en el appsettings.");

            // Case-insensitive para que "Gth" y "GTH" resuelvan al mismo buzón: el binder de
            // configuración crea el diccionario con comparador ordinal y una diferencia de
            // mayúsculas terminaría cayendo al remitente por defecto sin motivo.
            _byKey = new Dictionary<string, EmailSenderOptions>(StringComparer.OrdinalIgnoreCase);
            _byAddress = new Dictionary<string, EmailSenderOptions>(StringComparer.OrdinalIgnoreCase);

            foreach (var (key, sender) in settings.Senders)
            {
                if (string.IsNullOrWhiteSpace(sender?.Address))
                    throw new InvalidOperationException(
                        $"El remitente Email:Senders:{key} no tiene Address configurada.");

                _byKey[key] = sender;
                _byAddress[sender.Address.Trim()] = sender;
            }

            if (string.IsNullOrWhiteSpace(settings.DefaultSender))
                throw new InvalidOperationException(
                    "Email:DefaultSender no está configurado. Debe apuntar a una clave de Email:Senders.");

            if (!_byKey.TryGetValue(settings.DefaultSender, out var defaultSender))
                throw new InvalidOperationException(
                    $"Email:DefaultSender = '{settings.DefaultSender}' no existe en Email:Senders. " +
                    $"Remitentes registrados: {string.Join(", ", _byKey.Keys)}.");

            Default = defaultSender;
        }

        public EmailSenderOptions Resolve(string? sender)
        {
            if (string.IsNullOrWhiteSpace(sender))
                return Default;

            var value = sender.Trim();

            if (_byKey.TryGetValue(value, out var byKey))
                return byKey;

            // Red de seguridad para call sites que todavía pasen la dirección literal en vez
            // de la clave: sigue admitiendo solo buzones registrados, no cualquier dirección.
            if (_byAddress.TryGetValue(value, out var byAddress))
                return byAddress;

            // Nunca se envía desde un buzón no registrado: no tendría permiso Send As en el
            // Flow de PowerAutomate y el envío fallaría dentro del Flow, que corre asíncrono
            // y devuelve 202 antes de enviar — o sea, sin error visible para el backend.
            // Se cae al remitente por defecto, pero el warning deja rastro del sender inválido.
            _logger.LogWarning(
                "Remitente de correo '{Sender}' no está registrado en Email:Senders; se usa el " +
                "remitente por defecto '{Default}'. Remitentes válidos: {Validos}.",
                value, Default.Address, string.Join(", ", _byKey.Keys));

            return Default;
        }
    }
}
