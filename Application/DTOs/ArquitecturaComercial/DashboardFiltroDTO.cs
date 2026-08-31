namespace Abril_Backend.Application.DTOs.ArquitecturaComercial;

public class DashboardFiltroDTO
{
    public int? CategoriaId { get; set; }
    public int? ProyectoId  { get; set; }
    public int? UserId      { get; set; }
    public int? Semana      { get; set; }
    public int? Mes         { get; set; }
    public int? Anio        { get; set; }
}
