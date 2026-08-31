namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Body del PUT que guarda la evaluación de la entrevista de un candidato: los tres
    /// comentarios del informe de finalista, los tres obligatorios. El resultado (PASO / NO_PASO)
    /// no viaja aquí: lo define el correo de agradecimiento.
    ///
    /// Viaja como el campo <c>data</c> de un multipart: los archivos del informe (opcionales) van
    /// en el mismo envío como form files.
    /// </summary>
    public class EvaluacionGuardarDto
    {
        /// <summary>Resultado de la entrevista (qué se observó). Obligatorio.</summary>
        public string? ComentarioEntrevista { get; set; }

        /// <summary>Informe psicotécnico del candidato. Obligatorio.</summary>
        public string? ComentarioPsicotecnico { get; set; }

        /// <summary>Recomendación de GTH al área solicitante. Obligatorio.</summary>
        public string? ComentarioRecomendacion { get; set; }

        /// <summary>
        /// Códigos de <c>gth_evaluacion_archivo_tipo</c> cuyo archivo hay que dar de baja (GTH lo
        /// quitó de la pantalla). Los que no vengan acá ni lleguen como archivo nuevo se quedan
        /// como estaban: guardar de nuevo el informe no borra lo ya subido.
        /// </summary>
        public List<string>? ArchivosQuitados { get; set; }
    }

    /// <summary>
    /// Archivo del informe que llega en el multipart, ya enlazado con su tipo del catálogo por la
    /// clave con la que lo mandó el frontend.
    /// </summary>
    public class EvaluacionArchivoSubidaDto
    {
        /// <summary>Código de <c>gth_evaluacion_archivo_tipo</c> (INFORME_FINAL, …).</summary>
        public string TipoCodigo { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Archivo del informe ya subido a SharePoint (urls resueltas por el servicio), listo para
    /// persistir en <c>gth_candidato_evaluacion_archivo</c>.
    /// </summary>
    public class EvaluacionArchivoPersistDto
    {
        /// <summary>Código de <c>gth_evaluacion_archivo_tipo</c>.</summary>
        public string TipoCodigo { get; set; } = string.Empty;

        /// <summary>Nombre con el que quedó en SharePoint.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Nombre con el que GTH lo subió (el que se muestra y viaja en el correo).</summary>
        public string? NombreOriginal { get; set; }

        public string? Url { get; set; }
        public string? ItemId { get; set; }
        public string? DriveId { get; set; }
    }

    /// <summary>Archivo ya subido del informe, como lo ven GTH, el solicitante y el correo.</summary>
    public class EvaluacionArchivoDto
    {
        public int ArchivoId { get; set; }

        /// <summary>Código del documento (INFORME_FINAL / EVALUACION_CONOCIMIENTOS).</summary>
        public string TipoCodigo { get; set; } = string.Empty;

        /// <summary>Nombre visible del documento ("Informe final").</summary>
        public string TipoNombre { get; set; } = string.Empty;

        /// <summary>Nombre del archivo con el que GTH lo subió.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Link al archivo en SharePoint. Null si la subida no dejó url.</summary>
        public string? Url { get; set; }
    }

    /// <summary>Evaluación de la entrevista de un candidato, como la muestran GTH y el solicitante.</summary>
    public class EvaluacionResumenDto
    {
        public string? ComentarioEntrevista { get; set; }
        public string? ComentarioPsicotecnico { get; set; }
        public string? ComentarioRecomendacion { get; set; }

        /// <summary>
        /// Archivos del informe (informe final y resultados de la evaluación de conocimientos), en
        /// el orden del catálogo. Vacío si GTH no subió ninguno: los dos son opcionales.
        /// </summary>
        public List<EvaluacionArchivoDto> Archivos { get; set; } = new();

        /// <summary>Resultado alcanzado: PENDIENTE / PASO / NO_PASO (código estable).</summary>
        public string ResultadoCodigo { get; set; } = string.Empty;
        public string ResultadoNombre { get; set; } = string.Empty;

        /// <summary>Correo al que se envió el agradecimiento (null si no se envió).</summary>
        public string? AgradecimientoCorreo { get; set; }

        /// <summary>Momento del envío del agradecimiento (hora de Perú). Null si no se envió.</summary>
        public DateTime? AgradecimientoEnviadoEn { get; set; }

        /// <summary>
        /// Momento de la decisión final del área solicitante sobre el finalista (hora de Perú).
        /// Null mientras no haya decidido.
        /// </summary>
        public DateTime? DecididoEn { get; set; }
    }

    /// <summary>
    /// Evaluación guardada más la fase a la que avanzó el requerimiento, si avanzó. Guardar el
    /// informe es enviar al finalista, y eso pasa el requerimiento de ENTREVISTAS a
    /// SELECCION_JEFATURA (la decisión queda del lado del área solicitante).
    /// </summary>
    public class EvaluacionGuardadaDto
    {
        public EvaluacionResumenDto Evaluacion { get; set; } = new();

        /// <summary>
        /// Id de la fila de <c>gth_candidato_evaluacion</c>. Lo usa el servicio para colgarle los
        /// archivos recién subidos a SharePoint; no viaja al frontend.
        /// </summary>
        public int EvaluacionId { get; set; }

        /// <summary>Código del requerimiento: nombra la carpeta de SharePoint donde van los archivos.</summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Fase nueva del requerimiento. Null si la fase no se movió.</summary>
        public string? EstadoCodigo { get; set; }
        public string? EstadoNombre { get; set; }

        /// <summary>
        /// Datos para el correo que le avisa al solicitante que tiene un finalista por decidir
        /// (tipo FINALISTA_ENVIO). Viene siempre: el envío es best-effort y lo decide el servicio.
        /// </summary>
        public FinalistaEnvioContextoDto Envio { get; set; } = new();
    }

    /// <summary>
    /// Datos que necesita el servicio para armar el correo de "finalista enviado al solicitante".
    /// El destinatario principal es SIEMPRE el solicitante que registró la solicitud; la
    /// configuración de Reclutamiento solo aporta principales adicionales y copias.
    /// </summary>
    public class FinalistaEnvioContextoDto
    {
        public int RequerimientoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }

        /// <summary>Correo del solicitante. Null si el usuario no tiene uno cargado.</summary>
        public string? SolicitanteEmail { get; set; }

        public string CandidatoNombre { get; set; } = string.Empty;

        /// <summary>El informe que GTH acaba de guardar (los tres comentarios).</summary>
        public EvaluacionResumenDto Evaluacion { get; set; } = new();
    }

    /// <summary>Resultado de guardar la evaluación o de enviar el correo de agradecimiento.</summary>
    public class EvaluacionAccionResultDto
    {
        public string Message { get; set; } = string.Empty;
        public EvaluacionResumenDto Evaluacion { get; set; } = new();

        /// <summary>
        /// Fase nueva del requerimiento cuando la acción la movió (guardar la evaluación lo pasa a
        /// SELECCION_JEFATURA). Null si la fase quedó igual — el agradecimiento nunca la mueve.
        /// </summary>
        public string? EstadoCodigo { get; set; }
        public string? EstadoNombre { get; set; }
    }

    /// <summary>Datos que necesita el servicio para armar el correo de agradecimiento.</summary>
    public class AgradecimientoEnvioContextoDto
    {
        public string CandidatoNombre { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Correo del postulante al que se envía el agradecimiento.</summary>
        public string Correo { get; set; } = string.Empty;

        public EvaluacionResumenDto Resumen { get; set; } = new();
    }

    /// <summary>
    /// Informe de finalistas de un requerimiento tal como lo ve el área solicitante (modal
    /// "Finalistas enviados por GTH"): cabecera + finalistas con su evaluación y su CV.
    /// </summary>
    public class RevisionFinalistasDto
    {
        public int RequerimientoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>Finalistas ordenados alfabéticamente por nombre.</summary>
        public List<FinalistaDto> Finalistas { get; set; } = new();

        /// <summary>
        /// Área a la que entra el seleccionado: la de DESTINO del puesto del requerimiento
        /// (<c>puesto.area_destino_scope_id</c>). Define el <c>area_scope_id</c> de su ficha de
        /// pre-ingreso, y con ella se resuelve su jefatura.
        ///
        /// Es informativa: el solicitante NO la elige, la decide el puesto que pidió. Se manda
        /// para que la pantalla pueda decirle a dónde va el candidato que aprueba.
        ///
        /// Null cuando el puesto no tiene destino (el padrón de GTH solo cubrió personal de
        /// oficina): se cae al área del solicitante, que es lo que se usaba antes.
        ///
        /// El nombre es el del nodo, no la rama completa: si el puesto es de un área estándar ese
        /// nodo YA es el primero estándar de su rama, y si es de un nodo de gerencia (el caso de
        /// los puestos de categoría gerente, que cuelgan de ahí) el nombre de la gerencia es el
        /// que corresponde. En ninguno de los dos casos hay ancestros que mostrar.
        /// </summary>
        public OpcionDto? AreaDestino { get; set; }
    }

    /// <summary>
    /// Body del POST con la decisión final del área solicitante sobre un finalista: aprobarlo
    /// (pasa al EMO de ingreso, el último paso antes de que GTH cierre el proceso) o
    /// rechazarlo (se le envía el correo de agradecimiento).
    ///
    /// El área a la que entra el seleccionado NO viaja acá: la decide el puesto del
    /// requerimiento (<c>puesto.area_destino_scope_id</c>) y la resuelve el backend.
    /// </summary>
    public class FinalistaDecisionDto
    {
        public int CandidatoId { get; set; }

        /// <summary>true = aprobar y cerrar el proceso; false = rechazar al finalista.</summary>
        public bool Aprobado { get; set; }
    }

    /// <summary>Resultado de registrar la decisión final sobre un finalista.</summary>
    public class FinalistaDecisionResultDto
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>Estado en el que quedó el requerimiento tras la decisión.</summary>
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>true si la decisión fue aprobar (el proceso de reclutamiento queda cerrado).</summary>
        public bool Aprobado { get; set; }

        /// <summary>
        /// true si con este rechazo ya no queda ningún finalista en carrera: el requerimiento
        /// vuelve a LONG_LIST para que GTH envíe una nueva long list.
        /// </summary>
        public bool TodosRechazados { get; set; }

        /// <summary>Nombre del finalista decidido (para el mensaje al usuario).</summary>
        public string CandidatoNombre { get; set; } = string.Empty;

        /// <summary>
        /// Ficha de pre-ingreso creada para el seleccionado en <c>workers</c>. Es el id con el que
        /// el front arma el enlace a SSOMA · Salud Ocupacional · EMOs para programarle el examen
        /// de ingreso. Null cuando se rechazo al finalista, o cuando el candidato todavia no tiene
        /// formulario del postulante aprobado (no hay ficha en <c>person</c> de donde colgarla).
        /// </summary>
        public int? WorkerId { get; set; }
    }

    /// <summary>Datos que necesita el servicio para armar los correos de la decisión final.</summary>
    public class FinalistaDecisionContextoDto
    {
        public FinalistaDecisionResultDto Resultado { get; set; } = new();

        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }

        /// <summary>Nombre de quien tomó la decisión (para el correo a GTH).</summary>
        public string? SolicitanteNombre { get; set; }

        /// <summary>
        /// Correo del finalista rechazado, al que se le envía el fin de proceso. Vacío cuando la
        /// decisión fue aprobarlo (no se le escribe al candidato desde aquí).
        /// </summary>
        public string CandidatoCorreo { get; set; } = string.Empty;

        /// <summary>
        /// Finalistas que se quedaron sin elegir al aprobar a otro: el puesto ya quedó cubierto,
        /// así que se cierran como rechazados y reciben el mismo correo de fin de proceso. Vacío
        /// cuando la decisión fue rechazar (ahí el proceso sigue abierto para los demás).
        /// </summary>
        public List<FinalistaNoElegidoDto> NoElegidos { get; set; } = new();
    }

    /// <summary>
    /// Finalista que quedó fuera porque el solicitante eligió a otro. Solo lo que hace falta para
    /// escribirle: su nombre y el correo con el que se le contactó durante el proceso.
    /// </summary>
    public class FinalistaNoElegidoDto
    {
        public int CandidatoId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Correo del candidato. Vacío si no tiene uno cargado (no se le escribe).</summary>
        public string Correo { get; set; } = string.Empty;
    }

    /// <summary>Un finalista con el informe que GTH registró tras su entrevista.</summary>
    public class FinalistaDto
    {
        public int CandidatoId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Puesto del requerimiento (snapshot), no un dato capturado por candidato.</summary>
        public string? Puesto { get; set; }

        /// <summary>Nombre y link del CV que cargó GTH en la long list (para "Ver CV completo").</summary>
        public string? CvNombre { get; set; }
        public string? CvUrl { get; set; }

        /// <summary>
        /// Nombre y link del CV documentado que el propio postulante adjuntó en su formulario. Null
        /// si no llegó a subirlo. Se sirve junto al de GTH: el solicitante decide viendo los dos.
        /// </summary>
        public string? CvPostulanteNombre { get; set; }
        public string? CvPostulanteUrl { get; set; }

        /// <summary>Evaluación registrada por GTH (comentarios del informe).</summary>
        public EvaluacionResumenDto Evaluacion { get; set; } = new();
    }
}
