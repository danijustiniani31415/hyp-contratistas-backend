namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers
{
    /// <summary>
    /// Ítem del catálogo <c>workers_category</c> (categoría normalizada del trabajador,
    /// usada por Lecciones Aprendidas y Solicitud de Salidas) para el select del modal
    /// Configuración → Trabajadores.
    /// </summary>
    public class WorkerCategoryDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
