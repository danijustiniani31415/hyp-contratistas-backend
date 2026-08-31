namespace Abril_Backend.Features.Habilitacion.Application.Dtos.HabEmpresa
{
    public class ProyectoDisponibleDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EstaActiva { get; set; }
        public DateOnly? FechaInicio { get; set; }
    }
}
