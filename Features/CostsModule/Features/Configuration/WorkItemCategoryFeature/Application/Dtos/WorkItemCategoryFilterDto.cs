namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Dtos
{
    public class WorkItemCategoryFilterDto
    {
        public string? Description { get; set; }
        /// <summary>
        /// true: solo categorías con instructivo (carpeta sincronizada o manual).
        /// false: solo categorías sin instructivo. null: todas.
        /// </summary>
        public bool? HasInstructivo { get; set; }
        /// <summary>
        /// true: solo categorías con al menos una cláusula activa (contrato, anexo 3 o anexo 4).
        /// false: solo categorías sin ninguna cláusula. null: todas.
        /// </summary>
        public bool? HasClause { get; set; }
        /// <summary>Filtra las partidas de control por la especialidad a la que pertenecen. null: todas.</summary>
        public int? WorkSpecialtyId { get; set; }
        /// <summary>true: solo activas · false: solo inactivas · null: todas.</summary>
        public bool? Active { get; set; }
        public int Page { get; set; } = 1;
    }
}
