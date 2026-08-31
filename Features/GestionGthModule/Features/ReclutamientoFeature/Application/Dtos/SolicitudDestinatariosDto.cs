using System.Text.Json.Serialization;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Destinatarios efectivos del correo de "solicitud de personal por aprobar" (tipo
    /// APROBACION_GG). Es lo mismo que se usa para enviar y lo que se le previsualiza al
    /// solicitante en el modal, para que el aviso nunca mienta respecto del envío real.
    /// </summary>
    public class SolicitudDestinatariosDto
    {
        /// <summary>Destinatarios principales (Para/To).</summary>
        public List<DestinatarioSolicitudDto> Para { get; set; } = new();

        /// <summary>Destinatarios en copia (CC).</summary>
        public List<DestinatarioSolicitudDto> Copias { get; set; } = new();

        /// <summary>
        /// false = el destinatario principal que pone el sistema (el solicitante, el postulante, el
        /// candidato…) está apagado desde Configuración, así que el correo sale solo con los
        /// destinatarios de arriba. En los correos que no tienen principal automático no aplica y
        /// queda en true.
        /// </summary>
        public bool PrincipalAutomaticoActivo { get; set; } = true;

        /// <summary>Correos de <see cref="Para"/>, en el formato que espera el servicio de correo.</summary>
        /// <remarks>Solo de uso interno: el frontend consume <see cref="Para"/>.</remarks>
        [JsonIgnore]
        public List<string> EmailsPara => Para.Select(d => d.Email).ToList();

        /// <summary>Correos de <see cref="Copias"/>, en el formato que espera el servicio de correo.</summary>
        /// <remarks>Solo de uso interno: el frontend consume <see cref="Copias"/>.</remarks>
        [JsonIgnore]
        public List<string> EmailsCopias => Copias.Select(d => d.Email).ToList();
    }

    /// <summary>Un destinatario del correo, con la razón por la que lo recibe.</summary>
    public class DestinatarioSolicitudDto
    {
        public string Email { get; set; } = string.Empty;

        /// <summary>Nombre de la persona cuando se conoce (los buzones configurados no lo tienen).</summary>
        public string? Nombre { get; set; }

        /// <summary>
        /// Por qué recibe el correo: <c>Gerencia General</c> para los destinatarios configurados
        /// del tipo APROBACION_GG, o <c>Gerente de {área}</c> para el gerente del área del
        /// solicitante. Se muestra como tooltip en el aviso del modal.
        /// </summary>
        public string Origen { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gerente del área del solicitante, resuelto desde <c>workers.area_scope_id</c> subiendo por
    /// el árbol de áreas hasta el primer nodo con un trabajador de categoría GERENTE.
    /// </summary>
    public class GerenteAreaDto
    {
        public int WorkerId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Nombre { get; set; }

        /// <summary>Nombre del nodo de <c>area_scope</c> donde se encontró al gerente.</summary>
        public string? AreaNombre { get; set; }
    }
}
