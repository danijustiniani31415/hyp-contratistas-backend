using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/catalogos")]
    [Authorize]
    public class CatalogosHabilitacionController : ControllerBase
    {
        private readonly ICatalogosHabilitacionRepository _repo;
        private readonly IJefePersonalizadoService _jefePersonalizado;
        private readonly ILogger<CatalogosHabilitacionController> _logger;

        public CatalogosHabilitacionController(
            ICatalogosHabilitacionRepository repo,
            IJefePersonalizadoService jefePersonalizado,
            ILogger<CatalogosHabilitacionController> logger)
        {
            _repo = repo;
            _jefePersonalizado = jefePersonalizado;
            _logger = logger;
        }

        [HttpGet("items-trabajador")]
        public async Task<IActionResult> GetItemsTrabajador()
        {
            try
            {
                var items = await _repo.GetItemsTrabajadorAsync();
                var result = items.Select(x => new SsItemTrabajadorDto
                {
                    Id = x.Id,
                    Nombre = x.Nombre,
                    AplicaA = x.AplicaA,
                    Responsable = x.Responsable,
                    RequiereVigencia = x.RequiereVigencia,
                    EsSctrVidaley = x.EsSctrVidaley,
                    Orden = x.Orden,
                    Activo = x.Activo
                }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetItemsTrabajador"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("items-empresa")]
        public async Task<IActionResult> GetItemsEmpresa()
        {
            try
            {
                var items = await _repo.GetItemsEmpresaAsync();
                var result = items.Select(x => new SsItemEmpresaDto
                {
                    Id = x.Id,
                    Nombre = x.Nombre,
                    Responsable = x.Responsable,
                    Orden = x.Orden,
                    RequiereVigencia = x.RequiereVigencia,
                    Activo = x.Activo
                }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetItemsEmpresa"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("items-equipo")]
        public async Task<IActionResult> GetItemsEquipo()
        {
            try
            {
                var items = await _repo.GetItemsEquipoAsync();
                var result = items.Select(x => new SsItemEquipoDto
                {
                    Id = x.Id,
                    Nombre = x.Nombre,
                    RequiereVigencia = x.RequiereVigencia,
                    Orden = x.Orden,
                    Activo = x.Activo
                }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetItemsEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("criterios")]
        public async Task<IActionResult> GetCriterios()
        {
            try
            {
                var items = await _repo.GetCriteriosEvaluacionAsync();
                var result = items.Select(x => new SsCriterioDto
                {
                    Id = x.Id,
                    Criterio = x.Criterio,
                    Orden = x.Orden,
                    Activo = x.Activo
                }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetCriterios"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Árbol de áreas (area_scope) para los desplegables en cascada del formulario de
        /// trabajadores, con la equivalencia legacy area/subárea/jefatura y el revisor que le
        /// tocaría al trabajador ya resueltos por nodo. Una sola llamada: al cambiar de área el
        /// formulario no vuelve al servidor.
        /// </summary>
        [HttpGet("areas-arbol")]
        public async Task<IActionResult> GetAreaArbol()
        {
            try
            {
                return Ok(await _repo.GetAreaArbolAsync());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetAreaArbol"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Trabajadores que pueden ser jefe (los que tienen correo corporativo @abril.pe),
        /// para el desplegable que aparece al marcar "Jefe personalizado" en el formulario de
        /// trabajadores. No exige que tengan usuario del sistema.
        /// </summary>
        [HttpGet("jefes")]
        public async Task<IActionResult> GetJefes()
        {
            try
            {
                return Ok(await _jefePersonalizado.GetCandidatosAsync());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetJefes"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Catálogo Obra / Staff / Oficina Central, para el desplegable del formulario
        /// de trabajadores (workers.obra_oficina_staff_id).
        /// </summary>
        [HttpGet("obra-oficina-staff")]
        public async Task<IActionResult> GetObraOficinaStaff()
        {
            try
            {
                return Ok(await _repo.GetObraOficinaStaffAsync());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetObraOficinaStaff"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("areas")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAreas()
        {
            try
            {
                var areas = await _repo.GetAreasAsync();
                var result = areas.Select(a => new AreaSimpleDto { Area = a }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetAreas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("subareas")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSubareas([FromQuery] string? area)
        {
            try
            {
                var items = await _repo.GetSubareasAsync(area);
                var result = items.Select(x => new CatSubareaDto
                {
                    Id = x.Id,
                    Subarea = x.Subarea,
                    Area = x.Area,
                    Jefatura = x.Jefatura
                }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetSubareas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("categorias")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategorias()
        {
            try
            {
                var items = await _repo.GetCategoriasAsync();
                var result = items.Select(x => new CatCategoriaDto
                {
                    Id = x.CategoriaId,
                    Nombre = x.Nombre
                }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetCategorias"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Catálogo único de puestos. Reemplaza al viejo endpoint "ocupaciones": la
        /// ocupación dejó de existir como campo aparte y su data se fusionó acá.
        /// </summary>
        [HttpGet("puestos")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPuestos()
        {
            try
            {
                var items = await _repo.GetPuestosAsync();
                var result = items.Select(x => new PuestoDto
                {
                    Id = x.PuestoId,
                    Nombre = x.Nombre,
                    CategoriaId = x.CategoriaId
                }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosHabilitacionController.GetPuestos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── Tipos de equipo ──────────────────────────────────────────

        [HttpGet("tipos-equipo")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTiposEquipo()
        {
            try
            {
                var items = await _repo.GetTiposEquipoAsync();
                var result = items.Select(x => new TipoEquipoDto { Id = x.Id, Nombre = x.Nombre }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetTiposEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("tipos-equipo/admin")]
        [RequireFeature("habilitacion.catalogos.equipos")]
        public async Task<IActionResult> GetTiposEquipoAdmin()
        {
            try
            {
                var items = await _repo.GetTiposEquipoTodosAsync();
                return Ok(items.Select(MapTipoEquipo).ToList());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetTiposEquipoAdmin"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("tipos-equipo")]
        [RequireFeature("habilitacion.catalogos.equipos")]
        public async Task<IActionResult> CrearTipoEquipo([FromBody] CatNombreRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return BadRequest(new { message = "El nombre es requerido." });
                var tipo = await _repo.CrearTipoEquipoAsync(req.Nombre.Trim());
                return Ok(MapTipoEquipo(tipo));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CrearTipoEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("tipos-equipo/{id:int}")]
        [RequireFeature("habilitacion.catalogos.equipos")]
        public async Task<IActionResult> ActualizarTipoEquipo(int id, [FromBody] CatNombreRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return BadRequest(new { message = "El nombre es requerido." });
                var tipo = await _repo.ActualizarTipoEquipoAsync(id, req.Nombre.Trim());
                return Ok(MapTipoEquipo(tipo));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ActualizarTipoEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("tipos-equipo/{id:int}/toggle")]
        [RequireFeature("habilitacion.catalogos.equipos")]
        public async Task<IActionResult> ToggleTipoEquipo(int id, [FromBody] CatToggleRequest req)
        {
            try
            {
                await _repo.ToggleTipoEquipoAsync(id, req.Activo);
                return Ok();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ToggleTipoEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── Ítems de equipo (entregables) ────────────────────────────

        [HttpGet("items-equipo/admin")]
        [RequireFeature("habilitacion.catalogos.equipos")]
        public async Task<IActionResult> GetItemsEquipoAdmin()
        {
            try
            {
                var items = await _repo.GetItemsEquipoTodosAsync();
                return Ok(items.Select(MapItemEquipo).ToList());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetItemsEquipoAdmin"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("items-equipo")]
        [RequireFeature("habilitacion.catalogos.equipos")]
        public async Task<IActionResult> CrearItemEquipo([FromBody] ItemEquipoUpsertRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return BadRequest(new { message = "El nombre es requerido." });
                var item = await _repo.CrearItemEquipoAsync(req.Nombre.Trim(), req.RequiereVigencia, req.TipoEquipoId);
                return Ok(MapItemEquipo(item));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CrearItemEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("items-equipo/{id:int}")]
        [RequireFeature("habilitacion.catalogos.equipos")]
        public async Task<IActionResult> ActualizarItemEquipo(int id, [FromBody] ItemEquipoUpsertRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return BadRequest(new { message = "El nombre es requerido." });
                var item = await _repo.ActualizarItemEquipoAsync(id, req.Nombre.Trim(), req.RequiereVigencia, req.TipoEquipoId);
                return Ok(MapItemEquipo(item));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ActualizarItemEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("items-equipo/{id:int}/toggle")]
        [RequireFeature("habilitacion.catalogos.equipos")]
        public async Task<IActionResult> ToggleItemEquipo(int id, [FromBody] CatToggleRequest req)
        {
            try
            {
                await _repo.ToggleItemEquipoAsync(id, req.Activo);
                return Ok();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ToggleItemEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── Configuración → Categorías y Puestos ─────────────────────

        /// <summary>
        /// Carga inicial de la pantalla de configuración: categorías + puestos + árbol de
        /// áreas en una sola petición (la pantalla necesita las tres listas de entrada,
        /// porque cada puesto muestra y elige su categoría y sus áreas).
        /// </summary>
        [HttpGet("admin")]
        public async Task<IActionResult> GetCatalogosAdmin()
        {
            try
            {
                var categorias = await _repo.GetCategoriasTodasAsync();
                var puestos = await _repo.GetPuestosTodosAsync();
                var areaTree = await _repo.GetAreaTreePuestosAsync();
                return Ok(new CatalogosAdminDto
                {
                    Categorias = categorias.Select(MapCategoria).ToList(),
                    Puestos = puestos,
                    AreaTree = areaTree
                });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetCatalogosAdmin"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── Categorías CRUD ──────────────────────────────────────────

        [HttpGet("categorias/admin")]
        public async Task<IActionResult> GetCategoriasAdmin()
        {
            try
            {
                var items = await _repo.GetCategoriasTodasAsync();
                return Ok(items.Select(MapCategoria).ToList());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetCategoriasAdmin"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("categorias")]
        public async Task<IActionResult> CrearCategoria([FromBody] CatNombreRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return BadRequest(new { message = "El nombre es requerido." });
                var cat = await _repo.CrearCategoriaAsync(req.Nombre.Trim());
                return Ok(new CatCategoriaAdminDto { Id = cat.CategoriaId, Nombre = cat.Nombre, Orden = cat.Orden, Activo = cat.Active });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CrearCategoria"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("categorias/{id}")]
        public async Task<IActionResult> ActualizarCategoria(int id, [FromBody] CatNombreRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return BadRequest(new { message = "El nombre es requerido." });
                var cat = await _repo.ActualizarCategoriaAsync(id, req.Nombre.Trim());
                return Ok(new CatCategoriaAdminDto { Id = cat.CategoriaId, Nombre = cat.Nombre, Orden = cat.Orden, Activo = cat.Active });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ActualizarCategoria"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("categorias/{id}/toggle")]
        public async Task<IActionResult> ToggleCategoria(int id, [FromBody] CatToggleRequest req)
        {
            try
            {
                await _repo.ToggleCategoriaAsync(id, req.Activo);
                return Ok();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ToggleCategoria"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── Puestos CRUD ─────────────────────────────────────────────

        [HttpGet("puestos/admin")]
        public async Task<IActionResult> GetPuestosAdmin()
        {
            try
            {
                return Ok(await _repo.GetPuestosTodosAsync());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetPuestosAdmin"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Trabajadores que usan el puesto. Es el detalle que abre la fila de la tabla, así
        /// que se pide bajo demanda (un puesto puede tener cientos de fichas: mandarlas
        /// todas en la carga inicial de la pantalla sería traer data que casi nunca se abre).
        /// </summary>
        [HttpGet("puestos/{id}/trabajadores")]
        public async Task<IActionResult> GetTrabajadoresDePuesto(int id)
        {
            try
            {
                return Ok(await _repo.GetTrabajadoresPorPuestoAsync(id));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en GetTrabajadoresDePuesto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("puestos")]
        public async Task<IActionResult> CrearPuesto([FromBody] PuestoUpsertRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return BadRequest(new { message = "El nombre es requerido." });
                if (req.CategoriaId is null or <= 0)
                    return BadRequest(new { message = "La categoría es requerida." });
                var puesto = await _repo.CrearPuestoAsync(req.Nombre.Trim(), req.CategoriaId.Value, req.AreaSolicitanteScopeId, req.AreaDestinoScopeId);
                return Ok(MapPuesto(puesto));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CrearPuesto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("puestos/{id}")]
        public async Task<IActionResult> ActualizarPuesto(int id, [FromBody] PuestoUpsertRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return BadRequest(new { message = "El nombre es requerido." });
                if (req.CategoriaId is null or <= 0)
                    return BadRequest(new { message = "La categoría es requerida." });
                var puesto = await _repo.ActualizarPuestoAsync(id, req.Nombre.Trim(), req.CategoriaId.Value, req.AreaSolicitanteScopeId, req.AreaDestinoScopeId);
                return Ok(MapPuesto(puesto));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ActualizarPuesto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("puestos/{id}/toggle")]
        public async Task<IActionResult> TogglePuesto(int id, [FromBody] CatToggleRequest req)
        {
            try
            {
                await _repo.TogglePuestoAsync(id, req.Activo);
                return Ok();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TogglePuesto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Elimina (soft delete) un puesto. Se rechaza con 400 si hay trabajadores usándolo:
        /// en ese caso lo que corresponde es desactivarlo.
        /// </summary>
        [HttpDelete("puestos/{id}")]
        public async Task<IActionResult> EliminarPuesto(int id)
        {
            try
            {
                await _repo.EliminarPuestoAsync(id);
                return Ok();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EliminarPuesto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Elimina (soft delete) los puestos seleccionados en la tabla. Va por POST y no por
        /// DELETE porque el lote viaja en el cuerpo. Los puestos en uso se omiten y se
        /// informan en la respuesta en vez de rechazar todo el lote.
        /// </summary>
        [HttpPost("puestos/eliminar")]
        public async Task<IActionResult> EliminarPuestos([FromBody] PuestosEliminarRequest req)
        {
            try
            {
                return Ok(await _repo.EliminarPuestosAsync(req.Ids.Distinct().ToList()));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EliminarPuestos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── Mapeos compartidos ───────────────────────────────────────

        private static CatCategoriaAdminDto MapCategoria(Categoria x) => new()
        {
            Id = x.CategoriaId, Nombre = x.Nombre, Orden = x.Orden, Activo = x.Active
        };

        /// <summary>
        /// Respuesta de alta/edición de un puesto. Los nombres de las áreas van en null a
        /// propósito: el que guarda ya las tiene en pantalla y la tabla se recarga entera
        /// después de guardar, así que resolverlas acá serían dos joins para nada.
        /// </summary>
        private static PuestoAdminDto MapPuesto(Puesto x) => new()
        {
            Id = x.PuestoId,
            Nombre = x.Nombre,
            CategoriaId = x.CategoriaId,
            AreaSolicitanteScopeId = x.AreaSolicitanteScopeId,
            AreaDestinoScopeId = x.AreaDestinoScopeId,
            Orden = x.Orden,
            Activo = x.Active
        };

        private static TipoEquipoAdminDto MapTipoEquipo(SsTipoEquipo x) => new()
        {
            Id = x.Id, Nombre = x.Nombre, Orden = x.Orden, Activo = x.Activo
        };

        private static ItemEquipoAdminDto MapItemEquipo(SsItemEquipo x) => new()
        {
            Id = x.Id,
            Nombre = x.Nombre,
            RequiereVigencia = x.RequiereVigencia,
            Orden = x.Orden,
            Activo = x.Activo,
            TipoEquipoId = x.TipoEquipoId,
            TipoEquipoNombre = x.TipoEquipo?.Nombre,
        };
    }
}
