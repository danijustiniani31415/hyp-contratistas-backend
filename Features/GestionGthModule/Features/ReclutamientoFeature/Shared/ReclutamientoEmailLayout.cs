using Abril_Backend.Shared.Services.Email.Layout;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared
{
    /// <summary>
    /// El <see cref="AbrilEmailLayout"/> con el pie de Gestión GTH · Reclutamiento. Todo el chrome
    /// (tarjeta, cabecera, tablas, franjas, botones, colores) vive en la clase base, en
    /// <c>Shared/Services/Email/Layout/</c>: subió allá cuando Gestión Administrativa · Salidas
    /// empezó a usar el mismo diseño, y este archivo quedó como lo único propio del módulo — el
    /// pie y el criterio editorial de acá abajo. Si hay que tocar cómo se ve un correo, se toca la
    /// clase base y cambian los diez correos a la vez.
    ///
    /// Criterio editorial del módulo: el correo lleva datos y un acceso, no explicaciones. No se
    /// vuelve a agregar el párrafo que cuenta qué hace el botón, qué pasa después ni cómo sigue el
    /// flujo — eso se ve en la pantalla a la que lleva el enlace. La bajada es UNA línea.
    ///
    /// Ese criterio vale para quien entra a la app. Los dos correos a un candidato de fuera son la
    /// excepción y están así por pedido de GTH: el de fin de proceso (texto corrido) y el del PRIMER
    /// envío del formulario, que sí le cuenta qué se le pide y qué etapas vienen porque no tiene
    /// ninguna pantalla donde verlo. No "normalizarlos" al formato de los internos: los reenvíos
    /// siguientes de ese mismo formulario sí llevan el correo corto de siempre.
    /// </summary>
    public sealed class ReclutamientoEmailLayout : AbrilEmailLayout
    {
        /// <summary>
        /// Pie de TODOS los correos del módulo. Es el mismo para el candidato y para los internos:
        /// el código del requerimiento no va acá — es jerga nuestra y a quien postula no le dice
        /// nada.
        /// </summary>
        private const string PieReclutamiento =
            "Correo automático de Abril One · Gestión GTH · Reclutamiento.";

        public ReclutamientoEmailLayout(string assetsUrl) : base(assetsUrl, PieReclutamiento) { }

        /// <summary>
        /// Layout con el origen de las imágenes que corresponde. Ver <see cref="AssetsUrl"/> en la
        /// clase base para por qué esa clave es distinta de <c>App:FrontendUrl</c>.
        /// </summary>
        public static ReclutamientoEmailLayout Desde(IConfiguration configuration) =>
            new(AssetsUrl(configuration));
    }
}
