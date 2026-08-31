using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    /// <summary>
    /// Un paso del trabajador por Abril: desde que ingresó hasta que se retiró.
    /// Reemplaza a <c>workers.fecha_ingreso</c> / <c>workers.fecha_retiro</c>, que al
    /// ser dos columnas sueltas en la ficha solo podían guardar UN paso: cuando alguien
    /// reingresaba había que elegir entre pisar las fechas viejas o abrir una ficha nueva
    /// en <c>workers</c> — y se hacía lo segundo, que parte en dos el historial (EMOs,
    /// inducciones, amonestaciones) de una misma persona.
    ///
    /// Ahora un reingreso es una fila más acá y la ficha sigue siendo la misma:
    /// <c>HabTrabajadorRepository.ReingresarAsync</c> abre un periodo nuevo en vez de
    /// borrarle la fecha de retiro a la ficha.
    ///
    /// NO confundir con <c>worker_vinculaciones</c>, que es más fina: ahí se registra a
    /// qué razón social y a qué proyecto está vinculado, y un mismo periodo laboral puede
    /// tener varias vinculaciones (mover a alguien de Carpi a Salerno, o de obra a obra,
    /// no lo saca del grupo). En prod hay fichas con 39 vinculaciones y un solo ingreso.
    ///
    /// <para><b>Periodo vigente:</b> el que tiene <see cref="FechaRetiro"/> en null. Hay
    /// como máximo uno por ficha (índice único parcial
    /// <c>ux_workers_periodo_laboral_abierto</c>), y su ausencia significa que el
    /// trabajador está afuera — o que nunca entró, como las fichas de pre-ingreso
    /// (finalistas aprobados), que no tienen ningún periodo.</para>
    ///
    /// <para><b>Cómo leer "la fecha de ingreso" de una ficha:</b> es la del ÚLTIMO
    /// periodo, ordenando por <c>fecha_ingreso DESC, id DESC</c> — el vigente si hay
    /// uno, y si no el último cerrado. Ese orden es el canónico y hay que repetirlo tal
    /// cual en cada consulta; para el SQL crudo de Dapper está escrito una sola vez en
    /// <c>Shared/Constants/WorkersPeriodoLaboralSql.cs</c>.</para>
    /// </summary>
    [Table("workers_periodo_laboral")]
    public class WorkersPeriodoLaboral
    {
        /// <summary>
        /// La PK sale por convención de EF, que la busca como <c>Id</c> o
        /// <c>{NombreDeLaClase}Id</c>. Por eso la clase se llama <c>Workers</c>PeriodoLaboral
        /// en plural, igual que la tabla: si el nombre de la clase y el de esta propiedad
        /// dejan de coincidir, EF no encuentra la llave y falla al construir el modelo — o
        /// sea que se cae la aplicación entera, no solo lo que use esta tabla.
        /// </summary>
        [Key]
        [Column("workers_periodo_laboral_id")]
        public int WorkersPeriodoLaboralId { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("fecha_ingreso")]
        public DateOnly FechaIngreso { get; set; }

        /// <summary>Null = periodo vigente (el trabajador sigue adentro).</summary>
        [Column("fecha_retiro")]
        public DateOnly? FechaRetiro { get; set; }

        [Column("created_date_time")]
        public DateTimeOffset CreatedDateTime { get; set; }

        [Column("created_user_id")]
        public int? CreatedUserId { get; set; }

        [Column("updated_date_time")]
        public DateTimeOffset? UpdatedDateTime { get; set; }

        [Column("updated_user_id")]
        public int? UpdatedUserId { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("state")]
        public bool State { get; set; } = true;

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }
    }
}
