using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Shared.Models
{
    /// <summary>
    /// Catálogo único de categorías de trabajador. Es el campo de LÓGICA: todo filtro,
    /// comparación, bloqueo o restricción interna se hace contra esta tabla
    /// (a la que se llega por <c>workers.puesto_id → puesto.categoria_id</c>), nunca contra el
    /// NOMBRE del puesto, que es texto libre.
    ///
    /// Unifica los dos catálogos que existían antes:
    /// <list type="bullet">
    ///   <item><c>cat_categoria</c> — alimentaba el desplegable del modal y escribía
    ///   texto libre en <c>workers.categoria</c> (Habilitación, Charlas, Evaluaciones).</item>
    ///   <item><c>workers_category</c> — FK <c>workers.worker_category_id</c>
    ///   (Salidas, Revisores de Áreas, Lecciones Aprendidas).</item>
    /// </list>
    /// Ambas columnas viejas quedan congeladas para auditoría; ya no se leen.
    ///
    /// No confundir con <c>categoria_maestra</c> (EMPLEADO / PRACTICANTE PRE-PRO / RCC):
    /// eso es el tipo de vínculo laboral de la Data Maestra de GTH, otro eje, y sigue
    /// siendo quien manda para detectar practicantes.
    ///
    /// Los nombres se guardan siempre en MAYÚSCULAS.
    /// </summary>
    [Table("categoria")]
    public class Categoria
    {
        [Column("categoria_id")]
        public int CategoriaId { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("orden")]
        public int Orden { get; set; }

        [Column("created_date_time")]
        public DateTime CreatedDateTime { get; set; }

        [Column("created_user_id")]
        public int? CreatedUserId { get; set; }

        [Column("updated_date_time")]
        public DateTime? UpdatedDateTime { get; set; }

        [Column("updated_user_id")]
        public int? UpdatedUserId { get; set; }

        /// <summary>Habilitar/inhabilitar en filtros y desplegables.</summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>
        /// Si la categoría se ofrece en el desplegable de Solicitud de Personal (GTH).
        /// Esa pantalla contrata planilla de Abril: solo tienen sentido las categorías de
        /// Staff y Oficina Central, no las de Obra (Operario, Peón, Oficial…), que llegan
        /// por contratista y solo ensucian el combo.
        ///
        /// Es un flag de VISIBILIDAD por pantalla, no un <c>active</c> global: la categoría
        /// sigue viva y asignable en la ficha del trabajador. Se separó porque el catálogo
        /// lo comparten tres pantallas y apagar una categoría en el `active` general
        /// dejaría sin poder clasificar a los 2.036 trabajadores de obra.
        /// </summary>
        [Column("visible_solicitud_personal")]
        public bool VisibleSolicitudPersonal { get; set; }

        /// <summary>Soft-delete: false = eliminada (se conserva para histórico).</summary>
        [Column("state")]
        public bool State { get; set; } = true;
    }
}
