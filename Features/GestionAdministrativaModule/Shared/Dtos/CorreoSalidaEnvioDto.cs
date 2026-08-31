namespace Abril_Backend.Features.GestionAdministrativa.Shared.Dtos
{
    /// <summary>
    /// A quién se le manda un correo del flujo de salidas después de aplicar su configuración
    /// (ga_correo_evento / ga_correo_regla): el interruptor maestro del correo, el interruptor
    /// de su destinatario principal, las inclusiones y las exclusiones.
    /// </summary>
    public class CorreoSalidaEnvioDto
    {
        /// <summary>
        /// false = el correo NO debe enviarse: está apagado en la configuración o no quedó
        /// ningún destinatario al que mandárselo.
        /// </summary>
        public bool Enviar { get; set; }

        /// <summary>Destinatarios del "Para" (To). Vacío si <see cref="Enviar"/> es false.</summary>
        public List<string> Para { get; set; } = new();

        /// <summary>Destinatarios en copia (CC). Puede quedar vacío.</summary>
        public List<string> Copia { get; set; } = new();
    }
}
