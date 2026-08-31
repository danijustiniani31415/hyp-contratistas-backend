namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Application.Dtos
{
    public class LessonAreaSegmentDTO
    {
        public string AreaItemName { get; set; } = string.Empty;
        public string AreaTypeName { get; set; } = string.Empty;
    }

    /// <summary>Una fila por cada nodo de area_scope, con su path completo.</summary>
    public class LessonAreaConfigItemDTO
    {
        public int? LessonAreaId { get; set; } // null si todavía no se ha togglado
        public int AreaScopeId { get; set; }
        public List<LessonAreaSegmentDTO> Path { get; set; } = new();
        public bool Active { get; set; }

        // Flags de personalización (solo aplican si Active).
        public bool IncludeInForm { get; set; }
        public bool IncludeDescendants { get; set; }
        /// <summary>El área se muestra como opción independiente en el formulario (requiere IncludeInForm).</summary>
        public bool IncludeAsIndependent { get; set; }

        // Ayudas para la UI (habilitar/deshabilitar toggles).
        /// <summary>El nodo tiene plantilla (scope_item). Requisito para "En formulario".</summary>
        public bool HasScope { get; set; }
        /// <summary>El nodo tiene hijos en el árbol. Requisito para "Agrupar subáreas".</summary>
        public bool HasChildren { get; set; }
    }

    public class ToggleLessonAreaResultDTO
    {
        public int LessonAreaId { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>Resultado de prender/apagar un flag (include_in_form / include_descendants).</summary>
    public class SetLessonAreaFlagResultDTO
    {
        public int LessonAreaId { get; set; }
        public bool Value { get; set; }
    }
}
