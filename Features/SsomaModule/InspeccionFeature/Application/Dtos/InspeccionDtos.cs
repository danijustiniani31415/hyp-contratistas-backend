namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Dtos;

public class InspeccionTipoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ambito { get; set; } = string.Empty;
    public bool EsColaborativa { get; set; }
}

/// <summary>
/// Vista previa de a quién le va a llegar el correo de cierre de una inspección colaborativa —
/// se resuelve con el mismo criterio que el envío real, para que el usuario confirme el cierre
/// sabiendo exactamente quién se va a enterar. Un campo en null significa que ese rol no tiene
/// correo cargado y no va a recibir el aviso.
/// </summary>
public class InspeccionDestinatarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class InspeccionDestinatariosCierreDto
{
    public string? ResidenteEmail { get; set; }
    public string? CoordSsomaEmail { get; set; }
    public string? GerenteInmobiliarioEmail { get; set; }
    /// <summary>Prevencionistas (rol PREVENCIONISTA) con vinculación activa al proyecto de la inspección — puede haber varios, uno por contratista.</summary>
    public List<InspeccionDestinatarioDto> Prevencionistas { get; set; } = new();
    /// <summary>Quienes participaron de la inspección (SsomaInspeccionParticipante con WorkerId resuelto a correo, más el inspector original) — van en copia, no en el "para".</summary>
    public List<InspeccionDestinatarioDto> Participantes { get; set; } = new();
    /// <summary>Puesto único "JEFE DE SEGURIDAD Y SALUD EN EL TRABAJO" — mismo criterio que GerenteInmobiliarioEmail. Va en copia, no en el "para".</summary>
    public string? JefeSsomaEmail { get; set; }
    public string? TuEmail { get; set; }
}

public class InspeccionChecklistItemDto
{
    public int Id { get; set; }
    public int TipoId { get; set; }
    public string Pregunta { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int Orden { get; set; }
}

public class InspeccionRespuestaRequest
{
    public int ItemId { get; set; }
    public string Resultado { get; set; } = "NA";
    public string? Observacion { get; set; }
}

public class InspeccionHallazgoRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Menor";
    public string? Area { get; set; }
    public string? ResponsableNombre { get; set; }
    public string? ResponsableCargo { get; set; }
    public DateTime? FechaLimite { get; set; }
    public string? AccionCorrectiva { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public List<string> FotosBase64 { get; set; } = [];
}

/// <summary>Edición de un hallazgo ya creado — mismos campos editables que al crearlo, sin
/// fotos (las fotos originales del hallazgo no se tocan acá). Solo permitido mientras el
/// hallazgo siga "Abierto" y la inspección siga "Abierta" — ver InspeccionRepository.EditarHallazgoAsync.</summary>
public class EditarHallazgoRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Menor";
    public string? Area { get; set; }
    public string? ResponsableNombre { get; set; }
    public string? ResponsableCargo { get; set; }
    public DateTime? FechaLimite { get; set; }
    public string? AccionCorrectiva { get; set; }
}

public class CrearInspeccionRequest
{
    public int ProyectoId { get; set; }
    public int TipoId { get; set; }
    public int? EmpresaId { get; set; }
    /// <summary>
    /// Empresa contratista que sube la inspección. Solo lo setea el controller desde el JWT
    /// (no viene del cliente) — ver InspeccionController.Crear.
    /// </summary>
    public int? EmpresaInspectoraId { get; set; }
    public bool EsPlanificada { get; set; } = true;
    public DateTime Fecha { get; set; }
    public string? HoraInicio { get; set; }
    public string? HoraFin { get; set; }
    public string? Area { get; set; }
    public string? ResponsableArea { get; set; }
    /// <summary>
    /// Worker que hace la inspección. El formulario ya lo resuelve del usuario logueado; si no
    /// llega, el repositorio lo deduce del JWT. Es lo que permite atribuir la inspección en
    /// Desempeño Supervisor sin depender del texto del nombre.
    /// </summary>
    public int? InspectorWorkerId { get; set; }
    public string? InspectorNombre { get; set; }
    public string? InspectorCargo { get; set; }
    public string? InspectorEmpresa { get; set; }
    public string? FirmaInspectorBase64 { get; set; }
    public string? RepresentanteNombre { get; set; }
    public string? RepresentanteCargo { get; set; }
    public string? FirmaRepresentanteBase64 { get; set; }
    public string? DescripcionCausas { get; set; }
    public string? Conclusiones { get; set; }
    public bool EsColaborativa { get; set; } = false;
    public List<InspeccionRespuestaRequest> Respuestas { get; set; } = [];
    public List<InspeccionHallazgoRequest> Hallazgos { get; set; } = [];
    public List<string> FotosAreaBase64 { get; set; } = [];
}

public class UnirseInspeccionRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? Empresa { get; set; }
}

public class ParticipanteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? Empresa { get; set; }
    public DateTime FechaUnion { get; set; }
}

