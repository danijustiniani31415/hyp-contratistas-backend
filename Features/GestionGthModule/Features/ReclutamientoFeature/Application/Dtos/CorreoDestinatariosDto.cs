namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Destinatarios del correo de "nueva solicitud de personal". Sirve tanto para leer
    /// (GET) como para guardar (PUT) la configuración: dos listas de correos.
    /// </summary>
    public class CorreoDestinatariosDto
    {
        /// <summary>Destinatarios principales (Para/To).</summary>
        public List<string> Principales { get; set; } = new();
        /// <summary>Destinatarios en copia (CC).</summary>
        public List<string> Copias { get; set; } = new();
    }
}
