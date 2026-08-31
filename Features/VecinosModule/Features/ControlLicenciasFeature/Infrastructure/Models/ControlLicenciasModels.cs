namespace Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Infrastructure.Models
{
    /// <summary>Catálogo de tipos de licencia: plantilla base (ProjectId null) o tipo propio de un proyecto.</summary>
    public class VecinoLicenciaControlTipo
    {
        public int VecinoLicenciaControlTipoId { get; set; }
        public int? ProjectId { get; set; }
        public string Descripcion { get; set; } = null!;
        public int Orden { get; set; }
        /// <summary>Días de antelación sugeridos por defecto al subir el documento (el usuario puede cambiarlos).</summary>
        public int? DiasAntesDefault { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>Catálogo fijo de estados: Pendiente, Cargado, No aplica, Vencido.</summary>
    public class VecinoLicenciaControlEstado
    {
        public int VecinoLicenciaControlEstadoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>Registro vigente de una licencia/permiso por proyecto + tipo.</summary>
    public class VecinoLicenciaControl
    {
        public int VecinoLicenciaControlId { get; set; }

        public int ProjectId { get; set; }

        public int VecinoLicenciaControlTipoId { get; set; }
        public VecinoLicenciaControlTipo? Tipo { get; set; }

        public int VecinoLicenciaControlEstadoId { get; set; }
        public VecinoLicenciaControlEstado? Estado { get; set; }

        public string? ArchivoUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public DateOnly? FechaVencimiento { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>
    /// Un recordatorio de una licencia (N días antes de vencer). Una licencia puede tener
    /// varios activos a la vez (ej. 30, 15, 7 y 2 días antes); cada uno se envía y se marca
    /// una sola vez, independiente de los demás.
    /// </summary>
    public class VecinoLicenciaControlRecordatorio
    {
        public int VecinoLicenciaControlRecordatorioId { get; set; }
        public int VecinoLicenciaControlId { get; set; }
        public int DiasAntes { get; set; }
        public DateOnly FechaRecordatorio { get; set; }
        public DateTime? EnviadoDateTime { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>Versión anterior de una licencia, archivada al reemplazar el archivo vigente.</summary>
    public class VecinoLicenciaControlHistorial
    {
        public int VecinoLicenciaControlHistorialId { get; set; }
        public int VecinoLicenciaControlId { get; set; }
        public string ArchivoUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public DateOnly? FechaRecordatorio { get; set; }
        public int? DiasAntes { get; set; }
        public string? Motivo { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>Correo destinatario de recordatorios por proyecto + rol (Residente, Administrador, etc.).</summary>
    public class VecinoLicenciaControlDestinatario
    {
        public int VecinoLicenciaControlDestinatarioId { get; set; }
        public int ProjectId { get; set; }
        public string Rol { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
