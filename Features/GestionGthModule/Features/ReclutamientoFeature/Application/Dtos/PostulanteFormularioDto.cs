namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    // ── Público (página del postulante, acceso por token) ────────────────────

    /// <summary>
    /// Respuesta del GET público del formulario (por token): contexto del proceso + catálogos de
    /// los desplegables + respuestas ya guardadas (para reanudar) + estado. Todo en una sola petición.
    /// </summary>
    public class PostulanteFormularioPublicoDto
    {
        /// <summary>Puesto/convocatoria del proceso al que postula (para el encabezado del formulario).</summary>
        public string Puesto { get; set; } = string.Empty;

        /// <summary>Nombre con el que GTH registró al candidato (referencial, para saludarlo).</summary>
        public string CandidatoNombre { get; set; } = string.Empty;

        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>true si el formulario ya fue APROBADO por GTH → solo lectura.</summary>
        public bool SoloLectura { get; set; }

        /// <summary>
        /// Observaciones de GTH cuando el formulario fue RECHAZADO: el postulante vuelve a abrir el
        /// mismo enlace con sus respuestas precargadas, corrige lo observado y lo reenvía. Es null en
        /// cualquier otro estado (una vez corregido y reenviado ya no se le muestran).
        /// </summary>
        public string? Observaciones { get; set; }

        // Catálogos de los desplegables del formulario.
        public List<OpcionDto> EstadosCiviles { get; set; } = new();
        public List<TipoDocumentoOpcionDto> TiposDocumento { get; set; } = new();
        public List<DistritoOpcionDto> Distritos { get; set; } = new();
        public List<OpcionDto> Universidades { get; set; } = new();
        public List<OpcionDto> GradosAcademicos { get; set; } = new();
        public List<OpcionDto> Disponibilidades { get; set; } = new();
        public List<OpcionDto> MotivosCese { get; set; } = new();

        /// <summary>Respuestas ya guardadas (ids de catálogo + valores) para precargar el formulario.</summary>
        public PostulanteFormularioRespuestasDto Respuestas { get; set; } = new();

        /// <summary>
        /// Nombre del CV documentado que el postulante ya subió, para que al reabrir el enlace
        /// (o al corregir un formulario observado) sepa que no tiene que volver a adjuntarlo. Null
        /// si todavía no subió ninguno.
        ///
        /// No se sirve la url: el archivo vive en SharePoint y el postulante no tiene acceso, así
        /// que un enlace solo le daría un error de permisos.
        /// </summary>
        public string? CvNombre { get; set; }
    }

    /// <summary>
    /// Tipo de documento para el desplegable. Además del nombre lleva el <c>Codigo</c> estable
    /// (DNI / CE) para que el formulario pueda aplicar reglas por tipo (el DNI son 8 dígitos)
    /// sin depender del texto que se muestra.
    /// </summary>
    public class TipoDocumentoOpcionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        /// <summary>DNI o CE (espejo de gth_tipo_documento.codigo).</summary>
        public string Codigo { get; set; } = string.Empty;
    }

    /// <summary>Distrito para el desplegable (incluye la provincia para agrupar/mostrar).</summary>
    public class DistritoOpcionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        /// <summary>LIMA o CALLAO.</summary>
        public string Provincia { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuestas del formulario del postulante (ids de catálogo + valores libres). Se usa tanto para
    /// precargar (GET público) como para recibir el envío (POST público) del postulante.
    /// </summary>
    public class PostulanteFormularioRespuestasDto
    {
        // Página 0 · Consentimiento de protección de datos
        /// <summary>Autoriza el tratamiento de sus datos personales (Ley N.° 29733). Obligatorio para enviar.</summary>
        public bool? ConsentimientoDatosPersonales { get; set; }

        // Página 1 · Datos personales
        public string? NombresCompletos { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public int? EstadoCivilId { get; set; }
        public int? TipoDocumentoId { get; set; }
        public string? NumeroDocumento { get; set; }
        public int? DistritoId { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? NumeroCelular { get; set; }
        public string? PretensionesSalariales { get; set; }
        public int? DisponibilidadId { get; set; }
        public string? Linkedin { get; set; }
        public string? PortafolioLink { get; set; }

        // Página 2 · Estudios realizados
        public string? Profesion { get; set; }
        public int? UniversidadId { get; set; }
        public int? GradoAcademicoId { get; set; }
        public string? NumeroColegiatura { get; set; }

        // Página 3 · Experiencia laboral
        public string? Empresa { get; set; }
        public string? AreaTrabajo { get; set; }
        public string? Cargo { get; set; }
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaTermino { get; set; }
        public int? MotivoCeseId { get; set; }
        public string? FuncionesPrincipales { get; set; }
        public string? Logros { get; set; }
        public string? IngresoBrutoMensual { get; set; }
        public int? PersonasACargo { get; set; }
        public string? JefeInmediato { get; set; }
        public bool? AutorizaVerificacionReferencias { get; set; }

        // Página 4 · Consentimiento y veracidad
        public bool? DeclaracionVeracidad { get; set; }
        public bool? ConfirmacionDocumentos { get; set; }
    }

    /// <summary>
    /// Lo que necesita el servicio para subir el CV documentado del postulante ANTES de guardar el
    /// formulario: el código del requerimiento (nombra la carpeta y el archivo en SharePoint), el
    /// candidato al que pertenece y si ya había subido un CV en un envío anterior.
    ///
    /// Es una consulta aparte del guardado a propósito: el archivo se sube a SharePoint —que es un
    /// servicio externo y puede fallar— y su url tiene que quedar en la misma fila que las
    /// respuestas, así que el nombre del archivo hay que resolverlo antes de escribir nada.
    /// </summary>
    public class PostulanteCvContextoDto
    {
        public int CandidatoId { get; set; }

        /// <summary>Código del requerimiento (REQ-AAAA-NNNN).</summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>true si el formulario ya tenía un CV cargado (envío anterior o corrección).</summary>
        public bool TieneCv { get; set; }

        /// <summary>
        /// true si el formulario ya fue APROBADO por GTH, o sea que no admite cambios. Es un freno
        /// temprano para no dejar un archivo huérfano en SharePoint de un envío que el guardado va
        /// a rechazar igual: la regla la sigue mandando el repositorio al guardar.
        /// </summary>
        public bool SoloLectura { get; set; }
    }

    /// <summary>
    /// CV documentado ya subido a SharePoint, listo para grabarse en el formulario. Se pasa al
    /// repositorio junto con las respuestas para que todo quede en un solo guardado.
    /// </summary>
    public class PostulanteCvSubidaDto
    {
        /// <summary>Nombre del archivo tal como quedó en SharePoint.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Nombre con el que el postulante lo subió (el que se muestra).</summary>
        public string? NombreOriginal { get; set; }

        public string? Url { get; set; }
        public string? ItemId { get; set; }
        public string? DriveId { get; set; }
    }

    /// <summary>
    /// Contexto que devuelve el repositorio cuando el postulante envía su formulario: lo necesario
    /// para avisarle a GTH que ya lo completó, sin volver a consultar la base de datos.
    /// </summary>
    public class FormularioCompletadoContextoDto
    {
        /// <summary>Código del requerimiento (REQ-AAAA-NNNN).</summary>
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }

        /// <summary>
        /// Requerimiento y candidato a los que pertenece el formulario: son los que arman el enlace
        /// del botón del correo, que abre la bandeja de GTH en ese requerimiento y, encima, el modal
        /// «Ver formulario» de este postulante listo para aprobarlo o rechazarlo.
        /// </summary>
        public int RequerimientoId { get; set; }
        public int CandidatoId { get; set; }

        /// <summary>Nombre del postulante (el que declaró en el formulario; si no, el que registró GTH).</summary>
        public string CandidatoNombre { get; set; } = string.Empty;

        /// <summary>Correo y celular declarados, para que GTH pueda contactarlo sin abrir el sistema.</summary>
        public string? CorreoPostulante { get; set; }
        public string? NumeroCelular { get; set; }

        /// <summary>Momento del envío, ya en hora de Perú.</summary>
        public DateTime CompletadoEn { get; set; }

        /// <summary>true si es un reenvío tras un rechazo: el postulante corrigió lo observado.</summary>
        public bool EsCorreccion { get; set; }
    }

    // ── GTH (bandeja de reclutamiento: enviar, revisar, aprobar/rechazar) ─────

    /// <summary>Body del POST que envía el formulario al correo del postulante.</summary>
    public class EnviarFormularioDto
    {
        public string Correo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Body del POST que envía el formulario a varios postulantes de un mismo requerimiento en una
    /// sola operación (los candidatos que GTH seleccionó en la bandeja).
    /// </summary>
    public class EnviarFormularioMasivoDto
    {
        public List<EnviarFormularioMasivoItemDto> Candidatos { get; set; } = new();
    }

    /// <summary>Un candidato del lote con el correo al que se le envía el formulario.</summary>
    public class EnviarFormularioMasivoItemDto
    {
        public int CandidatoId { get; set; }
        public string Correo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado del envío masivo. El lote nunca se cae entero por un candidato: cada uno reporta su
    /// propio resultado para que la pantalla actualice los que sí salieron y muestre qué falló.
    /// </summary>
    public class FormularioEnvioMasivoResultDto
    {
        public string Message { get; set; } = string.Empty;
        public int Enviados { get; set; }
        public int Fallidos { get; set; }

        /// <summary>Un resultado por candidato del lote, en el mismo orden en que llegaron.</summary>
        public List<FormularioEnvioMasivoResultadoDto> Resultados { get; set; } = new();
    }

    /// <summary>Resultado del envío del formulario de un candidato dentro del lote.</summary>
    public class FormularioEnvioMasivoResultadoDto
    {
        public int CandidatoId { get; set; }

        /// <summary>true si el correo salió. false si no se pudo preparar el envío o si falló el correo.</summary>
        public bool Enviado { get; set; }

        /// <summary>Motivo del fallo para mostrarlo junto al candidato. null si se envió bien.</summary>
        public string? Error { get; set; }

        /// <summary>
        /// Estado del formulario tras el envío, para refrescar la ficha del candidato. Viene relleno
        /// incluso cuando el correo falló —el formulario ya quedó registrado en ese punto— y solo es
        /// null cuando no se llegó a tocar la base de datos (candidato inválido o correo mal escrito).
        /// </summary>
        public CandidatoFormularioResumenDto? Formulario { get; set; }
    }

    /// <summary>
    /// Un envío del lote tal como lo recibe el repositorio: candidato, correo destino y el token a
    /// usar si el formulario de ese candidato aún no existía.
    /// </summary>
    public class EnvioMasivoSolicitudDto
    {
        public int CandidatoId { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string NuevoToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lo que devuelve el repositorio por cada envío del lote: el contexto para armar el correo, o el
    /// motivo por el que ese candidato quedó fuera (los demás del lote sí se preparan igual).
    /// </summary>
    public class EnvioMasivoPreparadoDto
    {
        public int CandidatoId { get; set; }

        /// <summary>null si el candidato no pasó las validaciones (ver <see cref="Error"/>).</summary>
        public EnviarFormularioContextoDto? Contexto { get; set; }

        /// <summary>Motivo por el que no se preparó el envío. null si sí se preparó.</summary>
        public string? Error { get; set; }
    }

    /// <summary>Body del POST que registra la decisión de GTH sobre el formulario (aprobar/rechazar).</summary>
    public class FormularioDecisionDto
    {
        public bool Aprobado { get; set; }
        /// <summary>Motivo del rechazo (opcional, solo cuando se rechaza).</summary>
        public string? Motivo { get; set; }
    }

    /// <summary>
    /// Vista de GTH del formulario de un candidato (modal "Ver formulario"): estado + trazabilidad +
    /// datos ya listos para mostrar (los catálogos resueltos a su nombre). Si el postulante aún no lo
    /// completó, <see cref="Datos"/> viene null y solo se muestra la estructura/estado.
    /// </summary>
    public class FormularioRevisionDto
    {
        /// <summary>true si el formulario existe (GTH ya envió el enlace al postulante).</summary>
        public bool Existe { get; set; }

        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        public string CandidatoNombre { get; set; } = string.Empty;
        public string? CorreoEnvio { get; set; }
        public DateTime? EnviadoEn { get; set; }
        public DateTime? CompletadoEn { get; set; }
        public string? RevisadoNombre { get; set; }
        public DateTime? RevisadoEn { get; set; }
        public string? MotivoRechazo { get; set; }

        /// <summary>Datos declarados por el postulante (null si aún no completó el formulario).</summary>
        public FormularioDatosDto? Datos { get; set; }

        /// <summary>
        /// CV que GTH cargó en la long list de este candidato (<c>gth_candidato.cv_*</c>). Null si
        /// la carga no dejó url.
        /// </summary>
        public FormularioCvDto? CvGth { get; set; }

        /// <summary>
        /// CV documentado que adjuntó el propio postulante al enviar el formulario. Null si no lo
        /// subió (los formularios anteriores a este campo) o si aún no lo envió. Se sirve junto al
        /// de GTH porque el sentido de pedirlo es poder comparar los dos.
        /// </summary>
        public FormularioCvDto? CvPostulante { get; set; }

        /// <summary>
        /// Aviso para GTH: el documento que declaró el postulante ya existe en la base, así que
        /// aprobar actualizaría una ficha que ya estaba. Null cuando no coincide con nada (el caso
        /// normal). Nunca sale por los endpoints públicos del postulante.
        /// </summary>
        public FormularioCoincidenciaDto? Coincidencia { get; set; }
    }

    /// <summary>
    /// Un CV del candidato para abrirlo desde SharePoint. Los dos del proceso —el que cargó GTH en
    /// la long list y el que adjuntó el postulante en su formulario— se sirven con esta misma forma.
    /// </summary>
    public class FormularioCvDto
    {
        /// <summary>Nombre visible del archivo.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Link al archivo en SharePoint. Null si la subida no dejó url.</summary>
        public string? Url { get; set; }
    }

    /// <summary>
    /// Severidad de la coincidencia del documento declarado con la base. Solo el trabajador actual
    /// bloquea la aprobación; los otros dos son informativos y GTH decide.
    /// </summary>
    public static class NivelCoincidenciaPersona
    {
        /// <summary>
        /// Existe en <c>person</c> y no tiene ninguna ficha en <c>workers</c>: una persona del
        /// registro maestro que nunca fue trabajador (un postulante anterior, un representante
        /// legal, un familiar registrado…). Aprobar solo completa su ficha.
        /// </summary>
        public const string SoloPerson = "SOLO_PERSON";

        /// <summary>
        /// Tiene ficha en <c>workers</c> pero ninguna está adentro
        /// (<c>esta_adentro = false</c>): un retirado, un finalista aprobado de otro proceso o
        /// alguien que no llegó a ingresar. Aprobar está permitido —es justamente el caso del
        /// extrabajador que vuelve a postular— pero GTH tiene que saber que no es una ficha nueva.
        /// </summary>
        public const string FichaPrevia = "FICHA_PREVIA";

        /// <summary>
        /// Tiene al menos una ficha en <c>workers</c> con <c>esta_adentro = true</c>: trabaja en
        /// Abril hoy. Aprobar sobreescribiría los datos de un trabajador actual con lo que tecleó
        /// alguien en un formulario público, así que se bloquea.
        /// </summary>
        public const string TrabajadorActual = "TRABAJADOR_ACTUAL";
    }

    /// <summary>
    /// El documento que el postulante declaró ya existe en la base. Es información solo para GTH:
    /// aprobar el formulario no crea una ficha nueva en <c>person</c> sino que actualiza esta, y
    /// según de quién sea esa ficha la aprobación se permite o se bloquea.
    /// </summary>
    public class FormularioCoincidenciaDto
    {
        /// <summary>Documento declarado que coincide, normalizado (mayúsculas, sin espacios).</summary>
        public string Documento { get; set; } = string.Empty;

        /// <summary>Tipo de documento que declaró el postulante (DNI / CE). Null si no lo eligió.</summary>
        public string? TipoDocumento { get; set; }

        /// <summary>Ficha de <c>person</c> con la que coincide.</summary>
        public int PersonId { get; set; }

        /// <summary>Nombre con el que esa persona ya está registrada, para que GTH lo compare con el declarado.</summary>
        public string? NombreEnBd { get; set; }

        /// <summary>
        /// Ficha de <c>workers</c> de esa persona: la que está adentro si hay alguna, y si no la
        /// más reciente. Null si nunca tuvo ficha de trabajador.
        /// </summary>
        public int? WorkerId { get; set; }

        /// <summary>Código del estado de esa ficha (ACTIVO, RETIRADO…). Null si no hay ficha.</summary>
        public string? WorkersEstadoCodigo { get; set; }

        /// <summary>Nombre visible del estado de esa ficha. Null si no hay ficha.</summary>
        public string? WorkersEstadoNombre { get; set; }

        /// <summary>
        /// true si alguna de sus fichas está adentro de la empresa hoy
        /// (<c>workers_estado.esta_adentro</c>). Es lo mismo que
        /// <see cref="BloqueaAprobacion"/>: se sirven los dos porque uno describe a la persona y el
        /// otro la consecuencia, y la pantalla usa cada uno en un sitio distinto.
        /// </summary>
        public bool EstaAdentro { get; set; }

        /// <summary>
        /// Severidad, para que la pantalla elija el aviso. Ver
        /// <see cref="NivelCoincidenciaPersona"/>.
        /// </summary>
        public string Nivel { get; set; } = string.Empty;

        /// <summary>
        /// true si esta coincidencia impide aprobar el formulario. El backend lo vuelve a validar
        /// al registrar la decisión: esto es para que la pantalla no ofrezca un botón que va a
        /// fallar, no la garantía.
        /// </summary>
        public bool BloqueaAprobacion => EstaAdentro;
    }

    /// <summary>
    /// Datos del formulario ya listos para mostrar en el modal de GTH: los campos de catálogo vienen
    /// resueltos a su nombre (no id) y el resto tal cual los declaró el postulante.
    /// </summary>
    public class FormularioDatosDto
    {
        // Página 0
        public bool? ConsentimientoDatosPersonales { get; set; }

        // Página 1
        public string? NombresCompletos { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string? EstadoCivil { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Distrito { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? NumeroCelular { get; set; }
        public string? PretensionesSalariales { get; set; }
        public string? Disponibilidad { get; set; }
        public string? Linkedin { get; set; }
        public string? PortafolioLink { get; set; }

        // Página 2
        public string? Profesion { get; set; }
        public string? Universidad { get; set; }
        public string? GradoAcademico { get; set; }
        public string? NumeroColegiatura { get; set; }

        // Página 3
        public string? Empresa { get; set; }
        public string? AreaTrabajo { get; set; }
        public string? Cargo { get; set; }
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaTermino { get; set; }
        public string? MotivoCese { get; set; }
        public string? FuncionesPrincipales { get; set; }
        public string? Logros { get; set; }
        public string? IngresoBrutoMensual { get; set; }
        public int? PersonasACargo { get; set; }
        public string? JefeInmediato { get; set; }
        public bool? AutorizaVerificacionReferencias { get; set; }

        // Página 4
        public bool? DeclaracionVeracidad { get; set; }
        public bool? ConfirmacionDocumentos { get; set; }
    }

    /// <summary>Estado del formulario del postulante como se muestra en la bandeja de GTH por candidato.</summary>
    public class CandidatoFormularioResumenDto
    {
        /// <summary>Estado del formulario: null si GTH aún no envió el enlace.</summary>
        public string? EstadoCodigo { get; set; }
        public string? EstadoNombre { get; set; }
        public string? CorreoEnvio { get; set; }
        public DateTime? EnviadoEn { get; set; }
        public DateTime? CompletadoEn { get; set; }
        public string? RevisadoNombre { get; set; }
        public DateTime? RevisadoEn { get; set; }
    }

    /// <summary>Resultado de enviar el formulario o registrar la decisión (para refrescar el modal).</summary>
    public class FormularioAccionResultDto
    {
        public string Message { get; set; } = string.Empty;
        public CandidatoFormularioResumenDto Formulario { get; set; } = new();
    }

    /// <summary>
    /// Contexto que devuelve el repositorio al registrar la decisión de GTH: el resumen para
    /// refrescar el modal más lo necesario para armar el correo de rechazo (token del enlace, puesto,
    /// nombre y correo del postulante), sin volver a consultar la base de datos. Los datos del correo
    /// solo se llenan cuando la decisión es un rechazo (al aprobar no se envía nada al postulante).
    /// </summary>
    public class DecisionFormularioContextoDto
    {
        public CandidatoFormularioResumenDto Resumen { get; set; } = new();

        /// <summary>
        /// true = hay que avisarle al postulante del rechazo. Solo cuando había completado el
        /// formulario: si nunca lo llenó, el rechazo es una decisión interna para destrabar el
        /// proceso y no tiene sentido pedirle que corrija algo que nunca escribió.
        /// </summary>
        public bool AvisarAlPostulante { get; set; }

        /// <summary>Token del enlace público (el mismo del envío original: conserva las respuestas ya guardadas).</summary>
        public string Token { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;

        /// <summary>Nombre del postulante para el saludo del correo (el declarado en el formulario si lo hay).</summary>
        public string CandidatoNombre { get; set; } = string.Empty;

        /// <summary>Correo al que GTH envió el formulario (mismo destinatario del correo de rechazo).</summary>
        public string Correo { get; set; } = string.Empty;

        /// <summary>Observaciones del rechazo tal como se registraron (null si se aprobó o no se indicó motivo).</summary>
        public string? Motivo { get; set; }

        /// <summary>
        /// Ficha de <c>person</c> (data maestra) creada/actualizada al aprobar. Null en los rechazos:
        /// solo la aprobación de GTH da por buenos los datos declarados.
        /// </summary>
        public int? PersonId { get; set; }

        /// <summary>
        /// Aviso a mostrarle a GTH cuando la ficha de la base maestra quedó incompleta (sin documento
        /// no se puede crear; sin correo personal Onboarding no puede enviar la carta oferta). Null
        /// cuando quedó completa.
        /// </summary>
        public string? PersonAviso { get; set; }

        // ── Ingreso directo FFT ───────────────────────────────────────────────
        /// <summary>
        /// Datos del salto FFT que hizo esta aprobación: el proceso no tiene entrevistas ni decisión
        /// de finalista, así que aprobar el formulario deja al candidato seleccionado y al
        /// requerimiento en EMO de ingreso. Null cuando el proceso no es FFT o cuando se rechazó.
        /// </summary>
        public DecisionFormularioFftDto? Fft { get; set; }
    }

    /// <summary>
    /// Lo que el servicio necesita para avisar que un candidato FFT pasa a su EMO: el requerimiento
    /// al que pertenece, la ficha que se le abrió y el estado en el que quedó el proceso.
    /// </summary>
    public class DecisionFormularioFftDto
    {
        public int RequerimientoId { get; set; }

        /// <summary>Código REQ-AAAA-NNNN del requerimiento.</summary>
        public string Codigo { get; set; } = string.Empty;

        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }

        /// <summary>Nombre del candidato tal como quedó en su ficha de candidato.</summary>
        public string CandidatoNombre { get; set; } = string.Empty;

        /// <summary>Estado en el que quedó el requerimiento (EMO de ingreso).</summary>
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>
        /// Ficha de pre-ingreso del candidato: es el id con el que GTH abre la programación de su
        /// EMO. Null cuando el formulario no dejó <c>person_id</c> y no hay ficha que abrir.
        /// </summary>
        public int? WorkerId { get; set; }
    }

    /// <summary>
    /// Contexto que devuelve el repositorio al preparar el envío del formulario: el token de acceso
    /// (nuevo o reutilizado), datos para el correo y el estado resultante para refrescar el modal.
    /// </summary>
    public class EnviarFormularioContextoDto
    {
        public string Token { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string CandidatoNombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;

        /// <summary>
        /// true si lo que se está reenviando es un formulario RECHAZADO: en ese caso el correo que
        /// corresponde es el de correcciones (con las observaciones), no el de invitación inicial.
        /// </summary>
        public bool EsRechazo { get; set; }

        /// <summary>
        /// true cuando es la PRIMERA vez que se le envía el formulario a ese candidato (no existía
        /// su fila de formulario). Solo ese envío lleva el correo de bienvenida al proceso —el que
        /// le cuenta que avanza, qué se le pide y qué etapas vienen—: los reenvíos posteriores son
        /// para alguien que ya recibió esa explicación, así que llevan el correo corto de siempre.
        /// </summary>
        public bool EsPrimerEnvio { get; set; }

        /// <summary>Observaciones del rechazo, para el correo de correcciones. null si no es un rechazo.</summary>
        public string? Motivo { get; set; }

        public CandidatoFormularioResumenDto Resumen { get; set; } = new();
    }
}
