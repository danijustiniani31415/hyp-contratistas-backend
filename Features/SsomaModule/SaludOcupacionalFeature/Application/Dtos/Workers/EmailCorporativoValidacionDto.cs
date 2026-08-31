namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers
{
    /// <summary>Motivo por el que un correo corporativo no puede usarse.</summary>
    public static class EmailCorporativoMotivo
    {
        public const string Obligatorio = "OBLIGATORIO";
        public const string Formato = "FORMATO";
        public const string NoExisteEnTenant = "NO_EXISTE_EN_TENANT";
        public const string YaTomado = "YA_TOMADO";
    }

    /// <summary>
    /// Resultado de validar un correo corporativo contra el directorio de Abril (tenant de
    /// Microsoft) y contra los correos ya asignados en <c>workers.email_corporativo</c>.
    /// Se usa tanto para la verificación previa desde el formulario (endpoint GET) como
    /// internamente al crear/editar un trabajador.
    /// </summary>
    public class EmailCorporativoValidacionDto
    {
        /// <summary>True si el correo puede guardarse tal cual.</summary>
        public bool Valido { get; set; }

        /// <summary>Ver <see cref="EmailCorporativoMotivo"/>. Null cuando <see cref="Valido"/> es true.</summary>
        public string? Motivo { get; set; }

        /// <summary>Mensaje listo para mostrar al usuario. Null cuando el correo es válido.</summary>
        public string? Mensaje { get; set; }

        /// <summary>
        /// Correo canónico a persistir (minúsculas y sin espacios; si existe en el tenant, el
        /// que devuelve Graph). Null cuando el campo se envió vacío.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>Nombre para mostrar del buzón en el directorio de Abril, si se validó contra el tenant.</summary>
        public string? NombreEnTenant { get; set; }

        /// <summary>True si el correo se contrastó contra el tenant (solo aplica a correos corporativos).</summary>
        public bool VerificadoEnTenant { get; set; }

        /// <summary>Trabajador que ya tiene ese correo asignado, cuando el motivo es YA_TOMADO.</summary>
        public int? OcupadoPorWorkerId { get; set; }

        /// <summary>Nombre del trabajador que ya tiene ese correo asignado, cuando el motivo es YA_TOMADO.</summary>
        public string? OcupadoPorNombre { get; set; }
    }
}
