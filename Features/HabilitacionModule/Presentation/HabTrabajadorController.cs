using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/trabajadores")]
    [Authorize]
    public class HabTrabajadorController : ControllerBase
    {
        private static readonly string[] RolesAprobadoresSsoma = [Roles.AdministradorSsoma, Roles.AdministradorUdp];
        private static readonly string[] RolesAprobadoresAdmin = [Roles.AdministradorAdministracion, Roles.AdministradorUdp];
        // El área de Calidad no tiene rol propio: aprueba con USUARIO DE ABRIL, el rol
        // genérico que ya tienen asignado (ver "Entrevista con el Área de Calidad" en
        // CategoriaIds.cs). Ojo: ese rol lo usan también otras áreas sin permisos
        // especiales, así que cualquiera que lo tenga puede aprobar/rechazar estos
        // entregables de Calidad, no solo el área de Calidad.
        private static readonly string[] RolesAprobadoresCalidad = [Roles.UsuarioDeAbril, Roles.AdministradorUdp];

        private readonly IHabTrabajadorRepository _repo;
        private readonly ILogger<HabTrabajadorController> _logger;

        public HabTrabajadorController(IHabTrabajadorRepository repo, ILogger<HabTrabajadorController> logger)
        {
            _repo = repo;
            _logger = logger;
        }


        /// <summary>Widget "Interconsultas pendientes" junto al botón "EMOs Programados": solo
        /// trabajador, razón social, proyecto actual y días de retraso — sin datos clínicos.</summary>
        [HttpGet("interconsultas-pendientes")]
        public async Task<IActionResult> GetInterconsultasPendientes()
        {
            try
            {
                return Ok(await _repo.GetInterconsultasPendientesAsync());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en HabTrabajadorController.GetInterconsultasPendientes");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkers(
            [FromQuery] string? search,
            [FromQuery] int? empresaId,
            [FromQuery] int? proyectoId,
            [FromQuery] string? estadoHabilitacion,
            [FromQuery] string? contratistaCasa,
            [FromQuery] int? areaScopeId,
            [FromQuery] bool soloRetirados = false,
            [FromQuery] bool soloVerificacion = false,
            [FromQuery] bool soloSinEmo = false,
            [FromQuery] bool soloEmoVencido = false,
            [FromQuery] bool soloSinVidaLey = false,
            [FromQuery] bool soloSinLectura = false,
            [FromQuery] bool soloSinCertificado = false,
            [FromQuery] bool soloSinInterconsulta = false,
            [FromQuery] bool soloSinEmoCompleto = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";
                if (esContratista)
                {
                    if (!int.TryParse(User.FindFirst("empresaId")?.Value, out var empresaJwt))
                        return StatusCode(403, new { message = "Token de contratista inválido." });

                    var proyectosFiltro = ContratistaProyectosHelper.GetProyectosFiltro(User);
                    if (proyectosFiltro != null)
                    {
                        // scope POR_PROYECTO: puede buscar entre empresas dentro de SUS proyectos
                        // asignados (necesario para RAC/OPT/Inspecciones contra otra empresa en su obra),
                        // pero nunca fuera de esos proyectos — no se salta el filtro aunque pida
                        // soloVerificacion=true.
                        if (proyectoId == null)
                        {
                            if (proyectosFiltro.Count == 1)
                                proyectoId = proyectosFiltro[0];
                            else if (proyectosFiltro.Count == 0)
                                return Ok(new PagedResult<object> { Data = new() });
                            // Múltiples proyectos: no hay un único proyectoId para acotar aquí — se
                            // implementa después. Al menos queda acotado por su propia empresa abajo.
                        }
                        if (!soloVerificacion) empresaId = empresaJwt;
                    }
                    else
                    {
                        // scope TODOS: mismo criterio que POR_PROYECTO — un contratista puede reportar
                        // RAC/OPT/Inspecciones contra el trabajador de OTRA empresa en su propio
                        // proyecto/obra, así que soloVerificacion=true tampoco se acota por empresa acá.
                        // Fuera de ese caso (listados de habilitación propios, gestión de sus propios
                        // trabajadores) sí se restringe siempre a su propia empresa.
                        if (!soloVerificacion) empresaId = empresaJwt;
                    }
                }

                // pageSize sin tope permitía pedir 9999 filas de golpe. El cap defensivo solo aplica
                // a listados de habilitación "normales" (soloVerificacion=false) de un contratista sin
                // acotar por empresa/proyecto — ahí sí escanearía toda la tabla del sistema. Con
                // soloVerificacion=true (buscador de Observador/Trabajador en RAC/OPT/Inspección/
                // Auditoría ATS/Accidentes) el contratista necesita la lista completa igual que el
                // personal interno de Abril, incluso cross-empresa: si se capea acá, la lista queda
                // truncada alfabéticamente (ej. corta en la letra B) y faltan nombres reales.
                if (esContratista && !soloVerificacion && empresaId == null && proyectoId == null && pageSize > 200)
                    pageSize = 200;

                var (items, total) = await _repo.GetWorkersHabilitacionAsync(
                    search, empresaId, proyectoId, estadoHabilitacion, contratistaCasa, page, pageSize, soloRetirados, soloSinEmo, soloEmoVencido, soloSinVidaLey,
                    areaScopeId, soloSinLectura, soloSinCertificado, soloSinInterconsulta, soloSinEmoCompleto);

                var result = new PagedResult<WorkerHabilitacionListDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Data = items
                };

                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.GetWorkers"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var worker = await _repo.GetByIdAsync(id);
                if (worker is null)
                    return NotFound(new { message = "Trabajador no encontrado." });
                return Ok(worker);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.GetById"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] WorkerUpdateDto dto)
        {
            try
            {
                var actualizado = await _repo.UpdateAsync(id, dto);
                return Ok(actualizado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.Update"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{workerId:int}/entregables")]
        public async Task<IActionResult> GetEntregables(int workerId)
        {
            try
            {
                if (User.FindFirst("tipo")?.Value == "CONTRATISTA")
                {
                    var empresaClaim = User.FindFirst("empresaId")?.Value;
                    if (!int.TryParse(empresaClaim, out var empresaJwt))
                        return StatusCode(403, new { message = "Token de contratista inválido." });

                    var empresaActiva = await _repo.GetEmpresaActivaWorkerAsync(workerId);
                    if (empresaActiva is null || empresaActiva.Value != empresaJwt)
                        return StatusCode(403, new { message = "Este trabajador no pertenece a su empresa." });
                }

                return Ok(await _repo.GetEntregablesWorkerAsync(workerId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.GetEntregables"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("entregables/{id:int}")]
        public async Task<IActionResult> UpdateEntregable(int id, [FromBody] WorkerEntregableUpdateDto dto)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";
                if (esContratista)
                {
                    var itemId = await _repo.GetEntregableItemIdAsync(id);
                    if (itemId == HabItemIds.InduccionObra)
                        return BadRequest(new { message = "La Inducción de Obra no puede ser enviada por el contratista. Debe ser aprobada presencialmente." });

                    // Solo forzar "Enviado" cuando el contratista realmente está subiendo/reemplazando
                    // un archivo. Un PUT que solo actualiza observaciones (ej. al cerrar el panel de
                    // detalle sin subir nada) no debe resubir un documento ya aprobado.
                    if (!string.IsNullOrWhiteSpace(dto.ArchivoUrl))
                        dto.Estado = "Enviado";
                    // No borrar vigencia — el contratista puede enviarla cuando el item la requiere
                }

                var esAccionRestringida =
                    string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dto.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase) ||
                    dto.Vigencia.HasValue;

                if (!esContratista && esAccionRestringida)
                {
                    var responsable = await _repo.GetResponsableItemTrabajadorAsync(id);
                    var rolesPermitidos = string.Equals(responsable, "SSOMA", StringComparison.OrdinalIgnoreCase)
                        ? RolesAprobadoresSsoma
                        : string.Equals(responsable, "CALIDAD", StringComparison.OrdinalIgnoreCase)
                            ? RolesAprobadoresCalidad
                            : RolesAprobadoresAdmin;
                    var tienePermiso = User.FindAll(ClaimTypes.Role)
                        .Any(c => rolesPermitidos.Contains(c.Value, StringComparer.OrdinalIgnoreCase));
                    if (!tienePermiso)
                        return StatusCode(403, new { message = "No tienes permiso para modificar este documento." });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int? userId = userIdClaim != null && int.TryParse(userIdClaim.Value, out var uid) ? uid : null;

                int? empresaIdClaim = null;
                if (esContratista)
                {
                    var ec = User.FindFirst("empresaId")?.Value;
                    if (int.TryParse(ec, out var eid)) empresaIdClaim = eid;
                    userId = null;
                }

                var actualizado = await _repo.UpdateEntregableAsync(id, dto, userId, empresaIdClaim);
                return Ok(actualizado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.UpdateEntregable"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("entregables/{id:int}/versiones")]
        public async Task<IActionResult> GetVersionesDocumento(int id)
        {
            try
            {
                var versiones = await _repo.GetVersionesDocumentoAsync(id);
                return Ok(versiones);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.GetVersionesDocumento"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{workerId:int}/cambiar-obra")]
        public async Task<IActionResult> CambiarObra(int workerId, [FromBody] WorkerCambiarObraDto dto)
        {
            try
            {
                await _repo.CambiarObraAsync(workerId, dto);
                return Ok(new { message = "Cambio de obra registrado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.CambiarObra"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{workerId:int}/inicializar")]
        public async Task<IActionResult> InicializarEntregables(int workerId)
        {
            try
            {
                await _repo.InicializarEntregablesAsync(workerId);
                return Ok(new { message = "Entregables inicializados correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.InicializarEntregables"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{workerId:int}/reingreso")]
        public async Task<IActionResult> Reingreso(int workerId, [FromBody] WorkerReingresoDto dto)
        {
            try
            {
                await _repo.ReingresoAsync(workerId, dto);
                return Ok(new { message = "Trabajador reingresado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.Reingreso"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("baja-masiva")]
        public async Task<IActionResult> BajaMasiva([FromBody] WorkerBajaMasivaDto dto)
        {
            try
            {
                if (dto.Ids == null || dto.Ids.Count == 0)
                    return BadRequest(new { message = "Debe proporcionar al menos un ID de trabajador." });

                var fechaRetiro = dto.FechaRetiro ?? DateOnly.FromDateTime(DateTime.UtcNow);
                await _repo.BajaMasivaAsync(dto.Ids, fechaRetiro);
                return Ok(new { message = $"{dto.Ids.Count} trabajador(es) dado(s) de baja correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.BajaMasiva"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{workerId:int}/baja")]
        public async Task<IActionResult> Baja(int workerId, [FromBody] WorkerBajaDto dto)
        {
            try
            {
                var fechaRetiro = dto.FechaRetiro ?? DateOnly.FromDateTime(DateTime.UtcNow);
                await _repo.BajaAsync(workerId, fechaRetiro);
                return Ok(new { message = "Trabajador dado de baja correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.Baja"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{workerId:int}/eventos")]
        public async Task<IActionResult> GetEventos(int workerId)
        {
            try
            {
                var eventos = await _repo.GetEventosAsync(workerId);
                return Ok(eventos);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.GetEventos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{workerId:int}/proyectos")]
        public async Task<IActionResult> AgregarProyecto(int workerId, [FromBody] AgregarProyectoDto dto)
        {
            try
            {
                var creado = await _repo.AgregarProyectoAsync(workerId, dto);
                return Ok(creado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.AgregarProyecto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{workerId:int}/proyectos")]
        public async Task<IActionResult> GetProyectos(int workerId)
        {
            try
            {
                var proyectos = await _repo.GetProyectosAsync(workerId);
                return Ok(proyectos);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.GetProyectos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("{workerId:int}/proyectos/{proyectoId:int}")]
        public async Task<IActionResult> RetirarDeProyecto(int workerId, int proyectoId)
        {
            try
            {
                await _repo.RetirarDeProyectoAsync(workerId, proyectoId);
                return Ok(new { message = "Trabajador retirado del proyecto correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.RetirarDeProyecto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{workerId:int}/proyectos/{proyectoId:int}/induccion")]
        public async Task<IActionResult> MarcarInduccion(int workerId, int proyectoId)
        {
            try
            {
                await _repo.MarcarInduccionAsync(workerId, proyectoId);
                return Ok(new { message = "Inducción marcada como completada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.MarcarInduccion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("reparar-vinculaciones")]
        public async Task<IActionResult> RepararVinculaciones()
        {
            if (!User.Claims.Any(c => c.Type == ClaimTypes.Role && (RolesAprobadoresSsoma.Contains(c.Value) || RolesAprobadoresAdmin.Contains(c.Value))))
                return StatusCode(403, new { message = "Solo administradores pueden ejecutar esta operación." });
            try
            {
                var reparados = await _repo.RepararVinculacionesAsync();
                return Ok(new
                {
                    total = reparados.Count,
                    message = reparados.Count == 0
                        ? "No se encontraron workers con vinculaciones inconsistentes."
                        : $"{reparados.Count} worker(s) reparados.",
                    workers = reparados,
                });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabTrabajadorController.RepararVinculaciones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
