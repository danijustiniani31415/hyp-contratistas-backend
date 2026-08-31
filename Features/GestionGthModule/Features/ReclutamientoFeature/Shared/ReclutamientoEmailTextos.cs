using Col = Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared.ReclutamientoEmailLayout.Columna;
using Alinea = Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared.ReclutamientoEmailLayout.Alineacion;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared
{
    /// <summary>
    /// Fragmentos que comparten dos o más correos del módulo: las columnas de la tabla de vacantes
    /// y los trocitos de HTML que se repiten dentro de una celda. Están juntos para que la tabla de
    /// vacantes se vea igual en el correo al gerente, en el de GTH y en el de TI — que es
    /// exactamente la misma tabla y antes tenía tres anchos y tres estilos distintos.
    /// </summary>
    public static class ReclutamientoEmailTextos
    {
        // Los anchos suman el ancho interno de la tarjeta (640 − 60 de padding = 580) para que las
        // columnas quepan sin apretarse. El de «Código» está calculado para que un REQ-AAAA-NNNN
        // entre en una sola línea: si alguna vez el formato del código crece, hay que subir ese
        // ancho y bajar otro.

        /// <summary>Tabla de vacantes de los correos que sí llevan el sueldo (gerentes y GTH).</summary>
        public static readonly IReadOnlyList<Col> ColumnasVacantesConSalario = new List<Col>
        {
            new("Código", 116),
            new("Puesto", 110),
            new("Tipo", 132),
            new("Proyecto / Obra", 118),
            new("Salario bruto", 104, Alinea.Derecha),
        };

        /// <summary>
        /// Tabla de vacantes sin el sueldo, para el correo a TI: no participa del reclutamiento ni
        /// arma la oferta, así que la banda salarial no es asunto suyo.
        /// </summary>
        public static readonly IReadOnlyList<Col> ColumnasVacantes = new List<Col>
        {
            new("Código", 130),
            new("Puesto", 150),
            new("Tipo", 150),
            new("Proyecto / Obra", 150),
        };

        /// <summary>
        /// Línea secundaria (gris, chica) bajo el dato principal de una celda. Devuelve vacío si no
        /// hay texto, para poder concatenarla sin preguntar. Fija <c>font-weight:400</c> porque
        /// también se cuelga de celdas en negrita, donde si no heredaría el grosor y dejaría de
        /// leerse como dato secundario.
        /// </summary>
        public static string Subtexto(string? texto) =>
            string.IsNullOrWhiteSpace(texto)
                ? ""
                : $"<br /><span style='font-size:11px;line-height:16px;font-weight:400;color:{ReclutamientoEmailLayout.TextoBajada}'>{ReclutamientoEmailLayout.Esc(texto)}</span>";

        /// <summary>
        /// Línea "Reemplaza a {trabajador}" bajo el tipo, dentro de la misma celda. Vacía en las
        /// vacantes nuevas y en los requerimientos anteriores a que se pidiera ese dato.
        /// </summary>
        public static string Reemplaza(string? trabajador) =>
            string.IsNullOrWhiteSpace(trabajador) ? "" : Subtexto($"Reemplaza a {trabajador}");

        /// <summary>Enlace dentro de una celda o de una fila de tarjeta (verde de acción, subrayado).</summary>
        public static string Enlace(string url, string texto) =>
            $"<a href='{ReclutamientoEmailLayout.Esc(url)}' style='color:{ReclutamientoEmailLayout.VerdeAccion};font-weight:700;text-decoration:underline;word-break:break-word'>{ReclutamientoEmailLayout.Esc(texto)}</a>";

        /// <summary>Guion cuando el dato no está: nunca se deja una celda o una fila en blanco.</summary>
        public static string OGuion(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? "—" : ReclutamientoEmailLayout.Esc(valor);
    }
}
