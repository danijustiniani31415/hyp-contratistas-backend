namespace Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Application.Dtos
{
    /// <summary>
    /// Una fila por área de tipo "Área de Gerencia" o "Área Estándar" que sea el primer
    /// nodo de su mismo tipo en su rama del árbol area_scope (si un Área Estándar tiene
    /// como hijo otro Área Estándar, el hijo no se lista), junto a sus n revisores
    /// (area_revisores) ordenados por prioridad. Los revisores del área aplican a los
    /// trabajadores cuyo workers.area_scope_id cae en el subárbol del nodo, cuando el
    /// trabajador no tiene revisores propios en workers_revisores.
    /// </summary>
    public class AreaRevisorItemDto
    {
        public int AreaScopeId { get; set; }
        public string AreaName { get; set; } = string.Empty;
        /// <summary>Tipo del nodo: "Área de Gerencia" o "Área Estándar".</summary>
        public string AreaTypeName { get; set; } = string.Empty;
        /// <summary>Nombre del área padre (normalmente la gerencia). null = nodo raíz.</summary>
        public string? ParentName { get; set; }
        /// <summary>Revisores vivos del área (a nivel de área, project_id NULL), ordenados por prioridad.</summary>
        public List<AreaRevisorAsignadoDto> Revisores { get; set; } = new();

        /// <summary>
        /// Si true, el área se "subdivide por proyecto" (ga_salidas_area_config.filtra_por_proyecto):
        /// los revisores se asignan por proyecto y se muestran subfilas por proyecto.
        /// </summary>
        public bool FiltraPorProyecto { get; set; }

        /// <summary>
        /// Solo cuando <see cref="FiltraPorProyecto"/> es true: los proyectos del área que YA tienen
        /// al menos un revisor asignado, con sus revisores. El frontend renderiza una subfila por
        /// cada proyecto de la lista global <see cref="AreaRevisorInicialDto.Proyectos"/>, tomando de
        /// aquí los revisores del proyecto (vacío si aún no se asignó ninguno).
        /// </summary>
        public List<AreaProyectoRevisoresDto> Proyectos { get; set; } = new();
    }

    /// <summary>Revisores de un proyecto específico dentro de un área "filtrada por proyecto".</summary>
    public class AreaProyectoRevisoresDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        /// <summary>Revisores vivos del proyecto (area_revisores con ese project_id), por prioridad.</summary>
        public List<AreaRevisorAsignadoDto> Revisores { get; set; } = new();
    }

    /// <summary>Un revisor asignado a un área (fila viva de area_revisores).</summary>
    public class AreaRevisorAsignadoDto
    {
        /// <summary>area_revisores_id.</summary>
        public int Id { get; set; }
        public int RevisorWorkerId { get; set; }
        public string? RevisorFullName { get; set; }
        public string? RevisorEmail { get; set; }
        public string? RevisorCategory { get; set; }
        /// <summary>1 = primero en ser considerado; a mayor número, menor prioridad.</summary>
        public int OrdenPrioridad { get; set; }
        /// <summary>false = no se considera (ej. ausencia temporal del revisor).</summary>
        public bool Active { get; set; }
    }

    /// <summary>Opción del selector de revisor: worker con correo corporativo @abril.pe.</summary>
    public class AreaRevisorOptionDto
    {
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>Opción del selector/subfila de proyecto.</summary>
    public class ProyectoOptionDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
    }

    /// <summary>Carga inicial de la página: áreas configurables con sus revisores + opciones del selector.</summary>
    public class AreaRevisorInicialDto
    {
        public List<AreaRevisorItemDto> Areas { get; set; } = new();
        public List<AreaRevisorOptionDto> Options { get; set; } = new();
        /// <summary>Todos los proyectos activos, para armar las subfilas y el selector de proyecto.</summary>
        public List<ProyectoOptionDto> Proyectos { get; set; } = new();
    }

    /// <summary>
    /// Cuerpo del PUT: reemplaza el conjunto completo de revisores del área (si <see cref="ProjectId"/>
    /// es null) o del proyecto dentro del área (si tiene valor).
    /// </summary>
    public class AreaRevisoresUpdateDto
    {
        /// <summary>null = revisores a nivel de área; con valor = revisores de ese proyecto dentro del área.</summary>
        public int? ProjectId { get; set; }
        public List<AreaRevisorAsignacionDto> Revisores { get; set; } = new();
    }

    /// <summary>Una asignación de revisor dentro del PUT.</summary>
    public class AreaRevisorAsignacionDto
    {
        public int RevisorWorkerId { get; set; }
        public int OrdenPrioridad { get; set; }
        public bool Active { get; set; } = true;
    }

    /// <summary>Cuerpo del PUT de flag: marca/desmarca "filtrar por proyecto" para el área.</summary>
    public class AreaFiltroProyectoUpdateDto
    {
        public bool FiltraPorProyecto { get; set; }
    }
}
