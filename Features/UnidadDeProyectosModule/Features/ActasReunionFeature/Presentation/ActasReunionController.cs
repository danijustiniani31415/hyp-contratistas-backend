using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Presentation
{
    [Authorize]
    [ApiController]
    [Route("api/v1/actas-reunion")]
    public class ActasReunionController : ControllerBase
    {
        private readonly IActasReunionService _service;
        private readonly ILogger<ActasReunionController> _logger;
        private readonly IConfiguration _configuration;

        public ActasReunionController(IActasReunionService service, ILogger<ActasReunionController> logger, IConfiguration configuration)
        {
            _service = service;
            _logger = logger;
            _configuration = configuration;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        /// <summary>Carga inicial de la página: filtros (proyectos, estados) + primera página de reuniones.</summary>
        [HttpGet("pagina-inicial")]
        public async Task<IActionResult> GetPaginaInicial([FromQuery] ReunionFiltroRequest filtro)
        {
            try
            {
                return Ok(await _service.GetPaginaInicial(filtro, GetUserId()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION PAGINA INICIAL: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Listado filtrado/paginado de reuniones (sin volver a traer los filtros).</summary>
        [HttpGet]
        public async Task<IActionResult> GetReuniones([FromQuery] ReunionFiltroRequest filtro)
        {
            try
            {
                return Ok(await _service.GetReuniones(filtro, GetUserId()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION LISTADO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Trabajadores que calzan con un área/gerencia (incluye descendencia), un puesto y/o el
        /// staff asignado a un proyecto, para convocatoria masiva de participantes (ej. "todas las
        /// jefaturas de Proyectos", "todo el staff de esta obra").
        /// </summary>
        [HttpGet("trabajadores-por-filtro")]
        public async Task<IActionResult> BuscarTrabajadoresPorFiltro([FromQuery] int? areaScopeId, [FromQuery] List<int>? puestoIds, [FromQuery] int? projectId)
        {
            try
            {
                return Ok(await _service.BuscarTrabajadoresPorFiltro(areaScopeId, puestoIds, projectId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION TRABAJADORES POR FILTRO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Catálogo de puestos, para el filtro de convocatoria masiva.</summary>
        [HttpGet("puestos")]
        public async Task<IActionResult> GetPuestos()
        {
            try
            {
                return Ok(await _service.GetPuestos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION PUESTOS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Puestos que realmente existen dentro de un área/gerencia (con descendencia); sin areaScopeId trae todos.</summary>
        [HttpGet("puestos-por-area")]
        public async Task<IActionResult> GetPuestosPorArea([FromQuery] int? areaScopeId)
        {
            try
            {
                return Ok(await _service.GetPuestosPorArea(areaScopeId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION PUESTOS POR AREA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Da de alta un tema personalizado en el catálogo, para reutilizarlo como tema recurrente.</summary>
        [HttpPost("temas")]
        public async Task<IActionResult> AgregarTema([FromBody] TemaCreateRequest request)
        {
            try
            {
                return Ok(await _service.AgregarTema(request.Descripcion, GetUserId()));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION AGREGAR TEMA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Catálogo de temas predefinidos, para la pantalla de configuración de convocatoria por tema.</summary>
        [HttpGet("temas")]
        public async Task<IActionResult> GetTemasCatalogo()
        {
            try
            {
                return Ok(await _service.GetTemasCatalogo());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION GET TEMAS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Elimina un tema del catálogo (borrado real, no soft-delete). Las reuniones que ya lo usaban
        /// conservan su tema (texto propio) y solo pierden el vínculo al catálogo.</summary>
        [HttpDelete("temas/{reunionTemaId:int}")]
        public async Task<IActionResult> EliminarTema(int reunionTemaId)
        {
            try
            {
                var reunionesDesvinculadas = await _service.EliminarTema(reunionTemaId);
                return Ok(new { message = "Tema eliminado.", reunionesDesvinculadas });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION ELIMINAR TEMA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Convocatoria recurrente configurada para un tema (área + puestos habituales).</summary>
        [HttpGet("temas/{reunionTemaId:int}/convocatoria")]
        public async Task<IActionResult> GetConvocatoriaTema(int reunionTemaId)
        {
            try
            {
                return Ok(await _service.GetConvocatoriaTema(reunionTemaId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION GET CONVOCATORIA TEMA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Configura (reemplaza) la convocatoria recurrente de un tema.</summary>
        [HttpPut("temas/{reunionTemaId:int}/convocatoria")]
        public async Task<IActionResult> GuardarConvocatoriaTema(int reunionTemaId, [FromBody] TemaConvocatoriaSaveRequest request)
        {
            try
            {
                await _service.GuardarConvocatoriaTema(reunionTemaId, request, GetUserId());
                return Ok(new { message = "Convocatoria del tema guardada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION GUARDAR CONVOCATORIA TEMA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Configuración de recurrencia de un tema (generación automática de la siguiente reunión).</summary>
        [HttpGet("temas/{reunionTemaId:int}/recurrencia")]
        public async Task<IActionResult> GetRecurrenciaTema(int reunionTemaId)
        {
            try
            {
                return Ok(await _service.GetRecurrenciaTema(reunionTemaId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION GET RECURRENCIA TEMA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("temas/{reunionTemaId:int}/recurrencia")]
        public async Task<IActionResult> GuardarRecurrenciaTema(int reunionTemaId, [FromBody] TemaRecurrenciaSaveRequest request)
        {
            try
            {
                await _service.GuardarRecurrenciaTema(reunionTemaId, request, GetUserId());
                return Ok(new { message = "Recurrencia del tema guardada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION GUARDAR RECURRENCIA TEMA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Job de generación automática (disparado por un cron externo, no por usuarios): genera
        /// las siguientes ocurrencias de cada convocatoria recurrente cuya fecha teórica ya entró
        /// en su ventana de anticipación. El intervalo se calcula siempre desde la fecha ancla de
        /// la serie, nunca desde la fecha real de la última reunión (que puede haberse reprogramado
        /// o cancelado) — así una reprogramación no arrastra a toda la cadena.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("recurrencia/procesar")]
        public async Task<IActionResult> ProcesarGeneracionRecurrente()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                return Ok(await _service.ProcesarGeneracionRecurrente());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION PROCESAR GENERACION RECURRENTE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Job de recordatorio de agenda (disparado por un cron externo, no por usuarios): revisa
        /// las reuniones con agenda dinámica cuya hora de aviso ya llegó y envía correo +
        /// notificación in-app con el link directo para cargar los temas.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("recordatorios/procesar-agenda")]
        public async Task<IActionResult> ProcesarRecordatoriosAgenda()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                return Ok(await _service.ProcesarRecordatoriosAgenda());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION PROCESAR RECORDATORIOS AGENDA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Agenda de reunión ────────────────────────────────────────────────

        /// <summary>
        /// Agenda de una reunión concreta: fija (texto único) o dinámica (temas cargados por cada
        /// participante). Pensada para el link directo del recordatorio ("cargar mis temas").
        /// </summary>
        [HttpGet("{reunionId:int}/agenda")]
        public async Task<IActionResult> GetAgenda(int reunionId)
        {
            try
            {
                return Ok(await _service.GetAgenda(reunionId, GetUserId()));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION GET AGENDA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Reemplaza los temas a tratar del usuario autenticado para esta reunión (agenda dinámica).</summary>
        [HttpPut("{reunionId:int}/agenda/mis-temas")]
        public async Task<IActionResult> GuardarMisTemas(int reunionId, [FromBody] GuardarMisTemasRequest request)
        {
            try
            {
                await _service.GuardarMisTemas(reunionId, GetUserId(), request);
                return Ok(new { message = "Tus temas se guardaron exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION GUARDAR MIS TEMAS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Agrega un tema puntual a la agenda de esta ocurrencia — para reuniones de agenda
        /// fija que necesitan sumar un punto excepcional sin activar el flujo de agenda dinámica.</summary>
        [HttpPost("{reunionId:int}/agenda/temas-puntuales")]
        public async Task<IActionResult> AgregarTemaPuntual(int reunionId, [FromBody] ReunionAgendaItemInput request)
        {
            try
            {
                return Ok(await _service.AgregarTemaPuntual(reunionId, GetUserId(), request.Descripcion));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION AGREGAR TEMA PUNTUAL: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Elimina un tema puntual — solo quien lo agregó puede quitarlo.</summary>
        [HttpDelete("{reunionId:int}/agenda/temas-puntuales/{reunionAgendaItemId:int}")]
        public async Task<IActionResult> EliminarTemaPuntual(int reunionId, int reunionAgendaItemId)
        {
            try
            {
                await _service.EliminarTemaPuntual(reunionId, reunionAgendaItemId, GetUserId());
                return Ok(new { message = "Tema eliminado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION ELIMINAR TEMA PUNTUAL: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Dashboard personal: todos los acuerdos (de cualquier reunión) de los que el
        /// usuario autenticado es responsable, para el tab "Dashboard" de Actas de Reunión.</summary>
        [HttpGet("mis-acuerdos")]
        public async Task<IActionResult> GetMisAcuerdos()
        {
            try
            {
                return Ok(await _service.GetMisAcuerdos(GetUserId()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION MIS ACUERDOS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Listado filtrado/paginado de acuerdos de reuniones que el usuario organizó o a
        /// las que fue convocado (mismo alcance que GetReuniones), para la vista "Acuerdos".</summary>
        [HttpGet("acuerdos")]
        public async Task<IActionResult> GetAcuerdos([FromQuery] AcuerdoBusquedaFiltroRequest filtro)
        {
            try
            {
                return Ok(await _service.GetAcuerdos(filtro, GetUserId()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION LISTADO DE ACUERDOS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Acuerdos pendientes (no cumplidos/anulados) de ediciones anteriores de la misma
        /// convocatoria recurrente, para revisarlos al abrir esta reunión.</summary>
        [HttpGet("{reunionId:int}/acuerdos-pendientes-anteriores")]
        public async Task<IActionResult> GetAcuerdosPendientesAnteriores(int reunionId)
        {
            try
            {
                return Ok(await _service.GetAcuerdosPendientesAnteriores(reunionId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION PENDIENTES ANTERIORES: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Reprograma la fecha programada de un acuerdo, dejando registro de motivo y del
        /// contador de veces reprogramado (para detectar acuerdos que se siguen postergando).</summary>
        [HttpPatch("acuerdos/{reunionAcuerdoId:int}/reprogramar")]
        public async Task<IActionResult> ReprogramarAcuerdo(int reunionAcuerdoId, [FromBody] AcuerdoReprogramarRequest request)
        {
            try
            {
                await _service.ReprogramarAcuerdo(reunionAcuerdoId, request, GetUserId());
                return Ok(new { message = "Acuerdo reprogramado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION REPROGRAMAR ACUERDO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Marca un acuerdo como cumplido (usado desde la revisión de pendientes de
        /// ediciones anteriores).</summary>
        [HttpPatch("acuerdos/{reunionAcuerdoId:int}/marcar-cumplido")]
        public async Task<IActionResult> MarcarAcuerdoCumplido(int reunionAcuerdoId, [FromBody] AcuerdoMarcarCumplidoRequest request)
        {
            try
            {
                await _service.MarcarAcuerdoCumplido(reunionAcuerdoId, request, GetUserId());
                return Ok(new { message = "Acuerdo marcado como cumplido." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION MARCAR ACUERDO CUMPLIDO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Detalle completo del acta: cabecera, participantes, acuerdos, archivos y reprogramaciones.</summary>
        [HttpGet("{reunionId:int}")]
        public async Task<IActionResult> GetDetalle(int reunionId)
        {
            try
            {
                return Ok(await _service.GetDetalle(reunionId, GetUserId()));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION DETALLE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Agenda una nueva reunión (estado PROGRAMADA) con sus participantes.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReunionCreateRequest request)
        {
            try
            {
                var reunionId = await _service.Create(request, GetUserId());
                return Ok(new { reunionId, message = "Reunión agendada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION CREATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Actualiza los datos generales del acta y su lista de participantes.</summary>
        [HttpPut("{reunionId:int}")]
        public async Task<IActionResult> Update(int reunionId, [FromBody] ReunionUpdateRequest request)
        {
            try
            {
                await _service.Update(reunionId, request, GetUserId());
                return Ok(new { message = "Acta actualizada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION UPDATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Cambia la fecha de la reunión dejando rastro en el historial de reprogramaciones.</summary>
        [HttpPatch("{reunionId:int}/reprogramar")]
        public async Task<IActionResult> Reprogramar(int reunionId, [FromBody] ReunionReprogramarRequest request)
        {
            try
            {
                await _service.Reprogramar(reunionId, request, GetUserId());
                return Ok(new { message = "Reunión reprogramada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION REPROGRAMAR: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Cambia el estado de la reunión (PROGRAMADA, REALIZADA o CANCELADA).</summary>
        [HttpPatch("{reunionId:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int reunionId, [FromBody] ReunionCambiarEstadoRequest request)
        {
            try
            {
                await _service.CambiarEstado(reunionId, request, GetUserId());
                return Ok(new { message = "Estado actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION ESTADO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Elimina (soft delete) el acta de reunión.</summary>
        [HttpDelete("{reunionId:int}")]
        public async Task<IActionResult> Eliminar(int reunionId)
        {
            try
            {
                await _service.Eliminar(reunionId, GetUserId());
                return Ok(new { message = "Acta eliminada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION ELIMINAR: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Acuerdos ─────────────────────────────────────────────────────────

        /// <summary>Registra un acuerdo del acta con sus responsables.</summary>
        [HttpPost("{reunionId:int}/acuerdos")]
        public async Task<IActionResult> CrearAcuerdo(int reunionId, [FromBody] ReunionAcuerdoRequest request)
        {
            try
            {
                var acuerdoId = await _service.CrearAcuerdo(reunionId, request, GetUserId());
                return Ok(new { reunionAcuerdoId = acuerdoId, message = "Acuerdo registrado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION CREAR ACUERDO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Actualiza un acuerdo (descripción, fechas, estado, responsables).</summary>
        [HttpPut("acuerdos/{reunionAcuerdoId:int}")]
        public async Task<IActionResult> ActualizarAcuerdo(int reunionAcuerdoId, [FromBody] ReunionAcuerdoRequest request)
        {
            try
            {
                await _service.ActualizarAcuerdo(reunionAcuerdoId, request, GetUserId());
                return Ok(new { message = "Acuerdo actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION ACTUALIZAR ACUERDO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Elimina (soft delete) un acuerdo del acta.</summary>
        [HttpDelete("acuerdos/{reunionAcuerdoId:int}")]
        public async Task<IActionResult> EliminarAcuerdo(int reunionAcuerdoId)
        {
            try
            {
                await _service.EliminarAcuerdo(reunionAcuerdoId, GetUserId());
                return Ok(new { message = "Acuerdo eliminado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION ELIMINAR ACUERDO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Carpeta de SharePoint para adjuntos ──────────────────────────────

        /// <summary>Carpeta única configurada para guardar los adjuntos (null si aún no se configuró).</summary>
        [HttpGet("carpeta")]
        public async Task<IActionResult> GetCarpeta()
        {
            try
            {
                return Ok(await _service.GetFolder());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION GET CARPETA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Configura/actualiza la carpeta única: recibe el link, lo detecta y lo guarda.</summary>
        [HttpPut("carpeta")]
        public async Task<IActionResult> SaveCarpeta([FromBody] ReunionFolderSaveDto dto)
        {
            try
            {
                return Ok(await _service.SaveFolder(dto, GetUserId()));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION SAVE CARPETA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Archivos ─────────────────────────────────────────────────────────

        /// <summary>Adjunta uno o varios archivos a la reunión (diapositivas, documentos, etc.).</summary>
        [HttpPost("{reunionId:int}/archivos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubirArchivos(int reunionId, [FromForm] IFormFileCollection files)
        {
            try
            {
                var archivos = await _service.SubirArchivos(reunionId, files, GetUserId());
                return Ok(new { archivos, message = "Archivos subidos exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION SUBIR ARCHIVOS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Elimina (soft delete) un archivo adjunto.</summary>
        [HttpDelete("archivos/{reunionArchivoId:int}")]
        public async Task<IActionResult> EliminarArchivo(int reunionArchivoId)
        {
            try
            {
                await _service.EliminarArchivo(reunionArchivoId, GetUserId());
                return Ok(new { message = "Archivo eliminado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION ELIMINAR ARCHIVO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Info del acuerdo/responsable para la página de aceptar/rechazar (link del correo del acta).</summary>
        [HttpGet("acuerdos-responsables/{reunionAcuerdoResponsableId:int}")]
        public async Task<IActionResult> GetAcuerdoResponsableInfo(int reunionAcuerdoResponsableId)
        {
            try
            {
                return Ok(await _service.GetAcuerdoResponsableInfo(reunionAcuerdoResponsableId, GetUserId()));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION ACUERDO RESPONSABLE INFO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Acepta o rechaza el acuerdo, en representación del responsable autenticado.</summary>
        [HttpPost("acuerdos-responsables/{reunionAcuerdoResponsableId:int}/decision")]
        public async Task<IActionResult> ResponderAcuerdo(int reunionAcuerdoResponsableId, [FromBody] AcuerdoResponsableDecisionRequest request)
        {
            try
            {
                await _service.ResponderAcuerdo(reunionAcuerdoResponsableId, GetUserId(), request);
                return Ok(new { message = "Respuesta registrada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ACTAS REUNION RESPONDER ACUERDO: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
