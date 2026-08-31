using System.Net;
using System.Text;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Shared
{
    /// <summary>
    /// Arma el HTML del correo "EMO Confirmado" (programación aceptada por la clínica).
    ///
    /// Restricciones de correo que explican por qué el markup se ve así — no "limpiar" esto:
    /// - Todo el layout va en tablas anidadas con estilos inline. Outlook de escritorio usa el
    ///   motor de Word: no hay flex, ni grid, ni bloques &lt;style&gt;, y Power Automate puede
    ///   descartar el &lt;head&gt; de todos modos.
    /// - Los íconos son PNG hospedados, no SVG ni base64: Outlook no renderiza SVG y bloquea
    ///   las imágenes embebidas en base64.
    /// - Las imágenes se sirven desde los estáticos del frontend (public/images/emails/), no
    ///   desde el wwwroot del backend: en producción intranet.abril.pe es nginx, que solo proxea
    ///   /api/** al contenedor; cualquier otra ruta cae en el fallback del SPA y devuelve
    ///   index.html (200 text/html) en vez del PNG, por eso las imágenes salían rotas.
    /// - Los círculos verdes vienen dibujados dentro del propio PNG en lugar de armarse con
    ///   border-radius, porque Outlook de escritorio ignora border-radius y los dejaría cuadrados.
    /// - Cada &lt;img&gt; decorativa lleva alt vacío: si el cliente bloquea imágenes externas, deja
    ///   un hueco en blanco en vez de un texto alternativo suelto, y el correo se sigue leyendo
    ///   completo porque toda la información está en el HTML, no en las imágenes.
    /// </summary>
    public static class EmoConfirmacionEmailTemplate
    {
        private const string Fuente = "-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

        private const string Azul = "#005D9D";      // azul del logotipo (--color-abril-logo-blue)
        private const string Verde = "#64BC04";     // verde de la hoja del logo (--color-abril-lime)
        private const string Lienzo = "#F4F8F4";    // fondo de la hoja
        private const string Borde = "#E6EDE7";
        private const string TarjetaFondo = "#FBFDFB";
        private const string TarjetaBorde = "#E9EFE9";
        private const string Separador = "#EFF3EF";
        private const string Divisor = "#E7EDE8";
        private const string TextoValor = "#3F4A44";
        private const string TextoBajada = "#6B7A72";
        private const string AvisoFondo = "#EEF4FA";
        private const string AvisoTexto = "#0B5C97";

        /// <summary>Datos que se listan en la tarjeta central del correo.</summary>
        public sealed record Datos(
            string Trabajador,
            string TipoEmo,
            string Fecha,
            string Hora,
            string Proyecto,
            string Clinica,
            string? Direccion);

        /// <param name="assetsUrl">
        /// Base pública desde donde se sirven las imágenes (App:EmailAssetsUrl). Tiene que ser
        /// alcanzable desde internet: la resuelve el proxy de imágenes de Outlook, no el cliente.
        /// </param>
        public static string Construir(Datos datos, string assetsUrl)
        {
            var baseUrl = assetsUrl.TrimEnd('/');
            var iconos = $"{baseUrl}/images/emails/icons";
            var logoUrl = $"{baseUrl}/images/abril-logo.png";
            var recomendacionesUrl = $"{baseUrl}/images/emails/recomendaciones-emo.jpg";

            var filas = new List<(string Icono, string Etiqueta, string Valor)>
            {
                ("emo-trabajador", "Trabajador", datos.Trabajador),
                ("emo-tipo",       "Tipo EMO",   datos.TipoEmo),
                ("emo-fecha",      "Fecha",      datos.Fecha),
                ("emo-hora",       "Hora",       datos.Hora),
                ("emo-proyecto",   "Proyecto",   datos.Proyecto),
                ("emo-clinica",    "Clínica",    datos.Clinica),
            };

            if (!string.IsNullOrWhiteSpace(datos.Direccion))
                filas.Add(("emo-direccion", "Dirección", datos.Direccion!));

            var filasHtml = new StringBuilder();
            for (var i = 0; i < filas.Count; i++)
            {
                var (icono, etiqueta, valor) = filas[i];
                // La última fila no lleva línea inferior para no cortar el borde redondeado.
                var lineaInferior = i == filas.Count - 1 ? "" : $"border-bottom:1px solid {Separador};";

                filasHtml.Append($@"
<tr>
<td width='62' align='center' valign='middle' style='width:62px;padding:14px 0 14px 10px;{lineaInferior}'><img src='{iconos}/{icono}.png' width='28' height='28' alt='' style='display:block;width:28px;height:28px;border:0;outline:none;text-decoration:none' /></td>
<td width='150' valign='middle' style='width:150px;padding:14px 18px 14px 8px;{lineaInferior}border-right:1px solid {Divisor};font-family:{Fuente};font-size:15px;line-height:21px;font-weight:700;color:{Azul}'>{Esc(etiqueta)}:</td>
<td valign='middle' style='padding:14px 20px 14px 22px;{lineaInferior}font-family:{Fuente};font-size:15px;line-height:21px;color:{TextoValor}'>{Esc(valor)}</td>
</tr>");
            }

            return $@"<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' bgcolor='{Lienzo}' style='border-collapse:collapse;width:100%;background-color:{Lienzo};margin:0;padding:0'>
<tr>
<td align='center' style='padding:24px 12px'>

<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='640' style='border-collapse:collapse;width:100%;max-width:640px;background-color:#FFFFFF;border:1px solid {Borde};border-radius:16px'>

<tr>
<td style='padding:30px 34px 0 34px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse'>
<tr>
<td width='150' valign='middle' style='width:150px'><img src='{logoUrl}' width='150' alt='ABRIL Grupo Inmobiliario' style='display:block;width:150px;max-width:150px;height:auto;border:0;outline:none;text-decoration:none' /></td>
<td valign='middle' align='center' style='padding-left:16px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='border-collapse:collapse'>
<tr>
<td valign='middle' style='padding-right:12px'><img src='{iconos}/emo-check.png' width='44' height='44' alt='' style='display:block;width:44px;height:44px;border:0;outline:none;text-decoration:none' /></td>
<td valign='middle' style='font-family:{Fuente};font-size:30px;line-height:36px;font-weight:700;color:{Azul};letter-spacing:-0.4px;white-space:nowrap'>EMO Confirmado</td>
</tr>
<tr>
<td></td>
<td style='padding-top:6px'><table role='presentation' cellpadding='0' cellspacing='0' border='0' width='84' style='border-collapse:collapse;width:84px'><tr><td height='4' bgcolor='{Verde}' style='height:4px;line-height:4px;font-size:0;background-color:{Verde};border-radius:2px'>&nbsp;</td></tr></table></td>
</tr>
</table>
</td>
</tr>
</table>
</td>
</tr>

<tr>
<td align='center' style='padding:18px 40px 0 40px;font-family:{Fuente};font-size:15px;line-height:22px;color:{TextoBajada}'>Se ha confirmado la programación del Examen Médico Ocupacional:</td>
</tr>

<tr>
<td style='padding:22px 30px 0 30px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' bgcolor='{TarjetaFondo}' style='border-collapse:collapse;width:100%;background-color:{TarjetaFondo};border:1px solid {TarjetaBorde};border-radius:14px'>{filasHtml}
</table>
</td>
</tr>

<tr>
<td style='padding:24px 30px 30px 30px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse'>
<tr>
<td width='54' valign='middle' style='width:54px'><img src='{iconos}/emo-aviso.png' width='40' height='40' alt='' style='display:block;width:40px;height:40px;border:0;outline:none;text-decoration:none' /></td>
<td valign='middle' bgcolor='{AvisoFondo}' style='background-color:{AvisoFondo};border-radius:10px;padding:14px 18px;font-family:{Fuente};font-size:14px;line-height:20px;color:{AvisoTexto}'>El trabajador debe presentarse en la clínica en la fecha y hora indicadas. Ese mismo día se le brindarán los resultados.</td>
</tr>
</table>
</td>
</tr>

</table>

<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='640' style='border-collapse:collapse;width:100%;max-width:640px'>
<tr>
<td style='padding-top:18px'><img src='{recomendacionesUrl}' width='640' alt='Recomendaciones previas al Examen Médico Ocupacional' style='display:block;width:100%;max-width:640px;height:auto;border:0;outline:none;text-decoration:none;border-radius:14px' /></td>
</tr>
</table>

</td>
</tr>
</table>";
        }

        private static string Esc(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);
    }
}
