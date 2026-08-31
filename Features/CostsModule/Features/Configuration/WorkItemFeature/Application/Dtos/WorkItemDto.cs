namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Dtos
{
    public class WorkItemDto
    {
        public int WorkItemId { get; set; }
        public string WorkItemDescription { get; set; } = null!;
        public int? WorkItemCategoryId { get; set; }
        public string? WorkItemCategoryDescription { get; set; }
        /// <summary>Especialidad a la que pertenece la partida de control.</summary>
        public string? WorkSpecialtyDescription { get; set; }
        /// <summary>true/false: la partida de control tiene o no instructivo. Null: sin partida de control.</summary>
        public bool? CategoryHasInstructivo { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }

        /// <summary>Formas de valorización (cláusula 5.1), ordenadas por SortOrder.</summary>
        public List<WorkItemValorizationFormDto> ValorizationForms { get; set; } = [];
    }

    /// <summary>Opción para el desplegable de partidas de control en el formulario.</summary>
    public class WorkItemCategoryOptionDto
    {
        public int WorkItemCategoryId { get; set; }
        public string WorkItemCategoryDescription { get; set; } = null!;
    }

    /// <summary>Una línea de la forma de valorización (porcentaje + concepto) de la partida.</summary>
    public class WorkItemValorizationFormDto
    {
        public int     WorkItemValorizationFormId { get; set; }
        public string  Concept                    { get; set; } = null!;
        public decimal Percentage                 { get; set; }
        public int     SortOrder                  { get; set; }
    }
}
