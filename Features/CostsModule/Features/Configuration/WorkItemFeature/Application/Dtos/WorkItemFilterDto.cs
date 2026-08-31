namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Dtos
{
    public class WorkItemFilterDto
    {
        public string? Description { get; set; }
        /// <summary>
        /// true: solo partidas con al menos una forma de valorización activa.
        /// false: solo partidas sin forma de valorización. null: todas.
        /// </summary>
        public bool? HasValorizationForm { get; set; }
        /// <summary>Filtra las partidas por la partida de control a la que pertenecen. null: todas.</summary>
        public int? WorkItemCategoryId { get; set; }
        /// <summary>true: solo activas · false: solo inactivas · null: todas.</summary>
        public bool? Active { get; set; }
        public int Page { get; set; } = 1;
    }
}
