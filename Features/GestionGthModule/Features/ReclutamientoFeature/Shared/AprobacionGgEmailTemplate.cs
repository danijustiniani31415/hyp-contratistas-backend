namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared
{
    /// <summary>
    /// Correo "Solicitud de personal por aprobar" que va a los gerentes.
    ///
    /// Es el correo del que salió el brandeo del resto: todo el chrome (logo, aros lima, tarjetas,
    /// tabla azul, botón verde) vive ahora en <see cref="ReclutamientoEmailLayout"/> y lo comparten
    /// los diez correos del módulo. Acá solo queda qué datos van y en qué orden.
    ///
    /// El correo NO explica el flujo de aprobación ni qué hace el botón: eso se ve en la pantalla
    /// «Aprobaciones». Acá solo van los datos de la solicitud y el acceso. No volver a agregar esos
    /// párrafos.
    /// </summary>
    public static class AprobacionGgEmailTemplate
    {
        /// <summary>Una vacante de la solicitud, con los importes y textos ya formateados.</summary>
        /// <param name="Salario">
        /// Sueldo ya formateado, o null cuando la vacante no lo declara — los reemplazos no lo
        /// piden. Si NINGUNA de las vacantes del correo lo trae, la tabla sale sin esa columna en
        /// vez de con una fila de guiones.
        /// </param>
        public sealed record Vacante(
            string Codigo,
            string Puesto,
            string Tipo,
            string? Reemplazado,
            string ProyectoObra,
            string? Salario);

        /// <summary>Datos que se muestran en el correo.</summary>
        /// <param name="Link">
        /// Acceso a «Aprobaciones». Null en el correo informativo al gerente del área: esas
        /// vacantes no las decide él, así que sale sin botón y sin enlace — un acceso a una
        /// pantalla donde no le aparecen sería peor que ninguno.
        /// </param>
        /// <param name="EsRecordatorio">true en el reenvío: agrega la franja ámbar de recordatorio.</param>
        public sealed record Datos(
            string Area,
            string? Solicitante,
            IReadOnlyList<Vacante> Vacantes,
            string? Justificacion,
            string? SustentoUrl,
            string? SustentoNombre,
            string? Link,
            bool EsRecordatorio);

        /// <param name="assetsUrl">
        /// Base pública desde donde se sirven las imágenes (App:EmailAssetsUrl). Tiene que ser
        /// alcanzable desde internet: la resuelve el proxy de imágenes de Outlook, no el cliente.
        /// </param>
        public static string Construir(Datos datos, string assetsUrl)
        {
            var l = new ReclutamientoEmailLayout(assetsUrl);
            static string Esc(string? valor) => ReclutamientoEmailLayout.Esc(valor);

            // ── Tarjeta de cabecera: quién pide ───────────────────────────────
            var datosSolicitud = new List<ReclutamientoEmailLayout.Fila>
            {
                new("req-area", "Área solicitante", Esc(datos.Area)),
            };
            if (!string.IsNullOrWhiteSpace(datos.Solicitante))
                datosSolicitud.Add(new("req-solicitante", "Solicitante", Esc(datos.Solicitante)));

            // ── Tarjeta de cierre: por qué lo pide y con qué respaldo ─────────
            var datosSustento = new List<ReclutamientoEmailLayout.Fila>();
            if (!string.IsNullOrWhiteSpace(datos.Justificacion))
                datosSustento.Add(new("req-justificacion", "Justificación", Esc(datos.Justificacion)));
            if (!string.IsNullOrWhiteSpace(datos.SustentoUrl))
                datosSustento.Add(new(
                    "req-sustento",
                    "Sustento adjunto",
                    ReclutamientoEmailTextos.Enlace(datos.SustentoUrl!, datos.SustentoNombre ?? "Ver documento")));

            // La columna del sueldo solo aparece si alguna vacante lo trae: una solicitud de puros
            // reemplazos no lo declara y la columna quedaría llena de guiones.
            var conSalario = datos.Vacantes.Any(v => !string.IsNullOrWhiteSpace(v.Salario));

            // Sin enlace, el correo es un aviso: no se le pide una firma a nadie, así que ni la
            // bajada ni la franja del recordatorio pueden hablar de una aprobación pendiente.
            var conAccion = !string.IsNullOrWhiteSpace(datos.Link);

            return l.Documento(
                new ReclutamientoEmailLayout.Cabecera(
                    "req-solicitud", "Solicitud de Personal",
                    conAccion ? "Pendiente de aprobación:" : "Registrada para tu conocimiento:"),
                datos.EsRecordatorio && conAccion
                    ? l.Franja("req-recordatorio", ReclutamientoEmailLayout.Tono.Ambar,
                        "<b>Recordatorio:</b> sigue pendiente de aprobación.")
                    : "",
                l.Tarjeta(datosSolicitud),
                l.Seccion("req-vacantes", $"Vacantes solicitadas ({datos.Vacantes.Count})"),
                l.Tabla(
                    conSalario
                        ? ReclutamientoEmailTextos.ColumnasVacantesConSalario
                        : ReclutamientoEmailTextos.ColumnasVacantes,
                    FilasVacantes(datos.Vacantes, conSalario)),
                l.Tarjeta(datosSustento),
                conAccion ? l.Boton("Revisar y aprobar", datos.Link!) : "",
                conAccion ? l.EnlaceDirecto(datos.Link!) : "");
        }

        /// <summary>
        /// Filas de la tabla de vacantes. La línea "Reemplaza a {trabajador}" va en la misma celda
        /// del tipo (vacía en las vacantes nuevas y en los requerimientos anteriores a que se
        /// pidiera ese dato) para no agregar una columna a una tabla que ya tiene cinco.
        /// </summary>
        /// <param name="conSalario">false cuando ninguna vacante del correo declara sueldo.</param>
        private static List<IReadOnlyList<ReclutamientoEmailLayout.Celda>> FilasVacantes(
            IReadOnlyList<Vacante> vacantes, bool conSalario)
        {
            var filas = new List<IReadOnlyList<ReclutamientoEmailLayout.Celda>>(vacantes.Count);
            foreach (var v in vacantes)
            {
                var celdas = new List<ReclutamientoEmailLayout.Celda>
                {
                    new(ReclutamientoEmailLayout.Esc(v.Codigo), Negrita: true, Color: ReclutamientoEmailLayout.Azul, NoWrap: true),
                    new(ReclutamientoEmailLayout.Esc(v.Puesto)),
                    new(ReclutamientoEmailLayout.Esc(v.Tipo) + ReclutamientoEmailTextos.Reemplaza(v.Reemplazado)),
                    new(ReclutamientoEmailLayout.Esc(v.ProyectoObra)),
                };
                if (conSalario)
                    celdas.Add(new(ReclutamientoEmailTextos.OGuion(v.Salario), Negrita: true, NoWrap: true));
                filas.Add(celdas);
            }
            return filas;
        }
    }
}
