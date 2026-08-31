namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Dtos
{
    public class WorkItemCategoryCreateDto
    {
        public string WorkItemCategoryDescription { get; set; } = null!;

        /// <summary>Especialidad a la que pertenece la partida de control (requerido).</summary>
        public int? WorkSpecialtyId { get; set; }
    }
}
