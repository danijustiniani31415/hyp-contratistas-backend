namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// Fragmentos de SQL crudo para leer <c>workers_periodo_laboral</c> desde Dapper.
    ///
    /// Existen porque la tabla reemplazó a <c>workers.fecha_retiro</c>, que era una
    /// comparación de una línea, y ahora hay que ir al último periodo de la ficha. Al
    /// escribirlo una sola vez, el orden canónico (<c>fecha_ingreso DESC, id DESC</c>,
    /// el mismo que usan las consultas de EF) no puede divergir entre las tres
    /// consultas de Evaluaciones que lo usan.
    ///
    /// Todos asumen que la consulta que los interpola tiene la tabla <c>workers</c>
    /// aliaseada como <c>w</c>.
    /// </summary>
    public static class WorkersPeriodoLaboralSql
    {
        /// <summary>
        /// Traducción exacta del viejo <c>(w.fecha_retiro IS NULL OR w.fecha_retiro &gt;
        /// CURRENT_DATE)</c>: el trabajador no está retirado a hoy. Una ficha sin ningún
        /// periodo también pasa, igual que antes pasaba la que tenía la columna en NULL.
        /// </summary>
        public const string NoRetiradoHoy = @"
                COALESCE(
                    (SELECT pl.fecha_retiro
                       FROM workers_periodo_laboral pl
                      WHERE pl.worker_id = w.id AND pl.state
                      ORDER BY pl.fecha_ingreso DESC, pl.workers_periodo_laboral_id DESC
                      LIMIT 1),
                    DATE '9999-12-31') > CURRENT_DATE";

        /// <summary>
        /// Subconsulta escalar con la fecha de ingreso del último periodo, para los
        /// SELECT que antes leían <c>w.fecha_ingreso</c>.
        /// </summary>
        public const string FechaIngresoUltimoPeriodo = @"
                (SELECT pl.fecha_ingreso
                   FROM workers_periodo_laboral pl
                  WHERE pl.worker_id = w.id AND pl.state
                  ORDER BY pl.fecha_ingreso DESC, pl.workers_periodo_laboral_id DESC
                  LIMIT 1)";
    }
}
