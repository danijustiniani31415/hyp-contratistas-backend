using Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface ICatalogosHabilitacionRepository
    {
        Task<List<SsItemTrabajador>> GetItemsTrabajadorAsync();
        Task<List<SsItemEmpresa>> GetItemsEmpresaAsync();
        Task<List<SsItemEquipo>> GetItemsEquipoAsync();
        Task<List<SsCriterioEvaluacion>> GetCriteriosEvaluacionAsync();

        /// <summary>
        /// Árbol de áreas con la equivalencia legacy y el revisor resueltos por nodo. Reemplaza a
        /// GetAreasAsync/GetSubareasAsync en el formulario de trabajadores, que ahora elige el nodo
        /// del árbol en vez de los textos area/subarea.
        /// </summary>
        Task<List<AreaArbolNodoDto>> GetAreaArbolAsync();

        /// <summary>
        /// Catálogo Obra / Staff / Oficina Central (workers_obra_oficina_staff). Es el
        /// desplegable que define la ubicación del trabajador; antes esa distinción se
        /// deducía del último nodo del árbol de áreas (tipo "Área Obra_Oficina").
        /// </summary>
        Task<List<ObraOficinaStaffDto>> GetObraOficinaStaffAsync();

        /// <summary>Áreas del catálogo legacy cat_subarea. Sigue vivo para otros consumidores.</summary>
        Task<List<string>> GetAreasAsync();
        Task<List<CatSubarea>> GetSubareasAsync(string? area);
        /// <summary>Categorías activas: el campo de lógica del trabajador.</summary>
        Task<List<Categoria>> GetCategoriasAsync();

        /// <summary>Puestos activos: el campo de presentación. Cada uno apunta a una categoría.</summary>
        Task<List<Puesto>> GetPuestosAsync();

        // Categorías CRUD
        Task<List<Categoria>> GetCategoriasTodasAsync();
        Task<Categoria> CrearCategoriaAsync(string nombre);
        Task<Categoria> ActualizarCategoriaAsync(int id, string nombre);
        Task ToggleCategoriaAsync(int id, bool activo);

        // Tipos de equipo CRUD
        /// <summary>Tipos de equipo activos (Volquete, Excavadora de Oruga, …), para el desplegable del formulario de equipos.</summary>
        Task<List<SsTipoEquipo>> GetTiposEquipoAsync();
        Task<List<SsTipoEquipo>> GetTiposEquipoTodosAsync();
        Task<SsTipoEquipo> CrearTipoEquipoAsync(string nombre);
        Task<SsTipoEquipo> ActualizarTipoEquipoAsync(int id, string nombre);
        Task ToggleTipoEquipoAsync(int id, bool activo);

        // Ítems de equipo (entregables) CRUD
        /// <summary>Todos los ítems de equipo (activos e inactivos), con el nombre de su tipo si aplica solo a uno.</summary>
        Task<List<SsItemEquipo>> GetItemsEquipoTodosAsync();
        Task<SsItemEquipo> CrearItemEquipoAsync(string nombre, bool requiereVigencia, int? tipoEquipoId);
        Task<SsItemEquipo> ActualizarItemEquipoAsync(int id, string nombre, bool requiereVigencia, int? tipoEquipoId);
        Task ToggleItemEquipoAsync(int id, bool activo);

        // Puestos CRUD
        /// <summary>
        /// Puestos vivos para la pantalla de configuración, con su categoría y la cantidad
        /// de fichas de trabajadores que los usa ya resueltas en la misma consulta.
        /// </summary>
        Task<List<PuestoAdminDto>> GetPuestosTodosAsync();
        /// <summary>
        /// Fichas de trabajadores que usan el puesto (nombre + correo corporativo), para el
        /// detalle que abre la fila. Mismo criterio que el conteo de GetPuestosTodosAsync.
        /// </summary>
        Task<List<PuestoTrabajadorDto>> GetTrabajadoresPorPuestoAsync(int puestoId);
        /// <summary>
        /// Árbol de áreas como lista plana, para el filtro por área de la sección Puestos y
        /// el selector de áreas del modal. Versión ligera de GetAreaArbolAsync: sin la
        /// equivalencia legacy ni los revisores, que son lo caro de resolver.
        /// </summary>
        Task<List<PuestoAreaNodoDto>> GetAreaTreePuestosAsync();
        Task<Puesto> CrearPuestoAsync(string nombre, int categoriaId, int? areaSolicitanteScopeId, int? areaDestinoScopeId);
        Task<Puesto> ActualizarPuestoAsync(int id, string nombre, int categoriaId, int? areaSolicitanteScopeId, int? areaDestinoScopeId);
        Task TogglePuestoAsync(int id, bool activo);
        /// <summary>Soft delete (state = false). Se rechaza si algún trabajador usa el puesto.</summary>
        Task EliminarPuestoAsync(int id);
        /// <summary>
        /// Soft delete en bloque. Omite (no falla) los puestos que tengan trabajadores
        /// usándolos y devuelve cuántos se eliminaron y cuántos se omitieron.
        /// </summary>
        Task<PuestosEliminarResultDto> EliminarPuestosAsync(IReadOnlyCollection<int> ids);
    }
}
