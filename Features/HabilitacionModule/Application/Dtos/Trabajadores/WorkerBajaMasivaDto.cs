namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerBajaMasivaDto
    {
        public List<int> Ids { get; set; } = [];
        public DateOnly? FechaRetiro { get; set; }
    }
}
