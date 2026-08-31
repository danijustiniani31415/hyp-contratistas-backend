namespace Abril_Backend.Features.Ssoma.Rac.Dtos;

public class RacListQuery
{
    public int? ProyectoId { get; set; }
    public string? Estado { get; set; }
    public string? Severidad { get; set; }
    public string? Tipo { get; set; }
    public int? EmpresaReportadaId { get; set; }
    public int? EmpresaReportanteId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public bool? SoloConPenalidad { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Solo lo setea el controller (no viene del cliente): empresa del contratista
    /// logueado. A diferencia de EmpresaReportadaId/EmpresaReportanteId (filtros AND
    /// independientes para el admin), este se aplica como OR — un contratista debe
    /// ver los RAC donde participa en cualquiera de los dos roles.
    /// </summary>
    public int? EmpresaIdContratista { get; set; }
}

public class RacPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public class RacCreateRequest
{
    public int ProyectoId { get; set; }
    public string Tipo { get; set; } = "";
    public int CategoriaId { get; set; }
    public string Severidad { get; set; } = "";
    public bool EsAnonimoReportante { get; set; }
    public int? ReportanteId { get; set; }
    public string? ReportanteNombre { get; set; }
    public string? ReportanteCargo { get; set; }
    public int? EmpresaReportanteId { get; set; }
    public bool EsAnonimoObservado { get; set; }
    public int? ObservadoWorkerId { get; set; }
    public int? EmpresaReportadaId { get; set; }
    public string? ProyectoPiso { get; set; }
    public string? LugarDescripcion { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string Descripcion { get; set; } = "";
    public string? PlanAccion { get; set; }
    public DateTime FechaReporte { get; set; }
    public DateTime? PlazoLevantamiento { get; set; }
    public bool AplicaPenalidad { get; set; }
    public int? InfraccionId { get; set; }
    public string? DescripcionOcurrido { get; set; }
}

public class RacCerrarRequest
{
    public string CierreDescripcion { get; set; } = "";
    public string? FotoCierreUrl { get; set; }
}

public class RacFotoDto
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? NombreArchivo { get; set; }
    public int Orden { get; set; }
}

public class RacPenalidadResumenDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Estado { get; set; } = "";
    public decimal MontoCalculado { get; set; }
    public string? InfraccionNombre { get; set; }
}

public class RacListItemDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string? ProyectoNombre { get; set; }
    public string? ProyectoAbreviacion { get; set; }
    public string Tipo { get; set; } = "";
    public string CategoriaNombre { get; set; } = "";
    public string CategoriaAmbito { get; set; } = "";
    public string Severidad { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime FechaReporte { get; set; }
    public DateTime? PlazoLevantamiento { get; set; }
    public bool AplicaPenalidad { get; set; }
    public string? EmpresaReportadaNombre { get; set; }
    public string? EmpresaReportanteNombre { get; set; }
    public string? ReportanteNombre { get; set; }
    public string Descripcion { get; set; } = "";
}

public class RacDetalleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public int? ProyectoId { get; set; }
    public string? ProyectoNombre { get; set; }
    public string Tipo { get; set; } = "";
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = "";
    public string CategoriaAmbito { get; set; } = "";
    public string Severidad { get; set; } = "";
    public bool EsAnonimoReportante { get; set; }
    public int? ReportanteId { get; set; }
    public string? ReportanteNombre { get; set; }
    public string? ReportanteCargo { get; set; }
    public int? EmpresaReportanteId { get; set; }
    public string? EmpresaReportanteNombre { get; set; }
    public bool EsAnonimoObservado { get; set; }
    public int? ObservadoWorkerId { get; set; }
    public string? ObservadoNombre { get; set; }
    public int? EmpresaReportadaId { get; set; }
    public string? EmpresaReportadaNombre { get; set; }
    public string? ProyectoPiso { get; set; }
    public string? LugarDescripcion { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string Descripcion { get; set; } = "";
    public string? PlanAccion { get; set; }
    public DateTime FechaReporte { get; set; }
    public DateTime? PlazoLevantamiento { get; set; }
    public string Estado { get; set; } = "";
    public DateTime? FechaCierre { get; set; }
    public string? CierreDescripcion { get; set; }
    public string? CerradoPorNombre { get; set; }
    public string? CerradoPorCargo { get; set; }
    public bool AplicaPenalidad { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RacFotoDto> Fotos { get; set; } = new();
    public RacPenalidadResumenDto? Penalidad { get; set; }
}

public class RacDashboardDto
{
    public int TotalAbiertos { get; set; }
    public int TotalCerrados { get; set; }
    public int TotalConPenalidad { get; set; }
    public int CriticosAbiertos { get; set; }
    public int AltosAbiertos { get; set; }
    public int VencidosAbiertos { get; set; }
    // RACs donde esta empresa es la que reportó/levantó el hallazgo (no la reportada).
    public int TotalReportados { get; set; }
    public int TotalReportadosCerrados { get; set; }
    public List<RacPorProyectoDto> PorProyecto { get; set; } = new();
    public List<RacPorCategoriaDto> PorCategoria { get; set; } = new();
    public List<RacTendenciaDto> Tendencia { get; set; } = new();
}

public class RacPorProyectoDto
{
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = "";
    public int Total { get; set; }
    public int Abiertos { get; set; }
    public int Cerrados { get; set; }
}

public class RacPorCategoriaDto
{
    public string CategoriaNombre { get; set; } = "";
    public string Ambito { get; set; } = "";
    public int Total { get; set; }
}

public class RacTendenciaDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int Total { get; set; }
    public int Actos { get; set; }
    public int Condiciones { get; set; }
}

public class RacCategoriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? Ambito { get; set; }
    public int Orden { get; set; }
}

public class RacInfraccionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? MontoFijo { get; set; }
    public decimal? FactorUit { get; set; }
}

public class RacCreadoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public int? PenalidadId { get; set; }
    public string? PenalidadCodigo { get; set; }
}

public class RacFotoUploadResult
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string NombreArchivo { get; set; } = "";
}
