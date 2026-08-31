using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/evaluaciones/contratistas")]
    [Authorize]
    public class EvContratistaController : ControllerBase
    {
        private readonly IEvContratistaRepository _repo;
        private readonly IEvPeriodoRepository _periodoRepo;
        private readonly IEmailService _email;
        private readonly ILogger<EvContratistaController> _logger;

        public EvContratistaController(
            IEvContratistaRepository repo,
            IEvPeriodoRepository periodoRepo,
            IEmailService email,
            ILogger<EvContratistaController> logger)
        {
            _repo = repo;
            _periodoRepo = periodoRepo;
            _email = email;
            _logger = logger;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        /// <summary>Datos iniciales para la pantalla Evaluar Contratista.</summary>
        [HttpGet("inicio")]
        public async Task<IActionResult> GetInicio()
        {
            try
            {
                return Ok(await _repo.GetInicioAsync(GetUserId()));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EvContratistaController.GetInicio");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Datos para la lista Ver Evaluaciones (con filtros opcionales).</summary>
        [HttpGet("ver")]
        public async Task<IActionResult> GetVer([FromQuery] int? periodoId, [FromQuery] int? proyectoId)
        {
            try
            {
                return Ok(await _repo.GetVerInicioAsync(periodoId, proyectoId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EvContratistaController.GetVer");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Dashboard ejecutivo.</summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int? periodoId, [FromQuery] int? proyectoId)
        {
            try
            {
                return Ok(await _repo.GetDashboardAsync(periodoId, proyectoId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EvContratistaController.GetDashboard");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Registrar evaluación de un contratista.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EvContratistaEvaluacionCreateDto dto)
        {
            try
            {
                var periodo = await _periodoRepo.GetActivoAsync()
                    ?? throw new AbrilException("No hay período de evaluación activo.", 400);

                if (string.IsNullOrWhiteSpace(dto.Comentario) == false && dto.Detalles.Count == 0)
                    throw new AbrilException("Debe calificar al menos un criterio.", 400);

                if (dto.Detalles.Any(d => !d.EsNa && (d.Puntaje is null or < 0 or > 4)))
                    throw new AbrilException("El puntaje debe estar entre 0 y 4.", 400);

                // Determinar el área del evaluador en el repositorio
                var inicio = await _repo.GetInicioAsync(GetUserId());
                var areaNombre = inicio.MiAreaNombre
                    ?? throw new AbrilException("No se pudo determinar su área evaluadora. Verifique su perfil de trabajador.", 400);

                var existe = await _repo.ExisteAsync(periodo.Id, dto.ProyectoId, dto.ContributorId, areaNombre, GetUserId());
                if (existe)
                    throw new AbrilException("Ya registró una evaluación para este contratista en este período.", 409);

                var eval = new EvEvaluacionContratista
                {
                    PeriodoId = periodo.Id,
                    ProyectoId = dto.ProyectoId,
                    ContributorId = dto.ContributorId,
                    AreaNombre = areaNombre,
                    EvaluadorUserId = GetUserId(),
                    Comentario = dto.Comentario
                };

                var detalles = dto.Detalles.Select(d => new EvEvaluacionContratistaDetalle
                {
                    PlantillaId = d.PlantillaId,
                    Criterio = d.Criterio,
                    Puntaje = d.EsNa ? null : d.Puntaje,
                    EsNa = d.EsNa
                }).ToList();

                var result = await _repo.CreateAsync(eval, detalles);
                return StatusCode(201, new { result.Id, result.Nota, message = "Evaluación registrada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EvContratistaController.Create");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Marcar que este período no corresponde evaluar contratistas (sin contratistas asignados, etc).</summary>
        [HttpPost("no-aplica")]
        public async Task<IActionResult> MarcarNoAplica([FromBody] EvContratistaNoAplicaCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Motivo))
                    throw new AbrilException("Debe indicar el motivo.", 400);

                var periodo = await _periodoRepo.GetActivoAsync()
                    ?? throw new AbrilException("No hay período de evaluación activo.", 400);

                var inicio = await _repo.GetInicioAsync(GetUserId());
                var areaNombre = inicio.MiAreaNombre
                    ?? throw new AbrilException("No se pudo determinar su área evaluadora. Verifique su perfil de trabajador.", 400);

                bool esEspecifico = dto.ProyectoId.HasValue && dto.ContributorId.HasValue;

                if (esEspecifico)
                {
                    var existeEspecifico = await _repo.ExisteAsync(
                        periodo.Id, dto.ProyectoId!.Value, dto.ContributorId!.Value, areaNombre, GetUserId());
                    if (existeEspecifico)
                        throw new AbrilException("Ya registró una evaluación para este contratista en este período.", 409);
                }
                else
                {
                    var yaMarco = await _repo.ExisteNoAplicaAsync(periodo.Id, GetUserId());
                    if (yaMarco)
                        throw new AbrilException("Ya marcó que no corresponde evaluar contratistas este período.", 409);
                }

                await _repo.RegistrarNoAplicaAsync(
                    periodo.Id, GetUserId(), areaNombre, dto.Motivo, dto.ProyectoId, dto.ContributorId);
                return StatusCode(201, new { message = "Registrado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EvContratistaController.MarcarNoAplica");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Envía por correo a cada gerente de empresa contratista (Contributor.EmailAdministrador)
        /// sus resultados del período: evaluación de la empresa por área y las notas de sus
        /// supervisores en obra. Empresas sin correo registrado se omiten y se reportan.
        /// </summary>
        [HttpPost("enviar-resultados")]
        public async Task<IActionResult> EnviarResultados([FromQuery] int periodoId)
        {
            try
            {
                var periodo = await _periodoRepo.GetByIdAsync(periodoId)
                    ?? throw new AbrilException("Período no encontrado.", 404);

                var empresas = await _repo.GetResultadosParaEnvioAsync(periodoId);
                var mesAnio = new DateTime(periodo.Anio, periodo.Mes, 1)
                    .ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-PE"));

                int enviados = 0;
                var omitidos = new List<string>();

                foreach (var empresa in empresas)
                {
                    if (string.IsNullOrWhiteSpace(empresa.EmailAdministrador))
                    {
                        omitidos.Add(empresa.ContributorNombre);
                        continue;
                    }

                    try
                    {
                        await _email.SendAsync(
                            to: [empresa.EmailAdministrador],
                            subject: $"[Evaluaciones] Resultados de {empresa.ContributorNombre} — {mesAnio}",
                            body: BuildCuerpoResultados(empresa, mesAnio),
                            isHtml: true);
                        enviados++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enviando resultados a {Email}", empresa.EmailAdministrador);
                        omitidos.Add(empresa.ContributorNombre);
                    }
                }

                return Ok(new EvContratistaEnvioResultadoDto { Enviados = enviados, OmitidosSinEmail = omitidos });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EvContratistaController.EnviarResultados");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        private static string BuildCuerpoResultados(EmpresaResultadoEnvioDto empresa, string mesAnio)
        {
            string Nota(decimal? n) => n.HasValue ? n.Value.ToString("0.0") : "—";

            var filasEvaluacion = string.Join("", empresa.Evaluaciones.Select(e => $@"
      <tr>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0'>{e.ProyectoNombre}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center'>{Nota(e.NotaOT)}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center'>{Nota(e.NotaSsoma)}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center'>{Nota(e.NotaResidencia)}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center'>{Nota(e.NotaCalidad)}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center'>{Nota(e.NotaProduccion)}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center'>{Nota(e.NotaAdministracion)}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center;font-weight:bold'>{Nota(e.NotaTotal)}</td>
      </tr>"));

            var filasSupervisores = empresa.Supervisores.Count == 0
                ? "<tr><td colspan='3' style='padding:8px;color:#64748b'>Sin supervisores evaluados este período.</td></tr>"
                : string.Join("", empresa.Supervisores.Select(s => $@"
      <tr>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0'>{s.SupervisorNombre}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0'>{s.ProyectoNombre}</td>
        <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center;font-weight:bold'>{Nota(s.Nota)}</td>
      </tr>"));

            return $@"
<div style='font-family:Arial,sans-serif;max-width:680px;margin:0 auto;padding:20px'>
  <div style='background:#1E3A5F;padding:16px 24px;border-radius:8px 8px 0 0'>
    <h2 style='color:#fff;margin:0;font-size:18px'>Resultados de evaluación — {mesAnio}</h2>
  </div>
  <div style='background:#f8fafc;padding:24px;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p>Estimado(a) representante de <strong>{empresa.ContributorNombre}</strong>,</p>
    <p>Estos son los resultados de la evaluación de su empresa correspondiente a <strong>{mesAnio}</strong>:</p>
    <table style='width:100%;border-collapse:collapse;font-size:13px;margin-top:12px'>
      <thead>
        <tr style='background:#e2e8f0'>
          <th style='padding:8px;text-align:left'>Proyecto</th>
          <th style='padding:8px'>Of. Técnica</th>
          <th style='padding:8px'>SSOMA</th>
          <th style='padding:8px'>Residencia</th>
          <th style='padding:8px'>Calidad</th>
          <th style='padding:8px'>Producción</th>
          <th style='padding:8px'>Administración</th>
          <th style='padding:8px'>Total</th>
        </tr>
      </thead>
      <tbody>{filasEvaluacion}</tbody>
    </table>

    <p style='margin-top:20px'>Notas de sus supervisores en obra:</p>
    <table style='width:100%;border-collapse:collapse;font-size:13px'>
      <thead>
        <tr style='background:#e2e8f0'>
          <th style='padding:8px;text-align:left'>Supervisor</th>
          <th style='padding:8px;text-align:left'>Proyecto</th>
          <th style='padding:8px'>Nota</th>
        </tr>
      </thead>
      <tbody>{filasSupervisores}</tbody>
    </table>

    <p style='color:#64748b;font-size:0.85rem;margin-top:20px'>
      Sistema de Evaluaciones — Abril Grupo Inmobiliario
    </p>
  </div>
</div>";
        }
    }
}
