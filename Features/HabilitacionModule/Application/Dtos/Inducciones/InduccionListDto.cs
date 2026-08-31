namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Inducciones
{
    public class InduccionListDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string ApellidoNombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public int EmpresaId { get; set; }
        public string EmpresaNombre { get; set; } = string.Empty;
        public DateTime FechaProgramada { get; set; }
        public bool TrabajoAltura { get; set; }
        public bool EquipoElectrico { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool IngresoConfirmado { get; set; }
        public DateTime? FechaIngreso { get; set; }
    }
}
