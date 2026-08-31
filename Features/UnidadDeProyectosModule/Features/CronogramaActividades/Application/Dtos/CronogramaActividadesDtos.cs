namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Dtos
{
    public class ProyectoSimpleCronogramaDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = string.Empty;
        public string? ResponsableUdp { get; set; }
        public int TotalActividades { get; set; }
        public double AvanceAnteproyecto { get; set; }
        public double AvanceProyecto { get; set; }
        public double AvanceProyectoActualizacion { get; set; }
    }

    public class ActividadDto
    {
        public int ProjectActivityId { get; set; }
        public int ProjectId { get; set; }
        public string ActivityDescription { get; set; } = string.Empty;
        public DateOnly? PlannedStartDate { get; set; }
        public DateOnly? PlannedEndDate { get; set; }
        public DateOnly? ActualEndDate { get; set; }
        public DateOnly? BaselineStartDate { get; set; }
        public DateOnly? BaselineEndDate { get; set; }
        public int ProgressPercentage { get; set; }
        public int Order { get; set; }
        public int HierarchyLevel { get; set; }
        public int? ParentId { get; set; }
        /// <summary>Ids de las actividades predecesoras (Fin a Inicio). Solo hojas participan.</summary>
        public List<int> Predecesoras { get; set; } = new();
        /// <summary>True si la actividad tiene hijos (es nodo padre, fechas calculadas).</summary>
        public bool EsPadre { get; set; }
        public string TipoCronograma { get; set; } = "ANTEPROYECTO";
    }

    public class ActualizarLineaBaseRequest
    {
        public DateOnly? BaselineStartDate { get; set; }
        public DateOnly? BaselineEndDate { get; set; }
    }

    public class CrearActividadRequest
    {
        public string ActivityDescription { get; set; } = string.Empty;
        public DateOnly? PlannedStartDate { get; set; }
        public DateOnly? PlannedEndDate { get; set; }
        public int ProgressPercentage { get; set; } = 0;
        public int HierarchyLevel { get; set; } = 0;
        public int? ParentId { get; set; }
        public string TipoCronograma { get; set; } = "ANTEPROYECTO";
    }

    public class ReordenarItem
    {
        public int ProjectActivityId { get; set; }
        public int Order { get; set; }
    }

    public class CambiarJerarquiaRequest
    {
        public int ProjectActivityId { get; set; }
        public int NuevoHierarchyLevel { get; set; }
        public int? NuevoParentId { get; set; }
    }

    public class EditarActividadRequest
    {
        public string ActivityDescription { get; set; } = string.Empty;
        public DateOnly? PlannedStartDate { get; set; }
        public DateOnly? PlannedEndDate { get; set; }
        public DateOnly? ActualEndDate { get; set; }
        public int ProgressPercentage { get; set; }
        /// <summary>Si se incluye (no null), reemplaza por completo las predecesoras y aplica la cascada.</summary>
        public List<int>? PredecessorIds { get; set; }
    }

    public class CulminarActividadDto
    {
        public int ProjectActivityId { get; set; }
        public DateOnly? ActualEndDate { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class DebugProyectoDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = string.Empty;
        public bool TieneUnidadDeProyectos { get; set; }
        public bool State { get; set; }
    }

    public class ImportarMppResultDto
    {
        public int ActividadesImportadas { get; set; }
        public int ActividadesEliminadas { get; set; }
        public int ActividadesManualesConservadas { get; set; }
    }

    // ─────────────────────────── Feriados ───────────────────────────

    public class FeriadoDto
    {
        public int Id { get; set; }
        public DateOnly Fecha { get; set; }
        public string? Descripcion { get; set; }
    }

    public class CrearFeriadoRequest
    {
        public DateOnly Fecha { get; set; }
        public string? Descripcion { get; set; }
    }

    // ─────────────────────────── Predecesoras ───────────────────────────

    /// <summary>Reemplaza por completo el conjunto de predecesoras de una actividad.</summary>
    public class ActualizarPredecesorasRequest
    {
        public List<int> PredecessorIds { get; set; } = new();
    }

    /// <summary>
    /// Resultado de fijar predecesoras: la lista resultante + el preview de
    /// la cascada que se aplicaría (sin persistir todavía).
    /// </summary>
    public class ActualizarPredecesorasResultDto
    {
        public int ProjectActivityId { get; set; }
        public List<int> Predecesoras { get; set; } = new();
        public CascadaResultDto PreviewCascada { get; set; } = new();
    }

    // ─────────────────────────── Dashboard ───────────────────────────

    public class CronogramaDashboardKpisDto
    {
        public int TotalProyectos { get; set; }
        public int PorcentajeAvancePromedio { get; set; }
        public int ProyectosAlDia { get; set; }
        public int ProyectosConRetraso { get; set; }
        public int ProyectosSinActividades { get; set; }
        public int ActividadesVencidas { get; set; }
        public int ActividadesCulminadasEstaSemana { get; set; }
        public int ActividadesCulminadasEsteMes { get; set; }
    }

    public class CronogramaDashboardProyectoDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = string.Empty;
        public string? ResponsableUdp { get; set; }
        public int TotalActividades { get; set; }
        public int Culminadas { get; set; }
        public int EnProceso { get; set; }
        public int Vencidas { get; set; }
        public int Pendientes { get; set; }
        public int PorcentajeAvance { get; set; }
        public int DiasRetraso { get; set; }
        public string Semaforo { get; set; } = "VERDE";
        public string Estado { get; set; } = "AL_DIA";
        public decimal Spi { get; set; } = 1.0m;
    }

    public class CronogramaDashboardResponsableDto
    {
        public int UserId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
    }

    public class CronogramaDashboardResponseDto
    {
        public CronogramaDashboardKpisDto Kpis { get; set; } = new();
        public List<CronogramaDashboardProyectoDto> Proyectos { get; set; } = new();
        public List<CronogramaDashboardResponsableDto> Responsables { get; set; } = new();
    }

    // ─────────────────────────── GetActividades response ───────────────────────────

    /// <summary>Cabecera del proyecto en la vista de cronograma.</summary>
    public class ProyectoCronogramaHeaderDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = string.Empty;
        public string? ResponsableUdp { get; set; }
        public DateOnly? FechaInicio { get; set; }
    }

    /// <summary>Respuesta del GET /{proyectoId}/actividades.</summary>
    public class ActividadesProyectoResponseDto
    {
        public ProyectoCronogramaHeaderDto Proyecto { get; set; } = new();
        public List<ActividadDto> Actividades { get; set; } = new();
    }

    // ─────────────────────────── Crear / Editar actividad response ───────────────────────────

    /// <summary>Respuesta del POST /{proyectoId}/actividades.</summary>
    public class CrearActividadResultDto
    {
        public ActividadDto Actividad { get; set; } = new();
        /// <summary>Padres cuyas fechas (MIN/MAX) cambiaron al insertar la actividad. Null si ninguno cambió.</summary>
        public List<ActividadDto>? PadresActualizados { get; set; }
    }

    /// <summary>Respuesta del PUT /actividades/{id}.</summary>
    public class EditarActividadResultDto
    {
        public ActividadDto Actividad { get; set; } = new();
        /// <summary>Resultado de la cascada aplicada. Null si no se enviaron PredecessorIds y las fechas no dispararon cascada.</summary>
        public CascadaResultDto? Cascada { get; set; }
        /// <summary>Padres cuyas fechas (MIN/MAX) cambiaron al editar la actividad. Null si ninguno cambió.</summary>
        public List<ActividadDto>? PadresActualizados { get; set; }
    }

    // ─────────────────────────── Cascada ───────────────────────────

    /// <summary>Una actividad que se movería (o se movió) al recalcular la cascada.</summary>
    public class CascadaCambioDto
    {
        public int ProjectActivityId { get; set; }
        public string ActivityDescription { get; set; } = string.Empty;
        public DateOnly? InicioAnterior { get; set; }
        public DateOnly? InicioNuevo { get; set; }
        public DateOnly? FinAnterior { get; set; }
        public DateOnly? FinNuevo { get; set; }
        public DateOnly? BaselineStartDate { get; set; }
        public DateOnly? BaselineEndDate { get; set; }
    }

    public class CascadaResultDto
    {
        public bool HayCambios { get; set; }
        public List<CascadaCambioDto> Cambios { get; set; } = new();
    }

    // ─────────────────────────── Creación masiva ───────────────────────────

    public class CrearActividadMasivoItem
    {
        public string Nombre { get; set; } = string.Empty;
        public DateOnly? InicioProgramado { get; set; }
        public DateOnly? FinProgramado { get; set; }
        public string TipoCronograma { get; set; } = "ANTEPROYECTO";
    }

    public class CrearActividadesMasivoRequest
    {
        public List<CrearActividadMasivoItem> Actividades { get; set; } = new();
    }

    public class CrearActividadesMasivoResultDto
    {
        public int ActividadesCreadas { get; set; }
    }

    // ─────────────────────────── Última pestaña ───────────────────────────

    public class UltimaPestanaDto
    {
        public string? TipoCronograma { get; set; }
    }

    public class ActualizarUltimaPestanaRequest
    {
        public string TipoCronograma { get; set; } = string.Empty;
    }

    // ─────────────────────────── Plantilla ───────────────────────────

    public class AplicarPlantillaRequest
    {
        public string TipoCronograma { get; set; } = "PROYECTO";
    }

    public class AplicarPlantillaResultDto
    {
        public int ActividadesCreadas { get; set; }
    }
}
