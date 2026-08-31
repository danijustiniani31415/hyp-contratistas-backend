namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion
{
    public class ProgramacionInasistenciaEnviarCorreoResultDto
    {
        public int TotalSeleccionadas { get; set; }
        public int TotalEnviados { get; set; }
        public int TotalErrores { get; set; }
        public List<string> Detalles { get; set; } = new();
    }
}
