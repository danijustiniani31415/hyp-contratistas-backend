namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// IDs del catálogo <c>workers_estado</c>. Se insertan con id explícito en
    /// <c>Migrations_Manual/2026-08-20_workers_estado_y_emo_ingreso_finalista.sql</c>,
    /// así que son idénticos en dev y prod y se pueden usar como constantes.
    ///
    /// Sustituyen a las comparaciones por texto (<c>estado == "ACTIVO"</c>) que
    /// existían antes de normalizar la columna, incluidas las que estaban rotas
    /// por diferencias de mayúsculas.
    /// </summary>
    public static class WorkersEstadoIds
    {
        /// <summary>Trabajador vigente.</summary>
        public const int Activo = 1;

        /// <summary>Ya no trabaja en Abril. Sí llegó a ingresar.</summary>
        public const int Retirado = 2;

        /// <summary>Inhabilitado por SSOMA (escuelita). Sigue siendo trabajador.</summary>
        public const int InhabilitadoSsoma = 3;

        /// <summary>
        /// Ficha de pre-ingreso: el solicitante lo aprobó como finalista en
        /// Reclutamiento y GTH ya puede programarle el EMO de Ingreso, pero
        /// todavía no firma contrato. No tiene fila en <c>worker_vinculaciones</c>,
        /// que es lo que lo mantiene fuera del resto del sistema.
        /// </summary>
        public const int FinalistaAprobado = 4;

        /// <summary>
        /// Estado terminal del pre-ingreso: rechazó la carta oferta o el contrato.
        /// La ficha se conserva para auditoría (nunca se borra), pero nunca tuvo
        /// vinculación, así que nunca apareció en ninguna pantalla de trabajadores.
        /// </summary>
        public const int NoIngreso = 5;

        /// <summary>
        /// Los estados cuya ficha corresponde a alguien que sí llegó a ingresar a
        /// Abril (<c>workers_estado.llego_a_ingresar = true</c>). Es la lista que
        /// usa <c>WorkerQueryExtensions.SoloTrabajadores()</c>; preferir ese helper
        /// antes que repetir el <c>Contains</c> a mano.
        /// </summary>
        public static readonly int[] LlegaronAIngresar = { Activo, Retirado, InhabilitadoSsoma };

        /// <summary>
        /// Los estados de ficha de pre-ingreso: la persona todavía no es (o nunca
        /// fue) trabajador de Abril.
        /// </summary>
        public static readonly int[] PreIngreso = { FinalistaAprobado, NoIngreso };

        /// <summary>
        /// Los estados de alguien que trabaja en Abril HOY
        /// (<c>workers_estado.esta_adentro = true</c>). Condición actual, no hecho
        /// consumado: es lo que distingue a <see cref="Retirado"/> —que sí llegó a
        /// ingresar pero ya no está adentro— de <see cref="Activo"/>.
        ///
        /// Preferir la columna cuando la consulta ya toca <c>workers_estado</c>
        /// (así un estado nuevo no obliga a editar este array); el array es para
        /// las consultas que solo tienen <c>workers.workers_estado_id</c> a mano.
        /// </summary>
        public static readonly int[] EstanAdentro = { Activo, InhabilitadoSsoma };

        /// <summary>
        /// Trabajadores vigentes, retirados o no. Reemplaza al viejo
        /// <c>estado != "RETIRADO"</c>, que además de no excluir a las fichas de
        /// pre-ingreso venía escrito de tres formas distintas por el código
        /// (una de ellas, <c>!= "Retirado"</c>, no filtraba nada por la diferencia
        /// de mayúsculas).
        ///
        /// Es el mismo conjunto que <see cref="EstanAdentro"/> nombrado por lo que
        /// excluye; apunta al mismo array a propósito para que no puedan divergir.
        /// </summary>
        public static readonly int[] NoRetirados = EstanAdentro;

        /// <summary>
        /// <see cref="LlegaronAIngresar"/> lista para interpolar dentro de un
        /// <c>IN (...)</c> de SQL crudo ("1,2,3"). Misma fuente que el array, para
        /// que agregar un estado no obligue a acordarse de dos sitios.
        /// </summary>
        public static readonly string LlegaronAIngresarSql = string.Join(",", LlegaronAIngresar);

        /// <summary><see cref="NoRetirados"/> para interpolar en SQL crudo ("1,3").</summary>
        public static readonly string NoRetiradosSql = string.Join(",", NoRetirados);

        /// <summary>
        /// Codigo del estado a partir del id, sin ir a la base. Mismo patron que
        /// <see cref="ObraOficinaStaffIds.Nombre"/>: sirve para las proyecciones en
        /// memoria que antes leian el texto <c>workers.estado</c> y que no vale la
        /// pena convertir en un Include solo para mostrar una palabra.
        /// </summary>
        public static string? Codigo(int? id) => id switch
        {
            Activo            => "ACTIVO",
            Retirado          => "RETIRADO",
            InhabilitadoSsoma => "INHABILITADO_SSOMA",
            FinalistaAprobado => "FINALISTA_APROBADO",
            NoIngreso         => "NO_INGRESO",
            _                 => null
        };
    }
}
