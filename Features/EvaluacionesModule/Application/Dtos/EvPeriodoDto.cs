namespace Abril_Backend.Features.Evaluaciones.Application.Dtos
{
    public class EvPeriodoDto
    {
        public int Id { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public DateOnly FechaApertura { get; set; }
        public DateOnly FechaCierre { get; set; }
        public bool Activo { get; set; }
        public string NombreMes => new DateTime(Anio, Mes, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-PE"));
        public int DiasRestantes => Math.Max(0, (FechaCierre.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days);
    }

    public class EvPeriodoCreateDto
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public DateOnly FechaApertura { get; set; }
        public DateOnly FechaCierre { get; set; }
    }
}
