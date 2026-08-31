namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion
{
    public class ProgramacionResumenDto
    {
        public int Programados { get; set; }
        public int Aceptados { get; set; }
        public int EnAtencion { get; set; }
        public int Completados { get; set; }
        public int Rechazados { get; set; }
        public int NoPresento { get; set; }
        public int Automaticos { get; set; }
        public int Total { get; set; }
    }
}
