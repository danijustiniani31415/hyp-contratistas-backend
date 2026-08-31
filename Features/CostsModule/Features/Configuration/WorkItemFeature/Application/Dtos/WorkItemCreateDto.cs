namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Dtos
{
    public class WorkItemCreateDto
    {
        public string WorkItemDescription { get; set; } = null!;

        /// <summary>Partida de control a la que pertenece la partida (requerido).</summary>
        public int? WorkItemCategoryId { get; set; }
    }
}
