namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos
{
    /// <summary>
    /// Payload de edición de una lección (solo el autor). Hereda los campos de
    /// creación y agrega la lista de imágenes existentes a eliminar.
    /// </summary>
    public class LessonUpdateDTO : LessonCreateDTO
    {
        /// <summary>lesson_image_id de imágenes existentes que el autor quitó.</summary>
        public List<int>? RemovedImageIds { get; set; }
    }
}
