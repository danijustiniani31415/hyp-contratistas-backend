using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Shared.Extensions
{
    /// <summary>
    /// Filtros de <c>workers</c> que expresan una intención una sola vez, en vez de
    /// repetir comparaciones de estado en cada consulta.
    ///
    /// Existe porque desde que <c>workers</c> guarda también fichas de pre-ingreso
    /// (finalistas aprobados de Reclutamiento a los que hay que programarles el EMO
    /// de Ingreso antes de que firmen), una consulta que no diga nada del estado
    /// devuelve gente que todavía no es trabajadora de Abril.
    ///
    /// El aislamiento fuerte lo sigue dando <c>worker_vinculaciones</c> — la
    /// vinculación es el contrato, y un finalista no tiene ninguna, así que todo lo
    /// que exige vinculación vigente ya lo deja fuera sin tocar nada. Este helper es
    /// para el resto: buscadores, desplegables y reportes que leen <c>workers</c>
    /// directo.
    /// </summary>
    public static class WorkerQueryExtensions
    {
        /// <summary>
        /// Solo fichas de gente que llegó a ingresar a Abril (ACTIVO, RETIRADO o
        /// INHABILITADO_SSOMA). Deja fuera las de pre-ingreso.
        ///
        /// Es el filtro por defecto de cualquier consulta de trabajadores; usar
        /// <see cref="SoloPreIngreso"/> para lo contrario y <see cref="SoloActivos"/>
        /// cuando además se quiera excluir a los retirados.
        /// </summary>
        public static IQueryable<Worker> SoloTrabajadores(this IQueryable<Worker> query) =>
            query.Where(w => WorkersEstadoIds.LlegaronAIngresar.Contains(w.WorkersEstadoId));

        /// <summary>
        /// Solo fichas de pre-ingreso: finalistas aprobados esperando su EMO de
        /// Ingreso, y los que terminaron no ingresando.
        /// </summary>
        public static IQueryable<Worker> SoloPreIngreso(this IQueryable<Worker> query) =>
            query.Where(w => WorkersEstadoIds.PreIngreso.Contains(w.WorkersEstadoId));

        /// <summary>
        /// Solo trabajadores vigentes (ACTIVO). Equivale al viejo
        /// <c>estado == "ACTIVO"</c>, pero sin depender del texto.
        /// </summary>
        public static IQueryable<Worker> SoloActivos(this IQueryable<Worker> query) =>
            query.Where(w => w.WorkersEstadoId == WorkersEstadoIds.Activo);

        /// <summary>
        /// Trabajadores no retirados: reemplaza al viejo <c>estado != "RETIRADO"</c>.
        ///
        /// OJO: esa comparación negativa es justamente la que dejaría entrar a las
        /// fichas de pre-ingreso, así que acá se excluyen explícitamente además del
        /// retiro. Si lo que se quiere es "activo a secas", usar <see cref="SoloActivos"/>.
        /// </summary>
        public static IQueryable<Worker> SoloNoRetirados(this IQueryable<Worker> query) =>
            query.Where(w => w.WorkersEstadoId == WorkersEstadoIds.Activo
                          || w.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma);
    }
}
