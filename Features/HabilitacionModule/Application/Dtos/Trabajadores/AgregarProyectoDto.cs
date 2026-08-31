namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class AgregarProyectoDto
    {
        public int ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public DateOnly? FechaInicio { get; set; }
    }
}
