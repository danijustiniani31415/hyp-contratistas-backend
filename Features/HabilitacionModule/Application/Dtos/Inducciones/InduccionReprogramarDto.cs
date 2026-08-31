namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Inducciones
{
    public class InduccionReprogramarDto
    {
        public DateTime? FechaProgramada { get; set; }
        public int? ProyectoId { get; set; }
        public bool? TrabajoAltura { get; set; }
    }
}
