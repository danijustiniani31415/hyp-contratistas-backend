namespace Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Application.Dtos
{
    public class CatalogOptionDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
    }

    public class ProjectOptionDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
    }

    /// <summary>Un tipo de licencia dentro de la plantilla de un proyecto (base o propio).</summary>
    public class VecinoLicenciaTipoDto
    {
        public int VecinoLicenciaControlTipoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public int Orden { get; set; }
        /// <summary>true = viene de la plantilla base (compartida); false = agregado solo para este proyecto.</summary>
        public bool EsBase { get; set; }
        public int? DiasAntesDefault { get; set; }
    }

    /// <summary>Alta o edición de un tipo del catálogo base (visible en todos los proyectos).</summary>
    public class VecinoLicenciaTipoBaseUpsertDto
    {
        public string Descripcion { get; set; } = null!;
        public int? DiasAntesDefault { get; set; }
    }

    /// <summary>Estado de un tipo de licencia para un proyecto puntual (fila de la tabla del control).</summary>
    public class VecinoLicenciaItemDto
    {
        /// <summary>Id del registro vigente (null si el proyecto aún no tiene fila para este tipo).</summary>
        public int? VecinoLicenciaControlId { get; set; }
        public int VecinoLicenciaControlTipoId { get; set; }
        public string TipoDescripcion { get; set; } = null!;
        public int Orden { get; set; }
        public bool EsBase { get; set; }

        public int VecinoLicenciaControlEstadoId { get; set; }
        public string EstadoDescripcion { get; set; } = null!;

        public string? ArchivoUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        /// <summary>Días de antelación por defecto del tipo, para sugerir el primer recordatorio al subir.</summary>
        public int? DiasAntesDefault { get; set; }

        /// <summary>Recordatorios activos de la licencia vigente (puede haber varios, ej. 30/15/7/2 días antes).</summary>
        public List<VecinoLicenciaRecordatorioDto> Recordatorios { get; set; } = new();

        /// <summary>Cantidad de versiones anteriores archivadas en el historial.</summary>
        public int VersionesHistorial { get; set; }
    }

    /// <summary>Un recordatorio (N días antes de vencer) de una licencia.</summary>
    public class VecinoLicenciaRecordatorioDto
    {
        public int VecinoLicenciaControlRecordatorioId { get; set; }
        public int DiasAntes { get; set; }
        public DateOnly FechaRecordatorio { get; set; }
        public bool Enviado { get; set; }
    }

    /// <summary>Agrega un recordatorio adicional a la licencia vigente de un tipo.</summary>
    public class VecinoLicenciaRecordatorioCreateDto
    {
        public int DiasAntes { get; set; }
    }

    public class VecinoLicenciaPlantillaResponseDto
    {
        public List<VecinoLicenciaItemDto> Items { get; set; } = new();
        public List<CatalogOptionDto> Estados { get; set; } = new();
    }

    /// <summary>Datos del formulario al subir/reemplazar el documento de un tipo de licencia (el archivo va aparte).</summary>
    public class VecinoLicenciaUploadDto
    {
        public DateOnly FechaVencimiento { get; set; }
        /// <summary>Días de antelación de cada recordatorio a crear (ej. [30, 15, 7, 2]). Al menos uno.</summary>
        public List<int> DiasAntesRecordatorio { get; set; } = new();
    }

    public class VecinoLicenciaNoAplicaDto
    {
        public bool NoAplica { get; set; }
    }

    /// <summary>Agrega un tipo de licencia propio para un proyecto (no afecta la plantilla base ni a otros proyectos).</summary>
    public class VecinoLicenciaTipoCreateDto
    {
        public string Descripcion { get; set; } = null!;
        public int? DiasAntesDefault { get; set; }
    }

    /// <summary>Una versión anterior archivada de una licencia.</summary>
    public class VecinoLicenciaHistorialItemDto
    {
        public int VecinoLicenciaControlHistorialId { get; set; }
        public string ArchivoUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public DateOnly? FechaRecordatorio { get; set; }
        public int? DiasAntes { get; set; }
        public string? Motivo { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string? CreatedUserName { get; set; }
    }

    /// <summary>Correo resuelto automáticamente desde la ficha del proyecto (Residente, Coordinador SSOMA, Administración) — mismo criterio que EMOs.</summary>
    public class VecinoLicenciaDestinatarioAutomaticoDto
    {
        public string Rol { get; set; } = null!;
        public string? Email { get; set; }
    }

    /// <summary>Correo adicional configurado a mano para un proyecto (ej. Jefe SSOMA cuando aplique).</summary>
    public class VecinoLicenciaDestinatarioDto
    {
        public int VecinoLicenciaControlDestinatarioId { get; set; }
        public string Rol { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    public class VecinoLicenciaDestinatarioUpsertDto
    {
        public string Rol { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    /// <summary>Destinatarios de un proyecto: automáticos (ficha del proyecto) + adicionales configurados a mano.</summary>
    public class VecinoLicenciaDestinatariosResponseDto
    {
        public List<VecinoLicenciaDestinatarioAutomaticoDto> Automaticos { get; set; } = new();
        public List<VecinoLicenciaDestinatarioDto> Adicionales { get; set; } = new();
    }

    /// <summary>Un recordatorio pendiente de envío, con el contexto necesario para armar el correo.</summary>
    public class VecinoLicenciaRecordatorioPendienteDto
    {
        public int VecinoLicenciaControlRecordatorioId { get; set; }
        public int ProjectId { get; set; }
        public string TipoDescripcion { get; set; } = null!;
        public DateOnly FechaVencimiento { get; set; }
        public int DiasAntes { get; set; }
    }

    /// <summary>Resultado del procesamiento del cron de recordatorios (todos los proyectos).</summary>
    public class RecordatoriosResultDto
    {
        public int LicenciasProcesadas { get; set; }
        public int CorreosEnviados { get; set; }
        public List<string> Errores { get; set; } = new();
    }
}
