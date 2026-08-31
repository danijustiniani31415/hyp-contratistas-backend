namespace Abril_Backend.Features.Ssoma.Paso.Entities;

public class SsomaPasoCategoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Ambito { get; set; } = "";
    public string? Icono { get; set; }
    public bool Activo { get; set; } = true;
}

public class SsomaPaso
{
    public int Id { get; set; }
    public int? ProyectoId { get; set; }
    public int? PlantillaId { get; set; }
    public string Nombre { get; set; } = "";
    public int? Anio { get; set; }
    public int MesInicio { get; set; } = 1;
    public bool EsPlantilla { get; set; } = false;
    public string Estado { get; set; } = "Borrador";
    public int? AprobadoPor { get; set; }
    public DateTime? AprobadoEn { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<SsomaPasoActividad> Actividades { get; set; } = new();
}

public class SsomaPasoActividad
{
    public int Id { get; set; }
    public int PasoId { get; set; }
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? Alcance { get; set; }
    public string Frecuencia { get; set; } = "Mensual";
    public int? ResponsableId { get; set; }
    public string? ResponsableTexto { get; set; }
    public int MesInicio { get; set; } = 1;
    public int MesFin { get; set; } = 12;
    public int CantidadPlanificada { get; set; } = 1;
    public decimal? Horas { get; set; }
    public string? Recursos { get; set; }
    public string Indicador { get; set; } = "N° Actividades Ejecutadas/N°Programadas*100";
    public string Meta { get; set; } = "100%";
    public int? Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public string? MotivoEliminacion { get; set; }
    public SsomaPaso Paso { get; set; } = null!;
    public SsomaPasoCategoria Categoria { get; set; } = null!;
    public List<SsomaPasoEjecucion> Ejecuciones { get; set; } = new();
}

public class SsomaPasoAuditoria
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "";
    public string Entidad { get; set; } = "";
    public int EntidadId { get; set; }
    public int PasoId { get; set; }
    public string? Descripcion { get; set; }
    public string? Motivo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SsomaPasoEjecucion
{
    public int Id { get; set; }
    public int ActividadId { get; set; }
    public DateOnly FechaProgramada { get; set; }
    public DateOnly? FechaVerificacion { get; set; }
    public DateOnly? FechaEjecutada { get; set; }
    public DateOnly? FechaReprogramada { get; set; }
    public string? MotivoReprogramacion { get; set; }
    public string Estado { get; set; } = "Programado";
    public string? Observaciones { get; set; }
    public int? ParticipantesCount { get; set; }
    public string? EvidenciaNombre { get; set; }
    public string? EvidenciaUrl { get; set; }
    public string? EvidenciaSpId { get; set; }
    public int? RegistradoPor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public SsomaPasoActividad Actividad { get; set; } = null!;
    public List<SsomaPasoEjecucionArchivo> Archivos { get; set; } = new();
}

public class SsomaPasoEjecucionArchivo
{
    public int Id { get; set; }
    public int EjecucionId { get; set; }
    public string ArchivoUrl { get; set; } = "";
    public string ArchivoNombre { get; set; } = "";
    public string? ArchivoSpId { get; set; }
    public int Orden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public SsomaPasoEjecucion Ejecucion { get; set; } = null!;
}
