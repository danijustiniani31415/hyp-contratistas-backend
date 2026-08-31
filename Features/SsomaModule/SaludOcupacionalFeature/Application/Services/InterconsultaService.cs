using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Shared;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class InterconsultaService : IInterconsultaService
    {
        private static readonly HashSet<string> EstadosValidos = new() { "Pendiente", "Atendida", "Cancelada" };
        /// <summary>Ids de workers_obra_oficina_staff cuyo personal tiene correo corporativo propio.</summary>
        private static readonly HashSet<int> ObraOficinaConCorreoPropio = new(Abril_Backend.Shared.Constants.ObraOficinaStaffIds.StaffUOficinaCentral);

        private readonly IInterconsultaRepository _repo;
        private readonly IEmailService _emailService;

        public InterconsultaService(IInterconsultaRepository repo, IEmailService emailService)
        {
            _repo = repo;
            _emailService = emailService;
        }

        public Task<PagedResult<InterconsultaListDto>> List(InterconsultaFilterDto filter) => _repo.List(filter);

        public async Task<InterconsultaEnviarCorreoResultDto> EnviarRecordatorios(List<int> ids)
        {
            var result = new InterconsultaEnviarCorreoResultDto();
            if (ids == null || ids.Count == 0)
                throw new AbrilException("Debe seleccionar al menos una interconsulta.", 400);

            var info = await _repo.GetForEnvioCorreo(ids);
            result.TotalSeleccionadas = info.Count;
            if (info.Count == 0)
                throw new AbrilException("No se encontraron interconsultas para las seleccionadas.", 404);

            // Staff/Oficina Central con correo corporativo propio → correo individual a él + su jefatura de proyecto.
            var conCorreoPropio = info
                .Where(x => x.TieneCorreoPropio && ObraOficinaConCorreoPropio.Contains(x.ObraOficinaStaffId ?? 0))
                .ToList();

            // Obra/Contratista (u otros sin correo propio) → se agrupan por proyecto en un solo
            // correo consolidado al administrador encargado, porque el trabajador no tiene email.
            var sinCorreoPropio = info.Except(conCorreoPropio).ToList();

            foreach (var item in conCorreoPropio)
            {
                // Oficina Central no tiene proyecto: se notifica a su jefatura + admin de la empresa.
                // Staff sí está asignado a un proyecto real, se notifica a la jefatura de ese proyecto.
                var destinatariosRaw = item.EsOficinaCentral
                    ? new List<string?> { item.WorkerEmailCorporativo, item.JefaturaEmail, item.ContributorEmailAdministrador }
                    : new List<string?>
                    {
                        item.WorkerEmailCorporativo,
                        item.ProyectoEmailResidente,
                        item.ProyectoEmailResponsable,
                        item.ProyectoEmailRrhh,
                        item.ProyectoEmailCoordSsoma,
                        item.ProyectoEmailCoordAdmin
                    };

                var destinatarios = destinatariosRaw
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

                if (destinatarios.Count == 0)
                {
                    result.TotalErrores++;
                    result.Detalles.Add($"Interconsulta {item.Id} ({item.WorkerNombre}) — sin destinatarios. Omitida.");
                    continue;
                }

                try
                {
                    await _emailService.SendAsync(
                        to: destinatarios,
                        subject: $"Recordatorio de interconsulta pendiente - {item.WorkerNombre}",
                        body: BuildBodyIndividual(item),
                        isHtml: true,
                        sender: SaludOcupacionalEmailConstants.SenderKey);

                    result.TotalEnviados++;
                    result.Detalles.Add($"Interconsulta {item.Id} ({item.WorkerNombre}) — enviado a {destinatarios.Count} destinatario(s).");
                }
                catch (Exception ex)
                {
                    result.TotalErrores++;
                    result.Detalles.Add($"Interconsulta {item.Id} ({item.WorkerNombre}) — error al enviar: {ex.Message}");
                }
            }

            var gruposPorProyecto = sinCorreoPropio
                .GroupBy(x => new { x.ProyectoId, x.ProyectoNombre, x.ContributorNombre, Admin = x.ProyectoEmailCoordAdmin ?? x.ContributorEmailAdministrador });

            foreach (var grupo in gruposPorProyecto)
            {
                var admin = grupo.Key.Admin;
                var proyectoNombre = grupo.Key.ProyectoNombre ?? "Sin proyecto asignado";

                if (string.IsNullOrWhiteSpace(admin))
                {
                    result.TotalErrores += grupo.Count();
                    foreach (var it in grupo)
                        result.Detalles.Add($"Interconsulta {it.Id} ({it.WorkerNombre}) — sin administrador encargado en '{proyectoNombre}'. Omitida.");
                    continue;
                }

                try
                {
                    await _emailService.SendAsync(
                        to: new List<string> { admin.Trim() },
                        subject: $"Interconsultas pendientes - {proyectoNombre}",
                        body: BuildBodyConsolidado(proyectoNombre, grupo.Key.ContributorNombre, grupo.ToList()),
                        isHtml: true,
                        sender: SaludOcupacionalEmailConstants.SenderKey);

                    result.TotalEnviados += grupo.Count();
                    result.Detalles.Add($"{proyectoNombre} — enviado a {admin} ({grupo.Count()} trabajador(es) sin correo propio).");
                }
                catch (Exception ex)
                {
                    result.TotalErrores += grupo.Count();
                    result.Detalles.Add($"{proyectoNombre} — error al enviar a {admin}: {ex.Message}");
                }
            }

            return result;
        }

        private static string BuildBodyIndividual(InterconsultaEnvioInfoDto item)
        {
            return $@"
            <p>Estimados,</p>
            <p>
                Se informa que el trabajador <strong>{item.WorkerNombre}</strong> (DNI {item.WorkerDni})
                registra una <strong>interconsulta médica pendiente</strong>, con
                <strong style='color: #b00020;'>{item.DiasPendiente} día(s)</strong> de retraso.
            </p>
            <p>Se agradece coordinar su atención a la brevedad.</p>
            <p style='font-size: 12px; color: #666; margin-top: 24px;'>
                Este mensaje contiene información confidencial de salud ocupacional, de uso exclusivo del
                destinatario. Módulo de Interconsultas — Salud Ocupacional.
            </p>
            ";
        }

        private static string BuildBodyConsolidado(string proyectoNombre, string? razonSocial, List<InterconsultaEnvioInfoDto> items)
        {
            var filas = string.Join("", items.Select(it => $@"
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{it.WorkerNombre}</td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{it.WorkerDni}</td>
                    <td style='border: 1px solid #ddd; padding: 8px; color: #b00020;'><strong>{it.DiasPendiente} d.</strong></td>
                </tr>"));

            return $@"
            <p>Estimados,</p>
            <p>
                Se informa que los siguientes trabajadores de <strong>{proyectoNombre}</strong>{(string.IsNullOrWhiteSpace(razonSocial) ? "" : $" ({razonSocial})")}
                registran una <strong>interconsulta médica pendiente</strong>. Se agradece coordinar su atención a la brevedad:
            </p>
            <table style='border-collapse: collapse; font-family: Arial, sans-serif; font-size: 14px; width: 100%;'>
                <thead>
                    <tr>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>Trabajador</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>DNI</th>
                        <th style='border: 1px solid #ddd; padding: 8px; background: #f3f4f6;'>Días de retraso</th>
                    </tr>
                </thead>
                <tbody>{filas}</tbody>
            </table>
            <p style='font-size: 12px; color: #666; margin-top: 24px;'>
                Este mensaje contiene información confidencial de salud ocupacional, de uso exclusivo del
                destinatario. Módulo de Interconsultas — Salud Ocupacional.
            </p>
            ";
        }

        public Task<InterconsultaDetalleDto> GetById(int id) => _repo.GetById(id);

        public Task<int> Create(InterconsultaCreateDto dto, int? userId)
        {
            if (dto.EmoId <= 0) throw new AbrilException("El EMO es obligatorio.", 400);
            if (dto.WorkerId <= 0) throw new AbrilException("El trabajador es obligatorio.", 400);
            if (string.IsNullOrWhiteSpace(dto.Especialidad))
                throw new AbrilException("La especialidad es obligatoria.", 400);
            return _repo.Create(dto, userId);
        }

        public Task Update(int id, InterconsultaUpdateDto dto, int? userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Especialidad))
                throw new AbrilException("La especialidad es obligatoria.", 400);
            return _repo.Update(id, dto, userId);
        }

        public Task UpdateResultado(int id, InterconsultaResultadoPatchDto dto, int? userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Estado) || !EstadosValidos.Contains(dto.Estado))
                throw new AbrilException("El estado de la interconsulta no es válido.", 400);
            return _repo.UpdateResultado(id, dto, userId);
        }

        public Task UpdateDerivacion(int id, InterconsultaDerivacionPatchDto dto, int? userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Especialidad))
                throw new AbrilException("La especialidad es obligatoria.", 400);
            return _repo.UpdateDerivacion(id, dto, userId);
        }
    }
}
