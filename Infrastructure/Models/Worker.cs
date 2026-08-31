using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("workers")]
    public class Worker
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_trabajador")]
        public int? IdTrabajador { get; set; }

        [Column("person_id")]
        public int? PersonId { get; set; }

        [Column("contributor_id")]
        public int? ContributorId { get; set; }

        /*[Column("celular")]
        public string? Celular { get; set; }*/

        [Column("apellido_nombre")]
        public string? ApellidoNombre { get; set; }

        /// <summary>Correo corporativo @abril.pe del trabajador (antes llamado email_personal).</summary>
        [Column("email_corporativo")]
        public string? EmailCorporativo { get; set; }

        /// <summary>Celular corporativo del trabajador (el personal vive en <c>person.phone_number</c>).</summary>
        [Column("celular_corporativo")]
        public string? CelularCorporativo { get; set; }

        // La fecha de nacimiento dejó de vivir aquí: ahora es única en person.fecha_nacimiento.

        /// <summary>
        /// Los pasos del trabajador por Abril (ingreso → retiro). Reemplaza a las
        /// columnas <c>fecha_ingreso</c> / <c>fecha_retiro</c> que vivían acá y que solo
        /// podían guardar uno: un reingreso obligaba a pisarlas o a abrir otra ficha.
        /// Ver <see cref="WorkersPeriodoLaboral"/> — incluido cómo leer "la" fecha de
        /// ingreso de la ficha, que es la del último periodo.
        /// </summary>
        public ICollection<WorkersPeriodoLaboral> PeriodosLaborales { get; set; } = new List<WorkersPeriodoLaboral>();

        /// <summary>
        /// Puesto del trabajador: el campo de PRESENTACIÓN (es lo que se muestra en
        /// pantalla, PDFs y correos) y, a la vez, el ÚNICO camino a la categoría.
        ///
        /// La categoría es el campo de LÓGICA — todo filtro, comparación, bloqueo o
        /// restricción interna se hace contra ella — pero ya no vive en <c>workers</c>:
        /// se llega por <c>PuestoCatalogo.CategoriaId</c>. Antes había una FK
        /// <c>workers.categoria_id</c> en paralelo y las dos podían contradecirse (26%
        /// de las fichas de prod lo hacían); se bajó en
        /// <c>Migrations_Manual/2026-08-21_workers_categoria_desde_puesto.sql</c>.
        ///
        /// O sea: el NOMBRE del puesto no decide nada (es texto libre y editable), pero
        /// a qué categoría pertenece ese puesto sí. Cambiarle la categoría a un puesto
        /// desde Configuración → Categorías y Puestos le cambia el nivel a todos sus
        /// trabajadores.
        ///
        /// Sin puesto no hay categoría: una ficha con <c>PuestoId == null</c> queda fuera
        /// de todo filtro y de toda regla.
        /// </summary>
        [Column("puesto_id")]
        public int? PuestoId { get; set; }

        [ForeignKey(nameof(PuestoId))]
        public Puesto? PuestoCatalogo { get; set; }

        // ── Columnas congeladas ────────────────────────────────────────────────
        // Reemplazadas por PuestoId al unificar los cinco catálogos que describían
        // "qué hace" un trabajador (ver Migrations_Manual/
        // categoria_puesto_unificados.sql). Se conservan solo para auditoría:
        // no se leen ni se escriben desde ningún lado.

        /// <summary>Congelada. Antes: texto libre de la categoría. Usar <see cref="PuestoId"/>.</summary>
        [Column("categoria")]
        public string? Categoria { get; set; }

        /// <summary>Congelada. Antes: FK a <c>workers_category</c>. Usar <see cref="PuestoId"/>.</summary>
        [Column("worker_category_id")]
        public int? WorkerCategoryId { get; set; }

        /// <summary>Congelada. Antes: texto libre de la ocupación. Usar <see cref="PuestoId"/>.</summary>
        [Column("ocupacion")]
        public string? Ocupacion { get; set; }

        /// <summary>Congelada. Antes: FK a <c>cat_ocupacion</c>. Usar <see cref="PuestoId"/>.</summary>
        [Column("ocupacion_id")]
        public int? OcupacionId { get; set; }

        /// <summary>Congelada. Antes: texto libre del puesto. Usar <see cref="PuestoId"/>.</summary>
        [Column("puesto")]
        public string? Puesto { get; set; }

        /// <summary>
        /// FK al catálogo <c>categoria_maestra</c> (EMPLEADO / PRACTICANTE PRE-PRO / RCC),
        /// tomado de la columna CATEGORÍA de la Data Maestra de GTH.
        /// </summary>
        [Column("categoria_maestra_id")]
        public int? CategoriaMaestraId { get; set; }

        /// <summary>Nombre del contacto de emergencia del trabajador.</summary>
        [Column("contacto_emergencia")]
        public string? ContactoEmergencia { get; set; }

        /// <summary>Celular del contacto de emergencia.</summary>
        [Column("celular_emergencia")]
        public string? CelularEmergencia { get; set; }

        [Column("area")]
        public string? Area { get; set; }

        [Column("subarea")]
        public string? Subarea { get; set; }

        /// <summary>
        /// FK a <c>area_scope</c>. Reemplaza el uso de <see cref="Area"/>/<see cref="Subarea"/>
        /// (texto plano) para resolver jefaturas vía el árbol jerárquico.
        /// </summary>
        [Column("area_scope_id")]
        public int? AreaScopeId { get; set; }

        [Column("contrata_casa")]
        public string? ContrataCasa { get; set; }

        /// <summary>
        /// FK a <c>workers_obra_oficina_staff</c> (Obra / Staff / Oficina Central).
        /// Reemplaza al texto plano <c>obra_oficina</c> y al nodo hoja de tipo
        /// "Área Obra_Oficina" del árbol <c>area_scope</c>: la ubicación del
        /// trabajador ya NO se deduce del área. Ver <c>ObraOficinaStaffIds</c>.
        /// </summary>
        [Column("obra_oficina_staff_id")]
        public int? ObraOficinaStaffId { get; set; }

        [ForeignKey(nameof(ObraOficinaStaffId))]
        public WorkersObraOficinaStaff? ObraOficinaStaff { get; set; }

        [Column("jefatura")]
        public string? Jefatura { get; set; }

        /// <summary>
        /// CONGELADA: la reemplaza <see cref="WorkersEstadoId"/>. Un trigger de la
        /// base la mantiene sincronizada desde la FK mientras quede SQL crudo que
        /// la lea; se elimina en el deploy siguiente. No escribir ni filtrar por
        /// ella en codigo nuevo.
        /// </summary>
        [Column("estado")]
        public string? Estado { get; set; }

        /// <summary>
        /// Estado de la ficha (catalogo <c>workers_estado</c>). Ver
        /// <see cref="Shared.Constants.WorkersEstadoIds"/>. Para "solo los que
        /// llegaron a ingresar" usar <c>SoloTrabajadores()</c> en vez de comparar
        /// ids a mano.
        /// </summary>
        [Column("workers_estado_id")]
        public int WorkersEstadoId { get; set; }

        [ForeignKey(nameof(WorkersEstadoId))]
        public WorkersEstado? WorkersEstado { get; set; }

        [Column("habilitado_obra")]
        public bool? HabilitadoObra { get; set; }

        [Column("sctr")]
        public bool? Sctr { get; set; }

        [Column("condicion_medica")]
        public string? CondicionMedica { get; set; }

        [Column("procedencia")]
        public string? Procedencia { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }

        [Column("anios_experiencia")]
        public int? AniosExperiencia { get; set; }

        [Column("puntos_infraccion")]
        public int? PuntosInfraccion { get; set; }

        /// <summary>
        /// Jefe directo (worker) encargado de revisar las lecciones aprendidas de este
        /// trabajador. Autoreferencia a <c>workers.id</c>; null si aún no se asigna.
        /// </summary>
        [Column("worker_lesson_jefe_id")]
        public int? WorkerLessonJefeId { get; set; }

        /// <summary>
        /// Jefe directo (worker) encargado de aprobar/rechazar las solicitudes de salida
        /// de este trabajador. Autoreferencia a <c>workers.id</c>; null si aún no se asigna,
        /// en cuyo caso el aprobador se resuelve por el árbol de áreas (ApproverResolver).
        /// </summary>
        [Column("worker_salida_jefe_id")]
        public int? WorkerSalidaJefeId { get; set; }

        /// <summary>
        /// Si true, las lecciones aprendidas creadas por este trabajador se auto-aprueban
        /// al momento de crear o editar (solo en sus propias lecciones). Sin notificación.
        /// </summary>
        [Column("auto_approve_lesson")]
        public bool AutoApproveLesson { get; set; }

        /// <summary>
        /// Soft delete. <c>false</c> = ficha eliminada: no se muestra en ninguna pantalla,
        /// y el filtro global de <c>AppDbContext</c> la saca de toda consulta de EF sin que
        /// cada repositorio tenga que acordarse.
        ///
        /// Se agregó para las fichas duplicadas que dejó el modelo viejo de
        /// <c>fecha_ingreso</c>/<c>fecha_retiro</c>: como eran dos columnas de la ficha, un
        /// reingreso obligaba a abrir OTRA fila en <c>workers</c> para la misma persona.
        /// La fusión de esas fichas está en
        /// <c>Migrations_Manual/2026-08-25_workers_fusion_fichas_duplicadas.sql</c> y qué
        /// ficha quedó contra cuál se lee en <c>workers_ficha_fusionada</c>.
        ///
        /// OJO con el <c>= true</c>: sin él, el default de <c>bool</c> haría que toda ficha
        /// nueva naciera eliminada y desapareciera apenas se guarda.
        /// </summary>
        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        public Person? Person { get; set; }
        public Contributor? Contributor { get; set; }
    }
}
