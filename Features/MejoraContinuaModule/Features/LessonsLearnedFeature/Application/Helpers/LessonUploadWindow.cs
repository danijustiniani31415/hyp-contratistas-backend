using System;
using System.Collections.Generic;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Helpers
{
    /// <summary>
    /// Ventana de los últimos 5 días hábiles del mes para Lecciones Aprendidas:
    ///   • Ordinales 1–3 (los 3 más tempranos): ventana de SUBIDA — los usuarios
    ///     registran sus lecciones; el cron envía recordatorios.
    ///   • Ordinales 4–5 (los 2 últimos días hábiles): ventana de REVISIÓN de la
    ///     jefatura — NO se permite registrar nuevas lecciones.
    ///
    /// El bloqueo de subida cubre el RANGO CONTINUO desde el 4.º último día hábil
    /// hasta el último día hábil del mes (inclusive). Es decir, además de esos dos
    /// días hábiles, también quedan bloqueados los sábados, domingos y feriados que
    /// caigan ENTRE ellos: durante toda la ventana de revisión nadie puede subir.
    ///
    /// Para decidir qué es "día hábil" se descartan sábados, domingos y los feriados
    /// que se pasen en <paramref name="holidays"/> (misma fuente que el cron de
    /// recordatorios — ver <c>ReminderService</c>). La lógica de días hábiles está
    /// COPIADA (no compartida) a propósito para no acoplar el bloqueo de subida con
    /// el cron; si cambia el criterio de días hábiles, actualizar ambas copias.
    /// </summary>
    public static class LessonUploadWindow
    {
        /// <summary>
        /// true si <paramref name="date"/> cae dentro de la ventana de revisión de
        /// la jefatura (rango continuo del 4.º al último día hábil del mes, incluidos
        /// fines de semana y feriados intermedios → subida bloqueada).
        /// </summary>
        public static bool IsReviewWindow(DateTime date, HashSet<DateOnly>? holidays = null)
        {
            var (start, end) = ReviewWindowRange(date, holidays);
            if (start is null || end is null) return false;

            var d = DateOnly.FromDateTime(date.Date);
            return d >= start.Value && d <= end.Value;
        }

        /// <summary>
        /// Rango [inicio, fin] inclusivo de la ventana de revisión:
        ///   • inicio = fecha del 4.º último día hábil del mes (ordinal 4).
        ///   • fin    = fecha del último día hábil del mes (ordinal 5).
        /// Cualquier fecha entre ambos (sábados/domingos/feriados incluidos) está en
        /// la ventana. Devuelve (null, null) si el mes no tiene al menos 2 días
        /// hábiles (caso teórico).
        /// </summary>
        public static (DateOnly? Start, DateOnly? End) ReviewWindowRange(DateTime date, HashSet<DateOnly>? holidays = null)
        {
            var businessDays = LastFiveBusinessDays(date, holidays);
            if (businessDays.Count < 2) return (null, null);

            // businessDays[0] = último hábil del mes (ordinal 5).
            // businessDays[1] = penúltimo hábil (ordinal 4) → inicio de la revisión.
            var end = DateOnly.FromDateTime(businessDays[0].Date);
            var start = DateOnly.FromDateTime(businessDays[1].Date);
            return (start, end);
        }

        /// <summary>
        /// Los últimos (hasta) 5 días hábiles del mes de <paramref name="date"/>,
        /// del más tardío [0] al más temprano. No cuentan como hábiles los sábados,
        /// domingos ni los feriados en <paramref name="holidays"/>.
        /// </summary>
        private static List<DateTime> LastFiveBusinessDays(DateTime date, HashSet<DateOnly>? holidays)
        {
            var year = date.Year;
            var month = date.Month;
            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var businessDays = new List<DateTime>();
            for (var d = lastDay; d.Month == month; d = d.AddDays(-1))
            {
                var isWeekend = d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
                var isHoliday = holidays != null && holidays.Contains(DateOnly.FromDateTime(d));
                if (!isWeekend && !isHoliday)
                    businessDays.Add(d);
                if (businessDays.Count == 5) break;
            }

            return businessDays;
        }
    }
}
