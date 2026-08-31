namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Candidato de la long list de un requerimiento (tabla <c>gth_candidato</c>). GTH carga los
    /// CVs previamente filtrados y, por cada uno, registra el nombre y un comentario interno; el
    /// CV se sube a SharePoint. El puesto no se captura: es el del requerimiento y se guarda como
    /// snapshot. El solicitante los revisa desde su vista para aprobar/rechazar.
    ///
    /// Las columnas <see cref="ExperienciaAnios"/>, <see cref="Disponibilidad"/> y
    /// <see cref="GthCanalPublicacionId"/> quedaron OBSOLETAS: GTH pidió quitar esos campos de la
    /// carga de CVs. Se conservan por auditoría de los registros antiguos, pero ya no se escriben
    /// ni se sirven al frontend.
    /// </summary>
    public class GthCandidato
    {
        public int GthCandidatoId { get; set; }

        /// <summary>FK al requerimiento (vacante) al que pertenece la long list.</summary>
        public int GthRequerimientoId { get; set; }

        /// <summary>Nombre y apellido del candidato (lo captura/corrige GTH; a futuro lo prellena la IA).</summary>
        public string Nombre { get; set; } = null!;

        /// <summary>
        /// Puesto del requerimiento al momento de cargar la long list (snapshot, para que el
        /// solicitante lo vea en la revisión). Ya no lo escribe GTH: se copia del requerimiento.
        /// </summary>
        public string? Puesto { get; set; }

        /// <summary>OBSOLETO (ver resumen de la clase): ya no se captura. Solo lectura histórica.</summary>
        public int? ExperienciaAnios { get; set; }

        /// <summary>OBSOLETO (ver resumen de la clase): ya no se captura. Solo lectura histórica.</summary>
        public string? Disponibilidad { get; set; }

        /// <summary>OBSOLETO (ver resumen de la clase): la fuente de reclutamiento ya no se captura.</summary>
        public int? GthCanalPublicacionId { get; set; }

        /// <summary>Comentario interno de GTH sobre el candidato.</summary>
        public string? Comentario { get; set; }

        // ── CV (obligatorio) subido a SharePoint ──────────────────────────────
        public string? CvNombre { get; set; }
        public string? CvUrl { get; set; }
        public string? CvItemId { get; set; }
        public string? CvDriveId { get; set; }

        /// <summary>
        /// Portafolio/anexos del candidato: los N archivos opcionales que GTH sube además del CV.
        /// Es navegación y no una consulta aparte para que la long list se guarde entera en un solo
        /// SaveChanges (una transacción): si los anexos se insertaran después, un fallo dejaría al
        /// candidato guardado sin ellos.
        /// </summary>
        public ICollection<GthCandidatoAnexo> Anexos { get; set; } = new List<GthCandidatoAnexo>();

        /// <summary>FK a <c>gth_candidato_estado</c>: estado de revisión (PENDIENTE por defecto).</summary>
        public int GthCandidatoEstadoId { get; set; }

        // ── Multitest (control informativo) ───────────────────────────────────
        /// <summary>
        /// true si GTH marcó que el candidato ya rindió el Multitest. La prueba se gestiona fuera
        /// de la plataforma: este check es informativo y no cambia el flujo por sí solo (RG del
        /// requerimiento funcional). Sí es requisito para habilitar el paso a entrevistas.
        /// </summary>
        public bool MultitestRealizado { get; set; }

        /// <summary>Momento en que se marcó/desmarcó el Multitest.</summary>
        public DateTimeOffset? MultitestDateTime { get; set; }

        /// <summary>Usuario de GTH que marcó/desmarcó el Multitest.</summary>
        public int? MultitestUserId { get; set; }

        // ── Decisión del solicitante sobre el candidato de la long list ───────
        /// <summary>
        /// Momento en que el solicitante aprobó o rechazó a este candidato al revisar la long
        /// list. Null mientras no lo haya decidido. No se lee de <c>UpdatedDateTime</c> porque
        /// enviar una long list nueva da de baja la anterior y pisa esa fecha — justo lo que pasa
        /// cuando el solicitante rechaza a todos, que es el caso que muestra el historial.
        /// </summary>
        public DateTimeOffset? DecisionDateTime { get; set; }

        /// <summary>Usuario del área solicitante que decidió sobre el candidato en la long list.</summary>
        public int? DecisionUserId { get; set; }

        /// <summary>Orden del candidato dentro de la long list (posición de carga).</summary>
        public int Orden { get; set; }

        /// <summary>
        /// A qué long list del requerimiento pertenece el candidato (1 = la primera). Cuando el
        /// solicitante rechaza a todos, el requerimiento vuelve a LONG_LIST y la siguiente long
        /// list entra con el número siguiente: las anteriores se conservan como historial, con
        /// <see cref="State"/> en true y su rechazo registrado en el estado del candidato.
        ///
        /// La long list vigente es el mayor número entre las filas vivas del requerimiento
        /// (ver <c>CandidatosVigentes</c> en el repositorio). No se usa <see cref="State"/> para
        /// distinguir vueltas: <c>state = false</c> significa eliminado del sistema, y un
        /// candidato rechazado no está eliminado.
        /// </summary>
        public int NumeroLongList { get; set; } = 1;

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
