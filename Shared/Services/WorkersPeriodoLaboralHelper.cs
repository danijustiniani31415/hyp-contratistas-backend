using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services
{
    /// <summary>
    /// Las cuatro operaciones que antes eran asignarle una fecha a la ficha
    /// (<c>workers.fecha_ingreso</c> / <c>workers.fecha_retiro</c>) y que ahora abren,
    /// cierran o corrigen una fila de <c>workers_periodo_laboral</c>.
    ///
    /// Vive acá y no en un repositorio porque la escriben tres módulos distintos —
    /// SSOMA (alta y edición de la ficha), Habilitación (baja, baja masiva, reingreso y
    /// edición) y el retiro automático—, y todos tienen que dejar la tabla con la misma
    /// forma: como máximo un periodo abierto por ficha.
    ///
    /// Todos los métodos MUTAN el contexto que reciben y NO llaman a SaveChanges: el
    /// que llama ya está armando su propia transacción y guarda una sola vez.
    /// </summary>
    public static class WorkersPeriodoLaboralHelper
    {
        /// <summary>
        /// El último periodo de la ficha (el vigente si hay uno; si no, el último
        /// cerrado). Mismo orden canónico que las proyecciones de lectura:
        /// <c>fecha_ingreso DESC, id DESC</c>.
        /// </summary>
        public static Task<WorkersPeriodoLaboral?> UltimoAsync(AppDbContext ctx, int workerId) =>
            ctx.WorkersPeriodoLaboral
                .Where(p => p.WorkerId == workerId && p.State)
                .OrderByDescending(p => p.FechaIngreso)
                .ThenByDescending(p => p.WorkersPeriodoLaboralId)
                .FirstOrDefaultAsync();

        /// <summary>El periodo vigente de la ficha, o null si el trabajador está afuera.</summary>
        public static Task<WorkersPeriodoLaboral?> VigenteAsync(AppDbContext ctx, int workerId) =>
            ctx.WorkersPeriodoLaboral
                .Where(p => p.WorkerId == workerId && p.State && p.FechaRetiro == null)
                .OrderByDescending(p => p.FechaIngreso)
                .ThenByDescending(p => p.WorkersPeriodoLaboralId)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Abre un periodo: es el ingreso y también el REINGRESO, que antes se hacía
        /// borrándole la fecha de retiro a la ficha y perdía el paso anterior.
        ///
        /// Si ya hay un periodo abierto no crea otro (el índice único lo rechazaría):
        /// se entiende como una corrección de la fecha de ingreso del que está vigente.
        /// </summary>
        public static async Task AbrirAsync(
            AppDbContext ctx, int workerId, DateOnly fechaIngreso, DateTimeOffset ahora, int? userId = null)
        {
            var abierto = await VigenteAsync(ctx, workerId);
            if (abierto != null)
            {
                if (abierto.FechaIngreso == fechaIngreso) return;
                abierto.FechaIngreso = fechaIngreso;
                abierto.UpdatedDateTime = ahora;
                abierto.UpdatedUserId = userId;
                return;
            }

            // Un periodo con la misma fecha de ingreso ya cerrado tampoco se duplica: el
            // índice ux_workers_periodo_laboral_ingreso lo rechaza y lo que se está
            // pidiendo en realidad es reabrirlo.
            var mismoIngreso = await ctx.WorkersPeriodoLaboral
                .FirstOrDefaultAsync(p => p.WorkerId == workerId && p.State && p.FechaIngreso == fechaIngreso);
            if (mismoIngreso != null)
            {
                mismoIngreso.FechaRetiro = null;
                mismoIngreso.UpdatedDateTime = ahora;
                mismoIngreso.UpdatedUserId = userId;
                return;
            }

            ctx.WorkersPeriodoLaboral.Add(new WorkersPeriodoLaboral
            {
                WorkerId = workerId,
                FechaIngreso = fechaIngreso,
                CreatedDateTime = ahora,
                CreatedUserId = userId,
            });
        }

        /// <summary>
        /// Cierra el periodo vigente con la fecha de retiro. Si no hay ninguno abierto
        /// (baja repetida, o ficha que nunca registró su ingreso) corrige la fecha del
        /// último periodo, y si tampoco hay periodos crea uno de un solo día: la baja
        /// tiene que quedar registrada aunque la ficha llegara sin fecha de ingreso.
        /// </summary>
        public static async Task CerrarAsync(
            AppDbContext ctx, int workerId, DateOnly fechaRetiro, DateTimeOffset ahora, int? userId = null)
        {
            var periodo = await UltimoAsync(ctx, workerId);
            if (periodo == null)
            {
                ctx.WorkersPeriodoLaboral.Add(new WorkersPeriodoLaboral
                {
                    WorkerId = workerId,
                    FechaIngreso = fechaRetiro,
                    FechaRetiro = fechaRetiro,
                    CreatedDateTime = ahora,
                    CreatedUserId = userId,
                });
                return;
            }

            // Un retiro anterior al ingreso lo rechaza ck_workers_periodo_laboral_rango. Antes
            // de la tabla esto se guardaba sin chistar y es como se colaron las 25 fichas
            // torcidas que la migracion tuvo que dejar pasar; ahora se corta acá con un
            // mensaje legible en vez de dejar salir el 23514 como error del servidor.
            if (fechaRetiro < periodo.FechaIngreso)
                throw new AbrilException(
                    $"La fecha de retiro no puede ser anterior a la de ingreso " +
                    $"({periodo.FechaIngreso:dd/MM/yyyy}).", 400);

            periodo.FechaRetiro = fechaRetiro;
            periodo.UpdatedDateTime = ahora;
            periodo.UpdatedUserId = userId;
        }

        /// <summary>
        /// <see cref="CerrarAsync"/> para varias fichas de una sola vez. Trae todos los
        /// periodos en una consulta en vez de una por trabajador: la baja masiva de
        /// Habilitación llega con listas de decenas de ids.
        /// </summary>
        public static async Task CerrarVariosAsync(
            AppDbContext ctx, IReadOnlyCollection<int> workerIds, DateOnly fechaRetiro,
            DateTimeOffset ahora, int? userId = null)
        {
            if (workerIds.Count == 0) return;

            var periodos = await ctx.WorkersPeriodoLaboral
                .Where(p => workerIds.Contains(p.WorkerId) && p.State)
                .ToListAsync();

            var conIngresoPosterior = new List<int>();

            foreach (var workerId in workerIds.Distinct())
            {
                var ultimo = periodos
                    .Where(p => p.WorkerId == workerId)
                    .OrderByDescending(p => p.FechaIngreso)
                    .ThenByDescending(p => p.WorkersPeriodoLaboralId)
                    .FirstOrDefault();

                if (ultimo == null)
                {
                    ctx.WorkersPeriodoLaboral.Add(new WorkersPeriodoLaboral
                    {
                        WorkerId = workerId,
                        FechaIngreso = fechaRetiro,
                        FechaRetiro = fechaRetiro,
                        CreatedDateTime = ahora,
                        CreatedUserId = userId,
                    });
                    continue;
                }

                // Mismo motivo que en CerrarAsync, pero acumulando: una baja masiva no puede
                // cortarse en el primer trabajador y dejar a los demas a medias sin decir cual.
                if (fechaRetiro < ultimo.FechaIngreso)
                {
                    conIngresoPosterior.Add(workerId);
                    continue;
                }

                ultimo.FechaRetiro = fechaRetiro;
                ultimo.UpdatedDateTime = ahora;
                ultimo.UpdatedUserId = userId;
            }

            if (conIngresoPosterior.Count > 0)
                throw new AbrilException(
                    "La fecha de retiro es anterior a la fecha de ingreso de " +
                    $"{conIngresoPosterior.Count} trabajador(es) (ids: " +
                    string.Join(", ", conIngresoPosterior.Take(10)) +
                    (conIngresoPosterior.Count > 10 ? ", ..." : "") + "). No se dio de baja a nadie.", 400);
        }

        /// <summary>
        /// Corrección de la fecha de ingreso desde un formulario de edición: toca el
        /// último periodo y, si la ficha no tiene ninguno, lo abre.
        /// </summary>
        public static async Task SetFechaIngresoAsync(
            AppDbContext ctx, int workerId, DateOnly fechaIngreso, DateTimeOffset ahora, int? userId = null)
        {
            var periodo = await UltimoAsync(ctx, workerId);
            if (periodo == null)
            {
                await AbrirAsync(ctx, workerId, fechaIngreso, ahora, userId);
                return;
            }

            if (periodo.FechaIngreso == fechaIngreso) return;

            // Otro periodo de la MISMA ficha ya empieza ese dia: lo rechaza el indice
            // ux_workers_periodo_laboral_ingreso, asi que se traduce a un 400 legible en vez
            // de dejar salir el 23505 como error del servidor.
            var choca = await ctx.WorkersPeriodoLaboral.AnyAsync(p =>
                p.WorkerId == workerId && p.State && p.FechaIngreso == fechaIngreso
                && p.WorkersPeriodoLaboralId != periodo.WorkersPeriodoLaboralId);
            if (choca)
                throw new AbrilException(
                    "El trabajador ya tiene un periodo laboral que empieza en esa fecha de ingreso.", 400);

            periodo.FechaIngreso = fechaIngreso;
            periodo.UpdatedDateTime = ahora;
            periodo.UpdatedUserId = userId;
        }

        /// <summary>
        /// Corrección de la fecha de retiro desde un formulario de edición. Delega en
        /// <see cref="CerrarAsync"/>: corregir el retiro y darlo de baja escriben lo mismo.
        /// </summary>
        public static Task SetFechaRetiroAsync(
            AppDbContext ctx, int workerId, DateOnly fechaRetiro, DateTimeOffset ahora, int? userId = null) =>
            CerrarAsync(ctx, workerId, fechaRetiro, ahora, userId);

        /// <summary>
        /// Las fechas del último periodo, para las proyecciones que ya tienen la entidad
        /// <see cref="Worker"/> cargada con <c>Include(w =&gt; w.PeriodosLaborales)</c>.
        /// En consultas que se traducen a SQL hay que escribir la subconsulta inline —
        /// EF no traduce este método.
        /// </summary>
        public static (DateOnly? FechaIngreso, DateOnly? FechaRetiro) FechasDe(Worker w)
        {
            var ultimo = w.PeriodosLaborales
                .Where(p => p.State)
                .OrderByDescending(p => p.FechaIngreso)
                .ThenByDescending(p => p.WorkersPeriodoLaboralId)
                .FirstOrDefault();
            return (ultimo?.FechaIngreso, ultimo?.FechaRetiro);
        }
    }
}
