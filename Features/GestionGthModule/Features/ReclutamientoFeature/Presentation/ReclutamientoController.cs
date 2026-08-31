using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-gth/reclutamiento")]
    [Authorize]
    public class ReclutamientoController : ControllerBase
    {
        private readonly IReclutamientoService _service;
        private readonly ILogger<ReclutamientoController> _logger;

        public ReclutamientoController(IReclutamientoService service, ILogger<ReclutamientoController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Área del solicitante + catálogos (puestos, tipos, proyectos) del formulario, en una sola petición.</summary>
        [HttpGet("form-data")]
        public async Task<IActionResult> GetFormData()
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
                return Ok(await _service.GetFormData(userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GetFormData");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Destinatarios configurados de un correo de reclutamiento por tipo (<c>aprobacion-gg</c> / <c>solicitud</c> / <c>long-list</c> / …).</summary>
        /// <remarks>Acceso dinámico por feature: los roles con <c>gestion-gth.reclutamiento.configuracion</c> en role_feature.</remarks>
        [HttpGet("config/correo-destinatarios/{tipo}")]
        [RequireFeature("gestion-gth.reclutamiento.configuracion")]
        public async Task<IActionResult> GetCorreoDestinatarios(string tipo)
        {
            try
            {
                var tipoCodigo = CorreoTipoReclutamiento.FromSlug(tipo);
                if (tipoCodigo == null)
                    return BadRequest(new { message = "Tipo de correo no válido." });

                return Ok(await _service.GetCorreoDestinatarios(tipoCodigo));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GetCorreoDestinatarios");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Guarda los destinatarios de un correo de reclutamiento por tipo (reemplaza ambas listas).</summary>
        /// <remarks>Acceso dinámico por feature: los roles con <c>gestion-gth.reclutamiento.configuracion</c> en role_feature.</remarks>
        [HttpPut("config/correo-destinatarios/{tipo}")]
        [RequireFeature("gestion-gth.reclutamiento.configuracion")]
        public async Task<IActionResult> SaveCorreoDestinatarios(string tipo, [FromBody] CorreoDestinatariosDto dto)
        {
            try
            {
                var tipoCodigo = CorreoTipoReclutamiento.FromSlug(tipo);
                if (tipoCodigo == null)
                    return BadRequest(new { message = "Tipo de correo no válido." });

                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
                await _service.SaveCorreoDestinatarios(tipoCodigo, dto, userId);
                return Ok(new { message = "Destinatarios del correo actualizados." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.SaveCorreoDestinatarios");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Panel de la vista del solicitante: tarjetas de "Gestión de candidatos" (long lists que GTH
        /// le envió para revisar) + tabla "Mis solicitudes de vacante", en una sola petición.
        /// </summary>
        [HttpGet("solicitante/panel")]
        public async Task<IActionResult> GetSolicitantePanel()
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
                return Ok(await _service.GetSolicitantePanel(userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GetSolicitantePanel");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Revisión de la long list de un requerimiento del solicitante (modal "Revisar long list y CVs"):
        /// cabecera + candidatos con sus datos y CV, en una sola petición.
        /// </summary>
        [HttpGet("requerimiento/{id:int}/long-list/revision")]
        public async Task<IActionResult> GetRevisionLongList(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                return Ok(await _service.GetRevisionLongList(id, userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GetRevisionLongList");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Registra la decisión del solicitante sobre la long list (aprobar/rechazar por candidato)
        /// y notifica a GTH por correo. Solo el solicitante dueño y solo cuando el requerimiento está
        /// en LONG_LIST_ENVIADA. Devuelve el estado resultante y los conteos.
        /// </summary>
        [HttpPost("requerimiento/{id:int}/long-list/decision")]
        public async Task<IActionResult> RegistrarDecisionLongList(int id, [FromBody] LongListDecisionDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                var result = await _service.RegistrarDecisionLongList(id, dto, userId);
                var message = result.TodosRechazados
                    ? "Decisión registrada. Rechazaste a todos los candidatos; GTH enviará una nueva long list."
                    : "Decisión registrada y notificada a GTH.";
                return Ok(new { message, result.EstadoCodigo, result.EstadoNombre, result.Aprobados, result.Rechazados, result.TodosRechazados });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.RegistrarDecisionLongList");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: bandeja de reclutamiento (tarjeta "En proceso" + tabla de solicitudes de
        /// contratación de toda la organización), en una sola petición.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpGet("bandeja")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> GetBandeja()
        {
            try
            {
                return Ok(await _service.GetBandeja());
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GetBandeja");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Vista de GTH: actualiza la prioridad (Alta/Media/Baja) de un requerimiento.</summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPatch("requerimiento/{id:int}/prioridad")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> UpdatePrioridad(int id, [FromBody] UpdatePrioridadDto dto)
        {
            try
            {
                if (dto == null || dto.PrioridadId <= 0)
                    return BadRequest(new { message = "Debe indicar una prioridad válida." });

                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                await _service.UpdatePrioridad(id, dto.PrioridadId, userId);
                return Ok(new { message = "Prioridad actualizada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.UpdatePrioridad");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: detalle de un requerimiento (modal del ojo de la bandeja) — cabecera,
        /// asignación interna, catálogos con cupos por razón social y canales de publicación,
        /// en una sola petición.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpGet("requerimiento/{id:int}/detalle-gth")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> GetDetalleGth(int id)
        {
            try
            {
                return Ok(await _service.GetDetalleGth(id));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GetDetalleGth");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: guarda la asignación interna del requerimiento (responsable del proceso,
        /// tipo de proceso y SLA, prioridad interna y razón social activa).
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPatch("requerimiento/{id:int}/asignacion-gth")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> UpdateAsignacionGth(int id, [FromBody] AsignacionGthUpdateDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                await _service.UpdateAsignacionGth(id, dto, userId);
                return Ok(new { message = "Asignación actualizada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.UpdateAsignacionGth");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: registra los canales donde se publicó la vacante (reemplaza el conjunto)
        /// y avanza el requerimiento a la fase PUBLICACION. No publica en los portales (no hay
        /// APIs integradas): solo registra y continúa el flujo. Devuelve el estado resultante.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPut("requerimiento/{id:int}/publicaciones")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> ReplacePublicaciones(int id, [FromBody] PublicacionesUpdateDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                var estado = await _service.ReplacePublicaciones(id, dto, userId);
                return Ok(new { message = "Publicación de la vacante registrada.", estado.EstadoCodigo, estado.EstadoNombre });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.ReplacePublicaciones");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: inicia la revisión de CV — avanza el requerimiento de PUBLICACION a
        /// LONG_LIST. Devuelve el estado resultante.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPatch("requerimiento/{id:int}/iniciar-revision-cv")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> IniciarRevisionCv(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                var estado = await _service.IniciarRevisionCv(id, userId);
                return Ok(new { message = "Revisión de CV iniciada.", estado.EstadoCodigo, estado.EstadoNombre });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.IniciarRevisionCv");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: marca/desmarca el check informativo del Multitest de un candidato aprobado.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPatch("candidato/{candidatoId:int}/multitest")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> SetMultitest(int candidatoId, [FromBody] MultitestUpdateDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                await _service.SetMultitest(candidatoId, dto, userId);
                return Ok(new { message = dto.Realizado ? "Multitest marcado como realizado." : "Multitest marcado como pendiente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.SetMultitest");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: continúa a la programación de entrevistas — avanza el requerimiento de
        /// LONG_LIST_APROBADA a ENTREVISTAS. Devuelve el estado resultante.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPatch("requerimiento/{id:int}/continuar-entrevistas")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> ContinuarAEntrevistas(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                var estado = await _service.ContinuarAEntrevistas(id, userId);
                return Ok(new { message = "Programación de entrevistas habilitada.", estado.EstadoCodigo, estado.EstadoNombre });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.ContinuarAEntrevistas");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: retoma el proceso con un candidato del historial de rechazados y devuelve
        /// el requerimiento a la fase en la que se lo descartó. Solo tiene sentido —y solo se
        /// acepta— con el requerimiento en EMO_NO_APTO, la fase a la que llega cuando el EMO de
        /// ingreso del seleccionado sale No Apto.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPost("requerimiento/{id:int}/retomar-candidato/{candidatoId:int}")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> RetomarCandidatoRechazado(int id, int candidatoId)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                var res = await _service.RetomarCandidatoRechazado(id, candidatoId, userId);
                return Ok(new
                {
                    message = $"Se retomó el proceso con {res.CandidatoNombre} desde la etapa «{res.EtapaNombre}». {res.SiguientePaso}",
                    res.EstadoCodigo,
                    res.EstadoNombre,
                    res.EtapaCodigo,
                    res.EtapaNombre,
                    res.CandidatoNombre,
                });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.RetomarCandidatoRechazado");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: la otra salida de EMO_NO_APTO — descartar a los rechazados y volver a
        /// LONG_LIST para preparar una long list nueva.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPost("requerimiento/{id:int}/nueva-long-list")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> VolverALongListDesdeEmoNoApto(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                var estado = await _service.VolverALongListDesdeEmoNoApto(id, userId);
                return Ok(new
                {
                    message = "El proceso volvió a Long list. Carga los CVs de la nueva long list y envíasela al área solicitante.",
                    estado.EstadoCodigo,
                    estado.EstadoNombre,
                });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.VolverALongListDesdeEmoNoApto");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: cierra el proceso y habilita el paso a onboarding. Solo se acepta con el
        /// requerimiento en EMO_APTO o EMO_APTO_RESTRICCIONES — las fases a las que llega cuando el
        /// EMO de ingreso del seleccionado sale Apto (con o sin restricciones).
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPost("requerimiento/{id:int}/cerrar-proceso")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> CerrarProcesoDesdeEmoApto(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                var estado = await _service.CerrarProcesoDesdeEmoApto(id, userId);
                return Ok(new
                {
                    message = "Proceso cerrado. El seleccionado ya aparece en Onboarding como candidato por ingresar.",
                    estado.EstadoCodigo,
                    estado.EstadoNombre,
                });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.CerrarProcesoDesdeEmoApto");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: programa (o reprograma) la entrevista de un candidato y le envía la
        /// invitación al correo que declaró en su formulario del postulante.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPost("candidato/{candidatoId:int}/entrevista")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> GuardarEntrevista(int candidatoId, [FromBody] EntrevistaGuardarDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                return Ok(await _service.GuardarEntrevista(candidatoId, dto, userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GuardarEntrevista");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: guarda la evaluación de la entrevista de un candidato (los tres
        /// comentarios del informe que verá el área solicitante). Multipart: <c>data</c> = JSON con
        /// los comentarios y los archivos que se quitaron; los archivos nuevos viajan como form
        /// files con las claves <c>informeFinal</c> y <c>evaluacionConocimientos</c>, los dos
        /// opcionales.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPut("candidato/{candidatoId:int}/evaluacion")]
        [RequireFeature("gestion-gth.reclutamiento")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)] // los dos archivos del informe + margen
        public async Task<IActionResult> GuardarEvaluacion(int candidatoId, [FromForm] string data)
        {
            try
            {
                EvaluacionGuardarDto? dto;
                try
                {
                    dto = JsonSerializer.Deserialize<EvaluacionGuardarDto>(
                        data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "El campo 'data' no es un JSON válido." });
                }

                // Cada archivo se enlaza con su tipo del catálogo por la clave del form file: el
                // cliente no manda el código, así que no puede inventarse un tipo inexistente.
                var archivos = new List<EvaluacionArchivoSubidaDto>();
                foreach (var (clave, tipoCodigo) in EvaluacionArchivoCodigo.PorClaveDeFormulario)
                {
                    var archivo = Request.Form.Files[clave];
                    if (archivo == null || archivo.Length == 0) continue;

                    archivos.Add(new EvaluacionArchivoSubidaDto
                    {
                        TipoCodigo  = tipoCodigo,
                        FileName    = archivo.FileName,
                        ContentType = archivo.ContentType ?? "application/octet-stream",
                        Content     = await ToBytesAsync(archivo),
                    });
                }

                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                return Ok(await _service.GuardarEvaluacion(candidatoId, dto!, archivos, userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GuardarEvaluacion");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: envía al candidato el correo de agradecimiento por no continuar en el
        /// proceso y deja su resultado en NO_PASO.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPost("candidato/{candidatoId:int}/agradecimiento")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> EnviarAgradecimiento(int candidatoId)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                return Ok(await _service.EnviarAgradecimiento(candidatoId, userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.EnviarAgradecimiento");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: saca del proceso al postulante cuyo formulario quedó rechazado y le envía
        /// el correo de fin de proceso, dejando su resultado en NO_PASO.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPost("candidato/{candidatoId:int}/rechazo-postulante")]
        [RequireFeature("gestion-gth.reclutamiento")]
        public async Task<IActionResult> RechazarPostulante(int candidatoId)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                return Ok(await _service.RechazarPostulante(candidatoId, userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.RechazarPostulante");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Informe de finalistas de un requerimiento del solicitante (modal "Finalistas enviados por
        /// GTH"): cabecera + finalistas con sus comentarios y CV, en una sola petición.
        /// </summary>
        [HttpGet("requerimiento/{id:int}/finalistas/revision")]
        public async Task<IActionResult> GetRevisionFinalistas(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                return Ok(await _service.GetRevisionFinalistas(id, userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GetRevisionFinalistas");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista del solicitante: registra su decisión final sobre un finalista. Aprobar cierra el
        /// proceso de reclutamiento (el seleccionado pasa a onboarding); rechazar le envía el correo
        /// de agradecimiento y, si ya no queda ningún finalista, devuelve el requerimiento a
        /// LONG_LIST para que GTH envíe una nueva long list. En ambos casos se notifica a GTH.
        /// </summary>
        [HttpPost("requerimiento/{id:int}/finalistas/decision")]
        public async Task<IActionResult> RegistrarDecisionFinalista(int id, [FromBody] FinalistaDecisionDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                return Ok(await _service.RegistrarDecisionFinalista(id, dto, userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.RegistrarDecisionFinalista");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Vista de GTH: envía la long list al solicitante. Multipart: <c>data</c> = JSON con los
        /// candidatos (nombre, comentario, la clave de su CV y las de sus anexos); los archivos
        /// viajan como form files con esas claves (ej. <c>cv_0</c>, <c>anexo_0_0</c>). Envía el
        /// correo configurado (tipo LONG_LIST) con los adjuntos y avanza el requerimiento a
        /// LONG_LIST_ENVIADA.
        /// </summary>
        /// <remarks>Acceso por feature: los roles con <c>gestion-gth.reclutamiento</c> en role_feature.</remarks>
        [HttpPost("requerimiento/{id:int}/long-list/enviar")]
        [RequireFeature("gestion-gth.reclutamiento")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)] // 20 MB de adjuntos + margen
        public async Task<IActionResult> EnviarLongList(int id, [FromForm] string data)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;

                LongListEnviarMetaDto? meta;
                try
                {
                    meta = JsonSerializer.Deserialize<LongListEnviarMetaDto>(
                        data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "El campo 'data' no es un JSON válido." });
                }
                if (meta?.Candidatos == null || meta.Candidatos.Count == 0)
                    return BadRequest(new { message = "Debes cargar al menos un candidato para enviar la long list." });

                // Enlaza cada candidato con su CV y sus anexos del multipart (por la clave del
                // form file). El CV es obligatorio; los anexos son opcionales y pueden ser varios.
                var candidatos = new List<LongListCandidatoArchivoDto>(meta.Candidatos.Count);
                foreach (var m in meta.Candidatos)
                {
                    var cv = string.IsNullOrWhiteSpace(m.CvKey) ? null : Request.Form.Files[m.CvKey];
                    if (cv == null || cv.Length == 0)
                        return BadRequest(new { message = "Cada candidato debe tener su CV adjunto." });

                    var anexos = new List<LongListArchivoDto>(m.AnexoKeys?.Count ?? 0);
                    foreach (var key in m.AnexoKeys ?? new List<string>())
                    {
                        var archivo = string.IsNullOrWhiteSpace(key) ? null : Request.Form.Files[key];
                        if (archivo == null || archivo.Length == 0)
                            return BadRequest(new { message = "Un anexo del portafolio llegó vacío. Vuelve a cargarlo." });

                        anexos.Add(new LongListArchivoDto
                        {
                            FileName    = archivo.FileName,
                            ContentType = archivo.ContentType ?? "application/octet-stream",
                            Content     = await ToBytesAsync(archivo),
                        });
                    }

                    candidatos.Add(new LongListCandidatoArchivoDto
                    {
                        Nombre        = m.Nombre,
                        Comentario    = m.Comentario,
                        CvFileName    = cv.FileName,
                        CvContentType = cv.ContentType ?? "application/octet-stream",
                        CvContent     = await ToBytesAsync(cv),
                        Anexos        = anexos,
                    });
                }

                var estado = await _service.EnviarLongList(id, candidatos, userId);
                return Ok(new { message = "Long list enviada y correo notificado al solicitante.", estado.EstadoCodigo, estado.EstadoNombre });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.EnviarLongList");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        private static async Task<byte[]> ToBytesAsync(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return ms.ToArray();
        }

        /// <summary>Detalle de seguimiento de un requerimiento (cabecera + fases del pipeline), en una sola petición.</summary>
        [HttpGet("requerimiento/{id:int}/seguimiento")]
        public async Task<IActionResult> GetSeguimiento(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
                return Ok(await _service.GetSeguimiento(id, userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.GetSeguimiento");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Crea la solicitud de personal. Multipart: <c>data</c> = JSON de
        /// <see cref="SolicitudPersonalCreateDto"/>; <c>sustento</c> = adjunto opcional (PDF/DOC/DOCX/XLS/XLSX).
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(15 * 1024 * 1024)] // 10 MB de sustento + margen
        public async Task<IActionResult> Create([FromForm] string data, [FromForm] IFormFile? sustento)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;

                SolicitudPersonalCreateDto? dto;
                try
                {
                    dto = JsonSerializer.Deserialize<SolicitudPersonalCreateDto>(
                        data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "El campo 'data' no es un JSON válido." });
                }
                if (dto == null)
                    return BadRequest(new { message = "Datos de la solicitud no recibidos." });

                var result = await _service.Create(dto, userId, sustento);
                var codigos = result.Codigos.Count == 1
                    ? $"Requerimiento generado: {result.Codigos[0]}."
                    : $"Se generaron {result.Codigos.Count} requerimientos: {string.Join(", ", result.Codigos)}.";
                // Qué sigue depende de lo que traiga la solicitud, así que el mensaje lo dice
                // explícitamente (y avisa si algún correo no pudo salir, porque entonces hay que
                // reenviarlo). Un ingreso directo no tiene paso de aprobación: pasa derecho a GTH,
                // y lo que sale es el aviso del candidato. Una solicitud que mezcle los dos hace
                // las dos cosas a la vez.
                var msg = result.AprobacionGgOmitida
                    ? result.CorreoGerenciaEnviado
                        ? $"Ingreso directo FFT registrado y enviado a Gestión de Talento Humano. {codigos}"
                        : $"Ingreso directo FFT registrado. {codigos} No se pudo enviar el aviso a Gestión de " +
                          "Talento Humano, pero el requerimiento ya figura en su bandeja."
                    : result.HayIngresoDirecto
                        ? result.CorreoGerenciaEnviado
                            ? $"Solicitud registrada. {codigos} El ingreso directo pasó a Gestión de Talento " +
                              "Humano y el resto se envió para su aprobación."
                            : $"Solicitud registrada. {codigos} No se pudo enviar alguno de los correos: usa " +
                              "«Reenviar aprobación» en la tabla para reintentar el de aprobación."
                        : result.CorreoGerenciaEnviado
                            ? $"Solicitud registrada y enviada a Gerencia General para su aprobación. {codigos}"
                            : $"Solicitud registrada. {codigos} No se pudo enviar el correo a Gerencia General: " +
                              "usa «Reenviar a Gerencia General» en la tabla para reintentarlo.";
                return Ok(new
                {
                    id = result.SolicitudId,
                    codigos = result.Codigos,
                    correoGerenciaEnviado = result.CorreoGerenciaEnviado,
                    aprobacionGgOmitida = result.AprobacionGgOmitida,
                    hayIngresoDirecto = result.HayIngresoDirecto,
                    message = msg,
                });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutamientoController.Create");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
