namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion
{
    /// <summary>
    /// Origen de un destinatario, para poder etiquetarlo en el formulario. Coincide con el
    /// código del destinatario en la Configuración de EMOs (ver
    /// <c>EmoCorreoDestinatarioCodigo</c>): CLINICA, JEFE, TRABAJADOR, RESIDENTE,
    /// COORD_ADMIN, COORD_SSOMA, ADMIN_RAZON_SOCIAL, GTH, MEDICINA_OCUPACIONAL,
    /// ARQCOM_*, POSTVENTA_* o <see cref="Adicional"/>.
    /// </summary>
    public static class ProgramacionDestinatarioOrigen
    {
        public const string Clinica = "CLINICA";
        public const string Jefe    = "JEFE";
        /// <summary>Correo adicional escrito a mano en la Configuración de EMOs.</summary>
        public const string Adicional = "ADICIONAL";
    }

    public class ProgramacionDestinatarioDto
    {
        public string Email { get; set; } = string.Empty;
        /// <summary>Etiqueta para mostrar (nombre de la clínica, nombre del jefe, nombre del destinatario).</summary>
        public string? Nombre { get; set; }
        /// <summary>Código del destinatario — ver <see cref="ProgramacionDestinatarioOrigen"/>.</summary>
        public string Origen { get; set; } = ProgramacionDestinatarioOrigen.Adicional;
    }

    /// <summary>
    /// Destinatarios ya resueltos de un correo de EMO. Lo devuelve
    /// <c>IEmoDestinatariosResolver</c>, así que la vista previa del modal
    /// "Programar EMO con clínica" y el envío real salen del mismo cálculo: lo que el
    /// usuario ve antes de guardar es literalmente lo que se va a enviar.
    /// </summary>
    public class ProgramacionDestinatariosDto
    {
        public List<ProgramacionDestinatarioDto> Para { get; set; } = new();
        public List<ProgramacionDestinatarioDto> Copias { get; set; } = new();

        /// <summary>
        /// La clínica es destinataria activa de este correo para el perfil del trabajador,
        /// pero todavía no se sabe cuál es: la vista previa del modal se pide antes de
        /// elegirla. Sus correos de contacto se suman a <see cref="Para"/> en cuanto el
        /// usuario la elija (CLINICA es siempre destinatario principal en el catálogo).
        ///
        /// Sin este dato la vista previa afirmaba que el correo solo le llegaba al jefe,
        /// cuando programar exige clínica y a la clínica siempre termina llegándole.
        /// </summary>
        public bool ClinicaPendiente { get; set; }

        /// <summary>
        /// La clínica es destinataria activa y ya está elegida, pero no tiene ni un correo
        /// de contacto activo ni correo en su ficha: no le va a llegar nada.
        /// </summary>
        public bool ClinicaSinCorreos { get; set; }
    }

    /// <summary>
    /// Vista previa del modal "Programar EMO con clínica". Programar dispara DOS correos
    /// en momentos distintos y con destinatarios distintos según la Configuración de EMOs,
    /// así que se muestran por separado para que el usuario no asuma que es el mismo:
    ///   • <see cref="Manual"/>   — sale al guardar la cita (evento PROGRAMACION_MANUAL).
    ///   • <see cref="Aceptada"/> — sale después, si la clínica acepta (evento ACEPTADA).
    /// </summary>
    public class ProgramacionDestinatariosPreviewDto
    {
        public ProgramacionDestinatariosDto Manual { get; set; } = new();
        public ProgramacionDestinatariosDto Aceptada { get; set; } = new();
    }
}
