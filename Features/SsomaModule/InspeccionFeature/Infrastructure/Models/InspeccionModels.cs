using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Infrastructure.Models;

public class SsomaInspeccionTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ambito { get; set; } = "Seguridad";
    public bool Activo { get; set; } = true;
    /// <summary>
    /// Gerencial / cruzada: sin checklist fijo, varios coordinadores agregan hallazgos sueltos
    /// al mismo registro mientras esté "Abierta". Ver SsomaInspeccion.EsColaborativa.
    /// </summary>
    public bool EsColaborativa { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SsomaInspeccionChecklistItem> Items { get; set; } = [];
}

public class SsomaInspeccionChecklistItem
{
    public int Id { get; set; }
    public int TipoId { get; set; }
    public string Pregunta { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public SsomaInspeccionTipo? Tipo { get; set; }
}

public class SsomaInspeccion
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int TipoId { get; set; }
    public int? EmpresaId { get; set; }
    /// <summary>
    /// Empresa contratista que subió/registró la inspección (puede ser distinta de EmpresaId,
    /// la empresa inspeccionada). Permite que un contratista vea inspecciones que él mismo
    /// levantó contra otra empresa, igual que EmpresaReportanteId en RAC.
    /// </summary>
    public int? EmpresaInspectoraId { get; set; }
    public bool EsPlanificada { get; set; } = true;
    public DateTime Fecha { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public string? Area { get; set; }
    public string? ResponsableArea { get; set; }
    /// <summary>
    /// Worker que hizo la inspección. Es la fuente de verdad para atribuirla a su supervisor en
    /// Desempeño Supervisor: <see cref="InspectorNombre"/> es solo el texto que se imprime en el
    /// PDF y, siendo una foto del nombre al momento de crear el registro, deja de calzar en
    /// cuanto alguien corrige el nombre en la ficha del trabajador (incidencia Corilla, ago-2026).
    /// </summary>
    public int? InspectorWorkerId { get; set; }
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
    public string Estado { get; set; } = "Borrador";
    /// <summary>
    /// Inspecciones gerenciales/cruzadas: varios coordinadores agregan hallazgos por separado,
    /// desde sus propios dispositivos, a un mismo registro mientras Estado == "Abierta".
    /// </summary>
    public bool EsColaborativa { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    public Project? Proyecto { get; set; }
    public SsomaInspeccionTipo? Tipo { get; set; }
    public Contributor? Empresa { get; set; }
    // Mismo patrón que SsomaOpt.Observador — la FK la genera EF por convención.
    public Worker? InspectorWorker { get; set; }
    public ICollection<SsomaInspeccionRespuesta> Respuestas { get; set; } = [];
    public ICollection<SsomaInspeccionHallazgo> Hallazgos { get; set; } = [];
    public ICollection<SsomaInspeccionFotoArea> FotosArea { get; set; } = [];
    public ICollection<SsomaInspeccionParticipante> Participantes { get; set; } = [];
}

public class SsomaInspeccionRespuesta
{
    public int Id { get; set; }
    public int InspeccionId { get; set; }
    public int ItemId { get; set; }
    public string Resultado { get; set; } = "NA";
    public string? Observacion { get; set; }

    public SsomaInspeccion? Inspeccion { get; set; }
    public SsomaInspeccionChecklistItem? Item { get; set; }
}

public class SsomaInspeccionHallazgo
{
    public int Id { get; set; }
    public int InspeccionId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Menor";
    public string? Area { get; set; }
    public string? ResponsableNombre { get; set; }
    public string? ResponsableCargo { get; set; }
    public DateTime? FechaLimite { get; set; }
    public string Estado { get; set; } = "Abierto";
    public string? AccionCorrectiva { get; set; }
    public string? EvidenciaCierreUrl { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    /// <summary>Quién levantó este hallazgo puntual — relevante en inspecciones colaborativas con varios participantes.</summary>
    public int? CreadoPorWorkerId { get; set; }
    public string? CreadoPorNombre { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SsomaInspeccion? Inspeccion { get; set; }
    public ICollection<SsomaInspeccionHallazgoFoto> Fotos { get; set; } = [];
}

public class SsomaInspeccionParticipante
{
    public int Id { get; set; }
    public int InspeccionId { get; set; }
    public int? WorkerId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? Empresa { get; set; }
    public DateTime FechaUnion { get; set; } = DateTime.UtcNow;

    public SsomaInspeccion? Inspeccion { get; set; }
}

public class SsomaInspeccionHallazgoFoto
{
    public int Id { get; set; }
    public int HallazgoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SsomaInspeccionHallazgo? Hallazgo { get; set; }
}

public class SsomaInspeccionFotoArea
{
    public int Id { get; set; }
    public int InspeccionId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SsomaInspeccion? Inspeccion { get; set; }
}