public class InspeccionAbiertaListItemDto
{
    public int Id { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public string TipoNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int TotalHallazgos { get; set; }
    public int TotalParticipantes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CerrarHallazgoRequest
{
    public string AccionCorrectiva { get; set; } = string.Empty;
    public string? EvidenciaCierreBase64 { get; set; }
}

public class InspeccionHallazgoFotoDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
}

public class InspeccionHallazgoDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? ResponsableNombre { get; set; }
    public string? ResponsableCargo { get; set; }
    public DateTime? FechaLimite { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? AccionCorrectiva { get; set; }
    public string? EvidenciaCierreUrl { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? CreadoPorNombre { get; set; }
    public List<InspeccionHallazgoFotoDto> Fotos { get; set; } = [];
}

public class InspeccionRespuestaDto
{
    public int ItemId { get; set; }
    public string Pregunta { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int Orden { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public string? Observacion { get; set; }
}

public class InspeccionDetalleDto
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public int TipoId { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string TipoAmbito { get; set; } = string.Empty;
    public int? EmpresaId { get; set; }
    public string? EmpresaNombre { get; set; }
    public int? EmpresaInspectoraId { get; set; }
    public bool EsPlanificada { get; set; }
    public DateTime Fecha { get; set; }
    public string? HoraInicio { get; set; }
    public string? HoraFin { get; set; }
    public string? Area { get; set; }
    public string? ResponsableArea { get; set; }
    public string? InspectorNombre { get; set; }
    public string? InspectorCargo { get; set; }
    public string? InspectorEmpresa { get; set; }
    public string? FirmaInspectorUrl { get; set; }
    public string? RepresentanteNombre { get; set; }
    public string? RepresentanteCargo { get; set; }
    public string? FirmaRepresentanteUrl { get; set; }
    public string? DescripcionCausas { get; set; }
    public string? Conclusiones { get; set; }
    public int TotalItems { get; set; }
    public int TotalCumple { get; set; }
    public int TotalNoCumple { get; set; }
    public int TotalNa { get; set; }
    public decimal? TasaCumplimiento { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EsColaborativa { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InspeccionRespuestaDto> Respuestas { get; set; } = [];
    public List<InspeccionHallazgoDto> Hallazgos { get; set; } = [];
    public List<InspeccionHallazgoFotoDto> FotosArea { get; set; } = [];
    public List<ParticipanteDto> Participantes { get; set; } = [];
}

public class InspeccionListItemDto
{
    public int Id { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public string TipoNombre { get; set; } = string.Empty;
    public string TipoAmbito { get; set; } = string.Empty;
    public string? EmpresaNombre { get; set; }
    public bool EsPlanificada { get; set; }
    public DateTime Fecha { get; set; }
    public string? Area { get; set; }
    public string? InspectorNombre { get; set; }
    public int TotalHallazgos { get; set; }
    public int HallazgosCriticos { get; set; }
    public int HallazgosAbiertos { get; set; }
    public decimal? TasaCumplimiento { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class InspeccionDashboardDto
{
    public int TotalInspecciones { get; set; }
    public int TotalEsteMes { get; set; }
    public int HallazgosAbiertos { get; set; }
    public int HallazgosCriticosAbiertos { get; set; }
    public decimal? TasaCumplimientoPromedio { get; set; }
    public decimal? TasaCumplimientoEsteMes { get; set; }
    public List<InspeccionTendenciaMensualDto> TendenciaMensual { get; set; } = [];
    public List<InspeccionPorTipoDto> PorTipo { get; set; } = [];
    public List<InspeccionHallazgoPorAreaDto> HallazgosPorArea { get; set; } = [];
    public List<InspeccionHallazgoRecurrenteDto> HallazgosRecurrentes { get; set; } = [];
}

public class InspeccionTendenciaMensualDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string MesNombre { get; set; } = string.Empty;
    public int Total { get; set; }
    public decimal? TasaPromedio { get; set; }
}

public class InspeccionPorTipoDto
{
    public string TipoNombre { get; set; } = string.Empty;
    public string Ambito { get; set; } = string.Empty;
    public int Total { get; set; }
    public decimal? TasaPromedio { get; set; }
}

public class InspeccionHallazgoPorAreaDto
{
    public string Area { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Criticos { get; set; }
    public int Abiertos { get; set; }
}

public class InspeccionHallazgoRecurrenteDto
{
    public string Descripcion { get; set; } = string.Empty;
    public int Ocurrencias { get; set; }
    public string UltimoTipo { get; set; } = string.Empty;
}

public class HallazgoListItemDto
{
    public int Id { get; set; }
    public int InspeccionId { get; set; }
    public string? Proyecto { get; set; }
    public DateTime? FechaInspeccion { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? Area { get; set; }
    public string? ResponsableNombre { get; set; }
    public string? ResponsableCargo { get; set; }
    public DateTime? FechaLimite { get; set; }
    public string? AccionCorrectiva { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaCierre { get; set; }
    public List<string> FotosUrls { get; set; } = [];
}

public class LevantarHallazgoDto
{
    public string Estado { get; set; } = string.Empty;
    public string? EvidenciaUrl { get; set; }
    public string? EvidenciaNombre { get; set; }
}
