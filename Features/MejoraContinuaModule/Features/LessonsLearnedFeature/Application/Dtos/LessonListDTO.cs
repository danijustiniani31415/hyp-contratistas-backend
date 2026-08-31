namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos
{
    public class LessonListDTO
    {
        public int LessonId { get; set; }
        public string? LessonCode { get; set; }
        public string Period { get; set; } = string.Empty;
        public string? ProblemDescription { get; set; }
        public string? ReasonDescription { get; set; }
        public string? LessonDescription { get; set; }
        public string? ImpactDescription { get; set; }

        public int? ProjectId { get; set; }
        public string? ProjectDescription { get; set; }

        public int AreaId { get; set; }
        /// <summary>Path completo del área (incluye Gerencia). Para vista de detalle.</summary>
        public string? AreaDescription { get; set; }
        /// <summary>Path "corto" para tabla/tarjetas: sin Gerencia, MAYÚSCULAS.</summary>
        public string? AreaListDescription { get; set; }

        public int? LessonAreaId { get; set; }
        public int? CatalogItemId { get; set; }

        /// <summary>FK a <c>workers_obra_oficina_staff</c> (Obra / Staff / Oficina Central).</summary>
        public int? ObraOficinaStaffId { get; set; }
        public string? ObraOficinaStaffName { get; set; }
        /// <summary>
        /// Segmentos de la clasificación caminando scope_item hacia arriba.
        /// Cada entrada trae el nombre del catalog_type (Fase / Etapa / Nivel / …)
        /// y la descripción del catalog_item. Ordenados de raíz a hoja.
        /// </summary>
        public List<LessonClassificationSegmentDTO> ClassificationSegments { get; set; } = new();

        public int StateId { get; set; }
        public string StateDescription { get; set; } = string.Empty;
        /// <summary>PENDIENTE | APROBADA | RECHAZADA (flujo de aprobación por jefatura).</summary>
        public string ApprovalStatus { get; set; } = "PENDIENTE";
        public List<LessonImageDTO>? Images { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public string? CreatedUserFullName { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
    }
}
