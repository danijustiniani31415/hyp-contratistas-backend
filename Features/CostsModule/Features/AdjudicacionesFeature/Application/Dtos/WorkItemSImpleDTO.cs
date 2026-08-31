namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    public class WorkItemSimpleDTO {
        public int WorkItemId {get; set;}
        public string? WorkItemDescription {get; set;}
        /// <summary>Partida de control a la que pertenece la partida (FK a work_item_category). Permite filtrar en cascada en el front.</summary>
        public int? WorkItemCategoryId {get; set;}

        /// <summary>Formas de valorización (cláusula 5.1) asociadas a la partida, ordenadas por SortOrder.</summary>
        public List<WorkItemValorizationFormSimpleDTO> ValorizationForms { get; set; } = new();
    }

    /// <summary>Línea de forma de valorización (porcentaje + concepto) de una partida.</summary>
    public class WorkItemValorizationFormSimpleDTO {
        /// <summary>Solo se usa para agrupar al leer desde la BD; no se expone al front.</summary>
        public int WorkItemId {get; set;}
        public string? Concept {get; set;}
        public decimal Percentage {get; set;}
        public int SortOrder {get; set;}
    }
}
