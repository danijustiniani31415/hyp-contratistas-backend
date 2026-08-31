namespace Abril_Backend.Features.GestionAdministrativa.Shared.Services
{
    /// <summary>
    /// Rango del mes anterior al actual, en hora de Perú. Lo usan las dos pantallas de salidas para
    /// la acción "rendir el mes anterior", de ahí que viva en el Shared del módulo.
    /// </summary>
    public static class MesAnteriorPeru
    {
        /// <summary>
        /// Primer y último día del mes anterior al actual, en hora de Perú (UTC-5). El servidor
        /// corre en UTC: el día 1 de mes, antes de las 05:00 UTC, en Lima todavía es el mes
        /// anterior y el rango calculado con la hora del servidor saldría corrido un mes entero.
        /// </summary>
        public static (DateOnly Desde, DateOnly Hasta) Rango()
        {
            var hoy              = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            var primeroMesActual = new DateOnly(hoy.Year, hoy.Month, 1);
            return (primeroMesActual.AddMonths(-1), primeroMesActual.AddDays(-1));
        }
    }
}
