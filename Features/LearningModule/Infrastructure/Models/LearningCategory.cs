namespace Abril_Backend.Features.LearningModule.Infrastructure.Models
{
    /// <summary>
    /// Grupo/área del centro de aprendizaje (p. ej. "Gestión de Salidas", "Lecciones
    /// Aprendidas", "Contratistas"). Agrupa videos-guía y define dónde (superficie) y a
    /// quién (roles / público interno) se muestran.
    /// </summary>
    public class LearningCategory
    {
        public int LearningCategoryId { get; set; }

        /// <summary>Superficie donde aparece la categoría: LOGIN o INICIO (learning_surface).</summary>
        public int LearningSurfaceId { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Color de acento del panel (hex, ej. "#0F6E56"). Null = teal por defecto.</summary>
        public string? AccentColor { get; set; }

        /// <summary>Orden de aparición del grupo (menor primero).</summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Solo aplica en INICIO: si true, la categoría es visible para TODO usuario interno
        /// autenticado sin importar su rol ("todo Abril"). Si false, se filtra por los roles
        /// declarados en learning_category_role.
        /// </summary>
        public bool EsPublicoInterno { get; set; }

        /// <summary>No aparece en listados/desplegables si es false (pero no se borra).</summary>
        public bool Active { get; set; } = true;

        /// <summary>Soft delete: false = eliminado lógicamente.</summary>
        public bool State { get; set; } = true;

        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }

        public LearningSurface? Surface { get; set; }
        public ICollection<LearningVideo> Videos { get; set; } = new List<LearningVideo>();
        public ICollection<LearningCategoryRole> Roles { get; set; } = new List<LearningCategoryRole>();
    }
}
