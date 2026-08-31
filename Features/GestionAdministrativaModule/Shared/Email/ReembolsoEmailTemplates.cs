using Abril_Backend.Shared.Services.Email.Layout;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Email
{
    /// <summary>
    /// Datos que necesitan los tres correos del reembolso. Se arma en el repositorio con una sola
    /// consulta por solicitud: el correo no vuelve a la base a buscar nada.
    /// </summary>
    public sealed class ReembolsoCorreoDatos
    {
        public int SolicitudId { get; set; }
        /// <summary>Correlativo de la solicitud DENTRO del trabajador (el "#3" que ve él, no el id).</summary>
        public int NumeroUsuario { get; set; }
        public string Trabajador { get; set; } = string.Empty;
        /// <summary>Correo del trabajador que pidió la salida — el destinatario de los dos correos de decisión.</summary>
        public string? TrabajadorEmail { get; set; }
        public string? Area { get; set; }
        public DateOnly FechaSalida { get; set; }
        /// <summary>Número de la planilla de rendición ("TI: 000123"), o null si la salida no tiene planilla.</summary>
        public string? NumeroPlanilla { get; set; }
        public int TrayectosCount { get; set; }
        /// <summary>Suma de lo rendido en la salida (capturas o catálogo), en soles.</summary>
        public decimal MontoTotal { get; set; }
        /// <summary>Nombre de quien tomó la decisión (el jefe que aprobó o rechazó).</summary>
        public string? DecididoPor { get; set; }
        /// <summary>Observación del rechazo. Solo la usa el correo de rechazo.</summary>
        public string? Observacion { get; set; }
    }

    /// <summary>
    /// Los tres correos que cierran el ciclo de la rendición, todos con el mismo chrome de la
    /// intranet (<see cref="SalidaEmailLayout"/>):
    ///
    /// <list type="bullet">
    ///   <item>Al jefe/revisor: el trabajador ya adjuntó el Consolidado del S10 y hay un reembolso
    ///     esperando su revisión.</item>
    ///   <item>Al trabajador: su reembolso quedó aprobado.</item>
    ///   <item>Al trabajador: su reembolso quedó rechazado, con la observación a subsanar.</item>
    /// </list>
    ///
    /// Los tres llevan UN botón que abre la pantalla exacta en la intranet: el correo avisa y lleva,
    /// no explica el flujo (ver el criterio editorial en <see cref="AbrilEmailLayout"/>).
    /// </summary>
    public static class ReembolsoEmailTemplates
    {
        // Íconos del catálogo de public/images/emails/icons (los genera
        // Abril-Frontend/scripts/generate-email-icons.js). Se reutilizan los que ya existen: no
        // hace falta un juego propio de salidas para tres correos.
        private const string IconoRevisar     = "req-solicitud";
        private const string IconoAprobado    = "req-aprobada";
        private const string IconoRechazado   = "req-decision";
        private const string IconoFranjaOk    = "req-check";
        private const string IconoFranjaNo    = "req-rechazadas";
        private const string IconoFranjaAviso = "req-aviso";

        private const string FilaTrabajador = "req-solicitante";
        private const string FilaArea       = "req-area";
        private const string FilaFecha      = "req-fecha";
        private const string FilaPlanilla   = "req-codigo";
        private const string FilaMonto      = "req-sustento";
        private const string FilaDecision   = "req-vistobueno";

        /// <summary>
        /// Aviso al jefe/revisor: el trabajador ya adjuntó el Consolidado del S10 y su reembolso
        /// está esperando revisión. El botón abre Gestión de Salidas en esa solicitud.
        /// </summary>
        public static string RevisionPendiente(SalidaEmailLayout l, ReembolsoCorreoDatos d, string urlRevisar) =>
            l.Documento(
                new AbrilEmailLayout.Cabecera(
                    IconoRevisar,
                    "Reembolso por revisar",
                    $"<b>{AbrilEmailLayout.Esc(d.Trabajador)}</b> adjuntó el Consolidado del S10 de su salida del "
                    + $"<b>{d.FechaSalida:dd/MM/yyyy}</b>."),
                l.Tarjeta(FilasBase(d)),
                l.Franja(IconoFranjaAviso, AbrilEmailLayout.Tono.Info,
                    "La rendición ya está completa: falta tu visto bueno para que pase a firma y a tesorería."),
                l.Boton("Revisar el reembolso", urlRevisar),
                l.EnlaceDirecto(urlRevisar));

        /// <summary>
        /// Al solicitante: su reembolso quedó aprobado. El botón abre su solicitud en la intranet.
        /// </summary>
        public static string Aprobado(SalidaEmailLayout l, ReembolsoCorreoDatos d, string urlVer) =>
            l.Documento(
                new AbrilEmailLayout.Cabecera(
                    IconoAprobado,
                    "Reembolso aprobado",
                    $"Tu solicitud de salida <b>#{d.NumeroUsuario}</b> del <b>{d.FechaSalida:dd/MM/yyyy}</b> "
                    + "quedó aprobada para reembolso."),
                l.Franja(IconoFranjaOk, AbrilEmailLayout.Tono.Verde,
                    string.IsNullOrWhiteSpace(d.DecididoPor)
                        ? "Aprobado por tu jefatura."
                        : $"Aprobado por <b>{AbrilEmailLayout.Esc(d.DecididoPor)}</b>."),
                l.Tarjeta(FilasBase(d)),
                l.Boton("Ver mi solicitud", urlVer),
                l.EnlaceDirecto(urlVer));

        /// <summary>
        /// Al solicitante: su reembolso quedó rechazado. La observación va en la franja roja porque
        /// es lo único que tiene que leer, y el botón lo deja en la solicitud exacta a subsanar.
        /// </summary>
        public static string Rechazado(SalidaEmailLayout l, ReembolsoCorreoDatos d, string urlSubsanar)
        {
            var observacion = string.IsNullOrWhiteSpace(d.Observacion)
                ? "Sin observación registrada. Coordina con tu jefatura antes de volver a enviarlo."
                : AbrilEmailLayout.EscMultilinea(d.Observacion.Trim());

            return l.Documento(
                new AbrilEmailLayout.Cabecera(
                    IconoRechazado,
                    "Reembolso rechazado",
                    $"Tu solicitud de salida <b>#{d.NumeroUsuario}</b> del <b>{d.FechaSalida:dd/MM/yyyy}</b> "
                    + "volvió con observaciones."),
                l.Franja(IconoFranjaNo, AbrilEmailLayout.Tono.Rojo,
                    $"<b>Observación:</b> {observacion}"),
                l.Tarjeta(FilasBase(d)),
                l.Boton("Subsanar observaciones", urlSubsanar),
                l.EnlaceDirecto(urlSubsanar));
        }

        /// <summary>
        /// Las filas comunes a los tres correos. Las que no tienen dato no se agregan: una tarjeta
        /// con "—" repetidos no informa nada.
        /// </summary>
        private static List<AbrilEmailLayout.Fila> FilasBase(ReembolsoCorreoDatos d)
        {
            var filas = new List<AbrilEmailLayout.Fila>
            {
                new(FilaTrabajador, "Trabajador", AbrilEmailLayout.Esc(d.Trabajador)),
            };

            if (!string.IsNullOrWhiteSpace(d.Area))
                filas.Add(new(FilaArea, "Área", AbrilEmailLayout.Esc(d.Area)));

            var trayectos = d.TrayectosCount > 1 ? $" · {d.TrayectosCount} trayectos" : "";
            filas.Add(new(FilaFecha, "Fecha de salida", $"{d.FechaSalida:dd/MM/yyyy}{trayectos}"));

            if (!string.IsNullOrWhiteSpace(d.NumeroPlanilla))
                filas.Add(new(FilaPlanilla, "Planilla", AbrilEmailLayout.Esc(d.NumeroPlanilla)));

            if (d.MontoTotal > 0m)
                filas.Add(new(FilaMonto, "Monto rendido",
                    $"S/ {d.MontoTotal.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("es-PE"))}"));

            if (!string.IsNullOrWhiteSpace(d.DecididoPor))
                filas.Add(new(FilaDecision, "Revisado por", AbrilEmailLayout.Esc(d.DecididoPor)));

            return filas;
        }
    }
}
