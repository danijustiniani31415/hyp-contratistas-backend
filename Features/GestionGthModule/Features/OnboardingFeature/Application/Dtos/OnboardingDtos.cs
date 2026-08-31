namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos
{
    /// <summary>
    /// Todo lo que necesita la pantalla de Onboarding al entrar, en una sola petición: tarjetas de
    /// resumen, embudo de fases, tabla de colaboradores ingresados y los candidatos que ya pueden
    /// entrar al proceso (el desplegable del modal «Nuevo ingreso»).
    /// </summary>
    public class BandejaOnboardingDto
    {
        public ResumenOnboardingDto Resumen { get; set; } = new();

        /// <summary>Fases del catálogo en orden, con cuántos colaboradores hay parados en cada una.</summary>
        public List<FaseOnboardingDto> Fases { get; set; } = new();

        public List<OnboardingListItemDto> Colaboradores { get; set; } = new();

        /// <summary>
        /// Candidatos aptos para iniciar onboarding: seleccionados de requerimientos ya CERRADOS que
        /// todavía no tienen un onboarding abierto. Es el desplegable del modal «Nuevo ingreso».
        /// </summary>
        public List<CandidatoAptoDto> CandidatosAptos { get; set; } = new();
    }

    public class ResumenOnboardingDto
    {
        /// <summary>Colaboradores con fecha de ingreso dentro del mes en curso.</summary>
        public int IngresosDelMes { get; set; }

        /// <summary>Onboardings abiertos (todo lo que no está COMPLETO).</summary>
        public int EnProceso { get; set; }

        /// <summary>Onboardings terminados (estado COMPLETO).</summary>
        public int Completos { get; set; }

        /// <summary>Onboardings iniciados en los últimos 7 días.</summary>
        public int ColaboradoresNuevos { get; set; }

        /// <summary>Cuántos candidatos hay esperando que GTH les abra el onboarding.</summary>
        public int CandidatosPorIngresar { get; set; }
    }

    public class FaseOnboardingDto
    {
        public int FaseId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }

        /// <summary>Colaboradores parados en esta fase.</summary>
        public int Total { get; set; }

        /// <summary>
        /// Checklist operativo de la fase (catálogo, igual para todos los colaboradores). Viaja con
        /// la bandeja porque es lo que dibuja el modal de detalle y de donde salen sus contadores de
        /// avance: pedirlo aparte al abrir cada detalle sería una petición extra por fila.
        /// </summary>
        public List<ActividadOnboardingDto> Actividades { get; set; } = new();
    }

    /// <summary>
    /// Una actividad obligatoria del checklist (espejo de <c>gth_onboarding_actividad</c>). El
    /// avance del onboarding se mide en actividades, no en fases.
    /// </summary>
    public class ActividadOnboardingDto
    {
        public int ActividadId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }

        /// <summary>true = la cumple el sistema solo, sin acción de GTH (avisos preventivos).</summary>
        public bool Automatica { get; set; }
    }

    /// <summary>Una fila de la tabla «Colaboradores ingresados».</summary>
    public class OnboardingListItemDto
    {
        public int OnboardingId { get; set; }
        public int CandidatoId { get; set; }
        public int? PersonId { get; set; }

        /// <summary>Código del requerimiento que originó la contratación (REQ-AAAA-NNNN).</summary>
        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;
        public string? Puesto { get; set; }
        public string? Area { get; set; }

        /// <summary>Razón social con la que se contrata (la que GTH asignó al requerimiento).</summary>
        public string? Empresa { get; set; }

        /// <summary>Proyecto / obra destino de la vacante.</summary>
        public string? ProyectoObra { get; set; }

        public DateOnly? FechaIngreso { get; set; }

        /// <summary>Quien pidió la vacante: es el jefe directo del nuevo colaborador.</summary>
        public string? JefeDirecto { get; set; }

        /// <summary>Correo personal al que se le envió (o se le enviará) la carta oferta.</summary>
        public string? Correo { get; set; }

        public string FaseCodigo { get; set; } = string.Empty;
        public string FaseNombre { get; set; } = string.Empty;
        public int FaseOrden { get; set; }

        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>
        /// Avance del onboarding en % — se mide en ACTIVIDADES del checklist, no en fases
        /// (RF-ONB-27): una fase puede tener una sola actividad y otra ocho.
        /// </summary>
        public int AvancePorcentaje { get; set; }

        /// <summary>
        /// Códigos de las actividades del checklist ya cumplidas por este colaborador. Es el único
        /// origen del avance y de los checks del detalle: la pantalla no vuelve a deducir nada.
        /// </summary>
        public List<string> ActividadesHechas { get; set; } = new();

        // ── Carta oferta ──────────────────────────────────────────────────────
        public string? CartaOfertaNombre { get; set; }
        public string? CartaOfertaUrl { get; set; }

        /// <summary>Fecha de envío de la carta oferta, ya en hora de Perú.</summary>
        public DateTime? CartaOfertaEnviadaEn { get; set; }

        // ── Carta oferta firmada (la que devuelve el colaborador) ─────────────
        public string? CartaFirmadaNombre { get; set; }
        public string? CartaFirmadaUrl { get; set; }

        /// <summary>Fecha en que se adjuntó la carta firmada, ya en hora de Perú.</summary>
        public DateTime? CartaFirmadaSubidaEn { get; set; }

        /// <summary>
        /// Fecha en que el POSTULANTE firmó la carta desde el enlace público, ya en hora de Perú.
        /// Con valor, la pantalla puede decir que la firmó él y no hace falta que GTH adjunte nada;
        /// en null con <see cref="CartaFirmadaUrl"/> llena, la subió GTH a mano.
        /// </summary>
        public DateTime? CartaFirmadaPostulanteEn { get; set; }

        /// <summary>
        /// Fecha en que GTH aprobó la carta firmada, ya en hora de Perú. Null = todavía pendiente de
        /// revisión, que es la actividad que bloquea el avance de la primera fase.
        /// </summary>
        public DateTime? CartaFirmadaAprobadaEn { get; set; }

        /// <summary>Carpeta de SharePoint donde vive el file digital del colaborador.</summary>
        public string? FileDigitalCarpeta { get; set; }

        /// <summary>Observación interna que dejó GTH al abrir el onboarding.</summary>
        public string? Observacion { get; set; }

        /// <summary>Fecha en que se abrió el onboarding, ya en hora de Perú.</summary>
        public DateTime? IniciadoEn { get; set; }
    }

    /// <summary>Resultado de subir o aprobar la carta oferta firmada, o de avanzar de fase.</summary>
    public class OnboardingAccionResultDto
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>La fila ya actualizada, para refrescar tabla y modal sin recargar la bandeja.</summary>
        public OnboardingListItemDto? Colaborador { get; set; }
    }

    /// <summary>
    /// Candidato que terminó reclutamiento y puede pasar a onboarding: una opción del desplegable
    /// del modal «Nuevo ingreso».
    /// </summary>
    public class CandidatoAptoDto
    {
        public int CandidatoId { get; set; }
        public int RequerimientoId { get; set; }
        public int? PersonId { get; set; }

        /// <summary>Nombre + código del requerimiento: es lo que se lee en el desplegable.</summary>
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;

        public string? Puesto { get; set; }
        public string? Area { get; set; }
        public string? Empresa { get; set; }
        public string? ProyectoObra { get; set; }

        /// <summary>
        /// Correo personal al que iría la carta oferta. Sale de <c>person.email</c> (lo que GTH
        /// validó al aprobar el formulario) y, si esa ficha no existe todavía, del correo que el
        /// postulante declaró en su formulario. Null = no hay a dónde enviar y el modal lo bloquea.
        /// </summary>
        public string? Correo { get; set; }

        /// <summary>
        /// DNI (o documento de identidad) del colaborador. Sale de la misma ficha que el correo:
        /// <c>person.document_identity_code</c> y, si esa ficha aún no existe, del número que el
        /// postulante declaró en su formulario. Es lo que nombra su carpeta en el file de
        /// colaboradores («80508050 - NOMBRE»), así que null = no se puede abrir el onboarding y el
        /// modal lo bloquea antes de enviar nada.
        /// </summary>
        public string? Dni { get; set; }

        /// <summary>Jefe directo (el solicitante de la vacante), para mostrarlo en el resumen del modal.</summary>
        public string? JefeDirecto { get; set; }

        /// <summary>
        /// true si el candidato ya tiene ficha en <c>person</c> (la crea la aprobación de su
        /// formulario de postulante). Sin ficha no hay dónde guardar la firma que va a dibujar en el
        /// enlace público, así que el modal bloquea el envío. Se resuelve por <c>person_id</c> del
        /// formulario y, si falta, por coincidencia de documento contra la base maestra.
        /// </summary>
        public bool TieneFichaMaestra { get; set; }
    }

    /// <summary>Datos del modal «Nuevo ingreso» (el JSON del multipart; la carta va como archivo).</summary>
    public class OnboardingCreateDto
    {
        public int CandidatoId { get; set; }

        /// <summary>Fecha de ingreso pactada. Si no viene, se usa la fecha requerida del requerimiento.</summary>
        public DateOnly? FechaIngreso { get; set; }

        /// <summary>
        /// Correo al que enviar la carta oferta. Normalmente no viaja: el backend lo resuelve de la
        /// base de datos. Solo se usa si GTH lo corrigió a mano en el modal.
        /// </summary>
        public string? Correo { get; set; }

        public string? Observacion { get; set; }
    }

    /// <summary>Cuerpo del reenvío del enlace de firma. Todo opcional: normalmente va vacío.</summary>
    public class OnboardingReenviarEnlaceDto
    {
        /// <summary>
        /// Correo al que reenviar el enlace. Si no viene se usa el de la base maestra y, en su
        /// defecto, el que ya quedó registrado en el envío anterior.
        /// </summary>
        public string? Correo { get; set; }
    }

    public class OnboardingCreateResultDto
    {
        public int OnboardingId { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>La fila ya lista para insertarse en la tabla sin recargar la bandeja completa.</summary>
        public OnboardingListItemDto? Colaborador { get; set; }
    }

    /// <summary>
    /// Contexto que devuelve el repositorio al validar el inicio de un onboarding: todo lo que el
    /// servicio necesita para subir la carta a SharePoint y armar el correo, resuelto en un solo
    /// roundtrip y ANTES de escribir nada. También es lo que devuelve el reenvío del enlace, que
    /// necesita exactamente los mismos datos sobre un onboarding ya abierto.
    /// </summary>
    public class OnboardingContextoDto
    {
        public int CandidatoId { get; set; }
        public int RequerimientoId { get; set; }

        /// <summary>
        /// Ficha del colaborador en la base maestra. Es obligatoria en este flujo: la firma que el
        /// postulante dibuja en el enlace público se guarda en <c>person.signature_image_bytes</c>,
        /// así que sin ficha no habría dónde ponerla.
        /// </summary>
        public int PersonId { get; set; }

        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Token del enlace público que se le manda al postulante. Al abrir el onboarding lo genera
        /// el servicio; al reenviar el enlace es el que ya está guardado en la fila.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Documento de identidad: primer tramo del nombre de la carpeta del colaborador.</summary>
        public string Dni { get; set; } = string.Empty;

        public string? Puesto { get; set; }
        public string? Area { get; set; }
        public string? Empresa { get; set; }
        public string? ProyectoObra { get; set; }
        public string Correo { get; set; } = string.Empty;
        public DateOnly? FechaIngreso { get; set; }
        public string? JefeDirecto { get; set; }
    }

    /// <summary>Carta oferta ya subida a SharePoint, para persistirla junto con el onboarding.</summary>
    public class CartaOfertaPersistDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? ItemId { get; set; }
        public string? DriveId { get; set; }
    }

    /// <summary>
    /// Biblioteca de SharePoint donde Onboarding guarda los documentos del colaborador — hoy el
    /// «File de colaboradores» (<c>gth_carta_oferta_folder</c>): el link con el que se resuelve y su
    /// nombre legible, que es el que se muestra como primer tramo de la ruta del file digital.
    /// </summary>
    public class CartaOfertaFolderDto
    {
        public string LinkUrl { get; set; } = string.Empty;
        public string? FolderName { get; set; }
    }

    /// <summary>
    /// Carpeta de SharePoint que hace de file digital del colaborador («{DNI} - {NOMBRE}» dentro de
    /// la biblioteca configurada). Se resuelve una vez (al enviar la carta oferta) y se persiste en
    /// el onboarding: los documentos siguientes se suben ahí sin volver a derivarla del DNI y el
    /// nombre. Cada tipo de documento vive en una subcarpeta suya («Carta Oferta Enviada»,
    /// «Carta Oferta Firmada»), que se resuelve en el momento de subir.
    /// </summary>
    public class FileDigitalCarpetaDto
    {
        public string DriveId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;

        /// <summary>Ruta legible («File de colaboradores / 80508050 - DIEGO HERRERA»).</summary>
        public string? Ruta { get; set; }
    }

    /// <summary>
    /// Lo que el servicio necesita para subir un documento al file digital de un onboarding ya
    /// abierto: dónde está su carpeta y con qué identificarlo. Lo resuelve el repositorio en un solo
    /// roundtrip, antes de tocar SharePoint.
    /// </summary>
    public class OnboardingDocumentoContextoDto
    {
        public int OnboardingId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Documento de identidad, solo para rearmar la carpeta de los onboardings que no la tienen
        /// persistida. Los abiertos desde que se guarda no lo usan.
        /// </summary>
        public string Dni { get; set; } = string.Empty;

        /// <summary>Carpeta ya persistida. Null en onboardings anteriores a que se guardara.</summary>
        public FileDigitalCarpetaDto? Carpeta { get; set; }

        public string FaseCodigo { get; set; } = string.Empty;
    }
}
