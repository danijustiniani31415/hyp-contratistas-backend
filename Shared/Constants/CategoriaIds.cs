namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// IDs del catálogo <c>categoria</c> (<c>workers.puesto_id → puesto.categoria_id</c>) sobre los que hay
    /// lógica de negocio. Los ids son idénticos en dev y prod (verificado el 2026-08-13:
    /// las 41 categorías originales coinciden id ↔ nombre en ambas, y <see cref="Empleado"/>
    /// se insertó con id explícito), así que se pueden usar como constantes — mismo
    /// criterio que <see cref="CategoriaMaestraIds"/>.
    ///
    /// Solo están acá las categorías que el código realmente compara. El resto del
    /// catálogo (OPERARIO, PEÓN, ARQUITECTO…) es data pura: se muestra y se filtra desde
    /// la UI, pero ninguna regla depende de ellas, así que no tienen constante.
    ///
    /// Antes cada feature comparaba contra el NOMBRE (<c>c.Nombre == "GERENTE"</c>). Eso
    /// ataba la lógica a un texto que se puede editar desde Configuración → Categorías y
    /// Puestos: renombrar una categoría apagaba en silencio la funcionalidad que la
    /// buscaba. Comparando por id, renombrar es inofensivo.
    ///
    /// La categoría es el campo de LÓGICA, y desde 2026-08-21 se llega a ella por el puesto:
    /// <c>workers.puesto_id → puesto.categoria_id</c>. Lo que nunca decide nada es el NOMBRE
    /// del puesto — es texto libre y editable —, pero la categoría a la que ese puesto
    /// pertenece sí.
    /// </summary>
    public static class CategoriaIds
    {
        /// <summary>
        /// Habilitación la usa para el ítem Vida Ley (los practicantes no lo llevan).
        /// Ojo: no es el único eje de "es practicante" — Reclutamiento usa
        /// <see cref="CategoriaMaestraIds.PracticantePrePro"/>, que es el vínculo laboral
        /// de la Data Maestra de GTH. Los dos criterios conviven a propósito por ahora.
        /// </summary>
        public const int Practicante = 4;

        /// <summary>Lecciones Aprendidas (alcance por proyecto) y Desempeño de Supervisores.</summary>
        public const int Residente = 8;

        /// <summary>
        /// Gestión de Salidas (ve su gerencia completa y puede aprobar sus propias
        /// solicitudes) y Revisores de Áreas.
        /// No incluye a <see cref="GerenteGeneral"/>, <see cref="GerenteAdministracionFinanzas"/>
        /// ni SUB GERENTE: son categorías distintas y hoy quedan fuera de esas reglas.
        /// </summary>
        public const int Gerente = 11;

        /// <summary>
        /// Revisores de Áreas (ve su propia área), aprobador de salidas y jefatura de
        /// Lecciones Aprendidas.
        /// </summary>
        public const int Jefe = 17;

        /// <summary>Aprobador de salidas (segundo en el walk-up, después del Jefe).</summary>
        public const int SubGerente = 29;

        /// <summary>
        /// Revisores de Áreas: ve su propia área. No incluye a
        /// <see cref="CoordinadorSsoma"/>, que es una categoría aparte.
        /// </summary>
        public const int Coordinador = 22;

        /// <summary>Destinatario del Flash Report de accidentes.</summary>
        public const int Medico = 26;

        /// <summary>Charlas: quién cuenta como staff supervisor.</summary>
        public const int Supervisor = 37;

        /// <summary>Destinatario del Flash Report de accidentes.</summary>
        public const int GerenteGeneral = 39;

        /// <summary>Destinatario del Flash Report de accidentes.</summary>
        public const int GerenteAdministracionFinanzas = 40;

        /// <summary>Desempeño de Supervisores: puede ocultar/mostrar tarjetas y empresas.</summary>
        public const int CoordinadorSsoma = 41;

        /// <summary>
        /// Personal de planilla que no cae en ninguna categoría específica. Creada en
        /// <c>Migrations_Manual/2026-08-13_categoria_visible_solicitud_personal.sql</c> con
        /// id explícito para mantener la paridad dev/prod. Todavía no la mira ninguna regla:
        /// está acá porque su id es fijo por diseño y porque se espera darle lógica.
        ///
        /// No confundir con <see cref="CategoriaMaestraIds.Empleado"/>, que es el tipo de
        /// vínculo laboral de la Data Maestra de GTH: otro eje, otra tabla.
        /// </summary>
        public const int Empleado = 42;

        /// <summary>
        /// Tesorería. Gestión de Salidas la exige JUNTO con el rol
        /// <c>Roles.Tesorero</c>: tener el rol sin este puesto no abre la pantalla, y esta
        /// categoría sin el rol tampoco.
        ///
        /// El id lo fijó producción (ahí se creó primero); dev se alineó con
        /// <c>Migrations_Manual/_dev_alinear_categorias_roles_con_prod.sql</c>, que de paso trajo
        /// las otras categorías que prod había creado en el medio (43 ABOGADO, 44 ALMACENERO,
        /// 45 TESORERÍA). No renumerar: es la constante de la que cuelga toda la regla.
        /// </summary>
        public const int Tesorero = 46;

        // ── Categorías de las que depende una regla guardada en DATA ────────────
        // No aparecen en ninguna comparación de C#: las nombra
        // `ss_item_trabajador.aplica_categoria` / `.excluye_categoria_contratista`,
        // que son CSV de NOMBRES editables desde Habilitación → Reglas. Están acá
        // para que se sepa que no son categorías libres: si se renombran o se sacan
        // del catálogo, la regla deja de alcanzar a esa gente en silencio y no hay
        // código que grepear para descubrirlo.
        //
        // La normalización pendiente es que esa tabla referencie categoria_id (tabla
        // puente) en vez de guardar nombres; mientras tanto, esto es documentación
        // ejecutable — ver `ReglasPorData`.

        /// <summary>Regla "Entrevista con Residente o Producción".</summary>
        public const int Operador = 21;

        /// <summary>Regla "Entrevista con Residente o Producción".</summary>
        public const int Rigger = 10;

        /// <summary>Regla "Entrevista con Residente o Producción".</summary>
        public const int Vigia = 31;

        /// <summary>Reglas "Entrevista con el Jefe Corporativo SSOMA" y "CarnetRetcc".</summary>
        public const int Prevencionista = 35;

        /// <summary>Regla "Entrevista con el Área de Calidad".</summary>
        public const int Capataz = 36;

        /// <summary>
        /// Categorías nombradas por las reglas de entregables de Habilitación (data, no
        /// código). Súmalas a las que tienen lógica en C# antes de decidir que una
        /// categoría "no se usa en ningún lado".
        /// </summary>
        public static readonly int[] ReglasPorData =
            { Operador, Rigger, Vigia, Prevencionista, Capataz, Supervisor, Residente };

        /// <summary>
        /// Categorías cuyo trabajador puede ver su propia área en Revisores de Áreas.
        /// </summary>
        public static readonly int[] ConVistaDeSuArea = { Jefe, Coordinador, Gerente };

        /// <summary>
        /// Categorías que pueden aprobar la salida de un trabajador regular, en el orden
        /// en que las busca el walk-up por el árbol de áreas (ApproverResolver, regla C).
        /// El índice define la prioridad, así que el orden importa.
        /// </summary>
        public static readonly int[] AprobadoresWalkUp = { Jefe, SubGerente, Coordinador };

        /// <summary>
        /// Categorías que cuentan como "jefatura" en la configuración de recordatorios de
        /// Lecciones Aprendidas (quién aparece y puede activarse en esa sección).
        /// Independiente de quién PUEDE aprobar una lección, que es solo el jefe asignado
        /// a mano en <c>workers.worker_lesson_jefe_id</c>.
        /// </summary>
        public static readonly int[] Jefaturas = { Jefe, Coordinador, Residente };

        /// <summary>
        /// Categorías que reciben el Flash Report de accidentes por sí mismas (además de
        /// las que entran por área/subárea/jefatura).
        /// </summary>
        public static readonly int[] DestinatariosFlashReport =
            { Medico, GerenteGeneral, GerenteAdministracionFinanzas };
    }
}
