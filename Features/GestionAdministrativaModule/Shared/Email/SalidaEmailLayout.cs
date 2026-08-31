using Abril_Backend.Shared.Services.Email.Layout;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Email
{
    /// <summary>
    /// El <see cref="AbrilEmailLayout"/> con el pie de Gestión Administrativa · Salidas. Todo el
    /// chrome (tarjeta blanca sobre lienzo verdoso, cabecera centrada con el aro lima, tablas de
    /// cabecera azul, franjas de estado, botón verde y logo al pie) vive en la clase base: acá solo
    /// va lo propio del módulo.
    ///
    /// Es el mismo layout que usan los correos de Gestión GTH, a propósito: los correos de salidas
    /// que llegan a un trabajador tienen que verse como el resto de los de la intranet, no como
    /// otra aplicación.
    /// </summary>
    public sealed class SalidaEmailLayout : AbrilEmailLayout
    {
        private const string PieSalidas =
            "Correo automático de Abril One · Gestión Administrativa · Salidas.";

        public SalidaEmailLayout(string assetsUrl) : base(assetsUrl, PieSalidas) { }

        /// <summary>
        /// Layout con el origen de las imágenes que corresponde (<c>App:EmailAssetsUrl</c>, que en
        /// dev apunta a producción a propósito: Outlook descarga las imágenes por el proxy de
        /// Microsoft y ese proxy nunca puede alcanzar un localhost).
        /// </summary>
        public static SalidaEmailLayout Desde(IConfiguration configuration) =>
            new(AssetsUrl(configuration));
    }
}
