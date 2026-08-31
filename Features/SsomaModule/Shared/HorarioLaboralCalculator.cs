namespace Abril_Backend.Features.SsomaModule.Shared
{
    /// <summary>
    /// Jornada laboral estándar de 48h/semana: Lunes a Viernes 8.5h, Sábado 5.5h, Domingo 0h.
    /// Usado para convertir personas-día del Tareo en Horas Hombre, tanto en el dashboard/tabla
    /// de Horas Hombre como en el cálculo de HHT para los índices IF/IG/IA.
    /// </summary>
    public static class HorarioLaboralCalculator
    {
        public static decimal HorasPorDia(DateOnly fecha) => fecha.DayOfWeek switch
        {
            DayOfWeek.Sunday => 0m,
            DayOfWeek.Saturday => 5.5m,
            _ => 8.5m,
        };
    }
}
