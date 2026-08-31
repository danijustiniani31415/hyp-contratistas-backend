using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos
{
    /// <summary>
    /// Carga inicial de la pantalla de Usuarios: las opciones de los filtros y la primera
    /// página de la tabla en una sola petición. Los cambios de filtro, búsqueda o página
    /// posteriores van al endpoint <c>paged</c>, que solo devuelve la tabla — las opciones
    /// ya viajaron una vez y no cambian mientras la pantalla está abierta.
    /// </summary>
    public class UserListInitialDto
    {
        public List<UserCategoriaOptionDto> Categorias { get; set; } = new();
        public PagedResult<UserListItemDto> Users { get; set; } = null!;
    }

    /// <summary>
    /// Categoría de trabajador para el desplegable del filtro. Se llega a ella por
    /// <c>workers.puesto_id → puesto.categoria_id</c>: la ficha ya no guarda la categoría.
    ///
    /// Solo se listan las categorías que tienen al menos un usuario del sistema detrás, para
    /// no ofrecer opciones que dejarían la tabla vacía (en prod son 27 de las 44 del catálogo).
    /// </summary>
    public class UserCategoriaOptionDto
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = null!;
    }
}
