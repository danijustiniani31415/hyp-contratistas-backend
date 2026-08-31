namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Inducciones
{
    public class InduccionCreateDto
    {
        public int ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public DateTime FechaProgramada { get; set; }
        public bool TrabajoAltura { get; set; }
        public bool EquipoElectrico { get; set; }
        public List<int> WorkerIds { get; set; } = [];
    }
}
