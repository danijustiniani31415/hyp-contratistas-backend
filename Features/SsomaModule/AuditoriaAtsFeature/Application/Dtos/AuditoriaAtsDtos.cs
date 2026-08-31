namespace Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Dtos;

public class AuditoriaAtsPreguntaDto
{
    public int Id { get; set; }
    public int Orden { get; set; }
    public string Texto { get; set; } = string.Empty;
}

public class AuditoriaAtsRespuestaItemRequest
{
    public int PreguntaId { get; set; }
    public short Puntaje { get; set; }
    public string? Comentario { get; set; }
}

public class CrearAuditoriaAtsRequest
{
    public DateOnly Fecha { get; set; }
    public int AuditorWorkerId { get; set; }
    public int AuditadoWorkerId { get; set; }
    public int? ProyectoId { get; set; }
    public string? EmailAuditado { get; set; }
    public string? Actividad { get; set; }
    public string? Lugar { get; set; }
    public string? Observaciones { get; set; }
    public List<string> FotosBase64 { get; set; } = [];
    public List<AuditoriaAtsRespuestaItemRequest> Respuestas { get; set; } = [];
}

public class AuditoriaAtsRespuestaDto
{
    public int PreguntaId { get; set; }
    public string PreguntaTexto { get; set; } = string.Empty;
    public int Puntaje { get; set; }
    public string? Comentario { get; set; }
}

public class AuditoriaAtsDetalleDto
{
    public int Id { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public int AuditorWorkerId { get; set; }
    public string AuditorNombre { get; set; } = string.Empty;
    public string? AuditorEmpresaNombre { get; set; }
    public string? AuditorCategoria { get; set; }
    public string? AuditorOcupacion { get; set; }
    public int AuditadoWorkerId { get; set; }
    public string AuditadoNombre { get; set; } = string.Empty;
    public string? AuditadoEmpresaNombre { get; set; }
    public string? AuditadoCategoria { get; set; }
    public string? AuditadoOcupacion { get; set; }
    public int? EmpresaId { get; set; }
    /// <summary>Empresa del auditor (puede ser distinta a EmpresaId, la del auditado).</summary>
    public int? EmpresaAuditorId { get; set; }
    public int? ProyectoId { get; set; }
    public string? ProyectoNombre { get; set; }
    public string? EmailAuditado { get; set; }
    public string? Actividad { get; set; }
    public string? Lugar { get; set; }
    public decimal? PuntajePromedio { get; set; }
    public string? Nivel { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public List<AuditoriaAtsRespuestaDto> Respuestas { get; set; } = [];
    public List<string> Fotos { get; set; } = [];
}

public class AuditoriaAtsListItemDto
{
    public int Id { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public string AuditorNombre { get; set; } = string.Empty;
    public string? AuditorEmpresaNombre { get; set; }
    public string? AuditorCategoria { get; set; }
    public string? AuditorOcupacion { get; set; }
    public string AuditadoNombre { get; set; } = string.Empty;
    public string? AuditadoEmpresaNombre { get; set; }
    public string? AuditadoCategoria { get; set; }
    public string? AuditadoOcupacion { get; set; }
    public string? ProyectoNombre { get; set; }
    public string? Actividad { get; set; }
    public string? Lugar { get; set; }
    public decimal? PuntajePromedio { get; set; }
    public string? Nivel { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
