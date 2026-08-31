namespace Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Dtos
{
    /// <summary>
    /// Carga inicial de la funcionalidad "Delegación de Revisión" (usuario final).
    /// Lista las asignaciones (área, o área+proyecto) en las que el usuario logueado figura
    /// como revisor vivo en area_revisores, para que pueda designar suplentes de su área y
    /// activarse/desactivarse ("tomar/soltar el puesto").
    /// </summary>
    public class DelegacionInicialDto
    {
        /// <summary>workers.id del usuario logueado (0 si el usuario no tiene worker).</summary>
        public int CurrentWorkerId { get; set; }
        public List<DelegacionAsignacionItemDto> Asignaciones { get; set; } = new();
    }

    /// <summary>
    /// Una asignación que el usuario administra: un área (ProjectId null) o un proyecto dentro de
    /// un área "filtrada por proyecto" (ProjectId con valor), con su lista de revisores y las
    /// opciones de trabajadores que puede designar (los que pertenecen a esa área/proyecto).
    /// </summary>
    public class DelegacionAsignacionItemDto
    {
        public int AreaScopeId { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public string? ParentName { get; set; }
        /// <summary>null = asignación a nivel de área; con valor = asignación de ese proyecto.</summary>
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public List<DelegacionRevisorAsignadoDto> Revisores { get; set; } = new();
        /// <summary>Trabajadores designables (pertenecen al área/subárbol o al proyecto), con @abril.pe.</summary>
        public List<DelegacionOptionDto> Options { get; set; } = new();
    }

    /// <summary>Un revisor asignado (fila viva de area_revisores) mostrado en la delegación.</summary>
    public class DelegacionRevisorAsignadoDto
    {
        public int Id { get; set; }
        public int RevisorWorkerId { get; set; }
        public string? RevisorFullName { get; set; }
        public string? RevisorEmail { get; set; }
        public string? RevisorCategory { get; set; }
        public int OrdenPrioridad { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>Opción del selector: worker con correo corporativo @abril.pe.</summary>
    public class DelegacionOptionDto
    {
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>Cuerpo del PUT: reemplaza los revisores de una asignación (área o área+proyecto).</summary>
    public class DelegacionUpdateDto
    {
        public int? ProjectId { get; set; }
        public List<DelegacionAsignacionDto> Revisores { get; set; } = new();
    }

    /// <summary>Una asignación de revisor dentro del PUT.</summary>
    public class DelegacionAsignacionDto
    {
        public int RevisorWorkerId { get; set; }
        public int OrdenPrioridad { get; set; }
        public bool Active { get; set; } = true;
    }
}
