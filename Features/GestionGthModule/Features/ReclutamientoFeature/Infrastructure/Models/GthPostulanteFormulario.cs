namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Formulario de información del postulante (tabla <c>gth_postulante_formulario</c>): 1:1 con un
    /// candidato aprobado (<see cref="GthCandidato"/>). GTH envía el enlace al correo del postulante;
    /// el postulante lo llena desde una página pública (acceso por <c>token</c>, sin autenticación) y
    /// GTH luego lo revisa (aprueba/rechaza). Reemplaza al formulario externo de Microsoft Forms.
    ///
    /// Los campos del formulario (páginas 1-4) quedan en null hasta que el postulante lo completa;
    /// los que apuntan a un catálogo (estado civil, tipo de documento, distrito, universidad, grado,
    /// disponibilidad, motivo de cese) se guardan como FK, no como texto libre.
    ///
    /// La convocatoria no se pregunta ni se guarda acá: es la del proceso al que ya fue invitado el
    /// postulante y se lee por <c>gth_candidato → gth_requerimiento → puesto</c>.
    /// </summary>
    public class GthPostulanteFormulario
    {
        public int GthPostulanteFormularioId { get; set; }

        /// <summary>FK al candidato aprobado dueño del formulario (1:1 entre los vigentes).</summary>
        public int GthCandidatoId { get; set; }

        /// <summary>Token de acceso público al formulario (va en el enlace del correo). Único entre los vigentes.</summary>
        public string Token { get; set; } = null!;

        /// <summary>FK a <c>gth_postulante_formulario_estado</c>: ENVIADO / COMPLETADO / APROBADO / RECHAZADO.</summary>
        public int GthPostulanteFormularioEstadoId { get; set; }

        /// <summary>
        /// FK a <c>person</c>: la ficha de la data maestra que se creó/actualizó cuando GTH APROBÓ
        /// este formulario. Null mientras el formulario no esté aprobado.
        ///
        /// Esta tabla es el dato "declarado por el postulante": puede traer cualquier cosa hasta que
        /// alguien de GTH lo valida. <c>person</c> es la data maestra, así que solo se escribe en el
        /// momento de la aprobación y solo con los campos que tienen columna equivalente allá (ver
        /// <c>SincronizarPersonAsync</c> en el repositorio). El resto — pretensiones salariales,
        /// disponibilidad, LinkedIn, colegiatura y toda la experiencia laboral — se queda acá porque
        /// no existe columna donde ponerlo y no se inventó ninguna.
        ///
        /// Es también el enlace que usa Onboarding para saber a qué correo mandar la carta oferta
        /// (<c>person.email</c>, el correo personal).
        /// </summary>
        public int? PersonId { get; set; }

        /// <summary>Correo al que GTH envió el enlace del formulario.</summary>
        public string CorreoEnvio { get; set; } = null!;

        // ── Trazabilidad del flujo ────────────────────────────────────────────
        public DateTimeOffset? EnviadoDateTime { get; set; }
        public int? EnviadoUserId { get; set; }

        /// <summary>Momento en que el postulante envió (completó) el formulario.</summary>
        public DateTimeOffset? CompletadoDateTime { get; set; }

        /// <summary>Usuario de GTH que revisó (aprobó/rechazó) el formulario.</summary>
        public int? RevisadoUserId { get; set; }

        /// <summary>Nombre del revisor (snapshot para mostrar "Aprobado por …", estable en el tiempo).</summary>
        public string? RevisadoNombre { get; set; }

        public DateTimeOffset? RevisadoDateTime { get; set; }

        /// <summary>Motivo del rechazo (opcional, solo cuando el formulario se rechaza).</summary>
        public string? MotivoRechazo { get; set; }

        // ── CV documentado que sube el propio postulante ───────────────────────
        /// <summary>
        /// CV documentado que el postulante adjunta al enviar el formulario, subido a la misma
        /// carpeta de SharePoint del requerimiento que los CVs de la long list. Es un segundo CV,
        /// no un reemplazo: el de <c>gth_candidato.cv_*</c> es el que consiguió el reclutador y
        /// este es el que el postulante declara como suyo, y GTH y el solicitante ven los dos.
        ///
        /// Todo en null en los formularios anteriores a este campo (y mientras el postulante no
        /// haya enviado nada): la obligatoriedad se exige en el envío, no en el esquema.
        /// </summary>
        public string? CvNombre { get; set; }

        /// <summary>
        /// Nombre con el que el postulante subió el archivo. Es el que se muestra: el de SharePoint
        /// lleva el código del requerimiento y un timestamp, igual que en los anexos de la long list.
        /// </summary>
        public string? CvNombreOriginal { get; set; }

        public string? CvUrl { get; set; }
        public string? CvItemId { get; set; }
        public string? CvDriveId { get; set; }

        // ── Página 0 · Consentimiento de protección de datos ──────────────────
        /// <summary>
        /// Autoriza el tratamiento de sus datos personales (Ley N.° 29733). Es la primera página del
        /// formulario y es obligatoria: sin ella no se puede avanzar ni enviar. Queda en null en los
        /// formularios anteriores a este campo (no se puede dar por otorgado un consentimiento que
        /// nunca se pidió).
        /// </summary>
        public bool? ConsentimientoDatosPersonales { get; set; }

        // ── Página 1 · Datos personales ───────────────────────────────────────
        public string? NombresCompletos { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public int? GthEstadoCivilId { get; set; }
        public int? GthTipoDocumentoId { get; set; }
        public string? NumeroDocumento { get; set; }
        /// <summary>FK a <c>gth_distrito</c>: distrito de residencia (Lima o Callao).</summary>
        public int? GthDistritoId { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? NumeroCelular { get; set; }
        public string? PretensionesSalariales { get; set; }
        public int? GthDisponibilidadId { get; set; }
        public string? Linkedin { get; set; }
        public string? PortafolioLink { get; set; }

        // ── Página 2 · Estudios realizados ────────────────────────────────────
        public string? Profesion { get; set; }
        public int? GthUniversidadId { get; set; }
        public int? GthGradoAcademicoId { get; set; }
        public string? NumeroColegiatura { get; set; }

        // ── Página 3 · Experiencia laboral (más reciente) ─────────────────────
        public string? Empresa { get; set; }
        public string? AreaTrabajo { get; set; }
        public string? Cargo { get; set; }
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaTermino { get; set; }
        public int? GthMotivoCeseId { get; set; }
        public string? FuncionesPrincipales { get; set; }
        public string? Logros { get; set; }
        public string? IngresoBrutoMensual { get; set; }
        public int? PersonasACargo { get; set; }
        public string? JefeInmediato { get; set; }
        /// <summary>Autoriza la verificación de referencias de este trabajo (Sí/No).</summary>
        public bool? AutorizaVerificacionReferencias { get; set; }

        // ── Página 4 · Consentimiento y veracidad ─────────────────────────────
        /// <summary>Declara bajo juramento que la información es veraz (Sí/No).</summary>
        public bool? DeclaracionVeracidad { get; set; }
        /// <summary>Confirma haber completado todos los documentos requeridos (Sí/No).</summary>
        public bool? ConfirmacionDocumentos { get; set; }

        // ── Auditoría ─────────────────────────────────────────────────────────
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
