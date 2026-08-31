namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerReparacionVinculacionDto
    {
        public int WorkerId { get; set; }
        public int? EmpresaId { get; set; }
        public int? ProyectoId { get; set; }
    }
}
