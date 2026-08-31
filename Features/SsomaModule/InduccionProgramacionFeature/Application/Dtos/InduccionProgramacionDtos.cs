namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Dtos
{
    public class ProyectoSimpleInduccionDto
    {
        public int ProyectoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    /// <summary>Un turno (proyecto + responsable opcional) dentro de la cola de rotación.</summary>
    public class RotacionProyectoDto
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public int? ResponsableWorkerId { get; set; }
        public string? ResponsableNombre { get; set; }
    }

    public class RotacionAgregarDto
    {
        public int ProyectoId { get; set; }
        public int? ResponsableWorkerId { get; set; }
    }

    public class RotacionResponsableDto
    {
        public int? ResponsableWorkerId { get; set; }
    }

    /// <summary>Coordinador SSOMA o Prevencionista con vínculo activo en un proyecto — candidato
    /// a responsable de un turno de inducción.</summary>
    public class ResponsableProyectoDto
    {
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class RotacionReordenarItemDto
    {
        public int Id { get; set; }
        public int Orden { get; set; }
    }

    public class RotacionReordenarDto
    {
        public List<RotacionReordenarItemDto> Items { get; set; } = new();
    }

    public class RotacionActivoDto
    {
        public bool Activo { get; set; }
    }

    /// <summary>Una fecha ya generada (o manualmente creada) del calendario de inducciones.</summary>
    public class ProgramacionInduccionDto
    {
        public int Id { get; set; }
        public DateOnly Fecha { get; set; }
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public int? ResponsableWorkerId { get; set; }
        public string? ResponsableNombre { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool EsManual { get; set; }
        public string? MotivoCambio { get; set; }
        public bool AvisoEnviado { get; set; }
    }

    public class ProgramacionReasignarDto
    {
        public int ProyectoId { get; set; }
        public string? Motivo { get; set; }
    }

    public class ProgramacionResponsableDto
    {
        public int? ResponsableWorkerId { get; set; }
    }

    public class ProgramacionCancelarDto
    {
        public string? Motivo { get; set; }
    }

    public class ProgramacionReprogramarDto
    {
        public DateOnly NuevaFecha { get; set; }
        public string? Motivo { get; set; }
    }

    /// <summary>Resultado del endpoint de cron que envía los avisos.</summary>
    public class AvisoInduccionResultDto
    {
        public int Enviados { get; set; }
        public int Errores { get; set; }
        public List<string> Detalles { get; set; } = new();
    }
}
