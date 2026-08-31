using Abril_Backend.Features.SsomaModule.Shared;

namespace Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Dtos
{
    public class MiSaludResumenDto
    {
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }

        // EMO activo
        public bool TieneEmo { get; set; }
        public int? EmoId { get; set; }
        public string? TipoEmo { get; set; }
        public string? Aptitud { get; set; }
        public DateOnly? FechaEmo { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public int? DiasParaVencer { get; set; }

        // Restricciones vigentes del EMO activo
        public List<string> RestriccionesVigentes { get; set; } = [];

        // Último descanso
        public string? UltimoDescansoEstado { get; set; }
        public DateOnly? UltimoDescansoFechaFin { get; set; }

        // Catálogo para el formulario de registro (evita otro roundtrip al abrir el modal).
        // Solo los tipos que el trabajador puede elegir: "Accidente común" y "Enfermedad común",
        // que se le muestran con su nombre corto ("Accidente" / "Enfermedad").
        public List<DescansoTipoDto> TiposDescanso { get; set; } = [];
    }

    public class MiDescansoDto
    {
        public int Id { get; set; }
        /// <summary>Nombre del tipo resuelto desde el catálogo (ss_descanso_tipo).</summary>
        public string Tipo { get; set; } = string.Empty;
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int? Dias { get; set; }
        public string? Diagnostico { get; set; }
        public string? Estado { get; set; }
        public string? MotivoRechazo { get; set; }
        public string? UrlCertificado { get; set; }
        public string? UrlDocumento { get; set; }
        public List<MiDescansoAdjuntoDto> Adjuntos { get; set; } = [];
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class MiDescansoAdjuntoDto
    {
        /// <summary>
        /// Id del adjunto (ss_descanso_medico_adjunto). El frontend pide el archivo por este id
        /// al endpoint de descarga en vez de apuntar al link de SharePoint, que solo abre para
        /// quien ya tiene sesión de Microsoft 365 en el navegador.
        /// </summary>
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Nombre { get; set; }
    }

    /// <summary>Datos mínimos de un adjunto para servir su contenido desde el backend.</summary>
    public class MiDescansoAdjuntoArchivoDto
    {
        public string Url { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public string? DriveId { get; set; }
        public string? ItemId { get; set; }
    }

    public class CrearMiDescansoDto
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int? Dias { get; set; }
        /// <summary>Tipo del catálogo; solo se aceptan los marcados como disponibles en Mi Salud.</summary>
        public int TipoId { get; set; }
        public string? Diagnostico { get; set; }
        public List<IFormFile>? Documentos { get; set; }
    }

    public class MiDescansosFiltroDto
    {
        public int Page { get; set; } = 1;
    }

    /// <summary>
    /// Datos para la notificación por correo al registrar un descanso médico:
    /// trabajador (destinatario principal) + correo del área GTH (area_scope.email).
    /// </summary>
    public class DescansoNotificacionDatosDto
    {
        public string? WorkerNombre { get; set; }
        public string? WorkerEmail { get; set; }
        public string? GthEmail { get; set; }
        /// <summary>Nombre largo del tipo ("Accidente común"), que es lo que se reporta.</summary>
        public string? TipoNombre { get; set; }
    }

    /// <summary>
    /// Configuración de un destinatario del correo de descanso médico
    /// (pantalla de Configuración de Mi Salud).
    /// </summary>
    public class MiDescansoCorreoConfigDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        /// <summary>true = se envía el correo a este destinatario; false = no se envía.</summary>
        public bool Active { get; set; }
        public int Orden { get; set; }
    }

    /// <summary>Cuerpo del PUT para prender/apagar un destinatario de correo.</summary>
    public class ActualizarCorreoConfigDto
    {
        public bool Active { get; set; }
    }
}
