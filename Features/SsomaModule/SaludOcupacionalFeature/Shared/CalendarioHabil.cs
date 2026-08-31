namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Shared
{
    /// <summary>
    /// Calculadora de días hábiles para la programación de EMOs.
    /// Considera no hábiles: domingos, (opcionalmente) sábados y los feriados/días
    /// no laborables registrados en la tabla <c>holiday</c> (fecha exacta o recurrentes
    /// por mes/día, ej. 28 de julio todos los años).
    ///
    /// Antes, el cálculo de "días hábiles" solo saltaba domingos/sábados e ignoraba
    /// los feriados, por lo que las citas caían en feriados (ej. Fiestas Patrias).
    /// </summary>
    public sealed class CalendarioHabil
    {
        private readonly HashSet<DateOnly> _feriadosExactos = new();
        private readonly HashSet<(int Mes, int Dia)> _feriadosRecurrentes = new();

        /// <param name="feriados">
        /// Feriados activos/vigentes. <c>Recurrente=true</c> aplica todos los años (se
        /// compara solo mes/día); <c>false</c> aplica solo a esa fecha exacta.
        /// </param>
        public CalendarioHabil(IEnumerable<(DateOnly Fecha, bool Recurrente)> feriados)
        {
            foreach (var f in feriados)
            {
                if (f.Recurrente) _feriadosRecurrentes.Add((f.Fecha.Month, f.Fecha.Day));
                else _feriadosExactos.Add(f.Fecha);
            }
        }

        public bool EsFeriado(DateOnly fecha) =>
            _feriadosExactos.Contains(fecha) || _feriadosRecurrentes.Contains((fecha.Month, fecha.Day));

        /// <summary>
        /// Un día es no hábil si es domingo, sábado (cuando <paramref name="excluirSabado"/>
        /// es true, para calendarios Mon-Fri de staff/oficina) o feriado.
        /// </summary>
        public bool EsNoHabil(DateOnly fecha, bool excluirSabado)
        {
            var dow = fecha.DayOfWeek;
            if (dow == DayOfWeek.Sunday) return true;
            if (excluirSabado && dow == DayOfWeek.Saturday) return true;
            return EsFeriado(fecha);
        }

        /// <summary>Retrocede <paramref name="dias"/> días hábiles desde <paramref name="fecha"/>.</summary>
        public DateOnly RestarDiasHabiles(DateOnly fecha, int dias, bool excluirSabado)
        {
            var resultado = fecha;
            int conteo = 0;
            while (conteo < dias)
            {
                resultado = resultado.AddDays(-1);
                if (!EsNoHabil(resultado, excluirSabado)) conteo++;
            }
            return resultado;
        }

        /// <summary>Avanza <paramref name="dias"/> días hábiles desde <paramref name="fecha"/>.</summary>
        public DateOnly SumarDiasHabiles(DateOnly fecha, int dias, bool excluirSabado)
        {
            var resultado = fecha;
            int conteo = 0;
            while (conteo < dias)
            {
                resultado = resultado.AddDays(1);
                if (!EsNoHabil(resultado, excluirSabado)) conteo++;
            }
            return resultado;
        }
    }
}
