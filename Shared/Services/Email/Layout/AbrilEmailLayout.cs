using System.Net;
using System.Text;

namespace Abril_Backend.Shared.Services.Email.Layout
{
    /// <summary>
    /// Chrome compartido de TODOS los correos brandeados de Abril One: logo al pie, aro lima de
    /// los íconos, azul del logotipo en las etiquetas y tarjeta blanca sobre lienzo verdoso. Nació
    /// como el layout de los correos de Gestión GTH · Reclutamiento y se subió a Shared cuando
    /// Gestión Administrativa · Salidas empezó a usarlo: cambiar la identidad de los correos es
    /// cambiar este archivo y no una plantilla por módulo.
    ///
    /// Lo único que cambia entre módulos es el pie, que se pasa al construirlo.
    ///
    /// Criterio editorial, no solo visual: el correo lleva datos y un acceso, no explicaciones. No
    /// se agrega el párrafo que cuenta qué hace el botón, qué pasa después ni cómo sigue el flujo —
    /// eso se ve en la pantalla a la que lleva el enlace. La bajada es UNA línea. La excepción son
    /// los correos a alguien de fuera, que no tiene ninguna pantalla donde ver el proceso.
    ///
    /// Restricciones de correo que explican por qué el markup se ve así — no "limpiar" esto:
    /// - Todo el layout va en tablas anidadas con estilos inline. Outlook de escritorio usa el
    ///   motor de Word: no hay flex, ni grid, ni bloques &lt;style&gt;.
    /// - Los íconos son PNG hospedados, no SVG ni base64: Outlook no renderiza SVG y bloquea las
    ///   imágenes embebidas en base64. Se generan con
    ///   <c>Abril-Frontend/scripts/generate-email-icons.js</c>.
    /// - Las imágenes se sirven desde los estáticos del frontend (public/images/emails/), no desde
    ///   el wwwroot del backend: en producción intranet.abril.pe es nginx, que solo proxea /api/**
    ///   al contenedor; cualquier otra ruta cae en el fallback del SPA y devuelve index.html
    ///   (200 text/html) en vez del PNG, por eso las imágenes salían rotas.
    /// - Los aros verdes y los círculos de color vienen dibujados dentro del propio PNG en lugar de
    ///   armarse con border-radius, porque Outlook de escritorio ignora border-radius y los dejaría
    ///   cuadrados.
    /// - Las tablas usan <c>border-collapse:separate;border-spacing:0</c> (no <c>collapse</c>): con
    ///   collapse el border-radius de las esquinas no se aplica y la cabecera azul sale cuadrada
    ///   por fuera del borde redondeado de la tabla.
    /// - Cada &lt;img&gt; decorativa lleva alt vacío: si el cliente bloquea imágenes externas, deja
    ///   un hueco en blanco en vez de un texto alternativo suelto, y el correo se sigue leyendo
    ///   completo porque toda la información está en el HTML, no en las imágenes.
    /// - El logo va al pie, no en la cabecera: GTH pidió el título centrado y con el logo a la
    ///   izquierda de la misma fila el título quedaba centrado en el espacio sobrante, es decir
    ///   corrido a la derecha del centro real de la tarjeta. Con el logo abajo el bloque
    ///   ícono + título se centra contra el ancho completo. No devolverlo a la cabecera.
    /// - El logo es <c>images/emails/abril-logo.png</c> y no <c>images/abril-logo.png</c>, que es
    ///   el de la app: el de la app dice .png en el nombre pero sus bytes son WebP con
    ///   transparencia, y el proxy de imágenes de Gmail no reenvía WebP — lo recodifica a JPEG,
    ///   que no tiene canal alfa, así que el fondo transparente se aplanaba a NEGRO y el logo
    ///   salía en un recuadro negro solo en Gmail (en Outlook se veía bien). El de
    ///   <c>images/emails/</c> es PNG de verdad y opaco (blanco pintado adentro), lo genera
    ///   <c>Abril-Frontend/scripts/generate-email-icons.js</c> junto con los íconos. No apuntar
    ///   este correo al logo de la app.
    /// - Lo centrado se centra con <c>align='center'</c> en la celda, no con <c>margin:0 auto</c>:
    ///   Outlook de escritorio ignora los márgenes automáticos. El <c>margin:0 auto</c> del logo va
    ///   además del atributo, para los webmail que no heredan el align del &lt;td&gt;.
    /// </summary>
    public class AbrilEmailLayout
    {
        /// <summary>
        /// Las comillas de "Segoe UI" van como <c>&amp;#39;</c> y no como <c>'</c> literal porque
        /// todos los atributos del correo van entre comillas simples: un <c>'</c> crudo cierra el
        /// atributo <c>style</c> a media declaración y se pierde todo lo que viene después de
        /// <c>font-family</c> (font-size, font-weight y color). El navegador lo muestra en serif y
        /// sin los colores — pasó y no es obvio al leer el HTML.
        /// </summary>
        public const string Fuente =
            "-apple-system,&#39;Segoe UI&#39;,Roboto,Helvetica,Arial,sans-serif";

