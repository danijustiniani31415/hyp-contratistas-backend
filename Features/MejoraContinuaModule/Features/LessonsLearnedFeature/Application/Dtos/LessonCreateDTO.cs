using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos
{
    public class LessonCreateDTO
    {
        [MaxLength(2000, ErrorMessage = "La descripción del problema no puede exceder 2000 caracteres.")]
        public string ProblemDescription { get; set; } = string.Empty;
        [MaxLength(2000, ErrorMessage = "La descripción de la causa no puede exceder 2000 caracteres.")]
        public string ReasonDescription { get; set; } = string.Empty;
        [MaxLength(2000, ErrorMessage = "La descripción de la lección no puede exceder 2000 caracteres.")]
        public string LessonDescription { get; set; } = string.Empty;
        [MaxLength(2000, ErrorMessage = "La descripción del impacto no puede exceder 2000 caracteres.")]
        public string ImpactDescription { get; set; } = string.Empty;

        public int ProjectId { get; set; }

        /// <summary>Legacy: area_id (lecciones viejas). Opcional para lecciones nuevas.</summary>
        public int AreaId { get; set; }

        /// <summary>Rama seleccionada de lesson_area (modelo nuevo).</summary>
        public int? LessonAreaId { get; set; }

        /// <summary>Nodo hoja seleccionado del árbol de scope (catalog_item_id).</summary>
        public int? CatalogItemId { get; set; }

        /// <summary>
        /// Obra / Staff / Oficina Central (FK a <c>workers_obra_oficina_staff</c>).
        /// Sustituye al antiguo último nodo del árbol de áreas de tipo Obra_Oficina:
        /// ahora es un desplegable propio del formulario.
        /// </summary>
        public int? ObraOficinaStaffId { get; set; }

        public List<IFormFile>? OpportunityImages { get; set; }
        public List<IFormFile>? ImprovementImages { get; set; }
    }
}
