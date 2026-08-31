using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Services
{
    public class MiSaludService : IMiSaludService
    {
        private const string EmailAsistentaSocial   = "pquispe@abril.pe";
        private const string EmailMedicoOcupacional = "mediconm@abril.pe";

        // Códigos (deben coincidir con ss_descanso_correo_config.codigo) que mapean
        // cada destinatario del correo a su toggle activar/inactivar.
        private const string CorreoTrabajador        = "TRABAJADOR";
        private const string CorreoAsistentaSocial   = "ASISTENTA_SOCIAL";
        private const string CorreoGth               = "GTH";
        private const string CorreoMedicoOcupacional = "MEDICO_OCUPACIONAL";

        private readonly IMiSaludRepository _repo;
        private readonly IDescansoCertificadoStorage _certificados;
        private readonly IEmailService _emailService;
        private readonly ILogger<MiSaludService> _logger;

        public MiSaludService(
            IMiSaludRepository repo,
            IDescansoCertificadoStorage certificados,
            IEmailService emailService,
            ILogger<MiSaludService> logger)
        {
            _repo         = repo;
            _certificados = certificados;
            _emailService = emailService;
            _logger       = logger;
        }

        public async Task<MiSaludResumenDto> GetResumen(int userId)
        {
            var workerId = await _repo.ResolverWorkerIdAsync(userId);
            return await _repo.GetResumen(workerId);
        }

        public async Task<PagedResult<MiDescansoDto>> GetDescansos(int userId, int page)
        {
            var workerId = await _repo.ResolverWorkerIdAsync(userId);
            return await _repo.GetDescansos(workerId, page);
        }

        public async Task<int> CreateDescanso(int userId, CrearMiDescansoDto dto)
        {
            if (dto.FechaFin < dto.FechaInicio)
                throw new AbrilException("La fecha de fin no puede ser anterior a la fecha de inicio.", 400);

            var workerId = await _repo.ResolverWorkerIdAsync(userId);

            // Los certificados van a la carpeta de SharePoint configurada en ss_descanso_carpeta.
            // A diferencia de antes, si la subida falla el registro no se guarda: un descanso sin
            // su certificado no le sirve a nadie y el trabajador no se enteraba de que se perdió.
            var adjuntos = await _certificados.SubirAsync(dto.Documentos ?? [], "misalud");

            var descansoId = await _repo.CreateDescanso(workerId, dto, userId, adjuntos);

            // Notificación por correo (best-effort): el registro nunca falla por el email.
            try
            {
                await SendNotificacionDescansoAsync(workerId, userId, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando notificación de descanso médico {DescansoId} (worker {WorkerId})", descansoId, workerId);
            }

            return descansoId;
        }

        public async Task<DescansoCertificadoArchivoDto> GetCertificado(int userId, int adjuntoId)
        {
            var workerId = await _repo.ResolverWorkerIdAsync(userId);

            var adjunto = await _repo.GetAdjuntoDelWorkerAsync(adjuntoId, workerId)
                ?? throw new AbrilException("El certificado solicitado no existe o no te pertenece.", 404);

            return await _certificados.DescargarAsync(
                    adjunto.DriveId, adjunto.ItemId, adjunto.Url, adjunto.NombreArchivo)
                ?? throw new AbrilException("No se pudo obtener el certificado desde SharePoint.", 502);
        }

        public async Task<List<MiDescansoCorreoConfigDto>> GetCorreoConfigs()
            => await _repo.GetCorreoConfigsAsync();

        public async Task SetCorreoConfigActive(int id, bool active)
        {
            var ok = await _repo.SetCorreoConfigActiveAsync(id, active);
            if (!ok)
                throw new AbrilException("No se encontró el destinatario de correo indicado.", 404);
        }

        /// <summary>
        /// Correo al registrar un descanso médico. Destinatarios: el propio
        /// trabajador, la asistenta social, el área GTH (area_scope.email) y el
        /// médico ocupacional. Cada destinatario se envía solo si está activo en
        /// ss_descanso_correo_config (toggle configurable desde Mi Salud → Configuración).
        /// El primer destinatario activo con correo va en TO y el resto en CC, de modo
        /// que apagar al trabajador no impide notificar a los demás (útil para pruebas).
        /// </summary>
        private async Task SendNotificacionDescansoAsync(int workerId, int userId, CrearMiDescansoDto dto)
        {
            var datos  = await _repo.GetDatosNotificacionDescansoAsync(workerId, userId, dto.TipoId);
            var config = await _repo.GetCorreoConfigMapAsync();

            // Un destinatario está activo salvo que exista una fila explícita con active=false.
            // Así, si la tabla aún no está poblada, se conserva el comportamiento de enviar a todos.
            bool Activo(string codigo) => !config.TryGetValue(codigo, out var a) || a;

            var candidatos = new (string Codigo, string? Email)[]
            {
                (CorreoTrabajador,        datos.WorkerEmail),
                (CorreoAsistentaSocial,   EmailAsistentaSocial),
                (CorreoGth,               datos.GthEmail),
                (CorreoMedicoOcupacional, EmailMedicoOcupacional),
            };

            var destinatarios = new List<string>();
            foreach (var (codigo, email) in candidatos)
            {
                if (!Activo(codigo)) continue;
                if (string.IsNullOrWhiteSpace(email))
                {
                    if (codigo == CorreoGth)
                        _logger.LogWarning("Notificación de descanso médico sin copia a GTH: no hay correo configurado en area_scope.email.");
                    else if (codigo == CorreoTrabajador)
                        _logger.LogWarning("Notificación de descanso médico sin el trabajador {WorkerId}: no tiene correo registrado.", workerId);
                    continue;
                }

                var e = email.Trim();
                if (!destinatarios.Any(x => x.Equals(e, StringComparison.OrdinalIgnoreCase)))
                    destinatarios.Add(e);
            }

            if (destinatarios.Count == 0)
            {
                _logger.LogWarning(
                    "No se envió notificación de descanso médico (worker {WorkerId}): no hay destinatarios activos con correo.",
                    workerId);
                return;
            }

            var to = new List<string> { destinatarios[0] };
            var cc = destinatarios.Skip(1).ToList();

            var nombre = datos.WorkerNombre ?? "Trabajador";
            var dias   = dto.Dias ?? (dto.FechaFin.DayNumber - dto.FechaInicio.DayNumber + 1);

            var subject = $"Descanso médico registrado - {nombre} - {dto.FechaInicio:dd/MM/yyyy}";
            var body = $"""
                <p>Se ha registrado un <strong>descanso médico</strong> en la intranet, pendiente de aprobación.</p>
                <table style="border-collapse:collapse;font-family:Arial;font-size:13px;">
                  <tr><td style="padding:4px 12px;font-weight:bold;">Trabajador</td><td>{nombre}</td></tr>
                  <tr><td style="padding:4px 12px;font-weight:bold;">Fecha de inicio</td><td>{dto.FechaInicio:dd/MM/yyyy}</td></tr>
                  <tr><td style="padding:4px 12px;font-weight:bold;">Fecha de fin</td><td>{dto.FechaFin:dd/MM/yyyy}</td></tr>
                  <tr><td style="padding:4px 12px;font-weight:bold;">Días</td><td>{dias}</td></tr>
                  <tr><td style="padding:4px 12px;font-weight:bold;">Tipo</td><td>{datos.TipoNombre ?? "—"}</td></tr>
                  {(string.IsNullOrWhiteSpace(dto.Diagnostico) ? "" : $"<tr><td style='padding:4px 12px;font-weight:bold;'>Diagnóstico</td><td>{dto.Diagnostico}</td></tr>")}
                  <tr><td style="padding:4px 12px;font-weight:bold;">Estado</td><td>Pendiente de aprobación</td></tr>
                </table>
                <p style="color:#666;font-size:11px;margin-top:16px;">Sistema SSOMA - Abril</p>
                """;

            await _emailService.SendAsync(
                to: to,
                subject: subject,
                body: body,
                isHtml: true,
                cc: cc.Count > 0 ? cc : null);
        }
    }
}
