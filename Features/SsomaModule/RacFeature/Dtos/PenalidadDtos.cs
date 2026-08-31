namespace Abril_Backend.Features.Ssoma.Rac.Dtos;

public class PenalidadListQuery
{
    public int? ProyectoId { get; set; }
    public int? EmpresaId { get; set; }
    public string? Estado { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PenalidadListItemDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string RacCodigo { get; set; } = "";
    public int RacId { get; set; }
    public string? ProyectoNombre { get; set; }
    public string? EmpresaNombre { get; set; }
    public string? InfraccionNombre { get; set; }
    public decimal MontoCalculado { get; set; }
    public string Estado { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? DescargoFecha { get; set; }
    public DateTime? ResueltaEn { get; set; }
    public string? ResolucionTipo { get; set; }
}

public class PenalidadDetalleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public int RacId { get; set; }
    public string RacCodigo { get; set; } = "";
    public int? EmpresaId { get; set; }
    public string? EmpresaNombre { get; set; }
    public int? ProyectoId { get; set; }
    public string? ProyectoNombre { get; set; }
    public int? InfraccionId { get; set; }
    public string? InfraccionNombre { get; set; }
    public decimal MontoCalculado { get; set; }
    public decimal UitReferencia { get; set; }
    public string? DescripcionOcurrido { get; set; }
    public string Estado { get; set; } = "";
    public string? DescargoTexto { get; set; }
    public string? DocumentoUrl { get; set; }
    public DateTime? DescargoFecha { get; set; }
    public string? ResolucionTexto { get; set; }
    public string? ResolucionTipo { get; set; }
    public DateTime? ResueltaEn { get; set; }
    public string? PdfResolucionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PenalidadDescargaRequest
{
    public string DescargoTexto { get; set; } = "";
    public string DocumentoUrl { get; set; } = "";
}

public class PenalidadResolverRequest
{
    public string ResolucionTipo { get; set; } = "";
    public string ResolucionTexto { get; set; } = "";
}
