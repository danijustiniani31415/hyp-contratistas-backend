namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Payload del formulario "Nueva solicitud de personal". La justificación (obligatoria) y el
    /// sustento (adjunto multipart, aparte del JSON) son de la solicitud completa; cada elemento de
    /// <see cref="Vacantes"/> genera un requerimiento independiente.
    /// </summary>
    public class SolicitudPersonalCreateDto
    {
        /// <summary>
        /// Justificación general de la solicitud. Obligatoria: es el sustento que leen el gerente
        /// del área y Gerencia General para aprobar, y va en el cuerpo de sus correos.
        /// </summary>
        public string? Justificacion { get; set; }

        public List<VacanteCreateDto> Vacantes { get; set; } = new();
    }

    public class VacanteCreateDto
    {
        /// <summary>
        /// Puesto elegido del catálogo. Es el único origen posible: el solicitante no puede dar de
        /// alta puestos nuevos desde este formulario — eso lo hace GTH en el catálogo de puestos.
        /// </summary>
        public int? PuestoId { get; set; }

        public int TipoRequerimientoId { get; set; }

        /// <summary>
        /// Trabajador al que reemplaza la vacante. Obligatorio cuando el tipo de requerimiento es
        /// <c>REEMPLAZO</c> (salvo que el solicitante no tenga <c>area_scope_id</c> y por lo tanto
        /// no haya lista de dónde elegir); ignorado en las vacantes nuevas, donde se guarda null.
        /// Debe pertenecer al área del solicitante o a un área hija — el backend lo revalida, no se
        /// confía en lo que mande el cliente.
        /// </summary>
        public int? ReemplazaWorkerId { get; set; }

        public int ProjectId { get; set; }

        /// <summary>
        /// Salario bruto mensual de la vacante, en soles. Obligatorio en las vacantes NUEVAS: es
        /// parte de lo que Gerencia General aprueba. En los REEMPLAZOS no se pide —el puesto y su
        /// banda ya existen— y lo que llegue se descarta. Se guarda redondeado a 2 decimales.
        /// </summary>
        public decimal? SalarioBrutoMensual { get; set; }

        /// <summary>
        /// true = la vacante es un ingreso directo <b>FFT</b>: el solicitante ya sabe a quién
        /// quiere, así que obliga a declarar <see cref="FftCandidatoNombre"/>,
        /// <see cref="FftCandidatoDocumento"/> y <see cref="FftCandidatoCorreo"/>, y el proceso se
        /// salta publicación, revisión de CV, long list, entrevistas y finalistas.
        /// </summary>
        public bool EsFft { get; set; }

        /// <summary>Nombre completo del candidato FFT. Obligatorio cuando <see cref="EsFft"/>.</summary>
        public string? FftCandidatoNombre { get; set; }

        /// <summary>
        /// Tipo de documento del candidato FFT (id de <c>gth_tipo_documento</c>: DNI / CE).
        /// Obligatorio cuando <see cref="EsFft"/> — es lo que decide cuántos dígitos admite
        /// <see cref="FftCandidatoDocumento"/>.
        /// </summary>
        public int? FftTipoDocumentoId { get; set; }

        /// <summary>
        /// Número de documento del candidato FFT, obligatorio cuando <see cref="EsFft"/>. El largo
        /// depende del tipo: 8 dígitos exactos para el DNI, entre 8 y 12 para el carné de
        /// extranjería. Es la llave con la que el candidato entra a <c>person</c> apenas se registra
        /// la solicitud, así que no es un dato más del pedido — sin él no habría forma de saber si
        /// esa persona ya existe en la base maestra y se duplicarían fichas.
        /// </summary>
        public string? FftCandidatoDocumento { get; set; }

        /// <summary>
        /// Correo personal del candidato FFT: el buzón al que GTH le enviará su formulario.
        /// Obligatorio cuando <see cref="EsFft"/>.
        /// </summary>
        public string? FftCandidatoCorreo { get; set; }
    }

    /// <summary>Resultado de crear la solicitud: los códigos REQ-AAAA-NNNN generados.</summary>
    public class SolicitudPersonalCreateResultDto
    {
        public int SolicitudId { get; set; }
        public List<string> Codigos { get; set; } = new();

        /// <summary>
        /// ¿Salió TODO lo que tenía que salir? Las vacantes normales disparan el correo de
        /// aprobación y las de ingreso directo el aviso a GTH; una solicitud mixta manda los dos y
        /// esto es true solo si salieron ambos. false cuando no hay destinatarios configurados o el
        /// envío falló: la solicitud queda registrada esperando un reenvío, así que hay que
        /// avisárselo al solicitante.
        /// </summary>
        public bool CorreoGerenciaEnviado { get; set; }

        /// <summary>
        /// true = la solicitud no pasa por «Aprobaciones» porque TODAS sus vacantes son de ingreso
        /// directo FFT, y a un ingreso directo no lo aprueba nadie. Los requerimientos nacen ya en
        /// manos de GTH, con su candidato seleccionado y esperando el EMO de ingreso.
        /// </summary>
        public bool AprobacionGgOmitida { get; set; }

        /// <summary>
        /// true si la solicitud trae al menos una vacante de ingreso directo FFT. Junto con
        /// <see cref="AprobacionGgOmitida"/> distingue los tres casos que el mensaje de respuesta
        /// tiene que contar: solo vacantes normales, solo ingresos directos, o las dos cosas.
        /// </summary>
        public bool HayIngresoDirecto { get; set; }
    }
}