        public const string Azul = "#005D9D";        // azul del logotipo (--color-abril-logo-blue)
        public const string Lima = "#64BC04";        // verde de la hoja del logo (--color-abril-lime)
        public const string VerdeAccion = "#0F6E56"; // verde de acción de la app (--color-abril-standard)
        public const string Lienzo = "#F4F8F4";      // fondo de la hoja
        public const string Borde = "#E6EDE7";
        public const string TarjetaFondo = "#FBFDFB";
        public const string TarjetaBorde = "#E9EFE9";
        public const string Separador = "#EFF3EF";
        public const string Divisor = "#E7EDE8";
        public const string TextoValor = "#3F4A44";
        public const string TextoBajada = "#6B7A72";
        public const string TextoPie = "#9AA8A0";

        public const string VerdeOk = "#166534";     // texto de un resultado aprobado
        public const string RojoNo = "#991B1B";      // texto de un resultado rechazado

        /// <summary>
        /// Pie del correo, una línea al final de la tarjeta. Lo elige el módulo que arma el correo
        /// (ej. "Correo automático de Abril One · Gestión GTH · Reclutamiento."): es el único texto
        /// del chrome que cambia entre módulos. No poner acá jerga interna en correos que salen a
        /// alguien de fuera — un código de requerimiento no le dice nada a quien postula.
        /// </summary>
        protected readonly string Pie;

        private readonly string _iconos;
        private readonly string _logoUrl;

        public AbrilEmailLayout(string assetsUrl, string pie)
        {
            var baseUrl = (assetsUrl ?? "").TrimEnd('/');
            _iconos  = $"{baseUrl}/images/emails/icons";
            // images/emails/abril-logo.png y NO images/abril-logo.png (el de la app): ver la nota
            // del logo en el resumen de la clase.
            _logoUrl = $"{baseUrl}/images/emails/abril-logo.png";
            Pie      = pie ?? string.Empty;
        }

        /// <summary>
        /// Layout con el origen de las imágenes que corresponde. Es una clave aparte de
        /// App:FrontendUrl a propósito: Outlook no descarga las imágenes desde el cliente sino a
        /// través del proxy de imágenes de Microsoft, que nunca puede alcanzar un localhost. Con
        /// App:FrontendUrl (que en dev tiene que seguir apuntando a localhost para que el enlace
        /// del correo sea clicable) las imágenes salen siempre rotas al probar en local.
        /// </summary>
        public static AbrilEmailLayout Desde(IConfiguration configuration, string pie) =>
            new(AssetsUrl(configuration), pie);

        /// <summary>
        /// Origen de las imágenes del correo. Es una clave aparte de App:FrontendUrl a propósito:
        /// Outlook no descarga las imágenes desde el cliente sino a través del proxy de imágenes de
        /// Microsoft, que nunca puede alcanzar un localhost. Protegido para que los layouts
        /// derivados armen su propio factory sin repetir el fallback.
        /// </summary>
        protected static string AssetsUrl(IConfiguration configuration) =>
            configuration["App:EmailAssetsUrl"]
            ?? configuration["App:FrontendUrl"]
            ?? "https://intranet.abril.pe";

