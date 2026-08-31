namespace Abril_Backend.Features.Habilitacion.Application.Dtos.ControlAcceso
{
    public class ControlAccesoWorkerDto
    {
        public int WorkerId { get; set; }
        public string ApellidoNombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string EmpresaNombre { get; set; } = string.Empty;
        public string ProyectoNombre { get; set; } = string.Empty;
        public string EstadoHabilitacion { get; set; } = string.Empty;
        public bool EmpresaActiva { get; set; }
        /// <summary>Habilitación SSOMA de la empresa contratista (ignora entregables
        /// administrativos) — solo se evalúa para trabajadores Contratista, ver
        /// EmpresaHabilitacionHelper. Siempre true para Casa/oficina central.</summary>
        public bool EmpresaHabilitada { get; set; } = true;
        /// <summary>Motivo profesional a mostrar cuando el bloqueo es por la empresa y no por
        /// documentación propia del trabajador — null si no aplica.</summary>
        public string? MotivoNoAutorizado { get; set; }
        public List<string> DocumentosFaltantes { get; set; } = [];
        public List<string> DocumentosPorVencer { get; set; } = [];
        public List<EntregableResumenDto> Entregables { get; set; } = [];
        public bool Restringido { get; set; }
    }
}
