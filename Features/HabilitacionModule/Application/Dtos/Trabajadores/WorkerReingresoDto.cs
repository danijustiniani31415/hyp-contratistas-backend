namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerReingresoDto
    {
        public int? NuevoProyectoId { get; set; }
        public int? NuevaEmpresaId { get; set; }
        public DateOnly? FechaReingreso { get; set; }
    }
}
