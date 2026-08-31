namespace Abril_Backend.Features.GestionAdministrativa.Shared.Email
{
    /// <summary>
    /// Las URLs de la intranet a las que llevan los botones de los correos de salidas. Están acá y
    /// no escritas en cada plantilla porque el botón tiene que caer en la solicitud EXACTA: si la
    /// query cambia de nombre, cambia en un solo lugar y no en tres correos.
    ///
    /// Ambas pantallas leen <c>?solicitud=</c> al entrar y abren ese detalle solo.
    /// </summary>
    public static class SalidaEnlaces
    {
        /// <summary>Base del frontend (<c>App:FrontendUrl</c>), sin barra final.</summary>
        public static string Base(IConfiguration configuration) =>
            (configuration["App:FrontendUrl"] ?? "https://intranet.abril.pe").TrimEnd('/');

        /// <summary>
        /// Gestión de Salidas abierta en esa solicitud — es la pantalla donde el jefe aprueba o
        /// rechaza el reembolso.
        /// </summary>
        public static string Gestion(IConfiguration configuration, int solicitudId) =>
            $"{Base(configuration)}/gestion-administrativa/gestion-salidas?solicitud={solicitudId}";

        /// <summary>
        /// Solicitud de Salidas (el autoservicio del trabajador) abierta en esa solicitud — es
        /// donde ve el resultado y donde vuelve a adjuntar el Consolidado del S10 para subsanar.
        /// </summary>
        public static string Autoservicio(IConfiguration configuration, int solicitudId) =>
            $"{Base(configuration)}/gestion-administrativa/solicitud-salidas?solicitud={solicitudId}";
    }
}
