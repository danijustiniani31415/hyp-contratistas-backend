namespace Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos
{
    /// <summary>
    /// Correos SSOMA de un proyecto que se editan en Configuración → Proyectos.
    ///
    /// El residente ya no es un correo escrito a mano sino una referencia al trabajador
    /// (<see cref="ResidenteWorkersId"/>): su correo se lee de
    /// <c>workers.email_corporativo</c> al enviar, así no queda una segunda copia que se
    /// desactualiza cuando la persona cambia de correo.
    /// </summary>
    public class ProjectEmailsUpdateDto
    {
        /// <summary>
        /// Trabajador que es residente del proyecto, o null para dejarlo sin residente.
        /// A diferencia de los correos de texto, acá null SÍ limpia el valor: el
        /// formulario siempre manda el objeto completo.
        /// </summary>
        public int? ResidenteWorkersId { get; set; }

        public string? EmailResponsable { get; set; }
        public string? EmailRrhh { get; set; }
        public string? EmailCoordSsoma { get; set; }
        public string? EmailCoordAdmin { get; set; }
    }

    /// <summary>Un trabajador elegible como residente, para el desplegable del formulario.</summary>
    public class ResidenteOptionDto
    {
        public int WorkerId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta única del formulario de correos del proyecto: los valores actuales, el
    /// correo del residente ya resuelto (solo lectura) y los trabajadores elegibles, en
    /// una sola petición.
    /// </summary>
    public class ProjectEmailsDto
    {
        public int? ResidenteWorkersId { get; set; }
        /// <summary>Nombre del residente actual, para mostrarlo sin tener que buscarlo en la lista.</summary>
        public string? ResidenteNombre { get; set; }
        /// <summary>Correo corporativo del residente actual — el que realmente se va a usar.</summary>
        public string? ResidenteEmail { get; set; }

        public string? EmailResponsable { get; set; }
        public string? EmailRrhh { get; set; }
        public string? EmailCoordSsoma { get; set; }
        public string? EmailCoordAdmin { get; set; }

        public List<ResidenteOptionDto> Residentes { get; set; } = new();
    }
}
