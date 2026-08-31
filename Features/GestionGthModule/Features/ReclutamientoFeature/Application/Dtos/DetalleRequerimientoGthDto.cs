using Abril_Backend.Shared.Services;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Detalle de un requerimiento para la vista de GTH (modal del ojo de la bandeja):
    /// cabecera + asignación interna actual + catálogos de los desplegables (con cupos por
    /// razón social) + canales de publicación, todo en una sola petición.
    /// </summary>
    public class DetalleRequerimientoGthDto
    {
        public int RequerimientoId { get; set; }

        /// <summary>Código REQ-AAAA-NNNN.</summary>
        public string Codigo { get; set; } = string.Empty;

        public string Puesto { get; set; } = string.Empty;

        /// <summary>Área solicitante (snapshot al registrar).</summary>
        public string? Area { get; set; }

        /// <summary>
        /// Área a la que ENTRA el contratado: la de destino del puesto que se pidió
        /// (<c>puesto.area_destino_scope_id</c>). No siempre es la del solicitante — la Gerencia
        /// Inmobiliaria pide un INGENIERO RESIDENTE y el residente entra a Residencia.
        ///
        /// Null cuando el puesto no tiene destino (los de obra): entra al área del solicitante.
        /// </summary>
        public string? AreaDestino { get; set; }

        /// <summary>Proyecto/obra destino de la vacante.</summary>
        public string? ProyectoObra { get; set; }

        /// <summary>Tipo de requerimiento (Nuevo / Reemplazo).</summary>
        public string TipoRequerimiento { get; set; } = string.Empty;

        /// <summary>
        /// Trabajador al que reemplaza la vacante. Solo lo traen los requerimientos de tipo
        /// Reemplazo registrados desde que se pide ese dato; null en el resto.
        /// </summary>
        public string? TrabajadorReemplazado { get; set; }

        /// <summary>
        /// Salario bruto mensual que el área declaró para la vacante, en soles. Es el que Gerencia
        /// General aprobó, así que es el punto de partida de la oferta. Null en los requerimientos
        /// anteriores a que se pidiera el dato.
        /// </summary>
        public decimal? SalarioBrutoMensual { get; set; }

        /// <summary>
        /// true = ingreso directo <b>FFT</b>: el requerimiento nace con su candidato puesto y ya
        /// seleccionado, y se salta publicación, revisión de CV, long list, formulario del
        /// postulante, entrevistas y finalistas. Lo único que le queda a GTH es programarle el EMO
        /// de ingreso. El modal usa esto para no ofrecer los pasos que no existen en este flujo.
        /// </summary>
        public bool EsFft { get; set; }

        /// <summary>Nombre del candidato FFT que nombró el solicitante. Null cuando no es FFT.</summary>
        public string? FftCandidatoNombre { get; set; }

        /// <summary>
        /// Documento del candidato FFT, con su tipo: «DNI 12345678». GTH lo necesita para saber de
        /// quién se trata cuando hay nombres parecidos, y es el documento con el que el candidato ya
        /// quedó registrado en la base maestra al pedirse la vacante. Null cuando no es FFT o cuando
        /// el requerimiento es anterior a que se pidiera el dato.
        ///
        /// Viaja ya armado y no en dos campos porque la pantalla lo muestra en una sola línea: el
        /// tipo suelto no le sirve de nada.
        /// </summary>
        public string? FftCandidatoDocumento { get; set; }

        /// <summary>Vacantes de este requerimiento (cada vacante genera un requerimiento → 1).</summary>
        public int Vacantes { get; set; } = 1;

        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>Asignación interna de GTH actual del requerimiento.</summary>
        public AsignacionGthDto Asignacion { get; set; } = new();

        // ── Catálogos de los desplegables ─────────────────────────────────────
        /// <summary>Miembros GTH que pueden ser responsables del proceso.</summary>
        public List<OpcionDto> Responsables { get; set; } = new();

        public List<TipoProcesoOpcionDto> TiposProceso { get; set; } = new();

        public List<OpcionDto> Prioridades { get; set; } = new();

        /// <summary>Razones sociales activas (contributor.operativo = true) con sus cupos.</summary>
        public List<RazonSocialCupoDto> RazonesSociales { get; set; } = new();

        /// <summary>Canales de publicación con su estado de publicación para este requerimiento.</summary>
        public List<CanalPublicacionDto> Canales { get; set; } = new();

        /// <summary>Lugares donde se puede citar al candidato (desplegable de programación de entrevistas).</summary>
        public List<OpcionDto> LugaresEntrevista { get; set; } = new();

        /// <summary>
        /// Candidatos APROBADOS por el solicitante (solo relevante cuando el requerimiento ya está
        /// en LONG_LIST_APROBADA). Alimentan la vista de GTH "Long list aprobada": formulario del
        /// postulante y control del Multitest, y luego la programación de entrevistas. Vacío en
        /// fases anteriores.
        /// </summary>
        public List<CandidatoAprobadoDto> CandidatosAprobados { get; set; } = new();

        /// <summary>
        /// Candidatos RECHAZADOS a lo largo del proceso, con la etapa del rechazo, incluidos los de
        /// long lists anteriores. Alimenta la sección «Historial de candidatos rechazados»: cuando
        /// el solicitante rechaza a todos y el requerimiento vuelve a LONG_LIST, es lo que GTH mira
        /// para no volver a presentar a los mismos.
        /// </summary>
        public List<CandidatoRechazadoDto> CandidatosRechazados { get; set; } = new();

        /// <summary>
        /// Quién obtuvo el puesto: el candidato que el solicitante aprobó en la decisión final.
        /// Null mientras el proceso no se haya cerrado con un seleccionado.
        /// </summary>
        public SeleccionadoDto? Seleccionado { get; set; }
    }

    /// <summary>Candidato aprobado por el solicitante, tal como lo ve GTH en la fase "Long list aprobada".</summary>
    public class CandidatoAprobadoDto
    {
        public int CandidatoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Puesto { get; set; }

        /// <summary>Nombre y link del CV que GTH cargó en la long list de este candidato.</summary>
        public string? CvNombre { get; set; }
        public string? CvUrl { get; set; }

        /// <summary>
        /// Nombre y link del CV documentado que adjuntó el propio postulante al enviar su
        /// formulario. Null mientras no lo haya enviado (o en los formularios anteriores a que se
        /// pidiera el archivo). Va junto al de GTH porque el sentido de pedirlo es comparar los dos.
        /// </summary>
        public string? CvPostulanteNombre { get; set; }
        public string? CvPostulanteUrl { get; set; }

        /// <summary>Estado del formulario de información del postulante de este candidato (null si GTH aún no lo envió).</summary>
        public CandidatoFormularioResumenDto? Formulario { get; set; }

        /// <summary>
        /// Aviso para GTH cuando el documento que declaró el postulante ya existe en la base:
        /// aprobar su formulario actualizaría esa ficha en vez de crear una nueva, y si es de un
        /// trabajador actual no se puede aprobar. Va acá además de en el modal porque los botones
        /// Aprobar/Rechazar también están en esta ficha, sin abrirlo. Null cuando no coincide con
        /// nada, que es el caso normal.
        /// </summary>
        public FormularioCoincidenciaDto? Coincidencia { get; set; }

        /// <summary>true si GTH ya marcó el check informativo del Multitest de este candidato.</summary>
        public bool MultitestRealizado { get; set; }

        /// <summary>
        /// Correo del postulante al que se enviará la invitación a la entrevista: el que declaró en
        /// el formulario y, si no lo declaró, aquel al que GTH le envió el enlace. Null si aún no
        /// hay formulario.
        /// </summary>
        public string? CorreoContacto { get; set; }

        /// <summary>Entrevista programada de este candidato (null si aún no se programó).</summary>
        public EntrevistaResumenDto? Entrevista { get; set; }

        /// <summary>
        /// Evaluación de la entrevista (comentarios del informe y resultado). Null mientras GTH
        /// no registre nada ni envíe el correo de agradecimiento.
        /// </summary>
        public EvaluacionResumenDto? Evaluacion { get; set; }
    }

    /// <summary>Asignación interna de GTH de un requerimiento (todas opcionales/null = sin asignar).</summary>
    public class AsignacionGthDto
    {
        /// <summary>Id de gth_responsable_proceso (responsable del proceso).</summary>
        public int? ResponsableId { get; set; }

        /// <summary>Id de gth_tipo_proceso (tipo de proceso y SLA).</summary>
        public int? TipoProcesoId { get; set; }

        /// <summary>Id de gth_prioridad (prioridad interna).</summary>
        public int? PrioridadId { get; set; }

        /// <summary>Id de contributor (razón social activa).</summary>
        public int? ContributorId { get; set; }
    }

    /// <summary>Opción del desplegable "Tipo de proceso y SLA".</summary>
    public class TipoProcesoOpcionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int SlaDias { get; set; }
    }

    /// <summary>
    /// Canal de publicación de vacantes y su estado para el requerimiento consultado. No hay
    /// integración con las APIs de los portales: marcar el canal solo deja registro de dónde se
    /// publicó, la publicación siempre la hace GTH manualmente.
    /// </summary>
    public class CanalPublicacionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>true = la vacante ya está registrada como publicada en este canal.</summary>
        public bool Publicado { get; set; }
    }

    /// <summary>Body del PATCH que guarda la asignación interna de GTH (reemplaza los 4 campos).</summary>
    public class AsignacionGthUpdateDto
    {
        public int? ResponsableId { get; set; }
        public int? TipoProcesoId { get; set; }
        public int? PrioridadId { get; set; }
        public int? ContributorId { get; set; }
    }

    /// <summary>Body del PUT que registra los canales donde se publicó la vacante.</summary>
    public class PublicacionesUpdateDto
    {
        public List<int> CanalIds { get; set; } = new();
    }

    /// <summary>
    /// Estado resultante de una transición del pipeline (respuesta de registrar la publicación
    /// y de iniciar la revisión de CV): el frontend actualiza el badge y las secciones del
    /// modal sin volver a pedir el detalle.
    /// </summary>
    public class EstadoRequerimientoResultDto
    {
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;
    }
}
