namespace Abril_Backend.Features.LearningModule.Infrastructure.Models
{
    /// <summary>
    /// Video-guía individual (enlace de Loom/YouTube/etc.) perteneciente a una categoría.
    /// Hereda de su categoría la superficie (login/inicio) y la visibilidad por rol.
    /// </summary>
    public class LearningVideo
    {
        public int LearningVideoId { get; set; }
        public int LearningCategoryId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        /// <summary>Miniatura opcional; si es null el front muestra un ícono de play genérico.</summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>Orden de aparición del video dentro de su categoría (menor primero).</summary>
        public int DisplayOrder { get; set; }

        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;

        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }

        public LearningCategory? Category { get; set; }
    }
}
