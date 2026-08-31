namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerHabilitacionListDto
    {
        public int WorkerId { get; set; }
        public string ApellidoNombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string? EmpresaNombre { get; set; }
        public int? EmpresaId { get; set; }
        public string? ProyectoActual { get; set; }
        public int? ProyectoActualId { get; set; }
        public string EstadoHabilitacion { get; set; } = string.Empty;
        /// <summary>Habilitación SSOMA de la empresa Contratista (ignora entregables
        /// administrativos) — siempre true para Casa/oficina central. Cuando es false y
        /// EstadoHabilitacion es "No Autorizado", el motivo es la empresa y no la documentación
        /// propia del trabajador.</summary>
        public bool EmpresaHabilitada { get; set; } = true;
        /// <summary>Nombre de la categoría del puesto (campo de lógica). Solo lectura.</summary>
        public string? Categoria { get; set; }
        /// <summary>FK a <c>categoria</c>, derivada de <c>puesto.categoria_id</c> — necesaria
        /// para filtrar el catálogo de puestos por categoría en "Cambiar obra". Solo lectura.</summary>
        public int? CategoriaId { get; set; }
        /// <summary>Nombre del puesto (campo de presentación).</summary>
        public string? Puesto { get; set; }
        /// <summary>FK a <c>puesto</c> — necesaria para prellenar el selector de "Cambiar obra".</summary>
        public int? PuestoId { get; set; }
        public string? ContrataCasa { get; set; }
        public int? ObraOficinaStaffId { get; set; }
        public string? ObraOficina { get; set; }
        public string EstadoWorker { get; set; } = "ACTIVO";
        public bool TieneEmo { get; set; }
        public int? DiasRestantesEmo { get; set; }
        public string? EstadoProgramacionEmo { get; set; }
        public int? AniosExperiencia { get; set; }
        public string? FechaIngreso { get; set; }
        /// <summary>"Pendiente" si tiene una interconsulta sin levantar — usado para advertir
        /// antes de programarle un nuevo EMO (ver ProgramarEmoDialogComponent).</summary>
        public string? InterconsultaEstado { get; set; }
        public string? InterconsultaEspecialidad { get; set; }
    }
}
