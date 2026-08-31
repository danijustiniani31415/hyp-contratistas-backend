using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Dtos;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Services
{
    public class OnboardingService : IOnboardingService
    {
        private readonly IOnboardingRepository _repo;
        private readonly IFileDigitalColaboradorService _fileDigital;
        private readonly IEmailService _email;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OnboardingService> _logger;

        public OnboardingService(
            IOnboardingRepository repo,
            IFileDigitalColaboradorService fileDigital,
            IEmailService email,
            IConfiguration configuration,
            ILogger<OnboardingService> logger)
        {
            _repo          = repo;
            _fileDigital   = fileDigital;
            _email         = email;
            _configuration = configuration;
            _logger        = logger;
        }

        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        /// <summary>
        /// Formato aceptado para la carta oferta: PDF y nada más. El colaborador la ve dentro de la
        /// intranet y la firma ahí mismo, y las dos cosas necesitan un PDF — un DOC/DOCX no se puede
        /// mostrar en el navegador ni estampar sin convertirlo, y convertirlo puede mover el formato
        /// de la carta que GTH revisó. Se corta al subir, que es donde el mensaje sirve de algo.
        /// </summary>
        private static readonly HashSet<string> AllowedCartaExt = new(StringComparer.OrdinalIgnoreCase)
            { ".pdf" };

        /// <summary>
        /// Formatos aceptados para la carta oferta FIRMADA que sube GTH a mano (la vía de respaldo,
        /// para el colaborador que firma en papel en vez de usar el enlace). Acá no hay nada que
        /// estampar, así que se mantienen los formatos que ya se aceptaban.
        /// </summary>
        private static readonly HashSet<string> AllowedCartaFirmadaExt = new(StringComparer.OrdinalIgnoreCase)
            { ".pdf", ".doc", ".docx" };

        private const long MaxCartaBytes = 15L * 1024 * 1024; // 15 MB

        public Task<BandejaOnboardingDto> GetBandeja() => _repo.GetBandeja();

        public async Task<OnboardingCreateResultDto> Iniciar(
            OnboardingCreateDto dto,
            string cartaFileName,
            string cartaContentType,
            byte[] cartaContent,
            int? userId)
        {
            if (dto == null || dto.CandidatoId <= 0)
                throw new AbrilException("Selecciona al colaborador que inicia el onboarding.", 400);

            if (cartaContent == null || cartaContent.Length == 0)
                throw new AbrilException("Adjunta la carta oferta para poder enviarla.", 400);

            var ext = Path.GetExtension(cartaFileName);
            if (!AllowedCartaExt.Contains(ext))
                throw new AbrilException(
                    "La carta oferta debe ser un PDF: es el formato que el colaborador puede ver y firmar desde el enlace.", 400);
            if (cartaContent.Length > MaxCartaBytes)
                throw new AbrilException("La carta oferta supera el tamaño máximo permitido (15 MB).", 400);

            // Un correo escrito a mano se valida acá; el que sale de la base de datos ya es válido.
            var correoManual = string.IsNullOrWhiteSpace(dto.Correo) ? null : dto.Correo.Trim().ToLowerInvariant();
            if (correoManual != null && !EmailRegex.IsMatch(correoManual))
                throw new AbrilException("El correo indicado para la carta oferta no es válido.", 400);

            // 1) Validar que el candidato pueda entrar a onboarding y resolver el correo destino y su
            //    ficha de la base maestra ANTES de tocar SharePoint o mandar correos.
            var ctx = await _repo.PrepararInicio(dto.CandidatoId, dto.FechaIngreso, correoManual);
            ctx.Token = NuevoToken();

            // 2) Resolver la carpeta destino de la carta ANTES de enviar el correo. Si la biblioteca
            //    no está configurada o no se puede resolver, esto corta acá: lo contrario sería
            //    avisarle al colaborador para después fallar al guardar la carta y dejarlo con un
            //    enlace que no tiene nada que mostrar.
            var carpeta = await _fileDigital.ResolverCarpetaAsync(ctx.Dni, ctx.Nombre);

            // 3) Guardar la carta en el file del colaborador y registrar el onboarding. Va ANTES del
            //    correo —al revés que en el flujo viejo, donde la carta iba adjunta— porque ahora el
            //    correo solo lleva un enlace: si el enlace saliera primero, apuntaría a un token que
            //    todavía no existe y a una carta que todavía no está guardada. Con este orden, un
            //    correo que falla deja un onboarding completo del que GTH reenvía el enlace.
            var carta = await _fileDigital.SubirDocumentoAsync(
                carpeta, SubcarpetaFileDigital.CartaEnviada,
                _fileDigital.NombreArchivo("carta_oferta", ctx.Codigo, ext),
                cartaContent, cartaContentType, "la carta oferta");

            var colaborador = await _repo.Crear(ctx, carta, carpeta, dto.Observacion, userId);

            // 4) Avisarle al colaborador con el enlace a su carta. La carta NO va adjunta: se ve
            //    dentro de la intranet, que es donde también la firma.
            try
            {
                await EnviarCorreoEnlaceAsync(ctx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falló el correo del enlace de la carta oferta del onboarding {OnboardingId}",
                    colaborador.OnboardingId);
                throw new AbrilException(
                    $"El onboarding quedó abierto y la carta oferta guardada, pero no se pudo enviar el correo a {ctx.Correo}. " +
                    "Reenvía el enlace desde el detalle del colaborador.", 502);
            }

            return new OnboardingCreateResultDto
            {
                OnboardingId = colaborador.OnboardingId,
                Colaborador  = colaborador,
                Message      = $"Onboarding iniciado. Se le envió a {ctx.Correo} el enlace para revisar y firmar su carta oferta.",
            };
        }

        public async Task<OnboardingAccionResultDto> ReenviarEnlaceFirma(
            int onboardingId, string? correo, int? userId)
        {
            var correoManual = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim().ToLowerInvariant();
            if (correoManual != null && !EmailRegex.IsMatch(correoManual))
                throw new AbrilException("El correo indicado para el enlace de firma no es válido.", 400);

            // El token nuevo solo se usa si la fila no tiene uno (onboarding abierto antes de que
            // existiera la firma en línea): si ya tiene, se conserva para no romper el enlace que el
            // colaborador pueda tener en su bandeja.
            var ctx = await _repo.PrepararReenvio(onboardingId, correoManual, NuevoToken());

            await EnviarCorreoEnlaceAsync(ctx);

            // Recién con el correo afuera se deja registrado el envío (y el token, si era nuevo).
            var colaborador = await _repo.MarcarEnlaceEnviado(onboardingId, ctx, userId);

            return new OnboardingAccionResultDto
            {
                Colaborador = colaborador,
                Message     = $"Enlace de firma reenviado a {ctx.Correo}.",
            };
        }

        /// <summary>
        /// Manda el correo con el enlace a la página donde el colaborador ve y firma su carta oferta.
        /// Lo usan el alta del onboarding y el reenvío, así que el correo que recibe el colaborador es
        /// el mismo en los dos casos. Las excepciones se dejan salir: cada quien decide qué hacer con
        /// un correo que no salió (el alta ya tiene la fila creada, el reenvío no escribió nada).
        /// </summary>
        private async Task EnviarCorreoEnlaceAsync(OnboardingContextoDto ctx)
        {
            await _email.SendAsync(
                to:      new List<string> { ctx.Correo },
                subject: $"Carta oferta — {ctx.Puesto} · Abril Grupo Inmobiliario",
                body:    ConstruirCuerpoEnlaceCartaOferta(ctx, ConstruirLinkFirma(ctx.Token)),
                isHtml:  true);
        }

        /// <summary>Token del enlace público (hex, url-safe). Mismo formato que el del formulario del postulante.</summary>
        private static string NuevoToken() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

        /// <summary>Enlace público donde el colaborador ve y firma su carta oferta.</summary>
        private string ConstruirLinkFirma(string token)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? string.Empty;
            return $"{frontendUrl}/postulante/carta-oferta?token={Uri.EscapeDataString(token)}";
        }

        // ── Carta oferta firmada ───────────────────────────────────────────────

        public async Task<OnboardingAccionResultDto> SubirCartaFirmada(
            int onboardingId, string fileName, string contentType, byte[] content, int? userId)
        {
            if (content == null || content.Length == 0)
                throw new AbrilException("Adjunta la carta oferta firmada.", 400);

            var ext = Path.GetExtension(fileName);
            if (!AllowedCartaFirmadaExt.Contains(ext))
                throw new AbrilException("La carta oferta firmada tiene un formato no permitido. Solo PDF, DOC o DOCX.", 400);
            if (content.Length > MaxCartaBytes)
                throw new AbrilException("La carta oferta firmada supera el tamaño máximo permitido (15 MB).", 400);

            var ctx = await _repo.PrepararDocumento(onboardingId);

            // La carta firmada va al MISMO file que la enviada, pero a su propia subcarpeta. El file
            // normalmente ya está guardado en el onboarding; los abiertos antes de que se persistiera
            // se resuelven por nombre, que es exactamente como se resolvió la primera vez
            // (EnsureChildFolder devuelve la existente).
            var carpeta = ctx.Carpeta ?? await _fileDigital.ResolverCarpetaAsync(ctx.Dni, ctx.Nombre);

            var carta = await _fileDigital.SubirDocumentoAsync(
                carpeta, SubcarpetaFileDigital.CartaFirmada,
                _fileDigital.NombreArchivo("carta_oferta_firmada", ctx.Codigo, ext),
                content, contentType, "la carta oferta firmada");

            var colaborador = await _repo.GuardarCartaFirmada(onboardingId, carta, carpeta, userId);

            return new OnboardingAccionResultDto
            {
                Colaborador = colaborador,
                Message     = "Carta oferta firmada adjuntada al file digital. Queda pendiente de tu aprobación.",
            };
        }

        public async Task<OnboardingAccionResultDto> AprobarCartaFirmada(int onboardingId, int? userId)
        {
            var colaborador = await _repo.AprobarCartaFirmada(onboardingId, userId);
            return new OnboardingAccionResultDto
            {
                Colaborador = colaborador,
                Message     = "Carta oferta firmada aprobada.",
            };
        }

        public async Task<OnboardingAccionResultDto> Avanzar(int onboardingId, int? userId)
        {
            var colaborador = await _repo.Avanzar(onboardingId, userId);
            return new OnboardingAccionResultDto
            {
                Colaborador = colaborador,
                Message     = $"Onboarding avanzado a la fase «{colaborador.FaseNombre}».",
            };
        }

        /// <summary>
        /// Correo genérico con el enlace a la carta oferta. La carta NO va adjunta: el colaborador
        /// entra al enlace, la lee ahí, registra su firma y la firma en la misma página. El cuerpo
        /// resume la posición para que reconozca de qué proceso se trata sin abrir nada, pero las
        /// condiciones de la propuesta solo se ven dentro de la intranet.
        /// </summary>
        private static string ConstruirCuerpoEnlaceCartaOferta(OnboardingContextoDto ctx, string link)
        {
            static string Esc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
            static string Fila(string etiqueta, string? valor) =>
                string.IsNullOrWhiteSpace(valor)
                    ? string.Empty
                    : $"""
                        <tr>
                          <td style="padding:6px 10px;font-size:12px;color:#6b7280;white-space:nowrap">{Esc(etiqueta)}</td>
                          <td style="padding:6px 10px;font-size:13px;color:#1f2937"><b>{Esc(valor)}</b></td>
                        </tr>
                        """;

            var nombre = string.IsNullOrWhiteSpace(ctx.Nombre) ? "colaborador(a)" : Esc(ctx.Nombre);

            return $"""
                <div style="font-family:Arial,sans-serif;max-width:640px">
                  <div style="background:#005D9D;padding:14px 18px">
                    <h2 style="color:#fff;margin:0;font-size:18px">¡Bienvenido(a) a Abril Grupo Inmobiliario!</h2>
                  </div>
                  <div style="padding:18px;border:1px solid #e5e7eb;border-top:none">
                    <p style="font-size:13px;margin-top:0">Estimado(a) {nombre},</p>
                    <p style="font-size:13px">
                      Nos complace informarte que fuiste seleccionado(a) para la posición de
                      <b>{Esc(ctx.Puesto)}</b> en <b>Abril Grupo Inmobiliario</b>. Ya tienes disponible
                      tu <b>carta oferta</b> con las condiciones de la propuesta.
                    </p>
                    <table style="border-collapse:collapse;margin:14px 0;background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px">
                      {Fila("Puesto", ctx.Puesto)}
                      {Fila("Área", ctx.Area)}
                      {Fila("Proyecto / obra", ctx.ProyectoObra)}
                      {Fila("Empresa", ctx.Empresa)}
                      {Fila("Jefe directo", ctx.JefeDirecto)}
                      {Fila("Fecha de ingreso", ctx.FechaIngreso?.ToString("dd/MM/yyyy"))}
                    </table>
                    <p style="font-size:13px">
                      Ingresa al siguiente enlace para <b>leer tu carta oferta, registrar tu firma y
                      firmarla en línea</b>. No necesitas imprimir ni escanear nada.
                    </p>
                    <p style="margin:18px 0">
                      <a href="{Esc(link)}"
                         style="background:#005D9D;color:#fff;text-decoration:none;padding:11px 22px;border-radius:6px;font-size:13px;font-weight:bold;display:inline-block">
                        Ver y firmar mi carta oferta
                      </a>
                    </p>
                    <p style="font-size:11.5px;color:#888;word-break:break-all">
                      Si el botón no funciona, copia y pega este enlace en tu navegador:<br>{Esc(link)}
                    </p>
                    <p style="font-size:12.5px;color:#555">
                      Este enlace es personal: no lo compartas. Si tienes alguna consulta sobre la
                      propuesta, respóndenos este correo y el equipo de Gestión de Talento Humano te
                      apoyará.
                    </p>
                    <p style="font-size:11px;color:#888;margin-top:18px">Correo automático de Abril One · Gestión GTH · Onboarding.</p>
                  </div>
                </div>
                """;
        }

    }
}
