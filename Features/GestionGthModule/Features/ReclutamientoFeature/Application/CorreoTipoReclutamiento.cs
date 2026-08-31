namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application
{
    /// <summary>
    /// Códigos estables de los tipos de correo configurables del módulo de Reclutamiento
    /// (espejo de <c>gth_correo_tipo.codigo</c>). Se usan para scopear los destinatarios.
    /// </summary>
    public static class CorreoTipoReclutamiento
    {
        /// <summary>
        /// Correo de "vacantes NUEVAS por aprobar" (va al Gerente General). Es el primer correo del
        /// flujo para las vacantes de ruta <see cref="RutaAprobacion.GerenciaGeneral"/>: hasta que
        /// el GG no aprueba, GTH no recibe nada. Desde el corte por tipo de requerimiento ya no
        /// lleva al gerente del área — los reemplazos salen por
        /// <see cref="AprobacionReemplazo"/>.
        /// </summary>
        public const string AprobacionGg = "APROBACION_GG";

        /// <summary>
        /// Correo de "vacantes de REEMPLAZO por aprobar" que va al <b>gerente del área</b> del
        /// solicitante. Es el equivalente de <see cref="AprobacionGg"/> para la ruta
        /// <see cref="RutaAprobacion.AreaYGth"/>: mismo momento —al registrarse la solicitud— pero
        /// otros destinatarios y otras vacantes. Una solicitud que mezcla tipos dispara los dos, y
        /// cada uno lista solo las suyas.
        ///
        /// Ya no lleva a GTH: el reemplazo se firma en dos tiempos y GTH recibe lo suyo recién
        /// cuando el gerente del área aprueba (ver <see cref="AprobacionReemplazoGth"/>).
        /// </summary>
        public const string AprobacionReemplazo = "APROBACION_REEMPLAZO";

        /// <summary>
        /// Correo de "reemplazos aprobados por el área, pendientes de la firma de GTH" (va a GTH).
        /// Es el <b>segundo</b> tiempo de la ruta <see cref="RutaAprobacion.AreaYGth"/>: sale
        /// cuando el gerente del área aprueba alguna vacante de reemplazo, y lleva solo las que él
        /// aprobó. Hasta ese momento GTH no recibe nada ni ve la solicitud en «Aprobaciones» —
        /// la firma del área es la que le abre el turno.
        ///
        /// No confundir con <see cref="ReemplazoAprobado"/>, que sale después: ese avisa que la
        /// vacante ya juntó las DOS firmas y hay que reclutarla; este pide la segunda.
        /// </summary>
        public const string AprobacionReemplazoGth = "APROBACION_REEMPLAZO_GTH";

        /// <summary>
        /// Correo informativo de "se registró una solicitud de vacantes nuevas" que va al gerente
        /// del área del solicitante. Sale en el mismo momento que <see cref="AprobacionGg"/> y con
        /// las mismas vacantes, pero <b>no pide nada</b>: las vacantes nuevas las aprueba Gerencia
        /// General y el gerente del área solo tiene que enterarse, así que este correo va sin el
        /// botón de aprobar y sin enlace a «Aprobaciones».
        /// </summary>
        public const string AvisoGerenteArea = "AVISO_GERENTE_AREA";

        /// <summary>
        /// Correo de "aprobación de Gerencia a GTH". Sale recién cuando Gerencia General aprueba, y
        /// solo con las vacantes aprobadas. Como lo dispara la decisión de Gerencia, se configura
        /// desde Aprobaciones y no desde Solicitud de Personal.
        /// </summary>
        public const string Solicitud = "SOLICITUD";

        /// <summary>
        /// Correo de "reemplazos aprobados a GTH". Es el equivalente de <see cref="Solicitud"/> para
        /// la ruta <see cref="RutaAprobacion.AreaYGth"/>: mismo destinatario y mismo trabajo del otro
        /// lado —publicar la vacante y reclutar—, pero lo dispara otra decisión. Sale recién cuando
        /// una vacante de reemplazo junta las DOS firmas, o sea con la de GTH, que es siempre la
        /// segunda: la del gerente del área abre el turno de GTH con
        /// <see cref="AprobacionReemplazoGth"/> y todavía no manda nada acá. Se configura desde
        /// Aprobaciones, que es donde esas firmas se registran.
        /// </summary>
        public const string ReemplazoAprobado = "REEMPLAZO_APROBADO";

        /// <summary>
        /// Correo de "vacantes aprobadas a TI". Sale junto con el de GTH, en la misma decisión de
        /// Gerencia General y con las mismas vacantes aprobadas, pero es un aviso de preparación:
        /// TI necesita la anticipación para alistar equipo, usuario y accesos de cada ingreso. Es
        /// independiente del de GTH — apagar uno no apaga el otro.
        /// </summary>
        public const string Ti = "TI_VACANTES";

        /// <summary>
        /// Correo del candidato de un <b>ingreso directo FFT</b>. Es el que arranca (y casi termina)
        /// el flujo de esas vacantes: a un ingreso directo no lo aprueba nadie, así que este correo
        /// reemplaza al de <see cref="AprobacionGg"/> y va directo a GTH, que lo único que tiene que
        /// hacer es programarle el EMO. Sale al registrarse la solicitud, lo pida quien lo pida. Se
        /// configura desde Solicitud de Personal, que es de donde sale.
        /// </summary>
        public const string FftSolicitudGg = "FFT_SOLICITUD_GG";

        /// <summary>
        /// Correo del candidato <b>FFT</b> que Gerencia General aprobó. Es la contraparte de
        /// <see cref="Solicitud"/> para las vacantes FFT: mismo momento (la decisión del GG) y
        /// mismo destinatario (GTH), pero otro cuerpo — no hay vacante que publicar ni proceso que
        /// arrancar, hay un candidato al que programarle su EMO. Se configura desde Aprobaciones.
        /// Desde que el ingreso directo no se aprueba, solo sale por los FFT que quedaron esperando
        /// esa firma; los nuevos avisan con <see cref="FftSolicitudGg"/>.
        /// </summary>
        public const string FftAprobacionGg = "FFT_APROBACION_GG";

        /// <summary>
        /// Correo de "el candidato FFT pasa a su EMO". Lo dispara GTH al aprobar el formulario de
        /// un candidato FFT de los que quedaron del flujo <b>anterior</b>, el que sí pedía
        /// formulario. En el flujo actual el ingreso directo va derecho al EMO al aprobarse la
        /// vacante y quien lo anuncia es <see cref="FftAprobacionGg"/> (o
        /// <see cref="FftSolicitudGg"/>), así que este correo ya no sale para los pedidos nuevos.
        /// </summary>
        public const string FftEmo = "FFT_EMO";

        /// <summary>Correo de "long list enviada" (va al solicitante).</summary>
        public const string LongList = "LONG_LIST";

        /// <summary>Correo de "decisión de long list" (lo envía el solicitante y va a GTH).</summary>
        public const string LongListDecision = "LONG_LIST_DECISION";

        /// <summary>
        /// Correo de "finalista enviado al solicitante". Sale cuando GTH guarda el informe de la
        /// entrevista de un candidato, que es el acto de mandarlo como finalista: el solicitante
        /// tiene que entrar a decidir. El destinatario principal es SIEMPRE el solicitante que
        /// registró la solicitud; la configuración solo aporta principales adicionales y copias.
        /// </summary>
        public const string FinalistaEnvio = "FINALISTA_ENVIO";

        /// <summary>
        /// Correo de "decisión de finalista" (lo envía el solicitante al aprobar o rechazar a un
        /// finalista y va a GTH).
        /// </summary>
        public const string FinalistaDecision = "FINALISTA_DECISION";

        /// <summary>
        /// Correo de "formulario del postulante completado" (va a GTH). Sale solo, sin que nadie lo
        /// dispare, en cuanto el postulante envía su formulario desde la página pública, para que
        /// GTH sepa que ya lo puede revisar.
        /// </summary>
        public const string FormularioCompletado = "FORMULARIO_COMPLETADO";

        /// <summary>
        /// Correo de "formulario del postulante enviado" (va al postulante). Lo dispara GTH desde
        /// la bandeja, uno por uno o en lote, y lleva el enlace público del formulario. El
        /// destinatario principal es SIEMPRE el postulante; la configuración solo aporta
        /// principales adicionales y copias.
        /// </summary>
        public const string FormularioEnvio = "FORMULARIO_ENVIO";

        /// <summary>
        /// Correo de "correcciones del formulario" (va al postulante). Sale cuando GTH rechaza un
        /// formulario ya llenado: lleva las observaciones y el MISMO enlace del envío original.
        /// Es otro correo que el de invitación —otro asunto y otro cuerpo—, por eso se configura
        /// aparte de <see cref="FormularioEnvio"/>.
        /// </summary>
        public const string FormularioCorreccion = "FORMULARIO_CORRECCION";

        /// <summary>
        /// Correo de "invitación a la entrevista" (va al postulante citado). El destinatario
        /// principal es SIEMPRE el postulante; la configuración solo aporta principales adicionales
        /// y copias, por si GTH quiere quedarse con el registro de cada citación.
        /// </summary>
        public const string Entrevista = "ENTREVISTA";

        /// <summary>
        /// Correo de "respuesta del candidato a la entrevista" (va a GTH). Lo dispara el propio
        /// candidato al pulsar Confirmar o Rechazar en el correo de invitación. El destinatario
        /// principal es SIEMPRE el área de GTH (<c>area_scope.email</c> de su nodo, el mismo buzón
        /// que resuelve <see cref="CorreoDestinatarioCodigo.GthArea"/>), así que la configuración
        /// solo aporta principales adicionales y copias.
        /// </summary>
        public const string EntrevistaRespuesta = "ENTREVISTA_RESPUESTA";

        /// <summary>
        /// Correo de "el candidato confirmó su entrevista" que va al <b>solicitante</b>. Lo dispara
        /// el mismo acto que <see cref="EntrevistaRespuesta"/> —el candidato pulsando Confirmar—
        /// pero solo cuando confirma, y le habla a otra persona y con otro propósito: el
        /// solicitante tiene que ir a esa entrevista, así que lo que necesita es el día, la hora y
        /// el lugar. Un rechazo no lo dispara: ahí no hay cita a la que ir y quien tiene que
        /// reprogramarla es GTH.
        ///
        /// El destinatario principal es SIEMPRE el solicitante que registró la solicitud; la
        /// configuración solo aporta principales adicionales y copias.
        /// </summary>
        public const string EntrevistaConfirmadaSolicitante = "ENTREVISTA_CONFIRMADA_SOLICITANTE";

        /// <summary>
        /// Correo de "se retomó a un candidato del historial" que va al <b>solicitante</b>. Sale
        /// cuando GTH elige a un rechazado para continuar el proceso tras un EMO de ingreso No
        /// Apto: el proceso vuelve a la etapa en la que se lo había descartado y quien pidió la
        /// vacante tiene que saber con quién sigue y qué le toca hacer (en la decisión final, la
        /// pelota vuelve a su lado).
        ///
        /// El destinatario principal es SIEMPRE el solicitante; la configuración solo aporta
        /// principales adicionales y copias.
        /// </summary>
        public const string CandidatoRetomado = "CANDIDATO_RETOMADO";

        /// <summary>
        /// Correo de "fin de proceso" (va al candidato que no continúa). Sale desde cuatro lados
        /// con el mismo cuerpo: cuando GTH rechaza al postulante tras rechazarle el formulario,
        /// cuando lo marca como "no continúa" tras la entrevista, cuando el solicitante rechaza a
        /// un finalista y cuando aprueba a uno y los demás quedan sin elegir. Es un solo correo,
        /// así que un solo tipo gobierna los cuatro. El destinatario principal es SIEMPRE el
        /// candidato.
        /// </summary>
        public const string Agradecimiento = "AGRADECIMIENTO";

        /// <summary>
        /// Traduce el slug de la URL (<c>aprobacion-gg</c> / <c>aprobacion-reemplazo</c> /
        /// <c>aprobacion-reemplazo-gth</c> / <c>aviso-gerente-area</c> / <c>solicitud</c> /
        /// <c>reemplazo-aprobado</c> / <c>ti-vacantes</c> /
        /// <c>fft-solicitud-gg</c> / <c>fft-aprobacion-gg</c> / <c>fft-emo</c> / <c>long-list</c> /
        /// <c>decision-long-list</c> / <c>finalista-envio</c> / <c>decision-finalista</c> /
        /// <c>formulario-envio</c> / <c>formulario-completado</c> / <c>formulario-correccion</c> /
        /// <c>entrevista</c> / <c>entrevista-respuesta</c> /
        /// <c>entrevista-confirmada-solicitante</c> / <c>candidato-retomado</c> /
        /// <c>agradecimiento</c>) al código estable. Devuelve null si el slug no corresponde a un
        /// tipo conocido.
        /// </summary>
        public static string? FromSlug(string? slug) => slug?.Trim().ToLowerInvariant() switch
        {
            "aprobacion-gg"            => AprobacionGg,
            "aprobacion-reemplazo"     => AprobacionReemplazo,
            "aprobacion-reemplazo-gth" => AprobacionReemplazoGth,
            "aviso-gerente-area"       => AvisoGerenteArea,
            "solicitud"              => Solicitud,
            "reemplazo-aprobado"     => ReemplazoAprobado,
            "ti-vacantes"            => Ti,
            "fft-solicitud-gg"       => FftSolicitudGg,
            "fft-aprobacion-gg"      => FftAprobacionGg,
            "fft-emo"                => FftEmo,
            "long-list"              => LongList,
            "decision-long-list"     => LongListDecision,
            "finalista-envio"        => FinalistaEnvio,
            "decision-finalista"     => FinalistaDecision,
            "formulario-envio"       => FormularioEnvio,
            "formulario-completado"  => FormularioCompletado,
            "formulario-correccion"  => FormularioCorreccion,
            "entrevista"             => Entrevista,
            "entrevista-respuesta"   => EntrevistaRespuesta,
            "entrevista-confirmada-solicitante" => EntrevistaConfirmadaSolicitante,
            "candidato-retomado"     => CandidatoRetomado,
            "agradecimiento"         => Agradecimiento,
            _ => null,
        };
    }
}
