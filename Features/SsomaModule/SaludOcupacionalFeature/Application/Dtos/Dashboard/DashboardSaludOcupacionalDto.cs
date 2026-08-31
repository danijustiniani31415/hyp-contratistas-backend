namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Dashboard
{
    public class DashboardSaludOcupacionalDto
    {
        public int TotalTrabajadores { get; set; }
        public int TotalAbril { get; set; }
        public int TotalContratistas { get; set; }
        public AptitudResumenDto EmosPorAptitud { get; set; } = new();
        public VencimientoResumenDto EmosPorVencer { get; set; } = new();
        public int EmosVencidos { get; set; }
        public int InterconsultasPendientes { get; set; }
        public int ProgramacionesSemana { get; set; }
        public int TrabajadoresInhabilitados { get; set; }
        public List<ProximoVencerDto> ProximosVencer { get; set; } = new();
    }

    public class AptitudResumenDto
    {
        public int Apto { get; set; }
        public int AptoConRestricciones { get; set; }
        public int NoApto { get; set; }
        public int Observado { get; set; }
        public int SinEmo { get; set; }
    }

    public class VencimientoResumenDto
    {
        public int Dias30 { get; set; }
        public int Dias15 { get; set; }
        public int Dias7 { get; set; }
    }

    public class ProximoVencerDto
    {
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public DateOnly FechaVencimiento { get; set; }
        public int DiasParaVencer { get; set; }
        public string Empresa { get; set; } = string.Empty;
    }
}
