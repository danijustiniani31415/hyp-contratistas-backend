namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Infrastructure.Models
{
    /// <summary>
    /// Tabla filtro que decide qué rama de area_scope puede usarse en Lecciones Aprendidas.
    /// area_scope_id apunta a un nodo del árbol (típicamente una hoja); active = true
    /// significa que esa rama aparece como opción en /mejora-continua/lessons-learned.
    /// </summary>
    public class LessonArea
    {
        public int LessonAreaId { get; set; }
        public int AreaScopeId { get; set; }

        /// <summary>Interruptor maestro: si es false, el área no aparece en NINGÚN lado de lecciones.</summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Si true (y Active true), el área aparece como opción en el FORMULARIO de creación
        /// de lecciones. Requiere además tener plantilla (scope_item). Default false: el admin
        /// la habilita explícitamente.
        /// </summary>
        public bool IncludeInForm { get; set; } = false;

        /// <summary>
        /// Si true (y Active true), al seleccionar esta área en los FILTROS del dashboard/búsqueda
        /// se incluyen también sus áreas descendientes (rollup). Default false.
        /// </summary>
        public bool IncludeDescendants { get; set; } = false;

        /// <summary>
        /// Si true (requiere Active e IncludeInForm), el área se muestra como opción INDEPENDIENTE
        /// en el primer desplegable del formulario: aparece al tope, va directo a sus plantillas y
        /// NO despliega a sus áreas hijas. Las hijas solo aparecen si también son independientes.
        /// Default false.
        /// </summary>
        public bool IncludeAsIndependent { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