        // ── Piezas del correo ─────────────────────────────────────────────────

        /// <summary>Una fila ícono / etiqueta / valor de una tarjeta.</summary>
        public sealed record Fila(string Icono, string Etiqueta, string ValorHtml);

        public enum Alineacion { Izquierda, Centro, Derecha }

        /// <summary>
        /// Columna de una tabla. <paramref name="Ancho"/> va en px y el total de las columnas tiene
        /// que sumar el ancho interno de la tarjeta (640 − 60 de padding = 580) para que quepan sin
        /// apretarse.
        /// </summary>
        public sealed record Columna(string Titulo, int Ancho, Alineacion Alinea = Alineacion.Izquierda);

        /// <summary>Celda de una tabla. El color por defecto es el del texto de valor.</summary>
        public sealed record Celda(string Html, bool Negrita = false, string? Color = null, bool NoWrap = false);

        /// <summary>Color de una franja de estado (el bloque con ícono redondo y fondo de color).</summary>
        public enum Tono { Ambar, Verde, Rojo, Info }

        /// <summary>
        /// Cabecera del correo. <paramref name="Bajada"/> es UNA línea con lo que el destinatario
        /// tiene delante y va como HTML (admite &lt;b&gt;), así que el llamador tiene que escapar
        /// con <see cref="Esc"/> lo que venga de la base de datos.
        /// </summary>
        public sealed record Cabecera(string Icono, string Titulo, string? Bajada = null);

        // ── Documento ─────────────────────────────────────────────────────────

        /// <summary>
        /// Arma el correo completo: lienzo, tarjeta blanca, cabecera centrada, los bloques que se
        /// le pasen (en orden) y el pie. Los bloques nulos o vacíos se ignoran, así que un bloque
        /// condicional se pasa como <c>""</c> sin tener que armar la lista aparte.
        /// </summary>
        public string Documento(Cabecera cabecera, params string?[] bloques)
        {
            var cuerpo = new StringBuilder();
            foreach (var bloque in bloques)
                if (!string.IsNullOrWhiteSpace(bloque)) cuerpo.Append(bloque);

            // Los títulos largos bajan de cuerpo: el de la cabecera va en una sola línea (nowrap)
            // para que la barra lima quede centrada bajo el bloque ícono + título — con el título
            // en dos líneas la barra se separaría de él. El corte es a 30 caracteres porque el
            // logo dejó de compartir la fila con el título: ahora el título tiene el ancho interno
            // completo de la tarjeta (640 − 68 de padding = 572, menos 56 del ícono) en vez de los
            // ~350px que quedaban al lado del logo, así que ninguno de los títulos actuales se
            // encoge. Con el corte viejo de 21 los más largos salían chicos sin necesidad.
            var tamTitulo = cabecera.Titulo.Length > 30 ? "22px" : "26px";
            var altoTitulo = cabecera.Titulo.Length > 30 ? "28px" : "32px";

            var bajada = string.IsNullOrWhiteSpace(cabecera.Bajada)
                ? ""
                : $@"
<tr>
<td align='center' style='padding:18px 40px 0 40px;font-family:{Fuente};font-size:15px;line-height:22px;color:{TextoBajada}'>{cabecera.Bajada}</td>
</tr>";

            return $@"<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' bgcolor='{Lienzo}' style='border-collapse:collapse;width:100%;background-color:{Lienzo};margin:0;padding:0'>
<tr>
<td align='center' style='padding:24px 12px'>

<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='640' style='border-collapse:collapse;width:100%;max-width:640px;background-color:#FFFFFF;border:1px solid {Borde};border-radius:16px'>

<tr>
<td align='center' style='padding:30px 34px 0 34px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='border-collapse:collapse'>
<tr>
<td valign='middle' style='padding-right:12px'><img src='{_iconos}/{cabecera.Icono}.png' width='44' height='44' alt='' style='display:block;width:44px;height:44px;border:0;outline:none;text-decoration:none' /></td>
<td valign='middle' style='font-family:{Fuente};font-size:{tamTitulo};line-height:{altoTitulo};font-weight:700;color:{Azul};letter-spacing:-0.4px;white-space:nowrap'>{Esc(cabecera.Titulo)}</td>
</tr>
<tr>
<td colspan='2' align='center' style='padding-top:6px'><table role='presentation' cellpadding='0' cellspacing='0' border='0' width='84' align='center' style='border-collapse:collapse;width:84px'><tr><td height='4' bgcolor='{Lima}' style='height:4px;line-height:4px;font-size:0;background-color:{Lima};border-radius:2px'>&nbsp;</td></tr></table></td>
</tr>
</table>
</td>
</tr>
{bajada}{cuerpo}
<tr>
<td style='padding:22px 30px 0 30px'><table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse'><tr><td height='1' bgcolor='{Divisor}' style='height:1px;line-height:1px;font-size:0;background-color:{Divisor}'>&nbsp;</td></tr></table></td>
</tr>

<tr>
<td align='center' style='padding:20px 34px 0 34px'><img src='{_logoUrl}' width='150' alt='ABRIL Grupo Inmobiliario' style='display:block;width:150px;max-width:150px;height:auto;margin:0 auto;border:0;outline:none;text-decoration:none' /></td>
</tr>

<tr>
<td align='center' style='padding:12px 34px 28px 34px;font-family:{Fuente};font-size:11px;line-height:16px;color:{TextoPie}'>{Pie}</td>
</tr>

</table>

</td>
</tr>
</table>";
        }

