namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Inducciones
{
    public class InduccionTrabajadorDto
    {
        public int WorkerId { get; set; }
        public string ApellidoNombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public int? ObraOficinaStaffId { get; set; }
        public string? ObraOficina { get; set; }
        public int? EmpresaId { get; set; }
        public string EmpresaNombre { get; set; } = string.Empty;
        public bool YaIndujo { get; set; }
    }
}
