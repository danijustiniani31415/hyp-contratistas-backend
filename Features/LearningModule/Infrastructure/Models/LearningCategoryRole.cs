namespace Abril_Backend.Features.LearningModule.Infrastructure.Models
{
    /// <summary>
    /// Relación N:M entre una categoría de aprendizaje y los roles que pueden verla en
    /// /inicio. Solo se consulta cuando la categoría NO es <c>EsPublicoInterno</c>.
    /// La llave primaria compuesta (learning_category_id, role_id) se configura en
    /// AppDbContext.OnModelCreating.
    /// </summary>
    public class LearningCategoryRole
    {
        public int LearningCategoryId { get; set; }
        public int RoleId { get; set; }

        public LearningCategory? Category { get; set; }
    }
}
