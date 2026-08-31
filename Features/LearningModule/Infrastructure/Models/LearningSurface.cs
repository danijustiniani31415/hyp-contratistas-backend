namespace Abril_Backend.Features.LearningModule.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de "superficies" donde puede mostrarse una categoría de aprendizaje:
    /// LOGIN (modal de videos en /auth/login, público) o INICIO (Centro de aprendizaje
    /// y guías en /inicio, autenticado). Es el campo que diferencia dónde aparecen los
    /// videos (regla de normalización: los valores predefinidos viven en su propia tabla).
    /// </summary>
    public class LearningSurface
    {
        public int LearningSurfaceId { get; set; }
        /// <summary>Código estable usado en el código (LOGIN | INICIO).</summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>Nombre para mostrar en el admin.</summary>
        public string Name { get; set; } = string.Empty;
    }
}