        // ── Bloques ───────────────────────────────────────────────────────────

        /// <summary>
        /// Tarjeta de filas ícono / etiqueta / valor. La última fila no lleva línea inferior para no
        /// cortar el borde redondeado de la tarjeta. Devuelve vacío si no hay filas, para que el
        /// llamador pueda armarla condicionalmente sin preguntarse si quedó una tarjeta vacía.
        /// </summary>
        public string Tarjeta(IReadOnlyList<Fila> filas)
        {
            if (filas.Count == 0) return "";

            var html = new StringBuilder();
            for (var i = 0; i < filas.Count; i++)
            {
                var f = filas[i];
                var lineaInferior = i == filas.Count - 1 ? "" : $"border-bottom:1px solid {Separador};";

                html.Append($@"
<tr>
<td width='62' align='center' valign='top' style='width:62px;padding:14px 0 14px 10px;{lineaInferior}'><img src='{_iconos}/{f.Icono}.png' width='28' height='28' alt='' style='display:block;width:28px;height:28px;border:0;outline:none;text-decoration:none' /></td>
<td width='150' valign='top' style='width:150px;padding:14px 18px 14px 8px;{lineaInferior}border-right:1px solid {Divisor};font-family:{Fuente};font-size:15px;line-height:21px;font-weight:700;color:{Azul}'>{Esc(f.Etiqueta)}:</td>
<td valign='top' style='padding:14px 20px 14px 22px;{lineaInferior}font-family:{Fuente};font-size:15px;line-height:21px;color:{TextoValor}'>{f.ValorHtml}</td>
</tr>");
            }

            return $@"
<tr>
<td style='padding:22px 30px 0 30px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' bgcolor='{TarjetaFondo}' style='border-collapse:collapse;width:100%;background-color:{TarjetaFondo};border:1px solid {TarjetaBorde};border-radius:14px'>{html}
</table>
</td>
</tr>";
        }

        /// <summary>Título de sección (ícono chico + texto azul), el que va encima de una tabla.</summary>
        public string Seccion(string icono, string titulo) => $@"
<tr>
<td style='padding:26px 30px 0 30px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' style='border-collapse:collapse'>
<tr>
<td width='36' valign='middle' style='width:36px'><img src='{_iconos}/{icono}.png' width='26' height='26' alt='' style='display:block;width:26px;height:26px;border:0;outline:none;text-decoration:none' /></td>
<td valign='middle' style='font-family:{Fuente};font-size:16px;line-height:22px;font-weight:700;color:{Azul}'>{Esc(titulo)}</td>
</tr>
</table>
</td>
</tr>";

        /// <summary>
        /// Tabla con cabecera azul y esquinas redondeadas, alternando el fondo de las filas. El
        /// contenido de cada celda va como HTML ya escapado por el llamador (varias llevan un
        /// &lt;br&gt; con un dato secundario debajo).
        /// </summary>
        public string Tabla(IReadOnlyList<Columna> columnas, IReadOnlyList<IReadOnlyList<Celda>> filas)
        {
            const string baseTh =
                "padding:10px;font-family:" + Fuente + ";font-size:11.5px;line-height:15px;"
                + "font-weight:700;letter-spacing:0.3px;color:#FFFFFF";
            const string baseTd =
                "padding:11px 10px;font-family:" + Fuente + ";font-size:12.5px;line-height:18px";

            var cabecera = new StringBuilder("\n<tr>");
            for (var i = 0; i < columnas.Count; i++)
            {
                var c = columnas[i];
                var radio = i == 0 ? "border-top-left-radius:13px;"
                          : i == columnas.Count - 1 ? "border-top-right-radius:13px;"
                          : "";
                var alinea = Alinear(c.Alinea);
                cabecera.Append(
                    $"<th width='{c.Ancho}' align='{alinea}' bgcolor='{Azul}' style='width:{c.Ancho}px;background-color:{Azul};{radio}text-align:{alinea};{baseTh}'>{Esc(c.Titulo)}</th>");
            }
            cabecera.Append("</tr>");

            var cuerpo = new StringBuilder();
            for (var f = 0; f < filas.Count; f++)
            {
                var fondo  = f % 2 == 1 ? TarjetaFondo : "#FFFFFF";
                var ultima = f == filas.Count - 1;
                cuerpo.Append("\n<tr>");

                for (var i = 0; i < filas[f].Count; i++)
                {
                    var celda  = filas[f][i];
                    var alinea = Alinear(columnas[i].Alinea);
                    var radio  = !ultima ? ""
                               : i == 0 ? "border-bottom-left-radius:13px;"
                               : i == filas[f].Count - 1 ? "border-bottom-right-radius:13px;"
                               : "";

                    cuerpo.Append(
                        $"<td valign='top' align='{alinea}' bgcolor='{fondo}' style='background-color:{fondo};"
                        + $"border-top:1px solid {Separador};{radio}{baseTd};text-align:{alinea};"
                        + (celda.NoWrap ? "white-space:nowrap;" : "")
                        + (celda.Negrita ? "font-weight:700;" : "")
                        + $"color:{celda.Color ?? TextoValor}'>{celda.Html}</td>");
                }
                cuerpo.Append("</tr>");
            }

            return $@"
<tr>
<td style='padding:12px 30px 0 30px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:separate;border-spacing:0;width:100%;border:1px solid {TarjetaBorde};border-radius:14px'>{cabecera}{cuerpo}
</table>
</td>
</tr>";
        }

        /// <summary>
        /// Franja de estado: ícono redondo de color y un recuadro con el resultado en una línea o
        /// dos. Es para el desenlace (aprobado, rechazado, observado), no para explicar el proceso.
        /// </summary>
        public string Franja(string icono, Tono tono, string htmlTexto)
        {
            var (fondo, color) = tono switch
            {
                Tono.Verde => ("#F0FBF5", VerdeOk),
                Tono.Rojo  => ("#FEF2F2", RojoNo),
                Tono.Info  => ("#EEF7F3", "#115E4A"),
                _          => ("#FFF7E6", "#92600A"),
            };

            return $@"
<tr>
<td style='padding:18px 30px 0 30px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%' style='border-collapse:collapse'>
<tr>
<td width='54' valign='middle' style='width:54px'><img src='{_iconos}/{icono}.png' width='40' height='40' alt='' style='display:block;width:40px;height:40px;border:0;outline:none;text-decoration:none' /></td>
<td valign='middle' bgcolor='{fondo}' style='background-color:{fondo};border-radius:10px;padding:14px 18px;font-family:{Fuente};font-size:14px;line-height:20px;color:{color}'>{htmlTexto}</td>
</tr>
</table>
</td>
</tr>";
        }

        /// <summary>Botón verde de acción. Centrado, como en el resto de la familia.</summary>
        public string Boton(string texto, string url) => $@"
<tr>
<td align='center' style='padding:28px 30px 0 30px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='border-collapse:collapse'>
<tr>
<td align='center' bgcolor='{VerdeAccion}' style='background-color:{VerdeAccion};border-radius:10px'><a href='{Esc(url)}' style='display:inline-block;padding:14px 32px;font-family:{Fuente};font-size:15px;line-height:20px;font-weight:700;color:#FFFFFF;text-decoration:none'>{Esc(texto)}</a></td>
</tr>
</table>
</td>
</tr>";

        /// <summary>
        /// Par de botones lado a lado, para el correo que le pide al destinatario una respuesta
        /// (confirmar / rechazar la entrevista). Cada uno lleva su color porque la respuesta es lo
        /// que distingue a uno del otro: el verde de acción acepta y el rojo declina.
        ///
        /// Van en una tabla de dos celdas y no en dos <c>Boton</c> seguidos porque cada
        /// <c>Boton</c> es su propia fila del documento y quedarían uno encima del otro; la celda
        /// del medio es el separador (Outlook ignora los márgenes entre tablas hermanas).
        /// </summary>
        public string BotonesRespuesta(
            string textoPrimario, string urlPrimaria, string textoSecundario, string urlSecundaria) => $@"
<tr>
<td align='center' style='padding:28px 30px 0 30px'>
<table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='border-collapse:collapse'>
<tr>
<td align='center' bgcolor='{VerdeAccion}' style='background-color:{VerdeAccion};border-radius:10px'><a href='{Esc(urlPrimaria)}' style='display:inline-block;padding:14px 30px;font-family:{Fuente};font-size:15px;line-height:20px;font-weight:700;color:#FFFFFF;text-decoration:none'>{Esc(textoPrimario)}</a></td>
<td width='14' style='width:14px;font-size:0;line-height:0'>&nbsp;</td>
<td align='center' bgcolor='{RojoNo}' style='background-color:{RojoNo};border-radius:10px'><a href='{Esc(urlSecundaria)}' style='display:inline-block;padding:14px 30px;font-family:{Fuente};font-size:15px;line-height:20px;font-weight:700;color:#FFFFFF;text-decoration:none'>{Esc(textoSecundario)}</a></td>
</tr>
</table>
</td>
</tr>";

        /// <summary>
        /// La URL en texto debajo del botón. Reemplaza al párrafo de "si el botón no funciona,
        /// copia y pega este enlace": el enlace a la vista basta y no hay que explicarlo.
        /// </summary>
        public string EnlaceDirecto(string url) => $@"
<tr>
<td align='center' style='padding:14px 34px 0 34px;font-family:{Fuente};font-size:11.5px;line-height:17px;color:{TextoBajada}'>Enlace directo: <span style='color:{Azul};word-break:break-all'>{Esc(url)}</span></td>
</tr>";

        /// <summary>
        /// Párrafo de texto corrido. Solo para los correos a un candidato, donde una despedida o un
        /// agradecimiento no se pueden reemplazar por una tabla. En los correos internos no va.
        /// </summary>
        public string Parrafo(string htmlTexto) => $@"
<tr>
<td style='padding:18px 34px 0 34px;font-family:{Fuente};font-size:14px;line-height:21px;color:{TextoValor}'>{htmlTexto}</td>
</tr>";

        // ── Utilidades ────────────────────────────────────────────────────────

        public static string Esc(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);

        /// <summary>Escapa un texto de textarea conservando los saltos de línea que escribió el usuario.</summary>
        public static string EscMultilinea(string? valor) =>
            Esc(valor).Replace("\r\n", "<br />").Replace("\n", "<br />");

        private static string Alinear(Alineacion alineacion) => alineacion switch
        {
            Alineacion.Centro  => "center",
            Alineacion.Derecha => "right",
            _                  => "left",
        };
    }
}
