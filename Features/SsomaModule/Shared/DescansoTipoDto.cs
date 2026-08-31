namespace Abril_Backend.Features.SsomaModule.Shared
{
    /// <summary>
    /// Item del catálogo <c>ss_descanso_tipo</c> — el único clasificador de un descanso médico.
    /// Lo consumen Salud Ocupacional (Descansos, con los 4 tipos) y Mi Salud (solo los
    /// <c>disponible_mi_salud</c>), por eso vive en el Shared del módulo y no dentro de una feature.
    /// </summary>
    public class DescansoTipoDto
    {
        public int Id { get; set; }

        /// <summary>Nombre normalizado con el que se clasifica y se guarda ("Accidente común").</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Etiqueta corta para el trabajador ("Accidente"). Solo cambia lo que se muestra:
        /// lo que se guarda y se reporta siempre es <see cref="Nombre"/>.
        /// </summary>
        public string NombreCorto { get; set; } = string.Empty;
    }
}
