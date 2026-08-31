using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    /// <summary>
    /// Catálogo normalizado del estado de la ficha de trabajador. Reemplaza al
    /// texto plano <c>workers.estado</c>, que además de no estar normalizado
    /// venía con basura de mayúsculas ('Activo' vs 'ACTIVO') que rompía filtros
    /// en silencio.
    ///
    /// REINGRESO no está: nunca fue un estado de <c>workers</c> sino un tipo de
    /// evento de <c>worker_eventos</c> (ver <c>WorkerTipoEvento.Reingreso</c>);
    /// el reingreso deja al trabajador en ACTIVO.
    ///
    /// Los códigos son fijos — ver <see cref="Shared.Constants.WorkersEstadoCodigos"/>.
    /// </summary>
    [Table("workers_estado")]
    public class WorkersEstado
    {
        [Column("workers_estado_id")]
        public int WorkersEstadoId { get; set; }

        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("orden")]
        public int Orden { get; set; }

        /// <summary>
        /// Hecho ya consumado, no condición actual: una vez true no vuelve a false.
        /// Un RETIRADO tiene true (sí llegó a ingresar, y después salió); un
        /// FINALISTA_APROBADO tiene false (todavía no) y un NO_INGRESO también
        /// (nunca llegó).
        ///
        /// Es el filtro que separa a las fichas de pre-ingreso del resto del
        /// sistema: todo lo que quiera "solo trabajadores de verdad" pregunta por
        /// esto en vez de enumerar códigos a mano.
        /// </summary>
        [Column("llego_a_ingresar")]
        public bool LlegoAIngresar { get; set; }

        /// <summary>
        /// Condición actual, no hecho consumado: ¿esta persona trabaja en Abril hoy?
        /// Cambia en los dos sentidos (un RETIRADO que reingresa vuelve a ACTIVO).
        /// true para ACTIVO e INHABILITADO_SSOMA; false para RETIRADO,
        /// FINALISTA_APROBADO y NO_INGRESO.
        ///
        /// Es la diferencia con <see cref="LlegoAIngresar"/>, que solo difiere en
        /// RETIRADO: ese sí llegó a ingresar (true) pero ya no está adentro (false).
        ///
        /// Lo usa Reclutamiento para decidir si GTH puede aprobar el formulario de un
        /// postulante cuyo documento ya existe en la base: si la ficha que coincide
        /// está adentro, la aprobación se bloquea (aprobar sobreescribiría los datos
        /// de un trabajador actual con lo que tecleó un desconocido).
        /// </summary>
        [Column("esta_adentro")]
        public bool EstaAdentro { get; set; }

        /// <summary>Habilitar/inhabilitar en filtros y desplegables.</summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>Soft-delete: false = eliminado (se conserva para histórico).</summary>
        [Column("state")]
        public bool State { get; set; } = true;
    }
}
