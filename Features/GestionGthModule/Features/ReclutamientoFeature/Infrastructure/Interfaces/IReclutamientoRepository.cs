using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces
{
    public interface IReclutamientoRepository
    {
        Task<ReclutamientoFormDataDto> GetFormData(int? userId);

        /// <summary>
        /// Catálogo de tipos de documento (DNI / CE) con su código estable. Lo necesita la
        /// validación de una solicitud con vacantes FFT: el largo del número lo decide el tipo, y
        /// el código vive en la base. Es una tabla de dos filas y solo se consulta cuando hay FFT.
        /// </summary>
        Task<List<TipoDocumentoOpcionDto>> GetTiposDocumento();

        /// <summary>
        /// Área (nombre + area_scope) del worker vinculado al usuario. (null, null, null) si no
        /// resuelve. El nombre se deriva del árbol de áreas — el nodo de <c>area_scope_id</c> o su
        /// primer ancestro que no sea gerencia — y no de <c>workers.area</c>, texto congelado.
        /// </summary>
        Task<(string? AreaNombre, int? AreaScopeId, int? WorkerId)> ResolveSolicitante(int userId);

        /// <summary>
        /// Link de la carpeta de SharePoint donde se suben los sustentos (singleton
        /// gth_sustento_folder, primera fila vigente). Se define por BD: dev y prod
        /// apuntan a bibliotecas distintas. null si no está configurada.
        /// </summary>
        Task<string?> GetSustentoFolderUrl();

        /// <summary>
        /// Persiste la solicitud + un requerimiento por vacante, generando el código
        /// REQ-AAAA-NNNN correlativo por año. Devuelve el id de la solicitud y los códigos creados.
        ///
        /// Cada vacante nace donde le toca: las normales esperando su aprobación, y las de ingreso
        /// directo (<c>es_fft</c>) ya en manos de GTH —con su candidato seleccionado y su ficha de
        /// pre-ingreso abierta— porque no las aprueba nadie. Lo decide la vacante y no la solicitud:
        /// una misma solicitud puede traer de las dos.
        /// </summary>
        Task<SolicitudPersonalCreateResultDto> Create(
            GthSolicitud solicitud, List<VacanteCreateDto> vacantes, int? userId);

        /// <summary>
        /// Bandeja de la vista de GTH: tarjeta "En proceso" + tabla de solicitudes de contratación de
        /// toda la organización (todos los requerimientos vigentes), en 1 roundtrip.
        /// </summary>
        Task<BandejaReclutamientoDto> GetBandeja();

        /// <summary>
        /// Actualiza la prioridad de un requerimiento vigente. Lanza <see cref="Abril_Backend.Application.Exceptions.AbrilException"/>
        /// 400 si la prioridad no es válida y 404 si el requerimiento no existe.
        /// </summary>
        Task UpdatePrioridad(int requerimientoId, int prioridadId, int? userId);

        /// <summary>
        /// Detalle de seguimiento de un requerimiento (cabecera + fases del pipeline con su estado ya
        /// calculado), en 1 roundtrip. Devuelve null si el requerimiento no existe o queda fuera del
        /// alcance del usuario.
        /// </summary>
        Task<SeguimientoDto?> GetSeguimiento(int requerimientoId, SolicitudPersonalScope scope);

        /// <summary>
        /// Detalle de un requerimiento para la vista de GTH (modal del ojo): cabecera + asignación
        /// interna + catálogos con cupos + canales de publicación, en 1 roundtrip. Devuelve null si
        /// el requerimiento no existe.
        /// </summary>
        Task<DetalleRequerimientoGthDto?> GetDetalleGth(int requerimientoId);

        /// <summary>
        /// Guarda la asignación interna de GTH (responsable, tipo de proceso, prioridad y razón
        /// social) reemplazando los 4 campos. Lanza <see cref="Abril_Backend.Application.Exceptions.AbrilException"/>
        /// 400 si algún id no es válido y 404 si el requerimiento no existe.
        /// </summary>
        Task UpdateAsignacionGth(int requerimientoId, AsignacionGthUpdateDto dto, int? userId);

        /// <summary>
        /// Reemplaza el conjunto de canales donde se registró la publicación de la vacante
        /// (reconciliación: agrega los nuevos, da de baja los quitados) y avanza el
        /// requerimiento a la fase PUBLICACION si aún no la alcanzó. Devuelve el estado resultante.
        /// </summary>
        Task<EstadoRequerimientoResultDto> ReplacePublicaciones(int requerimientoId, List<int> canalIds, int? userId);

        /// <summary>
        /// Avanza el requerimiento de la fase PUBLICACION a LONG_LIST (inicio de la revisión de CV).
        /// Idempotente si ya está en Long list o más adelante. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 400 si la vacante aún
        /// no está publicada y 404 si el requerimiento no existe. Devuelve el estado resultante.
        /// </summary>
        Task<EstadoRequerimientoResultDto> IniciarRevisionCv(int requerimientoId, int? userId);

        /// <summary>
        /// Cabecera + estado del requerimiento para armar el correo de la long list (solo lectura,
        /// no cambia estado). Valida que la revisión de CV ya inició (fase LONG_LIST o posterior).
        /// Lanza <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 400 si aún no
        /// hay long list y 404 si el requerimiento no existe.
        /// </summary>
        Task<LongListEnvioContextoDto> GetLongListEnvioContexto(int requerimientoId);

        /// <summary>
        /// Persiste la long list (reemplaza los candidatos vigentes del requerimiento por el nuevo
        /// conjunto, con sus CVs ya subidos a SharePoint) y avanza el requerimiento a
        /// LONG_LIST_ENVIADA (idempotente si ya lo está). Se llama tras enviar el correo con éxito.
        /// Devuelve el estado resultante.
        /// </summary>
        Task<EstadoRequerimientoResultDto> GuardarLongListCandidatos(
            int requerimientoId, List<LongListCandidatoPersistDto> candidatos, int? userId);

        /// <summary>
        /// Panel de la vista del solicitante en 1 roundtrip: tarjetas de "Gestión de candidatos"
        /// (requerimientos con long list enviada, pendientes de revisar) + la tabla de solicitudes
        /// de vacante. Alcance por área, no por usuario:
        /// ver <see cref="SolicitudPersonalScope"/>.
        /// </summary>
        Task<SolicitantePanelDto> GetSolicitantePanel(SolicitudPersonalScope scope);

        /// <summary>
        /// Revisión de la long list de un requerimiento (cabecera + candidatos con su CV), en 1
        /// roundtrip. Devuelve null si el requerimiento no existe, queda fuera del alcance del
        /// usuario o su long list aún no fue enviada.
        /// </summary>
        Task<RevisionLongListDto?> GetRevisionLongList(int requerimientoId, SolicitudPersonalScope scope);

        /// <summary>
        /// Registra la decisión del solicitante sobre la long list (aprobar/rechazar por candidato) y
        /// avanza el requerimiento: a LONG_LIST_APROBADA si aprobó al menos uno, o de vuelta a LONG_LIST
        /// si rechazó a todos (para que GTH envíe una nueva long list; los rechazados quedan grabados).
        /// Scope: el área del usuario y solo si el requerimiento está en LONG_LIST_ENVIADA. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si no existe o está
        /// fuera de su alcance, 409 si ya no está pendiente de revisión y 400 si faltan decisiones.
        /// Devuelve el contexto para el correo (cabecera + candidatos con su decisión) además del
        /// estado resultante.
        /// </summary>
        Task<LongListDecisionContextoDto> RegistrarDecisionLongList(
            int requerimientoId, List<CandidatoDecisionDto> decisiones, SolicitudPersonalScope scope);

        /// <summary>
        /// Marca/desmarca el check informativo del Multitest de un candidato (con su trazabilidad).
        /// Lanza <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si el candidato
        /// no existe.
        /// </summary>
        Task SetMultitest(int candidatoId, bool realizado, int? userId);

        /// <summary>
        /// Avanza el requerimiento de LONG_LIST_APROBADA a ENTREVISTAS. Valida los requisitos del
        /// paso: Multitest marcado en todos los candidatos aprobados, todos sus formularios del
        /// postulante ya revisados (aprobados o rechazados) y al menos uno aprobado. Idempotente si
        /// ya está en Entrevistas o más adelante. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 400 si falta algún
        /// requisito y 404 si el requerimiento no existe. Devuelve el estado resultante.
        /// </summary>
        Task<EstadoRequerimientoResultDto> ContinuarAEntrevistas(int requerimientoId, int? userId);

        /// <summary>
        /// Retoma el proceso con un candidato del historial de rechazados y devuelve el
        /// requerimiento a la fase en la que se lo descartó: long list aprobada (rechazado en la
        /// revisión de CVs o en su formulario), entrevistas (descartado por GTH tras la cita) o
        /// selección de jefatura (rechazado por el solicitante en la decisión final).
        ///
        /// Es la salida de la fase EMO_NO_APTO, a la que se llega cuando el EMO de ingreso del
        /// seleccionado sale No Apto. Lanza <see cref="Abril_Backend.Application.Exceptions.AbrilException"/>
        /// 409 si el requerimiento no está en esa fase, 404 si el candidato no es de este
        /// requerimiento y 400 si no está rechazado o su rechazo fue el del propio EMO (a ese no se
        /// lo puede retomar: el examen médico no se revierte volviendo a elegirlo).
        /// </summary>
        /// <remarks>
        /// Devuelve además los datos del requerimiento y de su solicitante: el servicio le avisa a
        /// quien pidió la vacante con quién sigue el proceso, y ese contexto sale de la misma
        /// consulta en vez de un roundtrip aparte.
        /// </remarks>
        Task<RetomarCandidatoContextoDto> RetomarCandidatoRechazado(
            int requerimientoId, int candidatoId, int? userId);

        /// <summary>
        /// La otra salida de EMO_NO_APTO: descartar a todos los rechazados y volver a LONG_LIST para
        /// armar una long list nueva. No toca a los candidatos —siguen siendo el historial que GTH
        /// mira para no repetirlos— y la próxima carga de CVs entra como una vuelta nueva. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 409 si el requerimiento
        /// no está en esa fase.
        /// </summary>
        Task<EstadoRequerimientoResultDto> VolverALongListDesdeEmoNoApto(int requerimientoId, int? userId);

        /// <summary>
        /// Cierra el proceso de reclutamiento desde EMO_APTO / EMO_APTO_RESTRICCIONES: pasa el
        /// requerimiento a CERRADO, que es lo que hace aparecer al seleccionado en Onboarding como
        /// candidato por ingresar. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si el requerimiento
        /// no existe y 409 si no está en una de esas dos fases.
        /// </summary>
        Task<EstadoRequerimientoResultDto> CerrarProcesoDesdeEmoApto(int requerimientoId, int? userId);

        /// <summary>
        /// Programa (o reprograma) la entrevista de un candidato con formulario APROBADO: crea o
        /// actualiza su única fila vigente en <c>gth_entrevista</c> y resuelve el correo del
        /// postulante al que se envía la invitación. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si el candidato no
        /// existe y 400 si su formulario no está aprobado, si el lugar no es válido o si no tiene
        /// correo. Devuelve el contexto para armar el correo.
        /// </summary>
        Task<EntrevistaEnvioContextoDto> GuardarEntrevista(
            int candidatoId, DateOnly fecha, TimeOnly hora, int lugarId, int? userId,
            string nuevoToken);

        /// <summary>
        /// Registra la respuesta del candidato a su citación (CONFIRMADA / RECHAZADA) a partir del
        /// token público del correo. Idempotente: volver a pulsar el mismo botón deja todo igual y
        /// lo avisa con <c>YaHabiaRespondidoLoMismo</c> para no reenviarle a GTH un aviso repetido;
        /// pulsar el otro botón sí cambia la respuesta (el candidato se puede arrepentir). Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si el token no
        /// corresponde a ninguna entrevista vigente y 500 si falta el catálogo de respuestas.
        /// </summary>
        Task<EntrevistaRespuestaContextoDto> RegistrarRespuestaEntrevista(string token, string respuestaCodigo);

        /// <summary>
        /// Guarda la evaluación de la entrevista de un candidato (comentarios del informe),
        /// creando su única fila vigente en <c>gth_candidato_evaluacion</c> si no
        /// existía, y lo deja en PASO: guardar el informe es enviarlo como finalista, así que el
        /// requerimiento avanza de ENTREVISTAS a SELECCION_JEFATURA (la decisión pasa al área
        /// solicitante). No mueve la fase desde ninguna otra, para no retroceder un proceso que ya
        /// cerró o que volvió a long list. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si
        /// el candidato no existe, 400 si aún no se le envió la invitación a la entrevista y 400
        /// si ya no continúa (resultado NO_PASO: su evaluación quedó cerrada).
        /// </summary>
        Task<EvaluacionGuardadaDto> GuardarEvaluacion(int candidatoId, EvaluacionGuardarDto dto, int? userId);

        /// <summary>
        /// Persiste los archivos del informe ya subidos a SharePoint (informe final y resultados de
        /// la evaluación de conocimientos). Va aparte de <see cref="GuardarEvaluacion"/> porque la
        /// subida necesita el código del requerimiento, que sale de ese primer guardado. Reemplaza
        /// el archivo vivo de cada tipo (el anterior queda con <c>state = false</c>) y devuelve la
        /// lista resultante. Con la lista vacía no escribe nada: solo devuelve lo que ya había.
        /// </summary>
        Task<List<EvaluacionArchivoDto>> GuardarEvaluacionArchivos(
            int evaluacionId, List<EvaluacionArchivoPersistDto> archivos, int? userId);

        /// <summary>
        /// Marca al candidato como NO_PASO (no continúa) y registra la trazabilidad del correo de
        /// agradecimiento (a qué correo, cuándo y quién). Mismas validaciones que
        /// <see cref="GuardarEvaluacion"/> salvo la del resultado cerrado: reenviar el correo es
        /// válido si el envío anterior falló. Devuelve el contexto para armar el correo.
        /// </summary>
        Task<AgradecimientoEnvioContextoDto> RegistrarAgradecimiento(int candidatoId, int? userId);

        /// <summary>
        /// Saca del proceso al postulante cuyo formulario quedó RECHAZADO: lo marca como NO_PASO y
        /// registra la trazabilidad del correo de fin de proceso. Es el equivalente de
        /// <see cref="RegistrarAgradecimiento"/> antes de la entrevista, así que el correo sale de
        /// <c>gth_postulante_formulario</c> y no de <c>gth_entrevista</c>, que todavía no existe.
        /// Lanza <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si el
        /// candidato no existe y 400 si su formulario no está rechazado.
        /// </summary>
        Task<AgradecimientoEnvioContextoDto> RegistrarRechazoPostulante(int candidatoId, int? userId);

        /// <summary>
        /// Informe de finalistas de un requerimiento (cabecera + candidatos evaluados que siguen en
        /// carrera, con sus comentarios y CV), en 1 roundtrip por bloque.
        /// Devuelve null si el requerimiento no existe o queda fuera del alcance del usuario.
        /// </summary>
        Task<RevisionFinalistasDto?> GetRevisionFinalistas(int requerimientoId, SolicitudPersonalScope scope);

        /// <summary>
        /// Registra la decisión final del área solicitante sobre un finalista y mueve el
        /// requerimiento: aprobar lo deja en CERRADO (el seleccionado pasa a onboarding); rechazar
        /// lo deja en SELECCION_JEFATURA mientras queden finalistas por decidir y lo devuelve a
        /// LONG_LIST cuando ya no queda ninguno (GTH deberá enviar una nueva long list). Al rechazar
        /// también registra el envío del correo de agradecimiento. Scope: el área del usuario y
        /// solo con el requerimiento en SELECCION_JEFATURA. Lanza
        /// <see cref="Abril_Backend.Application.Exceptions.AbrilException"/> 404 si no existe o el
        /// candidato no es finalista, y 409 si el proceso ya salió de esa fase o el finalista ya
        /// estaba decidido. Devuelve el contexto para armar los correos.
        /// </summary>
        /// <remarks>
        /// El área a la que entra el seleccionado no se recibe: al aprobar se resuelve acá desde
        /// el puesto del requerimiento (<c>puesto.area_destino_scope_id</c>), con el área del
        /// solicitante como respaldo cuando el puesto no tiene destino.
        /// </remarks>
        Task<FinalistaDecisionContextoDto> RegistrarDecisionFinalista(
            int requerimientoId, int candidatoId, bool aprobado, SolicitudPersonalScope scope);

        /// <summary>Destinatarios vigentes del correo del tipo indicado (SOLICITUD / LONG_LIST): principales + copias.</summary>
        Task<CorreoDestinatariosDto> GetCorreoDestinatarios(string tipoCodigo);

        /// <summary>
        /// Gerente del área de un solicitante: parte de su <c>workers.area_scope_id</c> y sube por
        /// el árbol de áreas hasta el primer nodo con un trabajador ACTIVO de categoría GERENTE.
        /// Sube por el árbol porque los gerentes están registrados en el nodo "Área de Gerencia"
        /// y no en el área estándar de la que cuelga el solicitante. null si no hay ninguno.
        /// </summary>
        Task<GerenteAreaDto?> GetGerenteDeArea(int? areaScopeId);

        /// <summary>
        /// Reemplaza el conjunto de destinatarios vigentes del tipo indicado por el nuevo
        /// (reconciliación: conserva los que siguen, da de baja los quitados, agrega los nuevos).
        /// Los correos ya vienen normalizados (minúsculas) y sin duplicados desde el servicio.
        /// </summary>
        Task ReplaceCorreoDestinatarios(string tipoCodigo, List<string> principales, List<string> copias, int? userId);
    }
}
