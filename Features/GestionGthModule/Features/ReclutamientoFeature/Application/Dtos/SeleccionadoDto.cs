namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Quién obtuvo el puesto de un requerimiento: el candidato que el área solicitante aprobó en
    /// la decisión final (resultado SELECCIONADO), con la trazabilidad de quién y cuándo lo
    /// decidió. Null mientras el proceso no se haya cerrado con un seleccionado.
    ///
    /// Lo sirven las dos pantallas del proceso —el detalle del requerimiento (GTH) y el «Estado
    /// del reclutamiento» (solicitante)— para que el resultado quede registrado y consultable por
    /// ambos lados, no solo en el correo del momento.
    /// </summary>
    public class SeleccionadoDto
    {
        public int CandidatoId { get; set; }

        /// <summary>Nombre del candidato que obtuvo el puesto.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Puesto del requerimiento al cargar la long list (snapshot).</summary>
        public string? Puesto { get; set; }

        // ── CVs en SharePoint ─────────────────────────────────────────────────
        /// <summary>CV que cargó GTH en la long list.</summary>
        public string? CvNombre { get; set; }
        public string? CvUrl { get; set; }

        /// <summary>
        /// CV documentado que adjuntó el propio postulante en su formulario. Null si no llegó a
        /// subirlo (los procesos anteriores a que se pidiera el archivo).
        /// </summary>
        public string? CvPostulanteNombre { get; set; }
        public string? CvPostulanteUrl { get; set; }

        /// <summary>
        /// Momento en que el solicitante lo aprobó, en hora de Perú (UTC-5). Null solo si la
        /// decisión quedó registrada sin fecha (no debería pasar en el flujo actual).
        /// </summary>
        public DateTime? SeleccionadoEn { get; set; }

        /// <summary>
        /// Nombre del usuario del área solicitante que tomó la decisión final. Null si no se pudo
        /// resolver su ficha de trabajador.
        /// </summary>
        public string? SeleccionadoPor { get; set; }

        /// <summary>
        /// Responsable del proceso en GTH (el reclutador que llevó la vacante) al momento de la
        /// consulta. Null si el requerimiento nunca tuvo responsable asignado.
        /// </summary>
        public string? ResponsableGth { get; set; }

        /// <summary>
        /// Ficha de pre-ingreso del seleccionado en <c>workers</c>. Es el id con el que GTH salta
        /// a SSOMA · Salud Ocupacional · EMOs a programarle el examen de ingreso. Null si el
        /// candidato no llegó a tener formulario del postulante aprobado (sin ficha en
        /// <c>person</c> no hay de dónde colgarla).
        /// </summary>
        public int? WorkerId { get; set; }

        /// <summary>
        /// true mientras el EMO de Ingreso siga sin programarse: es lo que enciende el botón
        /// «Programar EMO de ingreso» del detalle de GTH.
        ///
        /// Ya no equivale a "el proceso sigue abierto": desde que el cierre lo decide la aptitud del
        /// examen, el requerimiento sigue en EMO_INGRESO con la cita ya creada y este flag en false.
        /// Lo que falta en ese momento lo cuentan <see cref="EmoProgramacionEstado"/> y
        /// <see cref="EmoAptitud"/>.
        /// </summary>
        public bool EmoIngresoPendiente { get; set; }

        /// <summary>
        /// Estado de la cita del EMO de Ingreso ("Programado", "Aceptado por Clínica", "En
        /// Atención", "Completado"…). Null si todavía no se le programó ninguna.
        ///
        /// Existe para que el detalle pueda decir por qué el requerimiento sigue abierto: sin esto,
        /// entre programar la cita y recibir el resultado GTH veía la fase «EMO de ingreso» sin
        /// botón y sin ninguna explicación.
        /// </summary>
        public string? EmoProgramacionEstado { get; set; }

        /// <summary>Fecha de la cita del EMO de Ingreso, en hora de Perú. Null si no hay cita.</summary>
        public DateTime? EmoFechaProgramada { get; set; }

        /// <summary>
        /// Aptitud del EMO ya registrado ("Apto", "Apto con Restricciones", "Observado", "No
        /// Apto"). Null mientras la clínica no cargue el resultado. "Observado" es la que deja el
        /// proceso esperando: la aptitud final la define la interconsulta.
        /// </summary>
        public string? EmoAptitud { get; set; }

        /// <summary>
        /// true en un escenario que no debería darse: el requerimiento sigue en la fase
        /// EMO_INGRESO pero la ficha que le tocó al seleccionado es de alguien que ya trabaja en
        /// Abril (<c>workers_estado.esta_adentro</c>), así que no es un pre-ingreso y el proceso no
        /// puede avanzar por acá — <c>ProgramacionEmoRepository.Create</c> rechaza esa cita.
        ///
        /// Se sirve para que el detalle lo diga en vez de quedarse sin botón y sin explicación.
        /// Es excluyente con <see cref="EmoIngresoPendiente"/>.
        /// </summary>
        public bool EmoIngresoBloqueado { get; set; }

        /// <summary>
        /// Estado de esa ficha ("Activo", "Inhabilitado por SSOMA") para nombrarlo en el aviso.
        /// Solo viene relleno cuando <see cref="EmoIngresoBloqueado"/> es true.
        /// </summary>
        public string? FichaEstadoNombre { get; set; }
    }
}
