namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    public class WorkItemCategorySimpleDTO {
        public int WorkItemCategoryId {get; set;}
        public string? WorkItemCategoryDescription {get; set;}
        /// <summary>Especialidad a la que pertenece la partida de control (FK a work_specialty). Permite filtrar en cascada en el front.</summary>
        public int? WorkSpecialtyId {get; set;}
        public int? InstructivosSyncStatus {get; set;}
        /// <summary>Nombre del instructivo asociado (carpeta sincronizada en OneDrive), si existe.</summary>
        public string? InstructivosFolderName {get; set;}
    }
}
