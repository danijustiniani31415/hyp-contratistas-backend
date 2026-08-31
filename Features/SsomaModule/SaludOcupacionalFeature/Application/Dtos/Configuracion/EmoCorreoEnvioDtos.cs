namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion
{
    /// <summary>
    /// Una celda activa de la matriz, aplanada para el envío: "a este destinatario le
    /// llega este correo cuando el trabajador es de este perfil".
    /// La consume <c>EmoDestinatariosResolver</c>; la pantalla usa
    /// <see cref="EmoCorreosConfigDto"/>, que es más rica.
    /// </summary>
    public class EmoCorreoReglaEnvioDto
    {
        /// <summary>OFICINA_CENTRAL, STAFF, OBRA o CONTRATISTA.</summary>
        public string PerfilCodigo { get; set; } = string.Empty;

        /// <summary>Código del destinatario de catálogo. Null en los correos adicionales.</summary>
        public string? DestinatarioCodigo { get; set; }

        /// <summary>Correo ya cargado (buzones de área y correos adicionales). Null en los dinámicos.</summary>
        public string? Email { get; set; }

        /// <summary>Etiqueta del destinatario, para mostrarla en la vista previa del modal.</summary>
        public string? Nombre { get; set; }

        /// <summary>true = va en "CC" en vez de en "Para".</summary>
        public bool EsCopia { get; set; }
    }
}
