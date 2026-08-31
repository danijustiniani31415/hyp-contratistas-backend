namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta
{
    public class InterconsultaEnviarCorreoDto
    {
        public List<int> Ids { get; set; } = new List<int>();
    }

    public class InterconsultaEnviarCorreoResultDto
    {
        public int TotalSeleccionadas { get; set; }
        public int TotalEnviados { get; set; }
        public int TotalErrores { get; set; }
        public List<string> Detalles { get; set; } = new List<string>();
    }
}
